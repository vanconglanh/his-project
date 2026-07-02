/**
 * run-smoke.js — Chạy bộ mô phỏng ở quy mô nhỏ (SIM_PATIENTS=10) mà không cần cài thêm
 * "cross-env" (repo hiện chưa có gói này). Set biến môi trường trực tiếp bằng Node rồi spawn
 * Playwright, nên chạy được cả trên PowerShell/cmd.exe (Windows) lẫn bash (macOS/Linux).
 *
 * Dùng qua: npm run test:sim:smoke
 * Tương đương: SIM_PATIENTS=10 playwright test --config=e2e/sim/playwright.sim.config.ts
 */
const { spawnSync } = require("node:child_process");
const path = require("node:path");

const env = { ...process.env, SIM_PATIENTS: process.env.SIM_PATIENTS || "10" };
const configPath = path.join("e2e", "sim", "playwright.sim.config.ts");

const result = spawnSync("npx", ["playwright", "test", "--config=" + configPath], {
  stdio: "inherit",
  env,
  shell: true,
});

process.exit(result.status ?? 1);
