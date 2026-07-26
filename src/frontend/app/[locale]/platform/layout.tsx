import React from "react";
import { OrbitPanel } from "@/components/shared/OrbitPanel";
import { AIPulseBar } from "@/components/ai/pulse-bar/AIPulseBar";
import { CommandPalette } from "@/components/ai/command-palette/CommandPalette";
import { AICopilotRail } from "@/components/ai/copilot-rail/AICopilotRail";

export default function PlatformLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="min-h-screen bg-surface-50 text-text-primary">
      <AIPulseBar />
      <OrbitPanel />
      <CommandPalette />
      <AICopilotRail />
      
      {/* 
        Main Content Area 
        The top padding accounts for the AIPulseBar.
        The left margin is handled by CSS based on OrbitPanel state,
        but for simplicity in this mockup we assume a flex layout or 
        let the OrbitPanel be fixed and add margin to main.
      */}
      <main className="transition-all duration-300 ease-default pt-[var(--pulse-bar-height,40px)] ml-[240px] [&:has(~aside.w-[60px])]:ml-[60px]">
        <div className="p-8 max-w-7xl mx-auto">
          {children}
        </div>
      </main>
    </div>
  );
}
