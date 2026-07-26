"use client";

import React, { useState } from "react";
import { Sparkles, Zap, Shield, Mail, FileText, ArrowRight, CheckCircle2 } from "lucide-react";
import { Card, CardContent } from "@/components/ui/Card";
import { Switch } from "@/components/ui/Switch";
import { Button } from "@/components/ui/Button";
import { useSubscription } from "@/hooks/useSubscription";

export function AIToolsOverview() {
  const { usage, isLoading } = useSubscription();

  const [workflows, setWorkflows] = useState({
    autoDraft: true,
    churnPrediction: true,
    sentimentAnalysis: false,
    smartScheduling: true,
  });

  const toggleWorkflow = (key: keyof typeof workflows) => {
    setWorkflows(prev => ({ ...prev, [key]: !prev[key] }));
  };

  const creditsUsed = usage?.aiCredits.used ?? 0;
  const creditsLimit = usage?.aiCredits.limit ?? 0;
  const unlimited = creditsLimit === -1;
  const pctUsed = unlimited || creditsLimit === 0
    ? 0
    : Math.min(100, (creditsUsed / creditsLimit) * 100);
  const nearLimit = !unlimited && creditsLimit > 0 && pctUsed >= 80;

  const renewsOn = usage?.periodEnd
    ? new Date(usage.periodEnd).toLocaleDateString("en-US", {
        month: "short",
        day: "numeric",
        year: "numeric",
      })
    : null;

  return (
    <div className="space-y-8 animate-fade-in">
      {/* Hero: Credit Usage Meter */}
      <Card variant="glow" className="bg-surface-base border-ai-200 overflow-hidden relative">
        <div className="absolute right-0 top-0 bottom-0 w-1/2 bg-gradient-to-l from-ai-50/50 to-transparent pointer-events-none" />
        <CardContent className="p-8">
          <div className="flex flex-col md:flex-row gap-8 items-center justify-between">
            <div className="flex-1 w-full">
              <div className="flex justify-between items-end mb-2">
                <div>
                  <h3 className="text-lg font-bold text-text-primary flex items-center gap-2">
                    <Zap size={18} className="text-ai-500" />
                    Monthly AI Credits
                  </h3>
                  <p className="text-sm text-text-secondary">
                    {renewsOn ? `Renews on ${renewsOn}` : " "}
                  </p>
                </div>
                <div className="text-right">
                  {isLoading && !usage ? (
                    <span className="text-2xl font-bold text-text-tertiary">—</span>
                  ) : (
                    <>
                      <span className="text-2xl font-bold text-text-primary">
                        {creditsUsed.toLocaleString()}
                      </span>
                      <span className="text-text-tertiary">
                        {unlimited ? " / unlimited" : ` / ${creditsLimit.toLocaleString()}`}
                      </span>
                    </>
                  )}
                </div>
              </div>

              {/* Progress Bar */}
              <div className="h-3 w-full bg-surface-200 rounded-full overflow-hidden mt-4">
                <div
                  className="h-full bg-ai-500 rounded-full transition-all duration-1000 ease-out"
                  style={{ width: `${pctUsed}%` }}
                />
              </div>
              {nearLimit && (
                <p className="text-xs text-ai-600 font-medium mt-2">
                  Approaching limit. Some background workflows may pause soon.
                </p>
              )}
              {!unlimited && creditsLimit === 0 && !isLoading && (
                <p className="text-xs text-text-tertiary mt-2">
                  AI credits are not included in your current plan.
                </p>
              )}
            </div>
            
            <div className="shrink-0 w-full md:w-auto flex flex-col gap-3 border-t md:border-t-0 md:border-l border-surface-200 pt-6 md:pt-0 md:pl-8">
              <Button variant="ai" fullWidth rightIcon={<ArrowRight size={16}/>}>
                Upgrade AI Plan
              </Button>
              <Button variant="outline" fullWidth>
                Buy One-Time Credits
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      <section>
        <h2 className="text-xl font-bold text-text-primary mb-6">Active Background Workflows</h2>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {/* Workflow 1 */}
          <Card className={`transition-colors duration-200 ${workflows.autoDraft ? 'border-ai-300 shadow-sm bg-surface-0' : 'bg-surface-50 opacity-80'}`}>
            <CardContent className="p-5">
              <div className="flex justify-between items-start mb-4">
                <div className={`p-2 rounded-lg ${workflows.autoDraft ? 'bg-ai-100 text-ai-600' : 'bg-surface-200 text-text-tertiary'}`}>
                  <Mail size={20} />
                </div>
                <Switch 
                  checked={workflows.autoDraft} 
                  onChange={() => toggleWorkflow('autoDraft')} 
                  variant="ai"
                  aria-label="Toggle Auto-Draft Responses"
                />
              </div>
              <h3 className="font-semibold text-text-primary text-lg mb-1">Auto-Draft Responses</h3>
              <p className="text-sm text-text-secondary leading-relaxed mb-4 min-h-[40px]">
                Copilot will automatically draft replies for incoming customer support queries based on your past resolutions.
              </p>
              <div className="flex items-center gap-2 text-xs font-medium text-success-600 bg-success-50 px-2 py-1 rounded inline-flex">
                <CheckCircle2 size={14} /> Saves ~4 hrs/week
              </div>
            </CardContent>
          </Card>

          {/* Workflow 2 */}
          <Card className={`transition-colors duration-200 ${workflows.churnPrediction ? 'border-ai-300 shadow-sm bg-surface-0' : 'bg-surface-50 opacity-80'}`}>
            <CardContent className="p-5">
              <div className="flex justify-between items-start mb-4">
                <div className={`p-2 rounded-lg ${workflows.churnPrediction ? 'bg-ai-100 text-ai-600' : 'bg-surface-200 text-text-tertiary'}`}>
                  <Shield size={20} />
                </div>
                <Switch 
                  checked={workflows.churnPrediction} 
                  onChange={() => toggleWorkflow('churnPrediction')} 
                  variant="ai"
                  aria-label="Toggle Churn Prediction"
                />
              </div>
              <h3 className="font-semibold text-text-primary text-lg mb-1">Churn Prediction</h3>
              <p className="text-sm text-text-secondary leading-relaxed mb-4 min-h-[40px]">
                Background scanning of user activity to flag accounts likely to cancel before they do.
              </p>
              <div className="flex items-center gap-2 text-xs font-medium text-text-tertiary bg-surface-100 px-2 py-1 rounded inline-flex">
                <Zap size={14} className="text-warning-500"/> High Credit Usage
              </div>
            </CardContent>
          </Card>

          {/* Workflow 3 */}
          <Card className={`transition-colors duration-200 ${workflows.sentimentAnalysis ? 'border-ai-300 shadow-sm bg-surface-0' : 'bg-surface-50 opacity-80'}`}>
            <CardContent className="p-5">
              <div className="flex justify-between items-start mb-4">
                <div className={`p-2 rounded-lg ${workflows.sentimentAnalysis ? 'bg-ai-100 text-ai-600' : 'bg-surface-200 text-text-tertiary'}`}>
                  <FileText size={20} />
                </div>
                <Switch 
                  checked={workflows.sentimentAnalysis} 
                  onChange={() => toggleWorkflow('sentimentAnalysis')} 
                  variant="ai"
                  aria-label="Toggle Document Sentiment"
                />
              </div>
              <h3 className="font-semibold text-text-primary text-lg mb-1">Document Sentiment</h3>
              <p className="text-sm text-text-secondary leading-relaxed mb-4 min-h-[40px]">
                Analyze uploaded files and survey responses for emotional tone and key extraction.
              </p>
            </CardContent>
          </Card>

        </div>
      </section>
    </div>
  );
}
