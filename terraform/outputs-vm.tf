# VM Agent Outputs

output "agent_vm_public_ip" {
  description = "Public IP address of the build agent VM"
  value       = azurerm_public_ip.agent.ip_address
}

output "agent_vm_name" {
  description = "Name of the build agent VM"
  value       = azurerm_linux_virtual_machine.agent.name
}

output "agent_ssh_command" {
  description = "SSH command to connect to the agent VM"
  value       = "ssh -i terraform/agent-ssh-key.pem azureuser@${azurerm_public_ip.agent.ip_address}"
}

output "agent_ssh_key_path" {
  description = "Path to the SSH private key"
  value       = local_file.ssh_private_key.filename
}
