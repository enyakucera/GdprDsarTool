#!/bin/bash
# =============================================================================
# Fix "Invalid object name 'Companies'" Error
# =============================================================================
# This script fixes the issue where database exists but tables don't

set -e

export KUBECONFIG=/etc/rancher/k3s/k3s.yaml

echo "==================================="
echo "Fixing Migration Issues"
echo "==================================="
echo ""

echo "This will:"
echo "1. Delete the current deployment"
echo "2. Optionally drop the database (if you want fresh start)"
echo "3. Redeploy with fresh migrations"
echo ""

read -p "Do you want to DROP the database and start fresh? (yes/no): " DROP_DB

if [ "$DROP_DB" = "yes" ]; then
    echo ""
    echo "⚠️  WARNING: This will delete ALL data!"
    read -p "Are you SURE? Type 'DELETE' to confirm: " CONFIRM
    
    if [ "$CONFIRM" != "DELETE" ]; then
        echo "Cancelled."
        exit 0
    fi
    
    echo ""
    echo "Getting connection string from secret..."
    CONN_STRING=$(kubectl get secret gdprdsar-secrets -n production -o jsonpath='{.data.connection-string}' | base64 -d)
    
    # Parse connection string to get server, user, password
    SERVER=$(echo "$CONN_STRING" | grep -oP 'Server=\K[^;]+')
    USER=$(echo "$CONN_STRING" | grep -oP 'User Id=\K[^;]+')
    PASSWORD=$(echo "$CONN_STRING" | grep -oP 'Password=\K[^;]+')
    DATABASE=$(echo "$CONN_STRING" | grep -oP 'Database=\K[^;]+')
    
    echo "Dropping database $DATABASE..."
    kubectl run mssql-drop --rm -it \
      --image=mcr.microsoft.com/mssql-tools \
      --restart=Never \
      --namespace=production \
      -- /opt/mssql-tools/bin/sqlcmd \
         -S "$SERVER" \
         -U "$USER" \
         -P "$PASSWORD" \
         -Q "DROP DATABASE IF EXISTS $DATABASE"
    
    echo "Database dropped!"
    echo ""
fi

echo "Deleting current deployment..."
kubectl delete deployment gdprdsar-tool -n production --ignore-not-found=true

echo "Waiting for pods to terminate..."
sleep 5

echo ""
echo "Reapplying deployment..."
kubectl apply -f k8s/

echo ""
echo "Watching deployment..."
kubectl rollout status deployment/gdprdsar-tool -n production --timeout=600s

echo ""
echo "==================================="
echo "Fix completed!"
echo "==================================="
echo ""

echo "Checking migration logs..."
POD_NAME=$(kubectl get pods -n production -l app=gdprdsar-tool -o jsonpath='{.items[0].metadata.name}')
kubectl logs $POD_NAME -c migration -n production

echo ""
echo "Checking application status..."
kubectl get pods -n production -l app=gdprdsar-tool
