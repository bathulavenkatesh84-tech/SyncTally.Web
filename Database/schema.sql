-- =====================================================================
-- SyncTally public website – PostgreSQL schema (schema: cmr_admin)
-- Run once:  psql -h localhost -U postgres -d postgres -f schema.sql
-- All names are snake_case. Safe to re-run (IF NOT EXISTS everywhere).
-- =====================================================================
CREATE SCHEMA IF NOT EXISTS cmr_admin;
SET search_path TO cmr_admin;

-- Key/value site settings (WhatsApp number, contact details, testimonial …)
CREATE TABLE IF NOT EXISTS cmr_admin.site_setting (
    setting_key    varchar(60)  PRIMARY KEY,
    setting_value  text         NOT NULL,
    updated_at     timestamptz  NOT NULL DEFAULT now()
);

-- Feature catalogue: drives the mega menu, the /Features pages and the home page
CREATE TABLE IF NOT EXISTS cmr_admin.site_feature (
    feature_id         serial       PRIMARY KEY,
    slug               varchar(80)  NOT NULL,
    title              varchar(120) NOT NULL,
    icon               varchar(16)  NOT NULL,
    live_status        varchar(10)  NOT NULL,
    lead               text         NOT NULL,
    points             text[]       NOT NULL,
    steps              text[]       NOT NULL,
    has_app            boolean      NOT NULL DEFAULT false,
    app_label          varchar(60)  NOT NULL DEFAULT 'Open in app',
    group_title        varchar(80)  NOT NULL,
    menu_column        smallint     NOT NULL,
    group_order        smallint     NOT NULL,
    sort_order         smallint     NOT NULL,
    is_home_highlight  boolean      NOT NULL DEFAULT false,
    is_active          boolean      NOT NULL DEFAULT true,
    problem            text         NOT NULL DEFAULT '',
    solution           text         NOT NULL DEFAULT '',
    benefits           text[]       NOT NULL DEFAULT '{}',   -- 'Title|Description'
    stats              text[]       NOT NULL DEFAULT '{}',   -- 'Value|Label'
    faqs               text[]       NOT NULL DEFAULT '{}',   -- 'Question|Answer'
    CONSTRAINT uq_site_feature_slug UNIQUE (slug),
    CONSTRAINT ck_site_feature_status CHECK (live_status IN ('live', 'sim', 'mobile')),
    CONSTRAINT ck_site_feature_column CHECK (menu_column BETWEEN 1 AND 4)
);
CREATE INDEX IF NOT EXISTS ix_site_feature_menu
    ON cmr_admin.site_feature (menu_column, group_order, sort_order) WHERE is_active;
CREATE INDEX IF NOT EXISTS ix_site_feature_group ON cmr_admin.site_feature (group_title);

-- Home page "one order, seven stops" journey
CREATE TABLE IF NOT EXISTS cmr_admin.site_journey_step (
    step_id      serial       PRIMARY KEY,
    icon         varchar(16)  NOT NULL,
    title        varchar(80)  NOT NULL,
    description  text         NOT NULL,
    sort_order   smallint     NOT NULL,
    CONSTRAINT uq_site_journey_order UNIQUE (sort_order)
);

CREATE TABLE IF NOT EXISTS cmr_admin.site_faq (
    faq_id      serial        PRIMARY KEY,
    question    varchar(200)  NOT NULL,
    answer      text          NOT NULL,
    sort_order  smallint      NOT NULL,
    is_active   boolean       NOT NULL DEFAULT true
);

CREATE TABLE IF NOT EXISTS cmr_admin.site_plan (
    plan_id      serial        PRIMARY KEY,
    name         varchar(40)   NOT NULL,
    price_1y     numeric(10,2) NOT NULL,
    features     text[]        NOT NULL,
    is_popular   boolean       NOT NULL DEFAULT false,
    sort_order   smallint      NOT NULL,
    CONSTRAINT uq_site_plan_name UNIQUE (name),
    CONSTRAINT ck_site_plan_price CHECK (price_1y > 0)
);

CREATE TABLE IF NOT EXISTS cmr_admin.site_partner (
    partner_id  serial        PRIMARY KEY,
    name        varchar(120)  NOT NULL,
    kind        varchar(40)   NOT NULL,
    city        varchar(60)   NOT NULL,
    blurb       text          NOT NULL,
    sort_order  smallint      NOT NULL DEFAULT 0,
    is_active   boolean       NOT NULL DEFAULT true,
    CONSTRAINT uq_site_partner_name UNIQUE (name)
);
CREATE INDEX IF NOT EXISTS ix_site_partner_kind ON cmr_admin.site_partner (kind);

CREATE TABLE IF NOT EXISTS cmr_admin.site_blog_post (
    post_id       serial        PRIMARY KEY,
    slug          varchar(100)  NOT NULL,
    title         varchar(200)  NOT NULL,
    tag           varchar(40)   NOT NULL,
    published_on  date          NOT NULL,
    read_minutes  smallint      NOT NULL DEFAULT 3,
    excerpt       text          NOT NULL,
    body          text[]        NOT NULL,
    view_count    integer       NOT NULL DEFAULT 0,
    is_published  boolean       NOT NULL DEFAULT true,
    icon          varchar(8)    NOT NULL DEFAULT '📰',
    author        varchar(80)   NOT NULL DEFAULT 'SyncTally Team',
    author_role   varchar(80)   NOT NULL DEFAULT 'Editorial',
    is_featured   boolean       NOT NULL DEFAULT false,
    CONSTRAINT uq_site_blog_slug UNIQUE (slug),
    CONSTRAINT ck_site_blog_minutes CHECK (read_minutes > 0)
);
CREATE INDEX IF NOT EXISTS ix_site_blog_published ON cmr_admin.site_blog_post (published_on DESC) WHERE is_published;
CREATE INDEX IF NOT EXISTS ix_site_blog_tag ON cmr_admin.site_blog_post (tag);

-- Form submissions ---------------------------------------------------
CREATE TABLE IF NOT EXISTS cmr_admin.site_contact_message (
    message_id  bigserial     PRIMARY KEY,
    name        varchar(100)  NOT NULL,
    email       varchar(200)  NOT NULL,
    topic       varchar(60)   NOT NULL,
    message     text          NOT NULL,
    ip_address  varchar(45),
    created_at  timestamptz   NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_site_contact_created ON cmr_admin.site_contact_message (created_at DESC);

CREATE TABLE IF NOT EXISTS cmr_admin.site_whatsapp_lead (
    lead_id     bigserial     PRIMARY KEY,
    name        varchar(100)  NOT NULL,
    phone       varchar(15)   NOT NULL,
    topic       varchar(60)   NOT NULL,
    message     text,
    ip_address  varchar(45),
    created_at  timestamptz   NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_site_lead_created ON cmr_admin.site_whatsapp_lead (created_at DESC);

CREATE TABLE IF NOT EXISTS cmr_admin.site_partner_application (
    application_id  bigserial     PRIMARY KEY,
    name            varchar(120)  NOT NULL,
    email           varchar(200)  NOT NULL,
    phone           varchar(15)   NOT NULL,
    city            varchar(60)   NOT NULL,
    created_at      timestamptz   NOT NULL DEFAULT now()
);

-- 15-day free trial sign-ups. One row per e-mail; the row is 'pending' until the e-mailed OTP is
-- verified and a password is chosen, then 'active'. Password is a salted PBKDF2 hash:
--   PBKDF2$<iterations>$<base64 salt>$<base64 hash>   (HMACSHA256, 32-byte hash)
-- so the application (tallywebapp) can verify it with the same algorithm.
CREATE TABLE IF NOT EXISTS cmr_admin.site_trial_signup (
    signup_id        bigserial     PRIMARY KEY,
    signup_ref       uuid          NOT NULL DEFAULT gen_random_uuid(),
    full_name        varchar(100)  NOT NULL,
    company_name     varchar(150)  NOT NULL,
    email            varchar(200)  NOT NULL,
    phone            varchar(15)   NOT NULL,
    city             varchar(60)   NOT NULL,
    salesmen_count   smallint      NOT NULL DEFAULT 1,
    plan_interest    varchar(40),
    status           varchar(10)   NOT NULL DEFAULT 'pending',
    password_hash    text,
    otp_hash         varchar(100),
    otp_expires_at   timestamptz,
    otp_attempts     smallint      NOT NULL DEFAULT 0,
    otp_sent_count   smallint      NOT NULL DEFAULT 0,
    otp_sent_at      timestamptz,
    trial_start      timestamptz,
    trial_end        timestamptz,
    ip_address       varchar(45),
    created_at       timestamptz   NOT NULL DEFAULT now(),
    activated_at     timestamptz,
    CONSTRAINT uq_site_trial_signup_ref UNIQUE (signup_ref),
    CONSTRAINT ck_site_trial_signup_status CHECK (status IN ('pending', 'active'))
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_site_trial_signup_email ON cmr_admin.site_trial_signup (lower(email));

-- One activity row per submission (written in the same transaction)
CREATE TABLE IF NOT EXISTS cmr_admin.site_activity (
    activity_id    bigserial    PRIMARY KEY,
    activity_type  varchar(30)  NOT NULL,
    reference_id   bigint       NOT NULL,
    detail         varchar(300),
    created_at     timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT ck_site_activity_type CHECK (activity_type IN ('contact', 'whatsapp_lead', 'partner_application', 'trial_signup', 'trial_activated'))
);
CREATE INDEX IF NOT EXISTS ix_site_activity_created ON cmr_admin.site_activity (created_at DESC);

-- Application error log (written by IErrorLogService)
CREATE TABLE IF NOT EXISTS cmr_admin.site_error_log (
    error_id      bigserial     PRIMARY KEY,
    logged_at     timestamptz   NOT NULL DEFAULT now(),
    level         varchar(10)   NOT NULL DEFAULT 'Error',
    source        varchar(200),
    message       text          NOT NULL,
    stack_trace   text,
    request_path  varchar(300),
    user_name     varchar(200),
    CONSTRAINT ck_site_error_level CHECK (level IN ('Info', 'Warning', 'Error'))
);
CREATE INDEX IF NOT EXISTS ix_site_error_logged ON cmr_admin.site_error_log (logged_at DESC);
