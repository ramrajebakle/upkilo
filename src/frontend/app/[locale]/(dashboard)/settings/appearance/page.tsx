'use client';

import { useState } from 'react';
import { Palette, Sun, Moon, Check, Save, Loader2, Zap, Sparkles, Monitor } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useToast } from '@/components/ui/Toast';
import { Button } from '@/components/ui/Button';

export default function AppearanceSettingsPage() {
    const { success: toastSuccess, error: toastError } = useToast();
    const [saving, setSaving] = useState(false);
    const [theme, setTheme] = useState('dark');
    const [accentColor, setAccentColor] = useState('#6366f1');

    const handleSave = async () => {
        setSaving(true);
        try {
            // In a real app, call api.settings.updateAppearance({ theme, accentColor })
            await new Promise(resolve => setTimeout(resolve, 1000));
            toastSuccess('Interface parameters synchronised');
        } catch (error) {
            toastError('Failed to save appearance settings');
        } finally {
            setSaving(false);
        }
    };

    const themes = [
        { key: 'light', label: 'Ethereal', icon: Sun, desc: 'High-luminosity interface' },
        { key: 'dark', label: 'Obsidian', icon: Moon, desc: 'High-contrast nocturnal mode' },
        { key: 'system', label: 'Neural', icon: Monitor, desc: 'Automatic device alignment' },
    ];

    const accents = [
        { color: '#6366f1', name: 'Indigo' },
        { color: '#8b5cf6', name: 'Violet' },
        { color: '#0ea5e9', name: 'Cyan' },
        { color: '#10b981', name: 'Emerald' },
        { color: '#f59e0b', name: 'Amber' },
        { color: '#ef4444', name: 'Rose' },
    ];

    return (
        <div className="max-w-4xl mx-auto space-y-12 animate-fade-in pb-20">
            {/* Header Bundle */}
            <div className="flex items-center gap-6 mb-12">
                <div className="p-4 bg-gradient-to-br from-primary-500 to-primary-600 rounded-[28px] shadow-2xl shadow-primary-500/20 border border-primary-500/20">
                    <Palette className="h-8 w-8 text-white" />
                </div>
                <div>
                    <h1 className="text-3xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Interface Core</h1>
                    <p className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mt-1">Visual Schema Configuration</p>
                </div>
            </div>

            <div className="p-10 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[40px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-12">
                {/* Theme Selection */}
                <div className="space-y-8">
                    <div className="flex items-center gap-4">
                        <div className="h-10 w-1 rounded-full bg-primary-500 shadow-lg shadow-primary-500/50" />
                        <h2 className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Luminance Protocol</h2>
                    </div>
                    
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                        {themes.map((item) => {
                            const Icon = item.icon;
                            const isActive = theme === item.key;
                            return (
                                <button
                                    key={item.key}
                                    onClick={() => setTheme(item.key)}
                                    className={cn(
                                        'flex flex-col items-start p-8 rounded-[32px] border transition-all duration-500 group relative overflow-hidden',
                                        isActive
                                            ? 'border-primary-500 ring-8 ring-primary-500/[0.03] bg-primary-50/10 dark:bg-primary-500/[0.02]'
                                            : 'border-slate-100 dark:border-slate-850 bg-slate-50/30 dark:bg-slate-950/20 hover:border-slate-200 dark:hover:border-slate-800'
                                    )}
                                >
                                    <div className={cn(
                                        'p-4 rounded-2xl mb-6 transition-all duration-500',
                                        isActive 
                                            ? 'bg-primary-500 text-white shadow-2xl shadow-primary-500/30' 
                                            : 'bg-white dark:bg-slate-900 text-slate-400 dark:text-slate-600 group-hover:scale-110'
                                    )}>
                                        <Icon className="h-6 w-6" />
                                    </div>
                                    <div className="text-left relative z-10">
                                        <p className={cn(
                                            'text-xs font-black uppercase tracking-widest mb-1.5',
                                            isActive ? 'text-primary-600 dark:text-primary-400' : 'text-slate-900 dark:text-white'
                                        )}>
                                            {item.label}
                                        </p>
                                        <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest leading-relaxed">{item.desc}</p>
                                    </div>
                                    
                                    {isActive && (
                                        <div className="absolute top-6 right-6 p-1.5 bg-primary-500 text-white rounded-lg shadow-lg">
                                            <Check className="h-3 w-3" />
                                        </div>
                                    )}
                                    
                                    <div className="absolute -bottom-8 -right-8 w-24 h-24 bg-primary-500/5 rounded-full blur-2xl group-hover:scale-150 transition-transform" />
                                </button>
                            );
                        })}
                    </div>
                </div>

                {/* Accent Color */}
                <div className="space-y-8">
                    <div className="flex items-center gap-4">
                        <div className="h-10 w-1 rounded-full bg-emerald-500 shadow-lg shadow-emerald-500/50" />
                        <h2 className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-[0.3em]">Chromatic Bias</h2>
                    </div>
                    
                    <div className="flex flex-wrap gap-6 p-8 bg-slate-50/50 dark:bg-slate-950/40 rounded-[32px] border border-transparent dark:border-slate-850 shadow-inner">
                        {accents.map((accent) => (
                            <button
                                key={accent.color}
                                onClick={() => setAccentColor(accent.color)}
                                className={cn(
                                    'w-14 h-14 rounded-[20px] border-4 transition-all duration-500 relative group overflow-hidden shadow-sm',
                                    accentColor === accent.color 
                                        ? 'border-white dark:border-slate-800 scale-110 shadow-2xl ring-4 ring-offset-4 ring-primary-500/20 dark:ring-primary-500/10' 
                                        : 'border-transparent hover:scale-110'
                                )}
                                style={{ backgroundColor: accent.color }}
                                title={accent.name}
                            >
                                {accentColor === accent.color && (
                                    <div className="absolute inset-0 flex items-center justify-center text-white">
                                        <div className="bg-black/20 backdrop-blur-md p-2 rounded-lg shadow-inner">
                                            <Check className="h-6 w-6" />
                                        </div>
                                    </div>
                                )}
                                <div className="absolute inset-0 bg-white/10 opacity-0 group-hover:opacity-100 transition-opacity" />
                            </button>
                        ))}
                    </div>
                    
                    <div className="flex items-center gap-4 px-2">
                        <Sparkles className="h-4 w-4 text-primary-500" />
                        <p className="text-[10px] font-black text-slate-400 dark:text-slate-600 uppercase tracking-widest leading-relaxed">
                            Global variables will propagate across all components instantly
                        </p>
                    </div>
                </div>
            </div>

            {/* Commit Action */}
            <div className="flex flex-col md:flex-row items-center justify-between gap-8 pt-10 border-t border-slate-100 dark:border-slate-800">
                <div className="flex items-center gap-4">
                    <div className="p-3 bg-primary-50 dark:bg-primary-950 rounded-2xl">
                        <Zap className="h-5 w-5 text-primary-500" />
                    </div>
                    <div>
                        <p className="text-[10px] font-black text-slate-900 dark:text-white uppercase tracking-widest">Interface Lock</p>
                        <p className="text-[9px] font-bold text-slate-500 dark:text-slate-600 uppercase tracking-widest mt-1">Changes are persistent across all neural nodes</p>
                    </div>
                </div>
                
                <Button
                    onClick={handleSave}
                    disabled={saving}
                    className="h-16 px-16 rounded-[24px] font-black uppercase tracking-[0.2em] text-[10px] shadow-2xl shadow-primary-500/30 active:scale-95 transition-all flex items-center gap-4"
                >
                    {saving
                        ? <><Loader2 className="h-5 w-5 animate-spin" /> Transmitting...</>
                        : <><Save className="h-5 w-5" /> Commit Schema</>
                    }
                </Button>
            </div>
        </div>
    );
}

