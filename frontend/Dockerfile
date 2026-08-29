# =====================================================================
# Pro-Diab HIS — Frontend (Next.js) — multi-stage standalone build
# Requires next.config: output: "standalone"
# Build: docker build --build-arg NEXT_PUBLIC_API_BASE_URL=https://his.diab.com.vn -t prodiab-frontend ./frontend
# =====================================================================
FROM node:22-alpine AS deps
WORKDIR /app
# HUSKY=0: bo qua script "prepare": "husky" (khong co .git trong build context)
ENV HUSKY=0
# --- Layer ON DINH: chi copy package.json/lock, khong copy source ---
# Chi khi doi dependency (them dong duoi) layer nay moi invalidate;
# sua code .ts/.tsx binh thuong se KHONG chay lai npm install.
COPY package.json package-lock.json ./
# npm install (khong ci) vi package-lock trong repo lech nhe voi package.json
RUN --mount=type=cache,target=/root/.npm \
    npm install --no-audit --no-fund

# ---------------------------------------------------------------------
FROM node:22-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
# NEXT_PUBLIC_* duoc bake luc build → tro ve URL cong khai (nginx route /api)
ARG NEXT_PUBLIC_API_BASE_URL
# Panel dang nhap nhanh theo role - CHI bat khi build local/dev tuong minh truyen
# --build-arg NEXT_PUBLIC_TEST_LOGIN_PANEL=true. Mac dinh KHONG bat (production/
# staging build binh thuong khong truyen arg nay → panel khong bao gio xuat hien).
ARG NEXT_PUBLIC_TEST_LOGIN_PANEL=""
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL \
    NEXT_PUBLIC_TEST_LOGIN_PANEL=$NEXT_PUBLIC_TEST_LOGIN_PANEL \
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
