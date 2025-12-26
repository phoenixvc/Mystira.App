namespace Mystira.App.Domain.Exceptions;

/// <summary>
/// Exception thrown when a business rule is violated.
/// Maps to HTTP 422 Unprocessable Entity.
/// </summary>
public class BusinessRuleException : DomainException
{
    public string RuleName { get; }

    public BusinessRuleException(string ruleName, string message)
        : base(message, "BUSINESS_RULE_VIOLATION",
            new Dictionary<string, object>
            {
                ["ruleName"] = ruleName
            })
    {
        RuleName = ruleName;
    }

    public BusinessRuleException(string ruleName, string message, IDictionary<string, object> context)
        : base(message, "BUSINESS_RULE_VIOLATION",
            new Dictionary<string, object>(context)
            {
                ["ruleName"] = ruleName
            })
    {
        RuleName = ruleName;
    }
}
