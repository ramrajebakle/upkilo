"use client";

import React, { useState, useEffect } from "react";
import {
  Users,
  Search,
  Plus,
  Calendar,
  TrendingUp,
  UserCheck,
  Phone,
  Mail,
  MoreVertical,
  ChevronRight,
  Star,
  Clock,
  Loader2,
  UserPlus,
  ArrowUpRight,
  Filter,
  Sparkles,
} from "lucide-react";
import { Card, CardContent } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import api from "@/lib/api";
import { toast } from "sonner";
import { cn, formatCurrency } from "@/lib/utils";
import Link from "next/link";

interface Customer {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  status: string;
  loyaltyTier: string;
  totalSpend: number;
  lastVisitAt: string | null;
  tags: string[];
  createdAt: string;
}

export default function TenantCustomersPage() {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<"all" | "active" | "inactive">("all");

  useEffect(() => {
    fetchCustomers();
  }, []);

  const fetchCustomers = async () => {
    try {
      setLoading(true);
      const res = await api.clients.list();
      const rawData = res.data?.data || res.data || [];
      const data = (Array.isArray(rawData) ? rawData : []).map((c: any) => ({
        id: c.id,
        firstName: c.firstName || '',
        lastName: c.lastName || '',
        email: c.email || '',
        phone: c.phone || c.phoneNumber || '',
        status: c.status || (c.isActive !== undefined ? (c.isActive ? 'active' : 'inactive') : 'active'),
        loyaltyTier: c.loyaltyTier || c.loyaltyPoints || '',
        totalSpend: c.totalSpend || c.lifetimeValue || 0,
        lastVisitAt: c.lastVisitAt || c.lastBookingAt || c.lastVisit || null,
        tags: c.tags || [],
        createdAt: c.createdAt || '',
      }));
      setCustomers(data);
    } catch (err) {
      console.error("Failed to fetch customers:", err);
      toast.error("Failed to load customers");
      setCustomers([]);
    } finally {
      setLoading(false);
    }
  };

  const filteredCustomers = customers.filter((c) => {
    const name = `${c.firstName} ${c.lastName}`.toLowerCase();
    const matchesSearch =
      !searchQuery ||
      name.includes(searchQuery.toLowerCase()) ||
      c.email?.toLowerCase().includes(searchQuery.toLowerCase()) ||
      c.phone?.includes(searchQuery);
    const matchesStatus =
      statusFilter === "all" ||
      (statusFilter === "active" && c.status === "active") ||
      (statusFilter === "inactive" && c.status !== "active");
    return matchesSearch && matchesStatus;
  });

  const totalCustomers = customers.length;
  const activeCustomers = customers.filter((c) => c.status === "active").length;
  const newThisMonth = customers.filter((c) => {
    if (!c.createdAt) return false;
    const created = new Date(c.createdAt);
    const now = new Date();
    return (
      created.getMonth() === now.getMonth() &&
      created.getFullYear() === now.getFullYear()
    );
  }).length;
  const totalRevenue = customers.reduce((sum, c) => sum + (c.totalSpend || 0), 0);

  const getInitials = (first: string, last: string) => {
    return `${(first || "?")[0]}${(last || "?")[0]}`.toUpperCase();
  };

  const formatDate = (dateStr: string | null) => {
    if (!dateStr) return "Never";
    return new Date(dateStr).toLocaleDateString("en-US", {
      month: "short",
      day: "numeric",
      year: "numeric",
    });
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case "active":
        return "bg-success-100 text-success-700";
      case "inactive":
        return "bg-neutral-100 text-neutral-600";
      case "vip":
        return "bg-amber-100 text-amber-700";
      default:
        return "bg-neutral-100 text-neutral-600";
    }
  };

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Header */}
      <header className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <p className="text-text-tertiary text-sm font-medium tracking-wide uppercase mb-1">
            Customer Management
          </p>
          <h1 className="text-3xl font-bold text-text-primary">Customers</h1>
        </div>
        <Link href="/en/clients">
          <Button variant="primary" leftIcon={<Plus size={16} />}>
            Add Customer
          </Button>
        </Link>
      </header>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <Card className="bg-surface-0 border-t-[4px] border-t-primary-500">
          <CardContent className="p-5">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm text-text-secondary font-medium">Total Customers</span>
              <Users size={18} className="text-primary-500" />
            </div>
            <span className="text-3xl font-bold text-text-primary">{totalCustomers}</span>
          </CardContent>
        </Card>

        <Card className="bg-surface-0 border-t-[4px] border-t-success-500">
          <CardContent className="p-5">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm text-text-secondary font-medium">Active</span>
              <UserCheck size={18} className="text-success-500" />
            </div>
            <span className="text-3xl font-bold text-text-primary">{activeCustomers}</span>
          </CardContent>
        </Card>

        <Card className="bg-surface-0 border-t-[4px] border-t-tenant-500">
          <CardContent className="p-5">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm text-text-secondary font-medium">New This Month</span>
              <UserPlus size={18} className="text-tenant-500" />
            </div>
            <span className="text-3xl font-bold text-text-primary">{newThisMonth}</span>
          </CardContent>
        </Card>

        <Card className="bg-ai-50 border-ai-200">
          <CardContent className="p-5">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm text-text-secondary font-medium">Lifetime Revenue</span>
              <TrendingUp size={18} className="text-ai-500" />
            </div>
            <span className="text-3xl font-bold text-text-primary">
              {formatCurrency(totalRevenue)}
            </span>
          </CardContent>
        </Card>
      </div>

      {/* Search & Filter Bar */}
      <div className="flex flex-col sm:flex-row gap-3">
        <div className="relative flex-1">
          <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-text-tertiary" />
          <input
            type="text"
            placeholder="Search by name, email, or phone..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-10 pr-4 py-2.5 rounded-lg border border-neutral-200 bg-surface-0 text-text-primary placeholder:text-text-tertiary focus:outline-none focus:ring-2 focus:ring-primary-500/30 focus:border-primary-500 transition-all text-sm"
          />
        </div>
        <div className="flex gap-2">
          {(["all", "active", "inactive"] as const).map((status) => (
            <button
              key={status}
              onClick={() => setStatusFilter(status)}
              className={cn(
                "px-4 py-2 rounded-lg text-sm font-medium capitalize transition-all",
                statusFilter === status
                  ? "bg-primary-500 text-white shadow-sm"
                  : "bg-surface-100 text-text-secondary hover:bg-surface-200"
              )}
            >
              {status}
            </button>
          ))}
        </div>
      </div>

      {/* Customer List */}
      {loading ? (
        <div className="space-y-3">
          {[...Array(5)].map((_, i) => (
            <div
              key={i}
              className="bg-surface-0 rounded-xl border border-neutral-200 p-4 animate-pulse"
            >
              <div className="flex items-center gap-4">
                <div className="w-10 h-10 rounded-full bg-neutral-200" />
                <div className="flex-1 space-y-2">
                  <div className="h-4 bg-neutral-200 rounded w-1/3" />
                  <div className="h-3 bg-neutral-200 rounded w-1/4" />
                </div>
                <div className="h-8 bg-neutral-200 rounded w-20" />
              </div>
            </div>
          ))}
        </div>
      ) : filteredCustomers.length === 0 ? (
        <Card className="bg-surface-0">
          <CardContent className="p-12 text-center">
            <div className="w-16 h-16 rounded-full bg-neutral-100 flex items-center justify-center mx-auto mb-4">
              <Users size={28} className="text-neutral-400" />
            </div>
            <h3 className="text-lg font-semibold text-text-primary mb-1">
              {searchQuery ? "No customers found" : "No customers yet"}
            </h3>
            <p className="text-text-secondary text-sm mb-4">
              {searchQuery
                ? "Try adjusting your search or filters"
                : "Start adding customers to grow your business"}
            </p>
            {!searchQuery && (
              <Link href="/en/clients">
                <Button variant="primary" leftIcon={<Plus size={16} />}>
                  Add First Customer
                </Button>
              </Link>
            )}
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-2">
          {filteredCustomers.map((customer) => (
            <Link
              key={customer.id}
              href={`/en/clients/${customer.id}`}
              className="block"
            >
              <div className="group bg-surface-0 rounded-xl border border-neutral-200 p-4 hover:border-primary-300 hover:shadow-md transition-all cursor-pointer">
                <div className="flex items-center gap-4">
                  {/* Avatar */}
                  <div className="w-10 h-10 rounded-full bg-gradient-to-br from-primary-400 to-primary-600 flex items-center justify-center text-white font-semibold text-sm shrink-0">
                    {getInitials(customer.firstName, customer.lastName)}
                  </div>

                  {/* Name & Contact */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="font-semibold text-text-primary truncate">
                        {customer.firstName} {customer.lastName}
                      </span>
                      {(customer.loyaltyTier === 'gold' || customer.loyaltyTier === 'platinum') && (
                        <Star size={14} className="text-amber-500 shrink-0" />
                      )}
                      <span
                        className={cn(
                          "text-xs font-medium px-2 py-0.5 rounded-full capitalize shrink-0",
                          getStatusColor(customer.status)
                        )}
                      >
                        {customer.status || "unknown"}
                      </span>
                    </div>
                    <div className="flex items-center gap-3 mt-0.5 text-sm text-text-secondary">
                      {customer.email && (
                        <span className="flex items-center gap-1 truncate">
                          <Mail size={12} />
                          <span className="truncate">{customer.email}</span>
                        </span>
                      )}
                      {customer.phone && (
                        <span className="flex items-center gap-1 shrink-0">
                          <Phone size={12} />
                          {customer.phone}
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Metrics */}
                  <div className="hidden md:flex items-center gap-6">
                    <div className="text-right">
                      <p className="text-xs text-text-tertiary">Last Visit</p>
                      <p className="text-sm font-medium text-text-primary">
                        {formatDate(customer.lastVisitAt)}
                      </p>
                    </div>
                    <div className="text-right">
                      <p className="text-xs text-text-tertiary">Total Spend</p>
                      <p className="text-sm font-bold text-text-primary">
                        {formatCurrency(customer.totalSpend || 0)}
                      </p>
                    </div>
                  </div>

                  <ChevronRight
                    size={18}
                    className="text-neutral-300 group-hover:text-primary-500 transition-colors shrink-0"
                  />
                </div>
              </div>
            </Link>
          ))}
        </div>
      )}

      {/* Quick Stats Footer */}
      {!loading && filteredCustomers.length > 0 && (
        <div className="text-center text-sm text-text-tertiary pt-2">
          Showing {filteredCustomers.length} of {totalCustomers} customers
        </div>
      )}
    </div>
  );
}
