using star.error;
using star.value;

namespace star;


public class RTResult
{
    public StValue Value = new StNull();
    public BaseError? Error = null;

    public bool HasError() => Error is not null;

    public StValue Register(RTResult other)
    {
        if (other.HasError()) Error = other.Error;
        return other.Value;
    }

    public RTResult Success(StValue value)
    {
        Error = null;
        Value = value;
        return this;
    }

    public RTResult Failure(BaseError error)
    {
        Error = error;
        return this;
    }
}