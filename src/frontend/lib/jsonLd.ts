/**
 * Serializes an object for a <script type="application/ld+json"> block.
 *
 * Escaping `<` is not optional here. JSON.stringify does NOT escape `</script>`, and these
 * blocks are injected via dangerouslySetInnerHTML, which bypasses React's own escaping — so
 * any string reaching JSON-LD (a tenant business name, a review body) could otherwise close
 * the script tag early and inject markup. `<` is valid inside a JSON string and parses
 * back to `<`, so escaping every `<` costs nothing and closes the hole regardless of which
 * field the untrusted value arrives in.
 *
 * Extracted from app/book/[category]/[city]/page.tsx and app/[locale]/book/[slug]/page.tsx,
 * which each carried their own copy — a third caller made a shared home worthwhile, and one
 * definition means a future fix to the escaping applies everywhere at once.
 */
export function safeJsonLd(obj: unknown): string {
  return JSON.stringify(obj).replace(/</g, '\\u003c');
}
