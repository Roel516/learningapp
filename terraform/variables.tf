# Project Configuration
variable "project_name" {
  description = "Name of the project (used for resource naming)"
  type        = string
  default     = "learningresources"
}

variable "environment" {
  description = "Environment (dev, staging, prod)"
  type        = string
  default     = "prod"
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "West Europe"
}

# App Service Configuration
variable "app_service_sku" {
  description = "App Service Plan SKU (B1, S1, P1v2, etc.)"
  type        = string
  default     = "B1"
}

# SQL Server Configuration
variable "sql_admin_username" {
  description = "SQL Server administrator username"
  type        = string
  default     = "sqladmin"
}

variable "sql_admin_password" {
  description = "SQL Server administrator password"
  type        = string
  sensitive   = true
}

variable "sql_sku" {
  description = "SQL Database SKU (Basic, S0, S1, etc.)"
  type        = string
  default     = "Basic"
}

variable "dev_ip_address" {
  description = "Your development IP address for SQL firewall (optional)"
  type        = string
  default     = ""
}

# GitHub Configuration
variable "github_token" {
  description = "GitHub Personal Access Token"
  type        = string
  sensitive   = true
}

variable "github_owner" {
  description = "GitHub repository owner (username or organization)"
  type        = string
}

variable "github_repo_name" {
  description = "GitHub repository name"
  type        = string
}

# Google OAuth Configuration
variable "google_client_id" {
  description = "Google OAuth Client ID"
  type        = string
  default     = ""
  sensitive   = true
}

variable "google_client_secret" {
  description = "Google OAuth Client Secret"
  type        = string
  default     = ""
  sensitive   = true
}

# Optional Features
variable "enable_staging_slot" {
  description = "Enable staging deployment slot"
  type        = bool
  default     = false
}

variable "enable_app_insights" {
  description = "Enable Application Insights monitoring"
  type        = bool
  default     = true
}

# Azure DevOps Configuration
variable "azdo_org_url" {
  description = "Azure DevOps organization URL (e.g., https://dev.azure.com/yourorg)"
  type        = string
  default     = ""
}

variable "azdo_pat" {
  description = "Azure DevOps Personal Access Token"
  type        = string
  sensitive   = true
  default     = ""
}
