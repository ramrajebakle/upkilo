"use client";

import React, { useState } from "react";
import { ShieldAlert, Trash2, Search, Download } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";

export default function PrivacyPortalPage() {
  const [email, setEmail] = useState("");
  const [submitted, setSubmitted] = useState(false);

  return (
    <div className="min-h-screen bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-3xl mx-auto space-y-8">
        <div className="text-center">
          <ShieldAlert className="mx-auto h-12 w-12 text-primary" />
          <h2 className="mt-6 text-3xl font-bold tracking-tight text-gray-900">Privacy & Data Requests</h2>
          <p className="mt-2 text-sm text-gray-600">
            Submit a request to access, export, or delete your personal data in accordance with GDPR and CCPA regulations.
          </p>
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Submit a Request</CardTitle>
            <CardDescription>Enter your email address to initiate a data lookup.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-6">
            {!submitted ? (
              <>
                <div>
                  <label htmlFor="email" className="block text-sm font-medium text-gray-700">Email Address</label>
                  <div className="mt-1">
                    <Input 
                      id="email" 
                      type="email" 
                      required 
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                      placeholder="you@example.com" 
                    />
                  </div>
                </div>
                
                <div className="grid gap-4 sm:grid-cols-2">
                  <Button 
                    className="w-full h-auto py-4 flex flex-col items-center justify-center gap-2" 
                    variant="outline"
                    onClick={() => setSubmitted(true)}
                  >
                    <Download className="w-5 h-5" />
                    <div className="text-center">
                      <div className="font-semibold text-sm">Export Data</div>
                      <div className="text-xs text-gray-500 font-normal">Request a copy of your data</div>
                    </div>
                  </Button>

                  <Button 
                    className="w-full h-auto py-4 flex flex-col items-center justify-center gap-2 bg-red-50 text-red-600 border-red-200 hover:bg-red-100 hover:border-red-300 border" 
                    variant="ghost"
                    onClick={() => setSubmitted(true)}
                  >
                    <Trash2 className="w-5 h-5" />
                    <div className="text-center">
                      <div className="font-semibold text-sm">Delete Data</div>
                      <div className="text-xs text-red-400 font-normal">Right to be forgotten</div>
                    </div>
                  </Button>
                </div>
              </>
            ) : (
              <div className="text-center py-6">
                <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-green-100 mb-4">
                  <ShieldAlert className="h-6 w-6 text-green-600" />
                </div>
                <h3 className="text-lg font-medium text-gray-900">Request Received</h3>
                <p className="mt-2 text-sm text-gray-500">
                  We've sent a confirmation email to <span className="font-semibold">{email}</span>. 
                  Please click the link in that email to verify your identity and proceed with the request.
                </p>
                <div className="mt-6">
                  <Button variant="outline" onClick={() => setSubmitted(false)}>Start New Request</Button>
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        <div className="text-center text-xs text-gray-500">
          <p>Protected by reCAPTCHA and subject to the Privacy Policy and Terms of Service.</p>
        </div>
      </div>
    </div>
  );
}
