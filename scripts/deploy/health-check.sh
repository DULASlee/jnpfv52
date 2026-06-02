#!/bin/bash
# =============================================================================
# JNPF V5.2 - Health Check Script
# =============================================================================
# Usage: ./scripts/deploy/health-check.sh [environment]
# =============================================================================

set -e

ENVIRONMENT=${1:-staging}
COMPOSE_FILE="docker-compose.${ENVIRONMENT}.yml"

echo "=== JNPF Health Check - ${ENVIRONMENT} ==="
echo ""

# Check if services are running
echo "1. Checking service status..."
docker compose -f "$COMPOSE_FILE" ps --format json | jq -r '.[] | "\(.Name): \(.State)"'

# Check API health endpoint
echo ""
echo "2. Checking API health..."
API_HEALTH=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/health || echo "000")
if [ "$API_HEALTH" = "200" ]; then
    echo "   API Health: OK (HTTP 200)"
else
    echo "   API Health: FAIL (HTTP $API_HEALTH)"
    exit 1
fi

# Check database connectivity
echo ""
echo "3. Checking database connectivity..."
DB_STATUS=$(docker exec "jnpf-sqlserver-${ENVIRONMENT}" /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -P "${DB_PASSWORD}" -Q "SELECT 1" -h -1 2>/dev/null | tr -d '[:space:]')
if [ "$DB_STATUS" = "1" ]; then
    echo "   Database: OK"
else
    echo "   Database: FAIL"
    exit 1
fi

# Check Redis connectivity
echo ""
echo "4. Checking Redis connectivity..."
REDIS_STATUS=$(docker exec "jnpf-redis-${ENVIRONMENT}" redis-cli -a "${REDIS_PASSWORD}" ping 2>/dev/null)
if [ "$REDIS_STATUS" = "PONG" ]; then
    echo "   Redis: OK"
else
    echo "   Redis: FAIL"
    exit 1
fi

# Check disk space
echo ""
echo "5. Checking disk space..."
DISK_USAGE=$(df -h / | awk 'NR==2 {print $5}' | sed 's/%//')
if [ "$DISK_USAGE" -lt 80 ]; then
    echo "   Disk usage: ${DISK_USAGE}% (OK)"
elif [ "$DISK_USAGE" -lt 90 ]; then
    echo "   Disk usage: ${DISK_USAGE}% (WARNING)"
else
    echo "   Disk usage: ${DISK_USAGE}% (CRITICAL)"
    exit 1
fi

# Check memory usage
echo ""
echo "6. Checking memory usage..."
MEMORY_USAGE=$(free | awk '/Mem:/ {printf "%.0f", $3/$2 * 100}')
if [ "$MEMORY_USAGE" -lt 80 ]; then
    echo "   Memory usage: ${MEMORY_USAGE}% (OK)"
elif [ "$MEMORY_USAGE" -lt 90 ]; then
    echo "   Memory usage: ${MEMORY_USAGE}% (WARNING)"
else
    echo "   Memory usage: ${MEMORY_USAGE}% (CRITICAL)"
    exit 1
fi

echo ""
echo "=== Health Check Complete ==="
echo "All services are healthy."
