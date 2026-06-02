#!/bin/bash
# =============================================================================
# JNPF V5.2 - Deployment Status Script
# =============================================================================
# Usage: ./scripts/deploy/status.sh [environment]
# =============================================================================

set -e

ENVIRONMENT=${1:-staging}
COMPOSE_FILE="docker-compose.${ENVIRONMENT}.yml"

echo "=== JNPF Deployment Status - ${ENVIRONMENT} ==="
echo ""

# Check if compose file exists
if [ ! -f "$COMPOSE_FILE" ]; then
    echo "ERROR: $COMPOSE_FILE not found"
    exit 1
fi

# Service status
echo "1. Service Status:"
echo "---"
docker compose -f "$COMPOSE_FILE" ps --format "table {{.Name}}\t{{.State}}\t{{.Status}}\t{{.Ports}}"
echo ""

# Container resource usage
echo "2. Resource Usage:"
echo "---"
docker stats --no-stream --format "table {{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}" \
    $(docker compose -f "$COMPOSE_FILE" ps -q) 2>/dev/null || echo "No running containers"
echo ""

# Recent logs (last 10 lines per service)
echo "3. Recent Logs (last 10 lines per service):"
echo "---"
for service in $(docker compose -f "$COMPOSE_FILE" config --services); do
    echo "--- $service ---"
    docker compose -f "$COMPOSE_FILE" logs --tail 10 "$service" 2>/dev/null || echo "No logs available"
    echo ""
done

# Disk usage
echo "4. Disk Usage:"
echo "---"
echo "Docker volumes:"
docker system df -v | grep -A 100 "VOLUME NAME" | head -20
echo ""
echo "Host disk:"
df -h / | awk 'NR==1 || NR==2'
echo ""

# Network information
echo "5. Network Information:"
echo "---"
docker network ls | grep jnpf || echo "No JNPF networks found"
echo ""

# Port mappings
echo "6. Port Mappings:"
echo "---"
docker compose -f "$COMPOSE_FILE" ps --format "{{.Name}}: {{.Ports}}" | grep -v "^$"
