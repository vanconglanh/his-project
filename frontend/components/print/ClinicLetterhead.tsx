import { cn } from "@/lib/utils";

export interface ClinicLetterheadProps {
  clinicName: string;
  companyName: string;
  cskcbCode?: string | null;
  address: string;
  phone: string;
  email: string;
  logoUrl?: string | null;
  className?: string;
}

/**
 * Tiêu đề phòng khám dùng cho báo cáo in A4.
 * Nền xanh teal (#0F766E), logo bên trái, tên + địa chỉ bên phải.
 */
export function ClinicLetterhead({
  clinicName,
  companyName,
  cskcbCode,
  address,
  phone,
  email,
  logoUrl,
  className,
}: ClinicLetterheadProps) {
  const initial = clinicName.charAt(0).toUpperCase();

  return (
    <div
      className={cn(
        "flex items-center gap-4 bg-teal-700 text-white px-6 py-4 rounded-sm",
        className
      )}
      style={{ backgroundColor: "#0F766E" }}
    >
      {/* Logo */}
      <div className="shrink-0">
        {logoUrl ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={logoUrl}
            alt={`Logo ${clinicName}`}
            width={64}
            height={64}
            className="h-16 w-16 rounded-full object-cover border-2 border-white/40"
          />
        ) : (
          <div className="h-16 w-16 rounded-full bg-white/20 border-2 border-white/40 flex items-center justify-center">
            <span className="text-2xl font-bold text-white">{initial}</span>
          </div>
        )}
      </div>

      {/* Thông tin phòng khám */}
      <div className="flex flex-col gap-0.5 min-w-0">
        <p className="text-[16pt] font-bold uppercase leading-tight tracking-wide">
          {clinicName}
        </p>
        <p className="text-[13pt] font-bold uppercase leading-tight tracking-wide opacity-90">
          {companyName}
        </p>
        {cskcbCode && (
          <p className="text-[9pt] opacity-80 leading-snug">
            Mã CSKCB: {cskcbCode}
          </p>
        )}
        <p className="text-[9pt] opacity-80 leading-snug mt-0.5">
          {address} &nbsp;·&nbsp; ☎ {phone} &nbsp;·&nbsp; {email}
        </p>
      </div>
    </div>
  );
}
