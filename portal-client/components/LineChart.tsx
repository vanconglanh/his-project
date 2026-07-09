"use client";

interface ChartPoint {
  date: string;
  value: number;
}

/** Định dạng dd/MM cho trục ngày */
function fmtDay(iso: string): string {
  const d = new Date(iso);
  return `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}`;
}

/** "Số đẹp" cho mốc trục Y */
function niceStep(range: number, ticks = 4): number {
  const raw = range / ticks || 1;
  const mag = Math.pow(10, Math.floor(Math.log10(raw)));
  const norm = raw / mag;
  const step = norm >= 5 ? 5 : norm >= 2 ? 2 : 1;
  return step * mag;
}

/**
 * Biểu đồ đường tự vẽ bằng SVG (không thư viện): trục Y có mốc, trục X có ngày,
 * lưới ngang, đường xu hướng + điểm, đánh dấu điểm mới nhất. Dùng cho màn Sức khoẻ.
 */
export function LineChart({
  points,
  unit,
  color = "#01645A",
}: {
  points: ChartPoint[];
  unit?: string | null;
  color?: string;
}) {
  if (points.length < 2) {
    return (
      <div className="flex h-56 items-center justify-center rounded-2xl border-2 border-dashed border-slate-200 text-base text-slate-400">
        Chưa đủ dữ liệu để vẽ biểu đồ
      </div>
    );
  }

  const W = 340, H = 240;
  const padL = 44, padR = 12, padT = 12, padB = 28;
  const plotW = W - padL - padR;
  const plotH = H - padT - padB;

  const values = points.map((p) => p.value);
  const rawMin = Math.min(...values);
  const rawMax = Math.max(...values);
  const step = niceStep(rawMax - rawMin || rawMax || 1);
  const yMin = Math.floor(rawMin / step) * step;
  const yMax = Math.ceil((rawMax + step * 0.1) / step) * step;
  const ySpan = yMax - yMin || 1;

  const ticks: number[] = [];
  for (let v = yMin; v <= yMax + 1e-9; v += step) ticks.push(Number(v.toFixed(4)));

  const x = (i: number) => padL + (plotW * i) / (points.length - 1);
  const y = (v: number) => padT + plotH * (1 - (v - yMin) / ySpan);

  const linePath = points
    .map((p, i) => `${i === 0 ? "M" : "L"}${x(i).toFixed(1)},${y(p.value).toFixed(1)}`)
    .join(" ");

  // Nhãn ngày ở trục X: hiển thị tối đa ~4 mốc để đỡ chồng
  const labelIdx = new Set<number>();
  const nLabels = Math.min(4, points.length);
  for (let k = 0; k < nLabels; k++) labelIdx.add(Math.round((k * (points.length - 1)) / (nLabels - 1)));

  return (
    <svg viewBox={`0 0 ${W} ${H}`} className="w-full" role="img" aria-label="Biểu đồ xu hướng">
      {/* Lưới ngang + mốc trục Y */}
      {ticks.map((t) => (
        <g key={t}>
          <line x1={padL} y1={y(t)} x2={W - padR} y2={y(t)} stroke="#e2e8f0" strokeWidth="1" strokeDasharray="3 3" />
          <text x={padL - 6} y={y(t) + 4} textAnchor="end" fontSize="10" fill="#94a3b8">
            {t}
          </text>
        </g>
      ))}
      {/* Nhãn ngày trục X */}
      {points.map((p, i) =>
        labelIdx.has(i) ? (
          <text key={i} x={x(i)} y={H - 8} textAnchor="middle" fontSize="10" fill="#94a3b8">
            {fmtDay(p.date)}
          </text>
        ) : null,
      )}
      {/* Vùng nền dưới đường */}
      <path
        d={`${linePath} L${x(points.length - 1).toFixed(1)},${(padT + plotH).toFixed(1)} L${x(0).toFixed(1)},${(padT + plotH).toFixed(1)} Z`}
        fill={color}
        opacity="0.08"
      />
      {/* Đường + điểm */}
      <path d={linePath} fill="none" stroke={color} strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" />
      {points.map((p, i) => (
        <circle key={i} cx={x(i)} cy={y(p.value)} r={i === points.length - 1 ? 4 : 2.5} fill={color} />
      ))}
      {unit && (
        <text x={padL - 6} y={padT - 2} textAnchor="end" fontSize="9" fill="#cbd5e1">
          {unit}
        </text>
      )}
    </svg>
  );
}
