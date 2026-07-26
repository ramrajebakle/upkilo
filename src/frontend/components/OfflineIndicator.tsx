"use client";

import { useEffect, useState } from "react";
import { WifiOff } from "lucide-react";

export function OfflineIndicator() {
    const [isOffline, setIsOffline] = useState(false);

    useEffect(() => {
        // Initial check
        setIsOffline(!navigator.onLine);

        const handleOnline = () => setIsOffline(false);
        const handleOffline = () => setIsOffline(true);

        window.addEventListener("online", handleOnline);
        window.addEventListener("offline", handleOffline);

        return () => {
            window.removeEventListener("online", handleOnline);
            window.removeEventListener("offline", handleOffline);
        };
    }, []);

    if (!isOffline) return null;

    return (
        <div className="fixed top-4 left-1/2 -translate-x-1/2 z-50 bg-destructive text-destructive-foreground px-4 py-2 rounded-full shadow-lg flex items-center space-x-2 text-sm font-medium animate-in slide-in-from-top-4">
            <WifiOff size={16} />
            <span>You are currently offline. Some features may be unavailable.</span>
        </div>
    );
}
