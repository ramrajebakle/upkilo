/**
 * The staff list mapped field names the API never returned — `employmentStatus`
 * (which exists nowhere in the backend), `averageRating`, `totalBookings`,
 * `employmentStartDate`. Each resolved to undefined and fell through to a
 * default, so every member rendered as "offline" with 0 bookings and no join
 * date regardless of the truth.
 *
 * This asserts against the shape StaffController actually projects.
 */
import { describe, it, expect, vi } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';

// Exactly what the controller's Select(...) emits, camel-cased by ASP.NET.
const API_ROW = {
  id: 'st-1',
  firstName: 'Ada',
  lastName: 'Lovelace',
  email: 'ada@salon.test',
  phone: '+1 555 0100',
  role: 'Stylist',
  color: '#5b4cf5',
  isActive: true,
  title: 'Senior Stylist',
  avatarUrl: 'https://example.test/a.png',
  dateJoined: '2024-03-01T00:00:00Z',
  specialties: ['colour', 'balayage'],
  bookingsToday: 3,
  bookingsTotal: 412,
};

vi.mock('@/lib/api', () => ({
  api: { staff: { list: () => Promise.resolve({ data: { data: [API_ROW] } }) } },
  apiClient: {},
}));

import { useStaff } from '@/lib/query/staff';

function wrapper({ children }: { children: React.ReactNode }) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return React.createElement(QueryClientProvider, { client }, children);
}

describe('staff mapping', () => {
  it('reads the fields the API actually returns', async () => {
    const { result } = renderHook(() => useStaff(), { wrapper });
    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(result.current.data?.[0]).toEqual({
      id: 'st-1',
      firstName: 'Ada',
      lastName: 'Lovelace',
      email: 'ada@salon.test',
      phone: '+1 555 0100',
      role: 'Stylist',
      // Derived from isActive, not the non-existent employmentStatus.
      status: 'active',
      bookingsToday: 3,
      bookingsTotal: 412,
      specialties: ['colour', 'balayage'],
      joinedAt: '2024-03-01T00:00:00Z',
      title: 'Senior Stylist',
      avatarUrl: 'https://example.test/a.png',
    });
  });

  it('does not report an active member as offline', async () => {
    // The precise regression: isActive true previously produced 'offline',
    // because the mapping consulted a field that was always undefined.
    const { result } = renderHook(() => useStaff(), { wrapper });
    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(result.current.data?.[0].status).not.toBe('offline');
  });
});
