/**
 * The blocking script that decides the theme before the browser paints.
 *
 * WHY THIS EXISTS
 * Theme used to be applied in a `useEffect` inside ThemeProvider. An effect runs after
 * hydration, which is after first paint, so every single page load rendered the light
 * theme first and then snapped to dark — a full-screen white flash on every navigation
 * that hit the server, and on every refresh. There is no way to fix that from React:
 * by the time any component runs, the paint has already happened.
 *
 * The only thing that runs before first paint is a synchronous, non-deferred script in
 * <head>. That is what this is. It is the one place in the app where a raw
 * `dangerouslySetInnerHTML` string is the correct tool rather than a smell.
 *
 * WHAT IT DOES
 *   1. Reads the stored preference ('light' | 'dark' | 'system').
 *   2. Resolves 'system' — and anything unset or corrupt — against prefers-color-scheme.
 *   3. Puts the resolved class on <html>, which is what both the `.dark` token block and
 *      the class-based `dark:` variant key off.
 *   4. Sets style.colorScheme, so the browser paints its own surfaces — form controls,
 *      scrollbars, the canvas behind an overscroll — to match. Without it, a dark page
 *      still gets a white scrollbar and white native selects.
 *
 * It is wrapped in try/catch because localStorage throws outright in Safari's private
 * mode and wherever a browser is configured to block site data. A theme preference is not
 * worth a white screen of death, so the failure mode is "fall back to the OS setting".
 *
 * Kept deliberately tiny — it is render-blocking, so every byte is on the critical path.
 */

// Single source of truth for the key, imported by ThemeProvider so the reader and the
// writer cannot drift apart.
export const THEME_STORAGE_KEY = 'theme';

const SCRIPT = `(function(){try{
var t=localStorage.getItem('${THEME_STORAGE_KEY}');
if(t!=='light'&&t!=='dark')t=window.matchMedia('(prefers-color-scheme: dark)').matches?'dark':'light';
var e=document.documentElement;
e.classList.add(t);
e.style.colorScheme=t;
}catch(_){}})();`;

export function ThemeScript() {
  return <script suppressHydrationWarning dangerouslySetInnerHTML={{ __html: SCRIPT }} />;
}
