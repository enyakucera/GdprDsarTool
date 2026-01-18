# ✅ Pre-Deployment Checklist

Před každým deploymentem projděte tento checklist, aby se předešlo problémům.

## 📋 Základní kontroly

- [ ] **Kód kompiluje** 
  ```bash
  cd src/GdprDsarTool
  dotnet build
  ```

- [ ] **Migrace jsou vytvořené** (pokud jste změnili model)
  ```bash
  dotnet ef migrations list
  ```

- [ ] **Migrace fungují lokálně**
  ```bash
  dotnet run -- --migrate
  # Nebo
  ./test-migration.sh
  ```

- [ ] **Connection string je správný**
  - Zkontrolujte secret v K8s: `kubectl get secret gdprdsar-secrets -n production`
  - Server je dostupný z Kubernetes clusteru

- [ ] **Docker image se buildí**
  ```bash
  docker build -t gdprdsar-tool:latest .
  ```

- [ ] **Testy prošly** (pokud existují)
  ```bash
  dotnet test
  ```

## 🔍 Pokročilé kontroly

- [ ] **Resources v clusteru jsou dostupné**
  ```bash
  kubectl top nodes
  kubectl get pods -n production
  ```

- [ ] **Žádné pending eventy v namespace**
  ```bash
  kubectl get events -n production | grep -i error
  ```

- [ ] **Předchozí deployment je stabilní**
  ```bash
  kubectl get pods -n production -l app=gdprdsar-tool
  # Měl by být ve stavu "Running"
  ```

- [ ] **Secret existuje a obsahuje connection-string**
  ```bash
  kubectl get secret gdprdsar-secrets -n production -o jsonpath='{.data.connection-string}' | base64 -d
  ```

- [ ] **Síťová dostupnost databáze**
  ```bash
  # Z K8s podu otestujte připojení k DB serveru
  kubectl run -it --rm debug --image=busybox --restart=Never -- nc -zv <db-server> 1433
  ```

## 🎯 Pro významné změny

- [ ] **Backup databáze**
  ```sql
  BACKUP DATABASE GdprDsarTool TO DISK = '/backup/GdprDsarTool_backup.bak'
  ```

- [ ] **Rollback plán připraven**
  - Znáte poslední funkční verzi
  - Víte jak vrátit migraci: `dotnet ef database update PreviousMigration`

- [ ] **Monitoring připraven**
  - Máte otevřené logy v druhém terminálu
  ```bash
  kubectl logs -f deployment/gdprdsar-tool -n production
  ```

- [ ] **Notifikovali jste tým** (pokud je to breaking change)

## 📦 Před prvním deploymentem (nový cluster)

- [ ] **Namespace vytvořen**
  ```bash
  kubectl create namespace production
  ```

- [ ] **Secret nastaven**
  ```bash
  kubectl create secret generic gdprdsar-secrets \
    --from-literal=connection-string="<your-conn-string>" \
    -n production
  ```

- [ ] **Ingress controller běží**
  ```bash
  kubectl get pods -n ingress-nginx
  ```

- [ ] **Cert-manager běží** (pro HTTPS)
  ```bash
  kubectl get pods -n cert-manager
  ```

- [ ] **Storage class je dostupný** (pro persistent volumes, pokud používáte)

## 🚀 Ready to Deploy

Pokud je vše zaškrtnuté ✅:

```bash
git add .
git commit -m "Your changes"
git push origin main
```

Sledujte deployment:
```bash
# GitHub Actions
# https://github.com/enyakucera/GdprDsarTool/actions

# Nebo přímo v clusteru
watch kubectl get pods -n production -l app=gdprdsar-tool
```

## ❌ Pokud něco selže

1. **Podívejte se na logy**
   ```bash
   ./debug-deployment.sh
   ```

2. **Přečtěte troubleshooting guide**
   - [DEPLOYMENT_TROUBLESHOOTING.md](DEPLOYMENT_TROUBLESHOOTING.md)

3. **Emergency rollback**
   ```bash
   kubectl rollout undo deployment/gdprdsar-tool -n production
   ```

---

**Tip:** Uložte si tento checklist do bookmarks nebo ho vytiskněte. Ušetří vám čas a nervy! 😊
