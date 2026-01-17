namespace Monoling0.NotificationService.Email.Models;

public readonly struct EmailAddress
{
    public EmailAddress(string email)
    {
        Value = email;
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(EmailAddress email) => email.Value;

    public static implicit operator EmailAddress(string email) => new(email);
}
