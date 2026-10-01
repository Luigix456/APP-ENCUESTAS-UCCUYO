namespace AcademicSurveySystem.Domain.Common;

public sealed class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string? Code { get; }
}
