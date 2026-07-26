'use client';

interface ManageCookiesButtonProps {
    className?: string;
    children?: React.ReactNode;
}

/**
 * Dispatches 'manage-cookies-open' which CookieConsent listens for.
 * Place this anywhere in a layout footer so users can re-open preferences.
 */
export function ManageCookiesButton({ className, children }: ManageCookiesButtonProps) {
    const handleClick = () => {
        window.dispatchEvent(new Event('manage-cookies-open'));
    };

    return (
        <button onClick={handleClick} className={className} type="button">
            {children ?? 'Cookie Settings'}
        </button>
    );
}
