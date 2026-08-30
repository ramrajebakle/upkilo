import React from "react";
import { Building2, ShieldCheck, AlertCircle } from "lucide-react";

interface BillingBannerProps {
  context: "platform" | "tenant";
  title: string;
  subtitle: string;
  status?: "active" | "warning" | "error";
  statusText?: string;
}

export const BillingBanner: React.FC<BillingBannerProps> = ({
  context,
  title,
  subtitle,
  status = "active",
  statusText,
}) => {
  const isPlatform = context === "platform";

  // Strict visual separation based on System Design
  const styles = {
    container: isPlatform
      ? "bg-gradient-to-r from-platform-900 to-platform-600 text-white"
      : "bg-gradient-to-r from-tenant-900 to-tenant-600 text-white",
    iconBg: isPlatform ? "bg-platform-500/20" : "bg-tenant-500/20",
    badge: {
      active: isPlatform ? "bg-platform-500/30 text-platform-50" : "bg-tenant-500/30 text-tenant-50",
      warning: "bg-warning-500/30 text-warning-50",
      error: "bg-danger-500/30 text-danger-50",
    },
  };

  const StatusIcon = status === "active" ? ShieldCheck : AlertCircle;

  return (
    <div className={`w-full rounded-xl p-6 shadow-md ${styles.container} relative overflow-hidden mb-8`}>
      {/* Decorative grain. Inline SVG turbulence rather than /noise.png — that file was never
          added to public/, so every render of this banner 404'd. */}
      <div
        className="absolute right-0 top-0 bottom-0 w-1/3 opacity-10 mix-blend-overlay pointer-events-none"
        style={{
          backgroundImage:
            "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='120' height='120'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.8' numOctaves='3'/%3E%3C/filter%3E%3Crect width='120' height='120' filter='url(%23n)' opacity='0.5'/%3E%3C/svg%3E\")",
        }}
      />
      <div className="absolute -right-10 -top-10 w-40 h-40 bg-card opacity-5 rounded-full blur-2xl pointer-events-none" />

      <div className="relative z-10 flex items-start sm:items-center justify-between gap-4 flex-col sm:flex-row">
        <div className="flex items-center gap-4">
          <div className={`p-3 rounded-xl backdrop-blur-sm ${styles.iconBg}`}>
            <Building2 size={28} className="text-white" />
          </div>
          <div>
            <div className="flex items-center gap-2 mb-1">
              <h1 className="text-2xl font-bold tracking-tight">{title}</h1>
              <span className="text-[10px] font-bold uppercase tracking-widest bg-white/20 px-2 py-0.5 rounded-full backdrop-blur-sm">
                {isPlatform ? "Platform" : "Tenant"}
              </span>
            </div>
            <p className="text-white/80 text-sm font-medium">{subtitle}</p>
          </div>
        </div>

        {statusText && (
          <div className={`flex items-center gap-2 px-3 py-1.5 rounded-lg backdrop-blur-sm text-sm font-semibold border border-white/10 ${styles.badge[status]}`}>
            <StatusIcon size={16} />
            {statusText}
          </div>
        )}
      </div>
    </div>
  );
};
