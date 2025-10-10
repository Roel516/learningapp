# Azure DevOps Outputs

output "azdo_project_id" {
  description = "Azure DevOps Project ID"
  value       = azuredevops_project.main.id
}

output "azdo_project_url" {
  description = "Azure DevOps Project URL"
  value       = "${var.azdo_org_url}/${azuredevops_project.main.name}"
}

output "azdo_pipeline_id" {
  description = "Azure DevOps Build Pipeline ID"
  value       = azuredevops_build_definition.main.id
}

output "azdo_pipeline_url" {
  description = "Azure DevOps Pipeline URL"
  value       = "${var.azdo_org_url}/${azuredevops_project.main.name}/_build?definitionId=${azuredevops_build_definition.main.id}"
}

output "azdo_variable_group_id" {
  description = "Azure DevOps Variable Group ID"
  value       = azuredevops_variable_group.main.id
}

# Auto-generated Service Principal Info
output "service_principal_application_id" {
  description = "Service Principal Application (Client) ID"
  value       = azuread_service_principal.azdo_sp.client_id
}

output "service_principal_object_id" {
  description = "Service Principal Object ID"
  value       = azuread_service_principal.azdo_sp.object_id
}

output "service_principal_display_name" {
  description = "Service Principal Display Name"
  value       = azuread_application.azdo_sp.display_name
}

# Azure Subscription Info
output "subscription_id" {
  description = "Azure Subscription ID"
  value       = data.azurerm_subscription.current.subscription_id
}

output "subscription_name" {
  description = "Azure Subscription Name"
  value       = data.azurerm_subscription.current.display_name
}

output "tenant_id" {
  description = "Azure Tenant ID"
  value       = data.azurerm_client_config.current.tenant_id
}
