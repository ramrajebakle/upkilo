'use client';

import { WebhookSettings } from '@/components/settings/WebhookSettings';
import { FeatureGate } from '@/components/ui/FeatureGate';

export default function WebhooksSettingsPage() {
    return (
        <FeatureGate 
            featureName="Webhooks" 
            title="Webhooks"
            description="Upgrade your plan to unlock webhooks and integrate your application with external services in real-time."
        >
            <WebhookSettings />
        </FeatureGate>
    );
}
