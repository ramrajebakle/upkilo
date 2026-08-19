'use client';

import { useState, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import {
    ArrowLeft,
    Download,
    Printer,
    Mail,
    Share2,
    CheckCircle2,
    Clock,
    DollarSign,
    CreditCard,
    Receipt,
    ExternalLink,
    AlertCircle,
    Building2,
} from 'lucide-react';
import { cn, formatCurrency, formatDate } from '@/lib/utils';
import api from '@/lib/api';
import { useToast } from '@/components/ui/Toast';

export default function InvoicePage() {
    const params = useParams();
    const id = params.id as string;
    const router = useRouter();
    const { success: toastSuccess, error: toastError } = useToast();
    
    const [loading, setLoading] = useState(true);
    const [invoice, setInvoice] = useState<any>(null);

    useEffect(() => {
        const fetchInvoice = async () => {
            if (!id) return;
            try {
                // Assuming we use the payment get endpoint for invoice data
                const res = await api.payments.get(id);
                setInvoice(res.data);
            } catch (error) {
                console.error('Failed to fetch invoice', error);
                toastError('Failed to load invoice');
                router.push('/payments');
            } finally {
                setLoading(false);
            }
        };
        fetchInvoice();
    }, [id, router, toastError]);

    const handlePrint = () => {
        window.print();
    };

    if (loading) {
        return (
            <div className="max-w-4xl mx-auto p-8 animate-pulse">
                <div className="h-8 w-48 bg-slate-100 rounded-lg mb-8" />
                <div className="bg-white rounded-3xl p-12 h-[800px] shadow-xl" />
            </div>
        );
    }

    if (!invoice) return null;

    return (
        <div className="max-w-5xl mx-auto pb-20 px-4 pt-4">
            {/* Action Bar */}
            <div className="flex flex-col sm:flex-row items-center justify-between gap-6 mb-10 print:hidden animate-fade-in-up">
                <Link
                    href="/payments"
                    className="flex items-center gap-2 text-slate-500 hover:text-primary-600 dark:text-slate-400 dark:hover:text-primary-400 font-bold uppercase tracking-widest text-[10px] transition-all group"
                >
                    <ArrowLeft className="h-4 w-4 transform transition-transform group-hover:-translate-x-1" />
                    Archive Directory
                </Link>
                <div className="flex items-center gap-3">
                    <button
                        onClick={handlePrint}
                        className="btn btn-secondary px-6 rounded-2xl dark:bg-slate-800 dark:border-slate-700 dark:text-slate-300 font-bold uppercase tracking-widest text-[10px] shadow-sm hover:translate-y-[-1px] transition-all"
                    >
                        <Printer className="h-4 w-4" />
                        Print
                    </button>
                    <button className="btn btn-secondary px-6 rounded-2xl dark:bg-slate-800 dark:border-slate-700 dark:text-slate-300 font-bold uppercase tracking-widest text-[10px] shadow-sm hover:translate-y-[-1px] transition-all">
                        <Download className="h-4 w-4" />
                        PDF
                    </button>
                    <button className="btn btn-primary px-6 rounded-2xl font-bold uppercase tracking-widest text-[10px] shadow-xl shadow-primary-500/20 hover:translate-y-[-1px] transition-all">
                        <Mail className="h-4 w-4" />
                        Dispatch
                    </button>
                </div>
            </div>

            {/* Invoice Document */}
            <div className="bg-white dark:bg-slate-900 rounded-[40px] shadow-2xl shadow-primary-500/10 dark:shadow-none overflow-hidden border border-slate-100 dark:border-slate-800 animate-fade-in-up relative">
                {/* Decorative Elements */}
                <div className="absolute top-0 right-0 w-96 h-96 bg-primary-500/5 dark:bg-primary-500/10 blur-[100px] -mr-48 -mt-48 pointer-events-none" />
                
                {/* Header Section */}
                <div className="p-10 sm:p-16 border-b border-slate-100 dark:border-slate-800/50 bg-slate-50/30 dark:bg-slate-950/20 relative">
                    <div className="flex flex-col sm:flex-row justify-between gap-12">
                        <div className="space-y-6">
                            <div className="flex items-center gap-4">
                                <div className="p-3.5 bg-primary-600 rounded-2xl shadow-2xl shadow-primary-600/30">
                                    <Building2 className="h-8 w-8 text-white" />
                                </div>
                                <div>
                                    <h1 className="text-3xl font-black text-slate-900 dark:text-white tracking-tighter" style={{ fontFamily: 'var(--font-display)' }}>
                                        UPKILO <span className="text-primary-600">SaaS</span>
                                    </h1>
                                    <p className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.4em]">Operations Unit</p>
                                </div>
                            </div>
                            <div className="text-xs font-bold text-slate-500 dark:text-slate-400 leading-relaxed uppercase tracking-widest bg-white dark:bg-slate-800/50 p-4 rounded-2xl border border-slate-100 dark:border-slate-800 shadow-sm inline-block">
                                <div className="flex items-center gap-2 mb-1">
                                    <div className="w-1.5 h-1.5 rounded-full bg-primary-500" />
                                    123 Business Avenue, Suite 400
                                </div>
                                <div className="flex items-center gap-2 mb-1">
                                    <div className="w-1.5 h-1.5 rounded-full bg-slate-300 dark:bg-slate-600" />
                                    San Francisco, CA 94107
                                </div>
                                <div className="flex items-center gap-2">
                                    <div className="w-1.5 h-1.5 rounded-full bg-slate-300 dark:bg-slate-600" />
                                    support@upkilo.com
                                </div>
                            </div>
                        </div>

                        <div className="text-right space-y-4">
                            <h2 className="text-7xl font-black text-slate-100 dark:text-slate-800/60 uppercase tracking-tighter leading-none" style={{ fontFamily: 'var(--font-display)' }}>
                                Receipt
                            </h2>
                            <div className="bg-white dark:bg-slate-800 inline-block px-6 py-4 rounded-3xl border border-slate-100 dark:border-slate-700 shadow-sm">
                                <p className="text-slate-400 dark:text-slate-500 text-[10px] uppercase font-black tracking-widest mb-1">System Identification</p>
                                <p className="text-slate-900 dark:text-white font-mono font-black text-lg tracking-tight">{id.toUpperCase()}</p>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Billing Info Section */}
                <div className="p-10 sm:p-16 grid grid-cols-1 sm:grid-cols-2 gap-16 relative">
                    <div className="space-y-6">
                        <div className="flex items-center gap-3">
                            <h3 className="text-[10px] font-black text-primary-500 dark:text-primary-400 uppercase tracking-[0.3em]">Capital Recipient</h3>
                        </div>
                        <div className="space-y-2">
                            <p className="text-2xl font-black text-slate-900 dark:text-white tracking-tight">{invoice.clientName}</p>
                            <div className="space-y-1.5">
                                <div className="flex items-center gap-2 text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest">
                                    <div className="w-2 h-2 rounded-full border-2 border-slate-200 dark:border-slate-800" />
                                    ID: {invoice.clientId?.substring(0, 8) || 'CLIENT_X_91'}
                                </div>
                                <div className="flex items-center gap-2 text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest">
                                    <div className="w-2 h-2 rounded-full border-2 border-slate-200 dark:border-slate-800" />
                                    client@example.com
                                </div>
                            </div>
                        </div>
                    </div>

                    <div className="sm:text-right space-y-8">
                        <div className="grid grid-cols-2 sm:flex sm:flex-col gap-8">
                            <div>
                                <h3 className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mb-2">Timestamp</h3>
                                <p className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-tighter">{formatDate(invoice.date)}</p>
                            </div>
                            <div>
                                <h3 className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mb-2">Network Status</h3>
                                <span className={cn(
                                    "inline-flex items-center gap-2 px-4 py-1.5 rounded-full text-[10px] font-black uppercase tracking-widest shadow-sm border",
                                    invoice.status === 'completed' ? "bg-emerald-50 dark:bg-emerald-400/10 text-emerald-600 dark:text-emerald-400 border-emerald-100 dark:border-emerald-400/20" :
                                    invoice.status === 'pending' ? "bg-amber-50 dark:bg-amber-400/10 text-amber-600 dark:text-amber-400 border-amber-100 dark:border-amber-400/20" :
                                    "bg-slate-50 dark:bg-slate-800 text-slate-400 dark:text-slate-500 border-slate-100 dark:border-slate-700"
                                )}>
                                    {invoice.status === 'completed' ? <CheckCircle2 className="h-3 w-3" /> : <Clock className="h-3 w-3" />}
                                    {invoice.status}
                                </span>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Items Table */}
                <div className="px-10 sm:px-16 pb-16">
                    <div className="rounded-[32px] border border-slate-100 dark:border-slate-800 overflow-hidden shadow-2xl shadow-slate-200/50 dark:shadow-none bg-white dark:bg-slate-950/20">
                        <table className="w-full text-left">
                            <thead className="bg-slate-50/50 dark:bg-slate-900/50">
                                <tr>
                                    <th className="px-8 py-5 text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-widest">Protocol / Description</th>
                                    <th className="px-8 py-5 text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-widest text-center">Unit</th>
                                    <th className="px-8 py-5 text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-widest text-right">Base Price</th>
                                    <th className="px-8 py-5 text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-widest text-right whitespace-nowrap">Net Capital</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-slate-100 dark:divide-slate-800/50">
                                <tr className="group hover:bg-slate-50/30 dark:hover:bg-slate-800/10 transition-colors">
                                    <td className="px-8 py-8">
                                        <p className="font-black text-slate-900 dark:text-white uppercase tracking-tight group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors">
                                            {invoice.serviceName || 'Strategic Business Deployment'}
                                        </p>
                                        <p className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase mt-1.5 tracking-widest">Enterprise Class Infrastructure</p>
                                    </td>
                                    <td className="px-8 py-8 text-slate-500 dark:text-slate-400 font-black text-center text-xs">01</td>
                                    <td className="px-8 py-8 text-slate-600 dark:text-slate-400 text-right font-mono font-bold text-xs">{formatCurrency(invoice.amount)}</td>
                                    <td className="px-8 py-8 text-slate-900 dark:text-white text-right font-black font-mono tracking-tighter text-lg">{formatCurrency(invoice.amount)}</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>

                {/* Totals Section */}
                <div className="p-10 sm:p-16 bg-slate-50/30 dark:bg-slate-950/40 relative">
                    <div className="flex flex-col lg:flex-row justify-between gap-16">
                        <div className="flex-1 space-y-10">
                            <div className="p-8 bg-white dark:bg-slate-900 rounded-[32px] border border-slate-100 dark:border-slate-800 shadow-xl inline-block min-w-[340px] relative overflow-hidden group">
                                <div className="absolute top-0 right-0 w-32 h-32 bg-primary-500/5 dark:bg-primary-500/10 blur-2xl rounded-full -mr-16 -mt-16 group-hover:scale-150 transition-transform duration-700" />
                                <h3 className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mb-6">Payment Protocol</h3>
                                <div className="flex items-center gap-5 relative">
                                    <div className="p-4 bg-primary-50 dark:bg-primary-400/10 rounded-2xl text-primary-600 dark:text-primary-400 shadow-inner">
                                        <CreditCard className="h-6 w-6" />
                                    </div>
                                    <div>
                                        <p className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-widest">{invoice.method || 'Digital Merchant'}</p>
                                        <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 mt-1 uppercase tracking-widest tracking-widest">Verified Transaction Gateway</p>
                                    </div>
                                </div>
                            </div>
                            <div className="max-w-sm">
                                <p className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.4em] mb-3">Transmission Notes</p>
                                <p className="text-xs font-bold text-slate-500 dark:text-slate-400 leading-relaxed italic uppercase tracking-widest">
                                    "Verified transaction integrity confirmed. Capital allocation successfully processed via secure uplink."
                                </p>
                            </div>
                        </div>

                        <div className="w-full lg:w-80 space-y-4">
                            <div className="flex justify-between text-[11px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.2em] px-2">
                                <span>Sub-Allocation</span>
                                <span className="font-mono text-slate-900 dark:text-white">{formatCurrency(invoice.amount)}</span>
                            </div>
                            <div className="flex justify-between text-[11px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.2em] px-2">
                                <span>Tax Load (0%)</span>
                                <span className="font-mono text-slate-900 dark:text-white">$0.00</span>
                            </div>
                            <div className="h-px bg-slate-200 dark:bg-slate-800 my-6 mx-2" />
                            <div className="flex justify-between items-center bg-primary-600 dark:bg-primary-500 p-8 rounded-[32px] shadow-[0_20px_40px_rgba(79,70,229,0.3)] dark:shadow-none text-white transform hover:scale-[1.02] transition-transform">
                                <span className="text-xs font-black uppercase tracking-[0.4em]">Grand Matrix</span>
                                <span className="text-3xl font-black font-mono tracking-tighter">{formatCurrency(invoice.amount)}</span>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Footer Section */}
                <div className="p-10 border-t border-slate-50 dark:border-slate-800 text-center bg-white dark:bg-slate-900/50">
                    <p className="text-[10px] font-black text-slate-300 dark:text-slate-700 uppercase tracking-[0.6em] animate-pulse">Electronic Document Alpha-91</p>
                </div>
            </div>

            {/* Print Styles */}
            <style jsx global>{`
                @media print {
                    body { background: white !important; }
                    .print\\:hidden { display: none !important; }
                    .bg-white { background-color: white !important; }
                    .text-slate-900 { color: #0f172a !important; }
                    .text-slate-500 { color: #64748b !important; }
                    .bg-slate-50\\/50 { background-color: #f8fafc !important; }
                    .bg-primary-600 { background-color: #4f46e5 !important; -webkit-print-color-adjust: exact; }
                    .shadow-2xl, .shadow-xl, .shadow-lg { box-shadow: none !important; }
                    .border { border: 1px solid #e2e8f0 !important; }
                    .rounded-\\[40px\\], .rounded-[32px] { border-radius: 0 !important; }
                }
            `}</style>
        </div>
    );
}
