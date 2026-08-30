"use client";

import React, { useState, useEffect, useCallback } from 'react';
import {
  Users, Search, Filter, TrendingUp, Award, Clock, ChevronRight,
  BarChart3, CheckCircle, AlertTriangle, RefreshCw, Crown, Star
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { Input } from '@/components/ui/Input';
import { toast } from 'sonner';

interface MemberProgress {
  memberId: string;
  memberName: string;
  memberEmail: string;
  membershipPlan: string;
  joinedAt: string;
  expiresAt?: string;
  completedContent: number;
  totalContent: number;
  progressPercent: number;
  lastActivity?: string;
  streakDays: number;
  status: 'active' | 'expired' | 'paused';
  achievements: string[];
}

const statusColors: Record<string, string> = {
  active: 'bg-emerald-100 text-emerald-700',
  expired: 'bg-red-100 text-red-600',
  paused: 'bg-amber-100 text-amber-700',
};

const planColors: Record<string, string> = {
  basic: 'bg-muted text-foreground',
  premium: 'bg-blue-100 text-blue-700',
  vip: 'bg-brand-subtle text-primary',
};

export default function MembersProgressPage() {
  const [members, setMembers] = useState<MemberProgress[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [planFilter, setPlanFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all');
  const [sortBy, setSortBy] = useState<'progress' | 'activity' | 'streak'>('progress');

  const fetchMembers = useCallback(async () => {
    try {
      setLoading(true);
      const res = await apiClient.get('/api/v1/memberships/members/progress');
      setMembers(res.data?.data || res.data || []);
    } catch {
      toast.error('Failed to load member progress');
      setMembers([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchMembers(); }, [fetchMembers]);

  const filtered = members
    .filter(m => {
      const matchSearch = !search || m.memberName.toLowerCase().includes(search.toLowerCase()) || m.memberEmail.toLowerCase().includes(search.toLowerCase());
      const matchPlan = planFilter === 'all' || m.membershipPlan.toLowerCase() === planFilter;
      const matchStatus = statusFilter === 'all' || m.status === statusFilter;
      return matchSearch && matchPlan && matchStatus;
    })
    .sort((a, b) => {
      if (sortBy === 'progress') return b.progressPercent - a.progressPercent;
      if (sortBy === 'streak') return b.streakDays - a.streakDays;
      if (sortBy === 'activity') return new Date(b.lastActivity || 0).getTime() - new Date(a.lastActivity || 0).getTime();
      return 0;
    });

  const stats = {
    total: members.length,
    active: members.filter(m => m.status === 'active').length,
    avgProgress: members.length > 0 ? Math.round(members.reduce((sum, m) => sum + m.progressPercent, 0) / members.length) : 0,
    topStreaker: members.reduce((max, m) => m.streakDays > (max?.streakDays || 0) ? m : max, members[0]),
  };

  const plans = [...new Set(members.map(m => m.membershipPlan.toLowerCase()))];

  return (
    <div className="p-6 max-w-6xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Member Progress</h1>
          <p className="text-foreground-secondary mt-1">Track member engagement and content completion</p>
        </div>
        <button onClick={fetchMembers} className="p-2 rounded-lg hover:bg-accent text-foreground-secondary">
          <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
        </button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: 'Total Members', value: stats.total, icon: <Users className="h-5 w-5 text-blue-500" /> },
          { label: 'Active', value: stats.active, icon: <CheckCircle className="h-5 w-5 text-success-fg" /> },
          { label: 'Avg Progress', value: `${stats.avgProgress}%`, icon: <TrendingUp className="h-5 w-5 text-primary" /> },
          { label: 'Top Streak', value: `${stats.topStreaker?.streakDays || 0}d`, icon: <Award className="h-5 w-5 text-warning-fg" /> },
        ].map(stat => (
          <div key={stat.label} className="bg-card border border-border rounded-xl p-4 flex items-center gap-3">
            <div className="p-2 rounded-lg bg-muted">{stat.icon}</div>
            <div>
              <div className="text-2xl font-bold text-foreground">{stat.value}</div>
              <div className="text-xs text-foreground-secondary">{stat.label}</div>
            </div>
          </div>
        ))}
      </div>

      {/* Filters & Sort */}
      <div className="flex flex-col sm:flex-row gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-foreground-muted" />
          <Input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search members..." className="pl-9" />
        </div>
        <div className="flex gap-2 flex-wrap">
          <select
            value={planFilter}
            onChange={e => setPlanFilter(e.target.value)}
            className="border border-border rounded-lg px-3 py-2 text-sm text-foreground-secondary bg-card"
          >
            <option value="all">All Plans</option>
            {plans.map(p => <option key={p} value={p} className="capitalize">{p}</option>)}
          </select>
          <select
            value={statusFilter}
            onChange={e => setStatusFilter(e.target.value)}
            className="border border-border rounded-lg px-3 py-2 text-sm text-foreground-secondary bg-card"
          >
            <option value="all">All Status</option>
            <option value="active">Active</option>
            <option value="expired">Expired</option>
            <option value="paused">Paused</option>
          </select>
          <select
            value={sortBy}
            onChange={e => setSortBy(e.target.value as typeof sortBy)}
            className="border border-border rounded-lg px-3 py-2 text-sm text-foreground-secondary bg-card"
          >
            <option value="progress">Sort: Progress</option>
            <option value="streak">Sort: Streak</option>
            <option value="activity">Sort: Last Active</option>
          </select>
        </div>
      </div>

      {/* Members List */}
      {loading ? (
        <div className="space-y-3">
          {[...Array(5)].map((_, i) => (
            <div key={i} className="bg-card border border-border rounded-xl p-4 animate-pulse">
              <div className="flex items-center gap-4">
                <div className="h-10 w-10 rounded-full bg-slate-200" />
                <div className="flex-1">
                  <div className="h-4 bg-slate-200 rounded w-32 mb-2" />
                  <div className="h-2 bg-muted rounded-full" />
                </div>
              </div>
            </div>
          ))}
        </div>
      ) : filtered.length === 0 ? (
        <div className="text-center py-16 bg-card rounded-xl border border-border">
          <Users className="h-12 w-12 text-slate-300 mx-auto mb-3" />
          <h3 className="text-lg font-semibold text-foreground">No members found</h3>
          <p className="text-foreground-secondary text-sm mt-1">Members will appear here once they join a plan</p>
        </div>
      ) : (
        <div className="space-y-3">
          {filtered.map(member => (
            <div key={member.memberId} className="bg-card border border-border rounded-xl p-4 hover:shadow-sm transition-shadow">
              <div className="flex items-center gap-4">
                {/* Avatar */}
                <div className="h-10 w-10 rounded-full bg-gradient-to-br from-primary-400 to-primary-600 flex items-center justify-center text-white font-semibold text-sm shrink-0">
                  {member.memberName.charAt(0).toUpperCase()}
                </div>

                {/* Info */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap mb-1">
                    <span className="font-semibold text-foreground">{member.memberName}</span>
                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${statusColors[member.status]}`}>{member.status}</span>
                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium capitalize ${planColors[member.membershipPlan.toLowerCase()] || 'bg-muted text-foreground-secondary'}`}>
                      {member.membershipPlan}
                    </span>
                    {member.streakDays >= 7 && (
                      <span className="flex items-center gap-0.5 text-xs font-semibold text-warning-fg">
                        <Star className="h-3 w-3 fill-amber-400 text-amber-400" /> {member.streakDays}d streak
                      </span>
                    )}
                  </div>
                  <p className="text-xs text-foreground-muted mb-2">{member.memberEmail}</p>

                  {/* Progress bar */}
                  <div className="flex items-center gap-3">
                    <div className="flex-1 h-2 bg-muted rounded-full overflow-hidden">
                      <div
                        className={`h-full rounded-full transition-all ${member.progressPercent >= 75 ? 'bg-emerald-500' : member.progressPercent >= 40 ? 'bg-blue-500' : 'bg-slate-400'}`}
                        style={{ width: `${member.progressPercent}%` }}
                      />
                    </div>
                    <span className="text-xs font-semibold text-foreground-secondary shrink-0 w-10 text-right">
                      {member.progressPercent}%
                    </span>
                    <span className="text-xs text-foreground-muted shrink-0">
                      {member.completedContent}/{member.totalContent} items
                    </span>
                  </div>
                </div>

                {/* Meta */}
                <div className="text-right shrink-0 hidden sm:block">
                  {member.lastActivity && (
                    <div className="flex items-center gap-1 text-xs text-foreground-muted justify-end mb-1">
                      <Clock className="h-3 w-3" />
                      <span>Active {new Date(member.lastActivity).toLocaleDateString()}</span>
                    </div>
                  )}
                  {member.expiresAt && (
                    <div className={`text-xs ${new Date(member.expiresAt) < new Date() ? 'text-danger-fg' : 'text-foreground-muted'}`}>
                      Expires {new Date(member.expiresAt).toLocaleDateString()}
                    </div>
                  )}
                </div>
              </div>

              {/* Achievements */}
              {member.achievements && member.achievements.length > 0 && (
                <div className="flex items-center gap-1.5 mt-3 flex-wrap">
                  {member.achievements.map((achievement, idx) => (
                    <span key={idx} className="flex items-center gap-0.5 px-2 py-0.5 bg-amber-50 text-amber-700 rounded-full text-xs font-medium border border-amber-100">
                      <Crown className="h-3 w-3" /> {achievement}
                    </span>
                  ))}
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
