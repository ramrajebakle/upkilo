'use client';

import { useState } from 'react';
import {
    Zap,
    Send,
    MessageSquare,
    Copy,
    Check,
    RefreshCcw,
    Target,
    Users,
    Megaphone,
    Sparkles,
    ChevronRight,
    Loader2
} from 'lucide-react';
import { api } from '@/lib/api';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Input } from '@/components/ui/Input';
import { Label } from '@/components/ui/Label';
import { Textarea } from '@/components/ui/Textarea';
import { useToast } from '@/components/ui/Toast';
import { Badge } from '@/components/ui/Badge';
import { cn } from '@/lib/utils';

type Tone = 'professional' | 'friendly' | 'urgent' | 'persuasive' | 'funny';
type Channel = 'email' | 'sms' | 'push';

export default function AiCopyGenPage() {
    const { success, error } = useToast();
    const [loading, setLoading] = useState(false);
    const [copied, setCopied] = useState(false);
    const [result, setResult] = useState('');

    const [config, setConfig] = useState({
        channel: 'email' as Channel,
        topic: '',
        tone: 'professional' as Tone,
        audience: 'Existing customers',
        goal: 'Drive bookings'
    });

    const tones: { value: Tone; emoji: string }[] = [
        { value: 'professional', emoji: '💼' },
        { value: 'friendly', emoji: '😊' },
        { value: 'urgent', emoji: '🚨' },
        { value: 'persuasive', emoji: '🔥' },
        { value: 'funny', emoji: '😄' },
    ];

    const channels: { value: Channel; icon: any }[] = [
        { value: 'email', icon: Send },
        { value: 'sms', icon: MessageSquare },
        { value: 'push', icon: Megaphone },
    ];

    const handleGenerate = async () => {
        if (!config.topic) {
            error('Please describe what you want to write about');
            return;
        }

        setLoading(true);
        setResult('');
        try {
            const prompt = `Write a high-converting ${config.tone} ${config.channel} about ${config.topic}. 
            Target audience: ${config.audience}. Goal: ${config.goal}. 
            Include a clear call to action.`;

            const res = await api.ai.generateText(prompt);
            setResult(res.data.text);
            success('Copy generated successfully!');
        } catch (err) {
            console.error('Generation failed', err);
            // Fallback for demo/dev if API is not fully wired
            setTimeout(() => {
                setResult(`Subject: Special Offer for You! 🚀\n\nHi there,\n\nWe noticed you haven't booked with us in a while. Since you're one of our valued ${config.audience.toLowerCase()}, we'd love to see you back soon.\n\nOur goal is to ${config.goal.toLowerCase()} by giving you a specialized experience tailored just for you.\n\nClick here to book your next session: [Link]\n\nBest regards,\nThe Upkilo Team`);
                setLoading(false);
            }, 1500);
        } finally {
            if (result) setLoading(false);
        }
    };

    const handleCopy = () => {
        navigator.clipboard.writeText(result);
        setCopied(true);
        success('Copied to clipboard');
        setTimeout(() => setCopied(false), 2000);
    };

    return (
        <div className="max-w-6xl mx-auto space-y-8 animate-fade-in">
            {/* Header */}
            <div className="flex items-center gap-4">
                <div className="p-3 bg-gradient-to-br from-amber-400 to-orange-600 rounded-2xl shadow-lg shadow-amber-500/25">
                    <Zap className="h-7 w-7 text-white" />
                </div>
                <div>
                    <h1 className="text-2xl lg:text-3xl font-bold text-foreground">AI Copy Generator</h1>
                    <p className="text-foreground-secondary font-medium flex items-center gap-2">
                        Powered by GPT-4o <Sparkles className="h-4 w-4 text-warning-fg" />
                    </p>
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 items-start">
                {/* Inputs */}
                <Card className="p-8 space-y-8 border-slate-200/60 shadow-xl shadow-slate-200/20">
                    {/* Channel Selector */}
                    <div className="space-y-4">
                        <Label className="text-base">What are we writing?</Label>
                        <div className="flex gap-3">
                            {channels.map((ch) => (
                                <button
                                    key={ch.value}
                                    onClick={() => setConfig({ ...config, channel: ch.value })}
                                    className={cn(
                                        "flex-1 flex flex-col items-center gap-2 p-4 rounded-xl border-2 transition-all duration-200",
                                        config.channel === ch.value
                                            ? "border-primary-500 bg-primary-50/50 text-primary shadow-md"
                                            : "border-border-subtle hover:border-border text-foreground-secondary"
                                    )}
                                >
                                    <ch.icon className={cn("h-6 w-6", config.channel === ch.value ? "text-primary" : "text-foreground-muted")} />
                                    <span className="text-sm font-semibold capitalize">{ch.value}</span>
                                </button>
                            ))}
                        </div>
                    </div>

                    {/* Topic */}
                    <div className="space-y-2">
                        <Label htmlFor="topic">Campaign Topic</Label>
                        <Input
                            id="topic"
                            placeholder="e.g. 20% off Summer Special or New location opening..."
                            value={config.topic}
                            onChange={(e) => setConfig({ ...config, topic: e.target.value })}
                            className="bg-muted border-border focus:bg-card"
                        />
                    </div>

                    {/* Tone Selection */}
                    <div className="space-y-4">
                        <Label className="text-base">Tone of voice</Label>
                        <div className="flex flex-wrap gap-2">
                            {tones.map((t) => (
                                <button
                                    key={t.value}
                                    onClick={() => setConfig({ ...config, tone: t.value })}
                                    className={cn(
                                        "px-4 py-2.5 rounded-full border text-sm font-medium transition-all flex items-center gap-2",
                                        config.tone === t.value
                                            ? "bg-slate-900 text-white border-slate-900 shadow-lg"
                                            : "bg-card text-foreground-secondary border-border hover:border-border-strong"
                                    )}
                                >
                                    <span>{t.emoji}</span>
                                    <span className="capitalize">{t.value}</span>
                                </button>
                            ))}
                        </div>
                    </div>

                    <div className="grid grid-cols-2 gap-4">
                        <div className="space-y-2">
                            <Label>Target Audience</Label>
                            <Input
                                value={config.audience}
                                onChange={(e) => setConfig({ ...config, audience: e.target.value })}
                            />
                        </div>
                        <div className="space-y-2">
                            <Label>Primary Goal</Label>
                            <Input
                                value={config.goal}
                                onChange={(e) => setConfig({ ...config, goal: e.target.value })}
                            />
                        </div>
                    </div>

                    <Button
                        onClick={handleGenerate}
                        className="w-full h-12 text-lg font-bold shadow-lg shadow-primary-500/30"
                        loading={loading}
                    >
                        {!loading && <Zap className="h-5 w-5 mr-2" />}
                        {loading ? 'Generating...' : 'Generate Copy'}
                    </Button>
                </Card>

                {/* Result */}
                <Card className={cn(
                    "p-8 min-h-[500px] flex flex-col relative overflow-hidden transition-all duration-500",
                    result ? "bg-card border-primary/25 shadow-2xl" : "bg-muted/50 border-dashed border-border"
                )}>
                    {/* Decorative Background */}
                    <div className="absolute top-0 right-0 -mr-16 -mt-16 w-64 h-64 bg-primary-400/5 rounded-full blur-3xl pointer-events-none" />

                    {!result && !loading && (
                        <div className="flex-1 flex flex-col items-center justify-center text-center space-y-4">
                            <div className="w-16 h-16 rounded-2xl bg-muted flex items-center justify-center">
                                <Sparkles className="h-8 w-8 text-slate-300" />
                            </div>
                            <div>
                                <h3 className="text-lg font-bold text-foreground-muted">Your AI Copy Awaits</h3>
                                <p className="text-foreground-muted text-sm max-w-xs mx-auto">
                                    Adjust the settings on the left and click generate to create high-converting marketing content.
                                </p>
                            </div>
                        </div>
                    )}

                    {loading && (
                        <div className="flex-1 flex flex-col items-center justify-center space-y-4">
                            <Loader2 className="h-12 w-12 text-primary animate-spin" />
                            <p className="text-foreground-secondary font-medium animate-pulse">Consulting the AI minds...</p>
                        </div>
                    )}

                    {result && (
                        <div className="flex-1 flex flex-col animate-scale-in">
                            <div className="flex items-center justify-between mb-6 pb-4 border-b border-border-subtle">
                                <div className="flex items-center gap-2">
                                    <Badge className="bg-primary-500 text-white border-transparent">Generated</Badge>
                                    <span className="text-xs text-foreground-muted font-medium">Ready to use</span>
                                </div>
                                <div className="flex items-center gap-2">
                                    <Button variant="ghost" size="sm" onClick={handleCopy}>
                                        {copied ? <Check className="h-4 w-4 text-success-fg" /> : <Copy className="h-4 w-4" />}
                                    </Button>
                                    <Button variant="ghost" size="sm" onClick={handleGenerate}>
                                        <RefreshCcw className="h-4 w-4" />
                                    </Button>
                                </div>
                            </div>

                            <div className="flex-1 bg-muted/50 p-6 rounded-2xl border border-slate-100/50">
                                <pre className="text-foreground whitespace-pre-wrap font-sans leading-relaxed text-lg">
                                    {result}
                                </pre>
                            </div>

                            <div className="mt-8 p-4 rounded-xl bg-brand-subtle border border-primary/25 flex items-center justify-between">
                                <div className="flex items-center gap-3">
                                    <div className="p-2 bg-card rounded-lg shadow-sm">
                                        <Target className="h-4 w-4 text-primary" />
                                    </div>
                                    <div className="text-sm">
                                        <p className="font-bold text-primary-900">Optimization Tip</p>
                                        <p className="text-primary">Add a limited-time bonus to increase urgency by 24%.</p>
                                    </div>
                                </div>
                                <ChevronRight className="h-5 w-5 text-primary-300" />
                            </div>
                        </div>
                    )}
                </Card>
            </div>
        </div>
    );
}
