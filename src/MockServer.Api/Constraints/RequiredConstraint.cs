namespace MockServer.Api.Constraints;

public class RequiredConstraint : IConstraint
{
    public bool Match(object value, out string message)
    {
        throw new NotImplementedException();
    }
}
