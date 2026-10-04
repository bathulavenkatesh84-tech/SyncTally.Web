using SyncTally.Web.Repositories.Interfaces;
using SyncTally.Web.Services.Interfaces;

namespace SyncTally.Web.Repositories.Implementations;

/// <summary>
/// Every submission is a two-step write (the record + an activity row) so both run in ONE transaction.
/// </summary>
public class ContactRepository : IContactRepository
{
    private readonly IFlexidb _db;
    public ContactRepository(IFlexidb db) => _db = db;

    public Task<(long Id, DateTime CreatedAt)> SaveContactAsync(string name, string email, string topic, string message, string? ip) =>
        _db.ExecuteTransactionAsync<(long Id, DateTime CreatedAt)>(async (conn, tx) =>
        {
            long id;
            DateTime createdAt;
            using (var insert = SqlHelper.Cmd(
                @"INSERT INTO site_contact_message (name, email, topic, message, ip_address)
                  VALUES (@name, @email, @topic, @message, @ip) RETURNING message_id, created_at",
                conn, tx, ("name", name), ("email", email), ("topic", topic), ("message", message), ("ip", ip)))
            {
                using var rd = await insert.ExecuteReaderAsync();
                await rd.ReadAsync();
                id = Convert.ToInt64(rd.GetValue(0));
                createdAt = Convert.ToDateTime(rd.GetValue(1));      // timestamptz saved by the database
            }
            using (var activity = SqlHelper.Cmd(
                "INSERT INTO site_activity (activity_type, reference_id, detail) VALUES ('contact', @id, @detail)",
                conn, tx, ("id", id), ("detail", $"{name} – {topic}")))
            {
                await activity.ExecuteNonQueryAsync();
            }
            return (id, createdAt);
        });

    public Task<long> SaveLeadAsync(string name, string phone, string topic, string? message, string? ip) =>
        _db.ExecuteTransactionAsync(async (conn, tx) =>
        {
            long id;
            using (var insert = SqlHelper.Cmd(
                @"INSERT INTO site_whatsapp_lead (name, phone, topic, message, ip_address)
                  VALUES (@name, @phone, @topic, @message, @ip) RETURNING lead_id",
                conn, tx, ("name", name), ("phone", phone), ("topic", topic), ("message", message), ("ip", ip)))
            {
                id = Convert.ToInt64(await insert.ExecuteScalarAsync());
            }
            using (var activity = SqlHelper.Cmd(
                "INSERT INTO site_activity (activity_type, reference_id, detail) VALUES ('whatsapp_lead', @id, @detail)",
                conn, tx, ("id", id), ("detail", $"{name} – {topic}")))
            {
                await activity.ExecuteNonQueryAsync();
            }
            return id;
        });

    public Task<(long Id, DateTime CreatedAt)> SaveApplicationAsync(string name, string email, string phone, string city) =>
        _db.ExecuteTransactionAsync<(long Id, DateTime CreatedAt)>(async (conn, tx) =>
        {
            long id;
            DateTime createdAt;
            using (var insert = SqlHelper.Cmd(
                @"INSERT INTO site_partner_application (name, email, phone, city)
                  VALUES (@name, @email, @phone, @city) RETURNING application_id, created_at",
                conn, tx, ("name", name), ("email", email), ("phone", phone), ("city", city)))
            {
                using var rd = await insert.ExecuteReaderAsync();
                await rd.ReadAsync();
                id = Convert.ToInt64(rd.GetValue(0));
                createdAt = Convert.ToDateTime(rd.GetValue(1));
            }
            using (var activity = SqlHelper.Cmd(
                "INSERT INTO site_activity (activity_type, reference_id, detail) VALUES ('partner_application', @id, @detail)",
                conn, tx, ("id", id), ("detail", $"{name} – {city}")))
            {
                await activity.ExecuteNonQueryAsync();
            }
            return (id, createdAt);
        });
}
