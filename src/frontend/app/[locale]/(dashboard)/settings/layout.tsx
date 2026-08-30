'use client';

import {
    Building2, User, Bell, CreditCard, Shield, Palette,
    Globe, Terminal, Webhook, Activity, Search, ShieldCheck, Users,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Link, usePathname } from '@/navigation';
import { useTranslations } from 'next-intl';
import { useEffect, useState } from 'react';
import { Breadcrumb } from '@/components/ui/Breadcrumb';

const navGroups = [
    {
        label: 'Account',
        items: [
            { id: 'profile', label: 'Profile', icon: User, href: '/settings/profile' },
            { id: 'notifications', label: 'Notifications', icon: Bell, href: '/settings/notifications' },
            { id: 'appearance', label: 'Appearance', icon: Palette, href: '/settings/appearance' },
        ],
    },
    {
        label: 'Business',
        items: [
            { id: 'business', label: 'Business Info', icon: Building2, href: '/settings/business' },
            { id: 'branding', label: 'Branding', icon: Palette, href: '/settings/branding' },
            { id: 'seo', label: 'SEO & Visibility', icon: Search, href: '/settings/seo' },
            { id: 'domains', label: 'Domains', icon: Globe, href: '/settings/domains' },
        ],
    },
    {
        label: 'Team & Billing',
        items: [
            { id: 'team', label: 'Team Members', icon: Users, href: '/settings/team' },
            { id: 'billing', label: 'Billing & Plans', icon: CreditCard, href: '/settings/billing' },
            { id: 'agency', label: 'Agency', icon: Building2, href: '/settings/agency' },
        ],
    },
    {
        label: 'Security & Compliance',
        items: [
            { id: 'security', label: 'Security', icon: Shield, href: '/settings/security' },
            { id: 'audit-logs', label: 'Audit Logs', icon: Activity, href: '/settings/audit-logs' },
        ],
    },
    {
        label: 'Integrations & API',
        items: [
            { id: 'integrations', label: 'Integrations', icon: Webhook, href: '/settings/integrations' },
            { id: 'developer', label: 'Developer', icon: Terminal, href: '/settings/developer' },
            { id: 'webhooks', label: 'Webhooks', icon: Webhook, href: '/settings/webhooks' },
        ],
    },
];

export default function SettingsLayout({ children }: { children: React.ReactNode }) {
    const pathname = usePathname();
    const t = useTranslations('Navigation');

    useEffect(() => {
        window.scrollTo({ top: 0, behavior: 'smooth' });
        const activeItem = document.querySelector('[data-active-tab="true"]');
        if (activeItem) {
            activeItem.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        }
    }, [pathname]);

    return (
        <div className="space-y-10">
            {/* Breadcrumb */}
            <Breadcrumb items={[{ label: 'Settings' }]} />

            {/* Header */}
            <div className="animate-fade-in-up">
                <div className="flex items-center gap-4 mb-4">
                    <div className="p-3 bg-gradient-to-br from-primary-600 to-primary-900 rounded-2xl shadow-xl shadow-primary-500/20 border border-primary-500/20">
                        <Building2 className="h-6 w-6 text-white" />
                    </div>
                    <div>
                        <h1 className="text-3xl font-black text-slate-900 dark:text-white uppercase tracking-tight">
                            {t('settings')}
                        </h1>
                        <p className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em] mt-1">Configuring Operative Infrastructure</p>
                    </div>
                </div>
            </div>

            <div className="flex flex-col lg:flex-row gap-10">
                {/* Sidebar */}
                <div className="lg:w-80 shrink-0 animate-fade-in-up" style={{ animationDelay: '100ms' }}>
                    <div className="bg-white dark:bg-slate-900/50 border border-slate-100 dark:border-slate-800 rounded-[32px] p-4 lg:sticky lg:top-24 shadow-2xl shadow-slate-200/40 dark:shadow-none backdrop-blur-xl max-h-[calc(100vh-12rem)] overflow-y-auto scrollbar-none">
                        <nav aria-label="Settings navigation">
                            {navGroups.map((group) => (
                                <div key={group.label} className="mb-4">
                                    <p className="px-3 mb-1 text-[9px] font-black text-foreground-muted uppercase tracking-[0.2em]">
                                        {group.label}
                                    </p>
                                    <div className="space-y-0.5">
                                        {group.items.map((tab) => {
                                            const Icon = tab.icon;
                                            const isActive = pathname === tab.href || (pathname === '/settings' && tab.id === 'business');
                                            return (
                                                <Link
                                                    key={tab.id}
                                                    href={tab.href as any}
                                                    data-active-tab={isActive}
                                                    aria-current={isActive ? 'page' : undefined}
                                                    className={cn(
                                                        'w-full flex items-center gap-3 px-4 py-2.5 rounded-xl text-[11px] font-bold uppercase tracking-widest transition-all group',
                                                        isActive
                                                            ? 'bg-primary-500 text-white shadow-lg shadow-primary-500/25'
                                                            : 'text-slate-500 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800/50 hover:text-slate-900 dark:hover:text-white'
                                                    )}
                                                >
                                                    <Icon className={cn("h-4 w-4 shrink-0 transition-transform group-hover:scale-110", isActive ? "text-white" : "text-foreground-muted")} aria-hidden="true" />
                                                    {tab.label}
                                                    {isActive && <div className="ml-auto w-1.5 h-1.5 rounded-full bg-white/80" aria-hidden="true" />}
                                                </Link>
                                            );
                                        })}
                                    </div>
                                </div>
                            ))}
                        </nav>
                        
                        <div className="mt-8 p-6 bg-slate-50 dark:bg-slate-950/50 rounded-2xl border border-slate-100 dark:border-slate-850">
                            <div className="flex items-center gap-3 mb-2">
                                <ShieldCheck className="h-4 w-4 text-primary-500" />
                                <span className="text-[9px] font-black text-slate-900 dark:text-white uppercase tracking-widest">Security Clearance</span>
                            </div>
                            <div className="w-full bg-slate-200 dark:bg-slate-800 h-1.5 rounded-full overflow-hidden">
                                <div className="bg-primary-500 h-full w-[85%] rounded-full shadow-glow" />
                            </div>
                            <p className="text-[8px] font-bold text-foreground-muted uppercase tracking-widest mt-3">Identity Integrity: 85%</p>
                        </div>
                    </div>
                </div>

                {/* Content Area */}
                <div className="flex-1 min-w-0">
                    <div className="animate-fade-in">
                        {children}
                    </div>
                </div>
            </div>
        </div>
    );
}
