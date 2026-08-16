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
      // active:scale-[0.97] is press feedback — the interface acknowledging it heard the
      // click. Buttons are pressed tens of times a day, which is the frequency tier where
      // motion has to be near-imperceptible or not exist: 0.97 and 160ms is deliberately at
      // the subtle end (the useful range is 0.95–0.98) so it registers as responsiveness
      // rather than as an animation.
      //
      // The duration is split from the blanket transition-all/200ms because press feedback
      // wants to be faster than colour transitions, and because transitioning `all` means
      // the browser watches every animatable property. transform and opacity are the two
      // that skip layout and paint, so scoping the transform to its own short duration keeps
      // the press on the compositor.
      //
      // motion-reduce:active:scale-100 opts the movement out under prefers-reduced-motion
      // while leaving the colour and shadow states intact — gentler, not zero.
      "inline-flex items-center justify-center gap-2 font-medium transition-all duration-200 " +
      "active:scale-[0.97] active:duration-[160ms] motion-reduce:active:scale-100 " +
      "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 disabled:opacity-50 disabled:pointer-events-none";

    const variants = {
      primary: "bg-primary-500 text-white hover:bg-primary-600 shadow-sm hover:shadow-md",
      secondary: "bg-surface-100 text-text-primary border border-surface-200 hover:bg-surface-200 hover:border-surface-300",
      ghost: "bg-transparent text-text-secondary hover:bg-surface-100 hover:text-text-primary",
      danger: "bg-danger-500 text-white hover:bg-danger-600 shadow-sm",
      ai: "bg-ai-500 text-white ring-1 ring-ai-500/30 shadow-[0_0_15px_rgba(124,58,237,0.3)] hover:shadow-[0_0_25px_rgba(124,58,237,0.5)]",
      outline: "bg-transparent border border-primary-500 text-primary-600 hover:bg-primary-50",
    };

    // Heights are a touch floor on small screens and revert to desktop density from `sm` up.
    //
    // A finger needs roughly 44px; a mouse pointer does not, and this is dashboard UI where
    // density is a feature rather than a compromise. Forcing 44px everywhere would inflate
    // every toolbar and table row on desktop to fix a problem that only exists on touch.
    // Tailwind is mobile-first, so `h-11 sm:h-9` reads as: 44px by default, 36px from 640px up.
    //
    // An audit at 390px measured 533 controls under 44px. These four tokens are the widest
    // single lever on that number — Button is imported by 127 of 205 dashboard pages — though
    // the raw <button> elements that never adopted this component are untouched and still
    // need the migration pass.
    const sizes = {
      sm: "h-11 sm:h-8 px-3 text-sm rounded-md",
      md: "h-11 sm:h-9 px-4 text-sm rounded-md",
      lg: "h-11 px-5 text-base rounded-lg",
      icon: "h-11 w-11 sm:h-9 sm:w-9 rounded-md",
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
