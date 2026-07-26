"use client";

import React from "react";
import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export interface SwitchProps extends Omit<React.ButtonHTMLAttributes<HTMLButtonElement>, 'onChange'> {
  checked: boolean;
  onChange: (checked: boolean) => void;
  variant?: "tenant" | "ai";
}

export const Switch = React.forwardRef<HTMLButtonElement, SwitchProps>(
  ({ className, checked, onChange, variant = "tenant", disabled, ...props }, ref) => {
    
    const bgColors = {
      tenant: checked ? "bg-tenant-500" : "bg-neutral-300",
      ai: checked ? "bg-ai-500 shadow-[0_0_10px_rgba(124,58,237,0.3)]" : "bg-neutral-300",
    };

    return (
      <button
        type="button"
        role="switch"
        aria-checked={checked}
        disabled={disabled}
        ref={ref}
        onClick={() => onChange(!checked)}
        className={cn(
          "relative inline-flex h-6 w-11 shrink-0 cursor-pointer items-center rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 focus-visible:ring-offset-surface-base disabled:cursor-not-allowed disabled:opacity-50",
          bgColors[variant],
          className
        )}
        {...props}
      >
        <span
          aria-hidden="true"
          className={cn(
            "pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow-sm ring-0 transition duration-200 ease-in-out",
            checked ? "translate-x-5" : "translate-x-0"
          )}
        />
      </button>
    );
  }
);

Switch.displayName = "Switch";
