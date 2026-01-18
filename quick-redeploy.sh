#!/bin/bash
# =============================================================================
# Quick Redeploy - Forces migration re-run
# =============================================================================

set -e

export KUBECONFIG=/etc/rancher/k3s/k3s.yaml

echo "==================================="
echo "Quick Redeploy with Fresh Migration"
echo "==================================="
echo ""

echo "1. Deleting current deployment..."
kubectl delete deployment gdprdsar-tool -n production --ignore-not-found=true

echo "2. Waiting for cleanup..."
sleep 3

echo "3. Reapplying deployment..."
kubectl apply -f k8s/

echo "4. Watching rollout (timeout 10 minutes)..."
kubectl rollout status deployment/gdprdsar-tool -n production --timeout=600s

echo ""
echo "==================================="
echo "Deployment Complete!"
echo "==================================="

POD_NAME=$(kubectl get pods -n production -l app=gdprdsar-tool -o jsonpath='{.items[0].metadata.name}')

echo ""
echo "Migration logs:"
kubectl logs $POD_NAME -c migration -n production || echo "Migration container finished"

echo ""
echo "Application logs:"
kubectl logs $POD_NAME -c web -n production --tail=20 || echo "App not started yet"

echo ""
echo "Pod status:"
kubectl get pods -n production -l app=gdprdsar-tool
