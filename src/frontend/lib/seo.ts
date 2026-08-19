/**
 * Shared JSON-LD builders for the marketing pages.
 *
 * These exist because the same two shapes — a breadcrumb trail and the canonical reference to
 * the Upkilo organisation — were about to be hand-written on eight pages. Entity clarity is the
 * whole point of this markup: an engine resolves "Upkilo" as one thing only when every page
 * describes it identically, so the `@id` below must stay byte-identical to the Organization
 * node defined on the homepage (app/[locale]/page.tsx) and referenced by the contact page.
 * Eight hand-written copies is eight chances for one to drift and split the entity in two.
 */

export const SITE_URL = process.env.NEXT_PUBLIC_SITE_URL || 'https://upkilo.com';

/** Stable reference to the Organization node declared once on the homepage. */
export const ORGANIZATION_REF = { '@id': `${SITE_URL}/#organization` } as const;

export interface Crumb {
  /** Label shown in the search result's breadcrumb trail. */
  name: string;
  /** Path relative to the site root, e.g. "/en/features". Pass "/" for home. */
  path: string;
}

/**
 * BreadcrumbList for a page. Gives engines a stated position in the site rather than one
 * inferred from URL depth, and renders as a path instead of a bare URL in results.
 *
 * Always start the trail at Home so the chain resolves to the site root.
 */
export function breadcrumbJsonLd(crumbs: Crumb[]) {
  return {
    '@context': 'https://schema.org',
    '@type': 'BreadcrumbList',
    itemListElement: crumbs.map((crumb, i) => ({
      '@type': 'ListItem',
      position: i + 1,
      name: crumb.name,
      item: `${SITE_URL}${crumb.path}`,
    })),
  };
}

/** Convenience: the Home crumb every trail begins with. */
export const HOME_CRUMB: Crumb = { name: 'Home', path: '/en' };
