#!/bin/bash
set -e

# Update system
sudo apt-get update
sudo apt-get upgrade -y

# Install required packages
sudo apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    software-properties-common \
    git \
    jq \
    wget \
    unzip

# Install .NET 8.0 SDK
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

# Install Docker (optional but useful for builds)
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker azureuser
rm get-docker.sh

# Create agent directory
sudo mkdir -p /opt/azagent
cd /opt/azagent

# Download and extract Azure Pipelines agent
AGENT_VERSION=$(curl -s https://api.github.com/repos/microsoft/azure-pipelines-agent/releases/latest | jq -r '.tag_name' | sed 's/v//')
wget "https://vstsagentpackage.azureedge.net/agent/$${AGENT_VERSION}/vsts-agent-linux-x64-$${AGENT_VERSION}.tar.gz"
tar zxvf "vsts-agent-linux-x64-$${AGENT_VERSION}.tar.gz"
rm "vsts-agent-linux-x64-$${AGENT_VERSION}.tar.gz"

# Set ownership
sudo chown -R azureuser:azureuser /opt/azagent

# Configure agent (as azureuser)
sudo -u azureuser bash -c "cd /opt/azagent && ./config.sh \
    --unattended \
    --url '${azdo_url}' \
    --auth pat \
    --token '${azdo_pat}' \
    --pool '${agent_pool}' \
    --agent '${agent_name}' \
    --replace \
    --acceptTeeEula"

# Install and start as a service
cd /opt/azagent
sudo ./svc.sh install azureuser
sudo ./svc.sh start

# Enable auto-updates
echo "Agent setup completed successfully"
