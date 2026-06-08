# JNPF v5.2 Environment Variables Guide

## Overview

Production secrets must NOT be stored in JSON config files. Use environment variables instead.

.NET 8 automatically loads environment variables with `__` as the nested key separator.

## Required Variables for Production

| Variable | Description | Example |
|---|---|---|
| `JWTSettings__IssuerSigningKey` | JWT signing key (base64, 32+ bytes) | `openssl rand -base64 32` |
| `ConnectionStrings__ConnectionConfigs__0__Password` | Default DB password | `YourSecurePassword` |
| `EventBus__HostName` | RabbitMQ host (if using RabbitMQ) | `192.168.0.232` |
| `EventBus__UserName` | RabbitMQ username | `jnpf` |
| `EventBus__Password` | RabbitMQ password | `your_rabbitmq_password` |

## Config File Structure

```
Configurations/
├── ConnectionStrings.json          # gitignored — local dev only
├── ConnectionStrings.example.json  # tracked — template
├── JWT.json                        # gitignored — local dev only
├── EventBus.json                   # gitignored — sanitized template
├── EventBus.Development.json       # gitignored — local dev overrides
├── Cache.json                      # tracked — no secrets (placeholder)
└── ...
```

## How Environment Override Works

1. .NET 8 `WebApplicationBuilder` automatically loads environment variables
2. JNPF framework then loads `Configurations/*.json` files
3. Environment variables take precedence over JSON values
4. Use `__` separator for nested keys: `Section__Key` → `Section.Key`

## Local Development Setup

1. Copy `ConnectionStrings.example.json` → `ConnectionStrings.json`
2. Fill in local DB credentials
3. `JWT.json` and `EventBus.json` are gitignored — create them locally with dev values

## Production Deployment

Set environment variables in your deployment platform:

```bash
# Linux / Docker
export JWTSettings__IssuerSigningKey="your_base64_key"
export ConnectionStrings__ConnectionConfigs__0__Password="your_db_password"

# Windows
set JWTSettings__IssuerSigningKey=your_base64_key
set ConnectionStrings__ConnectionConfigs__0__Password=your_db_password

# Docker Compose (in .env file)
JWTSettings__IssuerSigningKey=your_base64_key
ConnectionStrings__ConnectionConfigs__0__Password=your_db_password
```
