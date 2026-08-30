'use client';

import { useState } from 'react';
import { useRouter } from '@/navigation';
import { ArrowRight } from 'lucide-react';

/**
 * Email capture that forwards the entered address into the signup flow
 * (/register?email=...), so visitors don't re-type it.
 */
export default function EmailCapture({ dark = false }: { dark?: boolean }) {
  const router = useRouter();
  const [email, setEmail] = useState('');

  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = email.trim();
    router.push(trimmed ? `/register?email=${encodeURIComponent(trimmed)}` : '/register');
  };

  return (
    <form onSubmit={submit} className="mx-auto flex max-w-md flex-col gap-3 sm:flex-row">
      <input
        type="email"
        required
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        placeholder="Enter your work email"
        aria-label="Work email address"
        className={`flex-1 rounded-xl border px-4 py-3 text-sm outline-none transition-all focus:ring-2 focus:ring-primary-500 ${
          dark
            ? 'border-white/20 bg-white/10 text-white placeholder-foreground-muted focus:border-transparent'
            : 'border-border-strong bg-card text-foreground placeholder-foreground-muted focus:border-transparent'
        }`}
      />
      <button
        type="submit"
        className="group inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-xl bg-primary-600 px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-primary-500/30 transition-all hover:-translate-y-0.5 hover:bg-primary-500"
      >
        Start free trial
        <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-0.5" aria-hidden="true" />
      </button>
    </form>
  );
}
