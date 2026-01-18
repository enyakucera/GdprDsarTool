#!/bin/bash
# =============================================================================
# Test Migration Script
# =============================================================================
# Test migrations in a Docker container (simulates K8s environment)

set -e

IMAGE_NAME="gdprdsar-tool:latest"

echo "==================================="
echo "Testing Migration in Container"
echo "==================================="
echo ""

# Check if image exists
if ! docker images | grep -q "$IMAGE_NAME"; then
    echo "Image $IMAGE_NAME not found. Building..."
    docker build -t $IMAGE_NAME .
    echo ""
fi

echo "Testing migration with provided connection string..."
echo "Please provide connection string (or press Enter to use default from secrets):"
read -r CONN_STRING

if [ -z "$CONN_STRING" ]; then
    echo "No connection string provided. Using default from environment."
    CONN_STRING="${GDPRDSAR_CONNECTION_STRING:-Server=localhost;Database=GdprDsarTool_Test;User ID=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True}"
fi

echo ""
echo "Running migration in container..."
echo ""

docker run --rm \
    --network=host \
    -e "ConnectionStrings__DefaultConnection=$CONN_STRING" \
    -e "ASPNETCORE_ENVIRONMENT=Production" \
    $IMAGE_NAME \
    dotnet GdprDsarTool.dll --migrate

EXIT_CODE=$?

echo ""
if [ $EXIT_CODE -eq 0 ]; then
    echo "==================================="
    echo "✅ Migration test SUCCESSFUL"
    echo "==================================="
else
    echo "==================================="
    echo "❌ Migration test FAILED"
    echo "==================================="
fi

exit $EXIT_CODE
