namespace MARS_BE.Common.Errors;

public abstract class AppException : Exception
{
    public int Status { get; }
    public string Title { get; }

    protected AppException(int status, string title, string message)
        : base(message)
    {
        Status = status;
        Title  = title;
    }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message)
        : base(409, "Conflict", message) { }
}

public sealed class NotFoundAppException : AppException
{
    public NotFoundAppException(string message)
        : base(404, "Not Found", message) { }
}