using System.Text.RegularExpressions;
using TestNest.Admin.SharedLibrary.Common.Guards;
using TestNest.Admin.SharedLibrary.ValueObjects.Common;

namespace TestNest.Admin.SharedLibrary.ValueObjects;

public sealed partial class EmailAddress : ValueObject
{
    private static readonly Regex EmailRegex = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

    private static readonly Regex DomainRegex = DomainPattern();

    public string Email { get; }
    private static readonly Lazy<EmailAddress> _empty = new(() => new EmailAddress());

    public static EmailAddress Empty() => _empty.Value;

    public bool IsEmpty() => this == Empty();

    private EmailAddress() => Email = string.Empty;

    private EmailAddress(string value) => Email = value;

    private static Result ValidateEmail(string email)
    {
        var validations = new List<Result>
        {
            Guard.AgainstNullOrWhiteSpace(email,
            static () => EmailAddressException.InvalidFormat())
        };
        if (email != null)
        {
            validations.Add(Guard.AgainstCondition(email.Count(c => c == '@') != 1,
                static () => EmailAddressException.InvalidFormat()));
            validations.Add(Guard.AgainstCondition(!EmailRegex.IsMatch(email),
                static () => EmailAddressException.InvalidFormat()));
            if (EmailRegex.IsMatch(email))
            {
                string domain = email.Split('@')[1];
                validations.Add(Guard.AgainstCondition(!DomainRegex.IsMatch(domain),
                    static () => EmailAddressException.InvalidFormat()));
            }
        }
        return Result.Combine([.. validations]);
    }

    public static Result<EmailAddress> Create(string email)
    {
        Result validationResult = ValidateEmail(email);
        return validationResult.IsSuccess
            ? Result<EmailAddress>.Success(new EmailAddress(email!))
            : Result<EmailAddress>.Failure(ErrorType.Validation, validationResult.Errors);
    }

    public Result<EmailAddress> Update(string newEmail) => Create(newEmail);

    public static Result<EmailAddress> TryParse(string email) => Create(email);

    protected override IEnumerable<object?> GetAtomicValues() => [Email];

    public override string ToString() => Email;
    [GeneratedRegex(@"^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z0-9][a-z0-9-]{0,61}[a-z0-9]$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-PH")]
    private static partial Regex DomainPattern();
}
