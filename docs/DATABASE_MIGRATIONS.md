# Database Migrations

This document describes how database migrations are handled in the GdprDsarTool application.

## Overview

Database migrations are **NOT** run automatically on application startup. Instead, they must be explicitly executed during deployment or manually when needed.

## Migration Approaches

### 1. Automatic (Production) - Init Container

During Kubernetes deployment, migrations run automatically via an init container before the application starts.

**How it works:**
- Init container runs: `dotnet GdprDsarTool.dll --migrate`
- Migrations complete before main app starts
- If migrations fail, pods won't start

**Configuration:** See `k8s/deployment.yaml` - `initContainers` section

### 2. Manual - Kubernetes Job

Run migrations manually using a Kubernetes Job:

```bash
# Apply the migration job
kubectl apply -f k8s/migration-job.yaml

# Watch job progress
kubectl logs -f job/gdprdsar-migration -n production

# Check job status
kubectl get jobs -n production

# Delete job after completion
kubectl delete job gdprdsar-migration -n production
```

### 3. Local Development

#### Option A: Using migration scripts

**Windows:**
```cmd
migrate.bat Development
```

**Linux/Mac:**
```bash
chmod +x migrate.sh
./migrate.sh Development
```

#### Option B: Using dotnet run

```bash
cd src/GdprDsarTool
dotnet run -- --migrate
```

#### Option C: Using EF Core CLI

```bash
cd src/GdprDsarTool

# Apply migrations
dotnet ef database update

# Create new migration
dotnet ef migrations add MigrationName

# List migrations
dotnet ef migrations list

# Remove last migration
dotnet ef migrations remove
```

## Migration Runner Features

The custom `MigrationRunner` (`src/GdprDsarTool/MigrationRunner.cs`) provides:

- ✅ Connection testing before migration
- ✅ Retry logic for transient failures
- ✅ Detailed logging of migration steps
- ✅ Automatic database seeding (if empty)
- ✅ Masked connection strings in logs
- ✅ Exit codes for CI/CD integration

## Creating New Migrations

1. Make changes to your Entity models or `DbContext`
2. Create a migration:
   ```bash
   cd src/GdprDsarTool
   dotnet ef migrations add YourMigrationName
   ```
3. Review generated migration files in `Migrations/` folder
4. Test locally:
   ```bash
   dotnet run -- --migrate
   ```
5. Commit and push - deployment will apply automatically

## Deployment Flow

```
┌─────────────────┐
│  Git Push       │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  GitHub Action  │
│  - Build Image  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  K8s Deployment │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Init Container │
│  --migrate      │ ◄── Migrations run here
└────────┬────────┘
         │
      Success?
         │
         ├─ YES ──▶ App starts
         │
         └─ NO ───▶ Pod fails, rollback

```

## Troubleshooting

### Deployment Timeout (Most Common Issue)

**Symptoms:** GitHub Actions shows "timed out waiting for the condition"

**Solution Steps:**

1. **Run debug script on server:**
   ```bash
   chmod +x debug-deployment.sh
   ./debug-deployment.sh
   ```

2. **Check pod status manually:**
   ```bash
   export KUBECONFIG=/etc/rancher/k3s/k3s.yaml
   kubectl get pods -n production -l app=gdprdsar-tool
   ```

3. **Check init container (migration) logs:**
   ```bash
   POD_NAME=$(kubectl get pods -n production -l app=gdprdsar-tool -o jsonpath='{.items[0].metadata.name}')
   kubectl logs $POD_NAME -c migration -n production
   ```

4. **Check application logs:**
   ```bash
   kubectl logs $POD_NAME -c web -n production
   ```

5. **Check events:**
   ```bash
   kubectl get events -n production --sort-by='.lastTimestamp' | tail -20
   ```

**Common Causes:**
- ❌ Database connection string incorrect
- ❌ Database server not accessible from K8s
- ❌ Migration syntax errors
- ❌ Insufficient resources (memory/CPU)
- ❌ Network timeout

### Test Before Deploy

**Always test migrations locally first:**

```bash
# Test in Docker container (simulates K8s)
./test-migration.sh    # Linux/Mac
test-migration.bat     # Windows

# Or test directly
cd src/GdprDsarTool
dotnet run -- --migrate
```

### Init container fails
```bash
# Check init container logs
kubectl logs <pod-name> -c migration -n production

# Describe pod for events
kubectl describe pod <pod-name> -n production

# Check if database is accessible
kubectl run -it --rm debug \
  --image=mcr.microsoft.com/mssql-tools \
  --restart=Never \
  -- /opt/mssql-tools/bin/sqlcmd -S <server> -U <user> -P <pass> -Q "SELECT 1"
```

### Migration job fails
```bash
# Check job logs
kubectl logs job/gdprdsar-migration -n production

# Get job details
kubectl describe job gdprdsar-migration -n production
```

### Local migration fails
```bash
# Verify connection string
cat src/GdprDsarTool/appsettings.Development.json

# Test with verbose logging
cd src/GdprDsarTool
export ASPNETCORE_ENVIRONMENT=Development
dotnet run -- --migrate
```

### Database doesn't exist yet

This is OK! The migration runner will create it:
```
Database doesn't exist yet. Will be created during migration.
Checking for pending migrations...
```

### Connection timeout

Increase timeout in deployment or check network:
```yaml
# k8s/deployment.yaml
initContainers:
  - name: migration
    # Add timeout
    env:
    - name: CommandTimeout
      value: "300"
```

## Best Practices

✅ **DO:**
- Run migrations in init containers for production
- Test migrations locally before committing
- Use meaningful migration names
- Review generated migration code
- Keep migrations small and focused
- Backup database before major migrations

❌ **DON'T:**
- Run migrations on app startup in production
- Edit migration files after they're applied
- Delete old migrations that have been deployed
- Skip testing migrations locally

## Connection Strings

### Development
```
Server=localhost;Database=GdprDsarTool_Dev;User ID=sa;Password=***;TrustServerCertificate=True
```

### Production
Stored in Kubernetes secret: `gdprdsar-secrets` → `connection-string`

## Emergency Rollback

If a migration breaks production:

1. **Quick fix** - Revert to previous deployment:
   ```bash
   kubectl rollout undo deployment/gdprdsar-tool -n production
   ```

2. **Full rollback** - Revert migration in database:
   ```bash
   # Run migration job with specific target
   kubectl run migration-rollback --rm -it \
     --image=gdprdsar-tool:latest \
     --env="ConnectionStrings__DefaultConnection=$CONN_STRING" \
     --restart=Never \
     -- dotnet ef database update PreviousMigrationName
   ```

## Monitoring

Check migration status in logs:
```bash
# Application logs
kubectl logs -l app=gdprdsar-tool -n production

# Init container logs
kubectl logs deployment/gdprdsar-tool -c migration -n production --previous
```
