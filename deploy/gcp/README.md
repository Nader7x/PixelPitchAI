# Google Cloud Run Deployment Guide

This folder contains configurations to deploy the Footex monorepo stack on **Google Cloud Run** using Google's **multi-container sidecar** feature.

---

## 1. Architecture Design on Google Cloud

GCP Cloud Run allows you to deploy up to 10 containers together under a **single Cloud Run Service**. They share the same localhost network interface, meaning Caddy can proxy requests internally using `localhost:8080`, `localhost:8000`, etc.

```text
               ┌───────────────────────┐
               │    Google Cloud DNS   │
               └───────────┬───────────┘
                           │ HTTPS
               ┌───────────▼───────────┐
               │   GCP Load Balancer   │ (Managed SSL Ingress)
               └───────────┬───────────┘
                           │ 
               ┌───────────▼───────────┐
               │  Cloud Run Service    │ (Single Serverless Instance)
               │ ┌───────────────────┐ │
               │ │   Caddy Proxy     │ │ (Port 8080 Ingress Container)
               │ └─┬───────┬───────┬─┘ │
               │   │       │       │   │
               │ ┌─▼─┐   ┌─▼─┐   ┌─▼─┐ │
               │ │FE │   │API│   │AI │ │ (Sidecar Containers)
               │ └───┘   └───┘   └───┘ │
               └───────────────────────┘
```

---

## 2. Cloud Run YAML Manifest (`service.yaml`)

Deploy the multi-container configuration in Cloud Run by executing:

```yaml
apiVersion: serving.knative.dev/v1
kind: Service
metadata:
  name: footex-monorepo
  namespace: footex-project
  annotations:
    run.googleapis.com/launch-stage: BETA
spec:
  template:
    spec:
      containers:
        # 1. Caddy Ingress Container (Receives Traffic)
        - name: caddy
          image: gcr.io/footex-project/caddy:latest
          ports:
            - containerPort: 8080
          resources:
            limits:
              cpu: 1000m
              memory: 512Mi
          dependsOn:
            - football-frontend
            - footex-api
            - simulation-engine

        # 2. Next.js Frontend Container (Sidecar)
        - name: football-frontend
          image: gcr.io/footex-project/football-frontend:latest
          resources:
            limits:
              cpu: 1000m
              memory: 512Mi

        # 3. .NET 10 Backend API (Sidecar)
        - name: footex-api
          image: gcr.io/footex-project/footex-api:latest
          resources:
            limits:
              cpu: 1000m
              memory: 1024Mi
          env:
            - name: ConnectionStrings__DefaultConnection
              value: "Server=127.0.0.1;Port=5432;Database=Footex_Api;User Id=postgres;Password=footex_password;"

        # 4. Python AI Simulation Engine (Sidecar)
        - name: simulation-engine
          image: gcr.io/footex-project/simulation-engine:latest
          resources:
            limits:
              cpu: 1000m
              memory: 1024Mi
```

---

## 3. Deploying to Cloud Run
Run using the `gcloud` CLI:
```bash
gcloud run services replace service.yaml --platform managed --region us-central1
```
