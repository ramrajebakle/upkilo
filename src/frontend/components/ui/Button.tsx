import React from "react";
import { Loader2 } from "lucide-react";
import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "ghost" | "danger" | "ai" | "outline";
  size?: "sm" | "md" | "lg" | "icon";
  loading?: boolean;
  leftIcon?: React.ReactNode;
  rightIcon?: React.ReactNode;
  fullWidth?: boolean;
}

export const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      className,
      variant = "primary",
      size = "md",
      loading = false,
      leftIcon,
      rightIcon,
      fullWidth = false,
      children,
      disabled,
      ...props
    },
    ref
  ) => {
    const baseStyles =
      "inline-flex items-center justify-center gap-2 font-medium transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:opacity-50 disabled:pointer-events-none";

    const variants = {
      primary: "bg-primary-500 text-white hover:bg-primary-600 shadow-sm hover:shadow-md",
      secondary: "bg-surface-100 text-text-primary border border-surface-200 hover:bg-surface-200 hover:border-surface-300",
      ghost: "bg-transparent text-text-secondary hover:bg-surface-100 hover:text-text-primary",
      danger: "bg-danger-500 text-white hover:bg-danger-600 shadow-sm",
      ai: "bg-ai-500 text-white ring-1 ring-ai-500/30 shadow-[0_0_15px_rgba(124,58,237,0.3)] hover:shadow-[0_0_25px_rgba(124,58,237,0.5)]",
      outline: "bg-transparent border border-primary-500 text-primary-600 hover:bg-primary-50",
    };

    const sizes = {
      sm: "h-8 px-3 text-sm rounded-md",
      md: "h-9 px-4 text-sm rounded-md",
      lg: "h-11 px-5 text-base rounded-lg",
      icon: "h-9 w-9 rounded-md",
    };

    return (
      <button
        ref={ref}
        className={cn(
          baseStyles,
          variants[variant],
          sizes[size],
          fullWidth && "w-full",
          className
        )}
        disabled={disabled || loading}
        aria-busy={loading || undefined}
        {...props}
      >
        {loading && <Loader2 className="animate-spin" size={size === "sm" ? 14 : 16} aria-hidden="true" />}
        {!loading && leftIcon}
        {children}
        {!loading && rightIcon}
      </button>
    );
  }
);

Button.displayName = "Button";
