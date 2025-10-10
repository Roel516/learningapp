terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    azuredevops = {
      source  = "microsoft/azuredevops"
      version = "~> 0.11"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 2.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}

provider "azuredevops" {
  org_service_url       = var.azdo_org_url
  personal_access_token = var.azdo_pat
}

# Data sources to automatically retrieve Azure information
data "azurerm_client_config" "current" {}

data "azurerm_subscription" "current" {}

# Generate random password for service principal
resource "random_password" "sp_password" {
  length  = 32
  special = true
}

# Create Azure AD Application for Azure DevOps
resource "azuread_application" "azdo_sp" {
  display_name = "${var.project_name}-azdo-sp"
  owners       = [data.azurerm_client_config.current.object_id]
}

# Create Service Principal
resource "azuread_service_principal" "azdo_sp" {
  client_id = azuread_application.azdo_sp.client_id
  owners    = [data.azurerm_client_config.current.object_id]
}

# Create Service Principal Password
resource "azuread_service_principal_password" "azdo_sp" {
  service_principal_id = azuread_service_principal.azdo_sp.object_id
  end_date_relative    = "8760h" # 1 year
}

# Assign Contributor role to Service Principal on the subscription
resource "azurerm_role_assignment" "azdo_sp_contributor" {
  scope                = data.azurerm_subscription.current.id
  role_definition_name = "Contributor"
  principal_id         = azuread_service_principal.azdo_sp.object_id
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = "${var.project_name}-rg"
  location = var.location

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

# App Service Plan
resource "azurerm_service_plan" "main" {
  name                = "${var.project_name}-plan"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = var.app_service_sku

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

# SQL Server
resource "azurerm_mssql_server" "main" {
  name                         = "${var.project_name}-sqlserver"
  resource_group_name          = azurerm_resource_group.main.name
  location                     = azurerm_resource_group.main.location
  version                      = "12.0"
  administrator_login          = var.sql_admin_username
  administrator_login_password = var.sql_admin_password

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

# SQL Database
resource "azurerm_mssql_database" "main" {
  name           = "${var.project_name}-db"
  server_id      = azurerm_mssql_server.main.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  sku_name       = var.sql_sku
  max_size_gb    = 2
  zone_redundant = false

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

# Firewall rule to allow Azure services
resource "azurerm_mssql_firewall_rule" "allow_azure" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# Optional: Firewall rule to allow your IP (for development)
resource "azurerm_mssql_firewall_rule" "allow_dev_ip" {
  count            = var.dev_ip_address != "" ? 1 : 0
  name             = "AllowDevIP"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = var.dev_ip_address
  end_ip_address   = var.dev_ip_address
}

# App Service (hosts both API and Blazor WASM)
resource "azurerm_linux_web_app" "main" {
  name                = "${var.project_name}-app"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  service_plan_id     = azurerm_service_plan.main.id

  site_config {
    always_on = true

    application_stack {
      dotnet_version = "8.0"
    }

    cors {
      allowed_origins = ["*"]
    }
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT"                = var.environment
    "WEBSITE_RUN_FROM_PACKAGE"              = "1"
    "SCM_DO_BUILD_DURING_DEPLOYMENT"        = "false"

    # Google OAuth settings
    "Authentication__Google__ClientId"      = var.google_client_id
    "Authentication__Google__ClientSecret"  = var.google_client_secret
  }

  connection_string {
    name  = "DefaultConnection"
    type  = "SQLAzure"
    value = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }

  https_only = true

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

# Application Insights (optional but recommended)
resource "azurerm_application_insights" "main" {
  count               = var.enable_app_insights ? 1 : 0
  name                = "${var.project_name}-appinsights"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  application_type    = "web"
  workspace_id        = "/subscriptions/2fb2baa4-e931-4c9d-b63a-4f4c56f37358/resourceGroups/ai_learningresources-roel-appinsights_11b950cc-4419-4c56-94dd-8f21aaada60d_managed/providers/Microsoft.OperationalInsights/workspaces/managed-learningresources-roel-appinsights-ws"

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

# Azure DevOps Project
resource "azuredevops_project" "main" {
  name               = var.project_name
  description        = "Learning Resources App - Blazor WASM + ASP.NET Core"
  visibility         = "private"
  version_control    = "Git"
  work_item_template = "Agile"

  features = {
    "pipelines"    = "enabled"
    "repositories" = "enabled"
    "boards"       = "disabled"
    "testplans"    = "disabled"
    "artifacts"    = "disabled"
  }
}

# Service Connection to Azure
resource "azuredevops_serviceendpoint_azurerm" "main" {
  project_id            = azuredevops_project.main.id
  service_endpoint_name = "Azure-${var.project_name}"
  description           = "Managed by Terraform"

  credentials {
    serviceprincipalid  = azuread_service_principal.azdo_sp.client_id
    serviceprincipalkey = azuread_service_principal_password.azdo_sp.value
  }

  azurerm_spn_tenantid      = data.azurerm_client_config.current.tenant_id
  azurerm_subscription_id   = data.azurerm_subscription.current.subscription_id
  azurerm_subscription_name = data.azurerm_subscription.current.display_name

  depends_on = [azurerm_role_assignment.azdo_sp_contributor]
}

# GitHub Service Connection
resource "azuredevops_serviceendpoint_github" "main" {
  project_id            = azuredevops_project.main.id
  service_endpoint_name = "GitHub-${var.project_name}"
  description           = "Managed by Terraform"

  auth_personal {
    personal_access_token = var.github_token
  }
}

# Variable Group for Pipeline
resource "azuredevops_variable_group" "main" {
  project_id   = azuredevops_project.main.id
  name         = "${var.project_name}-variables"
  description  = "Variables for ${var.project_name} pipeline"
  allow_access = true

  variable {
    name  = "azureSubscription"
    value = azuredevops_serviceendpoint_azurerm.main.service_endpoint_name
  }

  variable {
    name  = "webAppName"
    value = azurerm_linux_web_app.main.name
  }

  variable {
    name  = "resourceGroupName"
    value = azurerm_resource_group.main.name
  }

  variable {
    name  = "sqlConnectionString"
    value         = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
    is_secret     = true
    secret_value  = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }

  variable {
    name          = "googleClientId"
    value         = var.google_client_id != "" ? var.google_client_id : "not-set"
    is_secret     = true
    secret_value  = var.google_client_id
  }

  variable {
    name          = "googleClientSecret"
    value         = var.google_client_secret != "" ? var.google_client_secret : "not-set"
    is_secret     = true
    secret_value  = var.google_client_secret
  }
}

# Build Definition (Pipeline)
resource "azuredevops_build_definition" "main" {
  project_id = azuredevops_project.main.id
  name       = "${var.project_name}-CI-CD"

  ci_trigger {
    use_yaml = true
  }

  repository {
    repo_type             = "GitHub"
    repo_id               = "${var.github_owner}/${var.github_repo_name}"
    branch_name           = "refs/heads/main"
    yml_path              = "azure-pipelines.yml"
    service_connection_id = azuredevops_serviceendpoint_github.main.id
  }

  variable_groups = [
    azuredevops_variable_group.main.id
  ]
}
