# 🔐 Repository Secrets Configuration Guide

## ⚠️ SECURITY ALERT: Leaked Secrets Detected

**CRITICAL**: This repository has detected leaked secrets in Git history. Follow the remediation steps below.

### 🚨 Immediate Actions Required

1. **Rotate all exposed credentials immediately**
2. **Review Azure Storage Account access logs**
3. **Change database passwords**
4. **Monitor for unauthorized access**

### 🔧 GitLeaks Integration

This project now includes GitLeaks configuration to prevent future secret leaks:

- `.gitleaks.toml` - Configuration file for secret detection
- Pre-commit hooks recommended (see setup below)

## Required GitHub Repository Secrets

To deploy the Footex CI/CD pipeline, configure these secrets in your GitHub repository:

**Settings → Secrets and variables → Actions → New repository secret**

### 🔵 Azure Deployment Secrets

| Secret Name             | Description                       | Example/Format                         |
| ----------------------- | --------------------------------- | -------------------------------------- |
| `AZURE_CLIENT_ID`       | Service principal application ID  | `12345678-1234-1234-1234-123456789abc` |
| `AZURE_TENANT_ID`       | Azure Active Directory tenant ID  | `87654321-4321-4321-4321-cba987654321` |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription identifier     | `abcdef12-3456-7890-abcd-ef1234567890` |
| `AZURE_CLIENT_SECRET`   | Service principal password/secret | `your-service-principal-secret`        |

### 🐳 Container Registry Secrets

| Secret Name                       | Description                  | Example/Format         |
| --------------------------------- | ---------------------------- | ---------------------- |
| `CONTAINER_REGISTRY_LOGIN_SERVER` | Azure Container Registry URL | `footexacr.azurecr.io` |
| `CONTAINER_REGISTRY_USERNAME`     | ACR admin username           | `footexacr`            |
| `CONTAINER_REGISTRY_PASSWORD`     | ACR admin password           | `your-acr-password`    |

### 🔧 External Service Secrets

| Secret Name       | Description                     | Example/Format               |
| ----------------- | ------------------------------- | ---------------------------- |
| `DOCKER_USERNAME` | Docker Hub username             | `your-dockerhub-username`    |
| `DOCKER_TOKEN`    | Docker Hub access token         | `dckr_pat_your-access-token` |
| `SONAR_TOKEN`     | SonarCloud authentication token | `sqp_your-sonar-token`       |

## 🚀 Quick Setup Commands

### Create Azure Service Principal

```bash
# Login to Azure
az login

# Create service principal for CI/CD
az ad sp create-for-rbac \
  --name "footex-cicd" \
  --role contributor \
  --scopes /subscriptions/{your-subscription-id}
```

### Create Azure Container Registry

```bash
# Create resource group
az group create --name footex-rg --location eastus

# Create ACR with admin user enabled
az acr create \
  --resource-group footex-rg \
  --name footexacr \
  --sku Basic \
  --admin-enabled true

# Get ACR credentials
az acr credential show --name footexacr
```

### Create Container Apps Environment

```bash
# Create Container Apps environment
az containerapp env create \
  --name footex-env \
  --resource-group footex-rg \
  --location eastus
```

## ✅ Verification Checklist

- [ ] All 10 repository secrets configured
- [ ] Azure service principal created with contributor role
- [ ] Azure Container Registry created with admin enabled
- [ ] SonarCloud project configured
- [ ] Docker Hub repository created
- [ ] GitHub Advanced Security enabled

## 🔍 Testing Secret Configuration

After configuring secrets, test by:

1. **Push a commit** to trigger the CI pipeline
2. **Check Actions tab** for successful workflow execution
3. **Verify Azure deployment** in Container Apps
4. **Confirm SonarCloud analysis** completes
5. **Validate Docker image** pushes to registry

## 🆘 Troubleshooting

### Common Issues

**Azure Authentication Fails:**

- Verify service principal has correct permissions
- Check subscription ID matches your Azure subscription
- Ensure client secret hasn't expired

**Container Registry Access Denied:**

- Confirm ACR admin user is enabled
- Verify registry URL format (include .azurecr.io)
- Check username/password from `az acr credential show`

**SonarCloud Integration Fails:**

- Verify token has project permissions
- Check project key matches repository name
- Ensure SonarCloud project is properly configured

---

## 🔒 Security Remediation for Leaked Secrets

### Immediate Actions Required

1. **Azure Storage Account Key Rotation:**

   ```bash
   # Rotate Azure Storage Account keys
   az storage account keys renew \
     --account-name footexblobstorage \
     --key primary \
     --resource-group your-resource-group
   ```

2. **Database Password Change:**

   - Change PostgreSQL password immediately
   - Update all application configurations
   - Restart all running instances

3. **Review Access Logs:**
   ```bash
   # Check Azure Storage access logs
   az storage blob list \
     --container-name logs \
     --account-name footexblobstorage
   ```

### GitLeaks Setup for Future Protection

1. **Install GitLeaks:**

   ```bash
   # Windows (using Chocolatey)
   choco install gitleaks

   # Or download from: https://github.com/gitleaks/gitleaks/releases
   ```

2. **Setup Pre-commit Hook:**

   ```bash
   # Create pre-commit hook
   echo '#!/bin/sh
   gitleaks protect --verbose --redact --staged' > .git/hooks/pre-commit
   chmod +x .git/hooks/pre-commit
   ```

3. **Scan Repository:**

   ```bash
   # Scan entire repository
   gitleaks detect --verbose

   # Scan only staged files
   gitleaks protect --staged
   ```

### Environment Variables Best Practices

1. **Use Azure Key Vault** for production secrets
2. **Environment-specific appsettings** files
3. **Never commit secrets** to version control
4. **Use .env files** for local development (add to .gitignore)
5. **Implement secret rotation** policies

### Example .env File Structure

Create a `.env` file in your project root (add to .gitignore):

```env
# Database Configuration
DB_CONNECTION_STRING=Server=localhost;Port=5432;Database=Footex_Api;User Id=postgres;Password=your-local-password;

# Azure Storage
AZURE_STORAGE_CONNECTION_STRING=DefaultEndpointsProtocol=https;AccountName=yourstorageaccount;AccountKey=your-new-key;EndpointSuffix=core.windows.net

# Redis Configuration
REDIS_CONNECTION_STRING=localhost:6379
REDIS_PASSWORD=your-redis-password

# JWT Configuration
JWT_SECRET=your-jwt-secret-key
JWT_ISSUER=https://PixelPitchAI.com
JWT_AUDIENCE=https://PixelPitchAI.com

# Admin User
ADMIN_EMAIL=admin@PixelPitchAI.com
ADMIN_PASSWORD=your-secure-admin-password
```

## 🛡️ Security Monitoring

### Continuous Monitoring Tools

1. **GitLeaks in CI/CD:**

   ```yaml
   - name: Run GitLeaks
     uses: gitleaks/gitleaks-action@v2
     env:
       GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
   ```

2. **Azure Security Center** monitoring
3. **Database audit logging** enabled
4. **Application Insights** security events

### Emergency Response Plan

1. **Detected Secret Leak:**

   - Immediately revoke/rotate the compromised secret
   - Check access logs for unauthorized usage
   - Update all systems using the secret
   - Notify security team

2. **Unauthorized Access:**
   - Change all related credentials
   - Review and audit recent activities
   - Check for data exfiltration
   - Implement additional security measures

---

⚠️ **Security Note**: Never commit these secrets to your repository. Always use GitHub's encrypted secrets feature.
