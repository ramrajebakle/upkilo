import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';
import { format, parseISO, formatDistanceToNow } from 'date-fns';

// Merge Tailwind classes
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

// Format currency.
//
// Never throws. Intl.NumberFormat raises a RangeError on an unrecognised currency code or
// malformed locale tag, and because this is called during render, that error propagated up and
// blanked the whole page — a bad currency string on one tenant record took out the screen rather
// than showing an odd symbol. Both failure modes now degrade to "CODE 1,234.56".
//
// Note Intl already applies the correct minor units per currency (JPY renders with no decimals),
// so no exponent handling is needed on this path.
export function formatCurrency(amount: number, currency = 'USD', locale?: string): string {
  const code = (currency || 'USD').trim().toUpperCase();

  // Guard non-finite input separately: NaN/Infinity format as "NaN"/"∞" rather than throwing,
  // which is arguably worse on a billing screen because it looks like real output.
  const value = Number.isFinite(amount) ? amount : 0;

  let resolvedLocale = locale;
  if (!resolvedLocale && typeof window !== 'undefined') {
    resolvedLocale = document.documentElement.lang || navigator.language || 'en-US';
  } else if (!resolvedLocale) {
    resolvedLocale = 'en-US';
  }

  try {
    return new Intl.NumberFormat(resolvedLocale, {
      style: 'currency',
      currency: code,
    }).format(value);
  } catch {
    try {
      return `${code} ${value.toLocaleString(undefined, {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      })}`;
    } catch {
      return `${code} ${value.toFixed(2)}`;
    }
  }
}

// Format date
export function formatDate(date: string | Date, formatStr = 'PP', locale?: string): string {
  const d = typeof date === 'string' ? parseISO(date) : date;
  
  let resolvedLocale = locale;
  if (!resolvedLocale && typeof window !== 'undefined') {
    resolvedLocale = document.documentElement.lang || navigator.language;
  }
  
  if (resolvedLocale) {
    try {
      return new Intl.DateTimeFormat(resolvedLocale, {
        dateStyle: 'medium'
      }).format(d);
    } catch (e) {
      console.warn('[formatDate] Failed to use Intl formatter:', e);
    }
  }
  
  return format(d, formatStr);
}

// Format relative time
export function formatRelativeTime(date: string | Date): string {
  const d = typeof date === 'string' ? parseISO(date) : date;
  return formatDistanceToNow(d, { addSuffix: true });
}

// Format time
export function formatTime(date: string | Date, locale?: string): string {
  const d = typeof date === 'string' ? parseISO(date) : date;
  
  let resolvedLocale = locale;
  if (!resolvedLocale && typeof window !== 'undefined') {
    resolvedLocale = document.documentElement.lang || navigator.language;
  }
  
  if (resolvedLocale) {
    try {
      return new Intl.DateTimeFormat(resolvedLocale, {
        timeStyle: 'short'
      }).format(d);
    } catch (e) {
      console.warn('[formatTime] Failed to use Intl formatter:', e);
    }
  }
  
  return format(d, 'h:mm a');
}

// Truncate text
export function truncate(text: string, length: number): string {
  if (text.length <= length) return text;
  return text.slice(0, length) + '...';
}

// Generate initials
export function getInitials(name: string): string {
  return name
    .split(' ')
    .map((n) => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);
}

// Debounce function
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function debounce<T extends (...args: any[]) => any>(
  func: T,
  wait: number
): (...args: Parameters<T>) => void {
  let timeout: NodeJS.Timeout;
  return (...args: Parameters<T>) => {
    clearTimeout(timeout);
    timeout = setTimeout(() => func(...args), wait);
  };
}

// Sleep function
export function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

// Status color mapping
export function getStatusColor(status: string): string {
  const colors: Record<string, string> = {
    confirmed: 'bg-green-100 text-green-800',
    pending: 'bg-yellow-100 text-yellow-800',
    cancelled: 'bg-red-100 text-red-800',
    completed: 'bg-blue-100 text-blue-800',
    no_show: 'bg-gray-100 text-gray-800',
  };
  return colors[status.toLowerCase()] || 'bg-gray-100 text-gray-800';
}
