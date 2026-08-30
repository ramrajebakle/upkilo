"use client";

import React, { useState } from "react";
import { 
  Terminal, History, Zap, RefreshCcw, 
  ShieldAlert, Activity, Play, Pause, 
  GitBranch, Server 
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

export default function DevOpsDashboardPage() {
  const { success, error } = useToast();
  const [rollingBack, setRollingBack] = useState(false);
  const [canaryWeight, setCanaryWeight] = useState(10);

  const handleRollback = async () => {
    if (!confirm("Are you sure you want to trigger a production rollback? This will revert the last 3 deployments.")) return;
    
    setRollingBack(true);
    // Simulate API call to DevOpsDashboardService
    await new Promise(r => setTimeout(r, 3000));
    setRollingBack(false);
    success("Rollback successful. Traffic redirected to v1.24.5");
  };

  const updateCanary = (val: number) => {
    setCanaryWeight(val);
    success(`Canary weight updated to ${val}% of total traffic.`);
  };

  return (
    <div className="space-y-8 max-w-6xl">
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">System Operations (DevOps)</h1>
          <p className="text-muted-foreground">Manage production deployments, rollbacks, and canary traffic.</p>
        </div>
        <div className="flex gap-3">
          <Button variant="outline" className="text-red-600 border-red-200 bg-red-50" onClick={handleRollback} disabled={rollingBack}>
            <RefreshCcw className={`h-4 w-4 mr-2 ${rollingBack ? 'animate-spin' : ''}`} />
            {rollingBack ? "Rolling Back..." : "Trigger Rollback"}
          </Button>
          <Button className="bg-primary hover:bg-primary/90">
            <Zap className="h-4 w-4 mr-2" /> New Deployment
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Canary Control */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <GitBranch className="h-5 w-5 text-primary" />
              Canary Traffic Routing (Task 1411)
            </CardTitle>
            <CardDescription>Split live production traffic between Stable and Canary (v1.25.0-beta) versions.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-8 py-6">
            <div className="space-y-4">
              <div className="flex justify-between text-sm font-medium">
                <span>Stable (v1.24.5)</span>
                <span>Canary (v1.25.0)</span>
              </div>
              <div className="h-6 w-full bg-muted rounded-full overflow-hidden flex">
                <div 
                  className="bg-primary-500 h-full transition-all duration-500 flex items-center justify-center text-[10px] text-white font-bold"
                  style={{ width: `${100 - canaryWeight}%` }}
                >
                  {100 - canaryWeight}%
                </div>
                <div 
                  className="bg-primary-500 h-full transition-all duration-500 flex items-center justify-center text-[10px] text-white font-bold"
                  style={{ width: `${canaryWeight}%` }}
                >
                  {canaryWeight}%
                </div>
              </div>
              <div className="flex justify-center gap-2">
                {[0, 10, 25, 50, 100].map(w => (
                  <Button 
                    key={w} 
                    variant={canaryWeight === w ? "primary" : "outline"}
                    size="sm"
                    onClick={() => updateCanary(w)}
                  >
                    {w}%
                  </Button>
                ))}
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="p-4 rounded-xl border bg-muted space-y-2">
                <div className="text-xs font-bold text-foreground-secondary uppercase tracking-wider">Stable Errors</div>
                <div className="text-2xl font-bold">0.02%</div>
                <div className="text-[10px] text-success-fg font-medium flex items-center">
                   <Activity className="h-3 w-3 mr-1" /> Healthy
                </div>
              </div>
              <div className="p-4 rounded-xl border bg-muted space-y-2">
                <div className="text-xs font-bold text-foreground-secondary uppercase tracking-wider">Canary Errors</div>
                <div className="text-2xl font-bold">0.05%</div>
                <div className="text-[10px] text-warning-fg font-medium flex items-center">
                   <ShieldAlert className="h-3 w-3 mr-1" /> Monitoring
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Deployment History */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <History className="h-5 w-5 text-foreground-secondary" />
              Deployment Logs
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-6">
              {[
                { version: "v1.25.0", status: "Canary", time: "2 hr ago", color: "bg-brand-subtle text-primary" },
                { version: "v1.24.5", status: "Stable", time: "2 days ago", color: "bg-green-100 text-green-700" },
                { version: "v1.24.4", status: "Rolled Back", time: "3 days ago", color: "bg-red-100 text-red-700" },
              ].map((log, i) => (
                <div key={i} className="flex justify-between items-start text-sm">
                  <div>
                    <div className="font-bold">{log.version}</div>
                    <div className="text-[10px] text-foreground-secondary">{log.time}</div>
                  </div>
                  <span className={`text-[10px] font-bold px-2 py-0.5 rounded ${log.color}`}>
                    {log.status}
                  </span>
                </div>
              ))}
              <Button variant="ghost" className="w-full text-xs text-foreground-muted">
                View Full CI/CD History
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
