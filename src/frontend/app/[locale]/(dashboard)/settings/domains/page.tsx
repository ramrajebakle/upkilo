'use client';

import { CustomDomainSettings } from '@/components/settings/CustomDomainSettings';
import { FeatureGate } from '@/components/ui/FeatureGate';
import { FEATURES } from '@/lib/featureKeys';

export default function DomainsSettingsPage() {
    return (
        <FeatureGate 
            featureName={FEATURES.WHITE_LABEL} 
            title="Custom Domain"
            description="Upgrade your plan to use a custom white-label domain for your booking portal."
        >
            <CustomDomainSettings />
        </FeatureGate>
    );
}
