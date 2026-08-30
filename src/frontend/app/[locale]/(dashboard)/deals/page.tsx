"use client";

import React, { useState, useEffect, useCallback } from 'react';
import { Plus, DollarSign, Loader2 } from 'lucide-react';
import { KanbanColumn } from '@/components/deals/KanbanColumn';
import api from '@/lib/api';

interface Deal {
  id: string;
  title: string;
  value: number;
  clientName: string;
}

interface PipelineStage {
  id: string;
  title: string;
  deals: Deal[];
}

export default function DealsPage() {
  const [stages, setStages] = useState<PipelineStage[]>([]);
  const [loading, setLoading] = useState(true);

  const loadPipeline = useCallback(async () => {
    try {
      setLoading(true);
      const [stagesRes, dealsRes] = await Promise.all([
        api.deals.stages(),
        api.deals.list()
      ]);

      const stageData = stagesRes.data?.data || stagesRes.data || [];
      const dealData = dealsRes.data?.data || dealsRes.data || [];

      // Map deals into their respective stages
      const mapped = (Array.isArray(stageData) ? stageData : []).map((stage: any) => ({
        id: stage.id,
        title: stage.name || stage.title || 'Untitled',
        deals: dealData
          .filter((d: any) => d.stageId === stage.id)
          .map((d: any) => ({
            id: d.id,
            title: d.title || d.name,
            value: d.value || d.amount || 0,
            clientName: d.clientName || d.client?.name || 'Unknown',
          }))
      }));

      setStages(mapped.length > 0 ? mapped : [
        { id: 'default-1', title: 'Lead In', deals: [] },
        { id: 'default-2', title: 'Contact Made', deals: [] },
        { id: 'default-3', title: 'Proposal Sent', deals: [] },
        { id: 'default-4', title: 'Won', deals: [] },
      ]);
    } catch (err) {
      console.error('Failed to load pipeline:', err);
      // Fallback to empty stages on error
      setStages([
        { id: 'default-1', title: 'Lead In', deals: [] },
        { id: 'default-2', title: 'Contact Made', deals: [] },
        { id: 'default-3', title: 'Proposal Sent', deals: [] },
        { id: 'default-4', title: 'Won', deals: [] },
      ]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { loadPipeline(); }, [loadPipeline]);

  const totalValue = stages.reduce((sum, s) => sum + s.deals.reduce((ds, d) => ds + d.value, 0), 0);
  const totalDeals = stages.reduce((sum, s) => sum + s.deals.length, 0);

  return (
    <div className="flex flex-col h-full space-y-6">
      <div className="flex justify-between items-center flex-wrap gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Sales Pipeline</h1>
          <p className="text-muted-foreground">
            Track and manage your deals through the sales lifecycle.
            {!loading && <span className="ml-2 text-sm text-foreground-secondary">({totalDeals} deals · ${totalValue.toLocaleString()} total value)</span>}
          </p>
        </div>
        <button className="inline-flex items-center gap-2 px-4 py-2.5 bg-primary text-primary-foreground rounded-xl font-medium hover:bg-primary/90 transition-colors">
          <Plus className="w-4 h-4" /> New Deal
        </button>
      </div>

      {loading ? (
        <div className="flex items-center justify-center flex-1">
          <Loader2 className="w-8 h-8 text-primary animate-spin" />
        </div>
      ) : (
        <div className="flex-1 overflow-x-auto pb-4">
          <div className="flex items-start gap-4 h-full min-w-max">
            {stages.map(stage => (
              <KanbanColumn
                key={stage.id}
                title={stage.title}
                deals={stage.deals}
              />
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
