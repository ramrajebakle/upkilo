'use client';

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { Command, X } from 'lucide-react';

interface Shortcut {
  keys: string[];
  description: string;
  category: string;
  action?: () => void;
}

const getShortcuts = (router: ReturnType<typeof useRouter>): Shortcut[] => [
  // Navigation
  { keys: ['G', 'D'], description: 'Go to Dashboard', category: 'Navigation', action: () => router.push('/dashboard') },
  { keys: ['G', 'B'], description: 'Go to Bookings', category: 'Navigation', action: () => router.push('/bookings') },
  { keys: ['G', 'C'], description: 'Go to Clients', category: 'Navigation', action: () => router.push('/clients') },
  { keys: ['G', 'S'], description: 'Go to Services', category: 'Navigation', action: () => router.push('/services') },
  { keys: ['G', 'T'], description: 'Go to Staff', category: 'Navigation', action: () => router.push('/staff') },
  { keys: ['G', 'P'], description: 'Go to Payments', category: 'Navigation', action: () => router.push('/payments') },
  { keys: ['G', 'A'], description: 'Go to Analytics', category: 'Navigation', action: () => router.push('/analytics') },
  { keys: ['G', 'W'], description: 'Go to Workflows', category: 'Navigation', action: () => router.push('/automation/workflows') },
  // Quick Actions
  { keys: ['N', 'B'], description: 'New Booking', category: 'Quick Actions', action: () => router.push('/bookings/new') },
  { keys: ['N', 'C'], description: 'New Client', category: 'Quick Actions', action: () => router.push('/clients/new') },
  { keys: ['N', 'S'], description: 'New Service', category: 'Quick Actions', action: () => router.push('/services/new') },
  { keys: ['N', 'W'], description: 'New Workflow', category: 'Quick Actions', action: () => router.push('/automation/workflows/new') },
  // Global
  { keys: ['⌘', 'K'], description: 'Open Command Palette', category: 'Global' },
  { keys: ['?'], description: 'Show Keyboard Shortcuts', category: 'Global' },
  { keys: ['Esc'], description: 'Close Modal / Dialog', category: 'Global' },
];

export function KeyboardShortcutsOverlay({ onClose }: { onClose?: () => void } = {}) {
  const [isOpen, setIsOpen] = useState(!onClose); // standalone mode when no onClose
  const router = useRouter();
  const shortcuts = getShortcuts(router);

  const close = () => {
    setIsOpen(false);
    onClose?.();
  };

  useEffect(() => {
    let keySequence: string[] = [];
    let sequenceTimer: ReturnType<typeof setTimeout> | null = null;

    const handleKeyDown = (e: KeyboardEvent) => {
      // Don't trigger in input fields
      const tag = (e.target as HTMLElement)?.tagName;
      if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return;

      // '?' to toggle overlay (only in standalone mode)
      if (!onClose && e.key === '?' && !e.metaKey && !e.ctrlKey) {
        e.preventDefault();
        setIsOpen(prev => !prev);
        return;
      }

      if (e.key === 'Escape') {
        close();
        keySequence = [];
        return;
      }

      // Two-key sequences
      if (e.metaKey || e.ctrlKey || e.altKey) return;

      const key = e.key.toUpperCase();
      keySequence.push(key);

      if (sequenceTimer) clearTimeout(sequenceTimer);
      sequenceTimer = setTimeout(() => { keySequence = []; }, 1000);

      if (keySequence.length >= 2) {
        const seq = keySequence.slice(-2);
        const match = shortcuts.find(s => s.action && s.keys.length === 2 &&
          s.keys[0] === seq[0] && s.keys[1] === seq[1]);
        if (match?.action) {
          e.preventDefault();
          match.action();
          keySequence = [];
        }
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [shortcuts]);

  const categories = [...new Set(shortcuts.map(s => s.category))];

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-[300] flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-sm">
      <div className="fixed inset-0" onClick={close} />
      <div className="relative w-full max-w-2xl bg-white rounded-2xl shadow-2xl border border-slate-200 overflow-hidden max-h-[80vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between px-5 py-4 border-b border-slate-100 bg-slate-50">
          <div className="flex items-center gap-2">
            <Command className="h-5 w-5 text-indigo-500" />
            <h2 className="font-semibold text-slate-900">Keyboard Shortcuts</h2>
          </div>
          <button onClick={close} className="text-slate-400 hover:text-slate-600 p-1 rounded">
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Content */}
        <div className="overflow-y-auto p-5 grid grid-cols-1 md:grid-cols-2 gap-6">
          {categories.map(category => (
            <div key={category}>
              <h3 className="text-xs font-bold text-slate-400 uppercase tracking-wider mb-3">{category}</h3>
              <div className="space-y-2">
                {shortcuts.filter(s => s.category === category).map((shortcut, idx) => (
                  <div key={idx} className="flex items-center justify-between py-1.5">
                    <span className="text-sm text-slate-700">{shortcut.description}</span>
                    <div className="flex items-center gap-1">
                      {shortcut.keys.map((key, ki) => (
                        <span key={ki} className="inline-flex items-center justify-center min-w-[28px] h-6 px-1.5 bg-slate-100 border border-slate-300 rounded text-xs font-bold text-slate-600 font-mono shadow-sm">
                          {key}
                        </span>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>

        {/* Footer */}
        <div className="px-5 py-3 bg-slate-50 border-t border-slate-100 text-center text-xs text-slate-500">
          Press <kbd className="bg-white border border-slate-300 rounded px-1 font-mono">?</kbd> to toggle this overlay &nbsp;·&nbsp;
          Press <kbd className="bg-white border border-slate-300 rounded px-1 font-mono">Esc</kbd> to close
        </div>
      </div>
    </div>
  );
}

// Hook for programmatic access
export function useKeyboardShortcut(keys: string[], callback: () => void) {
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      const tag = (e.target as HTMLElement)?.tagName;
      if (tag === 'INPUT' || tag === 'TEXTAREA') return;

      const key = e.key.toLowerCase();
      const metaKey = e.metaKey || e.ctrlKey;

      if (keys.length === 1 && keys[0].toLowerCase() === key && !metaKey) {
        callback();
      } else if (keys.includes('cmd') || keys.includes('ctrl')) {
        const mainKey = keys.find(k => k !== 'cmd' && k !== 'ctrl');
        if (metaKey && mainKey && mainKey.toLowerCase() === key) {
          e.preventDefault();
          callback();
        }
      }
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [keys, callback]);
}
