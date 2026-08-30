'use client';

import { WebhookSettings } from '@/components/settings/WebhookSettings';
import { FeatureGate } from '@/components/ui/FeatureGate';
import { FEATURES } from '@/lib/featureKeys';

export default function WebhooksSettingsPage() {
    return (
        <FeatureGate 
            featureName={FEATURES.API_ACCESS} 
            title="Webhooks"
            description="Upgrade your plan to unlock webhooks and integrate your application with external services in real-time."
        >
            <WebhookSettings />
        </FeatureGate>
    );
}
