'use client';

import { useState, useEffect, useRef, useCallback } from 'react';

// ─── Types ────────────────────────────────────────────────────────────────────

export interface ConsentState {
    essential: true;
    analytics: boolean;
    marketing: boolean;
    functional: boolean;
}

export interface StoredConsent extends ConsentState {
    version: string;
    timestamp: string;
}

// Increment when the privacy policy / cookie list changes materially.
// Existing users whose stored version differs will be re-prompted.
const CONSENT_VERSION = '2.0';
const CONSENT_KEY = 'upkilo-consent';

// ─── Storage helpers ──────────────────────────────────────────────────────────

export function getStoredConsent(): StoredConsent | null {
    try {
        if (typeof window === 'undefined') return null;
        const raw = localStorage.getItem(CONSENT_KEY);
        if (!raw) return null;
        const parsed = JSON.parse(raw) as StoredConsent;
        // Re-prompt if policy version changed
        if (parsed.version !== CONSENT_VERSION) return null;
        return parsed;
    } catch {
        return null;
    }
}

function persistConsent(consent: ConsentState): StoredConsent {
    const record: StoredConsent = {
        ...consent,
        version: CONSENT_VERSION,
        timestamp: new Date().toISOString(),
    };
    localStorage.setItem(CONSENT_KEY, JSON.stringify(record));
    // Dispatch event so useConsent hooks in other components update
    window.dispatchEvent(new Event('consent-updated'));
    return record;
}

async function syncToBackend(consent: ConsentState) {
    try {
        await fetch('/api/v1/legal/cookie-consent', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ essential: true, analytics: consent.analytics, marketing: consent.marketing }),
        });
    } catch {
        // Non-critical — localStorage is the source of truth for script gating
    }
}

// ─── Category metadata ────────────────────────────────────────────────────────

const CATEGORIES = [
    {
        key: 'essential' as const,
        label: 'Essential',
        required: true,
        description: 'Required for the platform to function. Includes authentication tokens, session management, CSRF protection, and timezone detection. Cannot be disabled.',
        examples: 'auth_token, session_id, csrf_token, timezone',
        retention: 'Session or up to 1 year',
        thirdParties: 'None',
    },
    {
        key: 'functional' as const,
        required: false,
        label: 'Functional',
        description: 'Remembers your preferences such as dark/light mode and language selection to personalise your experience.',
        examples: 'theme, language, ui_preferences',
        retention: '1 year',
        thirdParties: 'None',
    },
    {
        key: 'analytics' as const,
        required: false,
        label: 'Analytics',
        description: 'Helps us understand how users interact with the platform so we can improve features and fix problems. Data is aggregated and anonymised.',
        examples: 'usage_analytics, feature_tracking, session_recording',
        retention: '90 days',
        thirdParties: 'Internal analytics only',
    },
    {
        key: 'marketing' as const,
        required: false,
        label: 'Marketing',
        description: 'Used to measure the effectiveness of marketing campaigns and to show relevant communications. You can opt out at any time.',
        examples: 'campaign_tracking, referral_source, utm_params',
        retention: '30 days',
        thirdParties: 'None currently',
    },
] as const;

// ─── Component ────────────────────────────────────────────────────────────────

export function CookieConsent() {
    const [visible, setVisible] = useState(false);
    const [showPreferences, setShowPreferences] = useState(false);
    const [expandedCategory, setExpandedCategory] = useState<string | null>(null);
    const [prefs, setPrefs] = useState<ConsentState>({
        essential: true,
        analytics: false,
        marketing: false,
        functional: false,
    });

    const bannerRef = useRef<HTMLDivElement>(null);
    const declineBtnRef = useRef<HTMLButtonElement>(null);

    useEffect(() => {
        // Show immediately — no delay. A 1500ms delay allows users to "continue"
        // before seeing the banner, which regulators treat as implicit consent.
        if (!getStoredConsent()) {
            setVisible(true);
        }

        // Re-open with preferences panel when ManageCookiesButton fires this event
        const handleReopen = () => {
            const stored = getStoredConsent();
            if (stored) {
                setPrefs({ essential: true, analytics: stored.analytics, marketing: stored.marketing, functional: stored.functional });
            }
            setShowPreferences(true);
            setVisible(true);
        };
        window.addEventListener('manage-cookies-open', handleReopen);
        return () => window.removeEventListener('manage-cookies-open', handleReopen);
    }, []);

    // Move keyboard focus into the banner as soon as it mounts
    useEffect(() => {
        if (visible && declineBtnRef.current) {
            declineBtnRef.current.focus();
        }
    }, [visible]);

    // Trap focus inside banner while it is open
    const handleKeyDown = useCallback((e: React.KeyboardEvent) => {
        if (e.key === 'Escape') {
            // Pressing Escape = Decline All (safest default)
            handleDeclineAll();
        }
        if (e.key === 'Tab' && bannerRef.current) {
            const focusable = bannerRef.current.querySelectorAll<HTMLElement>(
                'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
            );
            const first = focusable[0];
            const last = focusable[focusable.length - 1];
            if (e.shiftKey && document.activeElement === first) {
                e.preventDefault();
                last.focus();
            } else if (!e.shiftKey && document.activeElement === last) {
                e.preventDefault();
                first.focus();
            }
        }
    }, []); // eslint-disable-line react-hooks/exhaustive-deps

    const handleAcceptAll = async () => {
        const consent: ConsentState = { essential: true, analytics: true, marketing: true, functional: true };
        persistConsent(consent);
        await syncToBackend(consent);
        setVisible(false);
    };

    const handleDeclineAll = async () => {
        const consent: ConsentState = { essential: true, analytics: false, marketing: false, functional: false };
        persistConsent(consent);
        await syncToBackend(consent);
        setVisible(false);
    };

    const handleSavePreferences = async () => {
        persistConsent(prefs);
        await syncToBackend(prefs);
        setShowPreferences(false);
        setVisible(false);
    };

    const toggleCategory = (key: keyof Omit<ConsentState, 'essential'>) => {
        setPrefs(p => ({ ...p, [key]: !p[key] }));
    };

    if (!visible) return null;

    return (
        // Role="dialog" + aria-modal tells screen readers this is a blocking dialog
        <div
            ref={bannerRef}
            role="dialog"
            aria-modal="true"
            aria-label="Cookie consent preferences"
            aria-describedby="cookie-consent-description"
            onKeyDown={handleKeyDown}
            style={{
                position: 'fixed',
                bottom: 0,
                left: 0,
                right: 0,
                zIndex: 9999,
                animation: 'ck-slideUp 0.3s ease-out',
            }}
        >
            <style>{`
                @keyframes ck-slideUp {
                    from { transform: translateY(100%); opacity: 0; }
                    to   { transform: translateY(0);     opacity: 1; }
                }
                .ck-toggle { position: relative; display: inline-flex; align-items: center; cursor: pointer; }
                .ck-toggle input { opacity: 0; width: 0; height: 0; position: absolute; }
                .ck-slider {
                    width: 40px; height: 22px; background: #334155;
                    border-radius: 11px; transition: background 0.2s;
                    display: flex; align-items: center; padding: 0 3px;
                }
                .ck-toggle input:checked + .ck-slider { background: #6366f1; }
                .ck-slider::after {
                    content: ''; width: 16px; height: 16px; background: white;
                    border-radius: 50%; transition: transform 0.2s; transform: translateX(0);
                }
                .ck-toggle input:checked + .ck-slider::after { transform: translateX(18px); }
                .ck-toggle input:focus-visible + .ck-slider { outline: 2px solid #818cf8; outline-offset: 2px; }
                .ck-category-btn { text-align: left; background: none; border: none; color: #e2e8f0;
                    font-size: 0.875rem; cursor: pointer; padding: 0; display: flex; align-items: center; gap: 6px; }
                .ck-category-btn:focus-visible { outline: 2px solid #818cf8; outline-offset: 2px; border-radius: 4px; }
            `}</style>

            <div
                style={{
                    background: 'rgba(15, 23, 42, 0.97)',
                    backdropFilter: 'blur(20px)',
                    borderTop: '1px solid rgba(99, 102, 241, 0.3)',
                    color: '#cbd5e1',
                    fontSize: '0.875rem',
                }}
            >
                {/* ── Preference centre (expanded) ─────────────────────────── */}
                {showPreferences && (
                    <div style={{ maxWidth: '1200px', margin: '0 auto', padding: '1.5rem' }}>
                        <h2
                            id="cookie-consent-description"
                            style={{ color: '#f1f5f9', fontWeight: 700, fontSize: '1.1rem', marginBottom: '0.25rem' }}
                        >
                            Manage Cookie Preferences
                        </h2>
                        <p style={{ color: '#94a3b8', marginBottom: '1.25rem', fontSize: '0.8rem' }}>
                            Choose which cookies you allow. Essential cookies cannot be disabled as the site requires them to function.
                            See our{' '}
                            <a href="/cookie-policy" style={{ color: '#818cf8' }}>Cookie Policy</a>
                            {' '}and{' '}
                            <a href="/privacy-policy" style={{ color: '#818cf8' }}>Privacy Policy</a>.
                        </p>

                        <div style={{ display: 'flex', flexDirection: 'column', gap: '0.75rem', marginBottom: '1.25rem' }}>
                            {CATEGORIES.map(cat => (
                                <div
                                    key={cat.key}
                                    style={{
                                        background: 'rgba(30, 41, 59, 0.8)',
                                        border: '1px solid rgba(100, 116, 139, 0.2)',
                                        borderRadius: '8px',
                                        padding: '0.875rem 1rem',
                                    }}
                                >
                                    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '1rem' }}>
                                        <div style={{ flex: 1 }}>
                                            <button
                                                className="ck-category-btn"
                                                aria-expanded={expandedCategory === cat.key}
                                                aria-controls={`ck-details-${cat.key}`}
                                                onClick={() => setExpandedCategory(expandedCategory === cat.key ? null : cat.key)}
                                            >
                                                <span style={{ fontWeight: 600, color: '#e2e8f0' }}>{cat.label}</span>
                                                <span style={{ fontSize: '0.7rem', color: '#64748b', transform: expandedCategory === cat.key ? 'rotate(180deg)' : 'none', transition: 'transform 0.15s' }}>▼</span>
                                            </button>
                                            <p style={{ margin: '0.25rem 0 0', color: '#94a3b8', fontSize: '0.78rem', lineHeight: 1.5 }}>
                                                {cat.description}
                                            </p>
                                        </div>

                                        {cat.required ? (
                                            <span style={{ fontSize: '0.72rem', color: '#64748b', whiteSpace: 'nowrap', background: 'rgba(100,116,139,0.15)', padding: '2px 8px', borderRadius: '12px' }}>
                                                Always on
                                            </span>
                                        ) : (
                                            <label
                                                className="ck-toggle"
                                                aria-label={`${cat.label} cookies: ${prefs[cat.key as keyof Omit<ConsentState, 'essential'>] ? 'enabled' : 'disabled'}`}
                                            >
                                                <input
                                                    type="checkbox"
                                                    checked={prefs[cat.key as keyof Omit<ConsentState, 'essential'>]}
                                                    onChange={() => toggleCategory(cat.key as keyof Omit<ConsentState, 'essential'>)}
                                                />
                                                <span className="ck-slider" />
                                            </label>
                                        )}
                                    </div>

                                    {expandedCategory === cat.key && (
                                        <div
                                            id={`ck-details-${cat.key}`}
                                            role="region"
                                            style={{
                                                marginTop: '0.75rem',
                                                paddingTop: '0.75rem',
                                                borderTop: '1px solid rgba(100,116,139,0.15)',
                                                display: 'grid',
                                                gridTemplateColumns: 'repeat(auto-fit, minmax(160px, 1fr))',
                                                gap: '0.5rem',
                                            }}
                                        >
                                            {[
                                                { label: 'Examples', value: cat.examples },
                                                { label: 'Retention', value: cat.retention },
                                                { label: 'Third parties', value: cat.thirdParties },
                                            ].map(({ label, value }) => (
                                                <div key={label}>
                                                    <span style={{ fontSize: '0.72rem', color: '#64748b', textTransform: 'uppercase', letterSpacing: '0.04em' }}>{label}</span>
                                                    <p style={{ margin: '2px 0 0', color: '#94a3b8', fontSize: '0.78rem' }}>{value}</p>
                                                </div>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            ))}
                        </div>

                        {/* Equal-prominence action row */}
                        <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap', justifyContent: 'flex-end' }}>
                            <button onClick={handleDeclineAll} style={btnOutline}>Reject All</button>
                            <button onClick={handleAcceptAll} style={btnOutline}>Accept All</button>
                            <button onClick={handleSavePreferences} style={btnPrimary}>Save Preferences</button>
                        </div>
                    </div>
                )}

                {/* ── Compact banner ───────────────────────────────────────── */}
                {!showPreferences && (
                    <div
                        style={{
                            maxWidth: '1200px',
                            margin: '0 auto',
                            padding: '1rem 1.5rem',
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'space-between',
                            gap: '1.5rem',
                            flexWrap: 'wrap',
                        }}
                    >
                        <p id="cookie-consent-description" style={{ margin: 0, flex: 1, minWidth: '260px', lineHeight: 1.5 }}>
                            We use essential cookies to make the platform work. With your consent, we also use optional cookies for analytics and personalisation.{' '}
                            <a href="/cookie-policy" style={{ color: '#818cf8', textDecoration: 'underline' }}>Cookie Policy</a>
                            {' · '}
                            <a href="/privacy-policy" style={{ color: '#818cf8', textDecoration: 'underline' }}>Privacy Policy</a>
                        </p>

                        {/* Three buttons with EQUAL prominence — GDPR requirement */}
                        <div style={{ display: 'flex', gap: '0.5rem', flexShrink: 0, flexWrap: 'wrap' }}>
                            <button
                                ref={declineBtnRef}
                                onClick={handleDeclineAll}
                                aria-label="Reject all optional cookies"
                                style={btnOutline}
                            >
                                Reject All
                            </button>
                            <button
                                onClick={() => setShowPreferences(true)}
                                aria-label="Customise cookie preferences"
                                style={btnOutline}
                            >
                                Customise
                            </button>
                            <button
                                onClick={handleAcceptAll}
                                aria-label="Accept all cookies"
                                style={btnPrimary}
                            >
                                Accept All
                            </button>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}

// ─── Shared button styles — intentionally equal weight ────────────────────────

const btnBase: React.CSSProperties = {
    padding: '0.5rem 1.1rem',
    borderRadius: '6px',
    fontWeight: '600',
    cursor: 'pointer',
    fontSize: '0.82rem',
    transition: 'all 0.15s ease',
    whiteSpace: 'nowrap',
};

const btnOutline: React.CSSProperties = {
    ...btnBase,
    border: '1px solid rgba(148, 163, 184, 0.4)',
    background: 'transparent',
    color: '#e2e8f0',
};

// Indigo-600, not indigo-500 (#6366f1). White on #6366f1 measures 4.46:1, which misses
// the WCAG 2.1 AA minimum of 4.5:1 for text this size — axe flagged it as a serious
// color-contrast violation on the landing, login and register pages. #4f46e5 measures
// 6.29:1 against white. Please keep any change to this pair above 4.5:1.
const btnPrimary: React.CSSProperties = {
    ...btnBase,
    border: '1px solid #4f46e5',
    background: '#4f46e5',
    color: '#fff',
};
