# Azure Container Apps (ACA) Deployment Guide

This folder contains standard templates and strategies to deploy the Footex monorepo stack on **Azure Container Apps**.

---

## 1. Architecture Design on Azure

Azure Container Apps runs on serverless Kubernetes (K8s) internally. All containers can reside inside a **single Azure Container App Environment**, sharing a private virtual network.

```text
               ┌───────────────────────┐
               │    Azure DNS / TLS    │
               └───────────┬───────────┘
                           │ HTTPS
               ┌───────────▼───────────┐
               │  Caddy Ingress Proxy  │ (Ingress Enabled Container App)
               └─────┬───────────┬─────┘
          api. /     │           │ /
      ┌──────────────┘           └──────────────┐
┌─────▼──────┐                             ┌────▼───────┐
│ C# Web API │ (Private Container App)     │ Next.js FE │ (Private Container App)
└─────┬──────┘                             └────────────┘
      │
┌─────▼──────────────────┐
│ Azure Flexible DB (PG) │ (Managed PostgreSQL Database)
└────────────────────────┘
```

---

## 2. Azure Container Apps Bicep Manifest (`main.bicep`)

Deploying using Bicep is the standard way to provision infrastructure on Azure. 

Create a resource deployment with the following definitions:

```bicep
param location string = resourceGroup().location
param environmentName string = 'footex-env'

// 1. Create Azure Container Apps Environment
resource env 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: environmentName
  location: location
  properties: {
    zoneRedundant: false
  }
}

// 2. Create Python AI Simulation Service (Private)
resource simulationEngine 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'simulation-engine'
  location: location
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 8000
        transport: 'http'
      }
    }
    template: {
      containers: [
        {
          name: 'simulation-engine'
          image: 'myregistry.azurecr.io/simulation-engine:latest'
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
        }
      ]
    }
  }
}

// 3. Create .NET Web API Service (Private)
resource footexApi 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'footex-api'
  location: location
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 8080
        transport: 'http'
      }
    }
    template: {
      containers: [
        {
          name: 'footex-api'
          image: 'myregistry.azurecr.io/footex-api:latest'
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
        }
      ]
    }
  }
}

// 4. Create Next.js Frontend (Private)
resource frontend 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'football-frontend'
  location: location
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 3000
        transport: 'http'
      }
    }
    template: {
      containers: [
        {
          name: 'football-frontend'
          image: 'myregistry.azurecr.io/football-frontend:latest'
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
        }
      ]
    }
  }
}

// 5. Create Caddy Reverse Proxy (Public Ingress)
resource caddy 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'caddy-ingress'
  location: location
  properties: {
    managedEnvironmentId: env.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 80
        transport: 'http'
      }
    }
    template: {
      containers: [
        {
          name: 'caddy'
          image: 'myregistry.azurecr.io/caddy:latest'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
    }
  }
}
```

---

## 3. Provisioning the Stack
Run using the Azure CLI:
```bash
az deployment group create --resource-group footex-rg --template-file main.bicep
```
