"use client";

import { useEffect, useCallback, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useLocale } from "next-intl";
import { KeyboardShortcutsOverlay } from "@/components/KeyboardShortcuts";

const ROUTE_MAP: Record<string, string> = {
  d: '/dashboard',
  b: '/bookings',
  c: '/clients',
  s: '/services',
  t: '/staff',
  p: '/payments',
  a: '/analytics',
  w: '/automation/workflows',
  r: '/reports',
  e: '/settings',
};

const ACTION_ROUTES: Record<string, string> = {
  b: '/bookings/new',
  c: '/clients/new',
  s: '/services/new',
  w: '/automation/workflows/new',
};

/**
 * ShortcutManager — global keyboard shortcuts:
 * CMD/CTRL + K: Open Command Palette
 * ?: Show keyboard shortcuts overlay
 * g+d: Dashboard, g+b: Bookings, g+c: Clients, etc.
 * n+b: New Booking, n+c: New Client, etc.
 */
export function ShortcutManager() {
  const router = useRouter();
  const locale = useLocale();
  const lastKeyRef = useRef<string | null>(null);
  const timeoutRef = useRef<NodeJS.Timeout | null>(null);
  const [showShortcuts, setShowShortcuts] = useState(false);

  const navigate = useCallback((path: string) => {
    router.push(`/${locale}${path}`);
  }, [router, locale]);

  const handleKeyDown = useCallback(
    (event: KeyboardEvent) => {
      const isInput =
        event.target instanceof HTMLInputElement ||
        event.target instanceof HTMLTextAreaElement ||
        (event.target as HTMLElement).isContentEditable;

      if (isInput && event.key !== "Escape") return;

      // Escape: close shortcut overlay
      if (event.key === "Escape") {
        setShowShortcuts(false);
        return;
      }

      // ? to toggle shortcuts overlay
      if (event.key === "?" && !event.metaKey && !event.ctrlKey) {
        event.preventDefault();
        setShowShortcuts(prev => !prev);
        return;
      }

      // CMD/CTRL + K: Command Palette
      if ((event.metaKey || event.ctrlKey) && event.key === "k") {
        event.preventDefault();
        // The GlobalSearch component handles this natively — fire a custom event as fallback
        document.dispatchEvent(new CustomEvent('open-command-palette'));
        return;
      }

      const k = event.key.toLowerCase();

      // Two-key sequences
      if (lastKeyRef.current === "g") {
        if (timeoutRef.current) clearTimeout(timeoutRef.current);
        lastKeyRef.current = null;
        const path = ROUTE_MAP[k];
        if (path) {
          event.preventDefault();
          navigate(path);
        }
        return;
      }

      if (lastKeyRef.current === "n") {
        if (timeoutRef.current) clearTimeout(timeoutRef.current);
        lastKeyRef.current = null;
        const path = ACTION_ROUTES[k];
        if (path) {
          event.preventDefault();
          navigate(path);
        }
        return;
      }

      // First key of sequence
      if (k === "g" || k === "n") {
        if (!event.metaKey && !event.ctrlKey && !event.altKey) {
          lastKeyRef.current = k;
          if (timeoutRef.current) clearTimeout(timeoutRef.current);
          timeoutRef.current = setTimeout(() => { lastKeyRef.current = null; }, 1200);
        }
        return;
      }

      lastKeyRef.current = null;
    },
    [navigate]
  );

  useEffect(() => {
    window.addEventListener("keydown", handleKeyDown);
    return () => {
      window.removeEventListener("keydown", handleKeyDown);
      if (timeoutRef.current) clearTimeout(timeoutRef.current);
    };
  }, [handleKeyDown]);

  return (
    <>
      {showShortcuts && (
        <KeyboardShortcutsOverlay onClose={() => setShowShortcuts(false)} />
      )}
    </>
  );
}

export default ShortcutManager;
