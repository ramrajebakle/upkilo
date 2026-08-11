"use client";

import React, { useState, useEffect } from "react";
import { 
  Puzzle, Download, Terminal, ToyBrick, 
  CheckCircle, Globe, Settings2, Trash2, Loader2
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { useToast } from "@/components/ui/Toast";
import api, { apiClient } from "@/lib/api";

export default function PluginMarketplacePage() {
  const { success, error: toastError } = useToast();
  const [loading, setLoading] = useState(true);
  const [installing, setInstalling] = useState<string | null>(null);
  const [plugins, setPlugins] = useState<any[]>([]);

  const fetchPlugins = async () => {
    try {
      // Reusing Integrations API for general app/plugin catalog
      const res = await apiClient.get('/api/v1/integrations');
      if (res.data?.data) {
          // Filter out core integrations (payment, calendar) and show others as "Extensions"
          // Or just show all as "Plugins" for the marketplace view.
        setPlugins(res.data.data);
      }
    } catch (err) {
      toastError("Failed to load plugin catalog.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPlugins();
  }, []);

  const handleInstall = async (pluginId: string) => {
    setInstalling(pluginId);
    try {
      await apiClient.post(`/api/v1/integrations/${pluginId}/connect`);
      success("Plugin installed and UI extensions registered.");
      await fetchPlugins();
    } catch (err) {
      toastError("Installation failed.");
    } finally {
      setInstalling(null);
    }
  };

  if (loading) {
      return (
          <div className="flex flex-col items-center justify-center py-20">
              <Loader2 className="h-10 w-10 text-primary animate-spin mb-4" />
              <p className="text-gray-500">Scanning Marketplace...</p>
          </div>
      );
  }

  return (
    <div className="space-y-8 max-w-6xl">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Plugin Marketplace</h1>
        <p className="text-muted-foreground">Extend Upkilo with community-built plugins and UI extensions.</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {plugins.map((pWrapper: any) => {
          const p = pWrapper.item;
          return (
          <Card key={p.id} className="flex flex-col relative overflow-hidden group hover:border-primary/50 transition-colors">
            <CardHeader className="pb-4 border-b">
              <div className="flex justify-between items-start mb-4">
                <div className="w-12 h-12 bg-gray-100 rounded-xl flex items-center justify-center">
                  <ToyBrick className="h-6 w-6 text-primary-500" />
                </div>
                {pWrapper.isConnected && (
                  <span className="text-[10px] font-bold uppercase tracking-wider text-green-700 bg-green-100 px-2 py-1 rounded">
                    Installed
                  </span>
                )}
              </div>
              <CardTitle className="text-lg">{p.name}</CardTitle>
              <CardDescription className="text-xs uppercase font-bold tracking-tighter text-gray-400">{p.category}</CardDescription>
            </CardHeader>
            <CardContent className="pt-4 flex-1 flex flex-col justify-between">
               <div className="space-y-2 mb-6">
                 <p className="text-sm text-gray-500 leading-relaxed min-h-[40px]">{p.description}</p>
                 <div className="flex flex-wrap gap-2 pt-2">
                    {p.features?.map((f: string) => (
                        <span key={f} className="text-[10px] px-2 py-0.5 bg-gray-50 text-gray-500 rounded-full border border-gray-100">
                            {f}
                        </span>
                    ))}
                 </div>
               </div>
               
               {pWrapper.isConnected ? (
                  <div className="flex gap-2 w-full mt-auto">
                     <Button variant="outline" className="flex-1"><Settings2 className="h-4 w-4 mr-2" /> Configure</Button>
                     <Button variant="ghost" className="text-red-500 hover:text-red-600 hover:bg-red-50"><Trash2 className="h-4 w-4" /></Button>
                  </div>
               ) : (
                  <Button 
                    className="w-full mt-auto" 
                    onClick={() => handleInstall(p.id)}
                    loading={installing === p.id}
                  >
                    <Download className="h-4 w-4 mr-2" /> Install Plugin
                  </Button>
               )}
            </CardContent>
          </Card>
        )})}
        
        {/* Developer CTA */}
        <Card className="flex flex-col items-center justify-center text-center p-6 border-dashed bg-gray-50 hover:bg-gray-100 transition-all cursor-pointer group">
           <Terminal className="h-10 w-10 text-gray-400 mb-4 group-hover:scale-110 transition-transform" />
           <CardTitle>Build a Plugin</CardTitle>
           <CardDescription className="max-w-[200px] mt-2 mb-4 text-xs font-medium">
             Use our CLI and UI slots API to build your own private extensions.
           </CardDescription>
           <Button variant="outline" size="sm" onClick={() => window.location.href = '/en/developers'}>Read the Docs</Button>
        </Card>
      </div>
    </div>
  );
}
