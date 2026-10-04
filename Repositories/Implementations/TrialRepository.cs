using System.Data;
using SyncTally.Web.Repositories.Interfaces;
using SyncTally.Web.Services.Interfaces;

namespace SyncTally.Web.Repositories.Implementations;

public class TrialRepository : ITrialRepository
{
    private readonly IFlexidb _db;
    public TrialRepository(IFlexidb db) => _db = db;

    public Task<DataTable> GetByEmailAsync(string email) => _db.GetdtAsync(SqlHelper.Cmd(
        @"SELECT signup_id, signup_ref::text AS signup_ref, status, otp_sent_count,
                 COALESCE(EXTRACT(EPOCH FROM (now() - otp_sent_at)), 99999)::int AS seconds_since_sent
            FROM site_trial_signup
           WHERE lower(email) = lower(@email)",
        ("email", email)));

    public Task<DataTable> GetByRefAsync(Guid signupRef) => _db.GetdtAsync(SqlHelper.Cmd(
        @"SELECT signup_id, signup_ref::text AS signup_ref, full_name, company_name, email, status,
                 otp_hash, otp_attempts, otp_sent_count,
                 (otp_expires_at IS NOT NULL AND otp_expires_at > now()) AS otp_valid,
                 COALESCE(EXTRACT(EPOCH FROM (now() - otp_sent_at)), 99999)::int AS seconds_since_sent,
                 trial_end
            FROM site_trial_signup
           WHERE signup_ref = @ref",
        ("ref", signupRef)));

    public Task<(long Id, Guid Ref)?> UpsertPendingAsync(string fullName, string company, string email, string phone, string city,
        int salesmen, string? plan, string otpHash, int otpMinutes, string? ip) =>
        _db.ExecuteTransactionAsync<(long Id, Guid Ref)?>(async (conn, tx) =>
        {
            long id;
            Guid reference;
            bool inserted;
            using (var upsert = SqlHelper.Cmd(
                @"INSERT INTO site_trial_signup
                      (full_name, company_name, email, phone, city, salesmen_count, plan_interest, status,
                       otp_hash, otp_expires_at, otp_attempts, otp_sent_count, otp_sent_at, ip_address)
                  VALUES (@name, @company, @email, @phone, @city, @salesmen, @plan, 'pending',
                          @otp, now() + (@mins * interval '1 minute'), 0, 1, now(), @ip)
                  ON CONFLICT ((lower(email))) DO UPDATE SET
                      full_name = EXCLUDED.full_name, company_name = EXCLUDED.company_name, phone = EXCLUDED.phone,
                      city = EXCLUDED.city, salesmen_count = EXCLUDED.salesmen_count, plan_interest = EXCLUDED.plan_interest,
                      otp_hash = EXCLUDED.otp_hash, otp_expires_at = EXCLUDED.otp_expires_at, otp_attempts = 0,
                      otp_sent_count = site_trial_signup.otp_sent_count + 1, otp_sent_at = now(),
                      ip_address = EXCLUDED.ip_address
                  WHERE site_trial_signup.status = 'pending'
                  RETURNING signup_id, signup_ref::text AS signup_ref, (xmax = 0) AS inserted",
                conn, tx,
                ("name", fullName), ("company", company), ("email", email), ("phone", phone), ("city", city),
                ("salesmen", salesmen), ("plan", plan), ("otp", otpHash), ("mins", otpMinutes), ("ip", ip)))
            {
                using var reader = await upsert.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return null;           // e-mail already has an active account
                id = Convert.ToInt64(reader.GetValue(0));
                reference = Guid.Parse(Convert.ToString(reader.GetValue(1))!);
                inserted = Convert.ToBoolean(reader.GetValue(2));
            }

            using (var activity = SqlHelper.Cmd(
                "INSERT INTO site_activity (activity_type, reference_id, detail) VALUES ('trial_signup', @id, @detail)",
                conn, tx, ("id", id), ("detail", $"{company} – {email}" + (inserted ? "" : " (re-sent)"))))
            {
                await activity.ExecuteNonQueryAsync();
            }
            return (id, reference);
        });

    public async Task SetOtpAsync(long signupId, string otpHash, int otpMinutes) => await _db.ExecuteAsync(SqlHelper.Cmd(
        @"UPDATE site_trial_signup
             SET otp_hash = @otp, otp_expires_at = now() + (@mins * interval '1 minute'), otp_attempts = 0,
                 otp_sent_count = otp_sent_count + 1, otp_sent_at = now()
           WHERE signup_id = @id AND status = 'pending'",
        ("otp", otpHash), ("mins", otpMinutes), ("id", signupId)));

    public async Task IncrementAttemptsAsync(long signupId) => await _db.ExecuteAsync(SqlHelper.Cmd(
        "UPDATE site_trial_signup SET otp_attempts = otp_attempts + 1 WHERE signup_id = @id",
        ("id", signupId)));

    public async Task<bool> IsEmailInAppAsync(string email)
    {
        var o = await _db.GetScalarAsync(SqlHelper.Cmd(
            @"SELECT (EXISTS (SELECT 1 FROM user_management WHERE lower(email) = lower(@e) OR lower(user_id) = lower(@e))
                   OR EXISTS (SELECT 1 FROM tally_sync_subscription WHERE lower(emailid) = lower(@e)))",
            ("e", email)));
        return o != null && o != DBNull.Value && Convert.ToBoolean(o);
    }

    public Task<ActivationStatus> ActivateAsync(long signupId, string passwordHash, string appPasswordHash, int trialDays) =>
        _db.ExecuteTransactionAsync<ActivationStatus>(async (conn, tx) =>
        {
            const string by = "Website Trial";
            string fullName, company, email, phone, city;

            // 1. lock + read the pending sign-up
            using (var sel = SqlHelper.Cmd(
                @"SELECT full_name, company_name, email, phone, city
                    FROM site_trial_signup WHERE signup_id = @id AND status = 'pending' FOR UPDATE",
                conn, tx, ("id", signupId)))
            using (var rd = await sel.ExecuteReaderAsync())
            {
                if (!await rd.ReadAsync()) throw new InvalidOperationException("Sign-up is not pending.");
                fullName = rd.GetString(0); company = rd.GetString(1); email = rd.GetString(2);
                phone = rd.GetString(3); city = rd.GetString(4);
            }

            // 2. the e-mail may have been created in the application while the OTP was pending
            using (var chk = SqlHelper.Cmd(
                @"SELECT (EXISTS (SELECT 1 FROM user_management WHERE lower(email) = lower(@e) OR lower(user_id) = lower(@e))
                       OR EXISTS (SELECT 1 FROM tally_sync_subscription WHERE lower(emailid) = lower(@e)))",
                conn, tx, ("e", email)))
            {
                if (Convert.ToBoolean(await chk.ExecuteScalarAsync())) return ActivationStatus.EmailAlreadyInApp;
            }

            // 3. role for the first user of a new subscription
            object? roleId;
            using (var role = SqlHelper.Cmd(
                "SELECT id FROM tally_roles WHERE lower(role_name) LIKE 'admin%' ORDER BY id LIMIT 1", conn, tx))
                roleId = await role.ExecuteScalarAsync();
            if (roleId == null || roleId == DBNull.Value) return ActivationStatus.AdminRoleMissing;

            // 4. subscription header: trial window, ONE company
            long subscriptionId;
            using (var sub = SqlHelper.Cmd(
                @"INSERT INTO tally_sync_subscription
                      (companyname, emailid, subscription_fromdate, subscription_todate, createdby,
                       purchasecompanycnt, usertoken, oauth_config_id)
                  VALUES (@company, @email, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + (@days * interval '1 day'), @by,
                          1, '', NULL)
                  RETURNING id",
                conn, tx, ("company", company), ("email", email), ("days", trialDays), ("by", by)))
                subscriptionId = Convert.ToInt64(await sub.ExecuteScalarAsync());

            // 5. the company typed on the trial form becomes the first (default, active) company
            long companyId;
            using (var dtl = SqlHelper.Cmd(
                @"INSERT INTO tally_sync_subscription_dtl
                      (fk_id, createdby, companyname, isdefault, activdated, activateddate, activatedby, deactivated)
                  VALUES (@fk, @by, @company, true, true, CURRENT_TIMESTAMP, @by, false)
                  RETURNING id",
                conn, tx, ("fk", subscriptionId), ("by", by), ("company", company)))
                companyId = Convert.ToInt64(await dtl.ExecuteScalarAsync());

            // 6. the user. These are exactly the fields tallywebapp's Login needs:
            //    lower(email) = username, BCrypt password_hash, istallysync = 'y', and ismainadmin = true
            //    (Login throws "Main admin not found" when the subscription has no main admin).
            //    pwd_chng_date is set so the "Activate" link flow treats the password as already chosen.
            long userId;
            using (var usr = SqlHelper.Cmd(
                @"INSERT INTO user_management
                      (user_id, subscription_id, email, mobile_number, location, role_id, status, created_by, created_on,
                       password_hash, pwd_chng_date, istallysync, ismainadmin)
                  VALUES (@email, @sub, @email, @phone, @city, @role, 'Active', @by, CURRENT_TIMESTAMP,
                          @pwd, CURRENT_TIMESTAMP, 'y', true)
                  RETURNING id",
                conn, tx, ("email", email), ("sub", subscriptionId), ("phone", phone), ("city", city),
                ("role", Convert.ToInt32(roleId)), ("by", by), ("pwd", appPasswordHash)))
                userId = Convert.ToInt64(await usr.ExecuteScalarAsync());

            // 7. user -> company link (the user sees only this company)
            using (var link = SqlHelper.Cmd(
                "INSERT INTO user_companies (user_id, company_id) VALUES (@u, @c) ON CONFLICT DO NOTHING",
                conn, tx, ("u", userId), ("c", companyId)))
                await link.ExecuteNonQueryAsync();

            // 8. website sign-up row: active + links to what was created
            using (var update = SqlHelper.Cmd(
                @"UPDATE site_trial_signup
                     SET status = 'active', password_hash = @hash, otp_hash = NULL, otp_expires_at = NULL, otp_attempts = 0,
                         trial_start = now(), trial_end = now() + (@days * interval '1 day'), activated_at = now(),
                         app_subscription_id = @sub, app_user_id = @usr, app_company_id = @cmp
                   WHERE signup_id = @id AND status = 'pending'",
                conn, tx, ("hash", passwordHash), ("days", trialDays), ("sub", subscriptionId), ("usr", userId),
                ("cmp", companyId), ("id", signupId)))
                await update.ExecuteNonQueryAsync();

            using (var activity = SqlHelper.Cmd(
                "INSERT INTO site_activity (activity_type, reference_id, detail) VALUES ('trial_activated', @id, @detail)",
                conn, tx, ("id", signupId), ("detail", $"{trialDays}-day trial started – subscription {subscriptionId}, user {userId}")))
                await activity.ExecuteNonQueryAsync();

            return ActivationStatus.Activated;
        });
}
