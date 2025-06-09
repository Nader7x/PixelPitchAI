# 🔐 Repository Secrets Configuration Guide

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

⚠️ **Security Note**: Never commit these secrets to your repository. Always use GitHub's encrypted secrets feature.
