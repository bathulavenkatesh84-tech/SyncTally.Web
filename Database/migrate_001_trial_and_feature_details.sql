-- migrate_001: 15-day trial sign-up + detailed feature pages.
-- Safe to run on a database that already has schema.sql from the first delivery. Re-run seed.sql afterwards.
ALTER TABLE cmr_admin.site_feature ADD COLUMN IF NOT EXISTS problem  text   NOT NULL DEFAULT '';
ALTER TABLE cmr_admin.site_feature ADD COLUMN IF NOT EXISTS solution text   NOT NULL DEFAULT '';
ALTER TABLE cmr_admin.site_feature ADD COLUMN IF NOT EXISTS benefits text[] NOT NULL DEFAULT '{}';
ALTER TABLE cmr_admin.site_feature ADD COLUMN IF NOT EXISTS stats    text[] NOT NULL DEFAULT '{}';
ALTER TABLE cmr_admin.site_feature ADD COLUMN IF NOT EXISTS faqs     text[] NOT NULL DEFAULT '{}';

ALTER TABLE cmr_admin.site_activity DROP CONSTRAINT IF EXISTS ck_site_activity_type;
ALTER TABLE cmr_admin.site_activity ADD CONSTRAINT ck_site_activity_type
    CHECK (activity_type IN ('contact', 'whatsapp_lead', 'partner_application', 'trial_signup', 'trial_activated'));


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
