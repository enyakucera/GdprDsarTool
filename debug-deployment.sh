#!/bin/bash
# =============================================================================
# Deployment Debug Script
# =============================================================================
# This script helps diagnose deployment issues

set -e

export KUBECONFIG=/etc/rancher/k3s/k3s.yaml

echo "==================================="
echo "Kubernetes Deployment Diagnostics"
echo "==================================="
echo ""

echo "1. Checking pods status..."
kubectl get pods -n production -l app=gdprdsar-tool
echo ""

echo "2. Checking recent events..."
kubectl get events -n production --sort-by='.lastTimestamp' | tail -20
echo ""

echo "3. Checking deployment status..."
kubectl describe deployment/gdprdsar-tool -n production
echo ""

echo "4. Getting pod details..."
POD_NAME=$(kubectl get pods -n production -l app=gdprdsar-tool -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || echo "")

if [ -n "$POD_NAME" ]; then
    echo "Found pod: $POD_NAME"
    echo ""
    
    echo "5. Pod description:"
    kubectl describe pod "$POD_NAME" -n production
    echo ""
    
    echo "6. Init container (migration) logs:"
    kubectl logs "$POD_NAME" -c migration -n production || echo "No migration logs available"
    echo ""
    
    echo "7. Application logs:"
    kubectl logs "$POD_NAME" -c web -n production || echo "No application logs available"
    echo ""
else
    echo "No pods found. Checking deployment issues..."
    
    echo "5. All pods in namespace:"
    kubectl get pods -n production
    echo ""
fi

echo "8. Checking secrets..."
kubectl get secret gdprdsar-secrets -n production -o jsonpath='{.data}' | jq || echo "Secret not found or jq not installed"
echo ""

echo "==================================="
echo "Diagnostics complete"
echo "==================================="
