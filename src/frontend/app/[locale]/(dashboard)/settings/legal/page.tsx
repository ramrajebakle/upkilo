"use client";

import { useState, useEffect } from 'react';
import { Save, FileText, Shield } from 'lucide-react';
import { apiClient } from '@/lib/api';

export default function LegalSettingsPage() {
    const [termsText, setTermsText] = useState('');
    const [privacyText, setPrivacyText] = useState('');
    const [loading, setLoading] = useState(false);
    const [saving, setSaving] = useState(false);

    useEffect(() => {
        const fetchLegalDocs = async () => {
            setLoading(true);
            try {
                // Adjust to the actual LegalController endpoint
                const termsReq = await apiClient.get('/api/v1/legal/terms');
                const privacyReq = await apiClient.get('/api/v1/legal/privacy');
                
                setTermsText(termsReq.data?.content || '');
                setPrivacyText(privacyReq.data?.content || '');
            } catch (err) {
                console.error('Error fetching legal documents', err);
            } finally {
                setLoading(false);
            }
        };
        fetchLegalDocs();
    }, []);

    const handleSave = async () => {
        setSaving(true);
        try {
            await apiClient.put('/api/v1/legal/terms', { content: termsText });
            await apiClient.put('/api/v1/legal/privacy', { content: privacyText });
            alert('Legal documents saved successfully.');
        } catch (err) {
            console.error('Error saving legal documents', err);
            alert('Failed to save legal documents.');
        } finally {
            setSaving(false);
        }
    };

    return (
        <div className="max-w-4xl space-y-8">
            <div>
                <h1 className="text-2xl font-bold text-foreground" style={{ fontFamily: 'var(--font-display)' }}>
                    Legal & Compliance
                </h1>
                <p className="text-sm text-foreground-secondary mt-1">
                    Manage your public Terms of Service and Privacy Policy.
                </p>
            </div>

            {loading ? (
                <div className="p-8 text-center text-foreground-secondary">Loading legal documents...</div>
            ) : (
                <div className="space-y-8">
                    {/* Terms of Service */}
                    <div className="card-elevated p-6">
                        <div className="flex items-center gap-3 border-b border-border-subtle pb-4 mb-4">
                            <div className="p-2 bg-blue-50 text-blue-600 rounded-lg">
                                <FileText className="h-5 w-5" />
                            </div>
                            <div>
                                <h3 className="text-lg font-bold text-foreground">Terms of Service</h3>
                                <p className="text-sm text-foreground-secondary">Rules users must agree to in order to use your service.</p>
                            </div>
                        </div>
                        <textarea
                            className="w-full h-64 p-4 rounded-xl border border-border focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 outline-none transition-all resize-none text-foreground font-mono text-sm"
                            placeholder="Enter your Terms of Service here (Markdown supported)..."
                            value={termsText}
                            onChange={(e) => setTermsText(e.target.value)}
                        />
                    </div>

                    {/* Privacy Policy */}
                    <div className="card-elevated p-6">
                        <div className="flex items-center gap-3 border-b border-border-subtle pb-4 mb-4">
                            <div className="p-2 bg-emerald-50 text-emerald-600 rounded-lg">
                                <Shield className="h-5 w-5" />
                            </div>
                            <div>
                                <h3 className="text-lg font-bold text-foreground">Privacy Policy</h3>
                                <p className="text-sm text-foreground-secondary">Details on how you collect, use, and protect user data.</p>
                            </div>
                        </div>
                        <textarea
                            className="w-full h-64 p-4 rounded-xl border border-border focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 outline-none transition-all resize-none text-foreground font-mono text-sm"
                            placeholder="Enter your Privacy Policy here (Markdown supported)..."
                            value={privacyText}
                            onChange={(e) => setPrivacyText(e.target.value)}
                        />
                    </div>

                    <div className="flex justify-end pt-4">
                        <button
                            onClick={handleSave}
                            disabled={saving}
                            className="btn btn-primary"
                        >
                            <Save className="h-4 w-4 mr-2" />
                            {saving ? 'Saving...' : 'Save Documents'}
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}
