# Azure Deployment with Terraform

This Terraform configuration deploys the Learning Resources App to Azure with:
- **Azure App Service** (Linux, .NET 9.0) hosting both API and Blazor WASM client
- **Azure SQL Database** for data storage
- **Application Insights** for monitoring (optional)
- **GitHub Actions** integration for CI/CD

## Prerequisites

1. **Azure CLI** - Install from https://docs.microsoft.com/en-us/cli/azure/install-azure-cli
2. **Terraform** - Install from https://www.terraform.io/downloads
3. **GitHub Personal Access Token** - Create at https://github.com/settings/tokens
   - Required scopes: `repo`, `admin:repo_hook`

## Setup Instructions

### 1. Authenticate with Azure

```bash
az login
az account show  # Verify correct subscription
```

### 2. Configure Terraform Variables

```bash
cd terraform
cp terraform.tfvars.example terraform.tfvars
```

Edit `terraform.tfvars` with your values:

```hcl
project_name       = "learningresources"
github_token       = "ghp_your_token_here"
github_owner       = "your-username"
github_repo_name   = "your-repo-name"
sql_admin_password = "YourSecurePassword123!"

# Optional
google_client_id     = "your-client-id"
google_client_secret = "your-client-secret"
```

### 3. Initialize Terraform

```bash
terraform init
```

### 4. Preview Changes

```bash
terraform plan
```

### 5. Deploy Infrastructure

```bash
terraform apply
```

Type `yes` when prompted. Deployment takes 5-10 minutes.

### 6. Push to GitHub

After Terraform completes:

```bash
git add .
git commit -m "Add Azure deployment configuration"
git push origin main
```

GitHub Actions will automatically:
- Build the solution
- Run tests (all 32 tests must pass)
- Publish to Azure
- Run database migrations

## What Gets Created

| Resource | Name | Purpose |
|----------|------|---------|
| Resource Group | `{project_name}-rg` | Container for all resources |
| App Service Plan | `{project_name}-plan` | Compute resources |
| App Service | `{project_name}-app` | Hosts the application |
| SQL Server | `{project_name}-sqlserver` | Database server |
| SQL Database | `{project_name}-db` | Application database |
| Application Insights | `{project_name}-appinsights` | Monitoring & telemetry |

## Automatic GitHub Secrets

Terraform automatically creates these GitHub secrets:

- `AZURE_WEBAPP_NAME` - App Service name
- `AZURE_WEBAPP_PUBLISH_PROFILE` - Deployment credentials
- `SQL_CONNECTION_STRING` - Database connection
- `GOOGLE_CLIENT_ID` - OAuth configuration
- `GOOGLE_CLIENT_SECRET` - OAuth configuration

## Post-Deployment

### Access Your Application

```bash
terraform output app_service_url
# Output: https://learningresources-app.azurewebsites.net
```

### Connect to SQL Database

Use these connection details from `terraform output`:

```
Server: {project_name}-sqlserver.database.windows.net
Database: {project_name}-db
Username: sqladmin (or your configured username)
Password: [from terraform.tfvars]
```

**Connection string:**
```
Server=tcp:{project_name}-sqlserver.database.windows.net,1433;Initial Catalog={project_name}-db;Persist Security Info=False;User ID=sqladmin;Password={your_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### View Logs

```bash
# Stream logs from Azure
az webapp log tail --name {app-name} --resource-group {rg-name}

# Or in Azure Portal
# Go to: App Service > Monitoring > Log stream
```

### View Deployments

Check GitHub Actions: https://github.com/{owner}/{repo}/actions

## Configuration

### Scaling

Edit `terraform.tfvars` to change SKUs:

```hcl
# Development
app_service_sku = "B1"
sql_sku        = "Basic"

# Production
app_service_sku = "P1v2"
sql_sku        = "S1"
```

Then run:
```bash
terraform apply
```

### Environment Variables

To add app settings:

1. Edit `main.tf` in the `azurerm_linux_web_app` resource
2. Add to `app_settings` block:
   ```hcl
   app_settings = {
     "MyNewSetting" = "value"
     # ... existing settings
   }
   ```
3. Run `terraform apply`

### Enable Staging Slot

In `terraform.tfvars`:
```hcl
enable_staging_slot = true
```

Then:
```bash
terraform apply
```

This creates a staging slot for testing before production deployment.

## Costs

**Estimated monthly costs** (West Europe):

| Configuration | Cost/Month |
|---------------|------------|
| Development (B1 + Basic DB) | ~€20-30 |
| Production (P1v2 + S1 DB) | ~€150-200 |

**Cost breakdown:**
- App Service B1: ~€13/month
- SQL Basic: ~€5/month
- Application Insights: Pay-as-you-go (usually <€5/month)

## Troubleshooting

### Deployment fails with "Name not available"

Resource names must be globally unique. Change `project_name` in `terraform.tfvars`:
```hcl
project_name = "learningresources-yourname"
```

### SQL connection fails

1. Check firewall rules in Azure Portal
2. Add your IP:
   ```hcl
   dev_ip_address = "your.ip.address.here"
   ```
3. Run `terraform apply`

### GitHub Actions fails

1. Check secrets are created:
   ```bash
   gh secret list --repo {owner}/{repo}
   ```

2. Verify publish profile:
   - Go to Azure Portal
   - App Service > Get publish profile
   - Update `AZURE_WEBAPP_PUBLISH_PROFILE` secret

### App won't start

Check logs:
```bash
az webapp log tail --name {app-name} --resource-group {rg-name}
```

Common issues:
- Connection string format
- Missing environment variables
- Database migrations not run

## Cleanup

To destroy all resources:

```bash
terraform destroy
```

Type `yes` to confirm. This:
- Deletes all Azure resources
- Removes GitHub secrets
- **Data will be permanently lost!**

## Security Best Practices

1. **Never commit `terraform.tfvars`** - It contains secrets
2. **Use Key Vault** for production secrets (not included in basic setup)
3. **Enable managed identity** instead of connection strings
4. **Set up custom domains** with SSL certificates
5. **Configure backup** for SQL Database

## Next Steps

- [ ] Configure custom domain
- [ ] Set up SSL certificate
- [ ] Enable automatic backups
- [ ] Configure alerts in Application Insights
- [ ] Set up Azure Key Vault for secrets
- [ ] Configure CORS properly for production
- [ ] Set up staging/production environments
- [ ] Enable diagnostic logging

## Support

- Terraform docs: https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs
- Azure App Service: https://docs.microsoft.com/en-us/azure/app-service/
- GitHub Actions: https://docs.github.com/en/actions
