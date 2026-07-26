/**
 * T4: 20 Vitest + Testing Library component tests.
 * Covers booking wizard, billing banner, demo banner, and offline indicator.
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor, act } from '@testing-library/react';
import React from 'react';

// ---------------------------------------------------------------------------
// Shared mocks (hoisted before imports)
// ---------------------------------------------------------------------------
vi.mock('next/navigation', () => ({
  useRouter: () => ({ push: vi.fn(), replace: vi.fn(), prefetch: vi.fn() }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/',
}));

vi.mock('next/link', () => ({
  default: ({ children, href }: { children: React.ReactNode; href: string }) =>
    React.createElement('a', { href }, children),
}));

// () => null is a valid React component — no React import needed inside the hoisted factory
vi.mock('lucide-react', () => ({
  WifiOff: () => null,
  TrendingDown: () => null,
  X: () => null,
  Beaker: () => null,
  RefreshCw: () => null,
  Zap: () => null,
  Check: () => null,
  CheckCircle2: () => null,
  Calendar: () => null,
  Clock: () => null,
  User: () => null,
  ArrowRight: () => null,
  ArrowLeft: () => null,
  Loader2: () => null,
}));

vi.mock('@/lib/api', () => ({
  apiClient: {
    get: vi.fn().mockResolvedValue({
      data: { data: { isSandbox: true, tenantName: 'Test Corp' } },
    }),
    post: vi.fn().mockResolvedValue({ data: {} }),
  },
}));

// ---------------------------------------------------------------------------
// 1–5: AnnualUpgradeBanner
// ---------------------------------------------------------------------------
import { AnnualUpgradeBanner } from '../components/billing/AnnualUpgradeBanner';

const BANNER_DATA = {
  eligible: true,
  showBanner: true,
  planName: 'Professional',
  monthlyAmount: 99,
  annualAmount: 79,
  savingsAmount: 240,
  savingsPercent: 20,
  currency: 'USD',
  monthsOnCurrentPlan: 3,
};

describe('AnnualUpgradeBanner', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({ ok: true, json: vi.fn().mockResolvedValue(BANNER_DATA) })
    );
  });
  afterEach(() => { vi.unstubAllGlobals(); });

  it('renders savings percentage after fetch resolves', async () => {
    render(<AnnualUpgradeBanner />);
    expect(await screen.findByText(/Save 20%/, {}, { timeout: 3000 })).toBeTruthy();
  });

  it('renders plan name after fetch resolves', async () => {
    render(<AnnualUpgradeBanner />);
    expect(await screen.findByText(/Professional/, {}, { timeout: 3000 })).toBeTruthy();
  });

  it('has a Switch Now link pointing to billing page', async () => {
    render(<AnnualUpgradeBanner />);
    const link = await screen.findByRole('link', { name: /switch now/i }, { timeout: 3000 });
    expect((link as HTMLAnchorElement).href).toContain('billing');
  });

  it('does not render when showBanner is false', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue({ ...BANNER_DATA, showBanner: false }),
      })
    );
    const { container } = render(<AnnualUpgradeBanner />);
    await act(async () => { await new Promise(r => setTimeout(r, 100)); });
    expect(container.firstChild).toBeNull();
  });

  it('does not render when eligible is false', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue({ ...BANNER_DATA, eligible: false }),
      })
    );
    const { container } = render(<AnnualUpgradeBanner />);
    await act(async () => { await new Promise(r => setTimeout(r, 100)); });
    expect(container.firstChild).toBeNull();
  });
});

// ---------------------------------------------------------------------------
// 6–10: DemoModeBanner
// ---------------------------------------------------------------------------
import DemoModeBanner from '../components/DemoModeBanner';

describe('DemoModeBanner', () => {
  it('shows DEMO MODE text when sandbox is active', async () => {
    render(<DemoModeBanner />);
    expect(await screen.findByText(/DEMO MODE/, {}, { timeout: 3000 })).toBeTruthy();
  });

  it('renders with role="banner" for accessibility', async () => {
    render(<DemoModeBanner />);
    expect(await screen.findByRole('banner', {}, { timeout: 3000 })).toBeTruthy();
  });

  it('shows Seed Data button when active', async () => {
    render(<DemoModeBanner />);
    expect(await screen.findByText(/Seed Data/, {}, { timeout: 3000 })).toBeTruthy();
  });

  it('shows Exit Demo button when active', async () => {
    render(<DemoModeBanner />);
    expect(await screen.findByText(/Exit Demo/, {}, { timeout: 3000 })).toBeTruthy();
  });

  it('has dismiss button with aria-label', async () => {
    render(<DemoModeBanner />);
    expect(
      await screen.findByRole('button', { name: /dismiss/i }, { timeout: 3000 })
    ).toBeTruthy();
  });
});

// ---------------------------------------------------------------------------
// 11–15: OfflineIndicator
// ---------------------------------------------------------------------------
import { OfflineIndicator } from '../components/OfflineIndicator';

describe('OfflineIndicator', () => {
  beforeEach(() => {
    Object.defineProperty(navigator, 'onLine', { value: true, configurable: true });
  });

  it('mounts without throwing', () => {
    expect(() => render(<OfflineIndicator />)).not.toThrow();
  });

  it('renders nothing when online', () => {
    const { container } = render(<OfflineIndicator />);
    expect(container.firstChild).toBeNull();
  });

  it('shows offline notice after window offline event', async () => {
    render(<OfflineIndicator />);
    await act(async () => {
      Object.defineProperty(navigator, 'onLine', { value: false, configurable: true });
      window.dispatchEvent(new Event('offline'));
    });
    expect(screen.getByText(/offline/i)).toBeTruthy();
  });

  it('hides offline notice after window online event', async () => {
    Object.defineProperty(navigator, 'onLine', { value: false, configurable: true });
    render(<OfflineIndicator />);
    await act(async () => { window.dispatchEvent(new Event('offline')); });
    await act(async () => {
      Object.defineProperty(navigator, 'onLine', { value: true, configurable: true });
      window.dispatchEvent(new Event('online'));
    });
    expect(screen.queryByText(/offline/i)).toBeNull();
  });

  it('renders alongside sibling components', () => {
    render(
      React.createElement('div', null,
        React.createElement(OfflineIndicator),
        React.createElement('span', null, 'sibling content')
      )
    );
    expect(screen.getByText('sibling content')).toBeTruthy();
  });
});

// ---------------------------------------------------------------------------
// 16–20: BookingWizard
// ---------------------------------------------------------------------------
import { BookingWizard } from '../components/booking/BookingWizard';

describe('BookingWizard', () => {
  beforeEach(() => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: true,
        json: vi.fn().mockResolvedValue([
          { id: 'svc1', name: 'Deep Tissue Massage', duration: 60, price: 85 },
        ]),
      })
    );
  });
  afterEach(() => { vi.unstubAllGlobals(); });

  it('renders a root element on mount', () => {
    const { container } = render(<BookingWizard tenantSlug="test-salon" />);
    expect(container.firstChild).toBeTruthy();
  });

  it('shows service name loaded from API', async () => {
    render(<BookingWizard tenantSlug="test-salon" />);
    expect(
      await screen.findByText('Deep Tissue Massage', {}, { timeout: 3000 })
    ).toBeTruthy();
  });

  it('shows service price', async () => {
    render(<BookingWizard tenantSlug="test-salon" />);
    expect(await screen.findByText(/85/, {}, { timeout: 3000 })).toBeTruthy();
  });

  it('shows service duration in minutes', async () => {
    render(<BookingWizard tenantSlug="test-salon" />);
    expect(await screen.findByText(/60/, {}, { timeout: 3000 })).toBeTruthy();
  });

  it('renders at least one interactive button', async () => {
    render(<BookingWizard tenantSlug="test-salon" />);
    await screen.findByText('Deep Tissue Massage', {}, { timeout: 3000 });
    expect(screen.queryAllByRole('button').length).toBeGreaterThan(0);
  });
});
