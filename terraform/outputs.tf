output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.main.name
}

output "app_service_name" {
  description = "Name of the App Service"
  value       = azurerm_linux_web_app.main.name
}

output "app_service_url" {
  description = "URL of the deployed application"
  value       = "https://${azurerm_linux_web_app.main.default_hostname}"
}

output "sql_server_fqdn" {
  description = "Fully qualified domain name of the SQL Server"
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "sql_database_name" {
  description = "Name of the SQL Database"
  value       = azurerm_mssql_database.main.name
}

output "app_insights_instrumentation_key" {
  description = "Application Insights instrumentation key"
  value       = var.enable_app_insights ? azurerm_application_insights.main[0].instrumentation_key : null
  sensitive   = true
}

output "deployment_instructions" {
  description = "Next steps for deployment"
  value       = <<-EOT

    ✅ Infrastructure deployed successfully!

    🌐 App URL: https://${azurerm_linux_web_app.main.default_hostname}
    🗄️  SQL Server: ${azurerm_mssql_server.main.fully_qualified_domain_name}

    📋 Next Steps:
    1. GitHub secrets have been automatically configured
    2. Create GitHub Actions workflow (see .github/workflows/azure-deploy.yml)
    3. Push to GitHub to trigger deployment
    4. Run database migrations on first deployment

    🔐 To connect to SQL Server locally:
       Server: ${azurerm_mssql_server.main.fully_qualified_domain_name}
       Database: ${azurerm_mssql_database.main.name}
       Username: ${var.sql_admin_username}
       Password: [from your terraform.tfvars]

  EOT
}
