'use client';

import { useEffect, useState } from 'react';
import { useTranslations } from 'next-intl';
import { AlertTriangle, Palette, Check, Save, Loader2, Sparkles } from 'lucide-react';
import { cn } from '@/lib/utils';
import { apiClient as api } from '@/lib/api';
import { applyTenantBrand, bestContrast, foregroundFor } from '@/lib/brand';
import { useToast } from '@/components/ui/Toast';
import { Button } from '@/components/ui/Button';
import { ThemeSelector } from '@/components/ThemeToggle';

const DEFAULT_ACCENT = '#6366f1';

export default function AppearanceSettingsPage() {
    const t = useTranslations('Theme');
    const { success: toastSuccess, error: toastError } = useToast();
    const [saving, setSaving] = useState(false);
    const [loading, setLoading] = useState(true);
    const [accentColor, setAccentColor] = useState(DEFAULT_ACCENT);
    // What the server currently holds, so the Save button can say whether there is anything
    // to save rather than always looking actionable.
    const [savedAccent, setSavedAccent] = useState(DEFAULT_ACCENT);

    // The accent is the tenant's white-label brand colour, the same field the Branding page
    // edits — it is stored per tenant, not per device like the theme above it. This page used
    // to hold it in local state and "save" it with a setTimeout that resolved and reported
    // success, so the picker moved a highlight and changed nothing, on either screen.
    useEffect(() => {
        let cancelled = false;
        (async () => {
            try {
                const res = await api.get('/api/v1/whitelabel');
                const cfg = res.data?.data || res.data || {};
                if (cancelled) return;
                const colour = cfg.primaryColor || DEFAULT_ACCENT;
                setAccentColor(colour);
                setSavedAccent(colour);
            } catch {
                // No white-label config yet — the default stands.
            } finally {
                if (!cancelled) setLoading(false);
            }
        })();
        return () => { cancelled = true; };
    }, []);

    const handleSave = async () => {
        setSaving(true);
        try {
            // PATCH-style merge: the Branding page owns logo, domain and custom CSS on this
            // same record, and sending only the colour would blank them.
            const current = await api.get('/api/v1/whitelabel');
            const cfg = current.data?.data || current.data || {};
            await api.put('/api/v1/whitelabel', { ...cfg, primaryColor: accentColor });
            setSavedAccent(accentColor);
            applyTenantBrand(accentColor);
            toastSuccess(t('accentSaved'));
        } catch {
            toastError(t('accentSaveFailed'));
        } finally {
            setSaving(false);
        }
    };

    const accents = [
        { color: DEFAULT_ACCENT, key: 'colourIndigo' },
        { color: '#8b5cf6', key: 'colourViolet' },
        { color: '#0ea5e9', key: 'colourCyan' },
        { color: '#10b981', key: 'colourEmerald' },
        { color: '#f59e0b', key: 'colourAmber' },
        { color: '#ef4444', key: 'colourRose' },
    ];

    const dirty = accentColor !== savedAccent;

    return (
        <div className="max-w-4xl mx-auto space-y-12 animate-fade-in pb-20">
            <div className="flex items-center gap-6 mb-12">
                <div className="p-4 bg-primary rounded-[28px] border border-primary/20">
                    <Palette className="h-8 w-8 text-primary-foreground" />
                </div>
                <div>
                    <h1 className="text-3xl font-bold text-foreground tracking-tight">{t('appearance')}</h1>
                    <p className="text-sm text-foreground-muted mt-1">
                        {t('appearanceSubtitle')}
                    </p>
                </div>
            </div>

            <div className="p-10 bg-card border border-border rounded-[40px] shadow-[var(--shadow-card)] space-y-12">
                <div className="space-y-6">
                    <div className="flex items-center gap-4">
                        <div className="h-10 w-1 rounded-full bg-primary" />
                        <div>
                            <h2 className="text-sm font-bold text-foreground uppercase tracking-[0.2em]">{t('themeSection')}</h2>
                            {/*
                              The picker used to be `useState('dark')` with no connection to
                              the theme system at all: clicking an option moved a highlight and
                              nothing else, and "Commit Schema" ran a setTimeout and claimed
                              success. The app's own settings screen was the one place a user
                              would go to change the theme, and it was the one control that
                              could not.

                              ThemeSelector writes through the provider, so the change lands on
                              the first frame and persists — there is nothing left to save,
                              which is why this section sits outside the Save button below.
                            */}
                            <p className="text-xs text-foreground-muted mt-1 normal-case tracking-normal">
                                {t('themeHint')}
                            </p>
                        </div>
                    </div>

                    <ThemeSelector />
                </div>

                <div className="space-y-6">
                    <div className="flex items-center gap-4">
                        <div className="h-10 w-1 rounded-full bg-success-500" />
                        <h2 className="text-sm font-bold text-foreground uppercase tracking-[0.2em]">{t('accentColour')}</h2>
                    </div>

                    <div
                        role="radiogroup"
                        aria-label={t('accentColour')}
                        aria-busy={loading}
                        className="flex flex-wrap items-center gap-6 p-8 bg-muted rounded-[32px] border border-border"
                    >
                        {accents.map((accent) => {
                            const selected = accentColor === accent.color;
                            return (
                                <button
                                    key={accent.color}
                                    type="button"
                                    role="radio"
                                    aria-checked={selected}
                                    onClick={() => setAccentColor(accent.color)}
                                    aria-label={t(accent.key)}
                                    className={cn(
                                        'w-14 h-14 rounded-[20px] border-4 transition-all duration-200 relative group overflow-hidden',
                                        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-card',
                                        selected
                                            ? 'border-foreground scale-110 shadow-[var(--shadow-popover)]'
                                            : 'border-transparent hover:scale-110'
                                    )}
                                    style={{ backgroundColor: accent.color }}
                                    title={t(accent.key)}
                                >
                                    {selected && (
                                        // The tick is drawn in whatever reads on this swatch, not
                                        // in white — the amber and emerald options are light
                                        // enough that a white tick disappeared into them.
                                        <span
                                            className="absolute inset-0 flex items-center justify-center"
                                            style={{ color: foregroundFor(accent.color) }}
                                        >
                                            <Check className="h-6 w-6" aria-hidden="true" />
                                        </span>
                                    )}
                                </button>
                            );
                        })}

                        {/* A preview of the pair the tenant is actually choosing: their fill with
                            the label colour derived from it. Six swatches cannot show whether a
                            button will be readable; this can. */}
                        <span
                            className="ms-auto inline-flex h-14 items-center rounded-[20px] px-5 text-sm font-semibold"
                            style={{ backgroundColor: accentColor, color: foregroundFor(accentColor) }}
                        >
                            {t('accentPreview')}
                        </span>
                    </div>

                    {/* Some perfectly reasonable brand colours cannot clear 4.5:1 against either
                        white or black — indigo-500 tops out at 4.45:1. That is a property of the
                        hue, not something a better foreground pick can solve, so say it plainly
                        rather than shipping a button whose label fails AA on every booking page
                        the tenant sends out. */}
                    {bestContrast(accentColor) < 4.5 && (
                        <p
                            role="status"
                            className="flex items-start gap-3 rounded-xl border border-warning-border bg-warning-surface px-4 py-3 text-xs text-warning-fg"
                        >
                            <AlertTriangle className="h-4 w-4 shrink-0 mt-0.5" aria-hidden="true" />
                            <span>{t('contrastWarning', { ratio: bestContrast(accentColor).toFixed(1) })}</span>
                        </p>
                    )}

                    <div className="flex items-start gap-3 px-2">
                        <Sparkles className="h-4 w-4 shrink-0 text-primary mt-0.5" aria-hidden="true" />
                        <p className="text-xs text-foreground-muted">
                            {t.rich('accentHelp', {
                                link: (chunks) => (
                                    <a href="/settings/branding" className="text-primary underline underline-offset-2">
                                        {chunks}
                                    </a>
                                ),
                            })}
                        </p>
                    </div>
                </div>
            </div>

            <div className="flex flex-col md:flex-row items-center justify-end gap-8 pt-10 border-t border-border">
                <Button
                    onClick={handleSave}
                    disabled={saving || loading || !dirty}
                    size="lg"
                    className="px-12"
                    leftIcon={saving ? <Loader2 className="h-5 w-5 animate-spin" /> : <Save className="h-5 w-5" />}
                >
                    {saving ? t('saving') : dirty ? t('saveAccent') : t('saved')}
                </Button>
            </div>
        </div>
    );
}
