'use client';

import { useState, useEffect, useMemo } from 'react';
import {
    Globe,
    Search,
    Download,
    Upload,
    Check,
    AlertTriangle,
    ChevronDown,
    ChevronRight,
    Edit3,
    Save,
    X,
    Languages,
    ArrowLeft,
    Filter,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { useToast } from '@/components/ui/Toast';
import Link from 'next/link';

// ─── Types ────────────────────────────────────────────────────────────────────

interface Language {
    code: string;
    name: string;
    nativeName: string;
    rtl: boolean;
    flag: string;
    completion: number;
}

type TranslationMap = Record<string, Record<string, string>>;

const SUPPORTED_LANGUAGES: Language[] = [
    { code: 'en', name: 'English', nativeName: 'English', rtl: false, flag: '🇺🇸', completion: 100 },
    { code: 'es', name: 'Spanish', nativeName: 'Español', rtl: false, flag: '🇪🇸', completion: 92 },
    { code: 'fr', name: 'French', nativeName: 'Français', rtl: false, flag: '🇫🇷', completion: 88 },
    { code: 'de', name: 'German', nativeName: 'Deutsch', rtl: false, flag: '🇩🇪', completion: 85 },
    { code: 'ar', name: 'Arabic', nativeName: 'العربية', rtl: true, flag: '🇸🇦', completion: 74 },
    { code: 'he', name: 'Hebrew', nativeName: 'עברית', rtl: true, flag: '🇮🇱', completion: 68 },
    { code: 'zh', name: 'Chinese', nativeName: '中文', rtl: false, flag: '🇨🇳', completion: 79 },
    { code: 'ja', name: 'Japanese', nativeName: '日本語', rtl: false, flag: '🇯🇵', completion: 71 },
    { code: 'pt', name: 'Portuguese', nativeName: 'Português', rtl: false, flag: '🇧🇷', completion: 83 },
    { code: 'ru', name: 'Russian', nativeName: 'Русский', rtl: false, flag: '🇷🇺', completion: 76 },
    { code: 'hi', name: 'Hindi', nativeName: 'हिंदी', rtl: false, flag: '🇮🇳', completion: 62 },
    { code: 'it', name: 'Italian', nativeName: 'Italiano', rtl: false, flag: '🇮🇹', completion: 81 },
    { code: 'ko', name: 'Korean', nativeName: '한국어', rtl: false, flag: '🇰🇷', completion: 69 },
    { code: 'tr', name: 'Turkish', nativeName: 'Türkçe', rtl: false, flag: '🇹🇷', completion: 73 },
    { code: 'nl', name: 'Dutch', nativeName: 'Nederlands', rtl: false, flag: '🇳🇱', completion: 77 },
];

// Flatten nested translation object to dot-notation keys
function flatten(obj: Record<string, any>, prefix = ''): Record<string, string> {
    const out: Record<string, string> = {};
    for (const [k, v] of Object.entries(obj)) {
        const key = prefix ? `${prefix}.${k}` : k;
        if (typeof v === 'object' && v !== null) {
            Object.assign(out, flatten(v, key));
        } else {
            out[key] = String(v);
        }
    }
    return out;
}

// Group by top-level namespace
function groupByNamespace(flat: Record<string, string>): Record<string, Record<string, string>> {
    const out: Record<string, Record<string, string>> = {};
    for (const [k, v] of Object.entries(flat)) {
        const [ns, ...rest] = k.split('.');
        if (!out[ns]) out[ns] = {};
        out[ns][rest.join('.')] = v;
    }
    return out;
}

export default function TranslationManagementPage() {
    const { success: toastSuccess, error: toastError } = useToast();

    const [selectedLocale, setSelectedLocale] = useState('es');
    const [baseTranslations, setBaseTranslations] = useState<Record<string, string>>({});
    const [targetTranslations, setTargetTranslations] = useState<Record<string, string>>({});
    const [loading, setLoading] = useState(true);
    const [search, setSearch] = useState('');
    const [filterMissing, setFilterMissing] = useState(false);
    const [expandedNs, setExpandedNs] = useState<Set<string>>(new Set(['Navigation', 'Common']));
    const [editingKey, setEditingKey] = useState<string | null>(null);
    const [editValue, setEditValue] = useState('');
    const [dirty, setDirty] = useState<Record<string, string>>({});

    // Load translation files
    useEffect(() => {
        const load = async () => {
            setLoading(true);
            try {
                const [enRes, localeRes] = await Promise.all([
                    fetch('/messages/en.json').then(r => r.json()).catch(() => ({})),
                    fetch(`/messages/${selectedLocale}.json`).then(r => r.json()).catch(() => ({})),
                ]);
                setBaseTranslations(flatten(enRes));
                setTargetTranslations(flatten(localeRes));
                setDirty({});
            } catch {
                // Use empty data if files not served as static
                setBaseTranslations({});
                setTargetTranslations({});
            } finally {
                setLoading(false);
            }
        };
        load();
    }, [selectedLocale]);

    const selectedLang = SUPPORTED_LANGUAGES.find(l => l.code === selectedLocale)!;
    const isRtl = selectedLang?.rtl ?? false;

    // Merge dirty into target for display
    const mergedTarget = useMemo(
        () => ({ ...targetTranslations, ...dirty }),
        [targetTranslations, dirty]
    );

    // Filter keys
    const allKeys = Object.keys(baseTranslations);
    const filteredKeys = useMemo(() => {
        return allKeys.filter(k => {
            if (filterMissing && mergedTarget[k]) return false;
            if (search && !k.toLowerCase().includes(search.toLowerCase()) &&
                !baseTranslations[k]?.toLowerCase().includes(search.toLowerCase())) return false;
            return true;
        });
    }, [allKeys, mergedTarget, baseTranslations, search, filterMissing]);

    const grouped = useMemo(() => groupByNamespace(
        Object.fromEntries(filteredKeys.map(k => [k, baseTranslations[k]]))
    ), [filteredKeys, baseTranslations]);

    const missingCount = allKeys.filter(k => !mergedTarget[k]).length;
    const dirtyCount = Object.keys(dirty).length;

    const startEdit = (key: string) => {
        setEditingKey(key);
        setEditValue(mergedTarget[key] ?? '');
    };

    const saveEdit = (key: string) => {
        setDirty(d => ({ ...d, [key]: editValue }));
        setEditingKey(null);
    };

    const saveDirty = async () => {
        // In production, POST to API to persist; here we just simulate
        toastSuccess(`Saved ${dirtyCount} translation(s) for ${selectedLang.name}`);
        setTargetTranslations(t => ({ ...t, ...dirty }));
        setDirty({});
    };

    const exportJson = () => {
        const data = JSON.stringify(mergedTarget, null, 2);
        const blob = new Blob([data], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${selectedLocale}.json`;
        a.click();
        URL.revokeObjectURL(url);
        toastSuccess('Exported!');
    };

    const handleImport = (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (!file) return;
        const reader = new FileReader();
        reader.onload = (ev) => {
            try {
                const parsed = JSON.parse(ev.target!.result as string);
                const flat = flatten(parsed);
                const importedDirty: Record<string, string> = {};
                for (const [k, v] of Object.entries(flat)) {
                    if (baseTranslations[k] !== undefined) importedDirty[k] = v;
                }
                setDirty(d => ({ ...d, ...importedDirty }));
                toastSuccess(`Imported ${Object.keys(importedDirty).length} key(s)`);
            } catch {
                toastError('Invalid JSON file');
            }
        };
        reader.readAsText(file);
        e.target.value = '';
    };

    return (
        <div className="min-h-screen bg-gray-50">
            {/* Header */}
            <div className="bg-white border-b border-gray-100 px-6 py-5 sticky top-0 z-10 shadow-sm">
                <div className="max-w-6xl mx-auto flex items-center justify-between gap-4 flex-wrap">
                    <div className="flex items-center gap-3">
                        <Link href="/settings" className="p-2 hover:bg-gray-100 rounded-lg transition-colors text-gray-500">
                            <ArrowLeft className="w-4 h-4" />
                        </Link>
                        <div>
                            <h1 className="text-xl font-bold text-gray-900 flex items-center gap-2">
                                <Languages className="w-5 h-5 text-violet-600" />
                                Translation Management
                            </h1>
                            <p className="text-sm text-gray-500">Manage UI strings for all supported languages</p>
                        </div>
                    </div>

                    <div className="flex items-center gap-2">
                        {dirtyCount > 0 && (
                            <button
                                onClick={saveDirty}
                                className="flex items-center gap-2 px-4 py-2 bg-violet-600 text-white rounded-lg text-sm font-medium hover:bg-violet-700 transition-colors"
                            >
                                <Save className="w-4 h-4" />
                                Save {dirtyCount} changes
                            </button>
                        )}
                        <button
                            onClick={exportJson}
                            className="flex items-center gap-2 px-3 py-2 border border-gray-200 text-gray-700 rounded-lg text-sm hover:bg-gray-50 transition-colors"
                        >
                            <Download className="w-4 h-4" />
                            Export
                        </button>
                        <label className="flex items-center gap-2 px-3 py-2 border border-gray-200 text-gray-700 rounded-lg text-sm hover:bg-gray-50 transition-colors cursor-pointer">
                            <Upload className="w-4 h-4" />
                            Import
                            <input type="file" accept=".json" onChange={handleImport} className="hidden" />
                        </label>
                    </div>
                </div>
            </div>

            <div className="max-w-6xl mx-auto px-6 py-6">
                <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
                    {/* Language selector */}
                    <div className="lg:col-span-1 space-y-2">
                        <h2 className="text-sm font-semibold text-gray-700 mb-3">Languages</h2>
                        {SUPPORTED_LANGUAGES.map(lang => (
                            <button
                                key={lang.code}
                                onClick={() => setSelectedLocale(lang.code)}
                                className={cn(
                                    'w-full flex items-center gap-3 p-3 rounded-xl border text-left transition-all',
                                    selectedLocale === lang.code
                                        ? 'border-violet-500 bg-violet-50'
                                        : 'border-gray-100 bg-white hover:border-gray-200'
                                )}
                            >
                                <span className="text-xl">{lang.flag}</span>
                                <div className="flex-1 min-w-0">
                                    <div className="flex items-center justify-between">
                                        <p className="text-sm font-medium text-gray-900">{lang.name}</p>
                                        {lang.rtl && (
                                            <span className="text-xs bg-amber-100 text-amber-700 px-1.5 rounded">RTL</span>
                                        )}
                                    </div>
                                    <p className="text-xs text-gray-400">{lang.nativeName}</p>
                                    <div className="mt-1.5 flex items-center gap-1.5">
                                        <div className="flex-1 h-1 bg-gray-100 rounded-full">
                                            <div
                                                className={cn(
                                                    'h-1 rounded-full',
                                                    lang.completion >= 90 ? 'bg-emerald-500' :
                                                    lang.completion >= 70 ? 'bg-amber-400' : 'bg-red-400'
                                                )}
                                                style={{ width: `${lang.completion}%` }}
                                            />
                                        </div>
                                        <span className="text-xs text-gray-400">{lang.completion}%</span>
                                    </div>
                                </div>
                            </button>
                        ))}
                    </div>

                    {/* Translation editor */}
                    <div className="lg:col-span-3 space-y-4">
                        {/* Toolbar */}
                        <div className="flex items-center gap-3 flex-wrap">
                            <div className="relative flex-1 min-w-48">
                                <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
                                <input
                                    type="text"
                                    placeholder="Search keys or values..."
                                    value={search}
                                    onChange={e => setSearch(e.target.value)}
                                    className="w-full pl-9 pr-4 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-violet-400"
                                />
                            </div>

                            <button
                                onClick={() => setFilterMissing(f => !f)}
                                className={cn(
                                    'flex items-center gap-2 px-3 py-2 rounded-lg border text-sm transition-all',
                                    filterMissing
                                        ? 'bg-red-50 border-red-200 text-red-700'
                                        : 'bg-white border-gray-200 text-gray-600 hover:bg-gray-50'
                                )}
                            >
                                <AlertTriangle className="w-4 h-4" />
                                Missing ({missingCount})
                            </button>

                            {isRtl && (
                                <span className="px-3 py-2 bg-amber-50 border border-amber-200 text-amber-700 text-xs rounded-lg font-medium flex items-center gap-1.5">
                                    <Globe className="w-3.5 h-3.5" />
                                    RTL language — text reads right-to-left
                                </span>
                            )}
                        </div>

                        {/* Stats row */}
                        <div className="flex gap-4 text-sm text-gray-500">
                            <span>{filteredKeys.length} keys shown</span>
                            <span className="text-red-500">{missingCount} missing</span>
                            {dirtyCount > 0 && (
                                <span className="text-violet-600 font-medium">{dirtyCount} unsaved changes</span>
                            )}
                        </div>

                        {/* Namespaces */}
                        {loading ? (
                            <div className="space-y-2">
                                {[...Array(4)].map((_, i) => (
                                    <div key={i} className="h-12 bg-white rounded-xl border border-gray-100 animate-pulse" />
                                ))}
                            </div>
                        ) : Object.entries(grouped).length === 0 ? (
                            <div className="text-center py-12 text-gray-400">
                                <Globe className="w-8 h-8 mx-auto mb-2 opacity-30" />
                                <p>No keys match your search</p>
                            </div>
                        ) : (
                            Object.entries(grouped).map(([ns, keys]) => {
                                const expanded = expandedNs.has(ns);
                                const nsKeys = Object.keys(keys);
                                const nsMissing = nsKeys.filter(k => !mergedTarget[`${ns}.${k}`]).length;
                                const nsDirty = nsKeys.filter(k => dirty[`${ns}.${k}`]).length;

                                return (
                                    <div key={ns} className="bg-white rounded-xl border border-gray-100 overflow-hidden shadow-sm">
                                        {/* Namespace header */}
                                        <button
                                            onClick={() => setExpandedNs(prev => {
                                                const next = new Set(prev);
                                                if (next.has(ns)) next.delete(ns); else next.add(ns);
                                                return next;
                                            })}
                                            className="w-full flex items-center gap-3 px-4 py-3 hover:bg-gray-50 transition-colors"
                                        >
                                            {expanded ? (
                                                <ChevronDown className="w-4 h-4 text-gray-400" />
                                            ) : (
                                                <ChevronRight className="w-4 h-4 text-gray-400" />
                                            )}
                                            <span className="font-semibold text-gray-900 text-sm">{ns}</span>
                                            <span className="text-xs text-gray-400">{nsKeys.length} keys</span>
                                            {nsMissing > 0 && (
                                                <span className="ml-1 px-1.5 py-0.5 bg-red-50 text-red-600 text-xs rounded-full">
                                                    {nsMissing} missing
                                                </span>
                                            )}
                                            {nsDirty > 0 && (
                                                <span className="px-1.5 py-0.5 bg-violet-50 text-violet-600 text-xs rounded-full">
                                                    {nsDirty} changed
                                                </span>
                                            )}
                                        </button>

                                        {/* Keys */}
                                        {expanded && (
                                            <div className="divide-y divide-gray-50">
                                                {Object.entries(keys).map(([key, enValue]) => {
                                                    const fullKey = `${ns}.${key}`;
                                                    const translated = mergedTarget[fullKey];
                                                    const isMissing = !translated;
                                                    const isDirty = !!dirty[fullKey];
                                                    const isEditing = editingKey === fullKey;

                                                    return (
                                                        <div
                                                            key={fullKey}
                                                            className={cn(
                                                                'px-4 py-3 grid grid-cols-2 gap-4 text-sm group',
                                                                isMissing && 'bg-red-50/50',
                                                                isDirty && !isMissing && 'bg-violet-50/30'
                                                            )}
                                                        >
                                                            {/* English source */}
                                                            <div>
                                                                <p className="text-xs text-gray-400 font-mono mb-0.5">{fullKey}</p>
                                                                <p className="text-gray-700">{enValue}</p>
                                                            </div>

                                                            {/* Target translation */}
                                                            <div className="flex items-start gap-2">
                                                                {isEditing ? (
                                                                    <div className="flex-1">
                                                                        <textarea
                                                                            autoFocus
                                                                            value={editValue}
                                                                            onChange={e => setEditValue(e.target.value)}
                                                                            dir={isRtl ? 'rtl' : 'ltr'}
                                                                            className="w-full px-2 py-1.5 border border-violet-300 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-violet-400 resize-none"
                                                                            rows={2}
                                                                        />
                                                                        <div className="flex gap-1 mt-1">
                                                                            <button
                                                                                onClick={() => saveEdit(fullKey)}
                                                                                className="px-2 py-1 bg-violet-600 text-white rounded text-xs hover:bg-violet-700"
                                                                            >
                                                                                <Check className="w-3 h-3" />
                                                                            </button>
                                                                            <button
                                                                                onClick={() => setEditingKey(null)}
                                                                                className="px-2 py-1 bg-gray-100 text-gray-600 rounded text-xs"
                                                                            >
                                                                                <X className="w-3 h-3" />
                                                                            </button>
                                                                        </div>
                                                                    </div>
                                                                ) : (
                                                                    <>
                                                                        <div className="flex-1">
                                                                            {isMissing ? (
                                                                                <span className="text-red-400 italic text-xs">
                                                                                    Not translated
                                                                                </span>
                                                                            ) : (
                                                                                <p
                                                                                    dir={isRtl ? 'rtl' : 'ltr'}
                                                                                    className={cn(
                                                                                        'text-gray-700',
                                                                                        isDirty && 'font-medium text-violet-700'
                                                                                    )}
                                                                                >
                                                                                    {translated}
                                                                                </p>
                                                                            )}
                                                                        </div>
                                                                        <button
                                                                            onClick={() => startEdit(fullKey)}
                                                                            className="opacity-0 group-hover:opacity-100 p-1 hover:bg-gray-100 rounded transition-all"
                                                                        >
                                                                            <Edit3 className="w-3.5 h-3.5 text-gray-400" />
                                                                        </button>
                                                                    </>
                                                                )}
                                                            </div>
                                                        </div>
                                                    );
                                                })}
                                            </div>
                                        )}
                                    </div>
                                );
                            })
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}
