terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
    github = {
      source  = "integrations/github"
      version = "~> 5.0"
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

provider "github" {
  token = var.github_token
  owner = var.github_owner
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
      dotnet_version = "9.0"
    }

    cors {
      allowed_origins = ["*"]
    }
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT"                = var.environment
    "WEBSITE_RUN_FROM_PACKAGE"              = "1"
    "SCM_DO_BUILD_DURING_DEPLOYMENT"        = "false"

    # Google OAuth settings (set these in GitHub secrets)
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

# Get App Service publish profile
resource "azurerm_linux_web_app_slot" "staging" {
  count          = var.enable_staging_slot ? 1 : 0
  name           = "staging"
  app_service_id = azurerm_linux_web_app.main.id

  site_config {
    always_on = false

    application_stack {
      dotnet_version = "9.0"
    }
  }

  tags = {
    Environment = "Staging"
    Project     = var.project_name
  }
}

# Create GitHub Actions secrets
resource "github_actions_secret" "azure_webapp_publish_profile" {
  repository      = var.github_repo_name
  secret_name     = "AZURE_WEBAPP_PUBLISH_PROFILE"
  plaintext_value = azurerm_linux_web_app.main.site_credential[0].password
}

resource "github_actions_secret" "azure_webapp_name" {
  repository      = var.github_repo_name
  secret_name     = "AZURE_WEBAPP_NAME"
  plaintext_value = azurerm_linux_web_app.main.name
}

resource "github_actions_secret" "sql_connection_string" {
  repository      = var.github_repo_name
  secret_name     = "SQL_CONNECTION_STRING"
  plaintext_value = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Initial Catalog=${azurerm_mssql_database.main.name};Persist Security Info=False;User ID=${var.sql_admin_username};Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
}

resource "github_actions_secret" "google_client_id" {
  count           = var.google_client_id != "" ? 1 : 0
  repository      = var.github_repo_name
  secret_name     = "GOOGLE_CLIENT_ID"
  plaintext_value = var.google_client_id
}

resource "github_actions_secret" "google_client_secret" {
  count           = var.google_client_secret != "" ? 1 : 0
  repository      = var.github_repo_name
  secret_name     = "GOOGLE_CLIENT_SECRET"
  plaintext_value = var.google_client_secret
}

# Application Insights (optional but recommended)
resource "azurerm_application_insights" "main" {
  count               = var.enable_app_insights ? 1 : 0
  name                = "${var.project_name}-appinsights"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  application_type    = "web"

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

# Add Application Insights connection string to App Service
resource "azurerm_linux_web_app_slot" "main_with_insights" {
  count          = var.enable_app_insights ? 1 : 0
  name           = "production"
  app_service_id = azurerm_linux_web_app.main.id

  app_settings = merge(
    azurerm_linux_web_app.main.app_settings,
    {
      "APPLICATIONINSIGHTS_CONNECTION_STRING" = azurerm_application_insights.main[0].connection_string
      "ApplicationInsightsAgent_EXTENSION_VERSION" = "~3"
    }
  )
}
