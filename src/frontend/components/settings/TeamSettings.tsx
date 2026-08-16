'use client';

import { useState, useEffect } from 'react';
import { Users, Mail, Shield, UserPlus, X, MoreVertical, Trash2, Clock } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Modal } from '@/components/ui/Modal';
import { api } from '@/lib/api';
import { cn } from '@/lib/utils';

interface TeamMember {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    role: string;
    isActive: boolean;
}

interface Invitation {
    id: string;
    email: string;
    role: string;
    expiresAt: string;
    createdAt: string;
}

export function TeamSettings() {
    const [team, setTeam] = useState<TeamMember[]>([]);
    const [invitations, setInvitations] = useState<Invitation[]>([]);
    const [loading, setLoading] = useState(true);
    const [showInviteModal, setShowInviteModal] = useState(false);

    // Form state
    const [inviteEmail, setInviteEmail] = useState('');
    const [inviteRole, setInviteRole] = useState('Staff');
    const [inviting, setInviting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const fetchData = async () => {
        try {
            const [staffRes, inviteRes] = await Promise.all([
                api.staff.list(),
                api.invitations.list()
            ]);
            setTeam(staffRes.data);
            setInvitations(inviteRes.data);
        } catch (err) {
            console.error('Error fetching team data:', err);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchData();
    }, []);

    const handleInvite = async (e: React.FormEvent) => {
        e.preventDefault();
        setInviting(true);
        setError(null);

        try {
            await api.invitations.create({
                email: inviteEmail,
                role: inviteRole
            });
            setShowInviteModal(false);
            setInviteEmail('');
            fetchData();
        } catch (err: any) {
            setError(err.response?.data?.message || 'Failed to send invitation');
        } finally {
            setInviting(false);
        }
    };

    const handleCancelInvite = async (id: string) => {
        if (!confirm('Are you sure you want to cancel this invitation?')) return;
        try {
            await api.invitations.delete(id);
            fetchData();
        } catch (err) {
            console.error('Error cancelling invitation:', err);
        }
    };

    if (loading) return <div className="p-8 text-center text-gray-500">Loading team data...</div>;

    return (
        <div className="space-y-10">
            {/* Active Team */}
            <div className="space-y-6">
                <div className="flex items-center justify-between">
                    <div>
                        <h2 className="text-xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Security Cluster</h2>
                        <p className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mt-1">Authorized entity directory</p>
                    </div>
                    <Button onClick={() => setShowInviteModal(true)} className="rounded-2xl font-black uppercase tracking-widest text-[10px] shadow-xl shadow-primary-500/20 hover:scale-105 active:scale-95 transition-all">
                        <UserPlus className="h-4 w-4 mr-2" />
                        Provision User
                    </Button>
                </div>

                <div className="grid gap-4">
                    {team.map((member) => (
                        <div key={member.id} className="flex items-center justify-between p-6 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[28px] hover:border-primary-400 dark:hover:border-primary-600 transition-all group shadow-xl shadow-slate-200/50 dark:shadow-none">
                            <div className="flex items-center gap-5">
                                <div className="w-14 h-14 bg-primary-100 dark:bg-primary-900/50 text-primary-600 dark:text-primary-400 rounded-2xl flex items-center justify-center font-black text-lg shadow-inner group-hover:scale-110 transition-transform">
                                    {member.firstName.charAt(0)}{member.lastName.charAt(0)}
                                </div>
                                <div className="space-y-1">
                                    <div className="flex items-center gap-3">
                                        <p className="font-black text-slate-900 dark:text-white uppercase tracking-tighter text-lg">
                                            {member.firstName} {member.lastName}
                                        </p>
                                        {!member.isActive && <span className="text-[10px] font-black text-rose-500 bg-rose-50 dark:bg-rose-400/10 px-3 py-1 rounded-lg border border-rose-100 dark:border-rose-400/20 uppercase tracking-widest">Off-line</span>}
                                    </div>
                                    <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest flex items-center gap-2">
                                        <Mail className="h-3 w-3" />
                                        {member.email}
                                    </p>
                                </div>
                            </div>
                            <div className="flex items-center gap-8">
                                <div className="text-right">
                                    <span className={cn(
                                        "text-[9px] font-black px-4 py-1.5 rounded-lg border uppercase tracking-[0.2em] shadow-sm",
                                        member.role === 'Owner' ? "bg-primary-50 dark:bg-primary-400/10 text-primary-600 dark:text-primary-400 border-primary-100 dark:border-primary-400/20" :
                                            member.role === 'Admin' ? "bg-blue-50 dark:bg-blue-400/10 text-blue-600 dark:text-blue-400 border-blue-100 dark:border-blue-400/20" :
                                                "bg-slate-50 dark:bg-slate-800 text-slate-500 dark:text-slate-400 border-slate-100 dark:border-slate-700"
                                    )}>
                                        {member.role}
                                    </span>
                                </div>
                                <button className="text-slate-300 dark:text-slate-600 hover:text-slate-900 dark:hover:text-white transition-colors p-2 bg-slate-50 dark:bg-slate-800 rounded-xl">
                                    <MoreVertical className="h-5 w-5" />
                                </button>
                            </div>
                        </div>
                    ))}
                </div>
            </div>

            {/* Pending Invitations */}
            {invitations.length > 0 && (
                <div className="space-y-6 pt-10 border-t border-slate-100 dark:border-slate-800/50">
                    <div>
                        <h2 className="text-xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Pending Uplinks</h2>
                        <p className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.3em] mt-1">Awaiting acceptance confirmation</p>
                    </div>

                    <div className="grid gap-4">
                        {invitations.map((invite) => (
                            <div key={invite.id} className="flex items-center justify-between p-6 bg-slate-50/50 dark:bg-slate-950/20 border border-slate-100 dark:border-slate-800 rounded-[28px] relative overflow-hidden group">
                                <div className="absolute top-0 right-0 w-32 h-32 bg-amber-500/5 blur-2xl -mr-16 -mt-16" />
                                <div className="flex items-center gap-5 relative">
                                    <div className="w-14 h-14 bg-white dark:bg-slate-800 border border-slate-100 dark:border-slate-700 text-slate-300 dark:text-slate-600 rounded-2xl flex items-center justify-center shadow-sm">
                                        <Mail className="h-6 w-6" />
                                    </div>
                                    <div className="space-y-1">
                                        <p className="font-black text-slate-900 dark:text-white uppercase tracking-widest text-sm">{invite.email}</p>
                                        <p className="text-[10px] font-bold text-slate-400 dark:text-slate-500 flex items-center gap-2 uppercase tracking-widest">
                                            <Clock className="h-3 w-3" />
                                            Dispatched: {new Date(invite.createdAt).toLocaleDateString()}
                                        </p>
                                    </div>
                                </div>
                                <div className="flex items-center gap-6 relative">
                                    <span className="text-[9px] font-black text-amber-600 dark:text-amber-400 uppercase tracking-[0.3em] bg-amber-50 dark:bg-amber-400/10 px-3 py-1 rounded-lg border border-amber-100 dark:border-amber-400/20">{invite.role}</span>
                                    <button
                                        onClick={() => handleCancelInvite(invite.id)}
                                        className="text-slate-300 dark:text-slate-600 hover:text-rose-600 transform hover:scale-110 transition-all p-2 bg-white dark:bg-slate-800 rounded-xl shadow-sm border border-slate-100 dark:border-slate-700"
                                        title="Cancel Invitation"
                                    >
                                        <Trash2 className="h-4 w-4" />
                                    </button>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            )}

            {/* Invite Modal */}
            <Modal
                isOpen={showInviteModal}
                onClose={() => setShowInviteModal(false)}
                title="Entity Provisioning"
            >
                <form onSubmit={handleInvite} className="space-y-8 p-2">
                    <p className="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest leading-relaxed">
                        Specify the network identifier for the candidate and assign an authorization level within the matrix.
                    </p>

                    {error && (
                        <div className="p-4 bg-rose-50 dark:bg-rose-400/10 text-rose-600 dark:text-rose-400 text-xs font-black uppercase tracking-widest rounded-2xl border border-rose-100 dark:border-rose-400/20 shadow-inner">
                            Critical Error: {error}
                        </div>
                    )}

                    <Input
                        label="NETWORK IDENTIFIER"
                        type="email"
                        placeholder="identity@matrix.com"
                        value={inviteEmail}
                        onChange={(e) => setInviteEmail(e.target.value)}
                        required
                        className="dark:bg-slate-800 dark:border-slate-700 dark:text-white"
                    />

                    <div className="space-y-4">
                        <label className="text-[10px] font-black text-slate-400 dark:text-slate-500 uppercase tracking-[0.4em]">Authorization Level</label>
                        <div className="grid grid-cols-3 gap-3">
                            {['Admin', 'Manager', 'Staff'].map((r) => (
                                <button
                                    key={r}
                                    type="button"
                                    onClick={() => setInviteRole(r)}
                                    className={cn(
                                        "px-4 py-4 text-[10px] font-black uppercase tracking-widest rounded-2xl border transition-all duration-300",
                                        inviteRole === r
                                            ? "border-primary-500 bg-primary-50 dark:bg-primary-500/10 text-primary-600 dark:text-primary-400 shadow-xl shadow-primary-500/10"
                                            : "border-slate-100 dark:border-slate-800 text-slate-400 dark:text-slate-600 hover:bg-slate-50 dark:hover:bg-slate-800"
                                    )}
                                >
                                    {r}
                                </button>
                            ))}
                        </div>
                        <div className="p-4 bg-slate-50 dark:bg-slate-800/50 rounded-2xl border border-slate-100 dark:border-slate-700">
                            <p className="text-[9px] font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest flex items-center gap-2">
                                <Shield className="h-3 w-3 text-primary-500" />
                                {inviteRole === 'Admin' && 'Full system control and data manipulation.'}
                                {inviteRole === 'Manager' && 'Operation control for logistics and clients.'}
                                {inviteRole === 'Staff' && 'Standard access for assigned tasks.'}
                            </p>
                        </div>
                    </div>

                    <div className="flex gap-4 justify-end pt-6">
                        <Button
                            type="button"
                            variant="outline"
                            onClick={() => setShowInviteModal(false)}
                            className="px-8 rounded-2xl dark:border-slate-700 dark:text-slate-400 font-bold uppercase tracking-widest text-[10px]"
                        >
                            Abort
                        </Button>
                        <Button
                            type="submit"
                            loading={inviting}
                            className="px-8 rounded-2xl font-black uppercase tracking-widest text-[10px] shadow-xl shadow-primary-500/20"
                        >
                            Commit Provisioning
                        </Button>
                    </div>
                </form>
            </Modal>
        </div>
    );
}
