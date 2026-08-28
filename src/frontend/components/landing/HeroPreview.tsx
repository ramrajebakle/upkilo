'use client';

import { motion, useReducedMotion } from 'framer-motion';
import CountUp from './CountUp';

const ROWS = [
  { t: '10:00', s: 'Hair Color · Priya', a: 'P', st: 'Confirmed', c: 'text-emerald-400' },
  { t: '11:30', s: 'Deep Tissue · Rahul', a: 'R', st: 'Checked in', c: 'text-blue-400' },
  { t: '13:00', s: 'Manicure · Aisha', a: 'A', st: 'Deposit paid', c: 'text-primary-300' },
];

/**
 * The hero's product preview.
 *
 * Moved out of the server-rendered page so the figures can animate. The panel
 * is explicitly labelled as an example: the numbers are illustrative, and an
 * unlabelled dashboard showing "$4,280" beside real customer language invites
 * the reader to take it as someone's actual revenue. Saying so plainly costs
 * nothing and is the difference between a demo and an implied claim.
 */
export default function HeroPreview() {
  const reduce = useReducedMotion();

  return (
    <div className="relative mx-auto mt-16 max-w-3xl">
      <div
        className="absolute -inset-4 rounded-3xl bg-gradient-to-r from-primary-600/20 to-blue-600/20 blur-2xl"
        aria-hidden="true"
      />
      <figure className="relative m-0">
        <div className="relative animate-float rounded-2xl border border-white/10 bg-slate-900/80 p-5 text-left shadow-2xl backdrop-blur">
          {/* window bar */}
          <div className="mb-4 flex items-center gap-1.5" aria-hidden="true">
            <span className="h-2.5 w-2.5 rounded-full bg-rose-400/70" />
            <span className="h-2.5 w-2.5 rounded-full bg-amber-400/70" />
            <span className="h-2.5 w-2.5 rounded-full bg-emerald-400/70" />
            <span className="ml-3 text-xs text-slate-500">Today&rsquo;s schedule · Glow Studio</span>
          </div>

          <div className="grid gap-3 sm:grid-cols-3">
            <div className="rounded-xl border border-white/5 bg-white/[0.03] p-4">
              <p className="text-xs text-slate-400">Bookings today</p>
              <p className="mt-1 text-2xl font-bold text-white">
                <CountUp value={28} />
              </p>
              <p className="mt-1 text-xs text-emerald-400">▲ 12% vs last week</p>
            </div>
            <div className="rounded-xl border border-white/5 bg-white/[0.03] p-4">
              <p className="text-xs text-slate-400">Revenue</p>
              <p className="mt-1 text-2xl font-bold text-white">
                <CountUp value={4280} prefix="$" />
              </p>
              <p className="mt-1 text-xs text-emerald-400">▲ 8% vs last week</p>
            </div>
            <div className="rounded-xl border border-white/5 bg-white/[0.03] p-4">
              <p className="text-xs text-slate-400">Utilization</p>
              <p className="mt-1 text-2xl font-bold text-white">
                <CountUp value={94} suffix="%" />
              </p>
              <p className="mt-1 text-xs text-primary-300">Waitlist auto-fills gaps</p>
            </div>
          </div>

          {/* Rows arrive in sequence, the way a day's schedule fills in. */}
          <div className="mt-3 space-y-2">
            {ROWS.map((row, i) => (
              <motion.div
                key={row.t}
                initial={reduce ? false : { opacity: 0, x: -12 }}
                whileInView={reduce ? undefined : { opacity: 1, x: 0 }}
                viewport={{ once: true, margin: '-40px' }}
                transition={{ duration: 0.4, delay: 0.5 + i * 0.12, ease: [0.23, 1, 0.32, 1] }}
                className="flex items-center gap-3 rounded-lg border border-white/5 bg-white/[0.02] px-3 py-2.5"
              >
                <span className="w-12 text-xs font-medium text-slate-400 tabular-nums">{row.t}</span>
                <span className="flex h-7 w-7 items-center justify-center rounded-full bg-primary-500/20 text-xs font-bold text-primary-200">
                  {row.a}
                </span>
                <span className="flex-1 truncate text-sm text-slate-200">{row.s}</span>
                <span className={`text-xs font-medium ${row.c}`}>{row.st}</span>
              </motion.div>
            ))}
          </div>
        </div>

        <figcaption className="mt-3 text-center text-xs text-slate-500">
          Example dashboard. Figures are illustrative, not a customer&rsquo;s results.
        </figcaption>
      </figure>
    </div>
  );
}
