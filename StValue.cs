using star.error;
using star.interp;

namespace star.value;


public abstract class StValue
{
    public static string LabelOf(Type type)
    {
        return type.Name switch
        {
            nameof(StValue) => "any",
            _ => "unknown"
        };
    }

    public SourcePos Pos = new("<undefined>");
    public abstract override string ToString();

    public Tuple<StValue?, BaseError?> IllegalOper(string name) => new(null, new StIllegalOperError($"illegal operation '{name}' on value of type '{LabelOf(GetType())}'", Pos));
    public StValue Repos(SourcePos new_)
    {
        Pos = new_;
        return this;
    }

    public virtual Tuple<StValue?, BaseError?> AddTo(StValue other) => IllegalOper("add");
    public virtual Tuple<StValue?, BaseError?> SubBy(StValue other) => IllegalOper("sub");
    public virtual Tuple<StValue?, BaseError?> MulBy(StValue other) => IllegalOper("mul");
    public virtual Tuple<StValue?, BaseError?> DivBy(StValue other) => IllegalOper("div");
}

public class StInt(int value) : StValue
{
    public int Value = value;

    public override string ToString()
    {
        return $"{Value}";
    }

    public override Tuple<StValue?, BaseError?> AddTo(StValue other)
    {
        if (other is not StInt i) return IllegalOper("add");
        return new(new StInt(Value + i.Value), null);
    }

    public override Tuple<StValue?, BaseError?> SubBy(StValue other)
    {
        if (other is not StInt i) return IllegalOper("add");
        return new(new StInt(Value - i.Value), null);
    }

    public override Tuple<StValue?, BaseError?> MulBy(StValue other)
    {
        if (other is not StInt i) return IllegalOper("add");
        return new(new StInt(Value * i.Value), null);
    }

    public override Tuple<StValue?, BaseError?> DivBy(StValue other)
    {
        if (other is not StInt i) return IllegalOper("add");
        return new(new StInt(Value / i.Value), null);
    }
}

public class StString(string value) : StValue
{
    public string Value = value;

    public override string ToString()
    {
        return $"{Value}";
    }
}

public class StNull : StValue
{
    public override string ToString()
    {
        return "null";
    }
}

public abstract class StReference(Interpreter interpreter) : StValue
{
    public Interpreter Interpreter = interpreter;

    public abstract Tuple<StValue?, BaseError?> Get();
    public abstract BaseError? Set(StValue value);
    public abstract override string ToString();
}

public class StRegister(int index, Interpreter interpreter) : StReference(interpreter)
{
    public int Index = index;

    public override Tuple<StValue?, BaseError?> Get()
    {
        StValue value = Interpreter.Memory.GetRegister(Index, Pos, out var error);
        if (error is not null) return new(null, error);

        return new(value, null);
    }

    public override BaseError? Set(StValue value)
    {
        return Interpreter.Memory.SetRegister(Index, ((StInt) value).Value, Pos);
    }

    public override string ToString()
    {
        return $"<reg {Index}>";
    }
}