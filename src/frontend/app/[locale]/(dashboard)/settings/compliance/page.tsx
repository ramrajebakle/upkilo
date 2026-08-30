"use client";

import React, { useState, useEffect } from "react";
import { Shield, CheckCircle2, AlertCircle, FileText, Loader2, Download, Trash2 } from "lucide-react";
import { apiClient } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

interface HipaaStatus { status: string; lastAssessment?: string; findings?: string[]; }
interface Soc2Evidence { category: string; evidence: string; collectedAt: string; }

export default function CompliancePage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [hipaa, setHipaa] = useState<HipaaStatus | null>(null);
  const [soc2, setSoc2] = useState<Soc2Evidence[]>([]);
  const [loading, setLoading] = useState(true);
  const [collecting, setCollecting] = useState(false);
  const [erasureEmail, setErasureEmail] = useState("");
  const [erasing, setErasing] = useState(false);

  useEffect(() => {
    Promise.all([
      apiClient.get("/api/v1/compliance/hipaa").catch(() => ({ data: null })),
      apiClient.get("/api/v1/compliance/soc2/evidence").catch(() => ({ data: [] })),
    ]).then(([h, s]) => {
      setHipaa(h.data?.data ?? h.data ?? null);
      setSoc2(Array.isArray(s.data) ? s.data : s.data?.data ?? []);
    }).finally(() => setLoading(false));
  }, []);

  const collectSoc2 = async () => {
    setCollecting(true);
    try {
      const r = await apiClient.post("/api/v1/compliance/soc2/collect");
      toastSuccess(`SOC2 evidence collected (${r.data?.count ?? 0} items)`);
      const updated = await apiClient.get("/api/v1/compliance/soc2/evidence").catch(() => ({ data: [] }));
      setSoc2(Array.isArray(updated.data) ? updated.data : updated.data?.data ?? []);
    } catch { toastError("SOC2 collection failed"); }
    finally { setCollecting(false); }
  };

  const requestErasure = async () => {
    if (!erasureEmail) return;
    setErasing(true);
    try {
      await apiClient.post("/api/v1/compliance/gdpr/erasure", { email: erasureEmail });
      toastSuccess("GDPR erasure request submitted"); setErasureEmail("");
    } catch (e: any) { toastError(e?.response?.data?.error ?? "Erasure request failed"); }
    finally { setErasing(false); }
  };

  return (
    <div className="space-y-6 animate-fade-in max-w-3xl">
      <header className="border-b border-surface-200 pb-6">
        <h1 className="text-3xl font-bold text-text-primary flex items-center gap-3">Compliance & Privacy <Shield className="text-success-fg" size={22} /></h1>
        <p className="text-text-secondary mt-1">HIPAA, SOC2, GDPR, CCPA compliance management and evidence collection.</p>
      </header>

      {loading ? <div className="flex justify-center py-10"><Loader2 className="h-6 w-6 animate-spin text-text-tertiary" /></div> : (
        <>
          <Card className={hipaa?.status === "Compliant" ? "border-green-200" : "border-amber-200"}>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                {hipaa?.status === "Compliant"
                  ? <CheckCircle2 className="h-5 w-5 text-success-fg" />
                  : <AlertCircle className="h-5 w-5 text-warning-fg" />}
                HIPAA Status
              </CardTitle>
              <CardDescription>Healthcare data protection assessment</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex items-center gap-3 p-3 rounded-xl bg-surface-50 border border-surface-200">
                <span className={`text-sm font-medium px-3 py-1 rounded-full ${hipaa?.status === "Compliant" ? "text-green-600 bg-green-50" : "text-amber-600 bg-amber-50"}`}>
                  {hipaa?.status ?? "Not assessed"}
                </span>
                {hipaa?.lastAssessment && <span className="text-xs text-text-tertiary">Last: {new Date(hipaa.lastAssessment).toLocaleDateString()}</span>}
              </div>
              {hipaa?.findings && hipaa.findings.length > 0 && (
                <ul className="space-y-1">
                  {hipaa.findings.map((f, i) => (
                    <li key={i} className="text-xs text-text-secondary flex items-start gap-1.5">
                      <AlertCircle className="h-3 w-3 text-warning-fg mt-0.5 flex-shrink-0" />{f}
                    </li>
                  ))}
                </ul>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>SOC2 Evidence</CardTitle>
              <CardDescription>Automatically collected compliance evidence for SOC2 Type II audit</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              <Button variant="outline" leftIcon={collecting ? <Loader2 size={14} className="animate-spin" /> : <Download size={14} />}
                onClick={collectSoc2} disabled={collecting}>{collecting ? "Collecting…" : "Collect Evidence Now"}</Button>
              {soc2.length > 0 && (
                <table className="w-full text-sm mt-3">
                  <thead><tr className="border-b border-surface-200">
                    {["Category", "Evidence", "Collected"].map((h) => (
                      <th key={h} className="text-left py-2 px-2 text-xs font-semibold text-text-tertiary uppercase">{h}</th>
                    ))}
                  </tr></thead>
                  <tbody>
                    {soc2.map((e, i) => (
                      <tr key={i} className="border-b border-surface-100">
                        <td className="py-2 px-2 text-xs font-medium text-text-primary">{e.category}</td>
                        <td className="py-2 px-2 text-xs text-text-secondary max-w-xs truncate">{e.evidence}</td>
                        <td className="py-2 px-2 text-xs text-text-tertiary">{new Date(e.collectedAt).toLocaleDateString()}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
              {soc2.length === 0 && <p className="text-sm text-text-tertiary">No evidence collected yet. Click above to collect.</p>}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2"><Trash2 className="h-4 w-4 text-danger-fg" /> GDPR Erasure Request</CardTitle>
              <CardDescription>Submit a data erasure request for an individual (Right to be Forgotten)</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex gap-2">
                <input type="email" value={erasureEmail} onChange={(e) => setErasureEmail(e.target.value)}
                  placeholder="user@example.com" className="flex-1 px-3 py-2 text-sm rounded-lg border border-surface-200 bg-surface-50 text-text-primary focus:outline-none focus:ring-2 focus:ring-ai-500" />
                <Button variant="primary" size="sm"
                  leftIcon={erasing ? <Loader2 size={14} className="animate-spin" /> : <Trash2 size={14} />}
                  onClick={requestErasure} disabled={!erasureEmail || erasing}>
                  {erasing ? "Submitting…" : "Request Erasure"}
                </Button>
              </div>
              <p className="text-xs text-text-tertiary">This will queue a deletion of all personal data associated with this email address. This action is irreversible.</p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader><CardTitle>CCPA — Do Not Sell</CardTitle><CardDescription>California Consumer Privacy Act compliance</CardDescription></CardHeader>
            <CardContent className="space-y-3">
              <div className="flex items-center gap-3 p-3 rounded-xl bg-green-50 border border-green-200">
                <CheckCircle2 className="h-5 w-5 text-success-fg flex-shrink-0" />
                <div>
                  <p className="text-sm font-medium text-green-800">Do-Not-Sell flag is respected</p>
                  <p className="text-xs text-success-fg">Client records with opt-out flags are excluded from all data sharing.</p>
                </div>
              </div>
              <div className="flex items-center gap-3 p-3 rounded-xl bg-green-50 border border-green-200">
                <CheckCircle2 className="h-5 w-5 text-success-fg flex-shrink-0" />
                <div>
                  <p className="text-sm font-medium text-green-800">Privacy Notice linked in booking portal</p>
                  <p className="text-xs text-success-fg">Clients are informed of their rights before booking.</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </>
      )}
    </div>
  );
}
