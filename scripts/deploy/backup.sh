#!/bin/bash
# =============================================================================
# JNPF V5.2 - Backup Script
# =============================================================================
# Usage: ./scripts/deploy/backup.sh [environment]
# =============================================================================

set -e

ENVIRONMENT=${1:-staging}
COMPOSE_FILE="docker-compose.${ENVIRONMENT}.yml"
BACKUP_DIR="backups/$(date +%Y%m%d_%H%M%S)"

echo "=== JNPF Backup - ${ENVIRONMENT} ==="
echo ""

# Create backup directory
echo "1. Creating backup directory: $BACKUP_DIR"
mkdir -p "$BACKUP_DIR"

# Backup database
echo "2. Backing up database..."
docker exec "jnpf-sqlserver-${ENVIRONMENT}" /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -P "${DB_PASSWORD}" \
    -Q "BACKUP DATABASE [${DB_NAME}] TO DISK = '/var/opt/mssql/backup/backup.bak' WITH FORMAT"

docker cp "jnpf-sqlserver-${ENVIRONMENT}:/var/opt/mssql/backup/backup.bak" "$BACKUP_DIR/database.bak"

# Backup Redis data
echo "3. Backing up Redis data..."
docker exec "jnpf-redis-${ENVIRONMENT}" redis-cli -a "${REDIS_PASSWORD}" BGSAVE
sleep 5
docker cp "jnpf-redis-${ENVIRONMENT}:/data/dump.rdb" "$BACKUP_DIR/redis.rdb"

# Backup configuration files
echo "4. Backing up configuration..."
cp ".env.${ENVIRONMENT}" "$BACKUP_DIR/" 2>/dev/null || true
cp "$COMPOSE_FILE" "$BACKUP_DIR/"
cp -r backend/application/JNPF.API.Entry/Configurations "$BACKUP_DIR/configurations" 2>/dev/null || true

# Backup uploaded files
echo "5. Backing up uploaded files..."
if [ -d "wwwroot" ]; then
    tar -czf "$BACKUP_DIR/wwwroot.tar.gz" wwwroot/
fi

# Backup logs (last 7 days)
echo "6. Backing up recent logs..."
if [ -d "logs" ]; then
    find logs -name "*.log" -mtime -7 -exec tar -czf "$BACKUP_DIR/logs.tar.gz" {} +
fi

# Calculate backup size
BACKUP_SIZE=$(du -sh "$BACKUP_DIR" | cut -f1)

echo ""
echo "=== Backup Complete ==="
echo "Backup location: $BACKUP_DIR"
echo "Backup size: $BACKUP_SIZE"
echo ""
echo "Contents:"
ls -lh "$BACKUP_DIR"
