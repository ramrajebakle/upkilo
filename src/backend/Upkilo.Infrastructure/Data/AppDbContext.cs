using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Upkilo.Core.Entities;
using Upkilo.Core.Interfaces;
using WorkflowEntity = Upkilo.Core.Entities.Workflow;

namespace Upkilo.Infrastructure.Data;

/// <summary>
/// Main database context with multi-tenant support
/// </summary>
public class AppDbContext : DbContext
{
    private readonly Guid? _tenantId;
    private readonly IDbConnectionSelector _connectionSelector;
    private readonly ReadWriteInterceptor? _readWriteInterceptor;
    private readonly SlowQueryInterceptor? _slowQueryInterceptor;
    private readonly FailoverInterceptor? _failoverInterceptor;
    private readonly SearchSyncInterceptor? _searchSyncInterceptor;
    private readonly AuditLogInterceptor? _auditLogInterceptor;
    private readonly DomainEventInterceptor? _domainEventInterceptor;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ITenantProvider? tenantProvider = null,
        IDbConnectionSelector? connectionSelector = null,
        ReadWriteInterceptor? readWriteInterceptor = null,
        SlowQueryInterceptor? slowQueryInterceptor = null,
        FailoverInterceptor? failoverInterceptor = null,
        SearchSyncInterceptor? searchSyncInterceptor = null,
        AuditLogInterceptor? auditLogInterceptor = null,
        DomainEventInterceptor? domainEventInterceptor = null) : base(options)
    {
        _tenantId = tenantProvider?.GetTenantId();
        _connectionSelector = connectionSelector!;
        _readWriteInterceptor = readWriteInterceptor;
        _slowQueryInterceptor = slowQueryInterceptor;
        _failoverInterceptor = failoverInterceptor;
        _searchSyncInterceptor = searchSyncInterceptor;
        _auditLogInterceptor = auditLogInterceptor;
        _domainEventInterceptor = domainEventInterceptor;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // NOTE: Do NOT reconfigure UseNpgsql here.
        // Program.cs configures this DbContext with an NpgsqlDataSource that has
        // EnableDynamicJson() enabled, which is required for Dictionary<string, object>
        // JSONB columns. Calling UseNpgsql again with a raw connection string would
        // create a new data source without that setting, causing serialization failures.

        var interceptors = new List<Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor>();
        if (_readWriteInterceptor != null) interceptors.Add(_readWriteInterceptor);
        if (_slowQueryInterceptor != null) interceptors.Add(_slowQueryInterceptor);
        if (_failoverInterceptor != null) interceptors.Add(_failoverInterceptor);
        if (_searchSyncInterceptor != null) interceptors.Add(_searchSyncInterceptor);
        if (_auditLogInterceptor != null) interceptors.Add(_auditLogInterceptor);
        if (_domainEventInterceptor != null) interceptors.Add(_domainEventInterceptor);

        if (interceptors.Any())
        {
            optionsBuilder.AddInterceptors(interceptors);
        }

        base.OnConfiguring(optionsBuilder);
    }

    // Core
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<SupportTicketComment> SupportTicketComments => Set<SupportTicketComment>();
    public DbSet<RecurringPattern> RecurringPatterns => Set<RecurringPattern>();
    public DbSet<CustomFieldDefinition> CustomFieldDefinitions { get; set; }
    public DbSet<WaitlistEntry> WaitlistEntries { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceItem> InvoiceItems { get; set; }
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<GiftCertificate> GiftCertificates => Set<GiftCertificate>();
    public DbSet<GiftCertificateRedemption> GiftCertificateRedemptions => Set<GiftCertificateRedemption>();
    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();
    public DbSet<ClientMembership> ClientMemberships => Set<ClientMembership>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();

    // Booking
    public DbSet<Service> Services => Set<Service>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<StaffService> StaffServices => Set<StaffService>();
    public DbSet<ServiceBundleItem> ServiceBundleItems => Set<ServiceBundleItem>();
    public DbSet<ServiceUpsell> ServiceUpsells => Set<ServiceUpsell>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<WorkingHours> StaffWorkingHours => Set<WorkingHours>();
    public DbSet<ScheduleException> StaffExceptions => Set<ScheduleException>();
    public DbSet<SlotHold> SlotHolds => Set<SlotHold>();
    public DbSet<AvailabilityCache> AvailabilityCaches => Set<AvailabilityCache>();
    public DbSet<CalendarSyncToken> CalendarSyncTokens => Set<CalendarSyncToken>();
    public DbSet<StaffShift> StaffShifts => Set<StaffShift>();
    public DbSet<StaffClockIn> StaffClockIns => Set<StaffClockIn>();
    public DbSet<StaffShiftSwap> StaffShiftSwaps => Set<StaffShiftSwap>();
    public DbSet<StaffCommission> StaffCommissions => Set<StaffCommission>();

    // CRM
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Upkilo.Core.Entities.LoyaltyTransaction> LoyaltyTransactions => Set<Upkilo.Core.Entities.LoyaltyTransaction>();
    public DbSet<ClientReferral> ClientReferrals => Set<ClientReferral>();
    public DbSet<ClientNote> ClientNotes => Set<ClientNote>();
    public DbSet<CommunicationLog> CommunicationLogs => Set<CommunicationLog>();
    public DbSet<ClientPhoto> ClientPhotos => Set<ClientPhoto>();
    public DbSet<Household> Households => Set<Household>();
    public DbSet<UserActivityLog> UserActivityLogs => Set<UserActivityLog>();
    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();
    public DbSet<ClientContraindication> ClientContraindications => Set<ClientContraindication>();

    // Marketing
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignAnalytics> CampaignAnalytics => Set<CampaignAnalytics>();
    public DbSet<MarketingAutoResponder> MarketingAutoResponders => Set<MarketingAutoResponder>();
    public DbSet<AdAccount> AdAccounts => Set<AdAccount>();
    public DbSet<AdCampaign> AdCampaigns => Set<AdCampaign>();
    public DbSet<LandingPage> LandingPages => Set<LandingPage>();
    public DbSet<LeadCapture> LeadCaptures => Set<LeadCapture>();
    public DbSet<ConversionEvent> ConversionEvents => Set<ConversionEvent>();
    public DbSet<BusinessListing> BusinessListings => Set<BusinessListing>();
    public DbSet<ReferralRecord> ReferralRecords => Set<ReferralRecord>();
    public DbSet<PartnerAccount> PartnerAccounts => Set<PartnerAccount>();
    public DbSet<AffiliateCommission> AffiliateCommissions => Set<AffiliateCommission>();
    public DbSet<AffiliatePayout> AffiliatePayouts => Set<AffiliatePayout>();

    // Membership Content & Progress
    public DbSet<MembershipContent> MembershipContents => Set<MembershipContent>();
    public DbSet<MembershipModule> MembershipModules => Set<MembershipModule>();
    public DbSet<MembershipLesson> MembershipLessons => Set<MembershipLesson>();
    public DbSet<ClientContentProgress> ClientContentProgresses => Set<ClientContentProgress>();

    public DbSet<DataProcessingLog> DataProcessingLogs => Set<DataProcessingLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    // Content & SEO
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();

    // Marketing Automation
    public DbSet<MarketingConfig> MarketingConfigs => Set<MarketingConfig>();
    public DbSet<SeoAnalysis> SeoAnalyses => Set<SeoAnalysis>();
    public DbSet<GeneratedContent> GeneratedContents => Set<GeneratedContent>();
    public DbSet<ContentCalendar> ContentCalendars => Set<ContentCalendar>();
    public DbSet<SocialPost> SocialPosts => Set<SocialPost>();
    public DbSet<MarketingAnalytics> MarketingAnalyticsRecords => Set<MarketingAnalytics>();
    public DbSet<MarketingForecast> MarketingForecasts => Set<MarketingForecast>();
    public DbSet<AgentAction> AgentActions => Set<AgentAction>();
    public DbSet<IndexingStatus> IndexingStatuses => Set<IndexingStatus>();
    public DbSet<MarketingTemplate> MarketingTemplates => Set<MarketingTemplate>();
    public DbSet<ConversionAnalysis> ConversionAnalyses => Set<ConversionAnalysis>();

    // Operations
    public DbSet<TenantQuota> TenantQuotas => Set<TenantQuota>();
    public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs => Set<WebhookDeliveryLog>();
    public DbSet<AdminImpersonationLog> AdminImpersonationLogs => Set<AdminImpersonationLog>();

    // Phase 7 — Forms, Sales Pipeline, Reviews, Webhooks, Reports
    public DbSet<CustomFieldValue> CustomFieldValues => Set<CustomFieldValue>();
    public DbSet<FormDefinition> FormDefinitions => Set<FormDefinition>();
    public DbSet<FormField> FormFields => Set<FormField>();
    public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();
    public DbSet<SalesPipeline> SalesPipelines => Set<SalesPipeline>();
    public DbSet<PipelineStage> PipelineStages => Set<PipelineStage>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<DealActivity> DealActivities => Set<DealActivity>();
    public DbSet<ExternalReview> ExternalReviews => Set<ExternalReview>();
    public DbSet<ReviewRequest> ReviewRequests => Set<ReviewRequest>();
    public DbSet<GdprConsent> GdprConsents => Set<GdprConsent>();
    public DbSet<UserConsent> UserConsents => Set<UserConsent>();
    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();
    public DbSet<ClientPortalConfig> ClientPortalConfigs => Set<ClientPortalConfig>();

    // Phase 7.12-7.15 — White-Label, Currency, Packages
    public DbSet<WhiteLabelConfig> WhiteLabelConfigs => Set<WhiteLabelConfig>();
    public DbSet<CurrencyConfig> CurrencyConfigs => Set<CurrencyConfig>();
    public DbSet<ServicePackage> ServicePackages => Set<ServicePackage>();

    // Phase 8 — Advanced Features
    public DbSet<AdvancedFeatures> AdvancedFeatures { get; set; }
    public DbSet<VoiceCall> VoiceCalls => Set<VoiceCall>();
    public DbSet<PluginDefinition> PluginDefinitions => Set<PluginDefinition>();
    public DbSet<PluginInstallation> PluginInstallations => Set<PluginInstallation>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<SsoConfig> SsoConfigs => Set<SsoConfig>();
    public DbSet<PredictiveScore> PredictiveScores => Set<PredictiveScore>();

    // Phase 9-10 — Micro Features
    public DbSet<Product> Products => Set<Product>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<LoyaltyProgram> LoyaltyPrograms => Set<LoyaltyProgram>();
    public DbSet<LoyaltyBalance> LoyaltyBalances => Set<LoyaltyBalance>();
    public DbSet<LoyaltyReward> LoyaltyRewards => Set<LoyaltyReward>();
    public DbSet<DigitalWaiver> DigitalWaivers => Set<DigitalWaiver>();
    public DbSet<WaiverSignature> WaiverSignatures => Set<WaiverSignature>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<PromoRedemption> PromoRedemptions => Set<PromoRedemption>();
    public DbSet<ClassDefinition> ClassDefinitions => Set<ClassDefinition>();

    // Remaining Infrastructure — Multi-Region, Chat, QR, 2FA, Audit, Import, Legal, i18n, Tenant Mgmt
    public DbSet<RegionConfig> RegionConfigs => Set<RegionConfig>();
    public DbSet<ChatWidget> ChatWidgets => Set<ChatWidget>();
    public DbSet<ChatVisitor> ChatVisitors => Set<ChatVisitor>();
    public DbSet<BookingCheckIn> BookingCheckIns => Set<BookingCheckIn>();
    public DbSet<TwoFactorConfig> TwoFactorConfigs => Set<TwoFactorConfig>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<DataImportJob> DataImportJobs => Set<DataImportJob>();
    public DbSet<LegalDocument> LegalDocuments => Set<LegalDocument>();
    public DbSet<TranslationEntry> TranslationEntries => Set<TranslationEntry>();
    public DbSet<TenantManagement> TenantManagements => Set<TenantManagement>();
    public DbSet<DuplicateClientMatch> DuplicateClientMatches => Set<DuplicateClientMatch>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

    // Missing entity registrations — ImportJob and SetupProgress were defined in Core.Entities
    // but not registered here, causing InvalidOperationException in SetupWizardService and ImportService.
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<SetupProgress> SetupProgresses => Set<SetupProgress>();

    // Final — SMS A2P 10DLC, WhatsApp, Tips, Waitlist
    public DbSet<SmsA2PBrand> SmsA2PBrands => Set<SmsA2PBrand>();
    public DbSet<SmsCampaignRegistration> SmsCampaignRegistrations => Set<SmsCampaignRegistration>();
    public DbSet<SmsConsent> SmsConsents => Set<SmsConsent>();
    public DbSet<WhatsAppConfig> WhatsAppConfigs => Set<WhatsAppConfig>();
    public DbSet<WhatsAppTemplate> WhatsAppTemplates => Set<WhatsAppTemplate>();
    public DbSet<StaffTip> StaffTips => Set<StaffTip>();
    public DbSet<WaitlistConfig> WaitlistConfigs => Set<WaitlistConfig>();

    // Final — PWA, Accessibility, API Standards, DB Ops, Deployments, Incidents, UI
    public DbSet<OfflineSyncQueue> OfflineSyncQueues => Set<OfflineSyncQueue>();
    public DbSet<AccessibilityScan> AccessibilityScans => Set<AccessibilityScan>();
    public DbSet<ApiErrorCode> ApiErrorCodes => Set<ApiErrorCode>();
    public DbSet<MigrationRecord> MigrationRecords => Set<MigrationRecord>();
    public DbSet<DeploymentRecord> DeploymentRecords => Set<DeploymentRecord>();
    public DbSet<IncidentRecord> IncidentRecords => Set<IncidentRecord>();
    public DbSet<UserUiPreference> UserUiPreferences => Set<UserUiPreference>();

    // All Remaining — Scaling, Templates, Funnels, Integrations, Warehouse, Compliance
    public DbSet<ScalingPolicy> ScalingPolicies => Set<ScalingPolicy>();
    public DbSet<HangfireWorkerNode> HangfireWorkerNodes => Set<HangfireWorkerNode>();
    public DbSet<SectionTemplate> SectionTemplates => Set<SectionTemplate>();
    public DbSet<PageAnalytics> PageAnalyticsRecords => Set<PageAnalytics>();
    public DbSet<FunnelStep> FunnelSteps => Set<FunnelStep>();
    public DbSet<GatedContent> GatedContents => Set<GatedContent>();
    public DbSet<MemberProgress> MemberProgresses => Set<MemberProgress>();
    public DbSet<PushNotificationToken> PushNotificationTokens => Set<PushNotificationToken>();
    public DbSet<WebPushSubscription> WebPushSubscriptions => Set<WebPushSubscription>();
    public DbSet<AccountingSyncConfig> AccountingSyncConfigs => Set<AccountingSyncConfig>();
    public DbSet<EmailMarketingSync> EmailMarketingSyncs => Set<EmailMarketingSync>();
    public DbSet<CalendarSync> CalendarSyncs => Set<CalendarSync>();
    public DbSet<DataWarehouseExport> DataWarehouseExports => Set<DataWarehouseExport>();
    public DbSet<SdkRelease> SdkReleases => Set<SdkRelease>();
    public DbSet<SandboxEnvironment> SandboxEnvironments => Set<SandboxEnvironment>();
    public DbSet<ShippingProvider> ShippingProviders => Set<ShippingProvider>();
    public DbSet<HipaaConfig> HipaaConfigs => Set<HipaaConfig>();
    public DbSet<Soc2Evidence> Soc2Evidences => Set<Soc2Evidence>();
    // Convenience aliases for services
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<StaffMember> Staff => Set<StaffMember>();
    public DbSet<WebhookDeliveryLog> WebhookEndpoints => Set<WebhookDeliveryLog>();

    // Payments
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CreditAccount> CreditAccounts => Set<CreditAccount>();
    public DbSet<CreditAccountTransaction> CreditAccountTransactions => Set<CreditAccountTransaction>();
    public DbSet<SplitPayment> SplitPayments => Set<SplitPayment>();

    // Dashboard Read Models
    public DbSet<TenantDashboardStats> TenantDashboardStats => Set<TenantDashboardStats>();
    public DbSet<TenantDailyMetric> TenantDailyMetrics => Set<TenantDailyMetric>();

    // OAuth2 Developer Platform
    public DbSet<OAuthApp> OAuthApps => Set<OAuthApp>();
    public DbSet<OAuthToken> OAuthTokens => Set<OAuthToken>();

    // Search Enhancements
    public DbSet<SavedSearchFilter> SavedSearchFilters => Set<SavedSearchFilter>();
    public DbSet<RecentSearch> RecentSearches => Set<RecentSearch>();

    // Automation
    public DbSet<WorkflowEntity> Workflows => Set<WorkflowEntity>();
    public DbSet<WorkflowTemplate> WorkflowTemplates => Set<WorkflowTemplate>();
    public DbSet<WorkflowExecution> WorkflowExecutions => Set<WorkflowExecution>();
    public DbSet<WorkflowExecutionLog> WorkflowExecutionLogs => Set<WorkflowExecutionLog>();

    // Platform Features
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<CustomDomain> CustomDomains => Set<CustomDomain>();
    public DbSet<Webhook> Webhooks => Set<Webhook>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<User2FA> User2FAs => Set<User2FA>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<Upkilo.Infrastructure.Services.UserDevice> UserDevices => Set<Upkilo.Infrastructure.Services.UserDevice>();
    public DbSet<UserPasskey> UserPasskeys => Set<UserPasskey>();
    public DbSet<UserTourProgress> UserTourProgresses => Set<UserTourProgress>();
    public DbSet<AIUsageLog> AIUsageLogs => Set<AIUsageLog>();
    public DbSet<AIDecisionLog> AIDecisionLogs => Set<AIDecisionLog>();
    public DbSet<AIEscalation> AIEscalations => Set<AIEscalation>();
    public DbSet<AIConversation> AIConversations => Set<AIConversation>();
    public DbSet<AIMessage> AIMessages => Set<AIMessage>();
    public DbSet<AIKnowledgeBase> AIKnowledgeBases => Set<AIKnowledgeBase>();
    public DbSet<AIAgentConfig> AIAgentConfigs => Set<AIAgentConfig>();
    public DbSet<AIDiscoveryReport> AIDiscoveryReports => Set<AIDiscoveryReport>();
    public DbSet<PromptVersion> PromptVersions => Set<PromptVersion>();
    public DbSet<StripePayout> StripePayouts => Set<StripePayout>();
    public DbSet<CrmTask> CrmTasks => Set<CrmTask>();
    public DbSet<SamlConfiguration> SamlConfigurations => Set<SamlConfiguration>();

    public DbSet<ProcessedWebhook> ProcessedWebhooks => Set<ProcessedWebhook>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<MagicLinkToken> MagicLinkTokens => Set<MagicLinkToken>();

    // Subscriptions & Pricing
    public DbSet<PricingPlan> PricingPlans => Set<PricingPlan>();
    public DbSet<PlanPrice> PlanPrices => Set<PlanPrice>();
    public DbSet<PricingFeature> PricingFeatures => Set<PricingFeature>();
    public DbSet<PlanFeatureMapping> PlanFeatureMappings => Set<PlanFeatureMapping>();
    public DbSet<PricingAddOn> PricingAddOns => Set<PricingAddOn>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<ServiceVehiclePrice> ServiceVehiclePrices => Set<ServiceVehiclePrice>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<PromoRedemption> PromotionRedemptions => Set<PromoRedemption>();
    public DbSet<PlatformDiscount> PlatformDiscounts => Set<PlatformDiscount>();

    // Phase 1 - Database Expansion
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<TenantStatusHistory> TenantStatusHistories => Set<TenantStatusHistory>();
    public DbSet<LegalAgreement> LegalAgreements => Set<LegalAgreement>();
    public DbSet<AvailabilitySnapshot> AvailabilitySnapshots => Set<AvailabilitySnapshot>();
    public DbSet<DataExport> DataExports => Set<DataExport>();
    public DbSet<PaymentDispute> PaymentDisputes => Set<PaymentDispute>();
    public DbSet<DunningCycle> DunningCycles => Set<DunningCycle>();
    public DbSet<JobQuota> JobQuotas => Set<JobQuota>();
    public DbSet<TwoFaRecoveryRequest> TwoFaRecoveryRequests => Set<TwoFaRecoveryRequest>();



    // Inventory & Products
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<ResourceBooking> ResourceBookings => Set<ResourceBooking>();
    // EquipmentController/ResourcesController query these, but they were never added to the
    // model — EF threw "Cannot create a DbSet for 'Equipment'" and every /equipment request
    // failed with HTTP 400.
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<EquipmentMaintenance> EquipmentMaintenance => Set<EquipmentMaintenance>();
    public DbSet<MarketingFunnel> MarketingFunnels => Set<MarketingFunnel>();
    public DbSet<ScheduleBlock> ScheduleBlocks => Set<ScheduleBlock>();
    public DbSet<TenantIntegration> TenantIntegrations => Set<TenantIntegration>();
    public DbSet<TenantIntegrationAudit> TenantIntegrationAudits => Set<TenantIntegrationAudit>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<DeadLetterMessage> DeadLetterMessages => Set<DeadLetterMessage>();
    public DbSet<TenantOnboardingProgress> OnboardingProgress => Set<TenantOnboardingProgress>();
    public DbSet<LoginHistory> LoginHistories => Set<LoginHistory>();
    public DbSet<Tip> Tips => Set<Tip>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<CustomRole> CustomRoles => Set<CustomRole>();
    public DbSet<Referral> Referrals => Set<Referral>();
    // PipelineStages, Deals already defined above (lines 96-97)
    // PromoCodes already defined above (line 125)
    public DbSet<GroupBooking> GroupBookings => Set<GroupBooking>();
    public DbSet<GroupBookingParticipant> GroupBookingParticipants => Set<GroupBookingParticipant>();
    public DbSet<GroupBookingRecurrence> GroupBookingRecurrences => Set<GroupBookingRecurrence>();
    public DbSet<BookingReminder> BookingReminders => Set<BookingReminder>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    // StaffCommissions already defined above (line 46)
    public DbSet<CommissionRule> CommissionRules => Set<CommissionRule>();
    public DbSet<UserConsent> Consents => Set<UserConsent>();
    public DbSet<StaffTimesheet> StaffTimesheets => Set<StaffTimesheet>();
    public DbSet<StaffCertification> StaffCertifications => Set<StaffCertification>();
    public DbSet<AuditEntryV2> AuditLogsV2 => Set<AuditEntryV2>();
    public DbSet<Experiment> Experiments => Set<Experiment>();

    // Phase 2 — Enterprise Sales
    public DbSet<EnterpriseLead> EnterpriseLeads => Set<EnterpriseLead>();

    // Legal & Compliance
    public DbSet<LegalDisclosureRequest> LegalDisclosureRequests => Set<LegalDisclosureRequest>();
    public DbSet<CookieConsentRecord> CookieConsentRecords => Set<CookieConsentRecord>();
    public DbSet<CcpaDeletionRequest> CcpaDeletionRequests => Set<CcpaDeletionRequest>();

    // Marketing automation & backups (PurchaseOrders DbSet already declared above)
    public DbSet<DripCampaign> DripCampaigns => Set<DripCampaign>();
    public DbSet<RebookPrompt> RebookPrompts => Set<RebookPrompt>();
    public DbSet<TenantBackup> TenantBackups => Set<TenantBackup>();

    private void ApplyGlobalFilters<TEntity>(ModelBuilder modelBuilder, bool isPostgres) where TEntity : class
    {
        bool isBaseEntity = typeof(BaseEntity).IsAssignableFrom(typeof(TEntity));
        bool isTenantEntity = typeof(TenantEntity).IsAssignableFrom(typeof(TEntity));

        if (isBaseEntity && isTenantEntity)
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
                !EF.Property<bool>(e, "IsDeleted") &&
                (_tenantId == null || EF.Property<Guid>(e, "TenantId") == _tenantId));
        }
        else if (isBaseEntity)
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => !EF.Property<bool>(e, "IsDeleted"));
        }
        else if (isTenantEntity)
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => _tenantId == null || EF.Property<Guid>(e, "TenantId") == _tenantId);
        }

        if (isBaseEntity && isPostgres)
        {
            modelBuilder.Entity<TEntity>()
                .Property<byte[]>(nameof(BaseEntity.RowVersion))
                .IsRowVersion();
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Detect if we're running on PostgreSQL (production) vs SQLite/InMemory (tests)
        var isPostgres = Database.ProviderName?.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase) ?? false;

        // ── SQLITE DECIMAL FIX ──────────────────────────────────────────────────────
        // SQLite has no native decimal type — EF Core throws NotSupportedException when
        // Sum(), Avg() etc. are called on decimal columns in tests. Convert all decimal
        // properties to double when running on SQLite so aggregates work correctly.
        // Production (PostgreSQL) is unaffected.
        if (!isPostgres)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(decimal))
                    {
                        property.SetValueConverter(
                            new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<decimal, double>(
                                v => (double)v,
                                v => (decimal)v));
                    }
                    else if (property.ClrType == typeof(decimal?))
                    {
                        property.SetValueConverter(
                            new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<decimal?, double?>(
                                v => v.HasValue ? (double?)v.Value : null,
                                v => v.HasValue ? (decimal?)v.Value : null));
                    }
                }
            }
        }

        // ── UNIVERSAL JSONB MAPPING FOR DICTIONARIES & COMPLEX TYPES ──
        // This ensures ANY Dictionary or PlanFeatures property is correctly serialized to JSON string for PostgreSQL
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(Dictionary<string, object>) ||
                            p.ClrType == typeof(Dictionary<string, string>) ||
                            p.ClrType == typeof(PlanFeatures));

            foreach (var property in properties)
            {
                // Use exact converters to avoid type mismatch errors
                // This serializes the C# objects (Dictionary, PlanFeatures) into JSON strings stored as standard text columns.
                // This avoids Npgsql's strict 'jsonb' type mapping requirements while maintaining full data integrity.
                if (property.ClrType == typeof(Dictionary<string, object>))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<Dictionary<string, object>, string>(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
                    ));
                }
                else if (property.ClrType == typeof(Dictionary<string, string>))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<Dictionary<string, string>, string>(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>()
                    ));
                }
                else if (property.ClrType == typeof(PlanFeatures))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<PlanFeatures, string>(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<PlanFeatures>(v, (JsonSerializerOptions?)null) ?? new PlanFeatures()
                    ));
                }
            }
        }

        // Apply global query filters using reflection
        var applyMethod = typeof(AppDbContext).GetMethod(nameof(ApplyGlobalFilters), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType == null || !entityType.ClrType.IsClass) continue;

            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) || typeof(TenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                applyMethod?.MakeGenericMethod(entityType.ClrType).Invoke(this, new object[] { modelBuilder, isPostgres });
            }
        }

        // CustomRole configuration
        modelBuilder.Entity<CustomRole>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
            if (isPostgres)
            {
                entity.Property(e => e.Permissions).HasColumnType("jsonb");
            }
            else
            {
                entity.Property(e => e.Permissions).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, bool>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, bool>());
            }
            entity.Ignore(e => e.Users); // No FK in User entity yet
        });

        // Booking configuration
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasIndex(e => e.Status).HasFilter("\"Status\" = 0"); // Partial index (0 = Pending)
            if (isPostgres)
            {
                entity.Property(e => e.Metadata).HasColumnType("jsonb");
            }
            else
            {
                entity.Property(e => e.Metadata).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            }
        });

        // Invoice configuration
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(e => e.Status).HasFilter("\"Status\" = 1"); // Partial index (1 = Sent)
            entity.Property(e => e.TotalAmount).HasPrecision(10, 2);
            if (isPostgres)
            {
                entity.Property(e => e.Metadata).HasColumnType("jsonb");
            }
            else
            {
                entity.Property(e => e.Metadata).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            }
        });


        // Additional Platform Configurations
        modelBuilder.Entity<AIUsageLog>(entity =>
        {
            entity.Property(e => e.Cost).HasPrecision(18, 8);
        });

        modelBuilder.Entity<AIAgentConfig>(entity =>
        {
            if (isPostgres)
            {
                entity.Property(e => e.HandoffTriggers).HasColumnType("jsonb");
            }
            else
            {
                entity.Property(e => e.HandoffTriggers).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());
            }
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            if (isPostgres)
            {
                entity.Property(e => e.Scopes).HasColumnType("jsonb");
            }
            else
            {
                entity.Property(e => e.Scopes).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());
            }
        });

        modelBuilder.Entity<VoiceCall>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<Webhook>(entity =>
        {
            if (isPostgres)
            {
                entity.Property(e => e.Events).HasColumnType("jsonb");
            }
            else
            {
                entity.Property(e => e.Events).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());
            }
        });





        // Phase 1 Expansion Mappings
        if (isPostgres)
        {
            modelBuilder.Entity<AvailabilitySnapshot>().Property(e => e.AvailabilityJson).HasColumnType("jsonb");
            modelBuilder.Entity<Tenant>().Property(e => e.Settings).HasColumnType("jsonb");
            modelBuilder.Entity<WebhookEvent>().Property(e => e.Payload).HasColumnType("jsonb");

            // Config GIN Indexes
            modelBuilder.Entity<Tenant>().HasIndex(e => e.Settings).HasMethod("GIN");
            modelBuilder.Entity<Tenant>().HasIndex(e => e.Metadata).HasMethod("GIN");
            modelBuilder.Entity<User>().HasIndex(e => e.Preferences).HasMethod("GIN");
        }

        modelBuilder.Entity<Booking>().HasIndex(e => new { e.TenantId, e.IsDeleted });
        modelBuilder.Entity<Client>().HasIndex(e => new { e.TenantId, e.IsDeleted });
        modelBuilder.Entity<User>().HasIndex(e => new { e.TenantId, e.IsDeleted });

        modelBuilder.Entity<UserTourProgress>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.TourKey }).IsUnique();
        });

        modelBuilder.Entity<ProcessedWebhook>(entity =>
        {
            entity.HasIndex(e => e.EventId).IsUnique();
        });


        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.HasIndex(e => e.Key).IsUnique();
            entity.HasIndex(e => e.ExpiresAt); // For cleanup queries
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
            entity.Property(e => e.Latitude).HasPrecision(18, 10);
            entity.Property(e => e.Longitude).HasPrecision(18, 10);
            if (isPostgres) entity.Property(e => e.BusinessHours).HasColumnType("jsonb");
            if (isPostgres) entity.Property(e => e.Holidays).HasColumnType("jsonb");
        });

        modelBuilder.Entity<CustomDomain>(entity =>
        {
            entity.HasIndex(e => e.Hostname).IsUnique();
        });

        modelBuilder.Entity<Campaign>(entity =>
        {
            if (isPostgres) entity.Property(e => e.AudienceFilters).HasColumnType("jsonb");
        });

        modelBuilder.Entity<CampaignAnalytics>(entity =>
        {
            if (isPostgres) entity.Property(e => e.TimelineData).HasColumnType("jsonb");
            if (isPostgres) entity.Property(e => e.DeviceData).HasColumnType("jsonb");
        });

        // Existing configurations...
        // Tenant configuration
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique();
            if (isPostgres)
            {
                entity.Property(e => e.Settings).HasColumnType("jsonb");
                entity.Property(e => e.Metadata).HasColumnType("jsonb");
            }
            else
            {
                entity.Property(e => e.Settings).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
                entity.Property(e => e.Metadata).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            }

            // Agency self-referencing
            entity.HasOne(e => e.ParentTenant)
                  .WithMany(t => t.SubTenants)
                  .HasForeignKey(e => e.ParentTenantId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
            if (isPostgres)
            {
                entity.Property(e => e.Preferences).HasColumnType("jsonb");
            }
            else
            {
                entity.Property(e => e.Preferences).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            }
        });

        // CommunicationLog configuration
        modelBuilder.Entity<CommunicationLog>(entity =>
        {
            if (isPostgres)
            {
                entity.Property(e => e.Metadata).HasColumnType("jsonb");
            }
        });

        // Payment configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            if (isPostgres)
            {
                entity.Property(e => e.Metadata).HasColumnType("jsonb");
            }
        });

        // Service configuration
        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Name }).IsUnique();
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.DepositAmount).HasPrecision(10, 2);
            if (isPostgres)
            {
                entity.Property(e => e.Settings).HasColumnType("jsonb");
            }
            else
            {
                entity.Property(e => e.Settings).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            }
        });

        // StaffMember configuration
        modelBuilder.Entity<StaffMember>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
            if (isPostgres)
            {
                entity.Property(e => e.Settings).HasColumnType("jsonb");
                entity.Property(e => e.Tags).HasColumnType("jsonb");
            }
            else
            {
                entity.Property(e => e.Settings).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
                entity.Property(e => e.Tags).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());
            }
            entity.Property(e => e.BaseCommissionRate).HasPrecision(10, 2);
        });

        // StaffService configuration
        modelBuilder.Entity<StaffService>(entity =>
        {
            entity.HasIndex(e => new { e.StaffId, e.ServiceId }).IsUnique();
            entity.Property(e => e.CustomPrice).HasPrecision(10, 2);
        });
        // Booking configuration
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => new { e.TenantId, e.StartTime });
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.DepositPaid).HasPrecision(10, 2);

            // From redundant block
            entity.HasIndex(e => new { e.StaffId, e.StartTime, e.EndTime });

            // PostgreSQL Partitioning (Monthly by StartTime)
            if (isPostgres)
            {
                // NOTE: HasPostgresPartitionByRange requires Npgsql EF Core v8+ with correct overload.
                // Revisit this optimization when upgrading the Npgsql provider.
                // entity.HasPostgresPartitionByRange(b => b.StartTime);
            }
        });

        // Client configuration
        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Email });
            entity.Property(e => e.LifetimeValue).HasPrecision(12, 2);
            if (isPostgres)
            {
                entity.Property(e => e.Tags).HasColumnType("jsonb");
                entity.Property(e => e.CustomFields).HasColumnType("jsonb");
            }
            else
            {
                entity.Property(e => e.Tags).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());
                entity.Property(e => e.CustomFields).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            }

            // Client belongs to a Household via HouseholdId (many-to-one)
            entity.HasOne(e => e.Household)
                  .WithMany(h => h.Members)
                  .HasForeignKey(e => e.HouseholdId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Household configuration
        modelBuilder.Entity<Household>(entity =>
        {
            // PrimaryClient is a separate one-way relationship
            entity.HasOne(e => e.PrimaryClient)
                  .WithMany()
                  .HasForeignKey(e => e.PrimaryClientId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // GDPR Consent configuration
        modelBuilder.Entity<GdprConsent>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ClientId, e.ConsentType });
        });

        modelBuilder.Entity<CommunicationLog>(entity =>
        {
            if (isPostgres)
            {
                entity.Property(e => e.Metadata).HasColumnType("jsonb");
                entity.HasIndex(e => e.Metadata).HasMethod("GIN");
            }
            else
            {
                entity.Property(e => e.Metadata).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>());
            }
        });

        modelBuilder.Entity<StaffCommission>(entity =>
        {
            entity.Property(e => e.BaseAmount).HasPrecision(10, 2);
            entity.Property(e => e.CommissionRate).HasPrecision(10, 2);
            entity.Property(e => e.TotalEarned).HasPrecision(10, 2);
        });

        // Payment configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.RefundAmount).HasPrecision(10, 2);
            if (isPostgres)
            {
                entity.Property(e => e.Metadata).HasColumnType("jsonb");
                entity.HasIndex(e => e.Metadata).HasMethod("GIN");
            }
            else
            {
                entity.Property(e => e.Metadata).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());
            }
        });

        // Workflow configuration
        modelBuilder.Entity<WorkflowEntity>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
            if (isPostgres) entity.Property(e => e.TriggerConfig).HasColumnType("jsonb");
            if (isPostgres) entity.Property(e => e.Steps).HasColumnType("jsonb");
        });

        // GiftCertificate configuration
        modelBuilder.Entity<GiftCertificate>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
            entity.Property(e => e.InitialAmount).HasPrecision(10, 2);
            entity.Property(e => e.RemainingAmount).HasPrecision(10, 2);
        });

        // GiftCertificateRedemption configuration
        modelBuilder.Entity<GiftCertificateRedemption>(entity =>
        {
            entity.Property(e => e.AmountRedeemed).HasPrecision(10, 2);
        });

        // MembershipPlan configuration
        modelBuilder.Entity<MembershipPlan>(entity =>
        {
            entity.Property(e => e.Price).HasPrecision(10, 2);
        });

        // ClientMembership configuration
        modelBuilder.Entity<ClientMembership>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ClientId, e.Status });
        });

        // TaxRate configuration
        modelBuilder.Entity<TaxRate>(entity =>
        {
            entity.Property(e => e.Percentage).HasPrecision(5, 2);
        });

        modelBuilder.Entity<TenantDashboardStats>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenantId).IsUnique();
            entity.Property(e => e.TotalRevenue).HasPrecision(18, 2);
            entity.Property(e => e.RevenueThisMonth).HasPrecision(18, 2);
        });

        modelBuilder.Entity<TenantDailyMetric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenantId, e.Date }).IsUnique();
            entity.Property(e => e.Revenue).HasPrecision(18, 2);
        });

        // Subscription configurations
        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.Property(e => e.AiMonthlyBudget).HasPrecision(18, 2);
            if (isPostgres)
            {
                entity.Property(e => e.AllowedAiModels).HasColumnType("jsonb");
            }
            else
            {
                entity.Property(e => e.AllowedAiModels).HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());
            }
        });

        // ── Composite Indexes for High-Traffic Queries ──

        // Bookings: Queried by tenant + time range + status on every dashboard/analytics call
        modelBuilder.Entity<Booking>()
            .HasIndex(b => new { b.TenantId, b.StartTime, b.Status })
            .HasDatabaseName("IX_Bookings_Tenant_StartTime_Status");

        // Bookings: Staff availability checks (most frequent scheduling query)
        modelBuilder.Entity<Booking>()
            .HasIndex(b => new { b.TenantId, b.StaffId, b.StartTime })
            .HasDatabaseName("IX_Bookings_Tenant_Staff_StartTime");

        // Clients: Lookup by email within tenant (login, dedup, CRM search)
        modelBuilder.Entity<Client>()
            .HasIndex(c => new { c.TenantId, c.Email })
            .HasDatabaseName("IX_Clients_Tenant_Email");

        // AuditEntries: Retention job + admin audit log viewer
        modelBuilder.Entity<AuditEntry>()
            .HasIndex(a => new { a.TenantId, a.PerformedAt })
            .HasDatabaseName("IX_AuditEntries_Tenant_PerformedAt");

        // Payments: Revenue analytics by tenant + date
        modelBuilder.Entity<Payment>()
            .HasIndex(p => new { p.TenantId, p.CreatedAt, p.Status })
            .HasDatabaseName("IX_Payments_Tenant_CreatedAt_Status");

        // Performance Boost: Dashboard & Funnel Analytics
        modelBuilder.Entity<Client>()
            .HasIndex(c => new { c.TenantId, c.LastVisitAt })
            .HasDatabaseName("IX_Clients_Tenant_LastVisit");

        modelBuilder.Entity<Booking>()
            .HasIndex(b => new { b.TenantId, b.CreatedAt })
            .HasDatabaseName("IX_Bookings_Tenant_CreatedAt");

        // SecurityEvents: Security audit middleware reads
        modelBuilder.Entity<SecurityEvent>()
            .HasIndex(se => new { se.TenantId, se.CreatedAt })
            .HasDatabaseName("IX_SecurityEvents_Tenant_CreatedAt");

        // ── Full-Text Search Indexes (PostgreSQL Specific) ──
        if (isPostgres)
        {
            modelBuilder.Entity<Client>()
                .HasIndex(c => new { c.Email, c.FirstName, c.LastName })
                .HasMethod("GIN")
                .HasOperators("gin_trgm_ops", "gin_trgm_ops", "gin_trgm_ops")
                .HasDatabaseName("IX_Clients_FullText");
        }

        // CommissionRule configuration
        modelBuilder.Entity<CommissionRule>(entity =>
        {
            entity.Property(e => e.Rate).HasPrecision(10, 2);
            entity.Property(e => e.MinAmount).HasPrecision(10, 2);
            entity.Property(e => e.MaxAmount).HasPrecision(10, 2);
            entity.HasIndex(e => new { e.TenantId, e.StaffId, e.ServiceId, e.IsActive })
                .HasDatabaseName("IX_CommissionRules_Tenant_Staff_Service");
        });

        // WebhookEvents config
        modelBuilder.Entity<WebhookEvent>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.Status });
            entity.HasIndex(e => new { e.Status, e.NextRetryAt }); // Index for queue processor
        });

        // P2: Performance indexes for subscription enforcement hot path
        modelBuilder.Entity<Subscription>()
            .HasIndex(s => new { s.TenantId, s.Status })
            .HasDatabaseName("IX_Subscriptions_Tenant_Status");

        // P2: PricingPlan feature lookup — hits this on every RequireFeature gate
        modelBuilder.Entity<PlanFeatureMapping>()
            .HasIndex(m => m.PricingPlanId)
            .HasDatabaseName("IX_PlanFeatureMappings_PricingPlanId");

        // One price row per service per vehicle class. The unique index is what makes the
        // fallback lookup deterministic: with duplicates, which of two prices a customer is
        // quoted would depend on row order.
        modelBuilder.Entity<ServiceVehiclePrice>(entity =>
        {
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.HasIndex(e => new { e.ServiceId, e.VehicleClass })
                  .IsUnique()
                  .HasDatabaseName("IX_ServiceVehiclePrices_Service_Class");
        });

        // Vehicles are listed per client on every booking screen for a detailer.
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.Property(e => e.Make).HasMaxLength(64);
            entity.Property(e => e.Model).HasMaxLength(64);
            entity.Property(e => e.LicensePlate).HasMaxLength(32);
            entity.Property(e => e.Color).HasMaxLength(32);
            entity.HasIndex(e => e.ClientId).HasDatabaseName("IX_Vehicles_ClientId");
        });

        // Published add-on prices. Key is the code-facing identifier, so it must be unique —
        // two rows for "extra_staff" would make which price we advertise arbitrary.
        modelBuilder.Entity<PricingAddOn>(entity =>
        {
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.Key).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
            entity.Property(e => e.BillingUnit).HasMaxLength(64);
            entity.Property(e => e.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.HasIndex(e => e.Key).IsUnique().HasDatabaseName("IX_PricingAddOns_Key");
        });

        // P2: AI usage log queries by tenant + date (quota enforcement + dashboard)
        modelBuilder.Entity<AIUsageLog>()
            .HasIndex(a => new { a.TenantId, a.CreatedAt })
            .HasDatabaseName("IX_AIUsageLogs_Tenant_CreatedAt");

        // ── Performance indexes: auth, scheduling, and infrastructure tables ──

        // Auth: lockout queries filter by Email+AttemptedAt; token tables looked up by token value only.
        modelBuilder.Entity<LoginAttempt>()
            .HasIndex(a => new { a.Email, a.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_Email_AttemptedAt");

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(t => t.Token).IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");

        modelBuilder.Entity<PasswordResetToken>()
            .HasIndex(t => t.Token).IsUnique()
            .HasDatabaseName("IX_PasswordResetTokens_Token");

        modelBuilder.Entity<EmailVerificationToken>()
            .HasIndex(t => t.Token).IsUnique()
            .HasDatabaseName("IX_EmailVerificationTokens_Token");

        // Users: cross-tenant email lookup (login, forgot-password by email only, not scoped to tenant).
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .HasDatabaseName("IX_Users_Email");

        // Scheduling: AvailabilityCache primary lookup key; SlotHold expiry scan; working-hours/exceptions per staff.
        modelBuilder.Entity<AvailabilityCache>()
            .HasIndex(c => new { c.TenantId, c.StaffId, c.Date }).IsUnique()
            .HasDatabaseName("IX_AvailabilityCaches_Tenant_Staff_Date");

        modelBuilder.Entity<SlotHold>()
            .HasIndex(h => new { h.StaffId, h.SlotDateTime, h.IsReleased })
            .HasDatabaseName("IX_SlotHolds_Staff_Slot_Released");

        modelBuilder.Entity<WorkingHours>()
            .HasIndex(w => new { w.StaffId, w.DayOfWeek })
            .HasDatabaseName("IX_WorkingHours_Staff_DayOfWeek");

        modelBuilder.Entity<ScheduleException>()
            .HasIndex(e => new { e.StaffId, e.Date })
            .HasDatabaseName("IX_ScheduleExceptions_Staff_Date");

        // Notifications: per-user unread badge count and paginated feed.
        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.TenantId, n.UserId, n.IsRead })
            .HasDatabaseName("IX_Notifications_Tenant_User_IsRead");

        // ConversionEvents: dashboard time-range aggregations scoped by tenant.
        modelBuilder.Entity<ConversionEvent>()
            .HasIndex(e => new { e.TenantId, e.CreatedAt })
            .HasDatabaseName("IX_ConversionEvents_Tenant_CreatedAt");

        // OutboxMessage: processor scans only unprocessed rows — partial index on Postgres,
        // composite on SQLite (partial indexes are a Postgres extension).
        if (isPostgres)
        {
            modelBuilder.Entity<OutboxMessage>()
                .HasIndex(m => m.CreatedAt)
                .HasFilter("\"ProcessedAt\" IS NULL")
                .HasDatabaseName("IX_OutboxMessages_Pending");
        }
        else
        {
            modelBuilder.Entity<OutboxMessage>()
                .HasIndex(m => new { m.ProcessedAt, m.CreatedAt })
                .HasDatabaseName("IX_OutboxMessages_Pending");
        }

        // ── PostgreSQL-only column type annotation for dictionary properties ──
        // The ValueConverter loop above already handles serialization on all providers.
        // SetColumnType("jsonb") is a Postgres-only storage hint; applying it on SQLite
        // (used in tests) overwrites the ValueConverter and breaks dictionary deserialization.
        if (isPostgres)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var dictionaryProperties = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(Dictionary<string, object>) ||
                                p.ClrType == typeof(Dictionary<string, string>));

                foreach (var property in dictionaryProperties)
                {
                    property.SetColumnType("jsonb");
                }
            }
        }

        // DB-01 FIX: CreditTransaction had no index; the credit-balance SumAsync query
        // (WHERE TenantId AND ClientId) was an unindexed scan that grows with every transaction.
        modelBuilder.Entity<CreditTransaction>()
            .HasIndex(e => new { e.TenantId, e.ClientId })
            .HasDatabaseName("IX_CreditTransactions_Tenant_Client");
    }

    private void ApplyTenantFilter<T>(ModelBuilder builder) where T : TenantEntity
    {
        builder.Entity<T>().HasQueryFilter(e => _tenantId == null || e.TenantId == _tenantId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestampsAndTenantId();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateTimestampsAndTenantId();
        return base.SaveChanges();
    }

    private void UpdateTimestampsAndTenantId()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;

                if (entry.Entity is TenantEntity tenantEntity && tenantEntity.TenantId == Guid.Empty)
                {
                    if (_tenantId.HasValue)
                    {
                        tenantEntity.TenantId = _tenantId.Value;
                    }
                    else
                    {
                        // In a production scenario, we might want to throw an exception here
                        // if we're trying to save a tenant-scoped entity without a tenant context.
                        // However, for certain background jobs or migrations, this might be bypassed.
                        // For now, let's log a warning if it's empty.
                    }
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;

                // Prevent changing TenantId on existing entities
                if (entry.Entity is TenantEntity tenantEntity)
                {
                    var originalTenantId = Entry(tenantEntity).Property(x => x.TenantId).OriginalValue;
                    if (originalTenantId != Guid.Empty && tenantEntity.TenantId != originalTenantId)
                    {
                        throw new InvalidOperationException("Cannot change the TenantId of an existing entity.");
                    }
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                // Soft Delete Interception
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTime.UtcNow;
                // DeletedBy would require current user context, omitted here for simplicity
            }
        }
    }
}
