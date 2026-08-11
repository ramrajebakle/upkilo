'use client';

import { useEffect, useState } from 'react';
import api from '@/lib/api';
import { Star, MessageSquare, Send, RefreshCw, Plus, TrendingUp, CheckCircle, Clock, ExternalLink } from 'lucide-react';
import { cn } from '@/lib/utils';

interface Review {
    id: string;
    platform: string;
    reviewerName: string;
    rating: number;
    reviewText: string | null;
    responseText: string | null;
    sentiment: string;
    reviewDate: string;
    isVerified: boolean;
    hasResponse: boolean;
}

interface ReviewStats {
    averageRating: number;
    totalCount: number;
    responseRate: number;
    recentCount: number;
    countByPlatform: Record<string, number>;
    ratingBreakdown: Record<string, number>;
}

const PLATFORM_COLORS: Record<string, string> = {
    Google:   'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300',
    Yelp:     'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
    Facebook: 'bg-primary-100 text-primary-700 dark:bg-primary-900/40 dark:text-primary-300',
    Upkilo:   'bg-primary-100 text-primary-700 dark:bg-primary-900/40 dark:text-primary-300',
};

const SENTIMENT_BADGE: Record<string, string> = {
    Positive: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
    Neutral:  'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400',
    Negative: 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
};

function StarDisplay({ rating }: { rating: number }) {
    return (
        <div className="flex gap-0.5">
            {[1, 2, 3, 4, 5].map((s) => (
                <Star key={s} className={cn('w-4 h-4', s <= rating ? 'fill-amber-400 text-amber-400' : 'text-slate-300 dark:text-slate-600')} />
            ))}
        </div>
    );
}

export default function ReviewsPage() {
    const [reviews, setReviews] = useState<Review[]>([]);
    const [stats, setStats] = useState<ReviewStats | null>(null);
    const [loading, setLoading] = useState(true);
    const [activeTab, setActiveTab] = useState<'all' | 'pending' | 'requests'>('all');
    const [filterPlatform, setFilterPlatform] = useState('');
    const [filterRating, setFilterRating] = useState('');

    // Add review modal state
    const [showAddModal, setShowAddModal] = useState(false);
    const [newReview, setNewReview] = useState({ platform: 'Google', reviewerName: '', rating: 5, reviewText: '', externalReviewId: '' });
    const [submitting, setSubmitting] = useState(false);

    // Respond modal state
    const [respondingTo, setRespondingTo] = useState<string | null>(null);
    const [responseText, setResponseText] = useState('');

    const load = async () => {
        setLoading(true);
        try {
            const params: any = {};
            if (filterPlatform) params.platform = filterPlatform;
            if (filterRating) params.rating = filterRating;
            const [rvRes, stRes] = await Promise.all([
                api.reviews.list(params),
                api.reviews.stats(),
            ]);
            setReviews(rvRes.data);
            setStats(stRes.data);
        } catch { /* handled gracefully */ }
        setLoading(false);
    };

    useEffect(() => { load(); }, [filterPlatform, filterRating]);

    const submitReview = async () => {
        setSubmitting(true);
        try {
            await api.reviews.add({ ...newReview, reviewDate: new Date().toISOString() });
            setShowAddModal(false);
            setNewReview({ platform: 'Google', reviewerName: '', rating: 5, reviewText: '', externalReviewId: '' });
            load();
        } catch { /* ignore */ }
        setSubmitting(false);
    };

    const submitResponse = async () => {
        if (!respondingTo) return;
        setSubmitting(true);
        try {
            await api.reviews.respond(respondingTo, { responseText });
            setRespondingTo(null);
            setResponseText('');
            load();
        } catch { /* ignore */ }
        setSubmitting(false);
    };

    const RESPONSE_TEMPLATES = [
        "Thank you so much for your kind words! We're thrilled you had a great experience and look forward to seeing you again soon.",
        "We really appreciate you taking the time to share your feedback! Your satisfaction is our top priority and we hope to see you again.",
        "Thank you for your review. We're sorry your experience wasn't perfect. Please contact us so we can make it right for you.",
    ];

    return (
        <div className="space-y-8">
            {/* Header */}
            <div className="flex items-start justify-between">
                <div>
                    <div className="flex items-center gap-3 mb-1">
                        <div className="p-2.5 bg-gradient-to-br from-amber-400 to-orange-500 rounded-xl shadow-lg shadow-amber-500/30">
                            <Star className="h-5 w-5 text-white fill-white" />
                        </div>
                        <h1 className="text-2xl font-bold text-slate-900 dark:text-white">Review Management</h1>
                    </div>
                    <p className="text-slate-500 dark:text-slate-400 text-sm ml-14">
                        Reviews are the <strong>#1 local SEO factor</strong>. Respond within 24h to boost your ranking.
                    </p>
                </div>
                <button
                    onClick={() => setShowAddModal(true)}
                    className="flex items-center gap-2 px-4 py-2.5 bg-primary-500 hover:bg-primary-600 text-white rounded-xl font-medium text-sm shadow-lg shadow-primary-500/30 transition-all"
                >
                    <Plus className="w-4 h-4" />
                    Import Review
                </button>
            </div>

            {/* Stats */}
            {stats && (
                <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
                    <div className="bg-white dark:bg-slate-900 rounded-2xl p-5 border border-slate-100 dark:border-slate-800 shadow-sm">
                        <p className="text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wide">Avg Rating</p>
                        <div className="flex items-end gap-2 mt-2">
                            <span className="text-3xl font-bold text-slate-900 dark:text-white">{stats.averageRating}</span>
                            <StarDisplay rating={Math.round(stats.averageRating)} />
                        </div>
                    </div>
                    <div className="bg-white dark:bg-slate-900 rounded-2xl p-5 border border-slate-100 dark:border-slate-800 shadow-sm">
                        <p className="text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wide">Total Reviews</p>
                        <p className="text-3xl font-bold text-slate-900 dark:text-white mt-2">{stats.totalCount}</p>
                        <p className="text-xs text-emerald-600 dark:text-emerald-400 mt-1">+{stats.recentCount} this month</p>
                    </div>
                    <div className="bg-white dark:bg-slate-900 rounded-2xl p-5 border border-slate-100 dark:border-slate-800 shadow-sm">
                        <p className="text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wide">Response Rate</p>
                        <p className="text-3xl font-bold text-slate-900 dark:text-white mt-2">{stats.responseRate}%</p>
                        <p className="text-xs text-slate-400 mt-1">Google rewards fast responses</p>
                    </div>
                    <div className="bg-white dark:bg-slate-900 rounded-2xl p-5 border border-slate-100 dark:border-slate-800 shadow-sm">
                        <p className="text-xs font-medium text-slate-500 dark:text-slate-400 uppercase tracking-wide">By Platform</p>
                        <div className="mt-2 space-y-1">
                            {Object.entries(stats.countByPlatform).map(([p, n]) => (
                                <div key={p} className="flex items-center justify-between">
                                    <span className="text-xs text-slate-600 dark:text-slate-300">{p}</span>
                                    <span className="text-xs font-bold text-slate-900 dark:text-white">{n}</span>
                                </div>
                            ))}
                        </div>
                    </div>
                </div>
            )}

            {/* Rating Breakdown */}
            {stats && (
                <div className="bg-white dark:bg-slate-900 rounded-2xl p-6 border border-slate-100 dark:border-slate-800 shadow-sm">
                    <h3 className="font-semibold text-slate-900 dark:text-white mb-4 text-sm">Rating Breakdown</h3>
                    <div className="space-y-2">
                        {[5,4,3,2,1].map((r) => {
                            const count = stats.ratingBreakdown?.[r.toString()] ?? 0;
                            const pct = stats.totalCount > 0 ? Math.round(count / stats.totalCount * 100) : 0;
                            return (
                                <div key={r} className="flex items-center gap-3">
                                    <span className="text-xs font-medium text-slate-600 dark:text-slate-300 w-4">{r}</span>
                                    <Star className="w-3.5 h-3.5 fill-amber-400 text-amber-400" />
                                    <div className="flex-1 h-2 bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
                                        <div className="h-full bg-amber-400 rounded-full transition-all" style={{ width: `${pct}%` }} />
                                    </div>
                                    <span className="text-xs text-slate-500 w-8 text-right">{count}</span>
                                </div>
                            );
                        })}
                    </div>
                </div>
            )}

            {/* SEO tip banner */}
            <div className="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800/40 rounded-2xl p-4 flex gap-3">
                <TrendingUp className="w-5 h-5 text-amber-600 dark:text-amber-400 shrink-0 mt-0.5" />
                <div className="text-sm">
                    <strong className="text-amber-800 dark:text-amber-300">SEO Impact:</strong>
                    <span className="text-amber-700 dark:text-amber-400"> Responding to every review signals activity to Google and can improve your local ranking. Aim to respond within 24 hours.</span>
                </div>
            </div>

            {/* Filters */}
            <div className="flex gap-3 flex-wrap">
                <select
                    value={filterPlatform}
                    onChange={(e) => setFilterPlatform(e.target.value)}
                    className="px-3 py-2 text-sm bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-700 dark:text-slate-300"
                >
                    <option value="">All Platforms</option>
                    {['Google','Yelp','Facebook','Upkilo'].map(p => <option key={p} value={p}>{p}</option>)}
                </select>
                <select
                    value={filterRating}
                    onChange={(e) => setFilterRating(e.target.value)}
                    className="px-3 py-2 text-sm bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-700 dark:text-slate-300"
                >
                    <option value="">All Ratings</option>
                    {[5,4,3,2,1].map(r => <option key={r} value={r}>{r} Stars</option>)}
                </select>
                <button onClick={load} className="px-3 py-2 text-sm bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-600 dark:text-slate-300 flex items-center gap-1.5">
                    <RefreshCw className="w-3.5 h-3.5" /> Refresh
                </button>
            </div>

            {/* Review List */}
            {loading ? (
                <div className="text-center py-16 text-slate-400">Loading reviews…</div>
            ) : reviews.length === 0 ? (
                <div className="text-center py-16 bg-white dark:bg-slate-900 rounded-2xl border border-slate-100 dark:border-slate-800">
                    <Star className="w-12 h-12 text-slate-200 dark:text-slate-700 mx-auto mb-3" />
                    <p className="font-medium text-slate-600 dark:text-slate-300">No reviews yet</p>
                    <p className="text-sm text-slate-400 mt-1">Import your first review or send review requests to clients after their appointments.</p>
                    <button onClick={() => setShowAddModal(true)} className="mt-4 px-4 py-2 bg-primary-500 text-white rounded-lg text-sm font-medium">Import Review</button>
                </div>
            ) : (
                <div className="space-y-4">
                    {reviews.map((r) => (
                        <div key={r.id} className="bg-white dark:bg-slate-900 rounded-2xl p-6 border border-slate-100 dark:border-slate-800 shadow-sm">
                            <div className="flex items-start justify-between gap-4">
                                <div className="flex-1">
                                    <div className="flex items-center gap-2 flex-wrap mb-2">
                                        <span className={cn('text-xs font-semibold px-2.5 py-1 rounded-full', PLATFORM_COLORS[r.platform] ?? 'bg-slate-100 text-slate-600')}>{r.platform}</span>
                                        <span className={cn('text-xs font-semibold px-2.5 py-1 rounded-full', SENTIMENT_BADGE[r.sentiment])}>{r.sentiment}</span>
                                        {r.hasResponse && <span className="text-xs font-semibold px-2.5 py-1 rounded-full bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300 flex items-center gap-1"><CheckCircle className="w-3 h-3" /> Responded</span>}
                                    </div>
                                    <div className="flex items-center gap-2 mb-1">
                                        <StarDisplay rating={r.rating} />
                                        <span className="font-semibold text-slate-900 dark:text-white text-sm">{r.reviewerName}</span>
                                        <span className="text-xs text-slate-400">{new Date(r.reviewDate).toLocaleDateString()}</span>
                                    </div>
                                    {r.reviewText && <p className="text-sm text-slate-600 dark:text-slate-300 mt-2 leading-relaxed">{r.reviewText}</p>}
                                    {r.responseText && (
                                        <div className="mt-3 pl-4 border-l-2 border-primary-300 dark:border-primary-700">
                                            <p className="text-xs font-semibold text-primary-600 dark:text-primary-400 mb-1">Your response</p>
                                            <p className="text-sm text-slate-600 dark:text-slate-300">{r.responseText}</p>
                                        </div>
                                    )}
                                </div>
                                {!r.hasResponse && (
                                    <button
                                        onClick={() => { setRespondingTo(r.id); setResponseText(''); }}
                                        className="flex items-center gap-1.5 px-3 py-2 bg-primary-50 dark:bg-primary-900/20 text-primary-600 dark:text-primary-400 rounded-lg text-sm font-medium hover:bg-primary-100 transition-colors shrink-0"
                                    >
                                        <MessageSquare className="w-4 h-4" />
                                        Respond
                                    </button>
                                )}
                            </div>

                            {/* Inline respond form */}
                            {respondingTo === r.id && (
                                <div className="mt-4 space-y-3 border-t border-slate-100 dark:border-slate-800 pt-4">
                                    <p className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Quick Templates</p>
                                    <div className="flex gap-2 flex-wrap">
                                        {RESPONSE_TEMPLATES.map((t, i) => (
                                            <button key={i} onClick={() => setResponseText(t)} className="text-xs px-3 py-1.5 bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 rounded-lg hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors">
                                                Template {i + 1}
                                            </button>
                                        ))}
                                    </div>
                                    <textarea
                                        value={responseText}
                                        onChange={(e) => setResponseText(e.target.value)}
                                        rows={3}
                                        placeholder="Write your response…"
                                        className="w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-900 dark:text-white resize-none"
                                    />
                                    <div className="flex gap-2">
                                        <button onClick={submitResponse} disabled={!responseText || submitting} className="px-4 py-2 bg-primary-500 text-white rounded-lg text-sm font-medium disabled:opacity-50 flex items-center gap-1.5">
                                            <Send className="w-3.5 h-3.5" />{submitting ? 'Sending…' : 'Post Response'}
                                        </button>
                                        <button onClick={() => setRespondingTo(null)} className="px-4 py-2 text-slate-600 dark:text-slate-300 rounded-lg text-sm font-medium">Cancel</button>
                                    </div>
                                </div>
                            )}
                        </div>
                    ))}
                </div>
            )}

            {/* Import Review Modal */}
            {showAddModal && (
                <div className="fixed inset-0 bg-slate-900/60 backdrop-blur-sm z-50 flex items-center justify-center p-4">
                    <div className="bg-white dark:bg-slate-900 rounded-2xl p-6 w-full max-w-md shadow-2xl border border-slate-100 dark:border-slate-800">
                        <h3 className="font-bold text-slate-900 dark:text-white mb-4">Import External Review</h3>
                        <div className="space-y-3">
                            <div>
                                <label className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Platform</label>
                                <select value={newReview.platform} onChange={e => setNewReview(p => ({...p, platform: e.target.value}))} className="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-900 dark:text-white">
                                    {['Google','Yelp','Facebook','Upkilo'].map(p => <option key={p}>{p}</option>)}
                                </select>
                            </div>
                            <div>
                                <label className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Reviewer Name</label>
                                <input value={newReview.reviewerName} onChange={e => setNewReview(p => ({...p, reviewerName: e.target.value}))} className="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-900 dark:text-white" placeholder="Jane Doe" />
                            </div>
                            <div>
                                <label className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Rating</label>
                                <div className="flex gap-2 mt-1">
                                    {[1,2,3,4,5].map(r => (
                                        <button key={r} onClick={() => setNewReview(p => ({...p, rating: r}))} className={cn('w-10 h-10 rounded-lg font-bold text-sm transition-all', newReview.rating >= r ? 'bg-amber-400 text-white' : 'bg-slate-100 dark:bg-slate-800 text-slate-500')}>{r}</button>
                                    ))}
                                </div>
                            </div>
                            <div>
                                <label className="text-xs font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wide">Review Text</label>
                                <textarea value={newReview.reviewText} onChange={e => setNewReview(p => ({...p, reviewText: e.target.value}))} rows={3} className="mt-1 w-full px-3 py-2 text-sm bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg text-slate-900 dark:text-white resize-none" placeholder="Review content…" />
                            </div>
                        </div>
                        <div className="flex gap-2 mt-4">
                            <button onClick={submitReview} disabled={!newReview.reviewerName || submitting} className="flex-1 py-2.5 bg-primary-500 text-white rounded-xl font-semibold text-sm disabled:opacity-50">
                                {submitting ? 'Importing…' : 'Import Review'}
                            </button>
                            <button onClick={() => setShowAddModal(false)} className="px-4 py-2.5 text-slate-600 dark:text-slate-300 rounded-xl font-semibold text-sm">Cancel</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}