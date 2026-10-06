namespace star.error;

public record BaseError(string Header, string Details, SourcePos Pos)
{
    public string Prettify()
    {
        return $"{Header}\n-- what: {Details}\n-- where: line {Pos.Row}, col {Pos.Col} of file '{Pos.FileName}'";
    }
}

public record StSyntaxError(string Details, SourcePos Pos) : BaseError("invalid syntax", Details, Pos){}
public record StIllegalOperError(string Details, SourcePos Pos) : BaseError("illegal operation error", Details, Pos){}
public record StUnexpectedNodeError(string Details, SourcePos Pos) : BaseError("unexpected ast node error", Details, Pos){}
public record StRuntimeError(string Details, SourcePos Pos) : BaseError("runtime error", Details, Pos){}