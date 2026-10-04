-- =====================================================================
-- DEMO ACCOUNT for tallywebapp  (fictional data only – never a real customer)
-- Creates: "Demo" role + its module permissions, a demo subscription, ONE demo company,
-- and ONE demo user linked to it. Safe to re-run (it skips anything that already exists).
--
-- BEFORE RUNNING – edit the 4 values in the DECLARE block:
--   v_email    the demo user's e-mail / login   (also the Google account that owns the demo Sheet)
--   v_password the demo login password          (stored as a BCrypt hash)
--   v_sheet_id the Google Sheet ID of the demo company data (the long id in the Sheet's URL)
--   v_oauth_id tally_sync_oauth_config.id of the Google project that can read that Sheet (NULL if none)
-- Requires the pgcrypto extension for BCrypt:  CREATE EXTENSION IF NOT EXISTS pgcrypto;
-- =====================================================================
SET search_path TO cmr_admin;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

DO $$
DECLARE
    v_email     text := 'testdemo2603@gmail.com';
    v_password  text := 'CHANGE_ME_Demo#2026';
    v_sheet_id  text := NULL;        -- e.g. '1AbC...xyz'
    v_oauth_id  int  := NULL;        -- e.g. 1

    v_company   text := 'Demo Distributors (Sample)';
    v_by        text := 'Demo Setup';
    v_role_id   int;
    v_sub_id    bigint;
    v_dtl_id    bigint;
    v_user_id   bigint;
    -- module ids (tally_modules.id) the demo role CAN open. Everything else is blocked.
    v_allowed   int[] := ARRAY[1,2,3,4,5,6,7,8,10,15,17,18,20,22,23,26];
    -- blocked on purpose: 9,11,13,14,16 (inactive), 12 CreateVoucher (writes), 19 UserManagement,
    --                     21 Settings, 24 WhatsAppTemplates (sends messages), 25 Ledger scrutiny
    m           record;
BEGIN
    IF EXISTS (SELECT 1 FROM user_management WHERE lower(email) = lower(v_email)) THEN
        RAISE NOTICE 'Demo user % already exists – nothing to do.', v_email;
        RETURN;
    END IF;

    -- 1. Demo role (looked up by name so re-runs reuse it)
    SELECT id INTO v_role_id FROM tally_roles WHERE role_name = 'Demo' LIMIT 1;
    IF v_role_id IS NULL THEN
        INSERT INTO tally_roles (role_name) VALUES ('Demo') RETURNING id INTO v_role_id;
    END IF;

    -- 2. Role permissions: allow only v_allowed, deny the rest
    FOR m IN SELECT id FROM tally_modules LOOP
        INSERT INTO tally_role_permissions (module_id, role_id, can_access, created_by)
        VALUES (m.id, v_role_id, m.id = ANY (v_allowed), v_by)
        ON CONFLICT (module_id, role_id) DO UPDATE SET can_access = EXCLUDED.can_access;
    END LOOP;

    -- 3. Subscription: far-future end date so expiry checks never lock the demo, ONE company
    INSERT INTO tally_sync_subscription
        (companyname, emailid, subscription_fromdate, subscription_todate, createdby,
         purchasecompanycnt, usertoken, oauth_config_id)
    VALUES ('SyncTally Demo', v_email, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + interval '10 years', v_by,
            1, '', v_oauth_id)
    RETURNING id INTO v_sub_id;

    -- 4. The one demo company (points at the demo Sheet)
    INSERT INTO tally_sync_subscription_dtl
        (fk_id, createdby, companyname, spreadsheetid, isdefault, activdated, activateddate, activatedby, deactivated)
    VALUES (v_sub_id, v_by, v_company, v_sheet_id, true, true, CURRENT_TIMESTAMP, v_by, false)
    RETURNING id INTO v_dtl_id;

    -- 5. Demo user (fields tallywebapp's Login needs: BCrypt hash, istallysync='y', ismainadmin=true)
    INSERT INTO user_management
        (user_id, subscription_id, email, mobile_number, location, role_id, status, created_by, created_on,
         password_hash, pwd_chng_date, istallysync, ismainadmin)
    VALUES (v_email, v_sub_id, v_email, NULL, 'Sample City', v_role_id, 'Active', v_by, CURRENT_TIMESTAMP,
            crypt(v_password, gen_salt('bf', 10)), CURRENT_TIMESTAMP, 'y', true)
    RETURNING id INTO v_user_id;

    INSERT INTO user_companies (user_id, company_id) VALUES (v_user_id, v_dtl_id) ON CONFLICT DO NOTHING;

    RAISE NOTICE 'Demo ready: subscription %, company %, user % (role %). Login: % / the password you set.',
        v_sub_id, v_dtl_id, v_user_id, v_role_id, v_email;
END $$;
