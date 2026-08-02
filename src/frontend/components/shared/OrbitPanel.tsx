"use client";

import React from "react";
import { useUIStore } from "@/stores/ui";
import {
  LayoutDashboard,
  Users,
  CreditCard,
  ShieldAlert,
  Settings,
  ChevronLeft,
  ChevronRight,
  Activity,
} from "lucide-react";
import { Link, usePathname } from "@/navigation";

// Platform staff only. Tenants use the full application shell in app/[locale]/(dashboard),
// which carries onboarding, global search, notifications, realtime and translations.
//
// These hrefs are locale-free by design: `@/navigation`'s Link prefixes the active locale.
// Using plain next/link here sent every click through the middleware's locale redirect,
// costing a 307 on each navigation.
const navItems = [
  { id: "command", label: "Command", icon: LayoutDashboard, href: "/platform/command" },
  { id: "tenants", label: "Tenants", icon: Users, href: "/platform/tenants" },
  { id: "revenue", label: "Platform Revenue", icon: CreditCard, href: "/platform/revenue" },
  { id: "ai-infra", label: "AI Infrastructure", icon: Activity, href: "/platform/ai-infra" },
  { id: "security", label: "Security", icon: ShieldAlert, href: "/platform/security" },
  { id: "settings", label: "Settings", icon: Settings, href: "/platform/settings" },
];

export const OrbitPanel = () => {
  const { orbitCollapsed, toggleOrbit } = useUIStore();
  const pathname = usePathname();

  return (
    <aside
      className={`fixed left-0 top-0 z-[var(--z-orbit)] h-screen border-r border-neutral-200 bg-surface-base transition-all duration-300 ease-default flex flex-col ${
        orbitCollapsed ? "w-[60px]" : "w-[240px]"
      }`}
    >
      <div className="flex h-[var(--pulse-bar-height,40px)] items-center justify-between px-4 border-b border-neutral-100">
        {!orbitCollapsed && (
          <span className="font-semibold text-text-primary tracking-tight">Upkilo</span>
        )}
        <button
          onClick={toggleOrbit}
          className="p-1 rounded-md hover:bg-neutral-100 text-neutral-500 transition-colors"
          aria-label={orbitCollapsed ? "Expand sidebar" : "Collapse sidebar"}
        >
          {orbitCollapsed ? <ChevronRight size={16} /> : <ChevronLeft size={16} />}
        </button>
      </div>

      <div className="flex-1 overflow-y-auto py-4 px-2 space-y-1">
        {navItems.map((item) => {
          const isActive = pathname?.startsWith(item.href);
          const Icon = item.icon;

          return (
            <Link
              key={item.id}
              href={item.href}
              className={`flex items-center gap-3 px-3 py-2 rounded-md transition-all duration-200 ${
                isActive
                  ? "bg-neutral-100 text-primary-600 font-medium relative"
                  : "text-text-secondary hover:bg-neutral-50 hover:text-text-primary"
              } ${orbitCollapsed ? "justify-center" : "justify-start"}`}
              title={orbitCollapsed ? item.label : undefined}
            >
              {isActive && !orbitCollapsed && (
                <div className="absolute left-0 top-1/2 -translate-y-1/2 w-[3px] h-3/5 bg-primary-500 rounded-r-sm" />
              )}
              <Icon size={18} className={isActive ? "text-primary-500" : ""} />
              {!orbitCollapsed && <span>{item.label}</span>}
            </Link>
          );
        })}
      </div>
    </aside>
  );
};
