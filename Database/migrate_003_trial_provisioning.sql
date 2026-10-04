-- =====================================================================
-- migrate_003: link a website trial sign-up to what was created in the application.
-- Only the website's OWN table (site_trial_signup) is changed – no tally_* / user_* table is altered.
-- Safe to re-run.
-- =====================================================================
SET search_path TO cmr_admin;

ALTER TABLE cmr_admin.site_trial_signup ADD COLUMN IF NOT EXISTS app_subscription_id bigint;
ALTER TABLE cmr_admin.site_trial_signup ADD COLUMN IF NOT EXISTS app_user_id         bigint;
ALTER TABLE cmr_admin.site_trial_signup ADD COLUMN IF NOT EXISTS app_company_id      bigint;

-- The role given to the first (trial) user is looked up by name: role_name LIKE 'admin%' (case-insensitive).
-- Check that it exists:
--   SELECT id, role_name FROM cmr_admin.tally_roles ORDER BY id;
