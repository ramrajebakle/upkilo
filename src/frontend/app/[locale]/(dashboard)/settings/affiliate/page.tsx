'use client';

import { useEffect, useState } from 'react';

interface AffiliateStats {
  referralCode: string;
  totalEarnings: number;
  pendingPayout: number;
  managedAccounts: number;
  revenueSharePercent: number;
  stripeConnected: boolean;
  commissions: Commission[];
}

interface Commission {
  id: string;
  amount: number;
  status: 'Pending' | 'Paid' | 'Failed';
  createdAt: string;
  tenantName: string;
}

export default function AffiliateDashboardPage() {
  const [stats, setStats] = useState<AffiliateStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    fetch('/api/v1/agency/partner/stats', {
      headers: { Authorization: `Bearer ${localStorage.getItem('token')}` }
    })
      .then(r => r.json())
      .then(j => setStats(j.data))
      .catch(() => setStats(null))
      .finally(() => setLoading(false));
  }, []);

  const copyLink = () => {
    if (!stats?.referralCode) return;
    navigator.clipboard.writeText(`https://upkilo.com/ref/${stats.referralCode}`);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin w-8 h-8 border-4 border-indigo-600 border-t-transparent rounded-full" />
      </div>
    );
  }

  if (!stats) {
    return (
      <div className="max-w-2xl mx-auto py-16 px-4 text-center">
        <div className="text-5xl mb-4">🤝</div>
        <h2 className="text-xl font-bold text-gray-800 dark:text-white mb-2">Become an Affiliate Partner</h2>
        <p className="text-gray-500 dark:text-gray-400 mb-6">
          Earn 20% recurring commission on every business you refer to Upkilo.
          Paid monthly via Stripe.
        </p>
        <a href="/enterprise" className="bg-indigo-600 text-white px-6 py-3 rounded-xl font-semibold hover:bg-indigo-700">
          Contact Us to Join
        </a>
      </div>
    );
  }

  const referralUrl = `https://upkilo.com/ref/${stats.referralCode}`;

  return (
    <div className="max-w-4xl mx-auto py-10 px-4">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Affiliate Dashboard</h1>
        <p className="text-gray-500 dark:text-gray-400 mt-1">Earn {stats.revenueSharePercent}% recurring commission on every referral.</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
        {[
          { label: 'Total Earned', value: `$${stats.totalEarnings.toFixed(2)}`, color: 'text-green-700 dark:text-green-400' },
          { label: 'Pending Payout', value: `$${stats.pendingPayout.toFixed(2)}`, color: 'text-amber-700 dark:text-amber-400' },
          { label: 'Referrals', value: stats.managedAccounts, color: 'text-indigo-700 dark:text-indigo-400' },
          { label: 'Commission Rate', value: `${stats.revenueSharePercent}%`, color: 'text-gray-800 dark:text-white' },
        ].map(s => (
          <div key={s.label} className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl p-5 shadow-sm">
            <p className={`text-2xl font-bold ${s.color}`}>{s.value}</p>
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">{s.label}</p>
          </div>
        ))}
      </div>

      {/* Referral Link */}
      <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl p-6 shadow-sm mb-6">
        <h2 className="text-base font-semibold text-gray-800 dark:text-white mb-3">Your Referral Link</h2>
        <div className="flex items-center gap-3">
          <div className="flex-1 bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl px-4 py-2.5 text-sm text-gray-700 dark:text-gray-300 font-mono truncate">
            {referralUrl}
          </div>
          <button
            onClick={copyLink}
            className={`px-4 py-2.5 rounded-xl text-sm font-semibold transition-colors ${copied ? 'bg-green-600 text-white' : 'bg-indigo-600 text-white hover:bg-indigo-700'}`}
          >
            {copied ? '✓ Copied' : 'Copy Link'}
          </button>
        </div>
        <p className="text-xs text-gray-500 dark:text-gray-500 mt-2">
          Share this link. You earn {stats.revenueSharePercent}% of their monthly subscription — forever.
        </p>
      </div>

      {/* Stripe Connect */}
      <div className={`rounded-2xl p-6 shadow-sm mb-6 border ${stats.stripeConnected ? 'bg-green-50 dark:bg-green-900/10 border-green-200 dark:border-green-900/30' : 'bg-amber-50 dark:bg-amber-900/10 border-amber-200 dark:border-amber-900/30'}`}>
        <div className="flex items-center justify-between">
          <div>
            <p className={`font-semibold text-sm ${stats.stripeConnected ? 'text-green-800 dark:text-green-300' : 'text-amber-800 dark:text-amber-300'}`}>
              {stats.stripeConnected ? '✓ Stripe Connect Active' : '⚠ Stripe Connect Not Set Up'}
            </p>
            <p className={`text-xs mt-0.5 ${stats.stripeConnected ? 'text-green-600 dark:text-green-400' : 'text-amber-600 dark:text-amber-400'}`}>
              {stats.stripeConnected
                ? 'Payouts are sent to your Stripe account on the 1st of each month.'
                : 'Connect Stripe to receive your affiliate payouts automatically.'}
            </p>
          </div>
          {!stats.stripeConnected && (
            <a
              href="/api/v1/agency/partner/stripe-connect"
              className="bg-amber-600 text-white px-4 py-2 rounded-xl text-sm font-semibold hover:bg-amber-700"
            >
              Connect Stripe →
            </a>
          )}
        </div>
      </div>

      {/* Commission history */}
      <div className="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 rounded-2xl shadow-sm overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-100 dark:border-gray-800">
          <h2 className="text-base font-semibold text-gray-800 dark:text-white">Commission History</h2>
        </div>
        {stats.commissions.length === 0 ? (
          <div className="px-6 py-10 text-center text-gray-400 dark:text-gray-600">
            No commissions yet. Share your referral link to start earning.
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50 dark:bg-gray-800">
              <tr>
                {['Date', 'Referral', 'Amount', 'Status'].map(h => (
                  <th key={h} className="text-left px-6 py-3 text-xs font-semibold text-gray-500 dark:text-gray-400">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {stats.commissions.map(c => (
                <tr key={c.id} className="border-t border-gray-50 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-gray-800/50">
                  <td className="px-6 py-3 text-xs text-gray-500">{new Date(c.createdAt).toLocaleDateString()}</td>
                  <td className="px-6 py-3 text-sm font-medium text-gray-800 dark:text-white">{c.tenantName}</td>
                  <td className="px-6 py-3 text-sm font-semibold text-gray-900 dark:text-white">${c.amount.toFixed(2)}</td>
                  <td className="px-6 py-3">
                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${
                      c.status === 'Paid' ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400' :
                      c.status === 'Pending' ? 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400' :
                      'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400'
                    }`}>
                      {c.status}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
