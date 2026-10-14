using star.error;
using star.value;

namespace star;


public enum RTFlow
{
    Continue, Break, Regular
}

public class RTResult
{
    public StValue Value = new StNull();
    public RTFlow Flow = RTFlow.Regular;
    public BaseError? Error = null;

    public bool HasError() => Error is not null;
    public bool IrregularFlow() => Flow != RTFlow.Regular;
    public bool IsAbnormal() => HasError() || IrregularFlow();

    public StValue Register(RTResult other)
    {
        if (other.HasError()) Error = other.Error;
        if (other.IrregularFlow()) Flow = other.Flow;
        return other.Value;
    }

    public RTResult Success(StValue value, RTFlow? flow = null)
    {
        Error = null;
        Value = value;
        Flow = flow ?? Flow;
        return this;
    }

    public RTResult Failure(BaseError error, RTFlow? flow = null)
    {
        Error = error;
        Flow = flow ?? Flow;
        return this;
    }
}