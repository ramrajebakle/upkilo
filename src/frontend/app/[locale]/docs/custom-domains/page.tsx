'use client';

import React from 'react';
import { 
    Globe, Server, ShieldCheck, ChevronRight, 
    ArrowLeft, ExternalLink, HelpCircle, AlertTriangle,
    CheckCircle2, Info
} from 'lucide-react';
import Link from 'next/link';
import { useParams } from 'next/navigation';

export default function CustomDomainsGuidePage() {
    const params = useParams();
    const locale = params.locale as string || 'en';

    return (
        <div className="min-h-screen bg-slate-50 dark:bg-slate-950 transition-colors duration-300">
            {/* Navigation Header */}
            <div className="bg-white/80 dark:bg-slate-900/80 backdrop-blur-md sticky top-0 z-50 border-b border-slate-200 dark:border-slate-800">
                <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex items-center justify-between">
                    <Link 
                        href={`/${locale}/settings/domains`}
                        className="flex items-center gap-2 text-sm font-medium text-slate-500 hover:text-primary-600 dark:text-slate-400 dark:hover:text-primary-400 transition-colors"
                    >
                        <ArrowLeft className="h-4 w-4" />
                        Back to Settings
                    </Link>
                    <div className="flex items-center gap-3">
                        <div className="h-8 w-8 bg-primary-100 dark:bg-primary-900/30 rounded-lg flex items-center justify-center">
                            <Globe className="h-5 w-5 text-primary-600 dark:text-primary-400" />
                        </div>
                        <span className="font-bold text-slate-900 dark:text-white">Upkilo Help Center</span>
                    </div>
                </div>
            </div>

            <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
                {/* Hero Section */}
                <div className="mb-12">
                    <h1 className="text-4xl font-extrabold text-slate-900 dark:text-white mb-4 tracking-tight" style={{ fontFamily: 'Outfit, sans-serif' }}>
                        Custom Domains Guide
                    </h1>
                    <p className="text-lg text-slate-600 dark:text-slate-400 max-w-2xl leading-relaxed">
                        Learn how to point your own domain or subdomain to your Upkilo booking page and set up professional email sending.
                    </p>
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-3 gap-12">
                    {/* Main Content */}
                    <div className="lg:col-span-2 space-y-12">
                        {/* Phase 1: Main Domain Setup */}
                        <section id="cname-setup" className="space-y-6">
                            <div className="flex items-center gap-3 text-2xl font-bold text-slate-900 dark:text-white">
                                <div className="h-10 w-10 bg-blue-100 dark:bg-blue-900/30 rounded-full flex items-center justify-center text-blue-600 dark:text-blue-400 text-sm">1</div>
                                <h2>Setup CNAME Record</h2>
                            </div>
                            <p className="text-slate-600 dark:text-slate-400">
                                To use a custom subdomain (like <code className="px-1.5 py-0.5 bg-slate-100 dark:bg-slate-800 rounded text-primary-600">booking.yourbrand.com</code>), you need to add a CNAME record in your DNS provider's settings.
                            </p>
                            
                            <div className="p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 border-l-4 border-l-primary-500 shadow-sm space-y-4">
                                <div className="grid grid-cols-3 gap-4 text-xs font-bold text-slate-500 dark:text-slate-500 uppercase tracking-wider">
                                    <div>Type</div>
                                    <div>Name (Host)</div>
                                    <div>Value (Points to)</div>
                                </div>
                                <div className="grid grid-cols-3 gap-4 font-mono text-sm text-slate-900 dark:text-slate-300">
                                    <div className="p-2 bg-slate-50 dark:bg-slate-800 rounded">CNAME</div>
                                    <div className="p-2 bg-slate-50 dark:bg-slate-800 rounded">booking</div>
                                    <div className="p-2 bg-slate-50 dark:bg-slate-800 rounded">proxy.upkilo.com</div>
                                </div>
                            </div>
                        </section>

                        {/* Phase 2: Provider Guides */}
                        <section id="providers" className="space-y-8">
                            <h2 className="text-2xl font-bold text-slate-900 dark:text-white">Provider Specific Steps</h2>
                            
                            <div className="space-y-4">
                                {/* GoDaddy */}
                                <div className="p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 hover:border-primary-500/50 transition-colors group">
                                    <div className="flex items-center justify-between mb-4">
                                        <h3 className="text-lg font-bold text-slate-900 dark:text-white flex items-center gap-2">
                                            GoDaddy
                                        </h3>
                                        <ExternalLink className="h-4 w-4 text-slate-400 group-hover:text-primary-500 transition-colors" />
                                    </div>
                                    <ol className="list-decimal pl-5 space-y-2 text-sm text-slate-600 dark:text-slate-400">
                                        <li>Log in to your <strong>GoDaddy Domain Portfolio</strong>.</li>
                                        <li>Select your domain to access the <strong>Domain Settings</strong> page.</li>
                                        <li>Select <strong>DNS</strong> to view your resource records.</li>
                                        <li>Select <strong>Add</strong> and then choose <strong>CNAME</strong> from the Type menu.</li>
                                        <li>Enter <code className="text-primary-600">booking</code> (or your choice) in the <strong>Name</strong> field.</li>
                                        <li>Enter <code className="text-primary-600">proxy.upkilo.com</code> in the <strong>Value</strong> field.</li>
                                        <li>Choose <strong>Save</strong>.</li>
                                    </ol>
                                </div>

                                {/* Cloudflare */}
                                <div className="p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 hover:border-primary-500/50 transition-colors group">
                                    <div className="flex items-center justify-between mb-4">
                                        <h3 className="text-lg font-bold text-slate-900 dark:text-white flex items-center gap-2">
                                            Cloudflare
                                        </h3>
                                        <ExternalLink className="h-4 w-4 text-slate-400 group-hover:text-primary-500 transition-colors" />
                                    </div>
                                    <ol className="list-decimal pl-5 space-y-2 text-sm text-slate-600 dark:text-slate-400">
                                        <li>Log in to your <strong>Cloudflare dashboard</strong>.</li>
                                        <li>Select your website/domain.</li>
                                        <li>Go to the <strong>DNS</strong> section.</li>
                                        <li>Click <strong>+ Add record</strong>.</li>
                                        <li>Set Type to <strong>CNAME</strong>.</li>
                                        <li>Set Name to <code className="text-primary-600">booking</code>.</li>
                                        <li>Set Target to <code className="text-primary-600">proxy.upkilo.com</code>.</li>
                                        <li><strong>Proxy Status:</strong> Ensure it is set to <strong>DNS Only</strong> (Grey cloud).</li>
                                        <li>Click <strong>Save</strong>.</li>
                                    </ol>
                                </div>

                                {/* Namecheap */}
                                <div className="p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 hover:border-primary-500/50 transition-colors group">
                                    <div className="flex items-center justify-between mb-4">
                                        <h3 className="text-lg font-bold text-slate-900 dark:text-white flex items-center gap-2">
                                            Namecheap
                                        </h3>
                                        <ExternalLink className="h-4 w-4 text-slate-400 group-hover:text-primary-500 transition-colors" />
                                    </div>
                                    <ol className="list-decimal pl-5 space-y-2 text-sm text-slate-600 dark:text-slate-400">
                                        <li>Sign in to your <strong>Namecheap account</strong>.</li>
                                        <li>Select <strong>Domain List</strong> from the left sidebar.</li>
                                        <li>Click <strong>Manage</strong> next to your domain.</li>
                                        <li>Navigate to the <strong>Advanced DNS</strong> tab.</li>
                                        <li>Click <strong>+ Add New Record</strong>.</li>
                                        <li>Select <strong>CNAME Record</strong> as the Type.</li>
                                        <li>Host: <code className="text-primary-600">booking</code></li>
                                        <li>Value: <code className="text-primary-600">proxy.upkilo.com</code></li>
                                        <li>Click the <strong>Checkmark</strong> to save.</li>
                                    </ol>
                                </div>
                            </div>
                        </section>

                        {/* Phase 3: Email Setup */}
                        <section id="email-setup" className="space-y-6 pt-6 border-t border-slate-200 dark:border-slate-800">
                            <div className="flex items-center gap-3 text-2xl font-bold text-slate-900 dark:text-white">
                                <div className="h-10 w-10 bg-emerald-100 dark:bg-emerald-900/30 rounded-full flex items-center justify-center text-emerald-600 dark:text-emerald-400 text-sm">2</div>
                                <h2>Email Authentication (SPF/DKIM)</h2>
                            </div>
                            <p className="text-slate-600 dark:text-slate-400">
                                To ensure your automated booking emails reach your clients' inboxes, you must verify your sending domain with SPF and DKIM records.
                            </p>
                            
                            <div className="space-y-4">
                                <div className="p-4 bg-amber-50 dark:bg-amber-900/20 border border-amber-100 dark:border-amber-800/50 rounded-xl flex gap-3 text-amber-900 dark:text-amber-200">
                                    <AlertTriangle className="h-5 w-5 shrink-0 mt-0.5" />
                                    <p className="text-sm">These records are mandatory for high deliverability. Without them, emails may be marked as spam.</p>
                                </div>
                                
                                <div className="space-y-4">
                                    <div className="p-6 bg-slate-50 dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800">
                                        <h4 className="text-sm font-bold text-slate-900 dark:text-white mb-2 uppercase tracking-wider">SPF Record</h4>
                                        <p className="text-xs text-slate-500 dark:text-slate-400 mb-4 font-mono">Type: TXT | Name: @ | Value: v=spf1 include:upkilo.com ~all</p>
                                    </div>
                                    <div className="p-6 bg-slate-50 dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800">
                                        <h4 className="text-sm font-bold text-slate-900 dark:text-white mb-2 uppercase tracking-wider">DKIM Record</h4>
                                        <p className="text-xs text-slate-500 dark:text-slate-400 mb-4 font-mono">Type: CNAME | Name: upkilo._domainkey | Value: dkim.upkilo.com</p>
                                    </div>
                                </div>
                            </div>
                        </section>
                    </div>

                    {/* Sidebar / Quick Nav */}
                    <div className="space-y-8">
                        <div className="p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm sticky top-24">
                            <h3 className="font-bold text-slate-900 dark:text-white mb-4">On this page</h3>
                            <nav className="space-y-3">
                                <a href="#cname-setup" className="block text-sm text-slate-600 dark:text-slate-400 hover:text-primary-600 transition-colors">Setup CNAME Record</a>
                                <a href="#providers" className="block text-sm text-slate-600 dark:text-slate-400 hover:text-primary-600 transition-colors">Provider Guides</a>
                                <a href="#email-setup" className="block text-sm text-slate-600 dark:text-slate-400 hover:text-primary-600 transition-colors">Email Authentication</a>
                            </nav>
                            
                            <hr className="my-6 border-slate-100 dark:border-slate-800" />
                            
                            <div className="space-y-4">
                                <h4 className="text-xs font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest">Help & Support</h4>
                                <div className="space-y-1">
                                    <Link href="#" className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-400 hover:text-primary-600 transition-colors">
                                        <Info className="h-4 w-4" />
                                        Contact Support
                                    </Link>
                                    <Link href="#" className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-400 hover:text-primary-600 transition-colors">
                                        <HelpCircle className="h-4 w-4" />
                                        Video Tutorials
                                    </Link>
                                </div>
                            </div>
                        </div>

                        <div className="p-6 bg-primary-600 rounded-2xl shadow-lg relative overflow-hidden group">
                            <div className="absolute top-0 right-0 p-4 opacity-10 transform translate-x-4 -translate-y-4 group-hover:scale-110 transition-transform">
                                <Globe className="h-24 w-24 text-white" />
                            </div>
                            <h3 className="text-white font-bold mb-2 relative z-10">Propagation Time</h3>
                            <p className="text-primary-100 text-sm relative z-10">
                                Most DNS changes take effect in minutes, but some can take up to 24-48 hours to fully propagate worldwide.
                            </p>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
