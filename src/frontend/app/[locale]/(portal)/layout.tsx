'use client';

import React from 'react';
import { Link, usePathname } from '@/navigation';
import { useEffect, useState } from 'react';
import {
    Calendar,
    User,
    ChevronLeft,
    LogOut,
    Menu,
    X,
    Bell,
    Zap
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';

interface NavItem {
    label: string;
    href: string;
    icon: any;
}

const navItems: NavItem[] = [
    { label: 'Overview', href: '/portal-dashboard', icon: Zap },
    { label: 'My Bookings', href: '/portal-bookings', icon: Calendar },
    { label: 'My Profile', href: '/profile', icon: User },
];

function PoweredByUpkiloBadge() {
    const [showBadge, setShowBadge] = useState<boolean | null>(null);

    useEffect(() => {
        // Check if the tenant has white_label feature enabled.
        // If the request fails or white_label is disabled, show the badge.
        fetch('/api/v1/whitelabel')
            .then((res) => {
                // 403 = no white_label feature on this plan → show badge
                // 200 = white_label enabled → hide badge
                setShowBadge(res.status === 403 || !res.ok);
            })
            .catch(() => setShowBadge(true));
    }, []);

    if (!showBadge) return null;

    return (
        <div className="flex items-center justify-center gap-1.5 text-xs text-slate-400 mt-2">
            <Zap className="h-3 w-3 text-primary-400" />
            <span>Powered by</span>
            <a
                href="https://upkilo.com"
                target="_blank"
                rel="noopener noreferrer"
                className="font-semibold text-primary-500 hover:text-primary-600 transition-colors"
                title="Grow your service business with Upkilo"
            >
                Upkilo
            </a>
        </div>
    );
}

export default function PortalLayout({ children }: { children: React.ReactNode }) {
    const pathname = usePathname();
    const [isMobileMenuOpen, setIsMobileMenuOpen] = React.useState(false);

    // usePathname() from '@/navigation' is locale-stripped, so these compare
    // against the real route names rather than a /portal/* prefix that the route
    // group never produced.
    const isGuestPage = pathname === '/portal-login' || pathname === '/verify';

    if (isGuestPage) {
        return <div className="min-h-screen bg-slate-50">{children}</div>;
    }

    return (
        <div className="min-h-screen bg-slate-50 flex flex-col">
            {/* Header */}
            <header className="bg-white border-b sticky top-0 z-50">
                <div className="max-w-5xl mx-auto px-4 h-16 flex items-center justify-between">
                    <div className="flex items-center gap-4">
                        <Link href="/portal-dashboard" className="flex items-center gap-2">
                            <div className="p-1.5 bg-primary rounded-lg">
                                <ChevronLeft className="h-5 w-5 text-white" />
                            </div>
                            <span className="font-bold text-slate-900 hidden sm:inline-block">Client Portal</span>
                        </Link>
                    </div>

                    {/* Desktop Nav */}
                    <nav className="hidden md:flex items-center gap-1">
                        {navItems.map((item) => (
                            <Link
                                key={item.href}
                                href={item.href}
                                className={cn(
                                    "px-4 py-2 rounded-lg text-sm font-medium transition-colors flex items-center gap-2",
                                    pathname === item.href
                                        ? "bg-primary-50 text-primary-600"
                                        : "text-slate-600 hover:bg-slate-50 hover:text-slate-900"
                                )}
                            >
                                <item.icon className="h-4 w-4" />
                                {item.label}
                            </Link>
                        ))}
                    </nav>

                    <div className="flex items-center gap-2">
                        <Button variant="ghost" size="icon" className="relative">
                            <Bell className="h-5 w-5 text-slate-500" />
                            <span className="absolute top-2 right-2 w-2 h-2 bg-red-500 rounded-full border-2 border-white" />
                        </Button>
                        <Button variant="ghost" size="icon" className="md:hidden" onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}>
                            {isMobileMenuOpen ? <X className="h-6 w-6" /> : <Menu className="h-6 w-6" />}
                        </Button>
                        <Button variant="ghost" size="sm" className="hidden md:flex items-center gap-2 text-slate-600">
                            <LogOut className="h-4 w-4" />
                            Sign Out
                        </Button>
                    </div>
                </div>

                {/* Mobile Nav */}
                <div className={cn(
                    "md:hidden absolute top-16 left-0 right-0 bg-white border-b shadow-lg transition-all duration-300 overflow-hidden",
                    isMobileMenuOpen ? "max-h-64 opacity-100" : "max-h-0 opacity-0"
                )}>
                    <div className="px-4 py-4 space-y-2">
                        {navItems.map((item) => (
                            <Link
                                key={item.href}
                                href={item.href}
                                onClick={() => setIsMobileMenuOpen(false)}
                                className={cn(
                                    "flex items-center gap-3 px-4 py-3 rounded-xl text-base font-medium transition-all",
                                    pathname === item.href
                                        ? "bg-primary text-white shadow-lg shadow-primary/20"
                                        : "text-slate-600 hover:bg-slate-50"
                                )}
                            >
                                <item.icon className="h-5 w-5" />
                                {item.label}
                            </Link>
                        ))}
                        <button className="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-base font-medium text-red-500 hover:bg-red-50">
                            <LogOut className="h-5 w-5" />
                            Sign Out
                        </button>
                    </div>
                </div>
            </header>

            {/* Main Content */}
            <main className="flex-1 w-full max-w-5xl mx-auto px-4 py-8">
                <div className="animate-fade-in">
                    {children}
                </div>
            </main>

            {/* Simple Footer */}
            <footer className="bg-white border-t py-8 mt-auto">
                <div className="max-w-5xl mx-auto px-4 text-center">
                    <p className="text-slate-400 text-sm">
                        &copy; 2026 Upkilo. All rights reserved.
                    </p>
                    <PoweredByUpkiloBadge />
                </div>
            </footer>
        </div>
    );
}
