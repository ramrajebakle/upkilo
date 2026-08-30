"use client";

import React, { useState, useEffect } from "react";
import { 
    Plus, UserCog, UserMinus, Shield, Mail, Trash2, Loader2, Send, 
    Monitor, Users, ShieldCheck, Zap, History, UserPlus
} from "lucide-react";
import api from "@/lib/api";
import { Button } from "@/components/ui/Button";
import { Modal } from "@/components/ui/Modal";
import { Input } from "@/components/ui/Input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/Select";
import { useToast } from "@/components/ui/Toast";
import { cn } from "@/lib/utils";

export default function TeamPage() {
  const { success: toastSuccess, error: toastError } = useToast();
  const [team, setTeam] = useState<any[]>([]);
  const [invitations, setInvitations] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);
  const [isInviting, setIsInviting] = useState(false);
  
  const [inviteForm, setInviteForm] = useState({
    email: '',
    role: 'Staff'
  });

  useEffect(() => {
    fetchData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [teamRes, inviteRes] = await Promise.all([
        api.users.list(),
        api.invitations.list()
      ]);
      
      setTeam(teamRes.data?.data || teamRes.data || []);
      setInvitations(inviteRes.data?.data || inviteRes.data || []);
    } catch (err) {
      console.error('Failed to fetch team data:', err);
      toastError('Failed to load team data');
    } finally {
      setLoading(false);
    }
  };

  const handleInvite = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!inviteForm.email) return;

    try {
      setIsInviting(true);
      await api.invitations.create({
        email: inviteForm.email,
        role: inviteForm.role
      });
      
      toastSuccess(`Invitation sent to ${inviteForm.email}`);
      setIsInviteModalOpen(false);
      setInviteForm({ email: '', role: 'Staff' });
      fetchData();
    } catch (err: any) {
      console.error('Failed to send invitation:', err);
      toastError(err.response?.data?.message || 'Failed to send invitation');
    } finally {
      setIsInviting(false);
    }
  };

  const handleCancelInvite = async (id: string, email: string) => {
    if (!confirm(`Are you sure you want to cancel the invitation for ${email}?`)) return;

    try {
      await api.invitations.delete(id);
      toastSuccess('Invitation cancelled');
      fetchData();
    } catch (err) {
      console.error('Failed to cancel invitation:', err);
      toastError('Failed to cancel invitation');
    }
  };

  if (loading && team.length === 0) {
    return (
        <div className="flex flex-col items-center justify-center min-h-[500px] gap-6">
            <Loader2 className="h-12 w-12 text-primary-500 animate-spin" />
            <p className="text-[10px] font-black uppercase tracking-[0.4em] text-foreground-secondary">Syncing Agent Spectrum...</p>
        </div>
    );
  }

  return (
    <div className="max-w-6xl mx-auto space-y-12 animate-fade-in pb-20">
      {/* Header Bundle */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-8 mb-12">
        <div className="flex items-center gap-6">
            <div className="p-4 bg-gradient-to-br from-primary-500 to-primary-600 rounded-[28px] shadow-2xl shadow-primary-500/20 border border-primary-500/20">
                <Users className="h-8 w-8 text-white" />
            </div>
            <div>
                <h1 className="text-3xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Agent Matrix</h1>
                <p className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em] mt-1">Human Resource & Permission Layer</p>
            </div>
        </div>
        <Button 
          onClick={() => setIsInviteModalOpen(true)}
          className="h-14 px-10 rounded-2xl font-black uppercase tracking-widest text-[10px] shadow-2xl shadow-primary-500/30 active:scale-95 transition-all flex items-center gap-3"
        >
          <UserPlus className="h-4 w-4" /> Initialize Deployment
        </Button>
      </div>

      {/* Stats Spectrum */}
      <div className="grid md:grid-cols-3 gap-8">
          {[
              { label: 'Total Agents', value: team.length, icon: Users, color: 'text-primary-500', bg: 'bg-primary-500/10' },
              { label: 'Active Nodes', value: team.filter(m => m.isActive).length, icon: Zap, color: 'text-success-fg', bg: 'bg-emerald-500/10' },
              { label: 'Pending Comms', value: invitations.length, icon: Mail, color: 'text-warning-fg', bg: 'bg-amber-500/10' }
          ].map((stat, i) => (
              <div key={i} className="p-8 bg-white dark:bg-slate-900 border border-slate-100 dark:border-slate-800 rounded-[32px] shadow-2xl shadow-slate-200/40 dark:shadow-none space-y-4 group overflow-hidden relative">
                  <div className="relative z-10 flex items-center justify-between">
                      <div className={cn("p-4 rounded-2xl border", stat.bg, "border-transparent dark:border-white/5")}>
                          <stat.icon className={cn("h-6 w-6", stat.color)} />
                      </div>
                      <span className="text-4xl font-black text-slate-900 dark:text-white tabular-nums tracking-tighter">{stat.value}</span>
                  </div>
                  <p className="relative z-10 text-[10px] font-black text-foreground-muted uppercase tracking-widest">{stat.label}</p>
                  <div className="absolute -bottom-6 -right-6 w-24 h-24 bg-slate-50 dark:bg-slate-850/30 rounded-full blur-2xl group-hover:scale-150 transition-transform" />
              </div>
          ))}
      </div>

      {/* Main Team Table Card */}
      <div className="bg-white dark:bg-slate-900 rounded-[40px] border border-slate-100 dark:border-slate-800 shadow-2xl shadow-slate-200/40 dark:shadow-none overflow-hidden">
        <div className="p-10 border-b border-slate-50 dark:border-slate-850 flex items-center justify-between bg-slate-50/20 dark:bg-slate-950/20">
            <div className="flex items-center gap-6">
                <div className="p-4 bg-white dark:bg-slate-900 rounded-2xl border border-slate-100 dark:border-slate-800 shadow-sm">
                    <Monitor className="h-6 w-6 text-foreground-muted" />
                </div>
                <div>
                    <h4 className="text-xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Active Operatives</h4>
                    <p className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.3em] mt-2">Authenticated System Access Map</p>
                </div>
            </div>
        </div>
        
        <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
                <thead>
                    <tr className="bg-slate-50/30 dark:bg-slate-950/30">
                        <th className="px-10 py-6 text-[10px] font-black text-foreground-muted uppercase tracking-[0.4em]">Agent Identity</th>
                        <th className="px-10 py-6 text-[10px] font-black text-foreground-muted uppercase tracking-[0.4em]">Auth Role</th>
                        <th className="px-10 py-6 text-[10px] font-black text-foreground-muted uppercase tracking-[0.4em]">Neural Stance</th>
                        <th className="px-10 py-6 text-[10px] font-black text-foreground-muted uppercase tracking-[0.4em]">Last Uplink</th>
                        <th className="px-10 py-6 text-right"></th>
                    </tr>
                </thead>
                <tbody className="divide-y divide-slate-50 dark:divide-slate-850">
                    {team.length === 0 ? (
                        <tr>
                            <td colSpan={5} className="px-10 py-24 text-center">
                                <div className="p-8 bg-slate-50 dark:bg-slate-950 rounded-full inline-block mb-6 shadow-inner">
                                    <Users className="h-10 w-10 text-slate-200" />
                                </div>
                                <p className="text-[10px] font-black text-foreground-muted uppercase tracking-[0.4em]">Zero agent nodes detected in matrix</p>
                            </td>
                        </tr>
                    ) : (
                        team.map((member) => (
                        <tr key={member.id} className="group hover:bg-slate-50/50 dark:hover:bg-primary-900/[0.03] transition-all">
                            <td className="px-10 py-8">
                                <div className="flex items-center gap-6">
                                    <div className="w-14 h-14 rounded-2xl bg-gradient-to-br from-slate-100 to-slate-200 dark:from-slate-800 dark:to-slate-900 flex items-center justify-center font-black text-slate-500 dark:text-slate-400 text-lg shadow-inner group-hover:scale-110 transition-transform">
                                        {member.firstName?.charAt(0) || member.email?.charAt(0).toUpperCase()}
                                    </div>
                                    <div>
                                        <p className="text-sm font-black text-slate-900 dark:text-white uppercase tracking-tight">{member.firstName} {member.lastName}</p>
                                        <p className="text-[10px] font-bold text-foreground-muted uppercase tracking-widest mt-1">{member.email}</p>
                                    </div>
                                </div>
                            </td>
                            <td className="px-10 py-8">
                                <div className="flex items-center gap-3">
                                    <div className="p-2 bg-slate-50 dark:bg-slate-850 rounded-lg border border-slate-200 dark:border-slate-800">
                                        <Shield className={cn("h-3 w-3", member.role === 'Owner' ? 'text-primary-500' : 'text-primary-500')} />
                                    </div>
                                    <span className="text-[10px] font-black text-slate-900 dark:text-white uppercase tracking-widest">{member.role}</span>
                                </div>
                            </td>
                            <td className="px-10 py-8">
                                <span className={cn(
                                    "px-4 py-1.5 rounded-xl text-[9px] font-black uppercase tracking-widest border shadow-sm",
                                    member.isActive 
                                        ? "bg-emerald-50 dark:bg-emerald-400/10 text-emerald-600 dark:text-emerald-400 border-emerald-100 dark:border-emerald-400/20 shadow-emerald-500/[0.05]" 
                                        : "bg-slate-100 dark:bg-slate-800 text-foreground-secondary border-slate-200 dark:border-slate-700 shadow-none"
                                )}>
                                    {member.isActive ? 'Active Node' : 'Suspended'}
                                </span>
                            </td>
                            <td className="px-10 py-8">
                                <p className="text-[10px] font-bold text-foreground-secondary uppercase tracking-[0.2em]">{member.lastLoginAt ? new Date(member.lastLoginAt).toLocaleDateString() : 'Historical'}</p>
                            </td>
                            <td className="px-10 py-8 text-right">
                                <div className="flex justify-end gap-3 opacity-0 group-hover:opacity-100 transition-opacity">
                                    <Button variant="ghost" size="icon" className="h-10 w-10 rounded-xl bg-white dark:bg-slate-850 hover:bg-primary-50 dark:hover:bg-primary-950 hover:text-primary-500 transition-all shadow-sm">
                                        <UserCog className="w-4 h-4" />
                                    </Button>
                                    {member.role !== 'Owner' && (
                                        <Button variant="ghost" size="icon" className="h-10 w-10 rounded-xl bg-white dark:bg-slate-850 hover:bg-red-50 dark:hover:bg-red-950 hover:text-red-500 transition-all shadow-sm">
                                            <UserMinus className="w-4 h-4" />
                                        </Button>
                                    )}
                                </div>
                            </td>
                        </tr>
                        ))
                    )}
                </tbody>
            </table>
        </div>
      </div>

      {/* Pending Invitations Corridor */}
      {invitations.length > 0 && (
        <div className="bg-white dark:bg-slate-900 rounded-[40px] border border-amber-100 dark:border-amber-900/30 shadow-2xl shadow-amber-500/[0.03] overflow-hidden">
          <div className="p-10 border-b border-amber-50 dark:border-amber-900/20 flex items-center justify-between bg-amber-50/[0.1] dark:bg-amber-900/[0.05]">
              <div className="flex items-center gap-6">
                  <div className="p-4 bg-amber-50 dark:bg-amber-900/40 rounded-2xl border border-amber-100 dark:border-amber-800/50 shadow-sm">
                      <Mail className="h-6 w-6 text-amber-600 dark:text-amber-500" />
                  </div>
                  <div>
                      <h4 className="text-xl font-black text-slate-900 dark:text-white uppercase tracking-tight">Pending Authorisations</h4>
                      <p className="text-[10px] font-black text-amber-600/60 dark:text-amber-500/60 uppercase tracking-[0.3em] mt-2">Synchronisation in Progress</p>
                  </div>
              </div>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-left">
              <thead>
                <tr className="bg-amber-50/20 dark:bg-amber-950/20">
                  <th className="px-10 py-6 text-[10px] font-black text-amber-600/60 uppercase tracking-[0.4em]">Invitee Node</th>
                  <th className="px-10 py-6 text-[10px] font-black text-amber-600/60 uppercase tracking-[0.4em]">Target Protocol</th>
                  <th className="px-10 py-6 text-[10px] font-black text-amber-600/60 uppercase tracking-[0.4em]">Broadcast Time</th>
                  <th className="px-10 py-6 text-[10px] font-black text-amber-600/60 uppercase tracking-[0.4em]">Void In</th>
                  <th className="px-10 py-6 text-right"></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-amber-100/50 dark:divide-amber-900/20">
                {invitations.map((invite) => (
                  <tr key={invite.id} className="group hover:bg-amber-50/30 dark:hover:bg-amber-900/5 transition-all">
                    <td className="px-10 py-8">
                       <p className="text-xs font-black text-slate-900 dark:text-white uppercase tracking-widest">{invite.email}</p>
                    </td>
                    <td className="px-10 py-8">
                      <span className="text-[10px] px-3 py-1 bg-white dark:bg-slate-850 rounded-lg font-black text-amber-600 dark:text-amber-500 uppercase tracking-widest shadow-sm border border-amber-100 dark:border-amber-800/50">
                        {invite.role}
                      </span>
                    </td>
                    <td className="px-10 py-8">
                      <p className="text-[10px] font-bold text-foreground-secondary uppercase tracking-widest">{new Date(invite.createdAt).toLocaleDateString()}</p>
                    </td>
                    <td className="px-10 py-8">
                      <p className="text-[10px] font-black text-warning-fg uppercase tracking-widest">{Math.ceil((new Date(invite.expiresAt).getTime() - Date.now()) / (1000 * 60 * 60 * 24))} CYCLES</p>
                    </td>
                    <td className="px-10 py-8 text-right">
                      <Button 
                        variant="ghost" 
                        size="icon" 
                        onClick={() => handleCancelInvite(invite.id, invite.email)}
                        className="h-10 w-10 text-foreground-muted hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-950/30 rounded-xl transition-all"
                      >
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Invite Modal Corridor */}
      <Modal
        isOpen={isInviteModalOpen}
        onClose={() => setIsInviteModalOpen(false)}
        title="Agent Deployment"
        description="Initialize a new authenticated node in the system matrix."
      >
        <form onSubmit={handleInvite} className="space-y-8 py-6">
          <div className="space-y-4">
            <label className="text-[10px] font-black text-foreground-muted uppercase tracking-widest ml-1">Agent Neural Address</label>
            <div className="relative">
                <Mail className="absolute left-5 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-300" />
                <Input 
                  type="email" 
                  placeholder="AGENT-ID@SYSTEM.ROOT"
                  className="h-14 pl-14 rounded-2xl bg-slate-50 dark:bg-slate-950 border-none shadow-inner text-xs font-black uppercase tracking-widest dark:text-white"
                  value={inviteForm.email}
                  onChange={(e) => setInviteForm({ ...inviteForm, email: e.target.value })}
                  required
                  disabled={isInviting}
                />
            </div>
          </div>
          
          <div className="space-y-4">
            <label className="text-[10px] font-black text-foreground-muted uppercase tracking-widest ml-1">System Privilege Matrix</label>
            <Select 
              value={inviteForm.role} 
              onValueChange={(val) => setInviteForm({ ...inviteForm, role: val })}
              disabled={isInviting}
            >
              <SelectTrigger className="h-14 rounded-2xl bg-slate-50 dark:bg-slate-950 border-none shadow-inner text-xs font-black uppercase tracking-widest dark:text-white">
                <SelectValue placeholder="Assign privilege level" />
              </SelectTrigger>
              <SelectContent className="rounded-2xl border-slate-100 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-2xl">
                <SelectItem value="Staff" className="py-4 text-xs font-black uppercase tracking-widest focus:bg-primary-50">Standard Operative</SelectItem>
                <SelectItem value="Manager" className="py-4 text-xs font-black uppercase tracking-widest focus:bg-primary-50">Cluster Supervisor</SelectItem>
                <SelectItem value="Admin" className="py-4 text-xs font-black uppercase tracking-widest focus:bg-primary-50">Root Administrator</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="flex justify-end gap-6 pt-10 border-t border-slate-50 dark:border-slate-850">
            <Button 
                type="button" 
                variant="ghost" 
                onClick={() => setIsInviteModalOpen(false)}
                disabled={isInviting}
                className="h-14 px-8 rounded-2xl font-black uppercase tracking-widest text-[10px] text-foreground-muted hover:text-foreground"
            >
              Abort
            </Button>
            <Button 
              type="submit" 
              className="h-14 px-12 rounded-2xl font-black uppercase tracking-widest text-[10px] shadow-2xl shadow-primary-500/20"
              disabled={isInviting || !inviteForm.email}
            >
              {isInviting ? (
                <Loader2 className="w-5 h-5 animate-spin" />
              ) : (
                <>
                  <Send className="w-4 h-4 mr-3" /> Initialise Deployment
                </>
              )}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}

