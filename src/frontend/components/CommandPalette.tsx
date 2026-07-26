'use client';

import { useState, useEffect, useCallback, useRef } from 'react';
import {
  Search, Command, Loader2, Calendar, Users, Briefcase, ChevronRight,
  Plus, Settings, BarChart3, Zap, FileText, CreditCard, Package, Star,
  Bell, Shield, Home, Clock, ArrowRight, Hash
} from 'lucide-react';
import { apiClient } from '@/lib/api';
import { useRouter } from 'next/navigation';

interface CommandItem {
  id: string;
  label: string;
  description?: string;
  icon: React.ReactNode;
  action: () => void;
  category: string;
  keywords?: string[];
}

interface SearchResult {
  id: string;
  name: string;
  type: string;
  subtitle?: string;
}

const NAV_COMMANDS: Omit<CommandItem, 'action'>[] = [
  { id: 'dashboard', label: 'Dashboard', description: 'Go to main dashboard', icon: <Home className="h-4 w-4" />, category: 'Navigate', keywords: ['home', 'overview'] },
  { id: 'bookings', label: 'Bookings', description: 'View all appointments', icon: <Calendar className="h-4 w-4" />, category: 'Navigate', keywords: ['appointments', 'schedule'] },
  { id: 'new-booking', label: 'New Booking', description: 'Create a new appointment', icon: <Plus className="h-4 w-4" />, category: 'Quick Actions', keywords: ['create', 'appointment'] },
  { id: 'clients', label: 'Clients', description: 'View client directory', icon: <Users className="h-4 w-4" />, category: 'Navigate', keywords: ['customers', 'contacts'] },
  { id: 'new-client', label: 'New Client', description: 'Add a new client', icon: <Plus className="h-4 w-4" />, category: 'Quick Actions', keywords: ['add', 'customer'] },
  { id: 'services', label: 'Services', description: 'Manage your services', icon: <Briefcase className="h-4 w-4" />, category: 'Navigate' },
  { id: 'staff', label: 'Staff', description: 'View team members', icon: <Users className="h-4 w-4" />, category: 'Navigate', keywords: ['team', 'employees'] },
  { id: 'payments', label: 'Payments', description: 'View transactions & invoices', icon: <CreditCard className="h-4 w-4" />, category: 'Navigate', keywords: ['billing', 'invoices'] },
  { id: 'analytics', label: 'Analytics', description: 'Business insights & reports', icon: <BarChart3 className="h-4 w-4" />, category: 'Navigate', keywords: ['reports', 'statistics'] },
  { id: 'campaigns', label: 'Campaigns', description: 'Email & SMS campaigns', icon: <Bell className="h-4 w-4" />, category: 'Navigate', keywords: ['marketing', 'email'] },
  { id: 'automation-workflows', label: 'Automations', description: 'Workflow automation', icon: <Zap className="h-4 w-4" />, category: 'Navigate', keywords: ['workflows', 'automation'] },
  { id: 'forms', label: 'Forms', description: 'Custom intake forms', icon: <FileText className="h-4 w-4" />, category: 'Navigate' },
  { id: 'packages', label: 'Packages', description: 'Service packages & bundles', icon: <Package className="h-4 w-4" />, category: 'Navigate', keywords: ['bundles', 'prepaid'] },
  { id: 'reviews', label: 'Reviews', description: 'Client reviews & ratings', icon: <Star className="h-4 w-4" />, category: 'Navigate' },
  { id: 'settings', label: 'Settings', description: 'Account & business settings', icon: <Settings className="h-4 w-4" />, category: 'Navigate' },
  { id: 'settings-billing', label: 'Billing Settings', description: 'Subscription & payment settings', icon: <CreditCard className="h-4 w-4" />, category: 'Settings', keywords: ['subscription', 'plan'] },
  { id: 'settings-team', label: 'Team Settings', description: 'Manage team members & roles', icon: <Users className="h-4 w-4" />, category: 'Settings', keywords: ['roles', 'permissions'] },
  { id: 'security', label: 'Security', description: 'Security & authentication settings', icon: <Shield className="h-4 w-4" />, category: 'Settings', keywords: ['2fa', 'password'] },
];

const routeMap: Record<string, string> = {
  'dashboard': '/dashboard',
  'bookings': '/bookings',
  'new-booking': '/bookings/new',
  'clients': '/clients',
  'new-client': '/clients/new',
  'services': '/services',
  'staff': '/staff',
  'payments': '/payments',
  'analytics': '/analytics',
  'campaigns': '/campaigns',
  'automation-workflows': '/automation/workflows',
  'forms': '/forms',
  'packages': '/packages',
  'reviews': '/reviews',
  'settings': '/settings',
  'settings-billing': '/settings/billing',
  'settings-team': '/settings/team',
  'security': '/security',
};

const categoryIcons: Record<string, React.ReactNode> = {
  Navigate: <ArrowRight className="h-3 w-3" />,
  'Quick Actions': <Plus className="h-3 w-3" />,
  Settings: <Settings className="h-3 w-3" />,
  Results: <Hash className="h-3 w-3" />,
};

export function CommandPalette() {
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [searchResults, setSearchResults] = useState<SearchResult[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [recentItems, setRecentItems] = useState<string[]>([]);
  const inputRef = useRef<HTMLInputElement>(null);
  const router = useRouter();

  // Build commands with actions
  const commands: CommandItem[] = NAV_COMMANDS.map(cmd => ({
    ...cmd,
    action: () => {
      const route = routeMap[cmd.id];
      if (route) {
        router.push(route);
        saveRecent(cmd.id);
      }
      close();
    },
  }));

  const saveRecent = (id: string) => {
    setRecentItems(prev => {
      const updated = [id, ...prev.filter(r => r !== id)].slice(0, 5);
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem('cmd_recent', JSON.stringify(updated));
      }
      return updated;
    });
  };

  const close = () => {
    setIsOpen(false);
    setQuery('');
    setSearchResults([]);
    setSelectedIndex(0);
  };

  useEffect(() => {
    if (typeof localStorage !== 'undefined') {
      try {
        const stored = JSON.parse(localStorage.getItem('cmd_recent') || '[]');
        setRecentItems(stored);
      } catch {}
    }
  }, []);

  // Kbd shortcut
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'k' && (e.metaKey || e.ctrlKey)) {
        e.preventDefault();
        setIsOpen(open => !open);
      }
      if (e.key === 'Escape') close();
    };
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, []);

  // Search API
  const handleSearch = useCallback(async (q: string) => {
    if (q.length < 2) { setSearchResults([]); return; }
    setLoading(true);
    try {
      const res = await apiClient.get(`/api/v1/search?q=${encodeURIComponent(q)}&limit=8`);
      setSearchResults(res.data?.data || res.data || []);
    } catch {
      setSearchResults([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    const t = setTimeout(() => { if (query) handleSearch(query); }, 250);
    return () => clearTimeout(t);
  }, [query, handleSearch]);

  // Filter commands by query
  const filteredCommands = query.length < 2
    ? commands.filter(c => recentItems.includes(c.id)).slice(0, 4)
    : commands.filter(c => {
      const q = query.toLowerCase();
      return c.label.toLowerCase().includes(q) ||
        c.description?.toLowerCase().includes(q) ||
        c.keywords?.some(k => k.includes(q));
    });

  // Group by category
  const grouped: Record<string, CommandItem[]> = {};
  if (query.length < 2 && recentItems.length > 0) {
    grouped['Recent'] = filteredCommands;
  } else {
    filteredCommands.forEach(c => {
      grouped[c.category] = grouped[c.category] || [];
      grouped[c.category].push(c);
    });
  }

  // Add search results
  const resultItems = searchResults.map(r => ({
    id: r.id,
    label: r.name,
    description: r.type,
    icon: r.type === 'Client' ? <Users className="h-4 w-4" /> :
          r.type === 'Booking' ? <Calendar className="h-4 w-4" /> :
          <Briefcase className="h-4 w-4" />,
    category: 'Results',
    action: () => {
      router.push(`/${r.type.toLowerCase()}s/${r.id}`);
      close();
    },
  }));

  if (resultItems.length > 0) grouped['Results'] = resultItems;

  const allItems = Object.values(grouped).flat();

  // Keyboard navigation
  useEffect(() => {
    if (!isOpen) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'ArrowDown') { e.preventDefault(); setSelectedIndex(i => Math.min(i + 1, allItems.length - 1)); }
      if (e.key === 'ArrowUp') { e.preventDefault(); setSelectedIndex(i => Math.max(i - 1, 0)); }
      if (e.key === 'Enter' && allItems[selectedIndex]) { allItems[selectedIndex].action(); }
    };
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, [isOpen, allItems, selectedIndex]);

  useEffect(() => { setSelectedIndex(0); }, [query]);

  useEffect(() => {
    if (isOpen) setTimeout(() => inputRef.current?.focus(), 50);
  }, [isOpen]);

  const defaultCommands = commands.slice(0, 8);

  return (
    <>
      {/* Trigger button */}
      <button
        onClick={() => setIsOpen(true)}
        className="relative flex items-center gap-2 px-3 py-1.5 text-sm text-slate-500 bg-slate-100 hover:bg-slate-200 transition-colors rounded-lg border border-slate-200 w-60 text-left"
      >
        <Search className="h-4 w-4 shrink-0" />
        <span className="flex-1 truncate">Search or jump to...</span>
        <kbd className="hidden sm:inline-flex h-5 items-center gap-0.5 rounded border border-slate-300 bg-white px-1 text-[10px] font-medium text-slate-400">
          <span className="text-xs">⌘</span>K
        </kbd>
      </button>

      {isOpen && (
        <div className="fixed inset-0 z-[200] flex items-start justify-center pt-[12vh] p-4 bg-slate-900/50 backdrop-blur-sm">
          <div className="fixed inset-0" onClick={close} />
          <div className="relative w-full max-w-xl bg-white rounded-2xl shadow-2xl border border-slate-200 overflow-hidden">
            {/* Search Input */}
            <div className="flex items-center gap-3 px-4 py-3 border-b border-slate-100 bg-slate-50">
              <Command className="h-4 w-4 text-slate-400 shrink-0" />
              <input
                ref={inputRef}
                type="text"
                placeholder="Search or type a command..."
                className="flex-1 bg-transparent border-none outline-none text-slate-900 placeholder-slate-400 text-sm"
                value={query}
                onChange={e => setQuery(e.target.value)}
              />
              {loading ? (
                <Loader2 className="h-4 w-4 animate-spin text-indigo-500" />
              ) : query ? (
                <button onClick={() => setQuery('')} className="text-slate-400 hover:text-slate-600 text-xs bg-slate-200 px-1.5 py-0.5 rounded">✕</button>
              ) : (
                <span className="text-[10px] font-bold text-slate-400 bg-slate-200 px-1.5 py-0.5 rounded">ESC</span>
              )}
            </div>

            {/* Results */}
            <div className="max-h-[60vh] overflow-y-auto p-2">
              {Object.keys(grouped).length > 0 ? (
                Object.entries(grouped).map(([category, items]) => (
                  <div key={category} className="mb-2">
                    <div className="flex items-center gap-1.5 px-3 py-1 text-[10px] font-bold text-slate-400 uppercase tracking-wider">
                      {categoryIcons[category]} {category}
                    </div>
                    {items.map((item, itemIdx) => {
                      const globalIdx = allItems.indexOf(item);
                      return (
                        <button
                          key={item.id}
                          onClick={item.action}
                          onMouseEnter={() => setSelectedIndex(globalIdx)}
                          className={`w-full flex items-center gap-3 px-3 py-2.5 text-left rounded-lg transition-colors ${globalIdx === selectedIndex ? 'bg-indigo-50 text-indigo-700' : 'hover:bg-slate-50'}`}
                        >
                          <div className={`p-1.5 rounded-lg shrink-0 ${globalIdx === selectedIndex ? 'bg-indigo-100 text-indigo-600' : 'bg-slate-100 text-slate-500'}`}>
                            {item.icon}
                          </div>
                          <div className="flex-1 min-w-0">
                            <p className="text-sm font-medium truncate">{item.label}</p>
                            {item.description && (
                              <p className="text-xs text-slate-500 truncate">{item.description}</p>
                            )}
                          </div>
                          <ChevronRight className={`h-3 w-3 shrink-0 ${globalIdx === selectedIndex ? 'text-indigo-400' : 'text-slate-300'}`} />
                        </button>
                      );
                    })}
                  </div>
                ))
              ) : query.length >= 2 ? (
                <div className="py-10 text-center text-slate-500">
                  <Command className="h-8 w-8 text-slate-200 mx-auto mb-2" />
                  <p className="text-sm">No results for &quot;{query}&quot;</p>
                </div>
              ) : (
                // Default: show common commands
                <div className="mb-2">
                  <div className="flex items-center gap-1.5 px-3 py-1 text-[10px] font-bold text-slate-400 uppercase tracking-wider">
                    <Clock className="h-3 w-3" /> Quick Navigation
                  </div>
                  {defaultCommands.map((cmd, idx) => (
                    <button
                      key={cmd.id}
                      onClick={cmd.action}
                      onMouseEnter={() => setSelectedIndex(idx)}
                      className={`w-full flex items-center gap-3 px-3 py-2.5 text-left rounded-lg transition-colors ${idx === selectedIndex ? 'bg-indigo-50 text-indigo-700' : 'hover:bg-slate-50'}`}
                    >
                      <div className={`p-1.5 rounded-lg shrink-0 ${idx === selectedIndex ? 'bg-indigo-100 text-indigo-600' : 'bg-slate-100 text-slate-500'}`}>
                        {cmd.icon}
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium">{cmd.label}</p>
                        {cmd.description && <p className="text-xs text-slate-500">{cmd.description}</p>}
                      </div>
                      <ChevronRight className={`h-3 w-3 ${idx === selectedIndex ? 'text-indigo-400' : 'text-slate-300'}`} />
                    </button>
                  ))}
                </div>
              )}
            </div>

            {/* Footer */}
            <div className="px-4 py-2 bg-slate-50 border-t border-slate-100 flex items-center gap-4 text-[10px] font-bold text-slate-400">
              <span className="flex items-center gap-1"><kbd className="bg-white border rounded px-1">↓</kbd><kbd className="bg-white border rounded px-1">↑</kbd> Navigate</span>
              <span className="flex items-center gap-1"><kbd className="bg-white border rounded px-1 py-0.5">↵</kbd> Select</span>
              <span className="flex items-center gap-1"><kbd className="bg-white border rounded px-1">Esc</kbd> Close</span>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
