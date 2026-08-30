import * as React from 'react';
import { cn } from '@/lib/utils';

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
    label?: string;
    error?: string;
    leftIcon?: React.ReactNode;
    rightIcon?: React.ReactNode;
    suffix?: string;
}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
    ({ className, label, error, leftIcon, rightIcon, suffix, id, ...props }, ref) => {
        const inputId = id ?? (label ? `input-${label.toLowerCase().replace(/\s+/g, '-')}` : undefined);
        return (
            <div className="w-full">
                {label && (
                    <label htmlFor={inputId} className="block text-sm font-medium text-muted-foreground mb-1">
                        {label}
                        {props.required && (
                            <span className="ms-0.5 text-danger-fg" aria-hidden="true">*</span>
                        )}
                    </label>
                )}
                <div className="relative flex">
                    {leftIcon && (
                        <div className="absolute start-3 top-1/2 -translate-y-1/2 text-muted-foreground" aria-hidden="true">
                            {leftIcon}
                        </div>
                    )}
                    <input
                        id={inputId}
                        aria-describedby={error ? `${inputId}-error` : undefined}
                        aria-invalid={error ? true : undefined}
                        className={cn(
                            'w-full px-4 py-3 rounded-lg border border-input text-foreground bg-input-background',
                            'focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary',
                            'placeholder:text-text-tertiary transition-all duration-200',
                            'disabled:bg-muted disabled:cursor-not-allowed disabled:text-muted-foreground',
                            leftIcon && 'ps-10',
                            rightIcon && 'pe-10',
                            suffix && 'rounded-e-none border-e-0',
                            error && 'border-destructive focus:ring-destructive/20',
                            className
                        )}
                        ref={ref}
                        {...props}
                    />
                    {suffix && (
                        <span className="inline-flex items-center px-3 rounded-e-lg border border-s-0 border-input bg-muted text-muted-foreground text-sm">
                            {suffix}
                        </span>
                    )}
                    {rightIcon && (
                        <div className="absolute end-3 top-1/2 -translate-y-1/2 text-muted-foreground">
                            {rightIcon}
                        </div>
                    )}
                </div>
                {error && (
                    <p id={inputId ? `${inputId}-error` : undefined} role="alert" className="mt-1 text-sm text-danger-fg">{error}</p>
                )}
            </div>
        );
    }
);

Input.displayName = 'Input';

export { Input };
