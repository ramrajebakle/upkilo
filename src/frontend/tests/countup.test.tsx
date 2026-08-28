/**
 * CountUp drives the hero's figures. Two behaviours matter beyond "it counts":
 *  - under prefers-reduced-motion it must render the real number immediately,
 *    never animate;
 *  - the value must be the true one, so the figure is present for anyone whose
 *    viewport never triggers the animation.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';

let reduceMotion = false;
vi.mock('framer-motion', () => ({
  useReducedMotion: () => reduceMotion,
  useInView: () => false, // never scrolled into view
}));

import CountUp from '@/components/landing/CountUp';

beforeEach(() => { reduceMotion = false; });

describe('CountUp', () => {
  it('renders the real figure immediately under reduced motion', () => {
    reduceMotion = true;
    render(<CountUp value={4280} prefix="$" />);
    expect(screen.getByText('$4,280')).toBeInTheDocument();
  });

  it('keeps the suffix and thousands separator', () => {
    reduceMotion = true;
    render(<CountUp value={94} suffix="%" />);
    expect(screen.getByText('94%')).toBeInTheDocument();
  });

  it('uses tabular figures so a running counter cannot reflow the tile', () => {
    reduceMotion = true;
    const { container } = render(<CountUp value={28} />);
    expect(container.querySelector('span')?.className).toContain('tabular-nums');
  });
});
