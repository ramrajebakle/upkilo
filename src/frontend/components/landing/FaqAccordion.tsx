'use client';

import { useState } from 'react';
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion';
import { ChevronDown } from 'lucide-react';

interface Faq {
  q: string;
  a: string;
}

export default function FaqAccordion({ faqs }: { faqs: Faq[] }) {
  const [open, setOpen] = useState<number | null>(0);
  const reduce = useReducedMotion();

  return (
    <div className="space-y-3">
      {faqs.map((faq, i) => {
        const isOpen = open === i;
        return (
          <div
            key={faq.q}
            className={`rounded-2xl border bg-white transition-colors ${
              isOpen ? 'border-violet-300 shadow-sm shadow-violet-500/5' : 'border-slate-200 hover:border-slate-300'
            }`}
          >
            <button
              type="button"
              onClick={() => setOpen(isOpen ? null : i)}
              aria-expanded={isOpen}
              className="flex w-full items-center justify-between gap-4 px-6 py-5 text-left"
            >
              <span className="text-sm font-semibold text-slate-900 md:text-base">{faq.q}</span>
              <ChevronDown
                className={`h-5 w-5 flex-shrink-0 text-violet-500 transition-transform duration-300 ${
                  isOpen ? 'rotate-180' : ''
                }`}
                aria-hidden="true"
              />
            </button>
            <AnimatePresence initial={false}>
              {isOpen && (
                <motion.div
                  key="content"
                  initial={reduce ? false : { height: 0, opacity: 0 }}
                  animate={{ height: 'auto', opacity: 1 }}
                  exit={reduce ? undefined : { height: 0, opacity: 0 }}
                  transition={{ duration: 0.28, ease: [0.21, 0.47, 0.32, 0.98] }}
                  className="overflow-hidden"
                >
                  <p className="px-6 pb-5 text-sm leading-relaxed text-slate-600">{faq.a}</p>
                </motion.div>
              )}
            </AnimatePresence>
          </div>
        );
      })}
    </div>
  );
}
