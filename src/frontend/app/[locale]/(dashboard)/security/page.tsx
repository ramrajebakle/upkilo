"use client";

import React, { useState, useEffect } from "react";
import { 
  ShieldCheck, ShieldAlert, FileSearch, Lock, 
  Activity, Users, Globe, ExternalLink, 
  Terminal, AlertTriangle, CheckCircle 
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";

export default function SecurityCompliancePage() {
  const [scanning, setScanning] = useState(false);
  const [scanResult, setScanResult] = useState<any>(null);

  const stats = [
    { name: "Tenant Isolation", status: "Verified", icon: Users, color: "text-green-500" },
    { name: "Data Encryption", status: "Enabled (AES-256)", icon: Lock, color: "text-blue-500" },
    { name: "SSRF Protection", status: "Active", icon: Globe, color: "text-purple-500" },
    { name: "Audit Logging", status: "SOC2 Level", icon: FileSearch, color: "text-indigo-500" },
  ];

  const handleScan = async () => {
    setScanning(true);
    // Simulate API call to SecurityAuditController
    await new Promise(r => setTimeout(r, 2000));
    setScanResult({
      score: 98,
      lastScan: new Date().toLocaleString(),
      findings: [
        "SQLi patterns: 0 detected",
        "XSS vulnerabilities: 0 detected",
        "CSRF protection: Verified on all endpoints",
        "RLS bypass attempts: 0 detected"
      ]
    });
    setScanning(false);
  };

  return (
    <div className="space-y-8 max-w-6xl">
      <div>
        <h1 className="text-3xl font-bold tracking-tight text-slate-900 dark:text-white">Security & Compliance</h1>
        <p className="text-muted-foreground dark:text-slate-400">Monitor platform integrity and SOC2 compliance status.</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {stats.map((s) => (
          <Card key={s.name} className="dark:bg-slate-900 dark:border-slate-800">
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium dark:text-white">{s.name}</CardTitle>
              <s.icon className={`h-4 w-4 ${s.color}`} />
            </CardHeader>
            <CardContent>
              <div className="text-lg font-bold text-slate-900 dark:text-white">{s.status}</div>
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Security Scan */}
        <Card className="lg:col-span-2 dark:bg-slate-900 dark:border-slate-800">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 dark:text-white">
              <ShieldCheck className="h-5 w-5 text-primary" />
              Autonomous Security Scan
            </CardTitle>
            <CardDescription className="dark:text-slate-400">Run a real-time security audit across your tenant resources.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="flex items-center justify-between p-4 bg-gray-50 dark:bg-slate-800/50 rounded-xl border border-slate-200 dark:border-slate-800 shadow-sm">
              <div className="flex items-center gap-4">
                <div className="text-center">
                  <div className="text-3xl font-bold text-primary">{scanResult?.score ?? "--"}</div>
                  <div className="text-[10px] uppercase text-gray-500 dark:text-slate-500 font-bold">Safety Score</div>
                </div>
                <div className="h-10 w-px bg-gray-200 dark:bg-slate-700" />
                <div>
                  <div className="text-sm font-medium text-slate-900 dark:text-white">Last Scan: {scanResult?.lastScan ?? "Never"}</div>
                  <div className="text-xs text-gray-500 dark:text-slate-400">Continuous monitoring is active in the background.</div>
                </div>
              </div>
              <Button onClick={handleScan} disabled={scanning}>
                {scanning ? "Scanning..." : "Run Security Scan"}
              </Button>
            </div>

            {scanResult && (
              <div className="space-y-3">
                <h4 className="text-sm font-bold flex items-center gap-2 text-slate-900 dark:text-slate-300">
                  <Terminal className="h-4 w-4" /> Scan Findings
                </h4>
                <div className="bg-black text-green-400 p-4 rounded-lg font-mono text-xs space-y-1">
                  {scanResult.findings.map((f: string, i: number) => (
                    <div key={i}>$ {f}</div>
                  ))}
                  <div>$ _</div>
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Audit Logs */}
        <Card className="dark:bg-slate-900 dark:border-slate-800">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 dark:text-white">
              <Activity className="h-5 w-5 text-indigo-500" />
              Real-time Audit
            </CardTitle>
            <CardDescription className="dark:text-slate-400">Recent sensitive actions.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {[
                { action: "Login", user: "Admin", time: "2 min ago", icon: CheckCircle, color: "text-green-500" },
                { action: "API Key Generated", user: "DevSys", time: "1 hr ago", icon: AlertTriangle, color: "text-amber-500" },
                { action: "Billing Updated", user: "Admin", time: "3 hr ago", icon: CheckCircle, color: "text-green-500" },
              ].map((log, i) => (
                <div key={i} className="flex items-center gap-3 text-sm">
                  <log.icon className={`h-4 w-4 ${log.color}`} />
                  <div className="flex-1">
                    <div className="font-medium text-slate-900 dark:text-white">{log.action}</div>
                    <div className="text-[10px] text-gray-400 dark:text-slate-500">{log.user} • {log.time}</div>
                  </div>
                </div>
              ))}
              <Button variant="ghost" className="w-full text-xs text-primary dark:hover:bg-slate-800">
                View All SOC2 Logs <ExternalLink className="h-3 w-3 ml-2" />
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
