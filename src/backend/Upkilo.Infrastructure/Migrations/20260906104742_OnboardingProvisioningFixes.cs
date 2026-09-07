using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Upkilo.Infrastructure.Migrations
{
    /// <summary>
    /// Repairs the three things tenant provisioning left unset, for tenants that already exist.
    /// AuthService now writes all of them at signup, but that only helps registrations from this
    /// deploy forward — every tenant created before it is still carrying the broken state.
    ///
    /// Also promotes the OnboardingProgress.TenantId index to unique, which needs the duplicate
    /// rows cleared first or the index creation fails.
    /// </summary>
    public partial class OnboardingProvisioningFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Collapse duplicate onboarding progress rows before the unique index goes on.
            //    GET /onboarding/checklist did get-or-create with no constraint behind it, so two
            //    concurrent first page loads could each insert a row. Keep the earliest — it is
            //    the one whose CreatedAt is closest to the tenant's real start, and the one the
            //    drip job's 7-day window was reading.
            migrationBuilder.Sql(@"
                DELETE FROM ""OnboardingProgress"" a
                USING ""OnboardingProgress"" b
                WHERE a.""TenantId"" = b.""TenantId""
                  AND (a.""CreatedAt"" > b.""CreatedAt""
                       OR (a.""CreatedAt"" = b.""CreatedAt"" AND a.""Id"" > b.""Id""));
            ");

            migrationBuilder.DropIndex(
                name: "IX_OnboardingProgress_TenantId",
                table: "OnboardingProgress");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingProgress_TenantId",
                table: "OnboardingProgress",
                column: "TenantId",
                unique: true);

            // 2. Backfill Tenants.Email from the tenant's owning user.
            //    Registration only ever set User.Email, so this column is null for essentially
            //    every existing tenant — and OnboardingDripJob skips any tenant without one,
            //    which is why the 7-day nudge has never reached anybody. Prefers Owner over
            //    Admin, then oldest, to pick the account that created the tenant.
            migrationBuilder.Sql(@"
                UPDATE ""Tenants"" t
                SET ""Email"" = sub.""Email""
                FROM (
                    SELECT DISTINCT ON (u.""TenantId"")
                           u.""TenantId"", u.""Email""
                    FROM ""Users"" u
                    WHERE u.""IsDeleted"" = false
                      AND u.""Email"" IS NOT NULL
                      AND u.""Email"" <> ''
                      AND u.""Role"" IN (0, 1)
                    ORDER BY u.""TenantId"", u.""Role"" ASC, u.""CreatedAt"" ASC
                ) AS sub
                WHERE t.""Id"" = sub.""TenantId""
                  AND (t.""Email"" IS NULL OR t.""Email"" = '');
            ");

            // 3. Backfill the denormalised plan columns from the subscription that already exists.
            //    Tenant.SubscriptionTier defaults to Starter (enum ordinal 1) and registration
            //    never overwrote it, so Free tenants have been resolving as Starter in
            //    AiModelResolver, JobQuotaService and TenantRateLimitMiddleware. Only rows with a
            //    null PricingPlanId are touched: a non-null value means SubscriptionService
            //    already synced this tenant and is the authority.
            //
            //    Tier ordinals: Free=0, Starter=1, Growth=2, Business=3, Agency=4, Enterprise=5.
            //    The name->tier mapping mirrors SubscriptionTierMap.FromPlanName, including its
            //    legacy aliases and its deliberate "unknown falls back to Free" rule.
            migrationBuilder.Sql(@"
                UPDATE ""Tenants"" t
                SET ""PricingPlanId"" = sub.""PricingPlanId"",
                    ""SubscriptionTier"" = CASE lower(trim(sub.""PlanName""))
                        WHEN 'free'         THEN 0
                        WHEN 'starter'      THEN 1
                        WHEN 'growth'       THEN 2
                        WHEN 'professional' THEN 2
                        WHEN 'business'     THEN 2
                        WHEN 'agency'       THEN 2
                        WHEN 'enterprise'   THEN 5
                        ELSE 0
                    END
                FROM (
                    SELECT DISTINCT ON (s.""TenantId"")
                           s.""TenantId"", s.""PricingPlanId"", p.""Name"" AS ""PlanName""
                    FROM ""Subscriptions"" s
                    JOIN ""PricingPlans"" p ON p.""Id"" = s.""PricingPlanId""
                    WHERE s.""IsDeleted"" = false
                      AND s.""PricingPlanId"" IS NOT NULL
                    ORDER BY s.""TenantId"", s.""CreatedAt"" DESC
                ) AS sub
                WHERE t.""Id"" = sub.""TenantId""
                  AND t.""PricingPlanId"" IS NULL;
            ");

            // 3b. Promote each tenant's founding user to Owner.
            //     Signup assigned Admin and nothing else ever created an Owner, so every
            //     self-service tenant has no owner at all. BillingController is
            //     [Authorize(Roles = "Owner")] for the whole controller, so those founders cannot
            //     view plans, open the billing portal or create a checkout session — they are
            //     unable to pay us. The Stripe Connect endpoints on PaymentsController are
            //     Owner-only too, which is the "Connect Payment Method" onboarding step.
            //
            //     Only tenants with NO existing Owner are touched, and only their earliest Admin
            //     is promoted — this grants nobody access to a tenant they were not already
            //     administering.
            migrationBuilder.Sql(@"
                UPDATE ""Users"" u
                SET ""Role"" = 0
                FROM (
                    SELECT DISTINCT ON (a.""TenantId"") a.""Id""
                    FROM ""Users"" a
                    WHERE a.""IsDeleted"" = false
                      AND a.""Role"" = 1
                      AND NOT EXISTS (
                          SELECT 1 FROM ""Users"" o
                          WHERE o.""TenantId"" = a.""TenantId""
                            AND o.""IsDeleted"" = false
                            AND o.""Role"" = 0
                      )
                    ORDER BY a.""TenantId"", a.""CreatedAt"" ASC
                ) AS founder
                WHERE u.""Id"" = founder.""Id"";
            ");

            // 4. Create the missing onboarding progress rows.
            //    The row used to appear only when someone opened the dashboard checklist, so a
            //    tenant who signed up and never returned had none — precisely the tenant the drip
            //    email targets. CreatedAt is set to the tenant's own CreatedAt so the job's
            //    "signed up 7-14 days ago" window means what it says rather than "first opened
            //    the dashboard 7-14 days ago".
            migrationBuilder.Sql(@"
                INSERT INTO ""OnboardingProgress"" (
                    ""Id"", ""TenantId"", ""UserId"",
                    ""BusinessProfileCompleted"", ""WorkingHoursCompleted"", ""ServicesAdded"",
                    ""StaffAdded"", ""BookingPageCustomized"", ""PaymentSetupCompleted"",
                    ""FirstBookingCreated"", ""ClientsImported"", ""IsDismissed"",
                    ""CreatedAt"", ""UpdatedAt"", ""IsDeleted"", ""Version""
                )
                SELECT gen_random_uuid(), t.""Id"", COALESCE(u.""Id"", '00000000-0000-0000-0000-000000000000'),
                       false, false, false,
                       false, false, false,
                       false, false, false,
                       t.""CreatedAt"", now() AT TIME ZONE 'utc', false, 1
                FROM ""Tenants"" t
                LEFT JOIN LATERAL (
                    SELECT u2.""Id""
                    FROM ""Users"" u2
                    WHERE u2.""TenantId"" = t.""Id""
                      AND u2.""IsDeleted"" = false
                      AND u2.""Role"" IN (0, 1)
                    ORDER BY u2.""Role"" ASC, u2.""CreatedAt"" ASC
                    LIMIT 1
                ) u ON true
                WHERE t.""IsDeleted"" = false
                  AND NOT EXISTS (
                      SELECT 1 FROM ""OnboardingProgress"" o WHERE o.""TenantId"" = t.""Id""
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only the index is reversible. The backfills wrote real values into columns that
            // were empty or wrong; undoing them would restore the defect, not the data, and
            // there is no record of which rows were blank beforehand.
            migrationBuilder.DropIndex(
                name: "IX_OnboardingProgress_TenantId",
                table: "OnboardingProgress");

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingProgress_TenantId",
                table: "OnboardingProgress",
                column: "TenantId");
        }
    }
}
