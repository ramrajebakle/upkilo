"use client";

import React from "react";
import { Shield } from "lucide-react";

export default function AdminAuthLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="min-h-screen bg-neutral-950 flex flex-col items-center justify-center p-4">
      <div className="mb-8 flex flex-col items-center">
        <div className="h-16 w-16 bg-red-600 rounded-2xl flex items-center justify-center shadow-[0_0_30px_rgba(220,38,38,0.4)] mb-4 animate-pulse">
          <Shield className="w-10 h-10 text-white" />
        </div>
        <h1 className="text-2xl font-bold text-white tracking-widest uppercase">
          Upkilo <span className="text-red-500">Secure</span> Admin
        </h1>
        <p className="text-neutral-500 text-sm mt-1">Platform Control Center</p>
      </div>
      
      <main className="w-full max-w-md animate-fade-in">
        {children}
      </main>

      <div className="mt-12 text-neutral-600 text-[10px] uppercase tracking-[0.2em]">
        Classified Section &bull; Authorization Required
      </div>
    </div>
  );
}
