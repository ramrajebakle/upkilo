"use client";

import React, { useState } from "react";
import { Plus, Settings2, FileText, Share2 } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { FormBuilder } from "@/components/forms/FormBuilder";

export default function FormsPage() {
  const [activeTab, setActiveTab] = useState("builder");

  return (
    <div className="flex flex-col h-full space-y-6">
      <div className="flex justify-between items-center flex-wrap gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Website Contact Form</h1>
          <p className="text-muted-foreground">Manage fields and embedded code for your primary website form.</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline">
            <Share2 className="w-4 h-4 mr-2" /> Share Link
          </Button>
          <Button className="bg-primary hover:bg-primary/90">
            Save Changes
          </Button>
        </div>
      </div>

      <div className="border-b border-gray-200">
        <nav className="-mb-px flex space-x-8">
          <button
            onClick={() => setActiveTab("builder")}
            className={`${activeTab === "builder" ? "border-primary text-primary" : "border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700"} whitespace-nowrap pb-4 px-1 border-b-2 font-medium text-sm flex items-center gap-2`}
          >
            <FileText className="w-4 h-4" /> Form Builder
          </button>
          <button
            onClick={() => setActiveTab("settings")}
            className={`${activeTab === "settings" ? "border-primary text-primary" : "border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700"} whitespace-nowrap pb-4 px-1 border-b-2 font-medium text-sm flex items-center gap-2`}
          >
            <Settings2 className="w-4 h-4" /> Routing & Settings
          </button>
        </nav>
      </div>

      <div className="flex-1 mt-4">
        {activeTab === "builder" ? (
          <FormBuilder />
        ) : (
          <div className="max-w-2xl bg-white p-6 rounded-lg border shadow-sm space-y-6">
            <h3 className="font-semibold text-lg">Submission Automation</h3>
            <p className="text-sm text-gray-500">What happens when a client submits this form?</p>
            
            <div className="space-y-4">
              <div className="flex items-center justify-between border-b pb-4">
                <div>
                  <h4 className="font-medium text-sm">Create CRM Contact</h4>
                  <p className="text-xs text-gray-500">Automatically creates or updates a Client profile.</p>
                </div>
                <input type="checkbox" defaultChecked className="toggle" />
              </div>
              <div className="flex items-center justify-between border-b pb-4">
                <div>
                  <h4 className="font-medium text-sm">Create Deal</h4>
                  <p className="text-xs text-gray-500">Pushes the inquiry into the 'Lead In' stage of your Sales Pipeline.</p>
                </div>
                <input type="checkbox" defaultChecked className="toggle" />
              </div>
              <div className="flex items-center justify-between">
                <div>
                  <h4 className="font-medium text-sm">Send Auto-Reply</h4>
                  <p className="text-xs text-gray-500">Emails the client confirming receipt.</p>
                </div>
                <input type="checkbox" className="toggle" />
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
