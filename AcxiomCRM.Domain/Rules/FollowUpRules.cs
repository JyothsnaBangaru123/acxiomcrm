using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Domain.Rules;

public static class FollowUpRules
{
    public const string PastDateError = "Follow-up date cannot be earlier than today.";

    public static (bool IsValid, string? ErrorMessage) ValidateDate(DateTime followUpDate, FollowUpStatus status = FollowUpStatus.Planned)
    {
        if (status == FollowUpStatus.Planned && followUpDate.Date < DateTime.UtcNow.Date)
        {
            return (false, PastDateError);
        }

        return (true, null);
    }
}
