namespace star;

public enum TokenType
{
    String, Int, Ident, Keyword, Newline, End,
    Plus, Minus, Mul, Div, Pow, Mod, Dot, Dollar, 
    Hash, LParen, RParen, LBrace, RBrace, LSqBrace, RSqBrace, Comma, QuesMark, Colon, Arrow, ExclMark, Equals,
    GT, LT, GTE, LTE, EE, NE
};

public record Token(TokenType Type, SourcePos Pos, object? Value = null)
{
    public override string ToString()
    {
        return Value is not null ? $"({Type}:{Value})" : $"({Type})";
    }
}