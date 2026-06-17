namespace woliver13.X12Viewer.Application;

public sealed class X271ValidationResult
{
    public X271ValidationResult(IReadOnlyList<X271ValidationError> errors)
    {
        Errors = errors;
    }

    public bool IsValid => Errors.Count == 0;
    public IReadOnlyList<X271ValidationError> Errors { get; }
}
