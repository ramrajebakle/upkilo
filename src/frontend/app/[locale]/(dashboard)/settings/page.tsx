'use client';

import { useEffect } from 'react';
import { useRouter } from '@/navigation';

export default function SettingsRootPage() {
    const router = useRouter();

    useEffect(() => {
        // Redirect to the default settings tab: Business
        router.push('/settings/business');
    }, [router]);

    return (
        <div className="flex items-center justify-center p-12">
            <div className="flex flex-col items-center gap-4">
                <div className="w-8 h-8 border-4 border-primary/25 border-t-primary-500 rounded-full animate-spin" />
                <p className="text-foreground-secondary font-medium">Redirecting to settings...</p>
            </div>
        </div>
    );
}
