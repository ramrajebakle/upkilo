// Checkout.js is a third-party hosted script (not an npm package — Razorpay does not
// publish one), so it must be injected once and awaited rather than imported normally.
// Injects at most once per page load: a second call while the first is still loading
// reuses the same in-flight promise instead of adding a duplicate <script> tag.

const CHECKOUT_JS_SRC = "https://checkout.razorpay.com/v1/checkout.js";

let loadPromise: Promise<void> | null = null;

export function loadRazorpayCheckout(): Promise<void> {
  if (typeof window === "undefined") {
    return Promise.reject(new Error("Razorpay Checkout can only be loaded in the browser"));
  }

  if ((window as any).Razorpay) {
    return Promise.resolve();
  }

  if (loadPromise) {
    return loadPromise;
  }

  loadPromise = new Promise<void>((resolve, reject) => {
    const existing = document.querySelector<HTMLScriptElement>(`script[src="${CHECKOUT_JS_SRC}"]`);
    if (existing) {
      existing.addEventListener("load", () => resolve());
      existing.addEventListener("error", () => reject(new Error("Failed to load Razorpay Checkout")));
      return;
    }

    const script = document.createElement("script");
    script.src = CHECKOUT_JS_SRC;
    script.async = true;
    script.onload = () => resolve();
    script.onerror = () => {
      loadPromise = null; // allow a retry on the next call rather than caching a permanent failure
      reject(new Error("Failed to load Razorpay Checkout"));
    };
    document.body.appendChild(script);
  });

  return loadPromise;
}
