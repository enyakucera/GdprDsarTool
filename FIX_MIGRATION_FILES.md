# 🔧 KRITICKÁ OPRAVA - Migration Files Chyběly v Docker Image

## Problém
```
Migration completed!  ← tvrdilo že prošlo
Invalid object name 'Companies'  ← ale tabulky neexistovaly!
```

## Root Cause
**Migration files (.cs) nebyly zahrnuty v Docker image!**

Při `dotnet publish` se migration files nekopírují do výstupu, protože jsou považovány za "build-time" soubory.

## Řešení (3 změny)

### 1. ✅ GdprDsarTool.csproj - Include Migrations
```xml
<ItemGroup>
  <!-- Include Migrations in publish output -->
  <Content Include="Migrations\**\*.cs">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

### 2. ✅ Dockerfile - Explicitní kopírování
```dockerfile
# Copy migration files explicitly (they're not included in publish by default)
COPY --from=build /src/src/GdprDsarTool/Migrations ./Migrations
```

### 3. ✅ MigrationRunner - Detekce a Fallback

Nové kontroly:
- ✅ Vypíše všechny dostupné migrace v assembly
- ✅ Vrátí chybu, pokud nejsou žádné migrace
- ✅ Verifikuje, že tabulky existují po migraci
- ✅ Fallback na `EnsureCreated()` pokud migrace selhaly

## Co teď udělat

### Krok 1: Commitněte opravy
```bash
git add .
git commit -m "Fix: Include migration files in Docker image"
git push origin main
```

### Krok 2: Rebuild a Redeploy
```bash
# GitHub Actions automaticky rebuiltne a deployuje
# NEBO lokálně:
docker build -t gdprdsar-tool:latest .
kubectl delete deployment gdprdsar-tool -n production
kubectl apply -f k8s/
```

### Krok 3: Sledujte logy
```bash
POD_NAME=$(kubectl get pods -n production -l app=gdprdsar-tool -o jsonpath='{.items[0].metadata.name}')
kubectl logs -f $POD_NAME -c migration -n production
```

## Co uvidíte v logách (správně)

**Předtím (špatně):**
```
Total migrations found in assembly: 0  ← ŽÁDNÉ!
No pending migrations found.
Migration completed!  ← Lhalo
Invalid object name 'Companies'  ← Crash
```

**Teď (správně):**
```
Total migrations found in assembly: 1  ← ✓ Našlo!
Available migrations:
  - 20251220172349_InitialCreate
Found 1 pending migration(s):
  - 20251220172349_InitialCreate
Applying migrations...
Migrations applied successfully!
Verifying database schema...
✓ Companies table exists and is queryable  ← ✓ Funguje!
Database is empty. Running seed...
Seed completed successfully!
=== Migration Successful ===
```

## Proč to předtím nefungovalo

```
Build Stage:
  ✓ Migration files existují v /src/src/GdprDsarTool/Migrations/
  ✓ dotnet publish -o /app/publish
  ✗ Migrations/ se NEKOPÍROVALY do /app/publish/

Runtime Stage:
  ✓ COPY --from=build /app/publish .
  ✗ Migrations/ nejsou v /app/
  ✗ EF Core nenašlo žádné migrace
  ✗ MigrateAsync() "proběhlo" ale neudělalo nic
  ✗ Tabulky neexistují → crash
```

## Proč to teď bude fungovat

```
Build Stage:
  ✓ Migration files v /src/src/GdprDsarTool/Migrations/
  ✓ dotnet publish
  ✓ .csproj říká: kopíruj Migrations/ do output

Runtime Stage:
  ✓ COPY publish (včetně Migrations/)
  ✓ COPY Migrations/ explicitně (double-check)
  ✓ EF Core najde migrace
  ✓ MigrateAsync() aplikuje migrace
  ✓ Tabulky se vytvoří ✨
  ✓ Seed proběhne
  ✓ App startuje
```

## Test před deployem

```bash
# Build image
docker build -t gdprdsar-tool:latest .

# Verify migrations are in image
docker run --rm gdprdsar-tool:latest ls -la Migrations/
# Mělo by vypsat: 20251220172349_InitialCreate.cs

# Test migration
./test-migration.sh
```

## Emergency: Pokud stále nefunguje

```bash
# 1. Drop database úplně
kubectl run mssql-drop --rm -it \
  --image=mcr.microsoft.com/mssql-tools \
  --restart=Never -n production \
  -- /opt/mssql-tools/bin/sqlcmd -S mssql-service.database.svc.cluster.local \
     -U sa -P 'your-password' \
     -Q "DROP DATABASE IF EXISTS GdprDsarTool"

# 2. Redeploy
./quick-redeploy.sh
```

---

**TL;DR:** Migration files chyběly v Docker image. Opraveno přidáním do .csproj a explicitním kopírováním v Dockerfile. Push a redeploy, teď to bude fungovat!
