# AWS ECS / Fargate Deployment Guide

This folder contains the standardized templates and configurations to deploy the Footex monorepo services together onto **AWS ECS (Elastic Container Service)** with **AWS Fargate** (serverless container hosting).

---

## Architecture Design on AWS

```text
               ┌───────────────────────┐
               │    Route 53 (DNS)     │
               └───────────┬───────────┘
                           │ HTTPS
               ┌───────────▼───────────┐
               │ Application Load Bal  │
               └───────────┬───────────┘
                           │ HTTP
               ┌───────────▼───────────┐
               │  Caddy Ingress Proxy  │ (Fargate Task)
               └─────┬───────────┬─────┘
          api. /     │           │ /
      ┌──────────────┘           └──────────────┐
┌─────▼──────┐                             ┌────▼───────┐
│ C# Web API │ (Fargate Task)              │ Next.js FE │ (Fargate Task)
└─────┬──────┘                             └────────────┘
      │
┌─────▼──────┐
│  Postgres  │ (RDS DB / Fargate Task)
└────────────┘
```

---

## 1. Deploying via AWS Copilot (Recommended)

AWS Copilot is the easiest and most modern CLI to manage production services on ECS. 

### Step 1: Initialize the Application
```bash
copilot app init footex
```

### Step 2: Deploy Infrastructure Backends
Deploy PostgreSQL, Redis, and RabbitMQ using Copilot addons or standard RDS/ElastiCache instances.

### Step 3: Deploy Services
Use the provided manifests to spin up each service:
* **Frontend**: `copilot svc init --name frontend --svc-type "Request-Driven Web Service"`
* **Backend**: `copilot svc init --name backend --svc-type "Backend Service"`
* **Simulation Engine**: `copilot svc init --name simulation --svc-type "Backend Service"`
* **Caddy**: `copilot svc init --name ingress --svc-type "Load Balanced Web Service"`

---

## 2. Standard ECS Task Definition (`task-definition.json`)

If you are using Terraform, CloudFormation, or the AWS Console, you can deploy all services together under a **single ECS Task Definition** (acting as a pod, where they share the same localhost network interface).

```json
{
  "family": "footex-monorepo",
  "networkMode": "awsvpc",
  "containerDefinitions": [
    {
      "name": "caddy",
      "image": "caddy:2.7-alpine",
      "essential": true,
      "portMappings": [
        {
          "containerPort": 80,
          "hostPort": 80
        },
        {
          "containerPort": 443,
          "hostPort": 443
        }
      ],
      "dependsOn": [
        {
          "containerName": "footex-api",
          "condition": "START"
        },
        {
          "containerName": "simulation-engine",
          "condition": "START"
        },
        {
          "containerName": "football-frontend",
          "condition": "START"
        }
      ]
    },
    {
      "name": "football-frontend",
      "image": "<AWS_ACCOUNT_ID>.dkr.ecr.<REGION>.amazonaws.com/football-frontend:latest",
      "essential": true,
      "portMappings": [
        {
          "containerPort": 3000
        }
      ]
    },
    {
      "name": "footex-api",
      "image": "<AWS_ACCOUNT_ID>.dkr.ecr.<REGION>.amazonaws.com/footex-api:latest",
      "essential": true,
      "portMappings": [
        {
          "containerPort": 8080
        }
      ]
    },
    {
      "name": "simulation-engine",
      "image": "<AWS_ACCOUNT_ID>.dkr.ecr.<REGION>.amazonaws.com/simulation-engine:latest",
      "essential": true,
      "portMappings": [
        {
          "containerPort": 8000
        }
      ]
    }
  ],
  "requiresCompatibilities": [
    "FARGATE"
  ],
  "cpu": "1024",
  "memory": "2048"
}
```
