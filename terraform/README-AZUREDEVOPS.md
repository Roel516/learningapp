# Azure DevOps Pipelines Deployment with Terraform

This guide covers deploying the Learning Resources App to Azure using **Azure DevOps Pipelines** instead of GitHub Actions.

## Overview

The Terraform configuration creates:
- **Azure Infrastructure**: App Service, SQL Database, Application Insights
- **Azure DevOps Project**: Project with Pipelines enabled
- **Service Connections**: Links to Azure and GitHub
- **Variable Groups**: Stores secrets and configuration
- **Build Pipeline**: YAML-based CI/CD pipeline

## Prerequisites

### 1. Azure DevOps Organization
- Create at https://dev.azure.com
- Note your organization URL (e.g., `https://dev.azure.com/yourorg`)

### 2. Azure DevOps Personal Access Token (PAT)
Create at: https://dev.azure.com/yourorg/_usersSettings/tokens

Required scopes:
- **Project and Team**: Read, write, & manage
- **Build**: Read & execute
- **Service Connections**: Read, query, & manage
- **Variable Groups**: Read, create, & manage

### 3. GitHub Personal Access Token
Create at: https://github.com/settings/tokens

Required scopes:
- `repo` (Full control of private repositories)

### 4. Terraform & Azure CLI
- Terraform: https://www.terraform.io/downloads
- Azure CLI: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli

**Note:** Terraform will automatically:
- Retrieve your Azure subscription and tenant information
- Create a service principal for Azure DevOps
- Assign appropriate permissions
- Configure the Azure DevOps service connection

## Setup Instructions

### Step 1: Configure Terraform Variables

```bash
cd terraform
cp terraform.tfvars.example terraform.tfvars
```

Edit `terraform.tfvars`:

```hcl
# Project Configuration
project_name = "learningresources-yourname"  # Must be globally unique
environment  = "prod"
location     = "West Europe"

# Azure Infrastructure
app_service_sku    = "B1"
sql_admin_username = "sqladmin"
sql_admin_password = "YourSecurePassword123!"  # Change this!
sql_sku           = "Basic"

# GitHub Configuration (source code location)
github_token     = "ghp_YourGitHubToken"
github_owner     = "your-github-username"
github_repo_name = "your-repo-name"

# Google OAuth (optional)
google_client_id     = "your-google-client-id.apps.googleusercontent.com"
google_client_secret = "your-google-client-secret"

# Azure DevOps Configuration
azdo_org_url = "https://dev.azure.com/yourorg"
azdo_pat     = "your-azdo-pat-here"
```

**Note:** You no longer need to provide Azure subscription, tenant, or service principal information. Terraform will automatically:
- Detect your current Azure subscription and tenant
- Create a service principal for Azure DevOps
- Configure all necessary permissions

### Step 2: Initialize Terraform

```bash
terraform init
```

### Step 3: Deploy Infrastructure

```bash
# Preview changes
terraform plan

# Apply (creates all resources)
terraform apply
```

Type `yes` when prompted. This will create:
- Azure resources (App Service, SQL Database, etc.)
- Azure DevOps project
- Service connections
- Variable groups
- Build pipeline definition

### Step 4: Push Code to GitHub

The pipeline is configured to trigger on commits to `main` branch:

```bash
# Ensure azure-pipelines.yml is in repository root
git add azure-pipelines.yml
git commit -m "Add Azure DevOps pipeline"
git push origin main
```

### Step 5: Monitor Pipeline Execution

Get the pipeline URL from Terraform output:

```bash
terraform output azdo_pipeline_url
```

Or navigate manually:
1. Go to https://dev.azure.com/yourorg
2. Open your project
3. Click **Pipelines** in left menu
4. Select your pipeline
5. Watch the build and deployment progress

## Pipeline Workflow

The `azure-pipelines.yml` pipeline performs these steps:

### Build Stage
1. **Setup .NET SDK** - Installs .NET 9.0
2. **Restore packages** - Downloads NuGet dependencies
3. **Build solution** - Compiles all projects
4. **Run tests** - Executes all 30 unit tests (must pass)
5. **Publish** - Creates deployment package
6. **Upload artifacts** - Stores build output

### Deploy Stage
1. **Download artifacts** - Retrieves build package
2. **Deploy to Azure** - Pushes to App Service
3. **Configure settings** - Sets environment variables and connection strings
4. **Database migrations** - Applied automatically on app startup

## Configuration

### App Settings

App settings are configured in `azure-pipelines.yml`:
- `ASPNETCORE_ENVIRONMENT=Production`
- `Authentication__Google__ClientId` (from variable group)
- `Authentication__Google__ClientSecret` (from variable group)

### Connection Strings

SQL connection string is automatically configured from the variable group.

### Variable Groups

The Terraform configuration creates a variable group named `{project_name}-variables` with:
- `azureSubscription` - Service connection name
- `webAppName` - App Service name
- `resourceGroupName` - Resource group name
- `sqlConnectionString` - Database connection (secret)
- `googleClientId` - OAuth client ID (secret)
- `googleClientSecret` - OAuth secret (secret)

### Modify Pipeline

Edit `azure-pipelines.yml` to customize:
- Build configuration
- Test execution
- Deployment steps
- Additional stages (e.g., staging environment)

## Accessing Your Application

Get the application URL:

```bash
terraform output app_service_url
# Output: https://learningresources-yourname-app.azurewebsites.net
```

Default admin credentials:
- Email: `admin@admin.nl`
- Password: `admin123`

## Troubleshooting

### Pipeline Fails at Build

Check build logs in Azure DevOps:
1. Open pipeline run
2. Click failed task
3. Review error messages

Common issues:
- NuGet restore failures (check internet connectivity)
- Build errors (verify code compiles locally)
- Test failures (run `dotnet test` locally)

### Pipeline Fails at Deploy

**Error: "No such host is known"**
- Verify service principal has Contributor role
- Check subscription ID is correct

**Error: "Web app not found"**
- Ensure Terraform applied successfully
- Verify app service name in variable group

### Database Connection Errors

View App Service logs:
```bash
az webapp log tail --name {app-name} --resource-group {rg-name}
```

Check:
- SQL firewall allows Azure services
- Connection string format is correct
- Database exists and is accessible

### Pipeline Not Triggering

Ensure:
- `azure-pipelines.yml` is in repository root
- GitHub service connection is authorized
- CI trigger is enabled in pipeline settings

## Viewing Logs

### Pipeline Logs
- Azure DevOps: Pipelines → Select run → View logs

### Application Logs
```bash
# Stream live logs
az webapp log tail --name {app-name} --resource-group {rg-name}

# Download logs
az webapp log download --name {app-name} --resource-group {rg-name}
```

### SQL Database Logs
- Azure Portal → SQL Database → Query Performance Insight
- Azure Portal → SQL Database → Intelligent Performance

## Scaling

### Change App Service SKU

Edit `terraform.tfvars`:
```hcl
app_service_sku = "P1v2"  # Upgrade to Premium
```

Apply changes:
```bash
terraform apply
```

### Change Database SKU

Edit `terraform.tfvars`:
```hcl
sql_sku = "S1"  # Upgrade to Standard
```

Apply changes:
```bash
terraform apply
```

## Cost Estimates

**Monthly costs** (West Europe):

| Configuration | App Service | SQL Database | Total |
|---------------|-------------|--------------|-------|
| Development (B1 + Basic) | €13 | €5 | ~€20 |
| Production (P1v2 + S1) | €150 | €30 | ~€180 |

Application Insights: ~€5/month (pay-as-you-go)

## Adding Staging Slot

Enable staging in `terraform.tfvars`:
```hcl
enable_staging_slot = true
```

Then update pipeline to deploy to staging first:
```yaml
- task: AzureWebApp@1
  inputs:
    slotName: 'staging'
```

After verification, swap slots:
```bash
az webapp deployment slot swap \
  --name {app-name} \
  --resource-group {rg-name} \
  --slot staging \
  --target-slot production
```

## Security Best Practices

1. **Rotate secrets regularly**
   - Service principal keys
   - Azure DevOps PAT
   - SQL admin password

2. **Use Azure Key Vault** (production)
   - Store secrets in Key Vault
   - Reference in App Service configuration

3. **Enable managed identity**
   - Remove SQL username/password
   - Use Azure AD authentication

4. **Restrict network access**
   - Configure App Service IP restrictions
   - Use Private Endpoints for SQL

5. **Enable security features**
   ```bash
   # Enable HTTPS only
   az webapp update --name {app-name} --resource-group {rg-name} --https-only true

   # Set minimum TLS version
   az webapp config set --name {app-name} --resource-group {rg-name} --min-tls-version 1.2
   ```

## Cleanup

To destroy all resources:

```bash
terraform destroy
```

Type `yes` to confirm. This will:
- Delete all Azure resources
- Remove Azure DevOps project
- **Permanently delete all data**

## Comparison: Azure DevOps vs GitHub Actions

| Feature | Azure DevOps | GitHub Actions |
|---------|--------------|----------------|
| **Cost** | 1800 free minutes/month | 2000 free minutes/month |
| **Integration** | Native Azure integration | Requires publish profile |
| **Secrets** | Variable groups | Repository secrets |
| **Artifacts** | Built-in artifact storage | Actions artifacts |
| **Environments** | Environments & approvals | Environments & protection rules |
| **Boards** | Integrated work items | Third-party integrations |

## Support

- Azure DevOps docs: https://docs.microsoft.com/en-us/azure/devops/
- Terraform AzureRM provider: https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs
- Terraform Azure DevOps provider: https://registry.terraform.io/providers/microsoft/azuredevops/latest/docs
