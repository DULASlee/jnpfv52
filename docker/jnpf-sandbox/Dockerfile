# JNPF Studio 交付预览沙箱镜像
# 用途：PipelineDeliveryCoordinator 注入 studio-preview 后在容器内 npm/vite
# 构建：docker build -t jnpf-sandbox:latest -f docker/jnpf-sandbox/Dockerfile .
FROM node:20-bookworm-slim

WORKDIR /app

RUN apt-get update \
  && apt-get install -y --no-install-recommends curl ca-certificates \
  && rm -rf /var/lib/apt/lists/*

# 预览端口（Vite）+ 兼容 SandboxManager 映射的 8080
EXPOSE 4173 8080

# 保持容器存活，由宿主 docker exec 执行 npm install / vite
CMD ["sleep", "infinity"]
