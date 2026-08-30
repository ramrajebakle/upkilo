'use client';

import { useState } from 'react';
import {
    Building2, Clock, CreditCard, Bell, Link2, Users,
    Key, Shield, Globe, Palette, Save, ChevronRight, Terminal
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { TeamSettings } from '@/components/settings/TeamSettings';
import { BillingSettings } from '@/components/settings/BillingSettings';

import WidgetGenerator from '@/components/booking/WidgetGenerator';
import { CustomFieldsSettings } from '@/components/settings/CustomFieldsSettings';
import BookingSettings from '@/components/settings/BookingSettings';
import { DeveloperSettings } from '@/components/settings/DeveloperSettings';

type SettingsTab =
    | 'business'
    | 'booking'
    | 'notifications'
    | 'payments'
    | 'integrations'
    | 'team'
    | 'security'
    | 'custom-fields'
    | 'developer';

const tabs = [
    { id: 'business' as const, label: 'Business', icon: Building2 },
    { id: 'booking' as const, label: 'Booking', icon: Clock },
    { id: 'notifications' as const, label: 'Notifications', icon: Bell },
    { id: 'payments' as const, label: 'Payments', icon: CreditCard },
    { id: 'integrations' as const, label: 'Integrations', icon: Link2 },
    { id: 'team' as const, label: 'Team', icon: Users },
    { id: 'security' as const, label: 'Security', icon: Shield },
    { id: 'custom-fields' as const, label: 'Custom Fields', icon: Palette },
    { id: 'developer' as const, label: 'Developer (API/OAuth)', icon: Terminal },
];

export default function SettingsPage() {
    const [activeTab, setActiveTab] = useState<SettingsTab>('business');
    const [saving, setSaving] = useState(false);

    const handleSave = async () => {
        setSaving(true);
        await new Promise((r) => setTimeout(r, 1000));
        setSaving(false);
    };

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-foreground">Settings</h1>
                    <p className="text-foreground-secondary mt-1">Manage your business configuration</p>
                </div>
                <Button onClick={handleSave} loading={saving}>
                    <Save className="h-4 w-4 mr-2" />
                    Save Changes
                </Button>
            </div>

            <div className="flex gap-6">
                {/* Sidebar */}
                <div className="w-56 flex-shrink-0">
                    <nav className="space-y-1">
                        {tabs.map((tab) => (
                            <button
                                key={tab.id}
                                onClick={() => setActiveTab(tab.id)}
                                className={cn(
                                    'w-full flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors',
                                    activeTab === tab.id
                                        ? 'bg-brand-subtle text-primary'
                                        : 'text-foreground-secondary hover:bg-accent'
                                )}
                            >
                                <tab.icon className="h-5 w-5" />
                                {tab.label}
                                {activeTab === tab.id && (
                                    <ChevronRight className="h-4 w-4 ml-auto" />
                                )}
                            </button>
                        ))}
                    </nav>
                </div>

                {/* Content */}
                <div className="flex-1 bg-card rounded-xl shadow-sm border border-border p-6">
                    {activeTab === 'business' && <BusinessSettings />}
                    {activeTab === 'booking' && <BookingSettings />}
                    {activeTab === 'notifications' && <NotificationSettings />}
                    {activeTab === 'payments' && <BillingSettings />}
                    {activeTab === 'integrations' && <WidgetGenerator />}
                    {activeTab === 'team' && <TeamSettings />}
                    {activeTab === 'security' && <SecuritySettings />}
                    {activeTab === 'custom-fields' && <CustomFieldsSettings />}
                    {activeTab === 'developer' && <DeveloperSettings />}
                </div>
            </div>
        </div>
    );
}

function BusinessSettings() {
    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-lg font-semibold text-foreground mb-4">Business Information</h2>
                <div className="grid gap-4 max-w-lg">
                    <Input label="Business Name" defaultValue="Beauty Studio" />
                    <Input label="Subdomain" defaultValue="beautystudio" suffix=".upkilo.com" />
                    <Input label="Phone" defaultValue="+1 (555) 123-4567" />
                    <Input label="Email" defaultValue="contact@beautystudio.com" />
                    <Input label="Website" defaultValue="https://beautystudio.com" />
                </div>
            </div>

            <div>
                <h2 className="text-lg font-semibold text-foreground mb-4">Address</h2>
                <div className="grid gap-4 max-w-lg">
                    <Input label="Street Address" defaultValue="123 Main Street" />
                    <div className="grid grid-cols-2 gap-4">
                        <Input label="City" defaultValue="New York" />
                        <Input label="State" defaultValue="NY" />
                    </div>
                    <div className="grid grid-cols-2 gap-4">
                        <Input label="Postal Code" defaultValue="10001" />
                        <Input label="Country" defaultValue="USA" />
                    </div>
                </div>
            </div>

            <div>
                <h2 className="text-lg font-semibold text-foreground mb-4">Regional Settings</h2>
                <div className="grid gap-4 max-w-lg">
                    <div>
                        <label className="block text-sm font-medium text-foreground mb-1">Timezone</label>
                        <select className="w-full px-3 py-2 border border-border-strong rounded-lg">
                            <option>America/New_York (Eastern Time)</option>
                            <option>America/Chicago (Central Time)</option>
                            <option>America/Los_Angeles (Pacific Time)</option>
                        </select>
                    </div>
                    <div>
                        <label className="block text-sm font-medium text-foreground mb-1">Currency</label>
                        <select className="w-full px-3 py-2 border border-border-strong rounded-lg">
                            <option>USD - US Dollar</option>
                            <option>EUR - Euro</option>
                            <option>GBP - British Pound</option>
                        </select>
                    </div>
                </div>
            </div>
        </div>
    );
}


function NotificationSettings() {
    return (
        <div className="space-y-6">
            <h2 className="text-lg font-semibold text-foreground">Notification Preferences</h2>

            <div>
                <h3 className="font-medium text-foreground mb-3">Email Notifications</h3>
                <div className="space-y-3">
                    <ToggleSetting label="New Booking" defaultChecked={true} />
                    <ToggleSetting label="Booking Cancellation" defaultChecked={true} />
                    <ToggleSetting label="Payment Received" defaultChecked={true} />
                    <ToggleSetting label="Daily Summary" defaultChecked={true} />
                </div>
            </div>

            <div className="pt-4 border-t">
                <h3 className="font-medium text-foreground mb-3">SMS Notifications</h3>
                <div className="space-y-3">
                    <ToggleSetting label="New Booking" defaultChecked={false} />
                    <ToggleSetting label="Booking Reminders" defaultChecked={true} />
                    <ToggleSetting label="Cancellation" defaultChecked={true} />
                </div>
            </div>
        </div>
    );
} function IntegrationSettings() {
    const integrations = [
        { id: 'google', name: 'Google Calendar', connected: true, icon: '📅' },
        { id: 'stripe', name: 'Stripe', connected: true, icon: '💳' },
        { id: 'twilio', name: 'Twilio SMS', connected: true, icon: '📱' },
        { id: 'zoom', name: 'Zoom', connected: false, icon: '🎥' },
        { id: 'quickbooks', name: 'QuickBooks', connected: false, icon: '📊' },
    ];

    return (
        <div className="space-y-6">
            <h2 className="text-lg font-semibold text-foreground">Connected Services</h2>

            <div className="space-y-3">
                {integrations.map((integration) => (
                    <div
                        key={integration.id}
                        className="flex items-center justify-between p-4 border border-border rounded-lg"
                    >
                        <div className="flex items-center gap-3">
                            <span className="text-2xl">{integration.icon}</span>
                            <div>
                                <p className="font-medium text-foreground">{integration.name}</p>
                                <p className="text-sm text-foreground-secondary">
                                    {integration.connected ? 'Connected' : 'Not connected'}
                                </p>
                            </div>
                        </div>
                        <button
                            className={cn(
                                'px-4 py-2 rounded-lg text-sm font-medium',
                                integration.connected
                                    ? 'text-danger-fg hover:bg-red-50'
                                    : 'bg-primary-500 text-white hover:bg-primary-600'
                            )}
                        >
                            {integration.connected ? 'Disconnect' : 'Connect'}
                        </button>
                    </div>
                ))}
            </div>
        </div>
    );
}


function SecuritySettings() {
    return (
        <div className="space-y-6">
            <h2 className="text-lg font-semibold text-foreground">Security Settings</h2>

            <div className="space-y-4">
                <div className="p-4 border border-border rounded-lg">
                    <div className="flex items-center justify-between mb-2">
                        <h3 className="font-medium text-foreground">Two-Factor Authentication</h3>
                        <Button variant="outline" size="sm">Enable</Button>
                    </div>
                    <p className="text-sm text-foreground-secondary">
                        Add an extra layer of security to your account
                    </p>
                </div>

                <div className="p-4 border border-border rounded-lg">
                    <div className="flex items-center justify-between mb-2">
                        <h3 className="font-medium text-foreground">API Keys</h3>
                        <Button variant="outline" size="sm">Manage</Button>
                    </div>
                    <p className="text-sm text-foreground-secondary">
                        1 active API key
                    </p>
                </div>

                <div className="p-4 border border-border rounded-lg">
                    <div className="flex items-center justify-between mb-2">
                        <h3 className="font-medium text-foreground">Active Sessions</h3>
                        <Button variant="outline" size="sm">View All</Button>
                    </div>
                    <p className="text-sm text-foreground-secondary">
                        2 active sessions
                    </p>
                </div>
            </div>
        </div>
    );
}

function ToggleSetting({
    label,
    description,
    defaultChecked = false,
}: {
    label: string;
    description?: string;
    defaultChecked?: boolean;
}) {
    const [checked, setChecked] = useState(defaultChecked);

    return (
        <label className="flex items-center justify-between cursor-pointer">
            <div>
                <p className="font-medium text-foreground">{label}</p>
                {description && <p className="text-sm text-foreground-secondary">{description}</p>}
            </div>
            <button
                type="button"
                role="switch"
                aria-checked={checked}
                onClick={() => setChecked(!checked)}
                className={cn(
                    'relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors',
                    checked ? 'bg-primary-500' : 'bg-gray-200'
                )}
            >
                <span
                    className={cn(
                        'pointer-events-none inline-block h-5 w-5 transform rounded-full bg-control-thumb shadow ring-0 transition',
                        checked ? 'translate-x-5' : 'translate-x-0'
                    )}
                />
            </button>
        </label>
    );
}
