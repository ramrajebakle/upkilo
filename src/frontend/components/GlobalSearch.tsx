'use client';

import { useState, useEffect, useCallback } from 'react';
import { Search, Command, Loader2, Calendar, Users, Briefcase, ChevronRight, Clock, Star, Save } from 'lucide-react';
import { api } from '@/lib/api';
import { cn } from '@/lib/utils';
import { useRouter } from 'next/navigation';

export function GlobalSearch() {
    const [isOpen, setIsOpen] = useState(false);
    const [query, setQuery] = useState('');
    const [results, setResults] = useState<any[]>([]);
    const [loading, setLoading] = useState(false);
    const [recentSearches, setRecentSearches] = useState<any[]>([]);
    const [savedSearches, setSavedSearches] = useState<any[]>([]);
    const [savingFilter, setSavingFilter] = useState(false);
    const [selectedIndex, setSelectedIndex] = useState(-1);
    const resultsContainerRef = useState<HTMLDivElement | null>(null)[0]; // Actually use useRef
    const selectedItemRef = useState<HTMLButtonElement | null>(null)[0]; 
    const router = useRouter();
    const [containerRef, setContainerRef] = useState<HTMLDivElement | null>(null);

    useEffect(() => {
        if (isOpen && query.length === 0) {
            loadSearchHistory();
        }
    }, [isOpen, query]);

    const loadSearchHistory = async () => {
        try {
            const [recentRes, savedRes] = await Promise.all([
                api.search.getRecent().catch(() => ({ data: [] })),
                api.search.getSaved().catch(() => ({ data: [] }))
            ]);
            setRecentSearches(Array.isArray(recentRes.data) ? recentRes.data : []);
            setSavedSearches(Array.isArray(savedRes.data) ? savedRes.data : []);
        } catch (err) {
            // Quieter log if search feature isn't fully implemented in backend
            console.debug('Failed to load search history', err);
        }
    };

    const handleSaveSearch = async () => {
        if (!query) return;
        setSavingFilter(true);
        try {
            await api.search.saveFilter({
                name: `Saved: ${query}`,
                query: query,
                searchType: 'Global'
            });
            // Reload saved searches if query is cleared later
        } catch (err) {
            console.error('Failed to save search', err);
        } finally {
            setSavingFilter(false);
        }
    };

    const handleSearch = useCallback(async (q: string) => {
        if (q.length < 2) {
            setResults([]);
            setSelectedIndex(-1);
            return;
        }
        setLoading(true);
        try {
            const res = await api.search.global(q);
            setResults(res.data);
        } catch (err) {
            console.error('Search failed', err);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        const timer = setTimeout(() => {
            if (query) handleSearch(query);
        }, 300);
        return () => clearTimeout(timer);
    }, [query, handleSearch]);

    // Open/close driven by ShortcutManager via custom event — no duplicate Ctrl+K here
    useEffect(() => {
        const open = () => setIsOpen((v) => !v);
        document.addEventListener('open-command-palette', open);
        return () => document.removeEventListener('open-command-palette', open);
    }, []);

    useEffect(() => {
        const down = (e: KeyboardEvent) => {
            // Navigation while open
            if (isOpen) {
                const totalResults = results.length > 0 ? results.length : (recentSearches.length + savedSearches.length);

                if (e.key === 'ArrowDown') {
                    e.preventDefault();
                    setSelectedIndex(prev => (prev < totalResults - 1 ? prev + 1 : prev));
                } else if (e.key === 'ArrowUp') {
                    e.preventDefault();
                    setSelectedIndex(prev => (prev > 0 ? prev - 1 : prev));
                } else if (e.key === 'Enter' && selectedIndex >= 0) {
                    e.preventDefault();
                    if (results.length > 0) {
                        navigateToResult(results[selectedIndex]);
                    } else {
                        // Handle history selection
                        const combined = [...savedSearches, ...recentSearches];
                        const selected = combined[selectedIndex];
                        if (selected) setQuery(selected.query);
                    }
                } else if (e.key === 'Escape') {
                    setIsOpen(false);
                }
            }
        };
        document.addEventListener('keydown', down);
        return () => document.removeEventListener('keydown', down);
    }, [isOpen, results, recentSearches, savedSearches, selectedIndex]);

    // Auto-scroll logic for keyboard navigation
    useEffect(() => {
        if (selectedIndex >= 0 && isOpen) {
            const activeElement = document.getElementById(`search-item-${selectedIndex}`);
            if (activeElement) {
                activeElement.scrollIntoView({
                    block: 'nearest',
                    behavior: 'smooth'
                });
            }
        }
    }, [selectedIndex, isOpen]);

    const navigateToResult = (result: any) => {
        setIsOpen(false);
        setQuery('');
        setResults([]);

        // Simple routing logic based on result type
        switch (result.type?.toLowerCase()) {
            case 'booking': router.push(`/bookings?id=${result.id}`); break;
            case 'client': router.push(`/clients?id=${result.id}`); break;
            case 'service': router.push(`/services?id=${result.id}`); break;
            default: break;
        }
    };

    return (
        <>
            <button
                onClick={() => setIsOpen(true)}
                className="GlobalSearch_trigger group relative flex items-center gap-2 px-3 py-1.5 text-sm font-medium text-slate-500 dark:text-slate-400 bg-slate-100 dark:bg-slate-900 hover:bg-slate-200 dark:hover:bg-slate-800 transition-colors rounded-lg border border-slate-200 dark:border-white/5 w-64 text-left shadow-sm"
            >
                <Search className="h-4 w-4" />
                <span className="flex-1">Search...</span>
                <kbd className="hidden sm:inline-flex h-5 items-center gap-1 rounded border border-slate-300 dark:border-white/10 bg-white dark:bg-slate-800 px-1.5 font-sans text-[10px] font-medium text-slate-400 opacity-100">
                    <span className="text-xs">⌘</span>K
                </kbd>
            </button>

            {isOpen && (
                <div className="fixed inset-0 z-[100] flex items-start justify-center pt-[15vh] p-4 bg-slate-900/40 backdrop-blur-sm animate-in fade-in duration-200">
                    <div
                        className="fixed inset-0"
                        onClick={() => setIsOpen(false)}
                    />
                    <div
                        role="dialog"
                        aria-modal="true"
                        aria-label="Search"
                        className="relative w-full max-w-2xl bg-white dark:bg-slate-900 rounded-2xl shadow-2xl border border-slate-200 dark:border-white/10 overflow-hidden animate-in slide-in-from-top-4 duration-300"
                    >
                        <div className="flex items-center gap-3 px-4 py-3 bg-slate-50 dark:bg-slate-950 border-b border-slate-100 dark:border-white/5">
                            <Search className="h-5 w-5 text-slate-400" aria-hidden="true" />
                            <input
                                autoFocus
                                role="combobox"
                                aria-expanded={results.length > 0 || recentSearches.length > 0}
                                aria-controls="search-listbox"
                                aria-autocomplete="list"
                                aria-activedescendant={selectedIndex >= 0 ? `search-item-${selectedIndex}` : undefined}
                                aria-label="Search bookings, clients, or services"
                                placeholder="Search bookings, clients, or services..."
                                className="flex-1 bg-transparent border-none outline-none text-slate-900 dark:text-white placeholder-slate-400 dark:placeholder-slate-500 text-base"
                                value={query}
                                onChange={(e) => setQuery(e.target.value)}
                            />
                            {loading ? (
                                <Loader2 className="h-4 w-4 animate-spin text-primary-500" />
                            ) : (
                                <button
                                    onClick={() => setIsOpen(false)}
                                    className="text-[10px] font-bold text-slate-400 hover:text-slate-600 dark:hover:text-slate-300 bg-slate-200 dark:bg-slate-800 px-1.5 py-0.5 rounded transition-colors"
                                >
                                    ESC
                                </button>
                            )}
                        </div>

                        <div id="search-listbox" role="listbox" aria-label="Search results" className="max-h-[60vh] overflow-y-auto p-2 scrollbar-thin">
                            {results.length > 0 ? (
                                <div className="space-y-1">
                                    {results.map((res: any, idx: number) => (
                                        <button
                                            key={`${res.type}-${res.id}`}
                                            id={`search-item-${idx}`}
                                            role="option"
                                            aria-selected={selectedIndex === idx}
                                            onClick={() => navigateToResult(res)}
                                            onMouseEnter={() => setSelectedIndex(idx)}
                                            className={cn(
                                                "w-full flex items-center gap-3 p-3 text-left rounded-xl transition-all group",
                                                selectedIndex === idx
                                                    ? "bg-slate-100 dark:bg-white/10 ring-1 ring-slate-200 dark:ring-white/10"
                                                    : "hover:bg-slate-50 dark:hover:bg-white/5"
                                            )}
                                        >
                                            <div className={cn(
                                                "p-2 rounded-lg",
                                                res.type === 'Booking' && "bg-blue-50 text-blue-600",
                                                res.type === 'Client' && "bg-emerald-50 text-emerald-600",
                                                res.type === 'Service' && "bg-amber-50 text-amber-600"
                                            )}>
                                                {res.type === 'Booking' && <Calendar className="h-4 w-4" />}
                                                {res.type === 'Client' && <Users className="h-4 w-4" />}
                                                {res.type === 'Service' && <Briefcase className="h-4 w-4" />}
                                            </div>
                                            <div className="flex-1 min-w-0">
                                                <div className="flex items-center gap-2 mb-0.5">
                                                    <p className="font-semibold text-slate-900 dark:text-white truncate">{res.name}</p>
                                                    <span className={cn(
                                                        'shrink-0 text-[10px] font-bold uppercase tracking-wider px-1.5 py-0.5 rounded',
                                                        res.type === 'Booking' && 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400',
                                                        res.type === 'Client' && 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400',
                                                        res.type === 'Service' && 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
                                                        !['Booking','Client','Service'].includes(res.type) && 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400',
                                                    )}>{res.type ?? 'Result'}</span>
                                                </div>
                                                {res.subtitle && <p className="text-xs text-slate-500 dark:text-slate-400 truncate">{res.subtitle}</p>}
                                            </div>
                                            <ChevronRight className="h-4 w-4 text-slate-300 opacity-0 group-hover:opacity-100 transition-opacity" />
                                        </button>
                                    ))}
                                </div>
                            ) : query.length >= 2 && !loading ? (
                                <div className="py-12 text-center text-slate-500">
                                    <p>No results found for "{query}"</p>
                                </div>
                            ) : (
                                <div className="space-y-6 py-4">
                                    {savedSearches.length > 0 && (
                                        <div>
                                            <h4 className="text-xs font-bold text-slate-400 uppercase tracking-wider px-4 mb-2 flex items-center gap-2">
                                                <Star className="h-3 w-3" /> Saved Searches
                                            </h4>
                                            {savedSearches.map((saved: any, idx: number) => (
                                                <button
                                                    key={saved.id}
                                                    id={`search-item-${idx}`}
                                                    onClick={() => setQuery(saved.query)}
                                                    onMouseEnter={() => setSelectedIndex(idx)}
                                                    className={cn(
                                                        "w-full flex items-center gap-3 px-4 py-2 text-left transition-colors",
                                                        selectedIndex === idx ? "bg-slate-100 dark:bg-white/10" : "hover:bg-slate-50 dark:hover:bg-white/5"
                                                    )}
                                                >
                                                    <Search className="h-4 w-4 text-slate-300 dark:text-slate-600" />
                                                    <span className="text-sm font-medium text-slate-700 dark:text-slate-300">{saved.name}</span>
                                                </button>
                                            ))}
                                        </div>
                                    )}
                                    {recentSearches.length > 0 && (
                                        <div>
                                            <h4 className="text-xs font-bold text-slate-400 uppercase tracking-wider px-4 mb-2 flex items-center gap-2">
                                                <Clock className="h-3 w-3" /> Recent Searches
                                            </h4>
                                            {recentSearches.map((recent: any, idx: number) => {
                                                const recentIdx = savedSearches.length + idx;
                                                return (
                                                    <button
                                                        key={recent.id}
                                                        id={`search-item-${recentIdx}`}
                                                        onClick={() => setQuery(recent.query)}
                                                        onMouseEnter={() => setSelectedIndex(recentIdx)}
                                                        className={cn(
                                                            "w-full flex items-center gap-3 px-4 py-2 text-left transition-colors",
                                                            selectedIndex === recentIdx ? "bg-slate-100 dark:bg-white/10" : "hover:bg-slate-50 dark:hover:bg-white/5"
                                                        )}
                                                    >
                                                        <Search className="h-4 w-4 text-slate-300 dark:text-slate-600" />
                                                        <span className="text-sm text-slate-600 dark:text-slate-300 truncate">{recent.query}</span>
                                                    </button>
                                                );
                                            })}
                                        </div>
                                    )}
                                    {savedSearches.length === 0 && recentSearches.length === 0 && (
                                        <div className="py-8 px-4 text-center">
                                            <div className="flex justify-center mb-3">
                                                <Command className="h-10 w-10 text-slate-200" />
                                            </div>
                                            <p className="text-sm text-slate-400">
                                                Search for anything across your platform.
                                            </p>
                                        </div>
                                    )}
                                </div>
                            )}
                        </div>

                        <div className="px-4 py-2 bg-slate-50 dark:bg-slate-950 border-t border-slate-100 dark:border-white/5 flex items-center justify-between text-[10px] font-bold text-slate-400">
                            <div className="flex gap-4">
                                <span className="flex items-center gap-1"><kbd className="bg-white dark:bg-slate-800 border dark:border-white/10 rounded px-1">↓</kbd><kbd className="bg-white dark:bg-slate-800 border dark:border-white/10 rounded px-1">↑</kbd> Navigate</span>
                                <span className="flex items-center gap-1"><kbd className="bg-white dark:bg-slate-800 border dark:border-white/10 rounded px-1">↵</kbd> Select</span>
                            </div>
                            {query.length > 0 && (
                                <button
                                    onClick={handleSaveSearch}
                                    disabled={savingFilter}
                                    className="flex items-center gap-1 hover:text-indigo-600 transition-colors bg-indigo-50 text-indigo-500 px-2 py-1 rounded"
                                >
                                    <Save className="h-3 w-3" />
                                    {savingFilter ? 'Saving...' : 'Save Search'}
                                </button>
                            )}
                        </div>
                    </div>
                </div>
            )}
        </>
    );
}
