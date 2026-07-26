'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Label } from '@/components/ui/Label';
import { Copy, Check } from 'lucide-react';
import { api } from '@/lib/api';

interface ServiceOption {
    id: string;
    name: string;
}

export default function WidgetGenerator() {
    const [tenantSlug, setTenantSlug] = useState('');
    const [services, setServices] = useState<ServiceOption[]>([]);
    const [selectedServiceId, setSelectedServiceId] = useState('');
    const [copied, setCopied] = useState(false);
    const [linkCopied, setLinkCopied] = useState(false);
    const [config, setConfig] = useState({
        width: '100%',
        height: '700',
        primaryColor: '#000000',
        transparent: false
    });

    useEffect(() => {
        // Resolve the tenant's booking slug from business settings.
        const fetchTenant = async () => {
            try {
                const res = await api.settings.getBusiness();
                const slug = res.data?.subdomain || localStorage.getItem('tenantSlug');
                if (slug) setTenantSlug(slug);
            } catch (e) {
                console.error(e);
                const fallback = localStorage.getItem('tenantSlug');
                if (fallback) setTenantSlug(fallback);
            }
        };

        // Load services so the tenant can build a "Book [specific service]" link.
        const fetchServices = async () => {
            try {
                const res = await api.services.list();
                const list = (res.data?.data ?? res.data ?? []) as ServiceOption[];
                setServices(list.map((s: any) => ({ id: s.id, name: s.name })));
            } catch (e) {
                console.error(e);
            }
        };

        fetchTenant();
        fetchServices();
    }, []);

    const origin = typeof window !== 'undefined' ? window.location.origin : 'https://upkilo.com';
    const serviceParam = selectedServiceId ? `&service=${selectedServiceId}` : '';

    const widgetUrl = `${origin}/book/${tenantSlug}?mode=widget&color=${encodeURIComponent(config.primaryColor)}&transparent=${config.transparent}${serviceParam}`;

    // Plain shareable link (full page, not an iframe) — for "Book Now" buttons / social bios.
    const directLink = `${origin}/book/${tenantSlug}${selectedServiceId ? `?service=${selectedServiceId}` : ''}`;

    const embedCode = `<iframe
  src="${widgetUrl}"
  width="${config.width}"
  height="${config.height}"
  frameborder="0"
  style="border:none; border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,0.1);"
></iframe>
<script>
  window.addEventListener('message', function(e) {
    if (e.data && e.data.type === 'resize-upkilo-widget') {
      const iframe = document.querySelector('iframe[src^="${widgetUrl.split('?')[0]}"]');
      if (iframe) iframe.height = e.data.height;
    }
  });
</script>`;

    const copyToClipboard = () => {
        navigator.clipboard.writeText(embedCode);
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
    };

    const copyDirectLink = () => {
        navigator.clipboard.writeText(directLink);
        setLinkCopied(true);
        setTimeout(() => setLinkCopied(false), 2000);
    };

    return (
        <div className="space-y-6">
            <Card>
                <CardHeader>
                    <CardTitle>Booking Widget</CardTitle>
                    <p className="text-sm text-gray-500">Embed the booking flow directly on your website, or share a direct link.</p>
                </CardHeader>
                <CardContent className="space-y-6">
                    <div className="grid gap-6 md:grid-cols-2">
                        {/* Configuration */}
                        <div className="space-y-4">
                            <div className="space-y-2">
                                <Label>Service</Label>
                                <select
                                    className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
                                    value={selectedServiceId}
                                    onChange={e => setSelectedServiceId(e.target.value)}
                                >
                                    <option value="">All services (let customer choose)</option>
                                    {services.map(s => (
                                        <option key={s.id} value={s.id}>{s.name}</option>
                                    ))}
                                </select>
                                <p className="text-xs text-gray-400">
                                    Pick a service to send customers straight to booking it. Leave on "All services" for the full menu.
                                </p>
                            </div>
                            <div className="space-y-2">
                                <Label>Width</Label>
                                <Input
                                    value={config.width}
                                    onChange={e => setConfig({ ...config, width: e.target.value })}
                                />
                            </div>
                            <div className="space-y-2">
                                <Label>Height (px)</Label>
                                <Input
                                    value={config.height}
                                    onChange={e => setConfig({ ...config, height: e.target.value })}
                                />
                            </div>
                            <div className="space-y-2">
                                <Label>Primary Color</Label>
                                <div className="flex gap-2">
                                    <Input
                                        type="color"
                                        className="w-12 h-10 p-1"
                                        value={config.primaryColor}
                                        onChange={e => setConfig({ ...config, primaryColor: e.target.value })}
                                    />
                                    <Input
                                        value={config.primaryColor}
                                        onChange={e => setConfig({ ...config, primaryColor: e.target.value })}
                                    />
                                </div>
                            </div>
                            <div className="flex items-center space-x-2">
                                <input
                                    type="checkbox"
                                    id="transparent"
                                    checked={config.transparent}
                                    onChange={e => setConfig({ ...config, transparent: e.target.checked })}
                                    className="rounded border-gray-300"
                                />
                                <Label htmlFor="transparent">Transparent Background</Label>
                            </div>
                        </div>

                        {/* Preview */}
                        <div className="space-y-4">
                            {/* Direct link */}
                            <div className="bg-gray-50 p-4 rounded-lg border">
                                <Label className="mb-2 block">Direct "Book Now" Link</Label>
                                <div className="flex gap-2">
                                    <Input readOnly value={directLink} className="text-sm" />
                                    <Button variant="outline" onClick={copyDirectLink} className="shrink-0">
                                        {linkCopied ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                                    </Button>
                                </div>
                                <p className="text-xs text-gray-400 mt-2">Use this for a button link, email, or social bio.</p>
                            </div>

                            {/* Embed code */}
                            <div className="bg-gray-50 p-4 rounded-lg border">
                                <Label className="mb-2 block">Embed Code (iframe)</Label>
                                <pre className="bg-gray-900 text-gray-100 p-4 rounded-md text-sm overflow-x-auto whitespace-pre-wrap break-all h-48">
                                    {embedCode}
                                </pre>
                                <Button className="w-full mt-4" onClick={copyToClipboard}>
                                    {copied ? <Check className="mr-2 h-4 w-4" /> : <Copy className="mr-2 h-4 w-4" />}
                                    {copied ? 'Copied!' : 'Copy Code'}
                                </Button>
                            </div>
                        </div>
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}
