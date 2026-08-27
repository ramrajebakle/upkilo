import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { qk } from './keys';
import { unwrapList } from './unwrap';
import { HOOK_MANAGED } from './config';

export interface Service {
  id: string;
  name: string;
  description: string;
  durationMinutes: number;
  price: number;
  currency: string;
  color: string;
  isActive: boolean;
  maxAttendees: number;
}

export function useServices() {
  return useQuery({
    queryKey: qk.services.list(),
    queryFn: () => api.services.list(HOOK_MANAGED).then(unwrapList<Service>),
    staleTime: 60_000,
  });
}
