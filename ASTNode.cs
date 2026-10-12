namespace star.ast;


public abstract class ASTNode
{
    public SourcePos Pos = new("<undefined>");

    public ASTNode Repos(SourcePos pos)
    {
        Pos = pos;
        return this;
    }
    public abstract override string ToString();
}


public class BodyNode(ASTNode[] body) : ASTNode
{
    public ASTNode[] Body = body;
    public override string ToString()
    {
        return $"{{ \n  {string.Join("\n  ", Body)}\n}}";
    }
}


public class IntNode(Token value) : ASTNode
{
    public Token Value = value;

    public override string ToString()
    {
        return $"INT({Value.Value})";
    }
}

public class StringNode(Token value) : ASTNode
{
    public Token Value = value;

    public override string ToString()
    {
        return $"STR({Value.Value})";
    }
}

public class UnaryOpNode(Token operToken, ASTNode rhs) : ASTNode
{
    public Token OperToken = operToken;
    public ASTNode Rhs = rhs;

    public override string ToString()
    {
        return $"UNARY({OperToken} {Rhs})";
    }
}

public class BinOpNode(Token operToken, ASTNode rhs, ASTNode lhs) : ASTNode
{
    public Token OperToken = operToken;
    public ASTNode Rhs = rhs;
    public ASTNode Lhs = lhs;

    public override string ToString()
    {
        return $"BIN({Lhs} {OperToken} {Rhs})";
    }
}

public class PrintNode(bool newline, ASTNode value) : ASTNode
{
    public ASTNode Value = value;
    public bool Newline = newline;

    public override string ToString()
    {
        return Newline ? $"WRN({Value})" : $"WR({Value})";
    }
}

public class ReadNode(bool newline) : ASTNode
{
    public bool Newline = newline;

    public override string ToString()
    {
        return Newline ? $"RDN()" : $"RD()";
    }
}
