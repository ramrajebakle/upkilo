'use client';

import { MarketplaceSettings } from '@/components/settings/MarketplaceSettings';
import { Breadcrumbs } from '@/components/ui/Breadcrumbs';

export default function MarketplacePage() {
    return (
        <div className="p-8 max-w-7xl mx-auto">
            <div className="mb-8">
                <Breadcrumbs 
                    items={[
                        { label: 'Dashboard', href: '/' },
                        { label: 'Settings', href: '/settings' },
                        { label: 'Marketplace', active: true }
                    ]} 
                />
                <h1 className="text-3xl font-black text-gray-900 mt-4 tracking-tight">Marketplace & Monetization</h1>
                <p className="text-gray-500 mt-1 max-w-2xl font-medium">
                    Boost your business visibility and manage your presence in the Upkilo consumer marketplace.
                </p>
            </div>

            <MarketplaceSettings />
        </div>
    );
}
