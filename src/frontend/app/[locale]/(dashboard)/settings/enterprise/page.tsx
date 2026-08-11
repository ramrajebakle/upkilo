"use client";

import React, { useState } from "react";
import { 
  Globe, ShieldCheck, Key, Database, 
  MapPin, RefreshCw, Plus, ExternalLink, 
  AlertCircle, CheckCircle2 
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Label } from "@/components/ui/Label";
import { useToast } from "@/components/ui/Toast";

export default function EnterpriseSettingsPage() {
  const { success, error } = useToast();
  const [verifying, setVerifying] = useState(false);
  const [domain, setDomain] = useState("app.mybrandedspa.com");

  const handleVerifyDns = async () => {
    setVerifying(true);
    // Task 1607: DomainManagementService.VerifyDomainAsync hook simulation
    await new Promise(r => setTimeout(r, 2000));
    setVerifying(false);
    success("DNS Verification Successful. SSL certificate is being provisioned.");
  };

  return (
    <div className="space-y-8 max-w-6xl">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Enterprise & White-Label</h1>
        <p className="text-muted-foreground">Manage custom domains, SSO federation, and data residency.</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        {/* Custom Domain Section */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Globe className="h-5 w-5 text-primary" />
              Custom Domain (Task 1605)
            </CardTitle>
            <CardDescription>Host your portal on your own branded domain.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="domain">Domain Name</Label>
              <div className="flex gap-2">
                <Input 
                  id="domain" 
                  value={domain} 
                  onChange={(e) => setDomain(e.target.value)} 
                  placeholder="e.g. portal.yourbrand.com"
                />
                <Button onClick={handleVerifyDns} loading={verifying}>Verify DNS</Button>
              </div>
            </div>
            
            <div className="p-4 bg-gray-50 rounded-lg border text-xs font-mono space-y-2">
              <div className="text-gray-500 uppercase font-bold text-[10px]">Required DNS TXT Record:</div>
              <div className="flex justify-between items-center text-gray-700">
                <span>Type: TXT</span>
                <span>Name: @</span>
              </div>
              <div className="p-2 bg-white border rounded break-all select-all">
                upkilo-verification=67ed59e4-c0b0-43eb-ab31-1d2e4c9e0d38
              </div>
            </div>
          </CardContent>
        </Card>

        {/* SSO Federation */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <ShieldCheck className="h-5 w-5 text-primary-500" />
              Enterprise SSO (Task 1737)
            </CardTitle>
            <CardDescription>Configure SAML 2.0 or OIDC for your team.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="p-4 rounded-xl border border-dashed flex flex-col items-center justify-center py-8 text-center space-y-4">
              <div className="w-12 h-12 bg-primary-50 rounded-full flex items-center justify-center">
                <Plus className="h-6 w-6 text-primary-500" />
              </div>
              <div>
                <p className="text-sm font-medium">No SSO Identity Providers</p>
                <p className="text-xs text-gray-500">Connect Okta, Azure AD, or Google Workspace.</p>
              </div>
              <Button variant="outline" size="sm">Add SSO Provider</Button>
            </div>
          </CardContent>
        </Card>

        {/* Data Residency */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <MapPin className="h-5 w-5 text-rose-500" />
              Data Residency & Privacy (Task 1794)
            </CardTitle>
            <CardDescription>Pin your tenant data to specific geographic regions for compliance.</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid md:grid-cols-3 gap-4">
              {[
                { name: "US East (Virginia)", status: "Active", latency: "24ms", selected: true },
                { name: "EU West (Dublin)", status: "Available", latency: "112ms", selected: false },
                { name: "Asia East (Singapore)", status: "Available", latency: "240ms", selected: false }
              ].map((region) => (
                <div key={region.name} className={`p-4 rounded-xl border flex flex-col justify-between h-32 transition-colors cursor-pointer ${region.selected ? 'border-primary bg-primary/5 ring-1 ring-primary' : 'bg-gray-50'}`}>
                  <div>
                    <div className="text-sm font-bold">{region.name}</div>
                    <div className="text-[10px] text-gray-500">Latency: {region.latency}</div>
                  </div>
                  {region.selected ? (
                    <div className="text-[10px] font-bold text-primary flex items-center gap-1">
                      <CheckCircle2 className="h-3 w-3" /> Pinned
                    </div>
                  ) : (
                    <Button variant="ghost" size="sm" className="h-6 text-[10px] p-0 text-gray-400">Migrate Region</Button>
                  )}
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
