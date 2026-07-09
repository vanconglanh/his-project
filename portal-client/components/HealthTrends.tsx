"use client";

import { useHealthTrends } from "@/lib/hooks";
import type { HealthTrendMetric } from "@/lib/types";

/** Màu theo cờ bất thường của kết quả XN */
function flagColor(flag: string | null): { text: string; badge: string; line: string } {
  const f = (flag ?? "").toUpperCase();
  if (["H", "HH", "HIGH", "CRITICAL"].includes(f))
    return { text: "text-red-600", badge: "bg-red-100 text-red-700", line: "#dc2626" };
  if (["L", "LL", "LOW"].includes(f))
    return { text: "text-amber-600", badge: "bg-amber-100 text-amber-700", line: "#d97706" };
  return { text: "text-[#01645A]", badge: "bg-teal-100 text-teal-800", line: "#01645A" };
}

/** Biểu đồ đường nhỏ (sparkline) vẽ bằng SVG — không cần thư viện */
function Sparkline({ values, color }: { values: number[]; color: string }) {
  const W = 120, H = 40, P = 4;
  if (values.length < 2) {
    return <div className="h-10 text-sm text-slate-400">Chưa đủ dữ liệu để vẽ xu hướng</div>;
  }
  const min = Math.min(...values), max = Math.max(...values);
  const span = max - min || 1;
  const step = (W - P * 2) / (values.length - 1);
  const pts = values.map((v, i) => {
    const x = P + i * step;
    const y = P + (H - P * 2) * (1 - (v - min) / span);
    return [x, y] as const;
  });
  const d = pts.map(([x, y], i) => `${i === 0 ? "M" : "L"}${x.toFixed(1)},${y.toFixed(1)}`).join(" ");
  const [lx, ly] = pts[pts.length - 1];
  return (
    <svg viewBox={`0 0 ${W} ${H}`} className="h-10 w-full" preserveAspectRatio="none" aria-hidden="true">
      <path d={d} fill="none" stroke={color} strokeWidth="2.5" strokeLinejoin="round" strokeLinecap="round" />
      <circle cx={lx} cy={ly} r="3.5" fill={color} />
    </svg>
  );
}

function MetricCard({ m }: { m: HealthTrendMetric }) {
  const c = flagColor(m.latestFlag);
  return (
    <div className="w-44 shrink-0 rounded-2xl border-2 border-slate-100 bg-white p-4 shadow-sm">
      <p className="truncate text-sm font-medium text-slate-500" title={m.testName}>
        {m.testName}
      </p>
      <div className="mt-1 flex items-baseline gap-1">
        <span className={`text-2xl font-bold ${c.text}`}>{m.latestValue}</span>
        {m.unit && <span className="text-sm text-slate-500">{m.unit}</span>}
      </div>
      {m.latestFlag && m.latestFlag.toUpperCase() !== "NORMAL" && (
        <span className={`mt-1 inline-block rounded-full px-2 py-0.5 text-xs font-semibold ${c.badge}`}>
          {m.latestFlag}
        </span>
      )}
      <div className="mt-2">
        <Sparkline values={m.series.map((p) => p.value)} color={c.line} />
      </div>
    </div>
  );
}

/** Dải "Chỉ số sức khoẻ" trên Trang chủ — thẻ chỉ số mới nhất + xu hướng, giống app diaB */
export function HealthTrends() {
  const { data: metrics } = useHealthTrends();
  if (!metrics || metrics.length === 0) return null;

  return (
    <section aria-label="Chỉ số sức khoẻ" className="mb-5">
      <h2 className="mb-3 text-lg font-bold text-slate-900">Chỉ số sức khoẻ</h2>
      <div className="-mx-1 flex gap-3 overflow-x-auto px-1 pb-1">
        {metrics.map((m) => (
          <MetricCard key={m.testCode} m={m} />
        ))}
      </div>
    </section>
  );
}
