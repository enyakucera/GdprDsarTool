#!/bin/bash
# =============================================================================
# Verify Migration Files in Docker Image
# =============================================================================

set -e

IMAGE_NAME="gdprdsar-tool:latest"

echo "==================================="
echo "Verifying Migration Files in Image"
echo "==================================="
echo ""

if ! docker images | grep -q "$IMAGE_NAME"; then
    echo "ERROR: Image $IMAGE_NAME not found!"
    echo "Run: docker build -t $IMAGE_NAME ."
    exit 1
fi

echo "Checking for Migrations folder..."
docker run --rm $IMAGE_NAME ls -la Migrations/ 2>/dev/null || {
    echo "❌ ERROR: Migrations folder NOT found in image!"
    echo ""
    echo "This is the problem! Migration files are missing."
    echo "Solution: Rebuild image after fixing Dockerfile and .csproj"
    exit 1
}

echo ""
echo "Checking for migration .cs files..."
docker run --rm $IMAGE_NAME ls -la Migrations/*.cs 2>/dev/null || {
    echo "❌ ERROR: No .cs files in Migrations folder!"
    exit 1
}

echo ""
echo "✅ SUCCESS: Migration files are present in the image!"
echo ""
echo "Files found:"
docker run --rm $IMAGE_NAME find Migrations/ -name "*.cs"

echo ""
echo "==================================="
echo "Image is ready for deployment"
echo "==================================="
