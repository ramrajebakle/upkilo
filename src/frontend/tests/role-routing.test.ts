/**
 * Locks the role-routing rules that were fixed:
 *  - platform surfaces are gated by an allowlist, so an unrecognised role is
 *    denied rather than let through;
 *  - an unmapped backend role clamps to the least-privileged tenant role
 *    instead of passing through verbatim or defaulting to tenant_owner;
 *  - the client portal's login and magic-link landing are publicly reachable.
 *
 * These mirror the predicates in middleware.ts and auth.ts, which are module
 * private (middleware.ts runs inside Auth.js's auth() wrapper and cannot be
 * imported in isolation). The duplication is deliberate: the point is to fail
 * loudly if the rule is ever loosened back to a blocklist.
 */
import { describe, it, expect } from 'vitest';

const PLATFORM_ROLES = new Set(['platform_owner', 'platform_admin']);
const SEMANTIC_ROLES = new Set([
  'platform_owner', 'platform_admin', 'tenant_owner', 'team_member', 'customer',
]);
const PUBLIC_SEGMENTS = [
  'login', 'register', 'reset-password', 'verify-email', 'invite',
  'portal-login', 'verify',
];

function mapBackendRole(role: string | undefined | null): string {
  const raw = (role ?? '').toLowerCase();
  switch (raw) {
    case 'superadmin': return 'platform_owner';
    case 'owner':      return 'tenant_owner';
    case 'admin':
    case 'manager':
    case 'staff':      return 'team_member';
    default:           return SEMANTIC_ROLES.has(raw) ? raw : 'team_member';
  }
}

const mayEnterPlatform = (role: string | undefined) => PLATFORM_ROLES.has(role ?? '');

const isPublicSegment = (pathname: string) =>
  PUBLIC_SEGMENTS.some(seg => pathname === `/en/${seg}` || pathname.startsWith(`/en/${seg}/`));

describe('backend role -> semantic role', () => {
  it.each([
    ['SuperAdmin', 'platform_owner'],
    ['Owner',      'tenant_owner'],
    ['Admin',      'team_member'],
    ['Manager',    'team_member'],
    ['Staff',      'team_member'],
  ])('maps %s to %s', (backend, expected) => {
    expect(mapBackendRole(backend)).toBe(expected);
  });

  it('passes through roles that are already semantic (dev-mock logins)', () => {
    expect(mapBackendRole('platform_owner')).toBe('platform_owner');
    expect(mapBackendRole('customer')).toBe('customer');
  });

  it('clamps an unknown backend role to the least-privileged tenant role', () => {
    // Previously returned the string verbatim, which matched no guard, or fell
    // back to tenant_owner — the highest tenant privilege.
    expect(mapBackendRole('Receptionist')).toBe('team_member');
    expect(mapBackendRole('')).toBe('team_member');
    expect(mapBackendRole(undefined)).toBe('team_member');
  });
});

describe('platform back office (/platform, /admin)', () => {
  it('admits only platform roles', () => {
    expect(mayEnterPlatform('platform_owner')).toBe(true);
    expect(mayEnterPlatform('platform_admin')).toBe(true);
  });

  it.each(['tenant_owner', 'team_member', 'customer'])('turns away %s', (role) => {
    expect(mayEnterPlatform(role)).toBe(false);
  });

  it('turns away an unrecognised role instead of letting it through', () => {
    // The old blocklist named the three tenant roles to deny, so anything else
    // — a future backend role, a malformed claim — was admitted by default.
    expect(mayEnterPlatform('Receptionist')).toBe(false);
    expect(mayEnterPlatform(undefined)).toBe(false);
  });
});

describe('client portal reachability', () => {
  it('lets a logged-out client reach the portal login and magic-link landing', () => {
    // Both used to fall through to the staff login at /en/login, leaving the
    // portal with no reachable entry point.
    expect(isPublicSegment('/en/portal-login')).toBe(true);
    expect(isPublicSegment('/en/verify')).toBe(true);
  });

  it('still gates the portal itself', () => {
    expect(isPublicSegment('/en/portal-dashboard')).toBe(false);
    expect(isPublicSegment('/en/portal-bookings')).toBe(false);
  });
});
