/**
 * Central date/time formatting utilities.
 * All date display in the web app should go through these functions
 * so locale changes and format preferences are applied consistently.
 *
 * Usage:
 *   import { fmtDate, fmtTime, fmtDateTime, fmtRelative } from '@/lib/dateFormatter';
 */

function resolveLocale(): string {
  if (typeof window !== 'undefined') {
    return document.documentElement.lang || navigator.language || 'en-US';
  }
  return 'en-US';
}

export function fmtDate(
  date: string | Date | null | undefined,
  opts: Intl.DateTimeFormatOptions = { year: 'numeric', month: 'short', day: 'numeric' }
): string {
  if (!date) return '—';
  try {
    const d = typeof date === 'string' ? new Date(date) : date;
    if (isNaN(d.getTime())) return '—';
    return new Intl.DateTimeFormat(resolveLocale(), opts).format(d);
  } catch {
    return String(date);
  }
}

export function fmtTime(
  date: string | Date | null | undefined,
  opts: Intl.DateTimeFormatOptions = { hour: 'numeric', minute: '2-digit' }
): string {
  if (!date) return '—';
  try {
    const d = typeof date === 'string' ? new Date(date) : date;
    if (isNaN(d.getTime())) return '—';
    return new Intl.DateTimeFormat(resolveLocale(), opts).format(d);
  } catch {
    return String(date);
  }
}

export function fmtDateTime(
  date: string | Date | null | undefined,
  opts: Intl.DateTimeFormatOptions = { year: 'numeric', month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit' }
): string {
  return fmtDate(date, opts);
}

export function fmtRelative(date: string | Date | null | undefined): string {
  if (!date) return '—';
  try {
    const d = typeof date === 'string' ? new Date(date) : date;
    if (isNaN(d.getTime())) return '—';
    const diffMs = d.getTime() - Date.now();
    const diffSecs = Math.round(diffMs / 1000);
    const diffMins = Math.round(diffSecs / 60);
    const diffHours = Math.round(diffMins / 60);
    const diffDays = Math.round(diffHours / 24);

    const rtf = new Intl.RelativeTimeFormat(resolveLocale(), { numeric: 'auto' });
    if (Math.abs(diffSecs) < 60) return rtf.format(diffSecs, 'second');
    if (Math.abs(diffMins) < 60) return rtf.format(diffMins, 'minute');
    if (Math.abs(diffHours) < 24) return rtf.format(diffHours, 'hour');
    return rtf.format(diffDays, 'day');
  } catch {
    return String(date);
  }
}
