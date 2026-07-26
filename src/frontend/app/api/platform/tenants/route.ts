import { NextResponse } from 'next/server';
import { auth } from '@/auth';

// VULN-A01 FIX: Previously generated a SuperAdmin JWT with no auth check, allowing any caller
// to enumerate all tenants. Now requires an authenticated platform_owner/platform_admin session.
export async function GET() {
  try {
    const session = await auth();

    if (!session?.user) {
      return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
    }

    const { role } = session.user;
    if (role !== 'platform_owner' && role !== 'platform_admin') {
      return NextResponse.json({ error: 'Forbidden' }, { status: 403 });
    }

    // Forward the user's own session token to the backend instead of minting a self-signed JWT.
    // The accessToken is populated in the jwt() callback when the backend issues one on real login.
    // auth.ts assigns the JWT to session.user.accessToken, not session.accessToken —
    // reading the wrong path made this always undefined, so the handler returned 401
    // and the page rendered "Failed to load" for every platform admin.
    const accessToken = session.user.accessToken;
    if (!accessToken) {
      return NextResponse.json({ error: 'No access token in session' }, { status: 401 });
    }

    const backendUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
    const res = await fetch(`${backendUrl}/api/v1/super-admin/tenants?pageSize=50`, {
      headers: {
        Authorization: `Bearer ${accessToken}`,
        'Content-Type': 'application/json',
      },
      cache: 'no-store',
    });

    if (!res.ok) {
      console.error('Backend fetch failed:', res.status, await res.text());
      return NextResponse.json({ error: 'Failed to fetch from backend' }, { status: 502 });
    }

    const data = await res.json();
    return NextResponse.json(data.data || []);
  } catch (error) {
    console.error('Error fetching tenants:', error);
    return NextResponse.json({ error: 'Internal Server Error' }, { status: 500 });
  }
}
