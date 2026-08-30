import React from "react";
import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: "default" | "elevated" | "interactive" | "glow";
}

export const Card = React.forwardRef<HTMLDivElement, CardProps>(
  ({ className, variant = "default", children, ...props }, ref) => {
    // bg-card, not bg-surface-base: a card painted in the PAGE colour is invisible in dark
    // mode, where the page sits a full step below every surface on it. The two tokens are
    // identical in light mode, which is why the bug never showed up there.
    //
    // Shadows come from --shadow-card / --shadow-popover rather than Tailwind's fixed
    // shadow-sm/md, because a shadow tuned for a white page all but vanishes on a dark one.
    const variants = {
      default: "bg-card text-card-foreground border border-border shadow-[var(--shadow-card)]",
      elevated: "bg-card text-card-foreground border border-border shadow-[var(--shadow-popover)]",
      interactive: "bg-card text-card-foreground border border-border shadow-[var(--shadow-card)] hover:shadow-[var(--shadow-popover)] hover:-translate-y-1 hover:border-border-strong cursor-pointer transition-all duration-200",
      // to-accent-400 was never a declared token, so the gradient had one stop and rendered
      // as a flat violet. The AI accent is the intended second stop.
      glow: "bg-card text-card-foreground border border-border shadow-[var(--shadow-card)] relative z-0 before:absolute before:inset-[-1px] before:rounded-[inherit] before:bg-gradient-to-br before:from-primary-400 before:to-ai-400 before:-z-10 before:opacity-0 hover:before:opacity-20 transition-all duration-200",
    };

    return (
      <div
        ref={ref}
        className={cn(
          "rounded-xl overflow-hidden",
          variants[variant],
          className
        )}
        {...props}
      >
        {children}
      </div>
    );
  }
);

Card.displayName = "Card";

export const CardHeader = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div ref={ref} className={cn("px-6 py-4 flex flex-col space-y-1.5", className)} {...props} />
  )
);
CardHeader.displayName = "CardHeader";

export const CardTitle = React.forwardRef<HTMLHeadingElement, React.HTMLAttributes<HTMLHeadingElement>>(
  ({ className, ...props }, ref) => (
    <h3 ref={ref} className={cn("font-semibold leading-none tracking-tight", className)} {...props} />
  )
);
CardTitle.displayName = "CardTitle";

export const CardDescription = React.forwardRef<HTMLParagraphElement, React.HTMLAttributes<HTMLParagraphElement>>(
  ({ className, ...props }, ref) => (
    <p ref={ref} className={cn("text-sm text-foreground-secondary", className)} {...props} />
  )
);
CardDescription.displayName = "CardDescription";

export const CardContent = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div ref={ref} className={cn("px-6 py-4 pt-0", className)} {...props} />
  )
);
CardContent.displayName = "CardContent";

export const CardFooter = React.forwardRef<HTMLDivElement, React.HTMLAttributes<HTMLDivElement>>(
  ({ className, ...props }, ref) => (
    <div ref={ref} className={cn("px-6 py-4 border-t border-border-subtle flex items-center", className)} {...props} />
  )
);
CardFooter.displayName = "CardFooter";
