'use client';

import React, { useState, useEffect } from 'react';
import { useSignalR, EscalationNotification } from '@/contexts/SignalRContext';
import { useToast } from '@/components/ui/Toast';
import { 
  ShieldAlert, 
  BrainCircuit, 
  Workflow, 
  CheckCircle2, 
  XCircle, 
  History,
  AlertTriangle,
  Fingerprint
} from 'lucide-react';

// JetBrains Mono-like font vibe for technical precision
const mono = 'font-mono text-xs';

export default function AIApprovalsPage() {
  const { connection, isConnected } = useSignalR();
  const { success, error, warning } = useToast();
  const [escalations, setEscalations] = useState<EscalationNotification[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  // In a real app, we'd fetch existing escalations from the API here
  useEffect(() => {
    const fetchEscalations = async () => {
        // Mock fetch for demonstration
        setIsLoading(false);
    };
    fetchEscalations();
  }, []);

  useEffect(() => {
    if (!connection) return;

    const handler = (notification: EscalationNotification) => {
      setEscalations(prev => [notification, ...prev]);
    };

    connection.on('SystemEscalation', handler);
    return () => { connection.off('SystemEscalation', handler); };
  }, [connection]);

  const handleAction = (id: string, action: 'Approve' | 'Reject' | 'Override') => {
    // In production, this would call an API:
    // await api.post(`/ai/approvals/${id}/resolve`, { action });
    
    setEscalations(prev => prev.filter(e => e.id !== id));
    success(`Escalation ${action}ed successfully.`);
  };

  const getModuleIcon = (module: string) => {
    switch (module.toUpperCase()) {
      case 'AI': return <BrainCircuit className="w-5 h-5 text-amber-400" />;
      case 'WORKFLOW': return <Workflow className="w-5 h-5 text-blue-400" />;
      case 'SECURITY': return <ShieldAlert className="w-5 h-5 text-danger-fg" />;
      default: return <AlertTriangle className="w-5 h-5 text-foreground-muted" />;
    }
  };

  return (
    <div className="min-h-screen bg-[#0a0a0a] text-gray-100 p-8 pt-24 selection:bg-amber-500/30">
      <header className="max-w-6xl mx-auto mb-12 flex justify-between items-end">
        <div>
          <div className="flex items-center gap-3 mb-2">
            <div className="h-2 w-2 rounded-full bg-amber-500 animate-pulse" />
            <span className="text-[10px] uppercase tracking-[0.3em] text-amber-500 font-bold">Autonomous Oversight</span>
          </div>
          <h1 className="text-4xl font-light tracking-tight">Escalation <span className="font-semibold text-white">Queue</span></h1>
          <p className="text-slate-400 mt-2 text-sm max-w-md italic">
            "Human-in-the-loop oversight for high-risk autonomous decisions and system uncertainties."
          </p>
        </div>
        
        <div className="flex gap-4">
            <div className="px-4 py-2 bg-white/5 border border-white/10 rounded-lg flex items-center gap-3">
                <div className={`h-2 w-2 rounded-full ${isConnected ? 'bg-emerald-500' : 'bg-rose-500'}`} />
                <span className="text-[10px] uppercase font-bold text-foreground-muted tracking-wider">Live Bridge {isConnected ? 'Active' : 'Offline'}</span>
            </div>
            <button className="p-3 bg-white/5 hover:bg-white/10 border border-white/10 rounded-lg transition-all">
                <History className="w-4 h-4 text-foreground-muted" />
            </button>
        </div>
      </header>

      <main className="max-w-6xl mx-auto">
        {escalations.length === 0 ? (
          <div className="h-64 border border-dashed border-white/10 rounded-2xl flex flex-col items-center justify-center text-center">
            <CheckCircle2 className="w-12 h-12 text-white/10 mb-4" />
            <p className="text-foreground-secondary text-sm">System stable. No pending escalations in queue.</p>
          </div>
        ) : (
          <div className="grid gap-6">
            {escalations.map((item, idx) => (
              <div 
                key={item.id}
                className="group relative bg-[#111] border border-white/10 rounded-xl overflow-hidden hover:border-white/20 transition-all duration-500"
                style={{ animationDelay: `${idx * 100}ms` }}
              >
                {/* Severity Accent Line */}
                <div className={`absolute left-0 top-0 bottom-0 w-1 ${
                  item.severity === 'Critical' ? 'bg-rose-600' : 
                  item.severity === 'High' ? 'bg-amber-600' : 'bg-blue-600'
                }`} />

                <div className="p-6 flex gap-6 items-start">
                  <div className="p-3 bg-white/5 rounded-lg border border-white/5">
                    {getModuleIcon(item.module)}
                  </div>

                  <div className="flex-grow">
                    <div className="flex justify-between items-start mb-2">
                      <div>
                        <h3 className="text-lg font-medium text-white">{item.reason}</h3>
                        <div className="flex gap-4 mt-1">
                          <span className={`${mono} flex items-center gap-1.5 text-foreground-muted uppercase tracking-widest`}>
                            <Fingerprint className="w-3 h-3" />
                            TXID-{item.id.substring(0, 8)}
                          </span>
                          <span className={`${mono} uppercase tracking-widest text-foreground-muted`}>
                            {item.module} · {item.severity}
                          </span>
                        </div>
                      </div>
                      <time className="text-[10px] font-mono text-foreground-secondary uppercase tracking-tighter">
                        {new Date(item.timestamp).toLocaleTimeString()}
                      </time>
                    </div>

                    <div className="mt-4 p-4 bg-black/40 rounded-lg border border-white/5">
                        <p className="text-sm text-gray-300 leading-relaxed font-light">
                            {item.metadata?.Content || "No detailed content provided for this escalation."}
                        </p>
                        {item.metadata && (
                            <pre className="mt-3 text-[10px] font-mono text-gray-500 overflow-x-auto p-2 bg-black/20 rounded">
                                {JSON.stringify(item.metadata, null, 2)}
                            </pre>
                        )}
                    </div>
                  </div>

                  <div className="flex flex-col gap-2 min-w-[140px]">
                    <button 
                        onClick={() => handleAction(item.id, 'Approve')}
                        className="w-full flex items-center justify-center gap-2 py-2 px-4 bg-emerald-500 text-black text-[10px] font-bold uppercase tracking-widest rounded-md hover:bg-emerald-400 transition-colors"
                    >
                        <CheckCircle2 className="w-3 h-3" /> Approve
                    </button>
                    <button 
                        onClick={() => handleAction(item.id, 'Reject')}
                        className="w-full flex items-center justify-center gap-2 py-2 px-4 bg-rose-600 text-white text-[10px] font-bold uppercase tracking-widest rounded-md hover:bg-rose-500 transition-colors"
                    >
                        <XCircle className="w-3 h-3" /> Reject
                    </button>
                    <button 
                        onClick={() => handleAction(item.id, 'Override')}
                        className="w-full py-2 px-4 border border-white/10 text-foreground-muted text-[10px] font-bold uppercase tracking-widest rounded-md hover:bg-white/5 hover:text-white transition-all"
                    >
                        Manual Override
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </main>

      <footer className="max-w-6xl mx-auto mt-16 pt-8 border-t border-white/5 flex justify-between items-center opacity-40">
        <p className="text-[9px] uppercase tracking-[0.4em] text-foreground-secondary uppercase">Secure Environment · Multi-Tenant Isolation: Active</p>
        <p className="text-[9px] uppercase tracking-[0.4em] text-foreground-secondary uppercase">Upkilo Autonomous Core v1.4.2</p>
      </footer>

      {/* A <style jsx global> block used to live here. It was the only place Outfit and
          JetBrains Mono were ever loaded — via @import from fonts.googleapis.com — while 58
          files across the app referenced Outfit and got a silent fallback.

          It also did two things a single route has no business doing: reassigning `body`
          font-family app-wide, so body text changed face depending on whether this page was
          mounted, and redefining Tailwind's own `.font-mono` utility to 'JetBrains+Mono' —
          a name that is not a font. The `+` is URL encoding from the Google Fonts query
          string, valid in the URL and meaningless in a font-family, so that rule never
          matched anything and every .font-mono element fell through to the next candidate.

          All three faces are now loaded by next/font in app/[locale]/layout.tsx and exposed
          as CSS variables that globals.css consumes. */}
    </div>
  );
}
