using star.ast;
using star.error;

namespace star;

public class ParseResult
{
    public BaseError? Error = null;
    public ASTNode? Node = null;

    public ASTNode? Register(ParseResult other)
    {
        if (other.Error is not null) Error = other.Error;

        Node = other.Node;
        return other.Node;
    }

    public ParseResult Success(ASTNode node)
    {
        Node = node;
        Error = null;
        return this;
    }

    public ParseResult Failure(BaseError error)
    {
        Error = error;
        return this;
    }

    public bool HasError() => Error is not null;
}