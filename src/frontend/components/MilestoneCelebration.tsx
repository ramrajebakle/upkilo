"use client";

import { useEffect, useState } from "react";
import { usePathname } from "@/navigation";

export function MilestoneCelebration() {
    const [showConfetti, setShowConfetti] = useState(false);
    const pathname = usePathname();

    // List of pages where milestones should NEVER pop up (Auth/Checkout)
    const isAuthPage = 
        pathname.includes("/login") || 
        pathname.includes("/register") || 
        pathname.includes("/reset-password");

    useEffect(() => {
        // Disabled the auto-demo timer. 
        // This logic is now ready to be triggered by a genuine 
        // SignalR event or API response after a real booking is completed.
        
        /* 
        const hasSeenFirstBooking = localStorage.getItem("milestone_first_booking");
        if (!hasSeenFirstBooking && !isAuthPage) {
            // Future trigger logic goes here
        }
        */
    }, [pathname, isAuthPage]);

    if (!showConfetti || isAuthPage) return null;

    return (
        <div className="fixed inset-0 pointer-events-none z-50 flex items-center justify-center">
            <div className="bg-primary text-primary-foreground p-6 rounded-lg shadow-2xl animate-bounce">
                🎉 Congratulations on your first booking! Your platform is ready to scale with you.
            </div>
        </div>
    );
}
