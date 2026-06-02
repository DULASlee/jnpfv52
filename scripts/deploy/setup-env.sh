#!/bin/bash
# =============================================================================
# JNPF V5.2 - Environment Setup Script
# =============================================================================
# Usage: ./scripts/deploy/setup-env.sh [environment]
# =============================================================================

set -e

ENVIRONMENT=${1:-development}
ENV_FILE=".env.${ENVIRONMENT}"
ENV_EXAMPLE=".env.example"

echo "=== JNPF Environment Setup - ${ENVIRONMENT} ==="
echo ""

# Check if .env.example exists
if [ ! -f "$ENV_EXAMPLE" ]; then
    echo "ERROR: .env.example not found"
    exit 1
fi

# Check if environment file already exists
if [ -f "$ENV_FILE" ]; then
    echo "WARNING: $ENV_FILE already exists"
    read -p "Do you want to overwrite it? (yes/no): " CONFIRM
    if [ "$CONFIRM" != "yes" ]; then
        echo "Setup cancelled."
        exit 0
    fi
fi

# Copy template
echo "1. Creating $ENV_FILE from template..."
cp "$ENV_EXAMPLE" "$ENV_FILE"

# Set environment-specific values
echo "2. Configuring environment-specific values..."
sed -i "s/ENVIRONMENT=development/ENVIRONMENT=$ENVIRONMENT/" "$ENV_FILE"

case $ENVIRONMENT in
    development)
        sed -i "s/DB_NAME=jnpf_v52_dev/DB_NAME=jnpf_v52_dev/" "$ENV_FILE"
        sed -i "s/LOG_LEVEL=Information/LOG_LEVEL=Debug/" "$ENV_FILE"
        sed -i "s/SWAGGER_ENABLED=true/SWAGGER_ENABLED=true/" "$ENV_FILE"
        ;;
    staging)
        sed -i "s/DB_NAME=jnpf_v52_dev/DB_NAME=jnpf_v52_staging/" "$ENV_FILE"
        sed -i "s/LOG_LEVEL=Information/LOG_LEVEL=Information/" "$ENV_FILE"
        sed -i "s/SWAGGER_ENABLED=true/SWAGGER_ENABLED=true/" "$ENV_FILE"
        ;;
    production)
        sed -i "s/DB_NAME=jnpf_v52_dev/DB_NAME=jnpf_v52_prod/" "$ENV_FILE"
        sed -i "s/LOG_LEVEL=Information/LOG_LEVEL=Warning/" "$ENV_FILE"
        sed -i "s/SWAGGER_ENABLED=true/SWAGGER_ENABLED=false/" "$ENV_FILE"
        sed -i "s/VITE_DROP_CONSOLE=false/VITE_DROP_CONSOLE=true/" "$ENV_FILE"
        ;;
esac

# Generate random secrets for production
if [ "$ENVIRONMENT" = "production" ]; then
    echo "3. Generating secure random secrets..."
    JWT_SECRET=$(openssl rand -base64 32)
    DB_PASSWORD=$(openssl rand -base64 16)
    REDIS_PASSWORD=$(openssl rand -base64 16)

    sed -i "s/JWT_SECRET_KEY=<CHANGE_ME_TO_32+_CHARS>/JWT_SECRET_KEY=$JWT_SECRET/" "$ENV_FILE"
    sed -i "s/DB_PASSWORD=<CHANGE_ME>/DB_PASSWORD=$DB_PASSWORD/" "$ENV_FILE"
    sed -i "s/REDIS_PASSWORD=/REDIS_PASSWORD=$REDIS_PASSWORD/" "$ENV_FILE"

    echo "   IMPORTANT: Save these passwords securely!"
    echo "   DB_PASSWORD: $DB_PASSWORD"
    echo "   REDIS_PASSWORD: $REDIS_PASSWORD"
    echo "   JWT_SECRET_KEY: (saved to $ENV_FILE)"
fi

# Create necessary directories
echo "4. Creating necessary directories..."
mkdir -p logs
mkdir -p wwwroot
mkdir -p backups

echo ""
echo "=== Setup Complete ==="
echo "Environment file created: $ENV_FILE"
echo ""
echo "Next steps:"
echo "1. Review and edit $ENV_FILE with your actual values"
echo "2. For development: docker compose up -d"
echo "3. For staging: docker compose -f docker-compose.staging.yml up -d"
echo "4. For production: docker compose -f docker-compose.production.yml up -d"
