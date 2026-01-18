#!/bin/bash
# =============================================================================
# Database Migration Script
# =============================================================================
# This script runs database migrations locally or in CI/CD
# Usage: ./migrate.sh [environment]
#        ./migrate.sh Development
#        ./migrate.sh Production

set -e

ENVIRONMENT="${1:-Development}"
PROJECT_DIR="src/GdprDsarTool"

echo "==================================="
echo "Database Migration Runner"
echo "==================================="
echo "Environment: $ENVIRONMENT"
echo "Project: $PROJECT_DIR"
echo ""

cd "$PROJECT_DIR"

export ASPNETCORE_ENVIRONMENT="$ENVIRONMENT"

echo "Running migrations..."
dotnet run --no-build -- --migrate

echo ""
echo "==================================="
echo "Migration completed successfully!"
echo "==================================="
