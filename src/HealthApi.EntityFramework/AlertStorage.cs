using HealthApi.Domain;

namespace HealthApi.EntityFramework;

public class AlertStorage(HealthApiDbContext db)
{
    public async Task CreateAsync(string patientIdentifier, string severity, string message, CancellationToken ct)
    {
        db.HealthAlerts.Add(new HealthAlert
        {
            PatientIdentifier = patientIdentifier,
            Severity = severity,
            Message = message,
        });
        await db.SaveChangesAsync(ct);
    }
}
