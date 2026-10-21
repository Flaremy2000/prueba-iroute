namespace IRoute.Domain.Exceptions;

public sealed class DomainConflictException : Exception
{
    public DomainConflictException(string message) : base(message)
    {
    }
}
