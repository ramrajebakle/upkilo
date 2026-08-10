"use client";

import { AnimatePresence, MotionConfig, motion } from "framer-motion";
import { usePathname } from "next/navigation";

// Strong ease-out. Framer's built-in "easeOut" is the weak CSS-equivalent curve; UI motion
// wants something that leaves fast and settles gently, so the element is where the user
// expects it well before the animation formally ends.
const EASE_OUT = [0.23, 1, 0.32, 1] as const;

export function AnimationProvider({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();

  return (
    // reducedMotion="user" is the important part, and it is deliberately here rather than in
    // each animated component.
    //
    // globals.css already zeroes animation-duration under prefers-reduced-motion, but that
    // rule only governs CSS animations and transitions. Framer Motion animates via
    // JavaScript, so it never saw that media query — every motion.* element in the app
    // ignored the OS setting outright. Only 2 of the 20 files using framer-motion called
    // useReducedMotion themselves, and this provider, which animates on every single route
    // change, was not one of them.
    //
    // Setting it once at the provider covers the whole tree, including components that never
    // opt in. Framer's "user" mode keeps opacity fades and drops transform-based movement,
    // which matches the guidance that reduced motion means gentler motion, not none.
    <MotionConfig reducedMotion="user">
      <AnimatePresence mode="wait">
        <motion.div
          key={pathname}
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -10 }}
          transition={{ duration: 0.2, ease: EASE_OUT }}
        >
          {children}
        </motion.div>
      </AnimatePresence>
    </MotionConfig>
  );
}
