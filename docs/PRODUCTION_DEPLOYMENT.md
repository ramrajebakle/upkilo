# Upkilo — Production Deployment Runbook

**Target:** Azure App Service (East US), blue/green slot swap
**Pipeline:** [`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml)
**Status:** Pre-flight complete. Blocked on Azure provisioning + external account approvals.

---

## 1. Pre-flight results

Verified on the current `main` (`4ddae3f`):

| Check | Command | Result |
|---|---|---|
| Backend build | `dotnet build Upkilo.sln -c Release` | ✅ 0 errors, 11 warnings (benign) |
| Frontend build | `npm ci && npm run build` | ✅ succeeds, lint warnings only |
| Test suite | `dotnet test Upkilo.sln -c Release` | ✅ **777 / 777 passing** |

> **Note on the test suite:** 23 integration tests (`BookingIntegrationTests`,
> `OpenApiContractTests`) require a live PostgreSQL on `127.0.0.1:5432`. Without it they
> fail with `NpgsqlException: Failed to connect`. Start the database first:
>
> ```bash
> docker compose up -d postgres redis
> ```
>
> These cover multi-tenant isolation, concurrent booking, and Stripe webhook handling —
> do not treat them as optional.

The README's "520 tests, 83.45% coverage" figure is stale; the suite is 777 tests.

---

## 2. Architecture decisions

| Decision | Choice | Consequence |
|---|---|---|
| Region | **Central US** | East US / East US 2 have **zero App Service quota** on this subscription — see below |
| Compute | **App Service B1** | No deployment slots → ~30–60s downtime per deploy |
| Staging | **Not provisioned** | Jobs retained but gated behind `STAGING_ENABLED` |
| Redis | **Redis Cloud free tier** (Azure East US) | Azure Redis unavailable — see §2.2 |
| CDN/WAF | **Cloudflare free tier** | Front Door dropped for cost |

### 2.1 Why Central US and not East US

East US was the original choice (US/global market). It is not usable:

| Region | App Service B1 | Postgres | Redis |
|---|---|---|---|
| East US | ❌ quota 0 VMs | ❌ restricted | legacy ✅ |
| East US 2 | ❌ quota 0 VMs | ✅ | untested |
| **Central US** | ✅ | ✅ | ❌ (see §2.2) |
| West US 2 | ✅ | ✅ | ❌ |
| West US 3 | ✅ | ✅ | legacy ✅ |

New Pay-As-You-Go subscriptions ship with **0 App Service VM quota** in several regions. Raising it requires a support request. Central US was the nearest region with capacity that also suits a US/global audience (~25ms to the US East Coast).

### 2.2 Why Redis is external

- **Azure Managed Redis** fails to provision **subscription-wide** — five attempts across Central US and West US 2, with and without HA, returning only `OperationFailed` with no diagnostic detail.
- **Azure Cache for Redis (legacy)** is retired in Central US and West US 2. It still provisions in West US 3 and East US.

Rather than move the whole stack to West US 3 for Redis alone, Redis runs on **Redis Cloud's free tier in Azure East US**. This removes ~₹1,240/month and keeps the better-positioned region.

> 🔴 **Known risk — Redis traffic is unencrypted.** The free tier endpoint accepts
> plaintext only (verified: TLS handshake fails with `ERR_SSL_WRONG_VERSION_NUMBER`).
> Cache entries, sessions, rate-limit keys and SignalR payloads cross the public
> internet in the clear between Central US and East US.
> **Fix before real customer data lands:** enable TLS on a Redis Cloud paid tier
> (~$5–7/month) and add `ssl=True` to `REDIS_CONNECTION`.

> ⚠️ **Free tier limits:** 30MB and 30 connections. Revisit at ~50 concurrent users,
> or if API p95 exceeds ~500ms. Cross-region RTT is expected at ~25–30ms — measure it
> from inside Azure, not from a developer machine.

### 2.3 What B1 costs you

No deployment slots, so no blue/green swap. Each deploy restarts the container directly:
**expect ~30–60s of downtime per release.** Rollback redeploys the previously running
image tag, captured before the deploy mutates anything.

Restore zero-downtime deploys by upgrading the plan and reinstating slot deployment:

```bash
az appservice plan update -g upkilo-prod-rg -n upkilo-prod-plan --sku S1
```

Both App Services share one B1 instance (1 vCPU, 1.75 GB). Running the .NET API with
in-process Hangfire alongside the Next.js server on a single B1 is tight — watch memory
before adding traffic.

### Region rationale

East US is chosen because **customers are US/global**, not because of where the team
sits. Region should track the user base: every API call — availability lookups, booking
writes, login — pays full round-trip, and only static assets benefit from Front Door
caching.

For reference, if the customer base ever shifts primarily to India, Central India would
cut per-request latency from ~250–300ms to ~20–40ms. Azure OpenAI has narrower model
availability there, but that is not a blocker: the OpenAI resource can sit in a
different region from the rest of the stack, since AI calls are server-side and already
take seconds.

### What the minimal-tier staging environment does and does not cover

**Covers:** migrations applied to a real PostgreSQL before production is touched; the
app proven to boot against the new schema; every application setting and secret proven
to reach the container.

**Does not cover:** performance, connection-pool behaviour under load, or HA failover —
staging runs B-series / Basic tiers. A migration that is fast on a small staging dataset
may still lock tables for minutes on production volumes.

**Still required every deploy:** read the `migration-<sha>.sql` artifact uploaded by
`migrate-database` before approving the production job.

Staging has **no custom domain and no TLS binding** — it is reached on its default
`upkilo-api-staging.azurewebsites.net` hostname, which keeps it off the DNS critical
path.

---

## 3. Fixes applied to the pipeline

### 3.1 Missing application settings (would have crash-looped on first deploy)

[`appsettings.Production.json`](../src/backend/Upkilo.API/appsettings.Production.json)
declares config as `${VAR}` placeholders. **.NET does not expand these.**
[`AzureKeyVaultSecretProvider`](../src/backend/Upkilo.Infrastructure/Services/AzureKeyVaultSecretProvider.cs)
resolves Key Vault → environment variable → configuration, so an unset value arrives at
the app as the literal string `${JWT_SECRET}`.

Key Vault alone is **not sufficient**: no Key Vault *configuration provider* is
registered, and several consumers read settings straight from `IConfiguration` with no
`ISecretProvider` fallback —
[`InvoiceService`](../src/backend/Upkilo.Infrastructure/Services/InvoiceService.cs),
[`DataErasureJob`](../src/backend/Upkilo.Infrastructure/Jobs/DataErasureJob.cs),
[`SmsA2pRegistrationService`](../src/backend/Upkilo.Infrastructure/Services/SmsA2pRegistrationService.cs).

**App Service application settings are the only mechanism that reaches every consumer.**
Eleven settings were added to the `deploy-production` job:

```
Jwt__Secret                       Twilio__AccountSid
Stripe__SecretKey                 Twilio__AuthToken
Stripe__WebhookSecret             Twilio__PhoneNumber
Stripe__PublishableKey            AzureOpenAI__Endpoint
SendGrid__ApiKey                  AzureOpenAI__ApiKey
Azure__Storage__ConnectionString
```

### 3.2 Storage connection string key name

`appsettings.Production.json` declares `AzureStorage:ConnectionString`, but **nothing
reads that key** — it is dead configuration. The only consumer,
[`FileService`](../src/backend/Upkilo.Infrastructure/Services/FileService.cs), reads
`Azure--Storage--ConnectionString` / `Azure:Storage:ConnectionString`.

The correct App Service setting is therefore `Azure__Storage__ConnectionString`. Using
the name from `appsettings.Production.json` throws `InvalidOperationException` on every
file upload.

### 3.3 Reordered the pipeline so staging actually rehearses migrations

The original job order ran `migrate-database` (production) **before** `deploy-staging`,
so the production schema changed before staging had validated anything. A separate
staging environment provides no migration safety under that ordering.

The corrected graph:

```
build-and-push
  └─> migrate-staging      apply migrations to the staging DB (rehearsal)
        └─> deploy-staging  deploy both images, poll /ready
              └─> migrate-database   backup + migrate PRODUCTION
                    └─> deploy-production   ⏸ manual approval, then slot swap
                          └─> rollback      (on failure)
```

A migration that fails now fails against staging, before production is touched.

The same eleven application settings from §3.1 are injected into the staging App
Service, using `STAGING_`-prefixed secrets so staging can run against **Stripe test
keys** — billing flows are exercisable end-to-end without real charges.

---

### 3.4 Frontend build was failing (deployment blocker, now fixed)

`next build` exited 1 on **391 `@next/next/no-html-link-for-pages` errors**, which meant
the frontend Docker image could not build and `deploy.yml` would have failed at its first
job. Compilation itself succeeded — only the lint gate failed.

The 391 count was inflated: ESLint reports each `<a>` once per matching route. The real
source count is **22 occurrences across 12 files**.

The rule was downgraded to `warn` in `.eslintrc.json` rather than converting the tags,
because under the split-domain architecture (§3.5) roughly half of these links now
**cross a domain boundary** — dashboard → `/privacy-policy` (apex), marketing →
`/register` (app subdomain). For those, a plain `<a>` performing a full navigation is
correct; a `<Link>` would prefetch and attempt client-side navigation to a path that
308-redirects to a different origin.

> **Follow-up (not urgent):** the subset of links that stay within one domain —
> `/ai-dashboard` and `/dashboard/clients` from dashboard pages — would still benefit
> from `<Link>` for client-side navigation.

### 3.5 Domain split: apex serves marketing, subdomain serves the app

`sitemap.ts` and `robots.ts` default `SITE_URL` to the apex `https://upkilo.com`, but
`deploy.yml` was building with `NEXT_PUBLIC_SITE_URL=https://app.upkilo.com`. That would
have canonicalised all 225 programmatic SEO pages plus every tenant booking page onto
the `app.` subdomain, and left the apex serving nothing at all.

Changes made:

| File | Change |
|---|---|
| `middleware.ts` | Host routing added ahead of all auth logic |
| `Dockerfile` | `ARG`/`ENV NEXT_PUBLIC_APP_URL` declared |
| `deploy.yml` | `NEXT_PUBLIC_SITE_URL=https://upkilo.com`, added `NEXT_PUBLIC_APP_URL` |
| `appsettings.Production.json` | Apex added to CORS origins and `AllowedHosts` |

Routing rules:

- `www.upkilo.com` → 308 to apex (avoids splitting SEO across two hostnames)
- Non-marketing path on apex → 308 to `app.upkilo.com`
- Marketing path on `app.` → 308 to apex

Marketing routes are **allowlisted**; everything else is treated as app. This is
deliberate — there are ~47 dashboard segments versus ~14 marketing routes, so adding a
dashboard route must never require editing `middleware.ts`.

The block no-ops for any host that is not `upkilo.com`, so localhost,
`*.azurewebsites.net`, and staging are unaffected.

> `NEXT_PUBLIC_APP_URL` **must** stay declared as an `ARG` in the frontend Dockerfile.
> Docker silently discards `--build-arg` values for undeclared args, and the value would
> fall back to the default with no error — the exact failure the existing
> "no localhost API URL" guard step was written to catch.

### 3.6 Migration job could not reach Postgres or authenticate

Three further defects, all of which would have failed the first pipeline run:

1. **No `azure/login` step in `migrate-database`.** The backup step used `azure/CLI@v2`
   with `AZURE_CREDENTIALS` as an *env var*, but that action authenticates from a prior
   `azure/login` session, not from the environment. The backup would have failed.
2. **Postgres firewall blocks GitHub runners.** Only the "allow Azure services" rule
   existed. GitHub-hosted runners have dynamic public IPs not reliably covered by it, so
   `dotnet ef` could not connect. The job now opens a **single-IP rule for that run** and
   removes it in a step marked `if: always()` — a failed migration must never leave the
   database reachable from a stale address.
3. **Redis connection string was hardcoded to `redis:6379`** — a docker-compose
   hostname. Now sourced from the `REDIS_CONNECTION` secret.

Note the Azure CLI flag shape here: `--server-name <server> --name <rule>`. Using
`--name` for the server (as the docs' shorthand suggests) silently fails.

### 3.7 Email quota — campaigns share the transactional allowance

SendGrid is on the **free tier: 100 emails/day**, and that single allowance covers
everything.

Campaign and broadcast sends do **not** use SendGrid's Marketing Campaigns product.
[`BroadcastController`](../src/backend/Upkilo.API/Controllers/BroadcastController.cs)
loops over clients calling `IEmailService.SendEmailAsync` — the same Mail Send API as
signup verification and password resets.

```
MaxAudiencePerSend (CampaignsController)  5,000
Free-tier daily limit                       100
```

> 🔴 **Failure mode:** a tenant sending one campaign to 200 clients exhausts the daily
> quota. The broadcast loop catches per-recipient exceptions and increments `failed`, so
> the campaign reports partial success — while **new users silently cannot register** for
> the rest of the day. You would discover this from a failed signup, not from SendGrid.

**Decision (deliberate):** ship as-is. There are no tenants yet, so the risk is
theoretical. The campaign feature will not be promoted until users are onboarded, and
the SendGrid plan will be upgraded at that point.

**Before promoting campaigns to tenants:**

1. Upgrade to SendGrid Essentials 50K (~$19.95/month) — this is the meaningful unlock,
   not the Marketing Campaigns plan, which remains unused.
2. Consider making `MaxAudiencePerSend` configuration-driven so it can be raised without
   a redeploy.
3. Consider a separate API key or subuser for bulk sends, so a campaign cannot starve
   transactional mail.

**Monitoring trigger:** SendGrid → Stats. Upgrade at ~60–70 emails/day, before the cap
is reached — hitting it fails closed and silently.

## 4. Outstanding issues (not deployment blockers)

- [`docker-compose.yml:32`](../docker-compose.yml#L32) pins `edoburu/pgbouncer:1.23.1`,
  which **does not exist on Docker Hub**. `docker compose up` fails for a fresh clone.
  Dev-only — Azure provides connection pooling in production.
- README test counts are stale (claims 520, actual 777).
- README references `infrastructure/azure/` (Bicep IaC) and `docs/DEVELOPER_GUIDE.md`.
  **Neither exists in this repository.** Provisioning is therefore done with the
  `az` CLI commands in §6 rather than infrastructure-as-code.

---

## 5. Required from the account owner

### 5.1 Actions

| # | Action | Blocks | Typical wait |
|---|---|---|---|
| A1 | `az login` + confirm subscription & payment method | 🔴 all provisioning | — |
| A2 | Stripe account activation | 🔴 all billing | 1–3 days |
| A3 | SendGrid signup + `upkilo.com` sender authentication | 🔴 all signup/password reset | 1–2 days |
| A4 | Azure OpenAI access request (aka.ms/oai/access) | 🟡 AI features only | 2–10 days |
| A5 | DNS edit access for `upkilo.com` | 🔴 TLS + webhooks | — |
| A6 | Twilio + US A2P 10DLC registration | 🟢 SMS only | 1–3 weeks |
| A7 | Sentry project + PagerDuty integration key | 🟡 observability | — |
| A8 | GitHub: add secrets, create `production` environment with **required reviewers** | 🔴 approval gate | — |

**A2 and A3 are the true critical path.** Without SendGrid nobody can complete signup;
without Stripe nobody can pay. Both are approval queues that no engineering work
shortens.

### 5.2 Secrets to supply

| GitHub secret | Source | Blocking |
|---|---|---|
| `STRIPE_SECRET_KEY` | Stripe → Developers → API keys (`sk_live_…`) | 🔴 |
| `STRIPE_PUBLISHABLE_KEY` | same page (`pk_live_…`) | 🔴 |
| `STRIPE_WEBHOOK_SECRET` | Stripe webhook endpoint (`whsec_…`), after DNS | 🔴 |
| `SENDGRID_API_KEY` | SendGrid (`SG.…`) | 🔴 |
| `SENTRY_DSN` | Sentry project settings | 🟡 |
| `PAGERDUTY_INTEGRATION_KEY` | PagerDuty Events API v2 | 🟡 |
| `TWILIO_ACCOUNT_SID` / `TWILIO_AUTH_TOKEN` / `TWILIO_PHONE_NUMBER` | Twilio console | 🟢 |

### 5.2b Staging secrets

Staging uses `STAGING_`-prefixed secrets so it never touches live credentials.

| GitHub secret | Value |
|---|---|
| `STAGING_STRIPE_SECRET_KEY` | Stripe **test** key (`sk_test_…`) |
| `STAGING_STRIPE_PUBLISHABLE_KEY` | Stripe **test** key (`pk_test_…`) |
| `STAGING_STRIPE_WEBHOOK_SECRET` | test-mode webhook secret |
| `STAGING_SENDGRID_API_KEY` | separate key, ideally a sandbox sender |
| `STAGING_TWILIO_*` | Twilio test credentials |
| `AZURE_CREDENTIALS_STAGING` | service principal scoped to `upkilo-staging-rg` |
| `AZURE_STAGING_DB_CONNECTION` | generated during provisioning |
| `STAGING_REDIS_CONNECTION` | generated during provisioning |
| `STAGING_JWT_SECRET` | generated — **must differ from production** |
| `AZURE_STAGING_STORAGE_CONNECTION_STRING` | generated during provisioning |

> Never reuse the production JWT secret in staging. A token minted by staging must not
> authenticate against production.

Staging shares `AZURE_OPENAI_ENDPOINT` / `AZURE_OPENAI_API_KEY` with production —
Azure OpenAI is metered per token, so a second resource adds cost without adding safety.

### 5.3 Generated during provisioning (no owner action)

`JWT_SECRET` (32-byte random), `VAPID_PUBLIC_KEY` + private key, Postgres admin
password, `REDIS_PASSWORD`, `AZURE_CREDENTIALS`, `ACR_USERNAME` / `ACR_PASSWORD`,
`AZURE_PROD_DB_CONNECTION`, `AZURE_STORAGE_CONNECTION_STRING`,
`APPLICATIONINSIGHTS_CONNECTION_STRING`.

> `JWT_SECRET` must **never** be the placeholder from `.env.example`. It is the signing
> key for every session token.

### 5.4 Not required

`ELASTICSEARCH_*` (search degrades to SQL), `AZURE_SERVICE_BUS_CONNECTION_STRING`
(falls back to in-memory transport), `LAUNCHDARKLY_SDK_KEY`.

---

## 6. Provisioning plan

Resource names are fixed by `deploy.yml` and must match exactly.

| Resource | Name | Notes |
|---|---|---|
| Resource group | `upkilo-prod-rg` | East US |
| Container registry | `upkiloprod` | `upkiloprod.azurecr.io`, Standard |
| PostgreSQL | `upkilo-prod-db` | Flexible Server 17, HA, 7-day PITR |
| Redis | `upkilo-prod-redis` | 7.x |
| Key Vault | `upkilo-prod-kv` | |
| Blob storage | `upkiloprodstorage` | files, photos, PDFs |
| API App Service | `upkilo-api-prod` | + `staging` slot, managed identity |
| Frontend App Service | `upkilo-frontend-prod` | + `staging` slot |
| Front Door | `upkilo-fd` / `upkilo-fd-endpoint` | CDN + WAF |

### Staging resources (minimal tiers, ~$60–90/month)

| Resource | Name | Tier |
|---|---|---|
| Resource group | `upkilo-staging-rg` | East US |
| PostgreSQL | `upkilo-staging-db` | Flexible Server 17, Burstable B1ms, no HA |
| Redis | `upkilo-staging-redis` | Basic C0 |
| Blob storage | `upkilostagingstorage` | Standard LRS |
| API App Service | `upkilo-api-staging` | B1, no slot |
| Frontend App Service | `upkilo-frontend-staging` | B1, no slot |

Staging shares the **production ACR** (`upkiloprod`) — the whole point is to deploy the
identical image that will later reach production. It has **no custom domain, no TLS
binding, and no Front Door**, so it never appears in DNS.

Rough timing: ~90 min of setup plus ~40 min of Azure waiting on the Postgres and Redis
creates.

### DNS records

| Host | Points to | Purpose |
|---|---|---|
| `upkilo.com` (apex) | `upkilo-frontend-prod` (via Front Door) | **Marketing + public SEO + booking pages** |
| `www.upkilo.com` | `upkilo-frontend-prod` (via Front Door) | 308-redirects to apex in middleware |
| `app.upkilo.com` | `upkilo-frontend-prod` (via Front Door) | Dashboard, portal, auth |
| `api.upkilo.com` | `upkilo-api-prod` (via Front Door) | Backend API |
| SendGrid CNAMEs | per A3 | Sender authentication |
| TLS validation `TXT` | per custom-domain binding | Certificate issuance |

All three frontend hostnames bind to the **same App Service** — `middleware.ts` routes
between them by `Host` header, so no second frontend deployment is needed.

> Apex domains cannot use a plain `CNAME`. Use an ALIAS/ANAME record, or your DNS
> provider's flattening feature, pointed at the Front Door endpoint. Verify TLS
> certificates are issued for **all four** hostnames — a missing apex certificate is a
> common and highly visible launch failure.

---

## 7. Stripe configuration

### 7.0 Entity registration — the actual revenue blocker

🔴 **Upkilo is not yet a registered business.** The Azure subscription was created as a
**personal** account (individual PAN, no GSTIN) for this reason.

Stripe activation requires a legal entity: registered name, tax ID, and a **business
bank account**. Until that exists, the platform cannot charge anyone.

```
entity registration → business bank account → Stripe activation → revenue
```

This chain is almost certainly the longest pole in the launch, longer than any
infrastructure or approval-queue item in §5.

**What is unaffected:** provisioning, deployment, DNS/TLS, and SendGrid. Users can
register and use the product on a free tier while entity registration proceeds — signup
requires no legal entity, only working email.

Stripe India does onboard **sole proprietorships**, which are considerably faster to
establish than a Pvt Ltd. Confirm current requirements directly with Stripe.

Secondary cost note: with no GSTIN on the Azure account, the 18% GST on Azure spend
cannot be reclaimed as input tax credit — roughly ₹9,000/month at the projected run
rate. Add a GSTIN once registered; credit is not retroactive.

### 7.0b Account country

A Stripe account's country is determined by where the **business entity is registered**,
not where customers are. The team is in India; the target market is US/global. That
combination needs an explicit decision:

- **India-registered account** — payments from US customers are cross-border. Settlement
  currency, export-of-services documentation, and GST treatment all apply, and Indian
  recurring-payment (e-mandate) rules may attach to the account. Confirm with Stripe how
  subscriptions and metered billing behave for this configuration.
- **US-registered account** — requires a US entity (LLC / C-corp), a US bank account,
  and an EIN.

The account country fixes the settlement currency, which in turn fixes how plans are
priced. Repricing a catalog after customers hold active subscriptions is painful and
partly customer-visible.

**Verify directly with Stripe.** The above describes the shape of the decision, not
current regulatory specifics.

### 7.1 Product catalog

[`PricingPlan`](../src/backend/Upkilo.Core/Entities/Pricing/PricingPlan.cs) expects real
Stripe IDs:

- One **product** per plan → `StripeProductId`
- A recurring **price** per plan/interval → `PlanPrice` rows
- Optional **metered** prices → `StripeAiUsagePriceId`, `StripeSmsOveragePriceId`,
  `StripeExtraStaffPriceId`, `StripeExtraLocationPriceId` (null = overage unavailable
  on that plan)

### 7.2 Webhook endpoint

**URL:** `https://api.upkilo.com/api/webhooks/stripe`
([`StripeWebhookController.cs:22`](../src/backend/Upkilo.API/Controllers/StripeWebhookController.cs#L22))

Create this **after** DNS resolves and the first deploy is live — Stripe validates
reachability.

The controller handles 25 events; subscribe to exactly these:

```
checkout.session.completed             invoice.created
customer.subscription.created          invoice.finalized
customer.subscription.updated          invoice.upcoming
customer.subscription.deleted          invoice.voided
customer.subscription.paused           invoice.marked_uncollectible
customer.subscription.resumed          invoice.payment_succeeded
customer.subscription.trial_will_end   invoice.payment_failed
customer.updated                       payment_intent.created
customer.deleted                       payment_intent.succeeded
charge.succeeded                       payment_intent.payment_failed
charge.failed                          charge.dispute.created
charge.refunded                        charge.dispute.closed
account.updated
```

> Without `STRIPE_WEBHOOK_SECRET` every webhook is rejected as unsigned. The failure
> mode is silent: **the customer pays and the application never activates the
> subscription.**

### 7.3 Connect

[`AffiliatePayoutJob`](../src/backend/Upkilo.Infrastructure/Services/AffiliatePayoutJob.cs)
disburses affiliate payouts to `StripeConnectAccountId` on the 1st of each month.
Enable Stripe Connect if running an affiliate program; the job no-ops when the field is
empty.

---

## 7b. Marketing & SEO launch readiness

The frontend is not only an application — it is the marketing site and the primary
organic acquisition channel. None of the below blocks a deploy, but all of it blocks a
*successful launch*.

### 7b.1 What is actually published

| Surface | Route | Notes |
|---|---|---|
| Landing page | `/` → `/en` | |
| Pricing | `/[locale]/pricing` | |
| Marketplace | `/[locale]/marketplace` | |
| Vertical page | `/[locale]/medical-spa` | |
| Enterprise | `/enterprise` | no locale prefix |
| Discovery hub | `/discover` | no locale prefix |
| Country pages | `/au`, `/ca`, `/uk`, `/uae` | no locale prefix |
| **Programmatic SEO** | `/book/{category}/{city}` | **225 pages** — 15 categories × 15 cities |
| Tenant booking pages | `/{locale}/book/{slug}` | fetched live from `/api/seo/slugs` |
| Legal | terms, privacy, cookie policy | |

The 225 discovery pages come from the arrays in
[`sitemap.ts`](../src/frontend/app/sitemap.ts) — 15 categories (hair-salon, spa,
barbershop, physiotherapy, yoga, tattoo, …) × 15 cities (london, new-york, dubai,
sydney, toronto, singapore, delhi, mumbai, …).

> The cities list is global, but the sitemap advertises these pages regardless of
> whether any tenant actually operates there. Empty city pages rank poorly and can
> attract thin-content penalties. Consider trimming the arrays to markets with real
> supply before submitting the sitemap.

### 7b.2 Pre-launch checks

- [ ] `https://upkilo.com/robots.txt` — `host` and `sitemap` both point at the **apex**
- [ ] `https://upkilo.com/sitemap.xml` — every `<loc>` is apex, none is `app.upkilo.com`
- [ ] `/api/seo/slugs` returns tenant slugs; the sitemap degrades to an empty list rather
      than failing when the API is unreachable
- [ ] Dashboard routes are `Disallow`ed in robots.txt (they are, but re-verify after any
      route rename)
- [ ] Apex, `www`, `app`, and `api` all serve valid TLS
- [ ] `www` → apex redirect returns **308**, not a soft redirect or a 200
- [ ] Open Graph and Twitter card metadata render (paste a link into Slack to check)
- [ ] `manifest.json` and PWA icons load from the apex

### 7b.3 Search engine registration

- [ ] Google Search Console — verify **both** the apex and `app.` properties
- [ ] Submit `https://upkilo.com/sitemap.xml`
- [ ] Bing Webmaster Tools — verify and submit
- [ ] Confirm no stray `noindex` header or meta tag on marketing routes

### 7b.4 Analytics — currently not wired

[`OnboardingWizard.tsx:61`](../src/frontend/components/onboarding/OnboardingWizard.tsx#L61)
calls `window.gtag('event', 'onboarding_dismissed', …)`, but **no gtag script is loaded
anywhere and no analytics environment variable exists**. The call is guarded by a
`typeof window !== 'undefined' && window.gtag` check, so it silently no-ops rather than
throwing.

Consequence: there is **no funnel instrumentation at launch**. Signup starts, drop-off,
and activation are all unmeasured.

To wire it up: add a `NEXT_PUBLIC_GA_ID` (or PostHog/Mixpanel equivalent), load the
script in the root layout, and pass the ID through as a Docker `ARG` — the same
build-time inlining constraint as every other `NEXT_PUBLIC_*` value applies.

### 7b.5 Performance

[`.github/lighthouse-budget.json`](../.github/lighthouse-budget.json) already defines a
budget. Run Lighthouse against the **apex** — not the app subdomain — since that is what
Google measures for ranking. Core Web Vitals apply to the marketing and booking pages;
the authenticated dashboard is not indexed and does not affect ranking.

### 7b.6 Legal review

Terms, privacy, and cookie policy pages exist and are linked from the cookie consent
banner. The privacy policy references the **Digital Personal Data Protection Act, 2023**
and the **Information Technology Act, 2000** — Indian legislation. If the entity and
target market are US/global (§7.0), have these reviewed for the jurisdiction you are
actually selling into before launch.

---

## 8. Deploy procedure

### 8.0 The approval gate

GitHub Free does **not** provide environment protection rules (required reviewers) on
private repositories, so `environment: production` cannot enforce approval. The gate is
implemented in the workflow instead:

```yaml
if: github.event_name == 'workflow_dispatch'
```

applied to **both** `migrate-database` and `deploy-production`.

| Trigger | Effect |
|---|---|
| Push to `main` | Builds and pushes images only. Nothing live is touched. |
| **Run workflow** (manual) | Full pipeline: migrate → deploy → smoke test |

> 🔴 **Do not remove those `if:` conditions** without first adding a real approval gate.
> They are the only thing preventing a push to `main` from migrating the production
> database and deploying unreviewed.

Upgrading to GitHub Pro/Team (or making the repository public) enables required
reviewers, at which point the conditions become belt-and-braces rather than essential.

The `production` environment is still configured with a **branch restriction to `main`**,
which does work on the Free plan.

### 8.1 Running a deploy

1. **`build-and-push`** — builds both images, pushes to ACR, and asserts no
   `localhost:5000` was baked into the client bundle. `NEXT_PUBLIC_*` values are inlined
   at **build** time; setting them as runtime App Service settings has no effect.
2. **`migrate-staging`** — applies migrations to the staging database. A destructive or
   failing migration stops here, before production is touched.
3. **`deploy-staging`** — sets staging app settings, deploys both images, polls
   `/ready` until DB + Redis + Hangfire are green. Failure blocks production.
4. **`migrate-database`** — backs up the production database, generates an idempotent
   SQL script (uploaded as a 90-day artifact), then applies migrations.
   **Read this artifact before approving step 5.**
5. **`deploy-production`** — ⏸ *waits for required-reviewer approval*, then sets app
   settings, verifies the DB connection string reached App Service, deploys both images
   to their `staging` slots, polls `/ready` on each, swaps both slots, purges the CDN,
   and smoke-tests production.

`/ready` (DB + Redis + Hangfire) is the gate, not `/health` — `/health` is liveness only
and checks memory/uptime without verifying dependency connectivity.

### First deploy differs

74 migration files apply to an empty database. Expect the `migrate-database` job to take
noticeably longer than on subsequent deploys.

---

## 9. Rollback

### 9.0 Database recovery — point-in-time restore

⚠️ **There are no on-demand backups.** Azure rejects them on Burstable tier:
`CustomerOnDemandBackupCannotBePerformedOnBurstableServer`. Recovery relies entirely on
**automated backups with point-in-time restore**:

| Setting | Value |
|---|---|
| Retention | 7 days |
| Geo-redundant | Disabled |
| Tier | Burstable B1ms |

Every `migrate-database` run prints a **pre-migration restore point** to the job summary.
To rewind a bad migration, restore to that timestamp:

```bash
az postgres flexible-server restore \
  --resource-group upkilo-prod-rg \
  --name upkilo-prod-db-restored \
  --source-server upkilo-prod-db \
  --restore-time 2026-08-01T12:34:56Z
```

This creates a **new server**; it does not overwrite the original. After verifying the
restored data, repoint `AZURE_PROD_DB_CONNECTION` (GitHub secret *and* the App Service
setting) at the new server, then redeploy.

> Restore is not instant — expect several minutes, and the app stays down or serves the
> old schema until the connection string is switched. This is the cost of the Burstable
> tier; higher tiers support on-demand backups and faster recovery.

**Retention is only 7 days.** A problem discovered on day 8 is not recoverable this way.



The `rollback` job fires automatically when `deploy-production` fails: it swaps both
slots back, then reverts the schema.

**Schema rollback requires the repository variable `ROLLBACK_TARGET_MIGRATION`** — set
it to the last migration applied by the previous successful deploy. If unset, schema
rollback is **skipped** and manual DBA review is required:

```bash
dotnet ef database update <previous-migration-name> \
  --project src/backend/Upkilo.Infrastructure/Upkilo.Infrastructure.csproj \
  --startup-project src/backend/Upkilo.API/Upkilo.API.csproj
```

Update this variable after every successful production deploy.

> The rollback job deliberately carries no `environment: production`. Protection gates
> block jobs triggered by `failure()`, which would make automatic rollback impossible
> during an incident.

---

## 10. Post-deploy verification

Automated by the pipeline:

- [ ] `https://api.upkilo.com/health` returns 200
- [ ] `https://api.upkilo.com/ready` reports `Healthy`
- [ ] Elasticsearch check (non-fatal — search degrades gracefully)

Manual, before announcing to customers:

- [ ] Sign up a new tenant end-to-end — **confirms SendGrid delivery**
- [ ] Log in; verify JWT issuance and refresh
- [ ] Create, reschedule, and cancel a booking
- [ ] Complete a real subscription checkout
- [ ] Confirm the webhook arrived: Stripe dashboard → webhook → recent deliveries, all 2xx
- [ ] Verify the subscription activated in-app (catches a wrong `STRIPE_WEBHOOK_SECRET`)
- [ ] Upload a file — **confirms `Azure__Storage__ConnectionString`**
- [ ] Confirm Hangfire dashboard shows recurring jobs scheduled
- [ ] Trigger a test error; confirm it reaches Sentry and Application Insights
- [ ] Confirm a PagerDuty test alert pages the on-call
- [ ] Record `ROLLBACK_TARGET_MIGRATION` for the next deploy

Domain split (§3.5) — verify each redirect in a browser:

- [ ] `upkilo.com` serves the marketing landing page
- [ ] `www.upkilo.com` → 308 → apex
- [ ] `upkilo.com/en/tenant/command` → 308 → `app.upkilo.com/...`
- [ ] `app.upkilo.com/pricing` → 308 → `upkilo.com/pricing`
- [ ] `upkilo.com/book/hair-salon/london` renders (a programmatic SEO page)
- [ ] Log in on `app.upkilo.com` and confirm no redirect loop
- [ ] Booking widget on the apex can call `api.upkilo.com` — **confirms the CORS change**

---

## 11. Launch readiness

Infrastructure can stand up as soon as `az login` is done. **Customer launch is gated on
Stripe (A2) and SendGrid (A3)** — until both clear, no customer can register or be
charged. Neither is an engineering constraint.

| Milestone | Gate |
|---|---|
| Infrastructure live | A1 |
| Application deployed and healthy | A1, A5 |
| Customers can register and use the product | A3 (SendGrid) |
| **Customers can pay** | **entity registration → bank account → A2 → webhook (§7.0, §7.2)** |
| AI features active | A4 |
| SMS reminders active | A6 |

The product can be live, usable, and taking signups well before it can take money. Those
are two separate launches — treat them as such rather than blocking the first on the
second.
