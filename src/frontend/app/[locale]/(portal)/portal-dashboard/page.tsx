"use client";

import React, { useState, useEffect, useCallback } from 'react';
import { Calendar, Clock, DollarSign, Star, MessageSquare, User, LogOut, Gift, ChevronRight, Loader2, Download } from 'lucide-react';
import { applyTenantBrand } from '@/lib/brand';

const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

function portalFetch(path: string, opts: any = {}) {
  const token = typeof window !== 'undefined' ? localStorage.getItem('portal_token') : null;
  return fetch(`${API_BASE}${path}`, {
    ...opts,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}), ...opts.headers },
  }).then(async r => {
    if (!r.ok) throw new Error(await r.text());
    return r.json();
  });
}

interface Appointment {
  id: string; date: string; time: string; service: string; staff: string; duration: number; price: number; status: string; canCancel: boolean;
}

export default function PortalDashboard() {
  const [profile, setProfile] = useState<any>(null);
  const [upcoming, setUpcoming] = useState<Appointment[]>([]);
  const [rewards, setRewards] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'upcoming' | 'history' | 'invoices' | 'messages'>('upcoming');
  const [history, setHistory] = useState<any[]>([]);
  const [invoices, setInvoices] = useState<any[]>([]);
  const [messages, setMessages] = useState<any[]>([]);
  const [reviewBooking, setReviewBooking] = useState<any>(null);
  const [reviewRating, setReviewRating] = useState(5);
  const [reviewText, setReviewText] = useState('');
  const [submittingReview, setSubmittingReview] = useState(false);

  const loadData = useCallback(async () => {
    try {
      setLoading(true);
      const [profileRes, upcomingRes, rewardsRes] = await Promise.all([
        portalFetch('/api/client-portal/profile'),
        portalFetch('/api/client-portal/appointments/upcoming'),
        portalFetch('/api/client-portal/rewards'),
      ]);
      setProfile(profileRes);
      setUpcoming(upcomingRes.data || []);
      setRewards(rewardsRes);

      // This block used to set --primary-color and --primary-color-hover but not
      // --primary-color-light, so the tinted surfaces on this page fell back to transparent.
      applyTenantBrand(profileRes.business?.primaryColor);
    } catch (err) {
      console.error('Failed to load portal data:', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { loadData(); }, [loadData]);

  const loadTab = async (tab: string) => {
    setActiveTab(tab as any);
    try {
      if (tab === 'history' && history.length === 0) {
        const res = await portalFetch('/api/client-portal/appointments/history');
        setHistory(res.data || []);
      } else if (tab === 'invoices' && invoices.length === 0) {
        const res = await portalFetch('/api/client-portal/invoices');
        setInvoices(res.data || []);
      } else if (tab === 'messages' && messages.length === 0) {
        const res = await portalFetch('/api/client-portal/messages');
        setMessages(res.data || []);
      }
    } catch (err) {
      console.error(`Failed to load ${tab}:`, err);
    }
  };

  const handleDownloadInvoice = async (invoiceId: string, invoiceNumber: string) => {
    try {
      const token = typeof window !== 'undefined' ? localStorage.getItem('portal_token') : null;
      const response = await fetch(`${API_BASE}/api/v1/export/invoices/${invoiceId}/pdf`, {
        headers: {
          ...(token ? { Authorization: `Bearer ${token}` } : {})
        }
      });
      
      if (!response.ok) throw new Error('Download failed');
      
      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `Invoice-${invoiceNumber || invoiceId}.pdf`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (err) {
      console.error('Failed to download invoice:', err);
      alert('Failed to download invoice. Please try again later.');
    }
  };

  const cancelAppointment = async (id: string) => {
    if (!confirm('Are you sure you want to cancel this appointment?')) return;
    try {
      await portalFetch(`/api/client-portal/appointments/${id}/cancel`, {
        method: 'POST', body: JSON.stringify({ reason: 'Client cancelled via portal' })
      });
      setUpcoming(prev => prev.filter(a => a.id !== id));
    } catch (err: any) {
      alert('Failed to cancel appointment.');
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('portal_token');
    window.location.href = '/portal-login';
  };

  const submitReview = async () => {
    if (!reviewBooking) return;
    try {
      setSubmittingReview(true);
      await portalFetch('/api/v1/reviews/portal/submit', {
        method: 'POST',
        body: JSON.stringify({
          bookingId: reviewBooking.id,
          rating: reviewRating,
          reviewText,
          reviewerName: `${profile?.firstName} ${profile?.lastName}`
        })
      });
      
      // Update local state to hide button
      setHistory(prev => prev.map(a => a.id === reviewBooking.id ? { ...a, hasReview: true } : a));
      setReviewBooking(null);
      setReviewText('');
      setReviewRating(5);
      alert('Thank you for your review!');
    } catch (err) {
      console.error('Failed to submit review:', err);
      alert('Failed to submit review. Please try again.');
    } finally {
      setSubmittingReview(false);
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-muted flex items-center justify-center">
        <Loader2 className="w-8 h-8 text-[var(--primary-color)] animate-spin" />
      </div>
    );
  }

  const statusColors: Record<string, string> = {
    confirmed: 'bg-emerald-100 text-emerald-700',
    pending: 'bg-amber-100 text-amber-700',
    completed: 'bg-blue-100 text-blue-700',
    cancelled: 'bg-red-100 text-red-700',
    noshow: 'bg-muted text-foreground-secondary',
  };

  return (
    <div className="min-h-screen bg-muted">
      {/* Header */}
      <header className="bg-card border-b border-border shadow-sm">
        <div className="max-w-5xl mx-auto px-4 py-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-full bg-[var(--primary-color)] flex items-center justify-center text-[var(--primary-color-foreground)] font-bold ring-2 ring-white shadow-sm overflow-hidden">
              {profile?.business?.logo ? (
                <img src={profile.business.logo} alt={profile.business.name} className="w-full h-full object-cover" />
              ) : (
                profile?.firstName?.[0] || 'U'
              )}
            </div>
            <div>
              <h1 className="font-semibold text-foreground">{profile?.firstName} {profile?.lastName}</h1>
              <p className="text-sm text-foreground-secondary">{profile?.business?.name || profile?.email}</p>
            </div>
          </div>
          <div className="flex items-center gap-4">
            {rewards && (
              <div className="flex items-center gap-2 px-3 py-1.5 bg-amber-50 border border-amber-200 rounded-full">
                <Gift className="w-4 h-4 text-warning-fg" />
                <span className="text-sm font-medium text-amber-700">{rewards.points} pts</span>
                <span className="text-xs text-warning-fg">({rewards.tier})</span>
              </div>
            )}
            <button onClick={handleLogout} className="p-2 text-foreground-muted hover:text-red-500">
              <LogOut className="w-5 h-5" />
            </button>
          </div>
        </div>
      </header>

      <main className="max-w-5xl mx-auto px-4 py-8 space-y-6">
        {/* Quick Stats */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {[
            { label: 'Upcoming', value: upcoming.length, icon: <Calendar className="w-5 h-5 text-[var(--primary-color)]" /> },
            { label: 'Total Visits', value: rewards?.lifetimePoints ? Math.floor(rewards.lifetimePoints / 10) : 0, icon: <Clock className="w-5 h-5 text-blue-500" /> },
            { label: 'Loyalty Points', value: rewards?.points || 0, icon: <Star className="w-5 h-5 text-warning-fg" /> },
            { label: 'Tier', value: rewards?.tier || 'Bronze', icon: <Gift className="w-5 h-5 text-success-fg" /> },
          ].map(s => (
            <div key={s.label} className="bg-card rounded-xl border border-border p-4 shadow-sm">
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm text-foreground-secondary">{s.label}</span>
                {s.icon}
              </div>
              <p className="text-xl font-bold text-foreground">{s.value}</p>
            </div>
          ))}
        </div>

        {/* Tab Navigation */}
        <div className="flex gap-2 bg-card rounded-xl border border-border p-1">
          {[
            { id: 'upcoming', label: 'Upcoming', icon: Calendar },
            { id: 'history', label: 'History', icon: Clock },
            { id: 'invoices', label: 'Invoices', icon: DollarSign },
            { id: 'messages', label: 'Messages', icon: MessageSquare },
          ].map(tab => (
            <button key={tab.id} onClick={() => loadTab(tab.id)}
              className={`flex-1 flex items-center justify-center gap-2 px-4 py-2.5 rounded-lg text-sm font-medium transition-all ${activeTab === tab.id ? 'bg-[var(--primary-color)] text-[var(--primary-color-foreground)] shadow-sm' : 'text-foreground-secondary hover:text-[var(--primary-color)] hover:bg-accent'}`}>
              <tab.icon className="w-4 h-4" /> {tab.label}
            </button>
          ))}
        </div>

        {/* Content */}
        <div className="bg-card rounded-xl border border-border shadow-sm">
          {activeTab === 'upcoming' && (
            <div className="divide-y divide-border-subtle">
              {upcoming.length === 0 ? (
                <div className="p-12 text-center">
                  <Calendar className="w-12 h-12 text-slate-300 mx-auto mb-3" />
                  <p className="text-foreground-secondary">No upcoming appointments</p>
                </div>
              ) : upcoming.map(apt => (
                <div key={apt.id} className="p-4 flex items-center gap-4 hover:bg-accent">
                  <div className="w-14 h-14 rounded-xl bg-brand-subtle flex flex-col items-center justify-center">
                    <span className="text-xs text-primary font-medium">{new Date(apt.date).toLocaleDateString('en-US', { month: 'short' })}</span>
                    <span className="text-lg font-bold text-primary">{new Date(apt.date).getDate()}</span>
                  </div>
                  <div className="flex-1">
                    <p className="font-semibold text-foreground">{apt.service}</p>
                    <p className="text-sm text-foreground-secondary">{apt.time} · {apt.duration}min · {apt.staff}</p>
                  </div>
                  <span className={`text-xs font-medium px-2.5 py-0.5 rounded-full ${statusColors[apt.status] || 'bg-muted'}`}>{apt.status}</span>
                  <span className="font-semibold text-foreground">${apt.price}</span>
                  {apt.canCancel && (
                    <button onClick={() => cancelAppointment(apt.id)} className="text-xs text-danger-fg hover:text-red-700">Cancel</button>
                  )}
                </div>
              ))}
            </div>
          )}

          {activeTab === 'history' && (
            <div className="divide-y divide-border-subtle">
              {history.length === 0 ? (
                <div className="p-12 text-center"><p className="text-foreground-secondary">No past appointments</p></div>
              ) : history.map((apt: any) => (
                <div key={apt.id} className="p-4 flex items-center gap-4">
                  <div className="flex-1">
                    <p className="font-medium text-foreground">{apt.service}</p>
                    <p className="text-sm text-foreground-secondary">{apt.date} at {apt.time} · {apt.staff}</p>
                  </div>
                  <div className="flex flex-col items-end gap-2">
                    <div className="flex items-center gap-2">
                      <span className={`text-xs font-medium px-2.5 py-0.5 rounded-full ${statusColors[apt.status] || 'bg-muted'}`}>{apt.status}</span>
                      <span className="text-foreground font-medium">${apt.price}</span>
                    </div>
                    {apt.status === 'completed' && !apt.hasReview && (
                      <button 
                        onClick={() => setReviewBooking(apt)}
                        className="text-xs text-[var(--primary-color)] hover:text-[var(--primary-color-hover)] font-medium flex items-center gap-1 transition-colors"
                      >
                        <Star className="w-3 h-3" /> Leave Review
                      </button>
                    )}
                    {apt.hasReview && (
                      <span className="text-xs text-foreground-muted flex items-center gap-1">
                        <Star className="w-3 h-3 fill-amber-400 text-amber-400" /> Reviewed
                      </span>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}

          {activeTab === 'invoices' && (
            <div className="divide-y divide-border-subtle">
              {invoices.length === 0 ? (
                <div className="p-12 text-center"><p className="text-foreground-secondary">No invoices found</p></div>
              ) : invoices.map((inv: any) => (
                <div key={inv.id} className="p-4 flex items-center gap-4">
                  <DollarSign className="w-5 h-5 text-success-fg" />
                  <div className="flex-1">
                    <p className="font-medium text-foreground">{inv.invoiceNumber} · ${inv.amount} {inv.currency}</p>
                    <p className="text-sm text-foreground-secondary">{inv.date}</p>
                  </div>
                  <span className={`text-xs font-medium px-2.5 py-0.5 rounded-full ${['paid', 'succeeded'].includes(inv.status) ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'}`}>{inv.status}</span>
                  <button onClick={() => handleDownloadInvoice(inv.id, inv.invoiceNumber)} className="p-2 text-foreground-muted hover:text-[var(--primary-color)] transition-colors">
                    <Download className="w-4 h-4" />
                  </button>
                </div>
              ))}
            </div>
          )}

          {activeTab === 'messages' && (
            <div className="divide-y divide-border-subtle">
              {messages.length === 0 ? (
                <div className="p-12 text-center"><p className="text-foreground-secondary">No messages yet</p></div>
              ) : messages.map((msg: any) => (
                <div key={msg.id} className="p-4">
                  <div className="flex items-center gap-2 mb-1">
                    <span className={`text-xs font-medium ${msg.direction === 'Inbound' ? 'text-blue-500' : 'text-[var(--primary-color)]'}`}>{msg.direction === 'Inbound' ? 'You' : 'Business'}</span>
                    <span className="text-xs text-foreground-muted">{new Date(msg.createdAt).toLocaleString()}</span>
                  </div>
                  {msg.subject && <p className="font-medium text-foreground text-sm">{msg.subject}</p>}
                  <p className="text-sm text-foreground-secondary mt-1">{msg.body}</p>
                </div>
              ))}
            </div>
          )}
        </div>
      </main>

      {/* Review Modal */}
      {reviewBooking && (
        <div className="fixed inset-0 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4 z-50">
          <div className="bg-card rounded-2xl shadow-xl max-w-md w-full p-6 animate-in fade-in zoom-in duration-200">
            <h3 className="text-lg font-bold text-foreground mb-1">Leave a Review</h3>
            <p className="text-sm text-foreground-secondary mb-6">How was your {reviewBooking.service} with {reviewBooking.staff}?</p>
            
            <div className="flex justify-center gap-2 mb-6">
              {[1, 2, 3, 4, 5].map(star => (
                <button 
                  key={star} 
                  onClick={() => setReviewRating(star)}
                  className="transition-transform active:scale-90"
                >
                  <Star className={`w-10 h-10 ${star <= reviewRating ? 'fill-amber-400 text-amber-400' : 'text-slate-200'}`} />
                </button>
              ))}
            </div>
            
            <textarea
              className="w-full rounded-xl border-border focus:ring-[var(--primary-color)] focus:border-[var(--primary-color)] min-h-[100px] text-sm mb-6"
              placeholder="Tell us about your experience (optional)..."
              value={reviewText}
              onChange={(e) => setReviewText(e.target.value)}
            />
            
            <div className="flex gap-3">
              <button 
                onClick={() => setReviewBooking(null)}
                className="flex-1 px-4 py-2 border border-border rounded-lg text-sm font-medium text-foreground-secondary hover:bg-accent"
              >
                Cancel
              </button>
              <button 
                onClick={submitReview}
                disabled={submittingReview}
                className="flex-1 px-4 py-2 bg-[var(--primary-color)] hover:bg-[var(--primary-color-hover)] rounded-lg text-sm font-medium text-[var(--primary-color-foreground)] disabled:opacity-50 flex items-center justify-center gap-2 transition-colors"
              >
                {submittingReview ? <Loader2 className="w-4 h-4 animate-spin" /> : 'Submit Review'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
