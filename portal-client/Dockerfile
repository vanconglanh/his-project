# =====================================================================
# Pro-Diab HIS — Portal benh nhan (Next.js) — multi-stage standalone
# Yeu cau next.config: output: "standalone"
# Build: docker build --build-arg NEXT_PUBLIC_API_BASE_URL=https://his.diab.com.vn \
#          -t prodiab-portal ./portal-client
# =====================================================================
FROM node:22-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json* ./
RUN npm install --no-audit --no-fund

# ---------------------------------------------------------------------
FROM node:22-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ARG NEXT_PUBLIC_API_BASE_URL
ARG NEXT_PUBLIC_DEV_SUBDOMAIN
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL \
    NEXT_PUBLIC_DEV_SUBDOMAIN=$NEXT_PUBLIC_DEV_SUBDOMAIN \
    NEXT_TELEMETRY_DISABLED=1
RUN npm run build

# ---------------------------------------------------------------------
FROM node:22-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=3000 \
    HOSTNAME=0.0.0.0
RUN apk add --no-cache curl
COPY --from=build /app/public ./public
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
EXPOSE 3000
CMD ["node", "server.js"]
