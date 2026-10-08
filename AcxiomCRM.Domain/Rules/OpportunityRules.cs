using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Domain.Rules;

public static class OpportunityRules
{
    public const string AmountPositiveError = "Opportunity Amount must be greater than 0.";
    public const string ProbabilityRangeError = "Probability must be between 0 and 100.";
    public const string PastCloseDateError = "Expected Close Date cannot be in the past.";

    public static (bool IsValid, string? ErrorMessage) Validate(decimal amount, int probability, DateTime expectedCloseDate, OpportunityStatus status = OpportunityStatus.Open)
    {
        if (status == OpportunityStatus.Open && amount <= 0)
        {
            return (false, AmountPositiveError);
        }

        if (probability < 0 || probability > 100)
        {
            return (false, ProbabilityRangeError);
        }

        if (status == OpportunityStatus.Open && expectedCloseDate.Date < DateTime.UtcNow.Date)
        {
            return (false, PastCloseDateError);
        }

        return (true, null);
    }
}
