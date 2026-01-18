# 🚨 Deployment Timeout - Quick Fix

## Problém
```
error: timed out waiting for the condition
```

## Rychlé řešení (5 minut)

### 1. Zjistěte stav podu
```bash
export KUBECONFIG=/etc/rancher/k3s/k3s.yaml
kubectl get pods -n production -l app=gdprdsar-tool
```

**Možné stavy:**
- `Init:0/1` nebo `Init:Error` → Problém v migration containeru
- `CrashLoopBackOff` → Aplikace padá při startu
- `Pending` → Nemohou se naplánovat resources
- `Running` → Vše OK, timeout byl zbytečný

### 2. Podívejte se na logy

```bash
# Získejte jméno podu
POD_NAME=$(kubectl get pods -n production -l app=gdprdsar-tool -o jsonpath='{.items[0].metadata.name}')

# Migration logy (init container)
kubectl logs $POD_NAME -c migration -n production

# App logy (pokud migration prošla)
kubectl logs $POD_NAME -c web -n production
```

### 3. Běžné problémy a řešení

#### A) Databáze neexistuje nebo není dostupná

**Příznaky:**
```
ERROR: Cannot connect to database
```

**Řešení:**
```bash
# Zkontrolujte connection string v secretu
kubectl get secret gdprdsar-secrets -n production -o json | jq -r '.data["connection-string"]' | base64 -d

# Otestujte připojení z podu
kubectl run -it --rm debug \
  --image=mcr.microsoft.com/mssql-tools \
  --restart=Never \
  --namespace=production \
  -- /opt/mssql-tools/bin/sqlcmd \
     -S <your-server> \
     -U <your-user> \
     -P <your-password> \
     -Q "SELECT 1"
```

**Fix:**
- Zkontrolujte, že SQL Server běží
- Ověřte síťovou dostupnost z Kubernetes
- Zkontrolujte credentials v secretu

#### B) Migration trvá příliš dlouho

**Příznaky:**
- Pod je ve stavu `Init:0/1` déle než 5 minut
- V logách vidíte "Applying migrations..."

**Řešení:**
```bash
# Zvětšete timeout v workflow
# .github/workflows/deploy.yml
kubectl rollout status deployment/gdprdsar-tool -n production --timeout=600s  # 10 min
```

#### C) Starý pod se neukončí

**Příznaky:**
```
Waiting for deployment "gdprdsar-tool" rollout to finish: 1 old replicas are pending termination...
```

**Řešení:**
```bash
# Force delete starého podu
OLD_POD=$(kubectl get pods -n production -l app=gdprdsar-tool -o jsonpath='{.items[0].metadata.name}')
kubectl delete pod $OLD_POD -n production --force --grace-period=0

# Restart deployment
kubectl rollout restart deployment/gdprdsar-tool -n production
```

#### D) Nedostatek resources

**Příznaky:**
```
0/1 nodes are available: insufficient memory
```

**Řešení:**
```bash
# Snižte limity v k8s/deployment.yaml
resources:
  requests:
    memory: "128Mi"  # Původně 256Mi
    cpu: "50m"       # Původně 100m
```

### 4. Úplný debug script

```bash
# Spusťte celý diagnostický script
chmod +x debug-deployment.sh
./debug-deployment.sh > deployment-debug.log 2>&1

# Prohlédněte si výstup
less deployment-debug.log
```

### 5. Emergency: Rollback k předchozí verzi

```bash
# Vrátí se k poslední fungující verzi
kubectl rollout undo deployment/gdprdsar-tool -n production

# Ověření
kubectl rollout status deployment/gdprdsar-tool -n production
```

### 6. Test migrace před deploymentem

**Vždy otestujte migrace lokálně před pushem:**

```bash
# Build image
docker build -t gdprdsar-tool:latest .

# Test migrace v containeru
./test-migration.sh

# Pokud projde ✅ pak push
git push origin main
```

## Pokročilé debugování

### Zkontrolujte deployment descriptor
```bash
kubectl describe deployment/gdprdsar-tool -n production
```

### Zkontrolujte events v namespace
```bash
kubectl get events -n production --sort-by='.lastTimestamp' | tail -30
```

### Exec do podu (pokud běží)
```bash
kubectl exec -it $POD_NAME -n production -c web -- /bin/bash
# V containeru:
env | grep ConnectionStrings
ls -la /app
```

### Zkontrolujte resources
```bash
kubectl top nodes
kubectl top pods -n production
```

## Prevence

✅ **Před každým deploymentem:**

1. Test migrace lokálně: `./test-migration.sh`
2. Build projde: `dotnet build`
3. Connection string je správný
4. Database server je dostupný
5. Dostatek resources v clusteru

## Kontakty na support

Pokud problém přetrvává:

1. Zkopírujte výstup: `./debug-deployment.sh`
2. Přidejte connection string (bez hesla!)
3. Popište, co jste zkoušeli

---

**TL;DR:**
```bash
# 1. Zjisti stav
kubectl get pods -n production -l app=gdprdsar-tool

# 2. Podívej se na logy
POD_NAME=$(kubectl get pods -n production -l app=gdprdsar-tool -o jsonpath='{.items[0].metadata.name}')
kubectl logs $POD_NAME -c migration -n production

# 3. Pokud nenajdeš problém
./debug-deployment.sh

# 4. Emergency rollback
kubectl rollout undo deployment/gdprdsar-tool -n production
```
