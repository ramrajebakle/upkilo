"use client";

import React, { useState } from "react";
import { ArrowRight, ArrowLeft, Mail, Smartphone, Users, CheckCircle, Send } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";

export function CampaignWizard() {
  const [step, setStep] = useState(1);
  const [type, setType] = useState<"email" | "sms" | null>(null);

  const nextStep = () => setStep(prev => Math.min(prev + 1, 4));
  const prevStep = () => setStep(prev => Math.max(prev - 1, 1));

  return (
    <div className="max-w-4xl mx-auto space-y-8">
      {/* Progress Bar */}
      <div className="flex items-center justify-between mb-8 relative">
        <div className="absolute top-1/2 left-0 w-full h-1 bg-muted -translate-y-1/2 z-0 rounded-full" />
        <div 
          className="absolute top-1/2 left-0 h-1 bg-primary -translate-y-1/2 z-0 rounded-full transition-all duration-300"
          style={{ width: `${((step - 1) / 3) * 100}%` }}
        />
        
        {[1, 2, 3, 4].map((i) => (
          <div key={i} className={`relative z-10 flex items-center justify-center w-8 h-8 rounded-full font-semibold text-sm transition-colors ${step >= i ? 'bg-primary text-primary-foreground' : 'bg-card text-foreground-muted border-2 border-border'}`}>
            {step > i ? <CheckCircle className="w-5 h-5" /> : i}
          </div>
        ))}
      </div>

      <Card className="min-h-[400px] flex flex-col">
        {step === 1 && (
          <>
            <CardHeader>
              <CardTitle className="text-xl">Choose Campaign Type</CardTitle>
            </CardHeader>
            <CardContent className="flex-1 flex flex-col space-y-6">
              <div className="grid grid-cols-2 gap-4">
                <button 
                  onClick={() => setType("email")}
                  className={`flex flex-col items-center justify-center p-8 rounded-xl border-2 transition-all ${type === 'email' ? 'border-primary bg-primary/5' : 'border-border hover:border-primary/50'}`}
                >
                  <Mail className={`w-12 h-12 mb-4 ${type === 'email' ? 'text-primary' : 'text-foreground-muted'}`} />
                  <h3 className="font-semibold text-lg">Email Broadcast</h3>
                  <p className="text-sm text-foreground-secondary text-center mt-2">Send beautifully formatted newsletters and promos.</p>
                </button>
                <button 
                  onClick={() => setType("sms")}
                  className={`flex flex-col items-center justify-center p-8 rounded-xl border-2 transition-all ${type === 'sms' ? 'border-primary bg-primary/5' : 'border-border hover:border-primary/50'}`}
                >
                  <Smartphone className={`w-12 h-12 mb-4 ${type === 'sms' ? 'text-primary' : 'text-foreground-muted'}`} />
                  <h3 className="font-semibold text-lg">SMS Text Message</h3>
                  <p className="text-sm text-foreground-secondary text-center mt-2">High open-rate text messages for quick alerts.</p>
                </button>
              </div>
            </CardContent>
          </>
        )}

        {step === 2 && (
          <>
            <CardHeader>
              <CardTitle className="text-xl">Audience Selection</CardTitle>
            </CardHeader>
            <CardContent className="flex-1 space-y-6">
              <div className="space-y-4">
                <label className="text-sm font-medium">Select target segment</label>
                <div className="grid gap-3">
                  {['All Active Clients (2,450)', 'VIP Members (312)', 'Lapsed Customers (845)'].map((segment, i) => (
                    <label key={i} className="flex items-center gap-3 p-4 border rounded-lg cursor-pointer hover:bg-accent">
                      <input type="radio" name="audience" className="w-4 h-4 text-primary" defaultChecked={i === 0} />
                      <div className="font-medium">{segment}</div>
                    </label>
                  ))}
                </div>
              </div>
            </CardContent>
          </>
        )}

        {step === 3 && (
          <>
            <CardHeader>
              <CardTitle className="text-xl">Message Content</CardTitle>
            </CardHeader>
            <CardContent className="flex-1 space-y-4">
              <div>
                <label className="text-sm font-medium mb-1 block">Campaign Name (Internal)</label>
                <Input placeholder="e.g. Spring Sale 2024" />
              </div>
              {type === 'email' && (
                <div>
                  <label className="text-sm font-medium mb-1 block">Subject Line</label>
                  <Input placeholder="Ready for summer?" />
                </div>
              )}
              <div>
                <label className="text-sm font-medium mb-1 block">Message Body</label>
                <textarea 
                  className="w-full min-h-[150px] p-3 rounded-md border border-input bg-background text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  placeholder="Type your message here..."
                />
                <p className="text-xs text-foreground-secondary mt-2">Available variables: {'{{client.first_name}}'}, {'{{client.last_visit}}'}</p>
              </div>
            </CardContent>
          </>
        )}

        {step === 4 && (
          <>
            <CardHeader>
              <CardTitle className="text-xl">Review & Send</CardTitle>
            </CardHeader>
            <CardContent className="flex-1 space-y-6">
              <div className="bg-muted p-6 rounded-lg space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <span className="text-sm text-foreground-secondary block">Type</span>
                    <span className="font-semibold capitalize">{type}</span>
                  </div>
                  <div>
                    <span className="text-sm text-foreground-secondary block">Target Audience</span>
                    <span className="font-semibold">All Active Clients (2,450)</span>
                  </div>
                </div>
              </div>
              <p className="text-sm text-center text-foreground-secondary">You are about to launch this campaign to 2,450 recipients. This action cannot be undone.</p>
            </CardContent>
          </>
        )}

        {/* Wizard Footer Navigation */}
        <div className="p-6 border-t mt-auto flex items-center justify-between bg-muted/50 rounded-b-xl">
          <Button variant="outline" onClick={prevStep} disabled={step === 1}>
            <ArrowLeft className="w-4 h-4 mr-2" /> Back
          </Button>
          <Button 
            onClick={nextStep} 
            disabled={(step === 1 && !type) || step === 4}
            className={step === 4 ? "hidden" : ""}
          >
            Continue <ArrowRight className="w-4 h-4 ml-2" />
          </Button>
          {step === 4 && (
            <Button className="bg-green-600 hover:bg-green-700 text-white">
              <Send className="w-4 h-4 mr-2" /> Launch Campaign
            </Button>
          )}
        </div>
      </Card>
    </div>
  );
}
