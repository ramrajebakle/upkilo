"use client";

import React, { useState } from "react";
import Image from "next/image";
import { UploadCloud, Database, Link as LinkIcon, RefreshCw, FileText, Settings, Plus, Search } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Badge } from "@/components/ui/Badge";

const mockSources = [
  { id: 1, name: "Company_Policies_2025.pdf", type: "file", status: "synced", lastSync: "2 hours ago", size: "2.4 MB" },
  { id: 2, name: "Zendesk Help Center", type: "integration", status: "synced", lastSync: "10 mins ago", size: "1,204 articles" },
  { id: 3, name: "Notion / Engineering Docs", type: "integration", status: "syncing", lastSync: "Syncing...", size: "Unknown" },
  { id: 4, name: "Product_Specs_v2.docx", type: "file", status: "error", lastSync: "Failed", size: "12 MB" },
];

export function KnowledgeEngine() {
  const [searchTerm, setSearchTerm] = useState("");

  return (
    <div className="space-y-8 animate-fade-in">
      <div className="flex justify-between items-start">
        <div>
          <h2 className="text-xl font-bold text-text-primary">Knowledge Engine Context</h2>
          <p className="text-sm text-text-secondary mt-1 max-w-2xl">
            Upload documents or connect external tools. Upkilo's AI Copilot uses these sources to understand your business, draft accurate responses, and make decisions.
          </p>
        </div>
        <div className="flex gap-3">
          <Button variant="outline" leftIcon={<UploadCloud size={16} />}>
            Upload File
          </Button>
          <Button variant="ai" leftIcon={<Plus size={16} />}>
            Connect App
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Quick Connect Cards */}
        <Card className="border-surface-200 shadow-sm hover:border-ai-300 transition-colors cursor-pointer group">
          <CardContent className="p-6 flex items-center gap-4">
            <div className="h-12 w-12 rounded-xl bg-surface-100 flex items-center justify-center group-hover:bg-ai-50 transition-colors">
              <Image src="https://upload.wikimedia.org/wikipedia/commons/e/e9/Notion-logo.svg" alt="Notion" width={24} height={24} className="opacity-80" />
            </div>
            <div>
              <h3 className="font-semibold text-text-primary">Notion</h3>
              <p className="text-xs text-text-secondary mt-1">Connect workspace</p>
            </div>
          </CardContent>
        </Card>

        <Card className="border-surface-200 shadow-sm hover:border-ai-300 transition-colors cursor-pointer group">
          <CardContent className="p-6 flex items-center gap-4">
            <div className="h-12 w-12 rounded-xl bg-surface-100 flex items-center justify-center group-hover:bg-ai-50 transition-colors">
              <Image src="https://upload.wikimedia.org/wikipedia/commons/a/a2/Google_Drive_icon_%282020%29.svg" alt="Google Drive" width={24} height={24} className="opacity-80" />
            </div>
            <div>
              <h3 className="font-semibold text-text-primary">Google Drive</h3>
              <p className="text-xs text-text-secondary mt-1">Select folders</p>
            </div>
          </CardContent>
        </Card>

        <Card className="border-surface-200 shadow-sm hover:border-ai-300 transition-colors cursor-pointer group">
          <CardContent className="p-6 flex items-center gap-4">
            <div className="h-12 w-12 rounded-xl bg-surface-100 flex items-center justify-center group-hover:bg-ai-50 transition-colors">
              <Database size={24} className="text-text-tertiary" />
            </div>
            <div>
              <h3 className="font-semibold text-text-primary">Custom API</h3>
              <p className="text-xs text-text-secondary mt-1">Webhook / REST</p>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Data Sources List */}
      <Card>
        <CardHeader className="border-b border-surface-100 pb-4 pt-6 px-6">
          <div className="flex justify-between items-center">
            <CardTitle className="text-lg">Connected Sources</CardTitle>
            <div className="w-64">
              <Input 
                placeholder="Search sources..." 
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                leftIcon={<Search size={16} />}
              />
            </div>
          </div>
        </CardHeader>
        <div className="p-0">
          <table className="w-full text-sm text-left">
            <thead className="bg-surface-50 text-text-tertiary uppercase text-xs font-semibold">
              <tr>
                <th className="px-6 py-4">Source Name</th>
                <th className="px-6 py-4">Type</th>
                <th className="px-6 py-4">Size / Volume</th>
                <th className="px-6 py-4">Last Sync</th>
                <th className="px-6 py-4">Status</th>
                <th className="px-6 py-4 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-surface-100">
              {mockSources.map((source) => (
                <tr key={source.id} className="hover:bg-surface-50/50 transition-colors">
                  <td className="px-6 py-4 font-medium text-text-primary flex items-center gap-3">
                    {source.type === 'file' ? <FileText size={16} className="text-ai-500" /> : <LinkIcon size={16} className="text-primary-500" />}
                    {source.name}
                  </td>
                  <td className="px-6 py-4 text-text-secondary capitalize">{source.type}</td>
                  <td className="px-6 py-4 text-text-secondary">{source.size}</td>
                  <td className="px-6 py-4 text-text-secondary">{source.lastSync}</td>
                  <td className="px-6 py-4">
                    <Badge 
                      variant={source.status === 'synced' ? 'success' : source.status === 'error' ? 'danger' : 'default'}
                      className="capitalize"
                    >
                      {source.status}
                    </Badge>
                  </td>
                  <td className="px-6 py-4 text-right">
                    <div className="flex justify-end gap-2">
                      <Button variant="ghost" size="sm" className="h-8 w-8 p-0" title="Resync">
                        <RefreshCw size={14} className="text-text-tertiary" />
                      </Button>
                      <Button variant="ghost" size="sm" className="h-8 w-8 p-0" title="Settings">
                        <Settings size={14} className="text-text-tertiary" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
}
