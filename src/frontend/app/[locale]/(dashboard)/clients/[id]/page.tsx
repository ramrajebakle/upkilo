'use client';

import { useState, useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import Link from 'next/link';
import {
    ArrowLeft,
    User,
    Mail,
    Phone,
    MapPin,
    Save,
    Trash2,
    AlertTriangle,
    Star,
    Calendar,
    Clock,
    DollarSign,
    MessageSquare,
    Tag,
    History,
    TrendingUp,
    Plus,
} from 'lucide-react';
import { cn, formatCurrency, formatDate } from '@/lib/utils';
import api, { apiClient } from '@/lib/api';
import { useToast } from '@/components/ui/Toast';

interface ClientData {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
    address: string;
    city: string;
    state: string;
    postalCode: string;
    notes: string;
    tags: string[];
    marketingConsent: boolean;
    smsConsent: boolean;
    // Stats
    totalBookings: number;
    totalSpent: number;
    lastVisit: string | null;
    rating: number;
    memberSince: string;
    loyaltyPoints: number;
    loyaltyTier: string;
}

interface LoyaltyTransaction {
    date: string;
    points: number;
    description: string;
    type: string;
}

interface ClientNote {
    id: string;
    content: string;
    isPrivate: boolean;
    category?: string;
    createdBy: string;
    createdAt: string;
}

interface CommunicationLog {
    id: string;
    type: 'Email' | 'SMS' | 'Call';
    direction?: 'Inbound' | 'Outbound' | 0 | 1;
    subject: string;
    content: string; // or body
    body?: string;   // backend property
    sentAt: string;
    createdAt?: string; // backend property
    status: 'Sent' | 'Failed' | 'Pending' | 'Received' | 'Delivered';
}

const availableTags = ['VIP', 'Regular', 'New', 'Premium', 'Loyal', 'At-Risk'];

export default function EditClientPage() {
    const router = useRouter();
    const params = useParams();
    const clientId = params.id as string;

    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [showDeleteModal, setShowDeleteModal] = useState(false);
    const [client, setClient] = useState<ClientData | null>(null);
    const [activeTab, setActiveTab] = useState<'overview' | 'notes' | 'communications' | 'loyalty' | 'activity'>('overview');
    const [activities, setActivities] = useState<{ type: string; description: string; date: string }[]>([]);
    const [loadingActivities, setLoadingActivities] = useState(false);

    // Notes & Communications & Loyalty State
    const [notes, setNotes] = useState<ClientNote[]>([]);
    const [communications, setCommunications] = useState<CommunicationLog[]>([]);
    const [loyaltyHistory, setLoyaltyHistory] = useState<LoyaltyTransaction[]>([]);
    const [loadingNotes, setLoadingNotes] = useState(false);
    const [loadingComm, setLoadingComm] = useState(false);
    const [loadingLoyalty, setLoadingLoyalty] = useState(false);
    const [newNote, setNewNote] = useState('');
    const [isPrivateNote, setIsPrivateNote] = useState(true);
    const [newMessage, setNewMessage] = useState('');
    const [sendingSms, setSendingSms] = useState(false);
    const { error: toastError, success: toastSuccess } = useToast();

    const [formData, setFormData] = useState({
        firstName: '',
        lastName: '',
        email: '',
        phone: '',
        address: '',
        city: '',
        state: '',
        postalCode: '',
        notes: '',
        tags: [] as string[],
        marketingConsent: false,
        smsConsent: false,
    });

    useEffect(() => {
        const fetchClient = async () => {
            setLoading(true);
            try {
                const response = await api.clients.get(clientId);
                const data = response.data;
                setClient(data);
                setFormData({
                    firstName: data.firstName || '',
                    lastName: data.lastName || '',
                    email: data.email || '',
                    phone: data.phone || '',
                    address: data.address || '',
                    city: data.city || '',
                    state: data.state || '',
                    postalCode: data.postalCode || '',
                    notes: data.notes || '',
                    tags: data.tags || [],
                    marketingConsent: data.marketingConsent || false,
                    smsConsent: data.smsConsent || false,
                });
            } catch (error) {
                console.error('Failed to fetch client:', error);
                toastError('Failed to load client details');
            } finally {
                setLoading(false);
            }
        };

        if (clientId) {
            fetchClient();
        }
    }, [clientId]);

    useEffect(() => {
        if (activeTab === 'notes' && clientId) {
            fetchNotes();
        } else if (activeTab === 'communications' && clientId) {
            fetchCommunications();
        } else if (activeTab === 'loyalty' && clientId) {
            fetchLoyalty();
        } else if (activeTab === 'activity' && clientId) {
            setLoadingActivities(true);
            apiClient.get(`/api/v1/clients/${clientId}/activities`).catch(() => ({ data: [] })).then((r) => {
                setActivities(Array.isArray(r.data) ? r.data : r.data?.data ?? []);
            }).finally(() => setLoadingActivities(false));
        }
    }, [activeTab, clientId]);

    const fetchNotes = async () => {
        setLoadingNotes(true);
        try {
            const res = await api.clients.notes(clientId);
            setNotes(res.data);
        } catch (error) {
            toastError('Failed to load notes');
        } finally {
            setLoadingNotes(false);
        }
    };

    const fetchCommunications = async () => {
        setLoadingComm(true);
        try {
            const res = await api.clients.communications(clientId);
            setCommunications(res.data);
        } catch (error) {
            toastError('Failed to load communications');
        } finally {
            setLoadingComm(false);
        }
    };

    const fetchLoyalty = async () => {
        setLoadingLoyalty(true);
        try {
            const res = await api.clients.loyalty(clientId);
            setLoyaltyHistory(res.data.history || []);
            // Update client points/tier from response if needed (optional since we have it in client detail)
        } catch (error) {
            toastError('Failed to load loyalty history');
        } finally {
            setLoadingLoyalty(false);
        }
    };

    const handleAddNote = async () => {
        if (!newNote.trim()) return;
        try {
            await api.clients.addNote(clientId, {
                content: newNote,
                isPrivate: isPrivateNote
            });
            setNewNote('');
            fetchNotes();
            toastSuccess('Note added successfully');
        } catch (error) {
            toastError('Failed to add note');
        }
    };

    const handleSendSms = async () => {
        if (!newMessage.trim()) return;
        setSendingSms(true);
        try {
            await api.communications.sendSms(clientId, newMessage);
            setNewMessage('');
            fetchCommunications(); // Refresh chat
            toastSuccess('SMS sent');
        } catch (error) {
            console.error(error);
            toastError('Failed to send SMS');
        } finally {
            setSendingSms(false);
        }
    };

    const handleSave = async () => {
        setSaving(true);
        try {
            await api.clients.update(clientId, formData);
            toastSuccess('Client updated successfully');
            router.push('/clients');
        } catch (error) {
            toastError('Failed to update client');
        } finally {
            setSaving(false);
        }
    };

    const handleDelete = async () => {
        setSaving(true);
        try {
            await api.clients.delete(clientId);
            toastSuccess('Client deleted successfully');
            router.push('/clients?deleted=true');
        } catch (error) {
            toastError('Failed to delete client');
            setSaving(false);
        }
    };

    const toggleTag = (tag: string) => {
        if (formData.tags.includes(tag)) {
            setFormData({ ...formData, tags: formData.tags.filter(t => t !== tag) });
        } else {
            setFormData({ ...formData, tags: [...formData.tags, tag] });
        }
    };

    if (loading) {
        return (
            <div className="max-w-4xl mx-auto animate-pulse">
                <div className="h-8 bg-slate-200 rounded w-1/3 mb-6" />
                <div className="grid grid-cols-3 gap-6">
                    <div className="col-span-2 card-elevated p-6 space-y-4">
                        <div className="h-20 bg-slate-200 rounded" />
                        <div className="h-40 bg-slate-200 rounded" />
                    </div>
                    <div className="card-elevated p-6">
                        <div className="h-60 bg-slate-200 rounded" />
                    </div>
                </div>
            </div>
        );
    }

    if (!client) {
        return (
            <div className="text-center py-20">
                <AlertTriangle className="h-12 w-12 text-amber-500 mx-auto mb-4" />
                <h2 className="text-xl font-semibold text-slate-900">Client Not Found</h2>
                <Link href="/clients" className="btn btn-primary mt-6">Back to Clients</Link>
            </div>
        );
    }

    return (
        <div className="max-w-4xl mx-auto">
            {/* Header */}
            <div className="flex items-center justify-between gap-4 mb-8 animate-fade-in-up">
                <div className="flex items-center gap-4">
                    <Link href="/clients" className="p-2 hover:bg-slate-100 rounded-xl transition-colors">
                        <ArrowLeft className="h-5 w-5 text-slate-600" />
                    </Link>
                    <div className="flex items-center gap-4">
                        <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-rose-400 to-pink-600 flex items-center justify-center text-white font-bold text-xl">
                            {client.firstName[0]}{client.lastName[0]}
                        </div>
                        <div>
                            <div className="flex items-center gap-3">
                                <h1 className="text-2xl font-bold text-slate-900" style={{ fontFamily: 'var(--font-display)' }}>
                                    {client.firstName} {client.lastName}
                                </h1>
                                <span className={cn(
                                    "px-3 py-1 rounded-full text-xs font-bold uppercase tracking-wider",
                                    client.loyaltyTier === 'Platinum' ? 'bg-slate-800 text-white border border-slate-700' :
                                        client.loyaltyTier === 'Gold' ? 'bg-amber-100 text-amber-700 border border-amber-200' :
                                            client.loyaltyTier === 'Silver' ? 'bg-slate-100 text-slate-600 border border-slate-200' :
                                                'bg-orange-50 text-orange-700 border border-orange-100'
                                )}>
                                    {client.loyaltyTier || 'Bronze'}
                                </span>
                            </div>
                            <p className="text-slate-500">Client since {formatDate(client.memberSince)} • {client.loyaltyPoints || 0} Points</p>
                        </div>
                    </div>
                </div>
                <div className="flex gap-2">
                    <button
                        onClick={() => setShowDeleteModal(true)}
                        className="btn btn-secondary text-red-600 hover:bg-red-50"
                    >
                        <Trash2 className="h-4 w-4" />
                        Delete
                    </button>
                    <button onClick={handleSave} disabled={saving} className="btn btn-primary">
                        {saving ? 'Saving...' : 'Save Changes'}
                    </button>
                </div>
            </div>

            {/* Tabs */}
            <div className="flex gap-4 border-b border-slate-200 mb-6 overflow-x-auto">
                {['overview', 'notes', 'communications', 'loyalty', 'activity'].map((tab) => (
                    <button
                        key={tab}
                        onClick={() => setActiveTab(tab as any)}
                        className={cn(
                            'px-4 py-3 text-sm font-medium border-b-2 transition-colors whitespace-nowrap',
                            activeTab === tab
                                ? 'border-primary-500 text-primary-600'
                                : 'border-transparent text-slate-600 hover:text-slate-900'
                        )}
                    >
                        {tab.charAt(0).toUpperCase() + tab.slice(1)}
                    </button>
                ))}
            </div>

            {activeTab === 'overview' && (
                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 animate-fade-in">
                    {/* Main Form */}
                    <div className="lg:col-span-2 space-y-6">
                        {/* Basic Info */}
                        <div className="card-elevated p-6">
                            <div className="flex items-center gap-3 mb-6">
                                <div className="p-2 bg-blue-100 rounded-lg">
                                    <User className="h-5 w-5 text-blue-600" />
                                </div>
                                <h2 className="text-lg font-semibold text-slate-900">Basic Information</h2>
                            </div>

                            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">First Name</label>
                                    <input
                                        type="text"
                                        value={formData.firstName}
                                        onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                                        className="input"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">Last Name</label>
                                    <input
                                        type="text"
                                        value={formData.lastName}
                                        onChange={(e) => setFormData({ ...formData, lastName: e.target.value })}
                                        className="input"
                                    />
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">Email</label>
                                    <div className="relative">
                                        <Mail className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                                        <input
                                            type="email"
                                            value={formData.email}
                                            onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                                            className="input pl-11"
                                        />
                                    </div>
                                </div>
                                <div>
                                    <label className="block text-sm font-medium text-slate-700 mb-2">Phone</label>
                                    <div className="relative">
                                        <Phone className="absolute left-4 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
                                        <input
                                            type="tel"
                                            value={formData.phone}
                                            onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                                            className="input pl-11"
                                        />
                                    </div>
                                </div>
                            </div>
                        </div>

                        {/* Address */}
                        <div className="card-elevated p-6">
                            <div className="flex items-center gap-3 mb-6">
                                <div className="p-2 bg-emerald-100 rounded-lg">
                                    <MapPin className="h-5 w-5 text-emerald-600" />
                                </div>
                                <h2 className="text-lg font-semibold text-slate-900">Address</h2>
                            </div>

                            <div className="space-y-4">
                                <input
                                    type="text"
                                    value={formData.address}
                                    onChange={(e) => setFormData({ ...formData, address: e.target.value })}
                                    className="input"
                                    placeholder="Street Address"
                                />
                                <div className="grid grid-cols-3 gap-4">
                                    <input
                                        type="text"
                                        value={formData.city}
                                        onChange={(e) => setFormData({ ...formData, city: e.target.value })}
                                        className="input"
                                        placeholder="City"
                                    />
                                    <input
                                        type="text"
                                        value={formData.state}
                                        onChange={(e) => setFormData({ ...formData, state: e.target.value })}
                                        className="input"
                                        placeholder="State"
                                    />
                                    <input
                                        type="text"
                                        value={formData.postalCode}
                                        onChange={(e) => setFormData({ ...formData, postalCode: e.target.value })}
                                        className="input"
                                        placeholder="Postal Code"
                                    />
                                </div>
                            </div>
                        </div>

                        {/* Tags */}
                        <div className="card-elevated p-6">
                            <div className="flex items-center gap-3 mb-4">
                                <div className="p-2 bg-primary-100 rounded-lg">
                                    <Tag className="h-5 w-5 text-primary-600" />
                                </div>
                                <h2 className="text-lg font-semibold text-slate-900">Tags</h2>
                            </div>
                            <div className="flex flex-wrap gap-2">
                                {availableTags.map((tag) => (
                                    <button
                                        key={tag}
                                        onClick={() => toggleTag(tag)}
                                        className={cn(
                                            'px-4 py-2 rounded-lg text-sm font-medium transition-all',
                                            formData.tags.includes(tag)
                                                ? 'bg-primary-500 text-white shadow-lg shadow-primary-500/25'
                                                : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                                        )}
                                    >
                                        {tag}
                                    </button>
                                ))}
                            </div>
                        </div>

                        {/* Notes (Deprecated in Overview, kept for compat or moved to Tab) */}
                        <div className="card-elevated p-6">
                            <div className="flex items-center gap-3 mb-4">
                                <MessageSquare className="h-5 w-5 text-slate-400" />
                                <h2 className="text-lg font-semibold text-slate-900">General Notes</h2>
                            </div>
                            <textarea
                                value={formData.notes}
                                onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                                className="w-full px-4 py-3 rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-primary-500 transition-all resize-none"
                                rows={4}
                                placeholder="Add general notes about this client..."
                            />
                        </div>
                    </div>

                    {/* Sidebar - Stats */}
                    <div className="space-y-6">
                        <div className="card-elevated p-6">
                            <h3 className="font-semibold text-slate-900 mb-4">Client Stats</h3>
                            <div className="space-y-4">
                                <div className="flex items-center justify-between p-3 bg-slate-50 rounded-xl">
                                    <div className="flex items-center gap-3">
                                        <Calendar className="h-5 w-5 text-blue-500" />
                                        <span className="text-slate-600">Total Bookings</span>
                                    </div>
                                    <span className="font-bold text-slate-900">{client.totalBookings}</span>
                                </div>
                                <div className="flex items-center justify-between p-3 bg-slate-50 rounded-xl">
                                    <div className="flex items-center gap-3">
                                        <DollarSign className="h-5 w-5 text-emerald-500" />
                                        <span className="text-slate-600">Total Spent</span>
                                    </div>
                                    <span className="font-bold text-emerald-600">{formatCurrency(client.totalSpent)}</span>
                                </div>
                                <div className="flex items-center justify-between p-3 bg-slate-50 rounded-xl">
                                    <div className="flex items-center gap-3">
                                        <Clock className="h-5 w-5 text-amber-500" />
                                        <span className="text-slate-600">Last Visit</span>
                                    </div>
                                    <span className="font-medium text-slate-900">{client.lastVisit ? formatDate(client.lastVisit) : 'Never'}</span>
                                </div>
                                <div className="flex items-center justify-between p-3 bg-slate-50 rounded-xl">
                                    <div className="flex items-center gap-3">
                                        <Star className="h-5 w-5 text-amber-400" />
                                        <span className="text-slate-600">Rating</span>
                                    </div>
                                    <span className="font-bold text-slate-900">{client.rating}</span>
                                </div>
                            </div>
                        </div>

                        <div className="card-elevated p-6">
                            <h3 className="font-semibold text-slate-900 mb-4">Communication</h3>
                            <div className="space-y-3">
                                <label className="flex items-center justify-between p-3 bg-slate-50 rounded-xl cursor-pointer">
                                    <span className="text-slate-600">Marketing Emails</span>
                                    <button
                                        type="button"
                                        onClick={() => setFormData({ ...formData, marketingConsent: !formData.marketingConsent })}
                                        className={cn(
                                            'relative w-10 h-5 rounded-full transition-colors',
                                            formData.marketingConsent ? 'bg-primary-500' : 'bg-slate-300'
                                        )}
                                    >
                                        <span className={cn(
                                            'absolute top-0.5 w-4 h-4 bg-white rounded-full shadow transition-all',
                                            formData.marketingConsent ? 'left-5' : 'left-0.5'
                                        )} />
                                    </button>
                                </label>
                                <label className="flex items-center justify-between p-3 bg-slate-50 rounded-xl cursor-pointer">
                                    <span className="text-slate-600">SMS Notifications</span>
                                    <button
                                        type="button"
                                        onClick={() => setFormData({ ...formData, smsConsent: !formData.smsConsent })}
                                        className={cn(
                                            'relative w-10 h-5 rounded-full transition-colors',
                                            formData.smsConsent ? 'bg-primary-500' : 'bg-slate-300'
                                        )}
                                    >
                                        <span className={cn(
                                            'absolute top-0.5 w-4 h-4 bg-white rounded-full shadow transition-all',
                                            formData.smsConsent ? 'left-5' : 'left-0.5'
                                        )} />
                                    </button>
                                </label>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {activeTab === 'notes' && (
                <div className="animate-fade-in space-y-6">
                    {/* Add Note */}
                    <div className="card-elevated p-6">
                        <h2 className="text-lg font-semibold text-slate-900 mb-4">Add Note</h2>
                        <div className="space-y-4">
                            <textarea
                                value={newNote}
                                onChange={(e) => setNewNote(e.target.value)}
                                className="w-full px-4 py-3 rounded-xl border border-slate-200 focus:outline-none focus:ring-2 focus:ring-primary-500 resize-none"
                                rows={3}
                                placeholder="Type your note here..."
                            />
                            <div className="flex items-center justify-between">
                                <label className="flex items-center gap-2 cursor-pointer">
                                    <input
                                        type="checkbox"
                                        checked={isPrivateNote}
                                        onChange={(e) => setIsPrivateNote(e.target.checked)}
                                        className="rounded border-slate-300 text-primary-600 focus:ring-primary-500"
                                    />
                                    <span className="text-sm text-slate-600">Private Note</span>
                                </label>
                                <button
                                    onClick={handleAddNote}
                                    disabled={!newNote.trim()}
                                    className="btn btn-primary"
                                >
                                    <Plus className="h-4 w-4" />
                                    Add Note
                                </button>
                            </div>
                        </div>
                    </div>

                    {/* Notes List */}
                    <div className="space-y-4">
                        {loadingNotes ? (
                            <div className="text-center py-10 text-slate-500">Loading notes...</div>
                        ) : notes.length === 0 ? (
                            <div className="text-center py-10 text-slate-500 card-elevated">No notes found.</div>
                        ) : (
                            notes.map((note) => (
                                <div key={note.id} className="card-elevated p-6">
                                    <div className="flex items-center justify-between mb-2">
                                        <div className="flex items-center gap-2">
                                            <span className="font-medium text-slate-900">{note.createdBy}</span>
                                            {note.isPrivate && (
                                                <span className="px-2 py-0.5 text-xs font-medium bg-amber-100 text-amber-700 rounded-full">Private</span>
                                            )}
                                        </div>
                                        <span className="text-sm text-slate-500">{formatDate(note.createdAt)}</span>
                                    </div>
                                    <p className="text-slate-600 whitespace-pre-wrap">{note.content}</p>
                                </div>
                            ))
                        )}
                    </div>
                </div>
            )}

            {activeTab === 'communications' && (
                <div className="animate-fade-in space-y-6">
                    <div className="card-elevated p-6 flex flex-col h-[600px]">
                        <div className="flex items-center justify-between mb-4 border-b border-slate-100 pb-4">
                            <h2 className="text-lg font-semibold text-slate-900">SMS & Chat</h2>
                            <div className="flex items-center gap-2 text-sm text-slate-500">
                                <span className="w-2 h-2 bg-emerald-500 rounded-full animate-pulse"></span>
                                Live capable
                            </div>
                        </div>

                        {/* Chat Window */}
                        <div className="flex-1 overflow-y-auto space-y-4 pr-2 mb-4">
                            {loadingComm ? (
                                <div className="text-center py-10 text-slate-500">Loading history...</div>
                            ) : communications.length === 0 ? (
                                <div className="text-center py-10 text-slate-500">No communication history found. Start a conversation!</div>
                            ) : (
                                communications.map((log) => {
                                    const isInbound = log.direction === 'Inbound' || log.direction === 0;
                                    const messageText = log.body || log.content || log.subject; // Fallback
                                    return (
                                        <div key={log.id} className={cn(
                                            "flex w-full",
                                            isInbound ? "justify-start" : "justify-end"
                                        )}>
                                            <div className={cn(
                                                "max-w-[70%] p-3 rounded-2xl text-sm relative group",
                                                isInbound
                                                    ? "bg-slate-100 text-slate-800 rounded-bl-sm"
                                                    : "bg-primary-500 text-white rounded-br-sm"
                                            )}>
                                                <p>{messageText}</p>
                                                <div className={cn(
                                                    "text-[10px] mt-1 opacity-70 flex items-center gap-1",
                                                    isInbound ? "text-slate-500" : "text-primary-100"
                                                )}>
                                                    {formatDate(log.createdAt || log.sentAt)}
                                                    {!isInbound && (
                                                        <span>• {log.status}</span>
                                                    )}
                                                </div>
                                            </div>
                                        </div>
                                    );
                                })
                            )}
                        </div>

                        {/* Input Area */}
                        <div className="pt-4 border-t border-slate-100 flex gap-3">
                            <input
                                type="text"
                                value={newMessage}
                                onChange={(e) => setNewMessage(e.target.value)}
                                onKeyDown={(e) => {
                                    if (e.key === 'Enter' && !e.shiftKey) {
                                        e.preventDefault();
                                        handleSendSms();
                                    }
                                }}
                                className="flex-1 input"
                                placeholder="Type an SMS message..."
                            />
                            <button
                                onClick={handleSendSms}
                                disabled={sendingSms || !newMessage.trim()}
                                className="btn btn-primary px-6"
                            >
                                {sendingSms ? <Clock className="h-4 w-4 animate-spin" /> : <MessageSquare className="h-4 w-4" />}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {activeTab === 'loyalty' && (
                <div className="animate-fade-in space-y-6">
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                        <div className="card-elevated p-6 flex flex-col items-center justify-center text-center">
                            <div className="w-12 h-12 bg-amber-100 rounded-full flex items-center justify-center mb-4">
                                <Star className="h-6 w-6 text-amber-600" />
                            </div>
                            <h3 className="text-3xl font-bold text-slate-900 mb-1">{client.loyaltyPoints || 0}</h3>
                            <p className="text-slate-500 text-sm">Available Points</p>
                        </div>
                        <div className="card-elevated p-6 flex flex-col items-center justify-center text-center">
                            <div className="w-12 h-12 bg-blue-100 rounded-full flex items-center justify-center mb-4">
                                <TrendingUp className="h-6 w-6 text-blue-600" />
                            </div>
                            <h3 className="text-3xl font-bold text-slate-900 mb-1">{client.loyaltyTier || 'Bronze'}</h3>
                            <p className="text-slate-500 text-sm">Current Tier</p>
                        </div>
                        <div className="card-elevated p-6 flex flex-col items-center justify-center text-center">
                            <div className="w-12 h-12 bg-emerald-100 rounded-full flex items-center justify-center mb-4">
                                <DollarSign className="h-6 w-6 text-emerald-600" />
                            </div>
                            <h3 className="text-3xl font-bold text-slate-900 mb-1">{formatCurrency(client.totalSpent || 0)}</h3>
                            <p className="text-slate-500 text-sm">Lifetime Value</p>
                        </div>
                    </div>

                    <div className="card-elevated p-6">
                        <h2 className="text-lg font-semibold text-slate-900 mb-6">Points History</h2>
                        {loadingLoyalty ? (
                            <div className="text-center py-10 text-slate-500">Loading history...</div>
                        ) : loyaltyHistory.length === 0 ? (
                            <div className="text-center py-10 text-slate-500">No points history found.</div>
                        ) : (
                            <div className="overflow-x-auto">
                                <table className="w-full">
                                    <thead>
                                        <tr className="border-b border-slate-200">
                                            <th className="text-left py-3 px-4 text-xs font-semibold text-slate-500 uppercase">Date</th>
                                            <th className="text-left py-3 px-4 text-xs font-semibold text-slate-500 uppercase">Description</th>
                                            <th className="text-left py-3 px-4 text-xs font-semibold text-slate-500 uppercase">Type</th>
                                            <th className="text-right py-3 px-4 text-xs font-semibold text-slate-500 uppercase">Points</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {loyaltyHistory.map((item, i) => (
                                            <tr key={i} className="border-b border-slate-100 last:border-0 hover:bg-slate-50">
                                                <td className="py-3 px-4 text-sm text-slate-600">{formatDate(item.date)}</td>
                                                <td className="py-3 px-4 text-sm text-slate-900">{item.description}</td>
                                                <td className="py-3 px-4 text-sm">
                                                    <span className={cn(
                                                        "px-2 py-0.5 rounded-full text-xs font-medium",
                                                        item.type === 'Earned' ? 'bg-emerald-100 text-emerald-700' :
                                                            item.type === 'Redeemed' ? 'bg-amber-100 text-amber-700' :
                                                                'bg-slate-100 text-slate-600'
                                                    )}>
                                                        {item.type}
                                                    </span>
                                                </td>
                                                <td className={cn(
                                                    "py-3 px-4 text-sm font-medium text-right",
                                                    item.points > 0 ? "text-emerald-600" : "text-amber-600"
                                                )}>
                                                    {item.points > 0 ? '+' : ''}{item.points}
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        )}
                    </div>
                </div>
            )}

            {activeTab === 'activity' && (
                <div className="card-elevated p-6 animate-fade-in">
                    <h2 className="text-lg font-semibold text-slate-900 mb-6">Activity Feed</h2>
                    {loadingActivities ? (
                        <div className="text-center py-10 text-slate-500">Loading activity...</div>
                    ) : activities.length === 0 ? (
                        <div className="text-center py-10 text-slate-500">No recent activity for this client.</div>
                    ) : (
                        <div className="space-y-4">
                            {activities.map((a, i) => (
                                <div key={i} className="flex items-start gap-3 pb-4 border-b border-slate-100 last:border-0 last:pb-0">
                                    <div className="w-2 h-2 rounded-full bg-primary-500 mt-2 flex-shrink-0" />
                                    <div className="flex-1 min-w-0">
                                        <div className="flex items-center gap-2">
                                            <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-slate-100 text-slate-600">{a.type}</span>
                                            <span className="text-xs text-slate-400">{formatDate(a.date)}</span>
                                        </div>
                                        <p className="text-sm text-slate-900 mt-1">{a.description}</p>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            )}

            {/* Actions (Only in Overview) */}
            {activeTab === 'overview' && (
                <div className="flex items-center justify-between pt-8 animate-fade-in">
                    <Link href="/clients" className="btn btn-secondary">Cancel</Link>
                </div>
            )}

            {/* Delete Modal */}
            {showDeleteModal && (
                <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 animate-fade-in">
                    <div className="bg-white rounded-2xl shadow-2xl max-w-md w-full p-6 animate-fade-in-up">
                        <div className="flex items-center gap-4 mb-4">
                            <div className="w-12 h-12 rounded-full bg-red-100 flex items-center justify-center">
                                <AlertTriangle className="h-6 w-6 text-red-600" />
                            </div>
                            <div>
                                <h3 className="text-lg font-semibold text-slate-900">Delete Client</h3>
                                <p className="text-slate-500">This action cannot be undone.</p>
                            </div>
                        </div>
                        <p className="text-slate-600 mb-6">
                            Are you sure you want to delete <strong>{client.firstName} {client.lastName}</strong>? All booking history will be removed.
                        </p>
                        <div className="flex gap-3">
                            <button onClick={() => setShowDeleteModal(false)} className="btn btn-secondary flex-1">Cancel</button>
                            <button onClick={handleDelete} disabled={saving} className="btn bg-red-500 text-white hover:bg-red-600 flex-1">
                                {saving ? 'Deleting...' : 'Delete'}
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
