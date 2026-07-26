'use client';

import React, { useEffect, useState } from 'react';
import DOMPurify from 'isomorphic-dompurify';

interface PluginSlotProps {
  slotId: string;           // e.g. "dashboard.header", "booking.sidebar", "client.actions"
  tenantId?: string;
  context?: Record<string, unknown>;
  fallback?: React.ReactNode;
}

interface SlotPlugin {
  pluginId: string;
  componentHtml?: string;
  webhookUrl?: string;
  order: number;
}

export function PluginSlot({ slotId, tenantId, context, fallback }: PluginSlotProps) {
  const [plugins, setPlugins] = useState<SlotPlugin[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchSlotPlugins = async () => {
      try {
        const res = await fetch(`/api/plugins/slots/${slotId}${tenantId ? `?tenantId=${tenantId}` : ''}`);
        if (res.ok) {
          const data = await res.json();
          setPlugins(data.plugins || []);
        }
      } catch {
        // fail silently — plugins are additive
      } finally {
        setLoading(false);
      }
    };
    fetchSlotPlugins();
  }, [slotId, tenantId]);

  if (loading) return null;
  if (plugins.length === 0) return <>{fallback}</>;

  return (
    <div className="plugin-slot" data-slot-id={slotId}>
      {plugins
        .sort((a, b) => a.order - b.order)
        .map((plugin) => (
          <div key={plugin.pluginId} className="plugin-slot__item" data-plugin-id={plugin.pluginId}>
            {plugin.componentHtml ? (
              <div dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(plugin.componentHtml) }} />
            ) : null}
          </div>
        ))}
    </div>
  );
}
