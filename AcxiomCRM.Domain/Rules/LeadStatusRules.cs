using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Domain.Rules;

public static class LeadStatusRules
{
    private static readonly Dictionary<LeadStatus, HashSet<LeadStatus>> AllowedTransitions = new()
    {
        [LeadStatus.New] = new HashSet<LeadStatus> { LeadStatus.Contacted, LeadStatus.Qualified, LeadStatus.Unqualified, LeadStatus.Lost },
        [LeadStatus.Contacted] = new HashSet<LeadStatus> { LeadStatus.Qualified, LeadStatus.Unqualified, LeadStatus.Lost, LeadStatus.Contacted },
        [LeadStatus.Qualified] = new HashSet<LeadStatus> { LeadStatus.Converted, LeadStatus.Lost, LeadStatus.Contacted, LeadStatus.Qualified },
        [LeadStatus.Unqualified] = new HashSet<LeadStatus> { LeadStatus.Contacted, LeadStatus.Qualified, LeadStatus.Lost, LeadStatus.Unqualified },
        [LeadStatus.Converted] = new HashSet<LeadStatus> { LeadStatus.Converted }, // Terminal
        [LeadStatus.Lost] = new HashSet<LeadStatus> { LeadStatus.Contacted, LeadStatus.Qualified, LeadStatus.Lost }
    };

    public static bool CanTransition(LeadStatus currentStatus, LeadStatus targetStatus)
    {
        if (currentStatus == targetStatus) return true;
        return AllowedTransitions.TryGetValue(currentStatus, out var allowed) && allowed.Contains(targetStatus);
    }

    public static (bool IsValid, string? ErrorMessage) ValidateTransition(LeadStatus currentStatus, LeadStatus targetStatus)
    {
        if (currentStatus == LeadStatus.Converted && targetStatus != LeadStatus.Converted)
        {
            return (false, "Converted leads are closed and cannot change status.");
        }

        if (!CanTransition(currentStatus, targetStatus))
        {
            return (false, $"Invalid status transition from '{currentStatus}' to '{targetStatus}'.");
        }

        return (true, null);
    }
}
