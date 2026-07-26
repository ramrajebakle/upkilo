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
