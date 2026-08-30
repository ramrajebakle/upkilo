'use client';

import { useState } from 'react';

interface OpenSlot {
  staffName: string;
  start: string;
  end: string;
  durationMinutes: number;
}

interface MatchedClient {
  clientId: string;
  name: string;
  phone: string | null;
  lastServiceName: string;
  daysSinceLastVisit: number;
  hasSmsConsent: boolean;
  score: number;
}

interface SlotMatch {
  slot: OpenSlot;
  clients: MatchedClient[];
}

interface FillCalendarData {
  totalOpenSlots: number;
  totalMatches: number;
  matches: SlotMatch[];
}

interface SendItem {
  clientId: string;
  phone: string;
  message: string;
  clientName: string;
  slotStart: string;
}

interface FillMyCalendarModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export default function FillMyCalendarModal({ isOpen, onClose }: FillMyCalendarModalProps) {
  const [step, setStep] = useState<'idle' | 'loading' | 'preview' | 'generating' | 'confirm' | 'sent'>('idle');
  const [data, setData] = useState<FillCalendarData | null>(null);
  const [sendItems, setSendItems] = useState<SendItem[]>([]);
  const [selectedItems, setSelectedItems] = useState<Set<string>>(new Set());
  const [sentCount, setSentCount] = useState(0);

  if (!isOpen) return null;

  const handleScan = async () => {
    setStep('loading');
    try {
      const res = await fetch('/api/v1/ai/fill-my-calendar?daysAhead=7', {
        headers: { Authorization: `Bearer ${localStorage.getItem('token')}` }
      });
      const json = await res.json();
      setData(json);
      setStep('preview');
    } catch {
      setStep('idle');
      alert('Failed to scan calendar. Please try again.');
    }
  };

  const handleGenerateSms = async () => {
    if (!data) return;
    setStep('generating');

    const items: SendItem[] = [];
    for (const match of data.matches.slice(0, 10)) {
      const topClient = match.clients[0];
      if (!topClient?.hasSmsConsent || !topClient.phone) continue;

      try {
        const res = await fetch('/api/v1/ai/fill-my-calendar/generate-sms', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${localStorage.getItem('token')}`
          },
          body: JSON.stringify({
            clientId: topClient.clientId,
            clientName: topClient.name,
            lastServiceName: topClient.lastServiceName,
            daysSinceLastVisit: topClient.daysSinceLastVisit,
            slotStart: match.slot.start
          })
        });
        const json = await res.json();
        if (json.message) {
          items.push({
            clientId: topClient.clientId,
            phone: topClient.phone,
            message: json.message,
            clientName: topClient.name,
            slotStart: match.slot.start
          });
        }
      } catch {
        // Skip failed items
      }
    }

    setSendItems(items);
    setSelectedItems(new Set(items.map(i => i.clientId)));
    setStep('confirm');
  };

  const handleSend = async () => {
    const toSend = sendItems.filter(i => selectedItems.has(i.clientId));
    try {
      const res = await fetch('/api/v1/ai/fill-my-calendar/send', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify({ items: toSend })
      });
      const json = await res.json();
      setSentCount(json.sent || 0);
      setStep('sent');
    } catch {
      alert('Failed to send messages. Please try again.');
    }
  };

  const toggleItem = (clientId: string) => {
    setSelectedItems(prev => {
      const next = new Set(prev);
      if (next.has(clientId)) next.delete(clientId);
      else next.add(clientId);
      return next;
    });
  };

  const formatTime = (iso: string) =>
    new Date(iso).toLocaleString('en-US', { weekday: 'short', month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' });

  return (
    <div className="fixed inset-0 z-50 bg-black/60 flex items-center justify-center p-4">
      <div className="bg-card rounded-2xl shadow-2xl w-full max-w-2xl max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="p-6 border-b flex items-center justify-between">
          <div>
            <h2 className="text-xl font-bold text-foreground">✨ Fill My Calendar</h2>
            <p className="text-sm text-foreground-secondary mt-1">AI finds open slots and matches lapsed clients for outreach</p>
          </div>
          <button onClick={onClose} className="text-foreground-muted hover:text-foreground-secondary text-2xl leading-none">&times;</button>
        </div>

        {/* Body */}
        <div className="flex-1 overflow-y-auto p-6">
          {step === 'idle' && (
            <div className="text-center py-8">
              <div className="text-6xl mb-4">📅</div>
              <h3 className="text-lg font-semibold text-foreground mb-2">Fill gaps in your schedule</h3>
              <p className="text-foreground-secondary mb-6 max-w-md mx-auto">
                Upkilo AI will scan your next 7 days for open slots, match them with clients who are due for a visit, and generate personalized SMS messages — ready to send in one click.
              </p>
              <button
                onClick={handleScan}
                className="bg-primary-600 text-white px-8 py-3 rounded-xl font-semibold hover:bg-primary-700 transition-colors"
              >
                Scan My Calendar
              </button>
            </div>
          )}

          {step === 'loading' && (
            <div className="text-center py-12">
              <div className="animate-spin w-10 h-10 border-4 border-primary-600 border-t-transparent rounded-full mx-auto mb-4" />
              <p className="text-foreground-secondary">Scanning your calendar and matching clients...</p>
            </div>
          )}

          {step === 'preview' && data && (
            <div>
              <div className="grid grid-cols-2 gap-4 mb-6">
                <div className="bg-brand-subtle rounded-xl p-4 text-center">
                  <p className="text-3xl font-bold text-primary">{data.totalOpenSlots}</p>
                  <p className="text-sm text-primary font-medium mt-1">Open Slots This Week</p>
                </div>
                <div className="bg-green-50 rounded-xl p-4 text-center">
                  <p className="text-3xl font-bold text-green-700">{data.totalMatches}</p>
                  <p className="text-sm text-success-fg font-medium mt-1">Client Matches Found</p>
                </div>
              </div>

              <div className="space-y-3 mb-6">
                {data.matches.slice(0, 5).map((match, i) => (
                  <div key={i} className="border border-border rounded-xl p-4">
                    <p className="text-sm font-semibold text-foreground mb-1">
                      {formatTime(match.slot.start)} — {match.slot.durationMinutes} min with {match.slot.staffName}
                    </p>
                    <div className="flex flex-wrap gap-2">
                      {match.clients.slice(0, 3).map(c => (
                        <span key={c.clientId} className="bg-muted text-foreground text-xs px-2 py-1 rounded-full">
                          {c.name} ({c.daysSinceLastVisit}d since visit)
                          {!c.hasSmsConsent && <span className="text-red-400 ml-1">no SMS</span>}
                        </span>
                      ))}
                    </div>
                  </div>
                ))}
              </div>

              <button
                onClick={handleGenerateSms}
                className="w-full bg-primary-600 text-white py-3 rounded-xl font-semibold hover:bg-primary-700 transition-colors"
              >
                Generate Personalized SMS Messages →
              </button>
            </div>
          )}

          {step === 'generating' && (
            <div className="text-center py-12">
              <div className="animate-spin w-10 h-10 border-4 border-primary-600 border-t-transparent rounded-full mx-auto mb-4" />
              <p className="text-foreground-secondary">Writing personalized messages for each client...</p>
            </div>
          )}

          {step === 'confirm' && (
            <div>
              <p className="text-sm text-foreground-secondary mb-4">
                Review and select messages to send. Uncheck any you don't want to include.
              </p>
              <div className="space-y-3 mb-6">
                {sendItems.map(item => (
                  <label key={item.clientId} className={`flex gap-3 border rounded-xl p-4 cursor-pointer transition-colors ${selectedItems.has(item.clientId) ? 'border-primary-400 bg-brand-subtle' : 'border-border'}`}>
                    <input
                      type="checkbox"
                      checked={selectedItems.has(item.clientId)}
                      onChange={() => toggleItem(item.clientId)}
                      className="mt-1 accent-primary-600"
                    />
                    <div className="flex-1 min-w-0">
                      <p className="font-semibold text-foreground text-sm">{item.clientName}</p>
                      <p className="text-xs text-foreground-secondary mb-1">{item.phone} · {formatTime(item.slotStart)}</p>
                      <p className="text-sm text-foreground bg-card border border-border rounded-lg p-2">{item.message}</p>
                    </div>
                  </label>
                ))}
              </div>

              <button
                onClick={handleSend}
                disabled={selectedItems.size === 0}
                className="w-full bg-green-600 text-white py-3 rounded-xl font-semibold hover:bg-green-700 disabled:opacity-40 transition-colors"
              >
                Send {selectedItems.size} Message{selectedItems.size !== 1 ? 's' : ''}
              </button>
            </div>
          )}

          {step === 'sent' && (
            <div className="text-center py-8">
              <div className="text-6xl mb-4">🎉</div>
              <h3 className="text-xl font-bold text-foreground mb-2">Messages Sent!</h3>
              <p className="text-foreground-secondary mb-6">
                {sentCount} personalized outreach message{sentCount !== 1 ? 's' : ''} sent. We'll notify you when clients respond.
              </p>
              <button onClick={onClose} className="bg-primary-600 text-white px-6 py-3 rounded-xl font-semibold hover:bg-primary-700">
                Done
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
