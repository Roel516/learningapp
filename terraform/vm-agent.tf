# Self-hosted Azure DevOps Build Agent VM
# Uses B1s (Free tier eligible) - 1 vCPU, 1 GB RAM

# Virtual Network
resource "azurerm_virtual_network" "agent" {
  name                = "${var.project_name}-agent-vnet"
  address_space       = ["10.0.0.0/16"]
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

# Subnet
resource "azurerm_subnet" "agent" {
  name                 = "${var.project_name}-agent-subnet"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.agent.name
  address_prefixes     = ["10.0.1.0/24"]
}

# Public IP
resource "azurerm_public_ip" "agent" {
  name                = "${var.project_name}-agent-pip"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  allocation_method   = "Static"
  sku                 = "Standard"

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

# Network Security Group
resource "azurerm_network_security_group" "agent" {
  name                = "${var.project_name}-agent-nsg"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  # Allow SSH
  security_rule {
    name                       = "SSH"
    priority                   = 1001
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "22"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

# Network Interface
resource "azurerm_network_interface" "agent" {
  name                = "${var.project_name}-agent-nic"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  ip_configuration {
    name                          = "internal"
    subnet_id                     = azurerm_subnet.agent.id
    private_ip_address_allocation = "Dynamic"
    public_ip_address_id          = azurerm_public_ip.agent.id
  }

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

# Connect NSG to NIC
resource "azurerm_network_interface_security_group_association" "agent" {
  network_interface_id      = azurerm_network_interface.agent.id
  network_security_group_id = azurerm_network_security_group.agent.id
}

# Generate SSH key
resource "tls_private_key" "agent_ssh" {
  algorithm = "RSA"
  rsa_bits  = 4096
}

# Virtual Machine - B1s (Free tier)
resource "azurerm_linux_virtual_machine" "agent" {
  name                = "${var.project_name}-agent-vm"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  size                = "Standard_B1s"
  admin_username      = "azureuser"

  network_interface_ids = [
    azurerm_network_interface.agent.id,
  ]

  admin_ssh_key {
    username   = "azureuser"
    public_key = tls_private_key.agent_ssh.public_key_openssh
  }

  os_disk {
    caching              = "ReadWrite"
    storage_account_type = "Standard_LRS"
    disk_size_gb         = 30
  }

  source_image_reference {
    publisher = "Canonical"
    offer     = "0001-com-ubuntu-server-jammy"
    sku       = "22_04-lts-gen2"
    version   = "latest"
  }

  # Cloud-init script to setup Azure DevOps agent
  custom_data = base64encode(templatefile("${path.module}/agent-setup.sh", {
    azdo_url   = var.azdo_org_url
    azdo_pat   = var.azdo_pat
    agent_pool = "Default"
    agent_name = "${var.project_name}-agent"
  }))

  tags = {
    Environment = var.environment
    Project     = var.project_name
  }
}

# Store SSH private key in local file (for access)
resource "local_file" "ssh_private_key" {
  content         = tls_private_key.agent_ssh.private_key_pem
  filename        = "${path.module}/agent-ssh-key.pem"
  file_permission = "0600"
}
