# JNPF V5.2 Multi-Environment Deployment Guide

## Overview

This document describes the multi-environment deployment architecture for JNPF V5.2, including development, staging, and production environments.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Environment Differences](#environment-differences)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Deployment Procedures](#deployment-procedures)
- [CI/CD Pipeline](#cicd-pipeline)
- [Monitoring & Health Checks](#monitoring--health-checks)
- [Backup & Recovery](#backup--recovery)
- [Troubleshooting](#troubleshooting)

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      Load Balancer                          │
└─────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
        ▼                     ▼                     ▼
┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│   PC Admin    │    │  Data Screen  │    │   Mobile API  │
│   (Vue 3)     │    │   (DataV)     │    │   (Backend)   │
└───────────────┘    └───────────────┘    └───────────────┘
        │                     │                     │
        └─────────────────────┼─────────────────────┘
                              │
                              ▼
                    ┌───────────────────┐
                    │    API Gateway    │
                    │   (Nginx/Envoy)  │
                    └───────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        │                     │                     │
        ▼                     ▼                     ▼
┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│  Backend API  │    │     Redis     │    │  SQL Server   │
│   (.NET 8)    │    │    Cache      │    │   Database    │
└───────────────┘    └───────────────┘    └───────────────┘
```

## Environment Differences

| Feature | Development | Staging | Production |
|---------|-------------|---------|------------|
| **Database** | Local SQL Server | Containerized | Dedicated Server |
| **Redis** | Local | Containerized | Dedicated/Managed |
| **Swagger** | Enabled | Enabled | Disabled |
| **Log Level** | Debug | Information | Warning |
| **Console Drop** | false | false | true |
| **HTTPS** | Optional | Required | Required |
| **Resource Limits** | None | Moderate | Strict |
| **Backup** | Manual | Daily | Real-time |

## Prerequisites

### Development
- Docker Desktop 4.0+
- .NET 8.0 SDK
- Node.js 18+
- pnpm 8+

### Staging/Production
- Linux server (Ubuntu 22.04 LTS recommended)
- Docker Engine 24.0+
- Docker Compose v2
- Minimum 4GB RAM, 2 CPU cores
- 50GB+ storage

## Quick Start

### 1. Clone and Setup

```bash
git clone https://github.com/your-org/jnpf-v52.git
cd jnpf-v52

# Setup environment
./scripts/deploy/setup-env.sh development
```

### 2. Start Development Environment

```bash
# Using docker compose
docker compose up -d

# Or run services separately
cd backend && dotnet run --project application/JNPF.API.Entry/JNPF.API.Entry.csproj
cd jnpf-web-vue3 && pnpm dev
```

### 3. Access Services

- **PC Admin**: http://localhost:3100
- **API Swagger**: http://localhost:5000/swagger
- **Data Screen**: http://localhost:3102/DataV/

## Configuration

### Environment Variables

All configuration is managed through environment variables. See `.env.example` for the complete reference.

#### Key Configuration Groups

1. **Database**: Connection strings, credentials
2. **Redis**: Cache configuration
3. **JWT**: Authentication secrets
4. **API**: Server settings, CORS
5. **Frontend**: Build-time variables
6. **Logging**: Log levels, file settings

### Configuration Files

```
backend/application/JNPF.API.Entry/Configurations/
├── App.json              # Application settings
├── ConnectionStrings.json # Database connections
├── JWT.json              # JWT configuration
├── Cache.json            # Redis configuration
├── Cors.json             # CORS settings
├── Swagger.json          # Swagger configuration
└── Logging.json          # Serilog configuration
```

## Deployment Procedures

### Development Deployment

```bash
# Setup
./scripts/deploy/setup-env.sh development

# Start
docker compose up -d

# Check status
./scripts/deploy/status.sh development

# View logs
docker compose logs -f api
```

### Staging Deployment

```bash
# Setup
./scripts/deploy/setup-env.sh staging

# Build and deploy
docker compose -f docker-compose.staging.yml up -d

# Verify
./scripts/deploy/health-check.sh staging
```

### Production Deployment

```bash
# Setup (generates secure secrets)
./scripts/deploy/setup-env.sh production

# Review configuration
cat .env.production

# Deploy
docker compose -f docker-compose.production.yml up -d

# Verify
./scripts/deploy/health-check.sh production

# Create backup
./scripts/deploy/backup.sh production
```

## CI/CD Pipeline

### GitHub Actions Workflows

1. **CI (ci.yml)**
   - Triggers: Push/PR to main/develop
   - Actions: Build, test, validate

2. **Staging Deployment (cd-staging.yml)**
   - Triggers: Push to develop, manual
   - Actions: Build images, deploy to staging

3. **Production Deployment (cd-production.yml)**
   - Triggers: Release, manual
   - Actions: Build images, backup, deploy, verify

### Required Secrets

Configure in GitHub Settings > Secrets:

```
STAGING_HOST         # Staging server IP
STAGING_USER         # SSH username
STAGING_SSH_KEY      # SSH private key
PRODUCTION_HOST      # Production server IP
PRODUCTION_USER      # SSH username
PRODUCTION_SSH_KEY   # SSH private key
DB_PASSWORD          # Database password
REDIS_PASSWORD       # Redis password
JWT_SECRET_KEY       # JWT signing key
```

## Monitoring & Health Checks

### Health Check Endpoints

- **API**: `GET /health` (returns 200 if healthy)
- **Database**: SQL Server connection test
- **Redis**: PING command

### Manual Health Check

```bash
./scripts/deploy/health-check.sh [environment]
```

### Service Status

```bash
./scripts/deploy/status.sh [environment]
```

### Log Monitoring

```bash
# View all logs
docker compose -f docker-compose.staging.yml logs -f

# View specific service
docker compose -f docker-compose.staging.yml logs -f api

# View last 100 lines
docker compose -f docker-compose.staging.yml logs --tail 100 api
```

## Backup & Recovery

### Creating Backups

```bash
# Manual backup
./scripts/deploy/backup.sh [environment]

# Automated backups (add to crontab)
0 2 * * * /opt/jnpf/scripts/deploy/backup.sh production
```

### Backup Contents

- Database dump (SQL Server .bak)
- Redis dump (dump.rdb)
- Configuration files
- Uploaded files (wwwroot)
- Recent logs

### Recovery Procedure

```bash
# List available backups
ls -lt backups/

# Restore from backup
./scripts/deploy/rollback.sh [environment] [backup_dir]
```

## Troubleshooting

### Common Issues

#### 1. Database Connection Failed

```bash
# Check SQL Server status
docker exec jnpf-sqlserver-staging /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -P "password" -Q "SELECT 1"

# Check logs
docker compose -f docker-compose.staging.yml logs sqlserver
```

#### 2. Redis Connection Failed

```bash
# Check Redis status
docker exec jnpf-redis-staging redis-cli ping

# Check logs
docker compose -f docker-compose.staging.yml logs redis
```

#### 3. API Not Responding

```bash
# Check API health
curl -v http://localhost:5000/health

# Check API logs
docker compose -f docker-compose.staging.yml logs api

# Restart API
docker compose -f docker-compose.staging.yml restart api
```

#### 4. Frontend Build Failed

```bash
# Check build logs
docker compose -f docker-compose.staging.yml logs web

# Rebuild image
docker compose -f docker-compose.staging.yml build --no-cache web
```

### Rollback Procedure

If deployment fails:

1. **Immediate Rollback**
   ```bash
   ./scripts/deploy/rollback.sh [environment] [last_working_backup]
   ```

2. **Manual Rollback**
   ```bash
   # Stop services
   docker compose -f docker-compose.staging.yml down

   # Restore database
   docker exec jnpf-sqlserver-staging /opt/mssql-tools/bin/sqlcmd \
       -S localhost -U sa -P "password" \
       -Q "RESTORE DATABASE [jnpf_v52_staging] FROM DISK = '/var/opt/mssql/backup/backup.bak' WITH REPLACE"

   # Restore configuration
   cp backups/[backup_dir]/.env.staging .env.staging

   # Start services
   docker compose -f docker-compose.staging.yml up -d
   ```

## Security Considerations

### Production Security Checklist

- [ ] Change all default passwords
- [ ] Disable Swagger UI
- [ ] Enable HTTPS
- [ ] Configure firewall rules
- [ ] Set up log monitoring
- [ ] Enable database encryption
- [ ] Configure backup retention
- [ ] Set resource limits
- [ ] Enable security headers
- [ ] Regular security updates

### Network Security

- Internal services communicate via isolated network
- External access only through Nginx reverse proxy
- Database and Redis not exposed to public internet

## Performance Tuning

### Resource Allocation

| Service | Development | Staging | Production |
|---------|-------------|---------|------------|
| API | 512MB | 1GB | 2GB |
| Database | 1GB | 2GB | 4GB |
| Redis | 128MB | 256MB | 512MB |
| Frontend | 128MB | 128MB | 256MB |

### Scaling

For high-traffic scenarios:

1. **Horizontal Scaling**: Add more API instances behind load balancer
2. **Database Scaling**: Read replicas, connection pooling
3. **Cache Scaling**: Redis Cluster
4. **CDN**: Static asset delivery

## Support

For deployment issues:

1. Check this documentation
2. Review logs: `docker compose logs`
3. Run health checks: `./scripts/deploy/health-check.sh`
4. Contact DevOps team
