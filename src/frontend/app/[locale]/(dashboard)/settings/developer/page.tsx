'use client';

import { DeveloperSettings } from '@/components/settings/DeveloperSettings';
import { FeatureGate } from '@/components/ui/FeatureGate';

export default function DeveloperSettingsPage() {
    return (
        <FeatureGate 
            featureName="ApiAccess" 
            title="API Access"
            description="Upgrade your plan to unlock API access and build custom integrations."
        >
            <DeveloperSettings />
        </FeatureGate>
    );
}
