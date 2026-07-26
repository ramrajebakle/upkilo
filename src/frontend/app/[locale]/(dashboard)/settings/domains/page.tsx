'use client';

import { CustomDomainSettings } from '@/components/settings/CustomDomainSettings';
import { FeatureGate } from '@/components/ui/FeatureGate';

export default function DomainsSettingsPage() {
    return (
        <FeatureGate 
            featureName="WhiteLabelDomain" 
            title="Custom Domain"
            description="Upgrade your plan to use a custom white-label domain for your booking portal."
        >
            <CustomDomainSettings />
        </FeatureGate>
    );
}
