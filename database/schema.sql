-- ============================================
-- UPKILO - COMPLETE DATABASE SCHEMA
-- PostgreSQL 17+ with Row-Level Security
-- Version: 2.0 (Latest LTS) | January 26, 2026
-- ============================================

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- ============================================
-- CORE: TENANTS & USERS
-- ============================================

CREATE TABLE tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(100) UNIQUE NOT NULL,
    domain VARCHAR(255),
    logo_url VARCHAR(500),
    primary_color VARCHAR(7) DEFAULT '#06B6D4',
    industry VARCHAR(100),
    timezone VARCHAR(50) DEFAULT 'UTC',
    currency VARCHAR(3) DEFAULT 'USD',
    locale VARCHAR(10) DEFAULT 'en-US',
    status VARCHAR(20) DEFAULT 'active', -- active, suspended, cancelled
    subscription_tier VARCHAR(20) DEFAULT 'starter', -- starter, professional, business, enterprise
    stripe_customer_id VARCHAR(100),
    stripe_subscription_id VARCHAR(100),
    trial_ends_at TIMESTAMPTZ,
    settings JSONB DEFAULT '{}',
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    email VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255),
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    avatar_url VARCHAR(500),
    phone VARCHAR(20),
    role VARCHAR(50) DEFAULT 'staff', -- owner, admin, manager, staff
    status VARCHAR(20) DEFAULT 'active', -- active, inactive, pending
    email_verified BOOLEAN DEFAULT false,
    email_verified_at TIMESTAMPTZ,
    last_login_at TIMESTAMPTZ,
    two_factor_enabled BOOLEAN DEFAULT false,
    two_factor_secret VARCHAR(255),
    preferences JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, email)
);

CREATE TABLE user_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL,
    ip_address INET,
    user_agent TEXT,
    device_type VARCHAR(50),
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- BOOKINGS & SCHEDULING
-- ============================================

CREATE TABLE services (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    duration_minutes INTEGER DEFAULT 60,
    buffer_before INTEGER DEFAULT 0,
    buffer_after INTEGER DEFAULT 0,
    price DECIMAL(10,2) DEFAULT 0,
    currency VARCHAR(3) DEFAULT 'USD',
    color VARCHAR(7),
    is_active BOOLEAN DEFAULT true,
    max_attendees INTEGER DEFAULT 1,
    requires_payment BOOLEAN DEFAULT false,
    deposit_amount DECIMAL(10,2),
    cancellation_policy TEXT,
    settings JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE staff_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(id),
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255),
    phone VARCHAR(20),
    title VARCHAR(100),
    bio TEXT,
    avatar_url VARCHAR(500),
    is_active BOOLEAN DEFAULT true,
    booking_url VARCHAR(255),
    calendar_sync_enabled BOOLEAN DEFAULT false,
    google_calendar_id VARCHAR(255),
    outlook_calendar_id VARCHAR(255),
    settings JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE staff_services (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    staff_id UUID REFERENCES staff_members(id) ON DELETE CASCADE,
    service_id UUID REFERENCES services(id) ON DELETE CASCADE,
    custom_price DECIMAL(10,2),
    custom_duration INTEGER,
    UNIQUE(staff_id, service_id)
);

CREATE TABLE availability_schedules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    staff_id UUID REFERENCES staff_members(id) ON DELETE CASCADE,
    day_of_week INTEGER NOT NULL, -- 0=Sunday, 6=Saturday
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    is_available BOOLEAN DEFAULT true,
    location_id UUID,
    CONSTRAINT valid_day CHECK (day_of_week BETWEEN 0 AND 6)
);

CREATE TABLE availability_overrides (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    staff_id UUID REFERENCES staff_members(id) ON DELETE CASCADE,
    date DATE NOT NULL,
    start_time TIME,
    end_time TIME,
    is_blocked BOOLEAN DEFAULT false,
    reason VARCHAR(255),
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE bookings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    client_id UUID,
    staff_id UUID REFERENCES staff_members(id),
    service_id UUID REFERENCES services(id),
    location_id UUID,
    status VARCHAR(20) DEFAULT 'confirmed', -- pending, confirmed, cancelled, completed, no_show
    start_time TIMESTAMPTZ NOT NULL,
    end_time TIMESTAMPTZ NOT NULL,
    timezone VARCHAR(50),
    notes TEXT,
    internal_notes TEXT,
    price DECIMAL(10,2),
    deposit_paid DECIMAL(10,2) DEFAULT 0,
    payment_status VARCHAR(20) DEFAULT 'pending', -- pending, paid, refunded, partial
    cancellation_reason TEXT,
    cancelled_at TIMESTAMPTZ,
    cancelled_by UUID,
    reminder_sent BOOLEAN DEFAULT false,
    reminder_sent_at TIMESTAMPTZ,
    source VARCHAR(50) DEFAULT 'manual', -- manual, website, api, chatbot, widget
    external_id VARCHAR(255),
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE booking_attendees (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    booking_id UUID REFERENCES bookings(id) ON DELETE CASCADE,
    client_id UUID,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255),
    phone VARCHAR(20),
    status VARCHAR(20) DEFAULT 'confirmed',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- CLIENTS / CRM
-- ============================================

CREATE TABLE clients (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    email VARCHAR(255),
    phone VARCHAR(20),
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    full_name VARCHAR(255) GENERATED ALWAYS AS (COALESCE(first_name, '') || ' ' || COALESCE(last_name, '')) STORED,
    avatar_url VARCHAR(500),
    date_of_birth DATE,
    gender VARCHAR(20),
    address_line1 VARCHAR(255),
    address_line2 VARCHAR(255),
    city VARCHAR(100),
    state VARCHAR(100),
    postal_code VARCHAR(20),
    country VARCHAR(2),
    tags JSONB DEFAULT '[]',
    source VARCHAR(50),
    notes TEXT,
    lifetime_value DECIMAL(12,2) DEFAULT 0,
    total_bookings INTEGER DEFAULT 0,
    last_booking_at TIMESTAMPTZ,
    stripe_customer_id VARCHAR(100),
    custom_fields JSONB DEFAULT '{}',
    marketing_consent BOOLEAN DEFAULT false,
    sms_consent BOOLEAN DEFAULT false,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE client_notes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID REFERENCES clients(id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(id),
    content TEXT NOT NULL,
    is_pinned BOOLEAN DEFAULT false,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- PAYMENTS & BILLING
-- ============================================

CREATE TABLE payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    booking_id UUID REFERENCES bookings(id),
    client_id UUID REFERENCES clients(id),
    amount DECIMAL(10,2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'USD',
    status VARCHAR(20) DEFAULT 'pending', -- pending, succeeded, failed, refunded
    payment_method VARCHAR(50), -- card, cash, bank_transfer
    stripe_payment_intent_id VARCHAR(255),
    stripe_charge_id VARCHAR(255),
    refund_amount DECIMAL(10,2) DEFAULT 0,
    refunded_at TIMESTAMPTZ,
    metadata JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE invoices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    client_id UUID REFERENCES clients(id),
    invoice_number VARCHAR(50) NOT NULL,
    status VARCHAR(20) DEFAULT 'draft', -- draft, sent, paid, overdue, cancelled
    subtotal DECIMAL(10,2) NOT NULL,
    tax_amount DECIMAL(10,2) DEFAULT 0,
    discount_amount DECIMAL(10,2) DEFAULT 0,
    total DECIMAL(10,2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'USD',
    due_date DATE,
    paid_at TIMESTAMPTZ,
    sent_at TIMESTAMPTZ,
    notes TEXT,
    footer TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE invoice_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id UUID REFERENCES invoices(id) ON DELETE CASCADE,
    description VARCHAR(500) NOT NULL,
    quantity DECIMAL(10,2) DEFAULT 1,
    unit_price DECIMAL(10,2) NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    service_id UUID REFERENCES services(id)
);

-- ============================================
-- SUBSCRIPTIONS (TENANT BILLING)
-- ============================================

CREATE TABLE subscription_plans (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    slug VARCHAR(50) UNIQUE NOT NULL,
    description TEXT,
    price_monthly DECIMAL(10,2),
    price_yearly DECIMAL(10,2),
    stripe_price_id_monthly VARCHAR(100),
    stripe_price_id_yearly VARCHAR(100),
    features JSONB DEFAULT '{}',
    limits JSONB DEFAULT '{}', -- max_bookings, max_staff, max_clients, etc.
    is_active BOOLEAN DEFAULT true,
    sort_order INTEGER DEFAULT 0,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE tenant_subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    plan_id UUID REFERENCES subscription_plans(id),
    stripe_subscription_id VARCHAR(100),
    status VARCHAR(20) DEFAULT 'active', -- active, past_due, cancelled, trialing
    current_period_start TIMESTAMPTZ,
    current_period_end TIMESTAMPTZ,
    cancelled_at TIMESTAMPTZ,
    cancel_at_period_end BOOLEAN DEFAULT false,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE usage_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    metric VARCHAR(50) NOT NULL, -- bookings, sms, emails, ai_tokens, storage_mb
    quantity INTEGER DEFAULT 1,
    recorded_at TIMESTAMPTZ DEFAULT NOW(),
    billing_period_start DATE,
    billing_period_end DATE
);

-- ============================================
-- NOTIFICATIONS & COMMUNICATIONS
-- ============================================

CREATE TABLE notification_templates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id),
    type VARCHAR(50) NOT NULL, -- booking_confirmation, reminder, cancellation, etc.
    channel VARCHAR(20) NOT NULL, -- email, sms, push, whatsapp
    subject VARCHAR(255),
    body TEXT NOT NULL,
    is_active BOOLEAN DEFAULT true,
    variables JSONB DEFAULT '[]',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    user_id UUID REFERENCES users(id),
    type VARCHAR(50) NOT NULL,
    title VARCHAR(255) NOT NULL,
    message TEXT,
    data JSONB DEFAULT '{}',
    read_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE email_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id),
    to_email VARCHAR(255) NOT NULL,
    subject VARCHAR(500),
    template_id UUID,
    status VARCHAR(20) DEFAULT 'sent', -- queued, sent, delivered, bounced, failed
    provider_id VARCHAR(100),
    error_message TEXT,
    opened_at TIMESTAMPTZ,
    clicked_at TIMESTAMPTZ,
    sent_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE sms_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id),
    to_phone VARCHAR(20) NOT NULL,
    message TEXT NOT NULL,
    status VARCHAR(20) DEFAULT 'sent',
    provider_id VARCHAR(100),
    segments INTEGER DEFAULT 1,
    cost DECIMAL(6,4),
    error_message TEXT,
    sent_at TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- AI & CHATBOT
-- ============================================

CREATE TABLE ai_conversations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    client_id UUID REFERENCES clients(id),
    channel VARCHAR(20) NOT NULL, -- web, whatsapp, sms, messenger
    session_id VARCHAR(100),
    status VARCHAR(20) DEFAULT 'active', -- active, ended, escalated
    escalated_to UUID REFERENCES users(id),
    started_at TIMESTAMPTZ DEFAULT NOW(),
    ended_at TIMESTAMPTZ,
    metadata JSONB DEFAULT '{}'
);

CREATE TABLE ai_messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conversation_id UUID REFERENCES ai_conversations(id) ON DELETE CASCADE,
    role VARCHAR(20) NOT NULL, -- user, assistant, system
    content TEXT NOT NULL,
    tokens_used INTEGER,
    model VARCHAR(50),
    function_calls JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE ai_usage (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    date DATE DEFAULT CURRENT_DATE,
    prompt_tokens INTEGER DEFAULT 0,
    completion_tokens INTEGER DEFAULT 0,
    total_tokens INTEGER DEFAULT 0,
    estimated_cost DECIMAL(10,4) DEFAULT 0,
    UNIQUE(tenant_id, date)
);

-- ============================================
-- MARKETING & CAMPAIGNS
-- ============================================

CREATE TABLE campaigns (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    type VARCHAR(50) NOT NULL, -- email, sms, workflow
    status VARCHAR(20) DEFAULT 'draft', -- draft, scheduled, running, paused, completed
    subject VARCHAR(500),
    content TEXT,
    audience_filter JSONB DEFAULT '{}',
    scheduled_at TIMESTAMPTZ,
    sent_at TIMESTAMPTZ,
    stats JSONB DEFAULT '{"sent": 0, "delivered": 0, "opened": 0, "clicked": 0}',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE workflows (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    trigger_type VARCHAR(50) NOT NULL, -- booking_created, client_created, form_submitted, etc.
    trigger_config JSONB DEFAULT '{}',
    is_active BOOLEAN DEFAULT true,
    version INTEGER DEFAULT 1,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE workflow_steps (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workflow_id UUID REFERENCES workflows(id) ON DELETE CASCADE,
    step_order INTEGER NOT NULL,
    action_type VARCHAR(50) NOT NULL, -- send_email, send_sms, wait, condition, webhook
    action_config JSONB NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE workflow_executions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workflow_id UUID REFERENCES workflows(id),
    trigger_data JSONB NOT NULL,
    status VARCHAR(20) DEFAULT 'running', -- running, completed, failed, paused
    current_step INTEGER DEFAULT 0,
    started_at TIMESTAMPTZ DEFAULT NOW(),
    completed_at TIMESTAMPTZ,
    error_message TEXT
);

-- ============================================
-- WEBHOOKS
-- ============================================

CREATE TABLE webhook_endpoints (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    url VARCHAR(1000) NOT NULL,
    secret VARCHAR(255) NOT NULL,
    events JSONB DEFAULT '["*"]', -- ["booking.created", "payment.received"]
    is_active BOOLEAN DEFAULT true,
    description VARCHAR(255),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE webhook_deliveries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    endpoint_id UUID REFERENCES webhook_endpoints(id) ON DELETE CASCADE,
    event_type VARCHAR(100) NOT NULL,
    payload JSONB NOT NULL,
    status VARCHAR(20) DEFAULT 'pending', -- pending, success, failed
    response_status INTEGER,
    response_body TEXT,
    attempts INTEGER DEFAULT 0,
    next_retry_at TIMESTAMPTZ,
    delivered_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- MARKETING AUTOMATION (AI)
-- ============================================

CREATE TABLE marketing_configs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE UNIQUE,
    business_url VARCHAR(500),
    industry VARCHAR(100),
    niche VARCHAR(200),
    target_audience JSONB,
    primary_goal VARCHAR(50),
    target_regions JSONB,
    automation_enabled BOOLEAN DEFAULT true,
    onboarding_completed BOOLEAN DEFAULT false,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE seo_analyses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    page_url VARCHAR(1000) NOT NULL,
    current_title VARCHAR(500),
    optimized_title VARCHAR(500),
    current_meta_desc TEXT,
    optimized_meta_desc TEXT,
    keywords JSONB,
    score DECIMAL(5,2),
    last_analyzed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE generated_contents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    content_type VARCHAR(50) NOT NULL,
    title VARCHAR(500) NOT NULL,
    content TEXT NOT NULL,
    keywords JSONB,
    status VARCHAR(20) DEFAULT 'draft',
    published_at TIMESTAMPTZ,
    performance_metrics JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE social_posts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    content_id UUID REFERENCES generated_contents(id),
    platform VARCHAR(50) NOT NULL,
    post_content TEXT NOT NULL,
    hashtags JSONB,
    scheduled_for TIMESTAMPTZ,
    published_at TIMESTAMPTZ,
    engagement_metrics JSONB DEFAULT '{}',
    status VARCHAR(20) DEFAULT 'draft',
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE marketing_analytics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    date DATE NOT NULL,
    traffic_organic INTEGER DEFAULT 0,
    traffic_social INTEGER DEFAULT 0,
    leads_generated INTEGER DEFAULT 0,
    conversions INTEGER DEFAULT 0,
    revenue_attributed DECIMAL(12,2) DEFAULT 0,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(tenant_id, date)
);

-- ============================================
-- AUDIT & COMPLIANCE
-- ============================================

CREATE TABLE audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID,
    user_id UUID,
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(100),
    entity_id UUID,
    old_values JSONB,
    new_values JSONB,
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE consent_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id),
    client_id UUID REFERENCES clients(id),
    consent_type VARCHAR(50) NOT NULL, -- marketing, sms, data_processing
    granted BOOLEAN NOT NULL,
    ip_address INET,
    granted_at TIMESTAMPTZ DEFAULT NOW(),
    revoked_at TIMESTAMPTZ
);

CREATE TABLE data_deletion_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id),
    requester_email VARCHAR(255) NOT NULL,
    status VARCHAR(20) DEFAULT 'pending', -- pending, processing, completed, rejected
    requested_at TIMESTAMPTZ DEFAULT NOW(),
    completed_at TIMESTAMPTZ,
    notes TEXT
);

-- ============================================
-- FEATURE FLAGS & SETTINGS
-- ============================================

CREATE TABLE feature_flags (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) UNIQUE NOT NULL,
    description TEXT,
    is_enabled BOOLEAN DEFAULT false,
    rollout_percentage INTEGER DEFAULT 0,
    targeting_rules JSONB DEFAULT '{}',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TABLE tenant_features (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES tenants(id) ON DELETE CASCADE,
    feature_flag_id UUID REFERENCES feature_flags(id) ON DELETE CASCADE,
    is_enabled BOOLEAN DEFAULT true,
    UNIQUE(tenant_id, feature_flag_id)
);

-- ============================================
-- INDEXES
-- ============================================

-- Tenants
CREATE INDEX idx_tenants_slug ON tenants(slug);
CREATE INDEX idx_tenants_status ON tenants(status);

-- Users & Sessions
CREATE INDEX idx_users_tenant ON users(tenant_id);
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_user_sessions_token ON user_sessions(token_hash);

-- Bookings
CREATE INDEX idx_bookings_tenant ON bookings(tenant_id);
CREATE INDEX idx_bookings_staff ON bookings(staff_id);
CREATE INDEX idx_bookings_client ON bookings(client_id);
CREATE INDEX idx_bookings_start_time ON bookings(start_time);
CREATE INDEX idx_bookings_status ON bookings(status);
CREATE INDEX idx_bookings_tenant_date ON bookings(tenant_id, start_time);

-- Clients
CREATE INDEX idx_clients_tenant ON clients(tenant_id);
CREATE INDEX idx_clients_email ON clients(tenant_id, email);
CREATE INDEX idx_clients_phone ON clients(tenant_id, phone);
CREATE INDEX idx_clients_name_trgm ON clients USING gin(full_name gin_trgm_ops);

-- Payments
CREATE INDEX idx_payments_tenant ON payments(tenant_id);
CREATE INDEX idx_payments_booking ON payments(booking_id);

-- Notifications
CREATE INDEX idx_notifications_user ON notifications(user_id);
CREATE INDEX idx_notifications_unread ON notifications(user_id, read_at) WHERE read_at IS NULL;

-- Audit logs
CREATE INDEX idx_audit_logs_tenant ON audit_logs(tenant_id);
CREATE INDEX idx_audit_logs_entity ON audit_logs(entity_type, entity_id);
CREATE INDEX idx_audit_logs_created ON audit_logs(created_at);

-- Webhooks
CREATE INDEX idx_webhook_deliveries_pending ON webhook_deliveries(status, next_retry_at) 
    WHERE status = 'pending' OR status = 'failed';

-- ============================================
-- ROW-LEVEL SECURITY (RLS)
-- ============================================

-- Enable RLS on all tenant-scoped tables
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE bookings ENABLE ROW LEVEL SECURITY;
ALTER TABLE clients ENABLE ROW LEVEL SECURITY;
ALTER TABLE services ENABLE ROW LEVEL SECURITY;
ALTER TABLE staff_members ENABLE ROW LEVEL SECURITY;
ALTER TABLE payments ENABLE ROW LEVEL SECURITY;
ALTER TABLE invoices ENABLE ROW LEVEL SECURITY;
ALTER TABLE campaigns ENABLE ROW LEVEL SECURITY;
ALTER TABLE workflows ENABLE ROW LEVEL SECURITY;
ALTER TABLE notifications ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_conversations ENABLE ROW LEVEL SECURITY;
ALTER TABLE webhook_endpoints ENABLE ROW LEVEL SECURITY;

-- Example RLS policies (tenant isolation)
CREATE POLICY tenant_isolation_users ON users
    FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);

CREATE POLICY tenant_isolation_bookings ON bookings
    FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);

CREATE POLICY tenant_isolation_clients ON clients
    FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);

CREATE POLICY tenant_isolation_services ON services
    FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);

-- ============================================
-- TRIGGERS
-- ============================================

-- Updated_at trigger function
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply to all tables with updated_at
CREATE TRIGGER update_tenants_updated_at BEFORE UPDATE ON tenants
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_bookings_updated_at BEFORE UPDATE ON bookings
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_clients_updated_at BEFORE UPDATE ON clients
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- (H-16 FIX: Removed update_client_booking_stats trigger to prevent row-level deadlocks during concurrent bookings. 
-- Aggregates should be calculated via background jobs or materialized views.)

-- ============================================
-- SEED DATA
-- ============================================

-- Subscription plans
INSERT INTO subscription_plans (name, slug, price_monthly, price_yearly, limits, features) VALUES
('Starter', 'starter', 49, 490, 
    '{"max_bookings": 500, "max_staff": 3, "max_clients": 500}',
    '{"booking": true, "crm": true, "payments": true, "ai_chatbot": false, "campaigns": false}'),
('Professional', 'professional', 99, 990, 
    '{"max_bookings": -1, "max_staff": 10, "max_clients": -1}',
    '{"booking": true, "crm": true, "payments": true, "ai_chatbot": true, "campaigns": true}'),
('Business', 'business', 199, 1990, 
    '{"max_bookings": -1, "max_staff": -1, "max_clients": -1}',
    '{"booking": true, "crm": true, "payments": true, "ai_chatbot": true, "campaigns": true, "api": true, "white_label": true}'),
('Enterprise', 'enterprise', NULL, NULL, 
    '{"max_bookings": -1, "max_staff": -1, "max_clients": -1}',
    '{"booking": true, "crm": true, "payments": true, "ai_chatbot": true, "campaigns": true, "api": true, "white_label": true, "sso": true, "dedicated_support": true}');

-- Feature flags
INSERT INTO feature_flags (name, description, is_enabled, rollout_percentage) VALUES
('ai_chatbot_v2', 'New AI chatbot with GPT-4o', false, 0),
('marketing_automation', 'AI marketing automation engine', false, 0),
('dark_mode', 'Dark mode UI', true, 100),
('multi_language', 'Multi-language support', true, 100),
('video_calls', 'Video conferencing integration', false, 0);

-- ============================================
-- COMMENTS / DOCUMENTATION
-- ============================================

COMMENT ON TABLE tenants IS 'Multi-tenant core table - each business is a tenant';
COMMENT ON TABLE users IS 'Staff/admin users belonging to a tenant';
COMMENT ON TABLE bookings IS 'Core bookings/appointments table';
COMMENT ON TABLE clients IS 'Customer records per tenant';
COMMENT ON TABLE ai_conversations IS 'AI chatbot conversation sessions';
COMMENT ON TABLE webhook_endpoints IS 'Outbound webhook configurations per tenant';
COMMENT ON TABLE audit_logs IS 'Comprehensive audit trail for all actions';

CREATE TABLE "ProcessedWebhooks" (
    "EventId" VARCHAR(255) PRIMARY KEY,
    "EventType" VARCHAR(255),
    "ProcessedAt" TIMESTAMPTZ DEFAULT NOW()
);

-- ============================================
-- SECURITY HARDENING (RLS for remaining tables)
-- ============================================

ALTER TABLE tenant_subscriptions ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON tenant_subscriptions FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);

ALTER TABLE usage_records ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON usage_records FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);

ALTER TABLE notification_templates ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON notification_templates FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);

ALTER TABLE email_logs ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON email_logs FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);

ALTER TABLE sms_logs ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON sms_logs FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);

ALTER TABLE ai_usage ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON ai_usage FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);

ALTER TABLE marketing_configs ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON marketing_configs FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);

ALTER TABLE seo_analyses ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON seo_analyses FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);

ALTER TABLE generated_contents ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON generated_contents FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);

ALTER TABLE social_posts ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON social_posts FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);

ALTER TABLE marketing_analytics ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON marketing_analytics FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);

ALTER TABLE audit_logs ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON audit_logs FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);

ALTER TABLE consent_records ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON consent_records FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);

ALTER TABLE data_deletion_requests ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON data_deletion_requests FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);

ALTER TABLE tenant_features ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON tenant_features FOR ALL USING (tenant_id = current_setting('app.tenant_id')::uuid);