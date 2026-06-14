namespace X271Viewer.Domain;

public sealed class X271ParseException : Exception
{
    public X271ParseException(string message) : base(message) { }
    public X271ParseException(string message, Exception inner) : base(message, inner) { }
}
