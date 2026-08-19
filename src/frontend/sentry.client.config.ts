import * as Sentry from "@sentry/nextjs";

Sentry.init({
  dsn: process.env.NEXT_PUBLIC_SENTRY_DSN,

  // Capture 10% of transactions for performance profiling.
  // Increase toward 1.0 only when investigating a specific latency regression.
  tracesSampleRate: 0.1,

  // Replay 10% of normal sessions, 100% of sessions with an error.
  replaysSessionSampleRate: 0.1,
  replaysOnErrorSampleRate: 1.0,

  integrations: [
    Sentry.replayIntegration({
      // Mask all text and block all media by default — GDPR-safe.
      maskAllText: true,
      blockAllMedia: true,
    }),
  ],

  // In development, print errors to console instead of sending to Sentry.
  enabled: process.env.NODE_ENV === "production",

  // Network-level fetch failures, in each browser's wording. These are environmental, not
  // defects: the request is aborted when the visitor navigates away mid-flight, loses
  // connectivity, or runs an extension or content blocker that intercepts window.fetch.
  //
  // The alert that prompted this was exactly that shape — an unhandled rejection on
  // /en/docs/custom-domains whose stack ran through `frame_ant.js`, an extension-injected
  // wrapper around window.fetch, on a page that itself issues no requests. Nothing degrades
  // for the visitor when it happens, and there is no fix available on our side.
  //
  // Deliberately narrow: these three strings are the browsers' own text for a transport-level
  // failure. Application API errors surface through axios as "Network Error" / AxiosError and
  // are NOT matched here, so real backend outages still alert.
  ignoreErrors: [
    "Failed to fetch", // Chrome/Edge
    "NetworkError when attempting to fetch resource", // Firefox
    "Load failed", // Safari
  ],

  // Errors thrown inside browser-extension code are not ours and cannot be fixed here. An
  // extension that wraps window.fetch puts its own frames in our stack traces, which is how
  // extension failures end up filed against the app.
  denyUrls: [
    /^chrome-extension:\/\//i,
    /^moz-extension:\/\//i,
    /^safari-web-extension:\/\//i,
    /extensions\//i,
  ],

  beforeSend(event) {
    // Strip any residual PII from the error message before it leaves the browser.
    if (event.message) {
      event.message = event.message.replace(
        /[\w.+-]+@[\w-]+\.[\w.]+/g,
        "[email]"
      );
    }
    return event;
  },
});
