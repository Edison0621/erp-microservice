# Deployment Guide

This guide covers deploying the ERP System to Kubernetes using Helm.

## Prerequisites

- [Docker](https://www.docker.com/) installed
- [Kubernetes](https://kubernetes.io/) cluster (minikube, AKS, EKS, GKE, etc.)
- [Helm 3](https://helm.sh/) installed
- [kubectl](https://kubernetes.io/docs/tasks/tools/) configured

## Build Docker Images

```bash
# Build all service images
docker build -t erp-system/finance:latest --build-arg SERVICE_NAME=Finance .
docker build -t erp-system/inventory:latest --build-arg SERVICE_NAME=Inventory .
docker build -t erp-system/sales:latest --build-arg SERVICE_NAME=Sales .
docker build -t erp-system/procurement:latest --build-arg SERVICE_NAME=Procurement .
docker build -t erp-system/production:latest --build-arg SERVICE_NAME=Production .
docker build -t erp-system/identity:latest --build-arg SERVICE_NAME=Identity .
docker build -t erp-system/reporting:latest --build-arg SERVICE_NAME=Reporting .
docker build -t erp-system/gateway:latest -f src/Gateways/ErpSystem.Gateway/Dockerfile .
```

## Deploy with Helm

### Install the chart

```bash
# Add dependencies
helm dependency update deploy/helm/erp-system

# Install with default values
helm install erp-system deploy/helm/erp-system

# Install with custom values
helm install erp-system deploy/helm/erp-system \
  --set postgresql.auth.password=mysecretpassword \
  --set ingress.host=erp.mycompany.com

# Upgrade existing deployment
helm upgrade erp-system deploy/helm/erp-system
```

### Validate the deployment

```bash
# Check pods
kubectl get pods -n erp-system

# Check services
kubectl get svc -n erp-system

# Check ingress
kubectl get ingress -n erp-system
```

## Deploy with Raw Kubernetes Manifests

If you prefer not to use Helm:

```bash
# Apply namespace
kubectl apply -f deploy/k8s/namespace.yaml

# Apply configmap and secrets
kubectl apply -f deploy/k8s/configmap.yaml
kubectl apply -f deploy/k8s/secrets.yaml

# Apply services
kubectl apply -f deploy/k8s/services/

# Apply ingress
kubectl apply -f deploy/k8s/ingress.yaml
```

## Configuration

### Key Configuration Values (values.yaml)

| Parameter | Description | Default |
|-----------|-------------|---------|
| `global.namespace` | Kubernetes namespace | `erp-system` |
| `services.*.replicaCount` | Number of replicas per service | `2` |
| `ingress.enabled` | Enable ingress | `true` |
| `ingress.host` | Ingress hostname | `erp.example.com` |
| `postgresql.enabled` | Deploy PostgreSQL | `true` |
| `redis.enabled` | Deploy Redis | `true` |

### Environment-specific Overrides

Create environment-specific value files:

```bash
# Production
helm install erp-system deploy/helm/erp-system -f values-prod.yaml

# Staging
helm install erp-system deploy/helm/erp-system -f values-staging.yaml
```

## Health Checks

All services expose health endpoints:
- `/health` - Liveness probe
- `/health/ready` - Readiness probe

## Monitoring

Services are configured with resource limits and can be monitored using:
- Prometheus metrics
- Grafana dashboards
- Kubernetes native monitoring
