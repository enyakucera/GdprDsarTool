# GdprDsarTool - Quick Reference

## 🚀 Common Tasks

### Database Migrations

```bash
# Local - Run migrations
./migrate.bat                           # Windows
./migrate.sh Development                # Linux/Mac

# Production - Check migration status
kubectl logs -l app=gdprdsar-tool -c migration -n production

# Manual migration (if needed)
kubectl apply -f k8s/migration-job.yaml
kubectl logs -f job/gdprdsar-migration -n production

# Create new migration
cd src/GdprDsarTool
dotnet ef migrations add MigrationName
```

### Deployment

```bash
# Deploy to production (automatic via GitHub Actions on push to main)
git push origin main

# Manual deployment (on self-hosted runner)
docker build -t gdprdsar-tool:latest .
kubectl apply -f k8s/
kubectl rollout restart deployment/gdprdsar-tool -n production

# Check deployment status
kubectl get pods -n production -l app=gdprdsar-tool
kubectl logs -f deployment/gdprdsar-tool -n production
```

### Local Development

```bash
# Run application
cd src/GdprDsarTool
dotnet run

# Run with specific environment
dotnet run --environment Development

# Run migrations
dotnet run -- --migrate

# Build
dotnet build

# Publish
dotnet publish -c Release
```

### Database Operations

```bash
# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# Rollback to specific migration
dotnet ef database update PreviousMigrationName

# List all migrations
dotnet ef migrations list

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script for migration
dotnet ef migrations script
```

### Kubernetes Operations

```bash
# View all resources
kubectl get all -n production

# Pod logs
kubectl logs -f deployment/gdprdsar-tool -n production

# Init container logs (migrations)
kubectl logs <pod-name> -c migration -n production

# Exec into pod
kubectl exec -it <pod-name> -n production -- /bin/bash

# Restart deployment
kubectl rollout restart deployment/gdprdsar-tool -n production

# Rollback deployment
kubectl rollout undo deployment/gdprdsar-tool -n production

# View secrets (base64 encoded)
kubectl get secret gdprdsar-secrets -n production -o yaml

# Update secret
kubectl create secret generic gdprdsar-secrets \
  --from-literal=connection-string="Server=..." \
  -n production --dry-run=client -o yaml | kubectl apply -f -
```

### Docker Operations

```bash
# Build image
docker build -t gdprdsar-tool:latest .

# Run locally
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Server=..." \
  gdprdsar-tool:latest

# Run migrations in container
docker run --rm \
  -e ConnectionStrings__DefaultConnection="Server=..." \
  gdprdsar-tool:latest dotnet GdprDsarTool.dll --migrate

# View logs
docker logs <container-id>

# Exec into container
docker exec -it <container-id> /bin/bash
```

### Troubleshooting

```bash
# Pod won't start - check init container logs
kubectl logs <pod-name> -c migration -n production

# Pod crashes - check app logs
kubectl logs <pod-name> -n production
kubectl describe pod <pod-name> -n production

# Database connection issues - test connection
kubectl run -it --rm debug \
  --image=mcr.microsoft.com/mssql-tools \
  --restart=Never \
  -- sqlcmd -S <server> -U <user> -P <pass> -Q "SELECT 1"

# Check secrets
kubectl get secret gdprdsar-secrets -n production -o json

# Ingress issues
kubectl describe ingress gdprdsar-tool-ingress -n production
kubectl logs -n ingress-nginx <nginx-pod-name>

# Certificate issues
kubectl describe certificate gdprdsar-tool-tls -n production
kubectl get certificaterequest -n production
```

### Monitoring

```bash
# Watch pods
watch kubectl get pods -n production -l app=gdprdsar-tool

# Resource usage
kubectl top pod -n production -l app=gdprdsar-tool

# Events
kubectl get events -n production --sort-by='.lastTimestamp'

# Application URL
curl -I https://gdprdsar.yourdomain.com
```

## 📝 Important Files

- `Program.cs` - Application entry point
- `MigrationRunner.cs` - Migration logic
- `appsettings.json` - Production config
- `appsettings.Development.json` - Dev config
- `k8s/deployment.yaml` - K8s deployment with init container
- `k8s/migration-job.yaml` - Manual migration job
- `.github/workflows/deploy.yml` - CI/CD pipeline
- `Dockerfile` - Container image
- `migrate.sh / migrate.bat` - Local migration scripts

## ⚠️ Important Notes

- **Migrations DO NOT run on app startup**
- Init containers run migrations before pods start
- Failed migrations prevent pod startup (safe!)
- Always test migrations locally first
- Backup database before major changes
