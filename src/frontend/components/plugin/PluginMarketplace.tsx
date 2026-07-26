'use client';

import React, { useEffect, useState } from 'react';

interface Plugin {
  id: string;
  name: string;
  description: string;
  category: string;
  price: number;
  isFree: boolean;
  installCount: number;
  rating: number;
  isVerified: boolean;
  isInstalled?: boolean;
}

interface PluginMarketplaceProps {
  tenantId: string;
}

export function PluginMarketplace({ tenantId }: PluginMarketplaceProps) {
  const [plugins, setPlugins] = useState<Plugin[]>([]);
  const [installed, setInstalled] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(true);
  const [installing, setInstalling] = useState<string | null>(null);
  const [filter, setFilter] = useState('All');

  const categories = ['All', 'Finance', 'Marketing', 'Analytics', 'Automation', 'CRM'];

  useEffect(() => {
    const loadData = async () => {
      try {
        const [defRes, instRes] = await Promise.all([
          fetch('/api/plugins/marketplace'),
          fetch(`/api/plugins/installed?tenantId=${tenantId}`)
        ]);
        const defs = defRes.ok ? await defRes.json() : [];
        const inst = instRes.ok ? await instRes.json() : [];
        setPlugins(defs);
        setInstalled(new Set(inst.map((i: { slug: string }) => i.slug)));
      } finally {
        setLoading(false);
      }
    };
    loadData();
  }, [tenantId]);

  const handleInstall = async (slug: string) => {
    setInstalling(slug);
    try {
      const res = await fetch(`/api/plugins/install`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ tenantId, pluginId: slug })
      });
      if (res.ok) {
        setInstalled(prev => new Set([...prev, slug]));
      }
    } finally {
      setInstalling(null);
    }
  };

  const filtered = filter === 'All' ? plugins : plugins.filter(p => p.category === filter);

  if (loading) return <div className="text-center p-8">Loading marketplace...</div>;

  return (
    <div className="plugin-marketplace">
      <div className="flex gap-2 mb-6">
        {categories.map(cat => (
          <button
            key={cat}
            onClick={() => setFilter(cat)}
            className={`px-4 py-2 rounded-full text-sm font-medium ${
              filter === cat ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
            }`}
          >
            {cat}
          </button>
        ))}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {filtered.map(plugin => (
          <div key={plugin.id} className="border rounded-xl p-5 bg-white shadow-sm">
            <div className="flex justify-between items-start mb-3">
              <div>
                <h3 className="font-semibold text-gray-900">{plugin.name}</h3>
                <span className="text-xs text-gray-500 bg-gray-100 px-2 py-0.5 rounded">{plugin.category}</span>
              </div>
              {plugin.isVerified && (
                <span className="text-xs text-green-600 bg-green-50 px-2 py-1 rounded">✓ Verified</span>
              )}
            </div>
            <p className="text-sm text-gray-600 mb-4 line-clamp-2">{plugin.description}</p>
            <div className="flex justify-between items-center">
              <div className="text-sm text-gray-500">
                ⭐ {plugin.rating.toFixed(1)} · {plugin.installCount.toLocaleString()} installs
              </div>
              {installed.has(plugin.id) ? (
                <span className="text-sm text-green-600 font-medium">✓ Installed</span>
              ) : (
                <button
                  onClick={() => handleInstall(plugin.id)}
                  disabled={installing === plugin.id}
                  className="px-4 py-2 bg-blue-600 text-white text-sm rounded-lg hover:bg-blue-700 disabled:opacity-50"
                >
                  {installing === plugin.id ? 'Installing...' : plugin.isFree ? 'Install Free' : `$${plugin.price}/mo`}
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
