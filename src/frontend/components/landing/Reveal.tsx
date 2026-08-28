'use client';

import { motion, useReducedMotion } from 'framer-motion';
import type { ReactNode } from 'react';

/**
 * Scroll-triggered reveal wrapper. Fades + slides children into view once.
 * Honours prefers-reduced-motion (renders statically when motion is reduced).
 */
export default function Reveal({
  children,
  delay = 0,
  y = 24,
  className = '',
}: {
  children: ReactNode;
  delay?: number;
  y?: number;
  className?: string;
}) {
  const reduce = useReducedMotion();

  if (reduce) {
    return <div className={className}>{children}</div>;
  }

  return (
    <motion.div
      // Stable hook for the <noscript> rule in app/[locale]/layout.tsx. Framer
      // Motion serialises `initial` as an inline style, so the server sends
      // opacity:0 on every wrapper — including the one around the <h1>. With
      // scripting unavailable nothing ever animates it back, and the marketing
      // page renders blank. The class gives that rule something to target.
      className={`reveal ${className}`}
      initial={{ opacity: 0, y }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: '-80px' }}
      transition={{ duration: 0.55, delay, ease: [0.21, 0.47, 0.32, 0.98] }}
    >
      {children}
    </motion.div>
  );
}
