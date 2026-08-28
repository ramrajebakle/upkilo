'use client';

import { useEffect, useRef, useState } from 'react';
import { useInView, useReducedMotion } from 'framer-motion';

/**
 * Counts a number up once, when it scrolls into view.
 *
 * The hero preview is the only place on this page that shows the product rather
 * than describing it, and a static screenshot of numbers reads as a screenshot.
 * Letting the figures settle makes the panel read as a working dashboard.
 *
 * Two details keep it from costing more than it gives:
 *  - the final value is rendered immediately under prefers-reduced-motion, and
 *    is what server-rendering emits, so the real number is in the HTML for
 *    search engines and anyone who never triggers the animation;
 *  - `tabular-nums` fixes digit width, so a counter running 0 -> 4,280 does not
 *    reflow the tile on every frame.
 */
export default function CountUp({
  value,
  prefix = '',
  suffix = '',
  durationMs = 1100,
  className = '',
}: {
  value: number;
  prefix?: string;
  suffix?: string;
  durationMs?: number;
  className?: string;
}) {
  const ref = useRef<HTMLSpanElement>(null);
  const inView = useInView(ref, { once: true, margin: '-40px' });
  const reduce = useReducedMotion();
  const [display, setDisplay] = useState(value);
  const [started, setStarted] = useState(false);

  useEffect(() => {
    if (reduce || !inView || started) return;
    setStarted(true);

    let frame = 0;
    const start = performance.now();

    const tick = (now: number) => {
      const t = Math.min((now - start) / durationMs, 1);
      // Ease-out cubic: leaves fast and settles, so the number is legible well
      // before the animation formally ends.
      const eased = 1 - Math.pow(1 - t, 3);
      setDisplay(Math.round(value * eased));
      if (t < 1) frame = requestAnimationFrame(tick);
    };

    frame = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frame);
  }, [inView, reduce, started, value, durationMs]);

  // Before the counter starts, show 0 rather than the final value, or the number
  // would visibly jump backwards on the first frame. Reduced motion never enters
  // this branch and keeps the real figure throughout.
  const shown = reduce ? value : started ? display : 0;

  return (
    <span ref={ref} className={`tabular-nums ${className}`}>
      {prefix}
      {shown.toLocaleString('en-US')}
      {suffix}
    </span>
  );
}
