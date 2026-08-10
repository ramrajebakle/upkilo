'use client';

import { Link, usePathname } from '@/navigation';
import { cn } from '@/lib/utils';
import {
    LayoutDashboard, Calendar, Users, Briefcase, Building2, Activity,
    CreditCard, BarChart3, Settings, LogOut, Menu, X, UserCog, FileText,
    Star, Megaphone, Package, Gift, Ticket, ChevronRight, ChevronDown,
    Bell, Sparkles, Zap, Crown, Clock, ClipboardList, Shield, TrendingUp,
    Sun, Moon, Globe, Layers, Award, MessageSquare, Upload, FlaskConical,
    Flag, Copy, GraduationCap, AlertTriangle, GitBranch, Bot, Inbox,
    DollarSign, RotateCcw, Database, Phone, ShieldAlert, ShoppingBag, Target,
    Share2, Coins, Home, UserCheck, Truck, Heart, Percent, Camera, ArrowLeftRight, ArrowUpRight, Wrench, LifeBuoy,
    CheckCircle2, Link2, Fingerprint, BookOpen, Download,
} from 'lucide-react';
import { useEffect, useState, useCallback } from 'react';
import { useAuthStore } from '@/store/authStore';
import { NotificationCenter } from '@/components/NotificationCenter';
import { GlobalSearch } from '@/components/GlobalSearch';
import { useTheme } from '@/components/ThemeProvider';
import { OnboardingWizard } from '@/components/onboarding/OnboardingWizard';
import { ProductTour } from '@/components/onboarding/ProductTour';
import { useTranslations } from 'next-intl';
import LocaleSwitcher from '@/components/LocaleSwitcher';
import { SignalRProvider, useSignalR } from '@/contexts/SignalRContext';
import DemoModeBanner from '@/components/DemoModeBanner';
import { apiClient } from '@/lib/api';
import { AICopilotRail } from '@/components/tenant/ai-tools/AICopilotRail';
import { ManageCookiesButton } from '@/components/ManageCookiesButton';

type NavItem = {
    name: string;
    href: string;
    icon: React.ComponentType<{ className?: string }>;
    badge?: 'escalation';
};

type NavGroup = {
    label: string;
    items: NavItem[];
};

function EscalationBadge() {
    const [count, setCount] = useState(0);
    const { connection } = useSignalR();

    const fetchStats = useCallback(async () => {
        try {
            const res = await apiClient.get('/api/v1/escalations');
            setCount(res.data.total || 0);
        } catch {}
    }, []);

    useEffect(() => {
        fetchStats();
        if (connection) {
            connection.on('SystemEscalation', () => { fetchStats(); });
            return () => { connection.off('SystemEscalation'); };
        }
    }, [connection, fetchStats]);

    if (count === 0) return null;
    return (
        <span className="ms-auto flex h-5 w-5 items-center justify-center rounded-full bg-red-500 text-[10px] font-bold text-white shadow-sm ring-2 ring-white dark:ring-slate-900 animate-pulse">
            {count > 9 ? '9+' : count}
        </span>
    );
}

function NavGroupSection({ group, pathname, defaultOpen = true }: {
    group: NavGroup;
    pathname: string | null;
    defaultOpen?: boolean;
}) {
    const hasActive = group.items.some(item => pathname === item.href || pathname?.startsWith(item.href + '/'));
    const [open, setOpen] = useState(defaultOpen || hasActive);

    return (
        <div className="mb-1">
            <button
                onClick={() => setOpen(o => !o)}
                className="w-full flex items-center justify-between px-4 py-2 text-xs font-semibold uppercase tracking-wider text-slate-400 dark:text-slate-500 hover:text-slate-600 dark:hover:text-slate-300 transition-colors rounded-lg hover:bg-slate-50 dark:hover:bg-white/5"
                aria-expanded={open}
            >
                <span>{group.label}</span>
                <ChevronDown className={cn('h-3.5 w-3.5 transition-transform duration-200', open && 'rotate-180')} />
            </button>
            {open && (
                <div className="mt-0.5 space-y-0.5">
                    {group.items.map((item) => {
                        const isActive = pathname === item.href || (item.href !== '/dashboard' && pathname?.startsWith(item.href + '/'));
                        return (
                            <Link
                                key={item.name}
                                href={item.href}
                                aria-current={isActive ? 'page' : undefined}
                                className={cn(
                                    'group flex items-center gap-3 px-4 py-2.5 rounded-xl transition-all duration-200 relative',
                                    isActive
                                        ? 'bg-primary-50 text-primary-600 dark:bg-slate-800 dark:text-primary-400 shadow-sm'
                                        : 'text-slate-500 hover:text-slate-900 hover:bg-slate-50 dark:text-slate-400 dark:hover:text-white dark:hover:bg-white/5'
                                )}
                            >
                                {isActive && (
                                    <div className="absolute left-0 top-1/2 -translate-y-1/2 w-1 h-7 bg-primary-500 rounded-r-full shadow-lg shadow-primary-500/50" />
                                )}
                                <item.icon className={cn(
                                    'h-4 w-4 flex-shrink-0 transition-colors',
                                    isActive ? 'text-primary-400' : 'text-slate-400 group-hover:text-slate-600 dark:group-hover:text-slate-300'
                                )} />
                                <span className="font-medium text-sm truncate">{item.name}</span>
                                {item.badge === 'escalation' && <EscalationBadge />}
                                {isActive && <ChevronRight className="h-3.5 w-3.5 ms-auto text-primary-400 flex-shrink-0" />}
                            </Link>
                        );
                    })}
                </div>
            )}
        </div>
    );
}

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
    const t = useTranslations('Navigation');
    const pathname = usePathname();
    const [sidebarOpen, setSidebarOpen] = useState(false);
    const [copilotOpen, setCopilotOpen] = useState(false);
    const { user, logout, checkAuth, isInitialized } = useAuthStore();
    const { setTheme, resolvedTheme } = useTheme();

    const navGroups: NavGroup[] = [
        {
            label: 'Scheduling',
            items: [
                { name: t('dashboard'), href: '/dashboard', icon: LayoutDashboard },
                { name: t('bookings'), href: '/bookings', icon: Calendar },
                { name: 'Calendar', href: '/calendar', icon: Calendar },
                { name: 'Waitlist', href: '/bookings/waitlist', icon: Clock },
                { name: 'Classes', href: '/bookings/classes', icon: GraduationCap },
                { name: 'Class Packages', href: '/bookings/packages', icon: Package },
                { name: 'Walk-In', href: '/bookings/walk-in', icon: Zap },
                { name: 'Check-In', href: '/bookings/check-in', icon: CheckCircle2 },
                { name: 'Schedule Blocks', href: '/bookings/schedule-blocks', icon: ShieldAlert },
                { name: 'Booking Conflicts', href: '/bookings/conflicts', icon: AlertTriangle },
                { name: 'Auto-Rebooking', href: '/bookings/rebook', icon: RotateCcw },
                { name: 'AI Booking Chat', href: '/bookings/chat', icon: Bot },
            ],
        },
        {
            label: 'Clients & Team',
            items: [
                { name: t('clients'), href: '/clients', icon: Users },
                { name: 'Duplicate Clients', href: '/clients/duplicates', icon: Copy },
                { name: 'Segments', href: '/clients/segments', icon: Target },
                { name: 'Referrals', href: '/clients/referrals', icon: Share2 },
                { name: 'Credits', href: '/clients/credits', icon: Coins },
                { name: 'Households', href: '/clients/households', icon: Home },
                { name: 'Client Photos', href: '/clients/photos', icon: Camera },
                { name: 'Medical Alerts', href: '/clients/contraindications', icon: AlertTriangle },
                { name: 'Consent Records', href: '/clients/consent', icon: FileText },
                { name: t('staff'), href: '/staff', icon: UserCog },
                { name: 'Commission Reports', href: '/staff/commission-reports', icon: DollarSign },
                { name: 'Timesheets', href: '/staff/timesheets', icon: Clock },
                { name: 'Attendance', href: '/staff/attendance', icon: UserCheck },
                { name: 'Certifications', href: '/staff/certifications', icon: Award },
                { name: 'Earnings', href: '/staff/earnings', icon: TrendingUp },
                { name: 'Payouts', href: '/staff/payout', icon: CreditCard },
                { name: 'Shift Swaps', href: '/staff/shift-swaps', icon: ArrowLeftRight },
                { name: 'Performance', href: '/staff/performance', icon: TrendingUp },
                { name: 'Staff Schedule', href: '/staff/schedule', icon: Clock },
                { name: 'Inbox', href: '/inbox', icon: Inbox },
            ],
        },
        {
            label: 'Services & Revenue',
            items: [
                { name: t('services'), href: '/services', icon: Briefcase },
                { name: 'Service Bundles', href: '/services/bundles', icon: Layers },
                { name: 'Upsells', href: '/services/upsells', icon: ArrowUpRight },
                { name: 'Dynamic Pricing', href: '/services/pricing', icon: TrendingUp },
                { name: 'Pricing Rules', href: '/services/pricing/dynamic', icon: Zap },
                { name: 'Payments', href: '/payments', icon: CreditCard },
                { name: 'Tips', href: '/payments/tips', icon: Heart },
                { name: 'Razorpay', href: '/payments/razorpay', icon: CreditCard },
                { name: 'Global Payments', href: '/payments/global', icon: Globe },
                { name: 'Embedded Finance', href: '/payments/embedded-finance', icon: DollarSign },
                { name: 'Memberships', href: '/memberships', icon: Award },
                { name: 'Gift Certificates', href: '/memberships/certificates', icon: Gift },
                { name: 'Loyalty', href: '/loyalty', icon: Star },
                { name: 'Coupons', href: '/coupons', icon: Ticket },
                { name: 'Gift Cards', href: '/gift-cards', icon: Gift },
                { name: 'Deals', href: '/deals', icon: TrendingUp },
                { name: 'Franchise', href: '/franchise', icon: Building2 },
                { name: 'Products', href: '/products', icon: Package },
                { name: 'Orders', href: '/orders', icon: ShoppingBag },
                { name: 'Suppliers', href: '/suppliers', icon: Truck },
                { name: 'Purchase Orders', href: '/purchase-orders', icon: ClipboardList },
                { name: 'Equipment', href: '/equipment', icon: Wrench },
                { name: 'Inventory', href: '/inventory', icon: Database },
                { name: 'Store', href: '/store', icon: Globe },
                { name: 'Waivers', href: '/settings/waivers', icon: FileText },
            ],
        },
        {
            label: 'Marketing',
            items: [
                { name: 'Campaigns', href: '/campaigns', icon: Megaphone },
                { name: 'Drip Campaigns', href: '/marketing/drip-campaigns', icon: GitBranch },
                { name: 'Email Templates', href: '/marketing/templates', icon: FileText },
                { name: 'SMS Templates', href: '/marketing/sms-templates', icon: MessageSquare },
                { name: 'SMS Opt-In Import', href: '/marketing/sms-import', icon: Phone },
                { name: t('marketingAutomation'), href: '/marketing/automation', icon: Zap },
                { name: 'Landing Pages', href: '/marketing/landing-pages', icon: Globe },
                { name: 'Growth & SEO', href: '/growth', icon: TrendingUp },
                { name: 'Reviews', href: '/reviews', icon: Star },
                { name: 'Blog & Content', href: '/blog', icon: FileText },
                { name: 'Social Posts', href: '/marketing/social', icon: Share2 },
                { name: 'Bio Link', href: '/marketing/bio-link', icon: Link2 },
                { name: 'Proactive Messaging', href: '/marketing/proactive', icon: MessageSquare },
            ],
        },
        {
            label: 'Automation',
            items: [
                { name: 'Workflows', href: '/automation/workflows', icon: Zap },
                { name: 'Templates', href: '/automation/templates', icon: Layers },
                { name: 'Workflow Analytics', href: '/automation/analytics', icon: BarChart3 },
            ],
        },
        {
            label: 'Insights',
            items: [
                { name: t('analytics'), href: '/analytics', icon: BarChart3 },
                { name: 'Reports', href: '/reports', icon: ClipboardList },
                { name: 'Report Builder', href: '/reports/builder', icon: BarChart3 },
                { name: 'AI Dashboard', href: '/ai-dashboard', icon: Sparkles },
                { name: 'AI Tools', href: '/ai', icon: Bot },
                { name: 'Business Intelligence', href: '/ai/intelligence', icon: BarChart3 },
                { name: 'Financial Intelligence', href: '/ai/financial', icon: DollarSign },
                { name: 'Fill My Calendar', href: '/ai/fill-my-calendar', icon: Calendar },
                { name: 'Knowledge Base', href: '/ai/knowledge-base', icon: BookOpen },
            ],
        },
        {
            label: 'Settings',
            items: [
                { name: t('settings'), href: '/settings', icon: Settings },
                { name: 'Roles', href: '/settings/roles', icon: Shield },
                { name: 'Tax Rates', href: '/settings/tax-rates', icon: Percent },
                { name: 'Booking Policies', href: '/settings/booking-policies', icon: Settings },
                { name: 'Zapier', href: '/settings/zapier', icon: Zap },
                { name: 'Branding', href: '/settings/branding', icon: Crown },
                { name: 'Agency', href: '/settings/agency', icon: UserCog },
                { name: 'Integrations', href: '/integrations', icon: Layers },
                { name: 'Data Import', href: '/settings/data-import', icon: Upload },
                { name: t('auditLogs'), href: '/settings/audit-logs', icon: Shield },
                { name: 'A/B Testing', href: '/settings/experiments', icon: FlaskConical },
                { name: 'Feature Flags', href: '/settings/feature-flags', icon: Flag },
                { name: 'Backup & Restore', href: '/settings/backup', icon: Database },
                { name: 'Data Exports', href: '/settings/data-exports', icon: Download },
                { name: 'Migration Wizard', href: '/settings/migration', icon: Database },
                { name: 'Advanced Features', href: '/settings/advanced', icon: Zap },
                { name: 'Demo Mode', href: '/settings/demo', icon: FlaskConical },
                { name: 'Industry Features', href: '/settings/verticals', icon: Layers },
                { name: 'Kiosk', href: '/kiosk', icon: Monitor },
                { name: 'Plugins', href: '/plugins', icon: Package },
                { name: 'Security', href: '/security', icon: ShieldAlert },
                { name: '2-Factor Auth', href: '/settings/2fa', icon: Shield },
                { name: 'Social Login', href: '/settings/social-login', icon: Globe },
                { name: 'Biometrics', href: '/settings/biometrics', icon: Fingerprint },
                { name: 'Sessions', href: '/settings/sessions', icon: Monitor },
                { name: 'Compliance', href: '/settings/compliance', icon: Shield },
                { name: 'Legal Requests', href: '/settings/legal-requests', icon: Shield },
                { name: 'iCal Tokens', href: '/settings/integrations/ical', icon: Calendar },
                { name: 'Push Notifications', href: '/settings/push-notifications', icon: Bell },
                { name: 'Voice Calls', href: '/settings/twilio-voice', icon: Phone },
                { name: 'Support', href: '/support', icon: LifeBuoy },
                { name: 'Developers', href: '/developers', icon: FileText },
                { name: 'Sandbox', href: '/developers/sandbox', icon: FlaskConical },
                { name: 'System Escalations', href: '/settings/ai-approval', icon: AlertTriangle, badge: 'escalation' },
            ],
        },
    ];

    const adminNavigation: NavItem[] = [
        { name: 'Platform Admin', href: '/admin', icon: Shield },
        { name: 'Tenant Management', href: '/admin/tenants', icon: Building2 },
        { name: 'Subscriptions', href: '/admin/plans', icon: CreditCard },
        { name: 'System Health', href: '/admin/health', icon: Activity },
        { name: 'AI Oversight', href: '/admin/ai-oversight', icon: Bot },
        { name: 'Security', href: '/admin/security', icon: ShieldAlert },
        { name: 'Audit Logs', href: '/admin/logs', icon: ClipboardList },
        { name: 'Platform Revenue', href: '/admin/revenue', icon: TrendingUp },
        { name: 'Impersonate Tenant', href: '/admin/impersonate', icon: UserCog },
        { name: 'Pricing Admin', href: '/admin/pricing', icon: DollarSign },
        { name: 'User Management', href: '/admin/users', icon: Users },
        { name: 'DLQ Dashboard', href: '/admin/dlq', icon: AlertTriangle },
        { name: 'Escalations', href: '/admin/escalations', icon: AlertTriangle },
        { name: 'Admin Billing', href: '/admin/billing', icon: CreditCard },
        { name: 'Global Settings', href: '/admin/settings', icon: Settings },
        { name: 'Back to Dashboard', href: '/dashboard', icon: LayoutDashboard },
    ];

    const isAdminSpace = pathname?.includes('/admin');

    useEffect(() => {
        if (!isInitialized) checkAuth();
    }, [checkAuth, isInitialized]);

    const toggleTheme = () => {
        setTheme(resolvedTheme === 'dark' ? 'light' : 'dark');
    };

    if (!isInitialized) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-slate-50 dark:bg-slate-950">
                <div className="w-12 h-12 border-4 border-primary-500 border-t-transparent rounded-full animate-spin" />
            </div>
        );
    }

    return (
        <SignalRProvider>
            <div className="min-h-screen bg-[var(--background)] text-[var(--text-primary)] transition-colors duration-300">
                <DemoModeBanner />
                <a href="#main-content" className="skip-to-content sr-only focus:not-sr-only focus:absolute focus:top-4 focus:left-4 focus:z-[9999] focus:px-4 focus:py-2 focus:bg-primary-500 focus:text-white focus:rounded-lg focus:shadow-lg">
                    Skip to content
                </a>

                {/* Mobile sidebar overlay */}
                {sidebarOpen && (
                    <div
                        className="fixed inset-0 bg-slate-900/60 backdrop-blur-sm z-40 lg:hidden"
                        onClick={() => setSidebarOpen(false)}
                        aria-hidden="true"
                    />
                )}

                {/* Sidebar */}
                <aside
                    role="navigation"
                    aria-label="Main navigation"
                    className={cn(
                        'fixed top-0 start-0 z-50 h-full w-72 transform transition-all duration-300 lg:translate-x-0',
                        'bg-white border-e border-slate-200 dark:bg-slate-900 dark:border-white/5',
                        sidebarOpen ? 'translate-x-0' : 'ltr:-translate-x-full rtl:translate-x-full'
                    )}
                >
                    <div className="absolute inset-0 overflow-hidden pointer-events-none">
                        <div className="absolute top-0 left-1/2 -translate-x-1/2 w-96 h-96 bg-primary-500/10 rounded-full blur-3xl" />
                    </div>

                    <div className="relative h-full flex flex-col">
                        {/* Logo */}
                        <div className="flex items-center justify-between h-16 px-5 border-b border-slate-100 dark:border-white/5">
                            <Link href="/dashboard" className="flex items-center gap-2.5 group" aria-label="Upkilo — go to dashboard">
                                <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-primary-500 to-primary-600 flex items-center justify-center shadow-md shadow-primary-500/30 group-hover:shadow-primary-500/50 transition-shadow">
                                    <Sparkles className="w-4 h-4 text-white" />
                                </div>
                                <span className="text-xl font-bold text-slate-900 dark:text-white tracking-tight" style={{ fontFamily: 'var(--font-display)' }}>
                                    Upkilo
                                </span>
                            </Link>
                            <button
                                className="lg:hidden p-2 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-100 dark:hover:text-white dark:hover:bg-white/10 transition-colors"
                                onClick={() => setSidebarOpen(false)}
                                aria-label="Close navigation menu"
                            >
                                <X className="h-5 w-5" />
                            </button>
                        </div>

                        {/* Navigation */}
                        <nav className="flex-1 overflow-y-auto py-4 px-3 scrollbar-thin" aria-label="Sidebar navigation">
                            {isAdminSpace ? (
                                <div className="space-y-0.5">
                                    {adminNavigation.map((item) => {
                                        const isActive = pathname === item.href;
                                        return (
                                            <Link
                                                key={item.name}
                                                href={item.href}
                                                aria-current={isActive ? 'page' : undefined}
                                                className={cn(
                                                    'group flex items-center gap-3 px-4 py-2.5 rounded-xl transition-all duration-200 relative',
                                                    isActive
                                                        ? 'bg-primary-50 text-primary-600 dark:bg-slate-800 dark:text-primary-400 shadow-sm'
                                                        : 'text-slate-500 hover:text-slate-900 hover:bg-slate-50 dark:text-slate-400 dark:hover:text-white dark:hover:bg-white/5'
                                                )}
                                            >
                                                {isActive && <div className="absolute left-0 top-1/2 -translate-y-1/2 w-1 h-7 bg-primary-500 rounded-r-full" />}
                                                <item.icon className={cn('h-4 w-4 flex-shrink-0', isActive ? 'text-primary-400' : 'text-slate-400 group-hover:text-slate-600 dark:group-hover:text-slate-300')} />
                                                <span className="font-medium text-sm">{item.name}</span>
                                            </Link>
                                        );
                                    })}
                                </div>
                            ) : (
                                <div className="space-y-2">
                                    {user?.role === 'superadmin' && (
                                        <Link
                                            href="/admin"
                                            className="flex items-center gap-3 px-4 py-2.5 rounded-xl text-sm font-medium text-slate-500 hover:text-slate-900 hover:bg-slate-50 dark:text-slate-400 dark:hover:text-white dark:hover:bg-white/5 transition-colors"
                                        >
                                            <Shield className="h-4 w-4 text-slate-400" />
                                            Platform Admin
                                        </Link>
                                    )}
                                    {navGroups.map((group, idx) => (
                                        <NavGroupSection
                                            key={group.label}
                                            group={group}
                                            pathname={pathname}
                                            defaultOpen={idx < 2}
                                        />
                                    ))}
                                </div>
                            )}
                        </nav>

                        {/* User section */}
                        <div className="p-3 border-t border-slate-100 dark:border-white/5">
                            <div className="flex items-center gap-3 p-3 rounded-xl bg-slate-50 dark:bg-white/5 mb-2">
                                <div className="w-8 h-8 rounded-lg bg-gradient-to-br from-violet-400 to-violet-600 flex items-center justify-center shadow-md flex-shrink-0">
                                    <span className="text-white font-semibold text-xs">
                                        {user?.firstName?.[0] || 'U'}
                                    </span>
                                </div>
                                <div className="flex-1 min-w-0">
                                    <p className="font-medium text-slate-900 dark:text-white text-sm truncate">
                                        {user?.firstName || 'User'} {user?.lastName || ''}
                                    </p>
                                    <p className="text-xs text-slate-500 dark:text-slate-400 truncate">{user?.email || ''}</p>
                                </div>
                            </div>
                            <button
                                onClick={logout}
                                className="flex items-center gap-3 w-full px-4 py-2.5 text-slate-500 hover:text-red-600 hover:bg-red-50 dark:text-slate-400 dark:hover:text-red-400 dark:hover:bg-red-500/10 rounded-xl transition-colors group"
                                aria-label="Sign out of Upkilo"
                            >
                                <LogOut className="h-4 w-4 group-hover:text-red-600 dark:group-hover:text-red-400 transition-colors" />
                                <span className="font-medium text-sm">Sign out</span>
                            </button>
                        </div>
                    </div>
                </aside>

                {/* Main content area */}
                <div className="lg:ps-72">
                    {/* Top header */}
                    <header className="sticky top-0 z-30 bg-white/80 dark:bg-slate-900/80 backdrop-blur-lg border-b border-slate-200 dark:border-white/5" role="banner">
                        <div className="flex items-center justify-between h-16 px-4 lg:px-6">
                            <div className="flex items-center gap-3">
                                <button
                                    className="lg:hidden p-2 rounded-lg text-slate-500 hover:text-slate-900 hover:bg-slate-100 dark:hover:text-white dark:hover:bg-white/10 transition-colors"
                                    onClick={() => setSidebarOpen(true)}
                                    aria-label="Open navigation menu"
                                    aria-expanded={sidebarOpen}
                                    aria-controls="sidebar"
                                >
                                    <Menu className="h-5 w-5" />
                                </button>
                                {!isAdminSpace && (
                                    <div className="hidden md:block">
                                        <GlobalSearch />
                                    </div>
                                )}
                            </div>

                            <div className="flex items-center gap-2 lg:gap-3">
                                <LocaleSwitcher />

                                <button
                                    onClick={toggleTheme}
                                    className="p-2 rounded-lg text-slate-500 hover:text-slate-900 hover:bg-slate-100 dark:hover:text-white dark:hover:bg-white/10 transition-colors"
                                    aria-label={resolvedTheme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
                                >
                                    {resolvedTheme === 'dark' ? <Sun className="h-5 w-5" aria-hidden="true" /> : <Moon className="h-5 w-5" aria-hidden="true" />}
                                </button>

                                {!isAdminSpace && (
                                    <button
                                        onClick={() => setCopilotOpen((o) => !o)}
                                        className={cn(
                                            'p-2 rounded-lg transition-colors flex items-center gap-1.5 text-sm font-medium',
                                            copilotOpen
                                                ? 'bg-indigo-100 text-indigo-700 dark:bg-indigo-900/40 dark:text-indigo-300'
                                                : 'text-slate-500 hover:text-slate-900 hover:bg-slate-100 dark:hover:text-white dark:hover:bg-white/10'
                                        )}
                                        aria-label={copilotOpen ? 'Close AI Copilot' : 'Open AI Copilot'}
                                        aria-expanded={copilotOpen}
                                    >
                                        <Bot className="h-5 w-5" aria-hidden="true" />
                                        <span className="hidden lg:inline">Copilot</span>
                                    </button>
                                )}

                                <NotificationCenter />

                                {!isAdminSpace && (
                                    <Link
                                        href="/bookings/new"
                                        className="btn btn-primary shadow-md shadow-primary-500/25 hover:shadow-primary-500/40 transition-all text-sm"
                                        aria-label="Create new booking"
                                    >
                                        <span className="hidden sm:inline">New Booking</span>
                                        <span className="sm:hidden" aria-hidden="true">+</span>
                                    </Link>
                                )}
                            </div>
                        </div>
                    </header>

                    <main id="main-content" tabIndex={-1} className="p-4 lg:p-8 min-h-[calc(100vh-4rem)] focus:outline-none">
                        <OnboardingWizard />
                        <ProductTour />
                        {children}
                    </main>

                    <footer className="px-6 py-5 border-t border-slate-200 dark:border-white/5 bg-white dark:bg-slate-900/50" role="contentinfo">
                        <div className="flex flex-col sm:flex-row items-center justify-between gap-2 text-xs text-slate-500 dark:text-slate-400">
                            <p>© 2026 Upkilo. All rights reserved.</p>
                            <nav aria-label="Footer links" className="flex items-center gap-4">
                                <a href="/help" className="hover:text-primary-500 transition-colors">Help</a>
                                <a href="/privacy-policy" className="hover:text-primary-500 transition-colors">Privacy</a>
                                <a href="/terms-of-service" className="hover:text-primary-500 transition-colors">Terms</a>
                                <a href="/cookie-policy" className="hover:text-primary-500 transition-colors">Cookies</a>
                                <ManageCookiesButton className="hover:text-primary-500 transition-colors bg-transparent border-0 p-0 text-xs text-slate-500 dark:text-slate-400 cursor-pointer">Manage Consent</ManageCookiesButton>
                            </nav>
                        </div>
                    </footer>
                </div>

                {copilotOpen && (
                    <div
                        className="fixed inset-0 z-30 bg-slate-900/20 backdrop-blur-[1px]"
                        onClick={() => setCopilotOpen(false)}
                        aria-hidden="true"
                    />
                )}
                <AICopilotRail
                    isOpen={copilotOpen}
                    onClose={() => setCopilotOpen(false)}
                    contextHint={pathname?.split('/').filter(Boolean).at(-1) ?? undefined}
                />
            </div>
        </SignalRProvider>
    );
}

// Add missing Monitor import alias
const Monitor = Database;
