"use client";

import React, { useState } from "react";
import { 
  MessageCircle, Bot, Code, Zap, 
  Settings2, Copy, Check
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";

export default function LiveChatSettingsPage() {
  const { success } = useToast();
  const [copied, setCopied] = useState(false);

  const embedCode = `<script>
  window.upkiloChatConfig = { tenantId: "tenant_abc123" };
</script>
<script src="https://cdn.upkilo.com/chat/widget.js" async></script>`;

  const handleCopy = () => {
    navigator.clipboard.writeText(embedCode);
    setCopied(true);
    success("Embed code copied to clipboard!");
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div className="space-y-8 max-w-6xl">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Live Chat Widget</h1>
        <p className="text-muted-foreground">Configure your website widget and AI chatbot handoff rules.</p>
      </div>

      <div className="grid lg:grid-cols-2 gap-8">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Code className="h-5 w-5 text-primary" />
              Embed Code (Task 1837)
            </CardTitle>
            <CardDescription>Place this snippet before the closing &lt;/body&gt; tag on your website.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
             <div className="relative">
               <pre className="bg-gray-900 text-green-400 p-4 rounded-lg text-xs font-mono overflow-x-auto">
                 {embedCode}
               </pre>
               <Button 
                 variant="secondary" 
                 size="sm" 
                 className="absolute top-2 right-2 h-7"
                 onClick={handleCopy}
               >
                 {copied ? <Check className="h-3 w-3 mr-1" /> : <Copy className="h-3 w-3 mr-1" />}
                 {copied ? 'Copied' : 'Copy'}
               </Button>
             </div>
             <p className="text-xs text-foreground-secondary flex items-center gap-1">
               <Zap className="h-3 w-3 text-warning-fg" /> Changes to widget colors and rules update instantly.
             </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Bot className="h-5 w-5 text-primary" />
              AI Chatbot Handoff (Task 1849)
            </CardTitle>
            <CardDescription>Rules for escalating chats to human staff.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
             <div className="space-y-4">
               <div className="flex justify-between items-center p-3 border rounded-lg bg-muted">
                 <div>
                   <div className="font-bold text-sm">First Responder</div>
                   <div className="text-xs text-foreground-secondary">Who answers incoming chats?</div>
                 </div>
                 <select className="text-sm border rounded p-1 bg-card">
                   <option>AI Agent</option>
                   <option>Human Staff</option>
                 </select>
               </div>
               
               <div className="flex justify-between items-center p-3 border rounded-lg bg-muted">
                 <div>
                   <div className="font-bold text-sm">Handoff Triggers</div>
                   <div className="text-xs text-foreground-secondary">When should AI tag a human?</div>
                 </div>
                 <Button variant="outline" size="sm">Configure 3 Rules</Button>
               </div>

               <div className="flex gap-2">
                  <span className="bg-brand-subtle text-primary text-[10px] uppercase font-bold px-2 py-1 rounded">
                    Trigger: Sentiment is Negative
                  </span>
                  <span className="bg-brand-subtle text-primary text-[10px] uppercase font-bold px-2 py-1 rounded">
                    Trigger: "Speak to agent"
                  </span>
               </div>
             </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
