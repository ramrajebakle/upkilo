'use client';

import { DeveloperSettings } from '@/components/settings/DeveloperSettings';
import { FeatureGate } from '@/components/ui/FeatureGate';
import { FEATURES } from '@/lib/featureKeys';

export default function DeveloperSettingsPage() {
    return (
        <FeatureGate 
            featureName={FEATURES.API_ACCESS} 
            title="API Access"
            description="Upgrade your plan to unlock API access and build custom integrations."
        >
            <DeveloperSettings />
        </FeatureGate>
    );
}
