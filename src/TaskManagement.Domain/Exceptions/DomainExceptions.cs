namespace TaskManagement.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string entity, object key)
        : base($"{entity} with id '{key}' was not found.") { }
}

public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message = "Unauthorized access.")
        : base(message) { }
}

public class DuplicateTaskException : DomainException
{
    public DuplicateTaskException(string title)
        : base($"A task with title '{title}' already exists for today.") { }
}

public class ValidationException : DomainException
{
    public ValidationException(string message) : base(message) { }
}
