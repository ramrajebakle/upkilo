'use client';

import { useEffect, useRef, useState } from 'react';
import { useSession, signOut } from 'next-auth/react';
import { AlertTriangle, RefreshCw, X } from 'lucide-react';
import { Button } from '@/components/ui/Button';

const WARN_BEFORE_MS = 5 * 60 * 1000; // 5 minutes
const CHECK_INTERVAL_MS = 30 * 1000;   // check every 30s

export function SessionExpiryWarning() {
  const { data: session, update } = useSession();
  const [showWarning, setShowWarning] = useState(false);
  const [showExpired, setShowExpired] = useState(false);
  const [secondsLeft, setSecondsLeft] = useState(WARN_BEFORE_MS / 1000);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const countdownRef = useRef<ReturnType<typeof setInterval> | null>(null);

  useEffect(() => {
    if (!session?.expires) return;

    const check = () => {
      const expiresAt = new Date(session.expires).getTime();
      const now = Date.now();
      const remaining = expiresAt - now;

      if (remaining <= 0) {
        setShowWarning(false);
        setShowExpired(true);
        clearInterval(intervalRef.current!);
        clearInterval(countdownRef.current!);
      } else if (remaining <= WARN_BEFORE_MS && !showExpired) {
        setShowWarning(true);
        setSecondsLeft(Math.floor(remaining / 1000));
      }
    };

    check();
    intervalRef.current = setInterval(check, CHECK_INTERVAL_MS);
    return () => {
      clearInterval(intervalRef.current!);
      clearInterval(countdownRef.current!);
    };
  }, [session?.expires]);

  // Live countdown when warning is visible
  useEffect(() => {
    if (!showWarning) return;
    countdownRef.current = setInterval(() => {
      setSecondsLeft((s) => {
        if (s <= 1) {
          setShowWarning(false);
          setShowExpired(true);
          clearInterval(countdownRef.current!);
          return 0;
        }
        return s - 1;
      });
    }, 1000);
    return () => clearInterval(countdownRef.current!);
  }, [showWarning]);

  const handleExtend = async () => {
    await update(); // NextAuth re-fetches and extends the JWT
    setShowWarning(false);
  };

  const handleSignOut = () => signOut({ callbackUrl: '/en/login' });

  const formatTime = (secs: number) => {
    const m = Math.floor(secs / 60);
    const s = secs % 60;
    return m > 0 ? `${m}:${s.toString().padStart(2, '0')}` : `${s}s`;
  };

  if (!showWarning && !showExpired) return null;

  // Session expired — full-screen blocking modal
  if (showExpired) {
    return (
      <div
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="session-expired-title"
        aria-describedby="session-expired-desc"
        className="fixed inset-0 z-[9999] flex items-center justify-center bg-black/60 backdrop-blur-sm p-4"
      >
        <div className="bg-white dark:bg-slate-900 rounded-2xl shadow-2xl p-8 max-w-sm w-full text-center">
          <div className="w-16 h-16 rounded-full bg-amber-100 dark:bg-amber-900/30 flex items-center justify-center mx-auto mb-4">
            <AlertTriangle className="h-8 w-8 text-amber-500" aria-hidden="true" />
          </div>
          <h2 id="session-expired-title" className="text-xl font-bold text-slate-900 dark:text-white mb-2">
            Session expired
          </h2>
          <p id="session-expired-desc" className="text-slate-500 dark:text-slate-400 text-sm mb-6">
            Your session has ended. Sign in again to continue — your unsaved work may be recoverable from auto-save.
          </p>
          <Button fullWidth onClick={handleSignOut} size="lg">
            Sign in again
          </Button>
        </div>
      </div>
    );
  }

  // 5-minute warning toast
  return (
    <div
      role="alert"
      aria-live="assertive"
      aria-atomic="true"
      className="fixed bottom-6 right-6 z-[9000] flex items-start gap-3 bg-amber-50 dark:bg-amber-950/80 border border-amber-200 dark:border-amber-800 rounded-2xl shadow-xl p-4 max-w-sm w-full animate-fade-in-up"
    >
      <AlertTriangle className="h-5 w-5 text-amber-500 shrink-0 mt-0.5" aria-hidden="true" />
      <div className="flex-1 min-w-0">
        <p className="text-sm font-semibold text-amber-900 dark:text-amber-200">
          Session expiring in {formatTime(secondsLeft)}
        </p>
        <p className="text-xs text-amber-700 dark:text-amber-400 mt-0.5">
          Click "Stay signed in" to continue your session.
        </p>
        <div className="flex gap-2 mt-3">
          <Button size="sm" variant="secondary" onClick={handleExtend} leftIcon={<RefreshCw size={13} />}>
            Stay signed in
          </Button>
          <Button size="sm" variant="ghost" onClick={handleSignOut}>
            Sign out
          </Button>
        </div>
      </div>
      <button
        onClick={() => setShowWarning(false)}
        className="text-amber-500 hover:text-amber-700 shrink-0"
        aria-label="Dismiss session warning"
      >
        <X size={16} />
      </button>
    </div>
  );
}
