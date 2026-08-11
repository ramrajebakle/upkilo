"use client";

import React, { useEffect, useState } from "react";
import { 
  ClipboardList, 
  Search, 
  Filter, 
  Download,
  Calendar,
  Clock,
  User as UserIcon,
  Shield,
  RefreshCw
} from "lucide-react";
import { useAuthStore } from "@/store/authStore";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Badge } from "@/components/ui/Badge";
import { 
  Table, 
  TableHeader, 
  TableBody, 
  TableHead, 
  TableRow, 
  TableCell 
} from "@/components/ui/Table";

export default function AdminLogsPage() {
  const { user, isInitialized } = useAuthStore();
  const router = useRouter();
  
  const [loading, setLoading] = useState(true);
  const [logs, setLogs] = useState<any[]>([]);
  const [searchQuery, setSearchQuery] = useState("");

  useEffect(() => {
    if (isInitialized && user?.role !== 'superadmin') {
      router.push('/dashboard');
    }
  }, [user, isInitialized, router]);

  const fetchData = async () => {
    setLoading(true);
    try {
      const res = await api.superAdmin.auditLogs({ page: 1, pageSize: 100 });
      setLogs(res.data?.data || res.data || []);
    } catch (error) {
      console.error("Failed to fetch logs:", error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (user?.role === 'superadmin') {
      fetchData();
    }
  }, [user]);

  if (user?.role !== 'superadmin') return null;

  const filteredLogs = logs.filter(log => 
    log.action?.toLowerCase().includes(searchQuery.toLowerCase()) ||
    log.details?.toLowerCase().includes(searchQuery.toLowerCase())
  );

  return (
    <div className="space-y-8 max-w-7xl mx-auto pb-12">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-3 mb-2">
            <div className="p-2.5 bg-gradient-to-br from-slate-700 to-slate-900 rounded-2xl shadow-lg shadow-slate-500/20">
              <ClipboardList className="h-6 w-6 text-white" />
            </div>
            <h1 className="text-3xl font-bold text-slate-900 dark:text-white tracking-tight" style={{ fontFamily: 'var(--font-display)' }}>
              Platform Audit Logs
            </h1>
          </div>
          <p className="text-slate-500 dark:text-slate-400">Complete immutable record of all system-wide administrative actions.</p>
        </div>
        <div className="flex items-center gap-3">
          <Button onClick={fetchData} variant="outline" size="sm">
            <RefreshCw className={`h-4 w-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
          <Button variant="outline" size="sm">
            <Download className="h-4 w-4 mr-2" />
            Export CSV
          </Button>
        </div>
      </div>

      {/* Control Bar */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 p-4 bg-white dark:bg-slate-900 border border-slate-200 dark:border-white/5 rounded-2xl shadow-sm">
        <div className="relative w-full md:w-96">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
          <Input 
            placeholder="Filter by action or details..." 
            className="pl-10"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
        </div>
        <div className="flex items-center gap-3">
          <Button variant="outline" size="sm" className="h-10">
            <Calendar className="h-4 w-4 mr-2" />
            Date Range
          </Button>
          <Button variant="outline" size="sm" className="h-10">
            <Filter className="h-4 w-4 mr-2" />
            Severity
          </Button>
        </div>
      </div>

      {/* Logs Table */}
      <div className="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-white/5 overflow-hidden shadow-sm">
        {loading ? (
          <div className="p-12 space-y-4">
            {[...Array(8)].map((_, i) => (
              <div key={i} className="h-12 bg-slate-50 dark:bg-white/5 rounded-xl animate-pulse" />
            ))}
          </div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow className="hover:bg-transparent">
                <TableHead className="w-[200px]">Timestamp</TableHead>
                <TableHead>Event</TableHead>
                <TableHead>Actor</TableHead>
                <TableHead>Details</TableHead>
                <TableHead className="w-[100px] text-right">Severity</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredLogs.map((log) => (
                <TableRow key={log.id} className="group hover:bg-slate-50 dark:hover:bg-white/5 transition-colors">
                  <TableCell className="text-xs text-slate-500">
                    <div className="flex items-center gap-2">
                      <Clock className="h-3 w-3" />
                      {new Date(log.timestamp).toLocaleString()}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="font-bold text-slate-900 dark:text-white flex items-center gap-2">
                       <Shield className="h-4 w-4 text-primary-500" />
                       {log.action}
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-400">
                      <div className="h-6 w-6 rounded-full bg-slate-100 dark:bg-white/10 flex items-center justify-center">
                        <UserIcon className="h-3 w-3" />
                      </div>
                      {log.userId || "System"}
                    </div>
                  </TableCell>
                  <TableCell>
                    <p className="text-sm text-slate-500 max-w-md truncate">{log.details}</p>
                  </TableCell>
                  <TableCell className="text-right">
                    <Badge 
                      variant={(log.action.includes('Failed') || log.action.includes('Delete')) ? 'secondary' : 'outline'}
                      className={log.action.includes('Failed') ? 'border-rose-200 bg-rose-50 text-rose-700' : ''}
                    >
                      {log.action.includes('Failed') ? 'High' : 'Info'}
                    </Badge>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}

        {!loading && filteredLogs.length === 0 && (
          <div className="py-20 text-center">
            <ClipboardList className="h-12 w-12 text-slate-200 mx-auto mb-4" />
            <p className="text-slate-500">No audit logs found for the selected criteria.</p>
          </div>
        )}
      </div>
    </div>
  );
}
