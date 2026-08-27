import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { qk } from './keys';
import { unwrapList } from './unwrap';
import { HOOK_MANAGED } from './config';

export interface StaffMember {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  role: string;
  /**
   * Only two states are real. The page previously also styled an amber "away"
   * badge, which nothing could ever produce — the value was derived from a
   * field the API does not return.
   */
  status: 'active' | 'offline';
  bookingsToday: number;
  bookingsTotal: number;
  specialties: string[];
  joinedAt: string;
  title: string;
  avatarUrl: string;
}

/**
 * Maps the fields StaffController actually projects.
 *
 * The page's old inline mapping read `employmentStatus`, `averageRating`,
 * `totalBookings` and `employmentStartDate` — none of which the endpoint
 * returns, and `employmentStatus` does not exist anywhere in the backend. Every
 * one resolved to undefined and fell through to a default, so the page showed
 * each member as "offline" with 0 bookings and no join date no matter what the
 * data said. Wrong with confidence is worse than blank.
 */
function normalizeStaff(s: Record<string, unknown>): StaffMember {
  const str = (v: unknown) => (typeof v === 'string' ? v : '');
  const num = (v: unknown) => (typeof v === 'number' ? v : 0);
  return {
    id: str(s.id),
    firstName: str(s.firstName),
    lastName: str(s.lastName),
    email: str(s.email),
    phone: str(s.phone),
    role: str(s.role),
    status: s.isActive === true ? 'active' : 'offline',
    bookingsToday: num(s.bookingsToday),
    bookingsTotal: num(s.bookingsTotal),
    specialties: Array.isArray(s.specialties) ? (s.specialties as string[]) : [],
    joinedAt: str(s.dateJoined),
    title: str(s.title),
    avatarUrl: str(s.avatarUrl),
  };
}

export function useStaff() {
  return useQuery({
    queryKey: qk.staff.list(),
    queryFn: () =>
      api.staff
        .list(HOOK_MANAGED)
        .then((res) => unwrapList<Record<string, unknown>>(res).map(normalizeStaff)),
    staleTime: 60_000,
  });
}

/**
 * Delete, then invalidate rather than splicing local state. The page used to
 * filter the row out by hand, which left the header counts and the pagination
 * total computed from a list the server had never confirmed.
 */
export function useDeleteStaff() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.staff.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: qk.staff.all }),
  });
}
