# Upkilo UI Plan — Web + Mobile

Written 2026-08-11. Every number below was measured against the working tree, not estimated.
Sources: `impeccable` (surface modes, craft floor), `ui-ux-pro-max` (severity-rated UX rules,
palette database), `design-taste-frontend` (colour consistency), `design-system` (token layers),
`animate` / `review-animations` (motion budget).

---

## 1. What the UI actually is

| | count |
|---|---|
| `page.tsx` (all locales) | 251 |
| Dashboard pages (`(dashboard)`) | **205** |
| Portal / auth / platform / admin | 20 |
| Marketing + legal + docs | 13 |
| Components | 110 (26 in `components/ui`) |

**The dashboard is the product.** 205 of 238 English pages. Marketing is 6 pages. Any plan that
spends its budget on the landing page is spending it in the wrong place.

### The component library is half-adopted — this is the central fact

| shared component | pages importing it | raw equivalent in the wild |
|---|---|---|
| `Button` | 127 / 205 | **535 raw `<button>`** |
| `Card` | 79 / 205 | 299 raw card divs |
| `Input` | 31 / 205 | **350 raw `<input>`** |
| `Modal` | 10 / 205 | — |
| `Badge` | 8 / 205 | — |
| `Table` | 2 / 205 | **70 raw `<table>`** |
| `StatCard` | **0** | — |
| `EmptyState` | **0** | — |
| `Skeleton` | **0** | — |
| `PageHeader` | 1 / 205 | — |

`Button.tsx` already solves touch target, focus ring, press feedback, disabled state, and loading
state. 535 elements opted out of all five. **The work is migration, not redesign.** Three components
were built and never used at all.

---

## 2. The one decision that blocks everything

Four brand colours are in play:

| token | usages |
|---|---|
| `primary-*` | 842 |
| `indigo-*` | 822 |
| `violet-*` | 423 |
| `purple-*` | 137 |

`manifest.json` declares `theme_color: #7C3AED` (violet-600). `Button.tsx` renders `primary-500`
(`#5b4cf5`). The PWA's own chrome disagrees with its primary button.

**Recommendation: `primary-*`, anchored at `#5b4cf5`.**

Three reasons, in order of weight:
1. It is already the token layer in `globals.css` — the other three are raw Tailwind palette
   references, which `ui-ux-pro-max` rule #6 classifies as an anti-pattern ("raw hex in components").
2. `ui-ux-pro-max` `colors.csv` row 2 (*Micro SaaS*) recommends `#6366F1` — a near-neighbour of
   `#5b4cf5`. The database independently lands where the token layer already is.
3. `design-taste-frontend`'s colour-consistency rule: a genuinely purple brand stays purple.
   Upkilo is purple. This is not a repaint, it is picking which purple.

Nothing downstream is safe to start until this is locked, because every migrated component
inherits the answer. **This is the only item on this plan requiring a human decision.**

---

## 3. Surface modes

`impeccable` assigns a mode per *surface*, not per product. Upkilo has four:

| surface | pages | mode | what "good" means |
|---|---|---|---|
| Dashboard, platform, admin | 213 | **Operate** | Scanability, consistency, density. Brand lives in precise details, not expression. |
| Booking widget `/book/[slug]`, portal, checkout | 11 | **Operate** | A stranger completing a task on a phone, once. Zero learning budget. |
| Landing, pricing, features, medical-spa, marketplace, contact | 6 | **Persuade** | Design *is* the product here. Earn the trial. |
| Docs, legal | 4 | **Read** | Structure for comprehension. |

The booking widget deserves separate emphasis: it is the only surface a *customer of your customer*
ever sees, it is overwhelmingly mobile, and it is the surface Upkilo is judged by in the wild.
It is 2 pages. It should be the best-finished thing in the repo and currently is not distinguished.

---

## 4. Mobile: measured state

| check | rule | measured | verdict |
|---|---|---|---|
| Touch targets ≥44×44 | ux-guidelines #22, **High** | 398 elements at `h-6/h-7/h-8` (24–32px) | **fail** |
| Tables scroll on mobile | #71, Medium | 70 `<table>`, 44 `overflow-x-auto`; **15+ pages have none** | **fail** |
| No horizontal scroll | #69, **High** | 47 fixed `w-[NNNpx]`, 23 `min-w-[NNNpx]` | **at risk** |
| Responsive breakpoints | #65/#66, **High** | 193 of 401 files have **no** breakpoint at all | **fail** |
| Dark mode | — | 85 of 401 files | 21% coverage |
| Mobile nav | #9 | Dashboard sidebar ✅, portal ✅ | pass |
| PWA shell | — | `standalone`, `portrait-primary` ✅, wrong `theme_color` ❌ | partial |

Breakpoint targets per #65: **320, 375, 414, 768, 1024, 1440**.

Note: 193 files without breakpoints is not automatically a defect — many are leaf components inside
already-responsive parents. It is a list to triage, not a list to fix.

---

## 5. Motion: measured state

| | count |
|---|---|
| `transition-*` usages | 1145 |
| `hover:scale-*` | 82 |
| framer-motion files | 20 |
| `motion-reduce:` guards | **4** |

Framer Motion is covered globally by `MotionConfig reducedMotion="user"` (added earlier this
session). CSS transitions are not, and 82 of them move geometry. Colour transitions need no guard;
transform transitions do. Scope: the 82, not the 1145.

---

## 6. Laws of UX — what they exposed that the audit above missed

Applied from [lawsofux.com](https://lawsofux.com/). Only the laws that produced a *measurable*
finding are listed; the rest are real but not actionable here.

### Hick's Law + Miller's Law — the largest single finding

> *Hick's Law: the time it takes to make a decision increases with the number and complexity of choices.*
> *Miller's Law: the average person holds 7±2 items in working memory.*

The dashboard sidebar has **132 destinations in 7 groups** — roughly **19 per group**.

The 7 top-level groups (Scheduling, Clients & Team, Services & Revenue, Marketing, Automation,
Insights, Settings) are exactly right: 7 is the centre of Miller's range. Everything beneath them
is 2–3× over it. A tenant owner looking for one screen scans a 19-item list, and 205 pages means
this gets worse with every feature shipped.

**Nothing in the audit above would have caught this** — every individual page can be well-built
while the structure connecting them is the actual problem. This is now Phase 1.

### Doherty Threshold — perceived speed

> *Productivity soars when interaction stays under 400ms.*

| | count |
|---|---|
| Pages with some loading state | 185 / 205 |
| Bare spinners (`animate-spin`) | **309** |
| `Skeleton` component usage | **0** |
| `loading.tsx` route files | 2 |

Coverage is good; the *technique* is wrong. A spinner communicates "wait" and nothing else, and it
throws away the layout. A skeleton holds the layout still and reads as faster at identical actual
speed. You already own `Skeleton.tsx` and nothing imports it. This is the cheapest perceived-
performance win available — swapping technique, not writing code.

### Law of Similarity / Law of Common Region — 89 different empty states

**89 of 205 pages have empty-state text. `EmptyState` usage: 0.** Eighty-nine hand-written,
mutually inconsistent versions of the same moment. Empty states are where an app reads as
unfinished, and they are disproportionately what a *new* tenant sees — every screen is empty on
day one. Adopting the component that already exists collapses 89 variants into one.

### Peak-End Rule — the booking confirmation

> *People judge an experience by its peak and its end, not its average.*

The public booking flow is 4 files, and exactly **1** contains confirmation/success wording. The
end of that flow is the last thing a stranger sees of Upkilo, and it is the moment they decide
whether the business they just booked looks professional. It is currently the thinnest part of the
most-judged surface.

### Jakob's Law — conventions on the booking widget

> *Users spend most of their time on other sites; they expect yours to work the same way.*

The booking widget competes with Calendly, Fresha, and Booksy. Novelty there is a cost, not a
feature. This argues for *restraint* on that surface — the opposite instinct to the marketing pages.

### Aesthetic-Usability Effect — why Phase 4 is not vanity

> *Users perceive aesthetically pleasing design as more usable.*

The polish work is not decoration; it changes how usable the product is judged to be, including in
trial-to-paid decisions. It still comes after correctness, because the effect does not survive a
layout that breaks at 320px.

---

## 7. Plan

### Phase 0 — Lock the brand *(blocking, ~1h, needs your yes)*
- Decide the purple. Recommendation above.
- Collapse `indigo-*` / `violet-*` / `purple-*` → `primary-*` (1,382 replacements, mechanical).
- Fix `manifest.json` `theme_color` to match.
- Extend `globals.css` semantic tokens so no page needs a raw palette reference again.
- **Gate:** zero raw `indigo|violet|purple` outside `globals.css`.

### Phase 1 — Navigation IA *(new — added by Hick's Law, cheapest high-impact work here)*
132 destinations, ~19 per group. Structural, not visual — no repaint required.
- Rank all 132 by actual usage. Pareto says ~20% carry most traffic; the tail belongs behind
  search or a "more" affordance, not in a permanent list.
- Target ≤7–9 visible items per group; demote the rest.
- Add command-palette / search-first navigation. At 132 destinations, search stops being a
  convenience and becomes the primary way in.
- Serial Position Effect: put the highest-value items **first and last** in each group — those are
  the positions users remember.
- **Gate:** no group exceeds 9 visible items.

### Phase 2 — Component migration *(the main event)*
Highest leverage in the repo. Each migration fixes a whole class of defect at once.

1. **535 raw `<button>` → `Button`** — fixes touch target, focus-visible ring, press feedback,
   disabled and loading states simultaneously. Resolves most of the 398 undersized targets.
2. **350 raw `<input>` → `Input`** — fixes visible labels (#8), focus rings, error placement.
3. **70 raw `<table>` → `Table`** — bake `overflow-x-auto` into the component so the 15 broken
   pages cannot recur.
4. **Adopt the three zero-usage components** — `StatCard`, `EmptyState`, `Skeleton`. Empty and
   loading states are where an app feels unfinished, and Upkilo currently has no shared treatment.

Do this surface by surface, most-trafficked first. Not all 205 pages at once.

### Phase 3 — Mobile correctness
- Triage the 47 fixed-px widths → `max-w-full` / fluid.
- Triage the 193 no-breakpoint files (expect most to be fine).
- Verify at all six widths from #65.
- **Gate:** no horizontal scroll at 320px on any surface.

### Phase 4 — Booking widget
Treat as its own project. 2 pages, mobile-first, the surface strangers judge you by.

### Phase 5 — Marketing (Persuade)
6 pages. Only after the product is coherent — a landing page promising polish the app doesn't
deliver is worse than a plain one.

### Phase 6 — Motion pass
- `motion-reduce:` on the 82 transform transitions.
- Audit durations against the 150–300ms budget (#7).
- Frequency discipline: high-frequency actions get near-imperceptible motion or none.

---

## 8. Verification

Per `impeccable`: **bounded passes, not a loop.** Build fully → inspect once with desktop and
mobile batched together → fix everything that round showed in one batch → confirm with at most
one more round → stop.

Non-negotiable gates: `tsc --noEmit` clean, `next build` exit 0, no horizontal scroll at 320px,
contrast ≥4.5:1 on changed surfaces.

Local stack for testing is documented in §8.

---

## 9. Local environment

No Docker required (WSL is not installed on the dev machine; Docker Desktop cannot start).

| service | where | notes |
|---|---|---|
| Frontend | `localhost:3000` | `npm run dev` in `src/frontend` |
| API | `localhost:5000` | `dotnet run --project src/backend/Upkilo.API` |
| PostgreSQL 17.6 | `localhost:5432` | portable, `C:\Users\Ramraje\upkilo-local\pgsql` |
| Redis 5.0.14 | `localhost:6379` | portable, **required** — JWT revocation is fail-closed |

Test login: `owner@glowbeauty.test` / `Test@1234!` (3 tenants, 58 bookings, 15 services seeded).
The dev quick-login buttons issue a token the backend rejects; use the real email to see data.

---

## 10. Deliberately excluded

- **shadcn/ui adoption.** 21st.dev and the `ui-styling` skill both assume it. Adding it now means
  two parallel component systems in one folder while the existing one is only half-adopted.
  Revisit after Phase 1.
- **Redesign.** `impeccable`: refinement preserves, redesign replaces, never split the difference.
  The identity here is fine; the execution is inconsistent. This plan is refinement throughout.
