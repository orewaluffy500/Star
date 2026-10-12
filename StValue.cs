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
            nameof(StInt) => "int",
            nameof(StString) => "str",
            nameof(StNull) => "null",
            nameof(StReference) => "reference",
            nameof(StRegister) => "register_reference",
            _ => "unknown"
        };
    }

    public SourcePos Pos = new("<undefined>");
    public abstract override string ToString();
    public abstract StValue Copy();

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
    public virtual Tuple<StValue?, BaseError?> Notted() => IllegalOper("not");
    public virtual bool AndBy(StValue other) => false;
    public virtual bool OrBy(StValue other) => false;
    public virtual bool LessThan(StValue other) => false;
    public virtual bool MoreThan(StValue other) => false;
    public virtual bool EqualTo(StValue other) => false;
    public virtual bool LessOrEqualTo(StValue other) => LessThan(other) || EqualTo(other);
    public virtual bool MoreOrEqualTo(StValue other) => MoreThan(other) || EqualTo(other);
    public virtual bool IsTrue() => false;
}

public class StInt(int value) : StValue
{
    public int Value = value;
    public static StInt FromBool(bool b) => new(b ? 1 : 0);

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

    public override Tuple<StValue?, BaseError?> Notted()
    {
        return new(FromBool(Value == 0), null);
    }

    public override bool AndBy(StValue other)
    {
        return other.IsTrue() && IsTrue();
    }

    public override bool OrBy(StValue other)
    {
        return other.IsTrue() || IsTrue();
    }

    public override bool IsTrue()
    {
        return Value != 0;
    }

    public override bool LessThan(StValue other)
    {
        return other is StInt i && Value < i.Value;
    }

    public override bool MoreThan(StValue other)
    {
        return other is StInt i && Value > i.Value;
    }

    public override bool EqualTo(StValue other)
    {
        return other is StInt i && Value == i.Value;
    }

    public override StValue Copy()
    {
        return new StInt(Value).Repos(Pos);
    }
}

public class StString(string value) : StValue
{
    public string Value = value;

    public override StValue Copy()
    {
        return new StString(Value).Repos(Pos);
    }

    public override bool IsTrue()
    {
        return Value.Length > 0;
    }

    public override bool EqualTo(StValue other)
    {
        if (other is not StString s) return false;
        return Value == s.Value;
    }

    public override string ToString()
    {
        return $"{Value}";
    }
}

public class StNull : StValue
{
    public override StValue Copy()
    {
        return new StNull().Repos(Pos);
    }

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

    public override StValue Copy()
    {
        return new StRegister(Index, Interpreter).Repos(Pos);
    }

    public override Tuple<StValue?, BaseError?> Get()
    {
        StValue value = Interpreter.Memory.GetRegister(Index, Pos, out var error);
        if (error is not null) return new(null, error);

        return new(value, null);
    }

    public override BaseError? Set(StValue value)
    {
        return Interpreter.Memory.SetRegister(Index, value.Copy(), Pos);
    }

    public override string ToString()
    {
        return $"<reg {Index}>";
    }
}