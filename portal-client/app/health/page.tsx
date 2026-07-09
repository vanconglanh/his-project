"use client";

import { useMemo, useState } from "react";
import { LineChart } from "@/components/LineChart";
import { useHealthTrends } from "@/lib/hooks";
import type { HealthTrendMetric } from "@/lib/types";
import { LoadingBlock } from "@/components/StateViews";

/** 7 chỉ số cố định hiển thị trên dashboard (khớp app diaB). Khớp theo mã XN (test_code). */
const METRIC_CONFIG: { codes: string[]; name: string; color: string }[] = [
  { codes: ["GLU", "GLUCOSE", "FBS"], name: "Đường huyết", color: "#7c3aed" },
  { codes: ["BP", "HABP", "HUYETAP"], name: "Huyết áp", color: "#ef4444" },
  { codes: ["WEIGHT", "CANNANG", "WT"], name: "Cân nặng", color: "#0ea5e9" },
  { codes: ["WAIST", "VONGEO", "WC"], name: "Vòng eo", color: "#f59e0b" },
  { codes: ["HBA1C", "A1C"], name: "HbA1c", color: "#01645A" },
  { codes: ["ALT", "SGPT", "GAN"], name: "Gan (ALT)", color: "#16a34a" },
  { codes: ["CREA", "CREATININE", "THAN"], name: "Thận (Creatinine)", color: "#64748b" },
];

function fmtDate(iso: string) {
  const d = new Date(iso);
  return `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}/${d.getFullYear()}`;
}

/** Nhãn trạng thái theo cờ bất thường của kết quả XN */
function statusOf(flag: string | null, metricName: string): { label: string; cls: string } {
  const f = (flag ?? "").toUpperCase();
  if (["H", "HH", "HIGH", "CRITICAL"].includes(f)) return { label: "Cao", cls: "bg-red-100 text-red-700" };
  if (["L", "LL", "LOW"].includes(f)) return { label: "Thấp", cls: "bg-amber-100 text-amber-700" };
  if (metricName === "HbA1c") return { label: "Lý tưởng", cls: "bg-teal-100 text-teal-800" };
  return { label: "Bình thường", cls: "bg-teal-100 text-teal-800" };
}

function Delta({ curr, prev }: { curr: number; prev: number | null }) {
  if (prev == null) return null;
  const d = curr - prev;
  if (Math.abs(d) < 1e-9) return <span className="text-sm text-slate-400">—</span>;
  const up = d > 0;
  return (
    <span className={`text-sm font-semibold ${up ? "text-red-600" : "text-teal-700"}`}>
      {up ? "▲" : "▼"} {Math.abs(Number(d.toFixed(2)))}
    </span>
  );
}

export default function HealthPage() {
  const { data: metrics, isLoading } = useHealthTrends();
  const [selCode, setSelCode] = useState<string | null>(null);
  const [range, setRange] = useState<"30" | "all">("all");

  // Ghép cấu hình 7 chỉ số với dữ liệu tra được
  const cards = useMemo(() => {
    const list = metrics ?? [];
    return METRIC_CONFIG.map((cfg) => ({
      cfg,
      metric: list.find((m) => cfg.codes.includes((m.testCode ?? "").toUpperCase())) ?? null,
    }));
  }, [metrics]);

  const selected: HealthTrendMetric | null = useMemo(() => {
    const withData = cards.filter((c) => c.metric).map((c) => c.metric!) as HealthTrendMetric[];
    if (withData.length === 0) return null;
    return withData.find((m) => m.testCode === selCode) ?? withData[0];
  }, [cards, selCode]);

  const selColor = METRIC_CONFIG.find((c) => c.codes.includes((selected?.testCode ?? "").toUpperCase()))?.color ?? "#01645A";

  const series = useMemo(() => {
    if (!selected) return [];
    if (range === "all") return selected.series;
    const cutoff = Date.now() - 30 * 86400 * 1000;
    const f = selected.series.filter((p) => new Date(p.date).getTime() >= cutoff);
    return f.length >= 2 ? f : selected.series;
  }, [selected, range]);

  const summary = useMemo(() => {
    if (series.length === 0) return null;
    const latest = series[series.length - 1];
    const prev = series.length >= 2 ? series[series.length - 2] : null;
    const avg = series.reduce((s, p) => s + p.value, 0) / series.length;
    return { latest, prev, avg };
  }, [series]);

  if (isLoading) return <div className="p-4"><LoadingBlock label="Đang tải chỉ số sức khoẻ..." /></div>;

  return (
    <div className="p-4">
      <h1 className="mb-4 text-slate-900">Chỉ số sức khoẻ</h1>

      {/* Lưới 7 thẻ chỉ số */}
      <section aria-label="Chỉ số tổng quát" className="mb-6 grid grid-cols-2 gap-3">
        {cards.map(({ cfg, metric }) => {
          const active = metric && selected?.testCode === metric.testCode;
          const prev = metric && metric.series.length >= 2 ? metric.series[metric.series.length - 2].value : null;
          const st = metric ? statusOf(metric.latestFlag, cfg.name) : null;
          return (
            <button
              key={cfg.name}
              type="button"
              disabled={!metric}
              onClick={() => metric && setSelCode(metric.testCode)}
              className={`rounded-2xl border-2 bg-white p-4 text-left shadow-sm transition-colors ${
                active ? "border-[#01645A]" : "border-slate-100"
              } ${!metric ? "opacity-70" : "hover:border-teal-400"}`}
            >
              <p className="text-sm font-medium text-slate-500" style={{ color: metric ? cfg.color : undefined }}>
                {cfg.name}
              </p>
              {metric ? (
                <>
                  <div className="mt-1 flex items-baseline gap-1">
                    <span className="text-2xl font-bold text-slate-900">{metric.latestValue}</span>
                    {metric.unit && <span className="text-sm text-slate-500">{metric.unit}</span>}
                    <span className="ml-auto">
                      <Delta curr={metric.latestValue} prev={prev} />
                    </span>
                  </div>
                  {st && (
                    <span className={`mt-1 inline-block rounded-full px-2 py-0.5 text-xs font-semibold ${st.cls}`}>
                      {st.label}
                    </span>
                  )}
                  <p className="mt-1 text-xs text-slate-400">{fmtDate(metric.latestDate)}</p>
                </>
              ) : (
                <p className="mt-3 text-sm text-slate-400">Chưa có dữ liệu</p>
              )}
            </button>
          );
        })}
      </section>

      {/* Biểu đồ tổng quát */}
      {selected ? (
        <section aria-label="Biểu đồ tổng quát">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-lg font-bold text-slate-900">{selected.testName}</h2>
            <div className="flex gap-1 rounded-full bg-slate-100 p-1 text-sm">
              <button
                type="button"
                onClick={() => setRange("30")}
                className={`rounded-full px-3 py-1 font-medium ${range === "30" ? "bg-white text-[#01645A] shadow-sm" : "text-slate-500"}`}
              >
                30 ngày
              </button>
              <button
                type="button"
                onClick={() => setRange("all")}
                className={`rounded-full px-3 py-1 font-medium ${range === "all" ? "bg-white text-[#01645A] shadow-sm" : "text-slate-500"}`}
              >
                Tất cả
              </button>
            </div>
          </div>

          <div className="rounded-2xl border-2 border-slate-100 bg-white p-3 shadow-sm">
            <div className="mb-2 flex items-center gap-2 text-sm text-slate-600">
              <span className="inline-block h-3 w-3 rounded-full" style={{ background: selColor }} />
              {selected.testName}
            </div>
            <LineChart points={series} unit={selected.unit} color={selColor} />
          </div>

          {summary && (
            <div className="mt-3 grid grid-cols-3 gap-3 text-center">
              <div className="rounded-2xl bg-white p-3 shadow-sm">
                <p className="text-xs text-slate-500">Lần trước</p>
                <p className="mt-1 text-lg font-bold text-slate-900">
                  {summary.prev ? `${summary.prev.value}` : "—"}
                </p>
                {summary.prev && <p className="text-xs text-slate-400">{fmtDate(summary.prev.date)}</p>}
              </div>
              <div className="rounded-2xl bg-white p-3 shadow-sm">
                <p className="text-xs text-slate-500">Gần nhất</p>
                <p className="mt-1 text-lg font-bold text-slate-900">{summary.latest.value}</p>
                <div className="text-xs">
                  <Delta curr={summary.latest.value} prev={summary.prev?.value ?? null} />
                </div>
              </div>
              <div className="rounded-2xl bg-white p-3 shadow-sm">
                <p className="text-xs text-slate-500">Trung bình</p>
                <p className="mt-1 text-lg font-bold text-slate-900">{summary.avg.toFixed(1)}</p>
                <p className="text-xs text-slate-400">{selected.unit}</p>
              </div>
            </div>
          )}
        </section>
      ) : (
        <p className="rounded-2xl border-2 border-dashed border-slate-200 p-6 text-center text-base text-slate-400">
          Chưa có chỉ số nào để hiển thị. Kết quả xét nghiệm sẽ hiện tại đây sau khi khám.
        </p>
      )}
    </div>
  );
}
