# =====================================================================
# Pro-Diab HIS — Frontend (Next.js) — multi-stage standalone build
# Requires next.config: output: "standalone"
# Build: docker build --build-arg NEXT_PUBLIC_API_BASE_URL=https://his.diab.com.vn \
#          --build-arg NEXT_PUBLIC_BASE_DOMAIN=diab.com.vn -t prodiab-frontend ./frontend
# =====================================================================
FROM node:22-alpine AS deps
WORKDIR /app
# HUSKY=0: bo qua script "prepare": "husky" (khong co .git trong build context)
ENV HUSKY=0
COPY package.json package-lock.json ./
# npm ci: cai dat dung khoa theo package-lock.json → build tai lap duoc (reproducible)
RUN npm ci --no-audit --no-fund

# ---------------------------------------------------------------------
FROM node:22-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
# NEXT_PUBLIC_* duoc bake luc build → tro ve URL cong khai (nginx route /api)
ARG NEXT_PUBLIC_API_BASE_URL
# NEXT_PUBLIC_BASE_DOMAIN: domain goc de ghep subdomain phong kham (vd "diab.com.vn"
# → hienthi "<subdomain>.diab.com.vn" o man quan ly tenant). Neu khong truyen, code
# fallback "prodiab.vn" (xem frontend/lib/utils/constants.ts) — CHI dung cho dev.
ARG NEXT_PUBLIC_BASE_DOMAIN
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL \
    NEXT_PUBLIC_BASE_DOMAIN=$NEXT_PUBLIC_BASE_DOMAIN \
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
