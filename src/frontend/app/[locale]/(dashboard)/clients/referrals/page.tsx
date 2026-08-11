"use client";

import React, { useState, useEffect, useCallback } from "react";
import { Share2, Plus, Gift, TrendingUp, Users, Copy, Check, Loader2, RefreshCw, Send } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface Referral {
  id: string;
  referrerName?: string;
  refereeName?: string;
  referrerEmail?: string;
  refereeEmail?: string;
  status: "Pending" | "Completed" | "Rewarded";
  createdAt: string;
  completedAt?: string;
  rewardAmount?: number;
}

interface ReferralAnalytics {
  totalReferrals: number;
  completedReferrals: number;
  pendingReferrals: number;
  totalRewards: number;
  conversionRate: number;
}

const STATUS_COLOR: Record<string, string> = {
  Pending: "text-amber-600 bg-amber-50",
  Completed: "text-blue-600 bg-blue-50",
  Rewarded: "text-green-600 bg-green-50",
};

export default function ReferralsPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [referrals, setReferrals] = useState<Referral[]>([]);
  const [analytics, setAnalytics] = useState<ReferralAnalytics>({ totalReferrals: 0, completedReferrals: 0, pendingReferrals: 0, totalRewards: 0, conversionRate: 0 });
  const [loading, setLoading] = useState(true);
  const [inviteEmail, setInviteEmail] = useState("");
  const [invitePhone, setInvitePhone] = useState("");
  const [sending, setSending] = useState(false);
  const [copied, setCopied] = useState(false);
  const [referralCode, setReferralCode] = useState("");

  const fetch = useCallback(async () => {
    setLoading(true);
    try {
      const [refRes, analyticsRes, codeRes] = await Promise.all([
        apiClient.get("/api/v1/referrals").catch(() => ({ data: [] })),
        apiClient.get("/api/v1/referrals/analytics").catch(() => ({ data: {} })),
        apiClient.post("/api/v1/referrals/generate-code", {}).catch(() => ({ data: { code: "" } })),
      ]);
      const d: Referral[] = Array.isArray(refRes.data) ? refRes.data : refRes.data?.data ?? [];
      setReferrals(d);
      const a = analyticsRes.data?.data ?? analyticsRes.data ?? {};
      setAnalytics({
        totalReferrals: a.totalReferrals ?? d.length,
        completedReferrals: a.completedReferrals ?? d.filter((r) => r.status === "Completed" || r.status === "Rewarded").length,
        pendingReferrals: a.pendingReferrals ?? d.filter((r) => r.status === "Pending").length,
        totalRewards: a.totalRewards ?? 0,
        conversionRate: a.conversionRate ?? 0,
      });
      setReferralCode(codeRes.data?.code ?? codeRes.data?.data?.code ?? "");
    } catch { toastError("Failed to load referrals"); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { fetch(); }, [fetch]);

  const sendInvite = async () => {
    if (!inviteEmail && !invitePhone) return;
    setSending(true);
    try {
      await apiClient.post("/api/v1/referrals/send-invite", { email: inviteEmail || null, phone: invitePhone || null });
      toastSuccess("Referral invite sent");
      setInviteEmail(""); setInvitePhone(""); fetch();
    } catch { toastError("Failed to send invite"); }
    finally { setSending(false); }
  };

  const copyCode = () => {
    navigator.clipboard.writeText(referralCode).then(() => { setCopied(true); setTimeout(() => setCopied(false), 2000); });
  };

  return (
    <div className="space-y-8 animate-fade-in">
      <header className="flex items-end justify-between border-b border-surface-200 pb-6">
        <div>
          <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Referrals <Share2 className="text-text-tertiary" size={22} /></h1>
          <p className="text-text-secondary mt-1">Track and manage your client referral programme.</p>
        </div>
        <Button variant="outline" leftIcon={<RefreshCw size={14} />} onClick={fetch} disabled={loading}>Refresh</Button>
      </header>

      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        {[
          { label: "Total referrals", value: analytics.totalReferrals, icon: Users, color: "text-blue-500" },
          { label: "Completed", value: analytics.completedReferrals, icon: Check, color: "text-green-500" },
          { label: "Pending", value: analytics.pendingReferrals, icon: TrendingUp, color: "text-amber-500" },
          { label: "Rewards paid", value: `$${analytics.totalRewards.toFixed(0)}`, icon: Gift, color: "text-primary-500" },
        ].map((s) => (
          <Card key={s.label}>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-xs font-medium text-text-secondary">{s.label}</CardTitle>
              <s.icon className={`h-4 w-4 ${s.color}`} />
            </CardHeader>
            <CardContent><p className={`text-2xl font-bold ${s.color}`}>{s.value}</p></CardContent>
          </Card>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {referralCode && (
          <Card>
            <CardHeader><CardTitle className="flex items-center gap-2"><Share2 className="h-4 w-4" /> Your Referral Code</CardTitle></CardHeader>
            <CardContent className="space-y-3">
              <div className="flex items-center gap-2">
                <div className="flex-1 px-4 py-3 bg-surface-50 rounded-lg border border-surface-200 font-mono font-bold text-lg text-text-primary tracking-widest">{referralCode}</div>
                <Button variant="outline" leftIcon={copied ? <Check size={14} className="text-green-500" /> : <Copy size={14} />} onClick={copyCode}>
                  {copied ? "Copied!" : "Copy"}
                </Button>
              </div>
            </CardContent>
          </Card>
        )}

        <Card>
          <CardHeader><CardTitle className="flex items-center gap-2"><Send className="h-4 w-4" /> Send Invite</CardTitle>
            <CardDescription>Invite a client to refer their friends</CardDescription></CardHeader>
          <CardContent className="space-y-3">
            <div>
              <label className="block text-xs font-medium text-text-secondary mb-1">Email</label>
              <input type="email" value={inviteEmail} onChange={(e) => setInviteEmail(e.target.value)} placeholder="client@email.com"
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
            </div>
            <div>
              <label className="block text-xs font-medium text-text-secondary mb-1">Phone (optional)</label>
              <input type="tel" value={invitePhone} onChange={(e) => setInvitePhone(e.target.value)} placeholder="+91 98765 43210"
                className="w-full px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
            </div>
            <Button variant="primary" className="w-full" leftIcon={sending ? <Loader2 size={14} className="animate-spin" /> : <Send size={14} />}
              onClick={sendInvite} disabled={(!inviteEmail && !invitePhone) || sending}>
              {sending ? "Sending…" : "Send Invite"}
            </Button>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><Gift className="h-4 w-4" /> Referral History</CardTitle>
          <CardDescription>{referrals.length} referrals</CardDescription></CardHeader>
        <CardContent>
          {loading ? <div className="flex justify-center py-10"><Loader2 className="h-5 w-5 animate-spin text-text-tertiary" /></div>
            : referrals.length === 0 ? (
              <div className="text-center py-10 text-text-tertiary">
                <Share2 className="h-10 w-10 mx-auto mb-3 opacity-20" />
                <p className="font-medium">No referrals yet</p>
                <p className="text-sm mt-1">Send your first invite to get started</p>
              </div>
            ) : (
              <table className="w-full text-sm">
                <thead><tr className="border-b border-surface-200">
                  {["Date", "Referrer", "Referred", "Status", "Reward"].map((h) => (
                    <th key={h} className="text-left py-3 px-3 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                  ))}
                </tr></thead>
                <tbody>
                  {referrals.map((r) => (
                    <tr key={r.id} className="border-b border-surface-100 hover:bg-surface-50">
                      <td className="py-3 px-3 text-xs text-text-secondary">{new Date(r.createdAt).toLocaleDateString([], { month: "short", day: "numeric" })}</td>
                      <td className="py-3 px-3 font-medium text-text-primary">{r.referrerName ?? r.referrerEmail ?? "—"}</td>
                      <td className="py-3 px-3 text-text-secondary">{r.refereeName ?? r.refereeEmail ?? "—"}</td>
                      <td className="py-3 px-3"><span className={`text-xs font-medium px-2 py-0.5 rounded-full ${STATUS_COLOR[r.status] ?? ""}`}>{r.status}</span></td>
                      <td className="py-3 px-3 font-medium text-green-600">{r.rewardAmount ? `$${r.rewardAmount}` : "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
        </CardContent>
      </Card>
    </div>
  );
}
