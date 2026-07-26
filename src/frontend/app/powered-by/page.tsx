"use client";

import { Suspense, useEffect } from "react";
import { useSearchParams, useRouter } from "next/navigation";

/**
 * Signup interceptor page for "Powered by Upkilo" widget badge clicks.
 *
 * Flow:
 *   1. Widget badge links to /powered-by?src={tenantSlug}&ref={encodedReferrerUrl}
 *   2. This page fires a tracking event to POST /api/v1/discovery/widget-click
 *   3. Redirects to /register?utm_source=widget&utm_medium=referral&utm_campaign={slug}
 *
 * The 3-second delay shows a value prop before redirecting — increases conversion.
 */
export default function PoweredByPage() {
  return (
    <Suspense>
      <PoweredByContent />
    </Suspense>
  );
}

function PoweredByContent() {
  const params = useSearchParams();
  const router = useRouter();

  const src = params.get("src") ?? "";
  const ref = params.get("ref") ?? "";

  useEffect(() => {
    const apiBase = process.env.NEXT_PUBLIC_API_URL ?? "";

    // Fire tracking event (fire-and-forget, don't block redirect)
    fetch(`${apiBase}/api/v1/discovery/widget-click`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ sourceSlug: src, referrerUrl: ref }),
      keepalive: true,
    }).catch(() => null);

    // Redirect to register after 2.5 seconds with attribution params
    const timer = setTimeout(() => {
      const registerUrl = `/register?utm_source=widget&utm_medium=referral&utm_campaign=${encodeURIComponent(src)}&utm_content=powered_by_badge`;
      router.replace(registerUrl);
    }, 2500);

    return () => clearTimeout(timer);
  }, [src, ref, router]);

  return (
    <div className="min-h-screen bg-gradient-to-br from-indigo-600 to-violet-700 flex flex-col items-center justify-center px-4 text-white text-center">
      <div className="max-w-md">
        <div className="mb-6 text-6xl">⚡</div>
        <h1 className="text-3xl font-extrabold mb-3">Want online booking like this?</h1>
        <p className="text-indigo-100 text-lg mb-6">
          This business runs on Upkilo — the AI-powered booking platform for service businesses.
          Get set up in under 10 minutes.
        </p>
        <ul className="text-left text-indigo-100 text-sm space-y-2 mb-8 max-w-xs mx-auto">
          <li className="flex items-center gap-2">✓ Online booking widget in 5 minutes</li>
          <li className="flex items-center gap-2">✓ AI Copilot that writes client messages for you</li>
          <li className="flex items-center gap-2">✓ Automated reminders — no more no-shows</li>
          <li className="flex items-center gap-2">✓ Free plan — no credit card required</li>
        </ul>
        <p className="text-indigo-200 text-sm">Taking you to sign up…</p>
        <div className="mt-4 h-1 bg-indigo-500 rounded-full overflow-hidden">
          <div className="h-full bg-white rounded-full animate-[progress_2.5s_ease-in-out_forwards]" style={{ width: "0%" }} />
        </div>
      </div>
    </div>
  );
}
