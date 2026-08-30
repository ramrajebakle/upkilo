'use client';

import { useState, useEffect } from 'react';
import {
    TrendingUp, Globe, Link2, Copy, CheckCircle, ArrowRight,
    Star, Zap, Users, Megaphone, Award, BarChart3, Search,
    Smartphone, Share2, ExternalLink, ChevronRight, Sparkles,
    Target, MessageSquare, Gift, FileText, Check, Clock,
    AlertCircle, Rocket, BookOpen, Code2
} from 'lucide-react';
import Link from 'next/link';
import { cn } from '@/lib/utils';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';

const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';

interface Business {
    name: string;
    subdomain: string; // API returns tenant.Slug as "subdomain"
    logo?: string;
    description?: string;
    phone?: string;
    email?: string;
    website?: string;
    address?: { line1?: string; city?: string };
}

interface Step {
    id: number;
    title: string;
    description: string;
    icon: React.ElementType;
    color: string;
    href: string;
    linkLabel: string;
    impact: string;
    done?: boolean;
    tip: string;
}

export default function GrowthPage() {
    const [business, setBusiness] = useState<Business | null>(null);
    const [loading, setLoading] = useState(true);
    const [copied, setCopied] = useState<string | null>(null);
    const { success } = useToast();

    useEffect(() => {
        api.settings.getBusiness()
            .then((res: any) => setBusiness(res.data))
            .catch(() => {})
            .finally(() => setLoading(false));
    }, []);

    const bookingUrl = business?.subdomain
        ? `${SITE_URL}/en/book/${business.subdomain}`
        : null;

    const widgetCode = bookingUrl
        ? `<iframe\n  src="${bookingUrl}"\n  width="100%"\n  height="700"\n  frameborder="0"\n  style="border-radius:16px;box-shadow:0 4px 32px rgba(0,0,0,.1)"\n  title="Book an appointment"\n></iframe>`
        : null;

    const copyToClipboard = async (text: string, key: string) => {
        await navigator.clipboard.writeText(text);
        setCopied(key);
        success('Copied to clipboard!');
        setTimeout(() => setCopied(null), 2000);
    };

    // Determine which steps are done based on business data
    const profileDone = !!(business?.name && business?.phone && business?.email && business?.address?.city);
    const slugDone    = !!business?.subdomain;
    const descDone    = !!business?.description;

    const steps: Step[] = [
        {
            id: 1,
            title: 'Complete your business profile',
            description: 'Add your address, phone, email, logo, and a short description. This powers your Google Business listing and every SEO page Google generates for you.',
            icon: FileText,
            color: 'from-violet-500 to-purple-600',
            href: '/settings/business',
            linkLabel: 'Open Business Settings',
            impact: 'Local SEO · Google Maps · Rich Snippets',
            done: profileDone,
            tip: 'Businesses with complete profiles get 7× more clicks from Google.',
        },
        {
            id: 2,
            title: 'Set your booking page slug',
            description: 'Your slug is the unique URL that clients use to book with you, e.g. upkilo.com/en/book/acme-salon. Pick something short, branded, and easy to remember.',
            icon: Link2,
            color: 'from-cyan-500 to-blue-600',
            href: '/settings/business',
            linkLabel: 'Set Your Slug',
            impact: 'Public Booking Page · Sitemap · SEO Title',
            done: slugDone,
            tip: 'Your booking page is automatically indexed by Google once the slug is set.',
        },
        {
            id: 3,
            title: 'Write a keyword-rich description',
            description: 'Add a 2–3 sentence description of your business. It becomes the Google meta description for your booking page and is included in the JSON-LD schema Google reads.',
            icon: BookOpen,
            color: 'from-amber-500 to-orange-600',
            href: '/settings/business',
            linkLabel: 'Add Description',
            impact: 'Google Snippet · Schema Markup · Conversions',
            done: descDone,
            tip: 'Include your city and main service type, e.g. "Award-winning hair salon in Austin, TX".',
        },
        {
            id: 4,
            title: 'Share your booking link',
            description: 'Add the link to your Instagram bio, Facebook page, Google Business profile, and email signature. Every click is a potential lead captured directly into Upkilo.',
            icon: Share2,
            color: 'from-pink-500 to-rose-600',
            href: '/settings/business',
            linkLabel: 'Copy Your Booking Link',
            impact: 'Direct Traffic · Social Leads · Zero Commission',
            done: false,
            tip: 'Instagram bio link is the #1 source of new bookings for service businesses.',
        },
        {
            id: 5,
            title: 'Embed the booking widget on your website',
            description: 'Paste one line of HTML on your existing website and clients can book without ever leaving your site. No redirects, fully branded.',
            icon: Code2,
            color: 'from-teal-500 to-emerald-600',
            href: '/settings/business',
            linkLabel: 'Get Embed Code',
            impact: 'Website Conversions · Branded Experience',
            done: false,
            tip: 'Websites with an embedded booking widget convert 3× more visitors than "Contact Us" forms.',
        },
        {
            id: 6,
            title: 'Create SEO landing pages',
            description: 'Build dedicated pages for each service: "Hair Colour Austin", "Deep Tissue Massage NYC". Each page gets its own title, description, and schema — more pages = more Google rankings.',
            icon: Globe,
            color: 'from-blue-500 to-indigo-600',
            href: '/marketing/landing-pages',
            linkLabel: 'Create Landing Pages',
            impact: 'Organic Traffic · Service Keywords · Conversions',
            done: false,
            tip: 'Businesses with 5+ landing pages generate 55% more leads than those with 1.',
        },
        {
            id: 7,
            title: 'Set up automated review collection',
            description: 'Reviews are the #1 ranking factor for local SEO. Set up an automation that sends a review request via SMS after every completed booking.',
            icon: Star,
            color: 'from-yellow-400 to-amber-500',
            href: '/automation/workflows',
            linkLabel: 'Create Review Workflow',
            impact: 'Google Rating · Local SEO · Trust Signals',
            done: false,
            tip: 'Businesses with 50+ Google reviews appear 3× more often in local search results.',
        },
        {
            id: 8,
            title: 'Run your first campaign',
            description: 'Send a re-engagement SMS to clients who haven\'t booked in 60 days. On average, every 100 messages recover 8–12 bookings.',
            icon: Megaphone,
            color: 'from-violet-500 to-indigo-600',
            href: '/marketing/campaigns',
            linkLabel: 'Create Campaign',
            impact: 'Re-engagement · Revenue Recovery',
            done: false,
            tip: 'The best time to send is Tuesday–Thursday between 10 AM and 12 PM.',
        },
        {
            id: 9,
            title: 'Launch a loyalty program',
            description: 'Clients who earn points book 2.4× more often. Set up a points-per-visit program in minutes — it also gives you data to personalise future campaigns.',
            icon: Award,
            color: 'from-emerald-500 to-green-600',
            href: '/loyalty',
            linkLabel: 'Set Up Loyalty',
            impact: 'Retention · Repeat Bookings · LTV',
            done: false,
            tip: 'Retained clients spend 67% more than new ones and cost 5× less to serve.',
        },
        {
            id: 10,
            title: 'Set up referral automation',
            description: 'After a booking is completed, automatically send an SMS with a referral link. For every new client they bring, reward them with bonus loyalty points.',
            icon: Users,
            color: 'from-orange-500 to-red-500',
            href: '/automation/workflows',
            linkLabel: 'Create Referral Workflow',
            impact: 'Word-of-mouth · Free Leads · Client Growth',
            done: false,
            tip: 'Referred clients have a 37% higher retention rate than cold-acquired ones.',
        },
    ];

    const doneCount = steps.filter(s => s.done).length;
    const pct = Math.round((doneCount / steps.length) * 100);

    return (
        <div className="space-y-8 max-w-5xl">
            {/* Header */}
            <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
                <div>
                    <div className="flex items-center gap-3 mb-2">
                        <div className="p-2.5 bg-gradient-to-br from-emerald-500 to-teal-600 rounded-xl shadow-lg shadow-emerald-500/25">
                            <Rocket className="h-5 w-5 text-white" />
                        </div>
                        <h1 className="text-2xl lg:text-3xl font-bold text-slate-900 dark:text-white" style={{ fontFamily: 'var(--font-display)' }}>
                            Growth & SEO
                        </h1>
                    </div>
                    <p className="text-slate-500 dark:text-slate-400">
                        Step-by-step guide to get more leads and rank on Google — built for your booking page.
                    </p>
                </div>
            </div>

            {/* Progress bar */}
            <div className="card-elevated p-6">
                <div className="flex items-center justify-between mb-3">
                    <div>
                        <p className="font-semibold text-slate-900 dark:text-white text-sm">Setup progress</p>
                        <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
                            {doneCount} of {steps.length} steps complete
                        </p>
                    </div>
                    <span className="text-2xl font-bold text-success-fg">{pct}%</span>
                </div>
                <div className="w-full h-2.5 bg-slate-100 dark:bg-slate-700 rounded-full overflow-hidden">
                    <div
                        className="h-full bg-gradient-to-r from-emerald-400 to-teal-500 rounded-full transition-all duration-700"
                        style={{ width: `${pct}%` }}
                    />
                </div>
                {pct === 100 && (
                    <p className="mt-3 text-sm text-emerald-600 dark:text-emerald-400 font-medium flex items-center gap-1.5">
                        <CheckCircle className="h-4 w-4" /> All steps complete — your growth engine is live!
                    </p>
                )}
            </div>

            {/* Booking link banner */}
            {bookingUrl && (
                <div className="card-elevated p-5 border border-emerald-200 dark:border-emerald-800/40 bg-gradient-to-br from-emerald-50/50 to-teal-50/50 dark:from-emerald-900/10 dark:to-teal-900/10">
                    <div className="flex flex-col sm:flex-row sm:items-center gap-4">
                        <div className="flex-1">
                            <p className="text-xs font-semibold text-emerald-600 dark:text-emerald-400 uppercase tracking-wider mb-1">
                                Your Public Booking Page
                            </p>
                            <p className="font-mono text-sm text-slate-700 dark:text-slate-300 break-all">{bookingUrl}</p>
                        </div>
                        <div className="flex gap-2 shrink-0">
                            <button
                                onClick={() => copyToClipboard(bookingUrl, 'link')}
                                className="flex items-center gap-2 px-4 py-2 bg-emerald-500 hover:bg-emerald-600 text-white rounded-xl text-sm font-medium transition-colors shadow-sm"
                            >
                                {copied === 'link' ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                                {copied === 'link' ? 'Copied!' : 'Copy Link'}
                            </button>
                            <a
                                href={bookingUrl}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="flex items-center gap-2 px-4 py-2 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm font-medium text-slate-700 dark:text-slate-300 hover:border-emerald-400 transition-colors"
                            >
                                <ExternalLink className="h-4 w-4" />
                                Preview
                            </a>
                        </div>
                    </div>
                </div>
            )}

            {/* Widget embed section */}
            {widgetCode && (
                <div className="card-elevated p-5">
                    <div className="flex items-center justify-between mb-3">
                        <div>
                            <p className="font-semibold text-slate-900 dark:text-white text-sm">Website embed code</p>
                            <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">Paste this into any website to embed your booking form</p>
                        </div>
                        <button
                            onClick={() => copyToClipboard(widgetCode, 'widget')}
                            className="flex items-center gap-2 px-4 py-2 bg-slate-900 dark:bg-slate-700 text-white rounded-xl text-sm font-medium hover:bg-slate-700 dark:hover:bg-slate-600 transition-colors"
                        >
                            {copied === 'widget' ? <Check className="h-4 w-4" /> : <Code2 className="h-4 w-4" />}
                            {copied === 'widget' ? 'Copied!' : 'Copy Code'}
                        </button>
                    </div>
                    <pre className="bg-slate-950 text-emerald-400 text-xs rounded-xl p-4 overflow-x-auto font-mono leading-relaxed">
                        {widgetCode}
                    </pre>
                </div>
            )}

            {/* Steps */}
            <div className="space-y-4">
                <h2 className="text-lg font-semibold text-slate-900 dark:text-white">Your 10-step growth plan</h2>

                {steps.map((step, i) => {
                    const Icon = step.icon;
                    return (
                        <div
                            key={step.id}
                            className={cn(
                                'card-elevated p-5 flex flex-col sm:flex-row sm:items-start gap-4 transition-all',
                                step.done && 'opacity-75'
                            )}
                        >
                            {/* Step number / done indicator */}
                            <div className="shrink-0 flex flex-col items-center gap-1">
                                <div className={cn(
                                    'w-12 h-12 rounded-xl flex items-center justify-center shadow-sm',
                                    step.done
                                        ? 'bg-emerald-100 dark:bg-emerald-900/30'
                                        : `bg-gradient-to-br ${step.color}`
                                )}>
                                    {step.done
                                        ? <CheckCircle className="h-6 w-6 text-success-fg" />
                                        : <Icon className="h-5 w-5 text-white" />
                                    }
                                </div>
                                <span className="text-xs font-bold text-foreground-muted">#{step.id}</span>
                            </div>

                            {/* Content */}
                            <div className="flex-1 min-w-0">
                                <div className="flex flex-wrap items-start justify-between gap-2 mb-1.5">
                                    <h3 className={cn(
                                        'font-semibold text-sm',
                                        step.done
                                            ? 'line-through text-foreground-muted'
                                            : 'text-slate-900 dark:text-white'
                                    )}>
                                        {step.title}
                                    </h3>
                                    {step.done && (
                                        <span className="inline-flex items-center gap-1 px-2 py-0.5 bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400 text-xs font-medium rounded-full">
                                            <Check className="h-3 w-3" /> Done
                                        </span>
                                    )}
                                </div>

                                <p className="text-sm text-slate-500 dark:text-slate-400 mb-3 leading-relaxed">
                                    {step.description}
                                </p>

                                {/* Impact tags */}
                                <div className="flex flex-wrap gap-1.5 mb-3">
                                    {step.impact.split(' · ').map(tag => (
                                        <span
                                            key={tag}
                                            className="px-2 py-0.5 text-[11px] font-medium bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 rounded-full"
                                        >
                                            {tag}
                                        </span>
                                    ))}
                                </div>

                                {/* Pro tip */}
                                <div className="flex items-start gap-2 p-2.5 bg-amber-50 dark:bg-amber-900/10 border border-amber-100 dark:border-amber-800/30 rounded-lg mb-3">
                                    <Sparkles className="h-3.5 w-3.5 text-warning-fg shrink-0 mt-0.5" />
                                    <p className="text-xs text-amber-700 dark:text-amber-400">{step.tip}</p>
                                </div>

                                {/* CTA */}
                                {!step.done && (
                                    <Link
                                        href={step.href}
                                        className={cn(
                                            'inline-flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-medium text-white shadow-sm transition-all hover:-translate-y-0.5',
                                            `bg-gradient-to-r ${step.color}`
                                        )}
                                    >
                                        {step.linkLabel}
                                        <ArrowRight className="h-4 w-4" />
                                    </Link>
                                )}
                            </div>
                        </div>
                    );
                })}
            </div>

            {/* SEO status card */}
            <div className="card-elevated p-6">
                <div className="flex items-center gap-3 mb-5">
                    <div className="p-2 bg-gradient-to-br from-blue-500 to-primary-600 rounded-xl">
                        <Search className="h-5 w-5 text-white" />
                    </div>
                    <div>
                        <h3 className="font-semibold text-slate-900 dark:text-white">What Google sees on your booking page</h3>
                        <p className="text-xs text-slate-500 dark:text-slate-400">Auto-generated from your business profile</p>
                    </div>
                </div>

                <div className="space-y-3">
                    {/* Google preview */}
                    <div className="p-4 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl">
                        <p className="text-xs text-foreground-muted mb-2 uppercase tracking-wider">Google Search Preview</p>
                        <p className="text-blue-600 dark:text-blue-400 text-base font-medium hover:underline cursor-pointer">
                            Book {business?.name || 'Your Business'} — Online Booking
                        </p>
                        <p className="text-green-700 dark:text-green-500 text-xs mt-0.5">
                            {bookingUrl || 'upkilo.com/en/book/your-slug'}
                        </p>
                        <p className="text-slate-600 dark:text-slate-400 text-sm mt-1 leading-relaxed">
                            {business?.description
                                ? business.description.slice(0, 155) + (business.description.length > 155 ? '…' : '')
                                : 'Book appointments online with ' + (business?.name || 'this business') + '. Fast, easy, and instant confirmation.'}
                        </p>
                    </div>

                    {/* Schema checklist */}
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                        {[
                            { label: 'Page title',        done: true },
                            { label: 'Meta description',  done: true },
                            { label: 'Canonical URL',     done: true },
                            { label: 'OpenGraph tags',    done: true },
                            { label: 'JSON-LD schema',    done: true },
                            { label: 'ReserveAction',     done: true },
                            { label: 'Business address',  done: !!business?.address?.city },
                            { label: 'Business phone',    done: !!business?.phone },
                            { label: 'Sitemap entry',     done: slugDone },
                            { label: 'robots.txt',        done: true },
                        ].map(item => (
                            <div
                                key={item.label}
                                className={cn(
                                    'flex items-center gap-2 px-3 py-2 rounded-lg text-sm',
                                    item.done
                                        ? 'bg-emerald-50 dark:bg-emerald-900/15 text-emerald-700 dark:text-emerald-400'
                                        : 'bg-amber-50 dark:bg-amber-900/15 text-amber-700 dark:text-amber-400'
                                )}
                            >
                                {item.done
                                    ? <CheckCircle className="h-4 w-4 shrink-0" />
                                    : <AlertCircle className="h-4 w-4 shrink-0" />
                                }
                                {item.label}
                            </div>
                        ))}
                    </div>
                </div>
            </div>

            {/* Quick links */}
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                {[
                    { label: 'Campaigns',      href: '/marketing/campaigns',     icon: Megaphone,    color: 'text-primary-500' },
                    { label: 'Landing Pages',  href: '/marketing/landing-pages', icon: Globe,        color: 'text-blue-500' },
                    { label: 'Loyalty',        href: '/loyalty',                 icon: Award,        color: 'text-success-fg' },
                    { label: 'Analytics',      href: '/analytics',               icon: BarChart3,    color: 'text-warning-fg' },
                ].map(item => {
                    const Icon = item.icon;
                    return (
                        <Link
                            key={item.href}
                            href={item.href}
                            className="card-elevated p-4 flex flex-col items-center gap-2 text-center hover:-translate-y-0.5 transition-transform group"
                        >
                            <Icon className={cn('h-6 w-6', item.color)} />
                            <span className="text-xs font-medium text-slate-700 dark:text-slate-300 group-hover:text-slate-900 dark:group-hover:text-white transition-colors">
                                {item.label}
                            </span>
                        </Link>
                    );
                })}
            </div>
        </div>
    );
}
