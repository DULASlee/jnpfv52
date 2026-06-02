#!/bin/bash
# =============================================================================
# JNPF V5.2 - Rollback Script
# =============================================================================
# Usage: ./scripts/deploy/rollback.sh [environment] [backup_dir]
# =============================================================================

set -e

ENVIRONMENT=${1:-staging}
BACKUP_DIR=${2:-""}
COMPOSE_FILE="docker-compose.${ENVIRONMENT}.yml"
BACKUP_BASE="backups"

echo "=== JNPF Rollback - ${ENVIRONMENT} ==="
echo ""

# If no backup directory specified, list available backups
if [ -z "$BACKUP_DIR" ]; then
    echo "Available backups:"
    ls -lt "$BACKUP_BASE" | head -10
    echo ""
    echo "Usage: $0 $ENVIRONMENT <backup_dir>"
    exit 1
fi

BACKUP_PATH="${BACKUP_BASE}/${BACKUP_DIR}"

if [ ! -d "$BACKUP_PATH" ]; then
    echo "ERROR: Backup directory not found: $BACKUP_PATH"
    exit 1
fi

echo "Rolling back to: $BACKUP_PATH"
echo ""

# Confirm rollback
read -p "Are you sure you want to rollback? (yes/no): " CONFIRM
if [ "$CONFIRM" != "yes" ]; then
    echo "Rollback cancelled."
    exit 0
fi

# Stop current services
echo "1. Stopping current services..."
docker compose -f "$COMPOSE_FILE" down

# Restore database if backup exists
if [ -f "$BACKUP_PATH/database.bak" ]; then
    echo "2. Restoring database..."
    docker exec "jnpf-sqlserver-${ENVIRONMENT}" /opt/mssql-tools/bin/sqlcmd \
        -S localhost -U sa -P "${DB_PASSWORD}" \
        -Q "RESTORE DATABASE [${DB_NAME}] FROM DISK = '/var/opt/mssql/backup/rollback.bak' WITH REPLACE"
fi

# Restore configuration
echo "3. Restoring configuration..."
if [ -f "$BACKUP_PATH/.env.${ENVIRONMENT}" ]; then
    cp "$BACKUP_PATH/.env.${ENVIRONMENT}" ".env.${ENVIRONMENT}"
fi

if [ -f "$BACKUP_PATH/docker-compose.${ENVIRONMENT}.yml" ]; then
    cp "$BACKUP_PATH/docker-compose.${ENVIRONMENT}.yml" "$COMPOSE_FILE"
fi

# Start services with previous configuration
echo "4. Starting services..."
docker compose -f "$COMPOSE_FILE" up -d

# Wait for services to be healthy
echo "5. Waiting for services to be healthy..."
sleep 30

# Run health check
echo "6. Running health check..."
./scripts/deploy/health-check.sh "$ENVIRONMENT"

echo ""
echo "=== Rollback Complete ==="
echo "Services have been rolled back to: $BACKUP_DIR"
