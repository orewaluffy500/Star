using star.ast;
using star.error;

namespace star;


public class Parser(string fileName, Token[] tokens)
{
    public SourcePos Pos = new(fileName);
    public Token[] Tokens = tokens;
    public Token Current = tokens.Last();
    public bool IsEnd { get => Current.Type == TokenType.End; }

    public void Next()
    {
        Pos.Next(Current.Type == TokenType.Newline);
        Current = Pos.Index < Tokens.Length ? Tokens[Pos.Index] : Tokens.Last();
    }

    // RULES

    public ParseResult BinOp(Func<ParseResult> factory, TokenType[] expected, Func<ParseResult>? factory2 = null)
    {
        ParseResult result = new();
        var lhs = result.Register(factory());
        if (result.HasError()) return result;

        while (expected.Contains(Current.Type))
        {
            Token operToken = Current;
            Next();

            var rhs = result.Register(factory2 is null ? factory() : factory2());
            if (result.HasError()) return result;

            lhs = new BinOpNode(operToken, rhs!, lhs!);
        }

        return result.Success(lhs!);
    }

    public ParseResult Factor()
    {
        ParseResult result = new();
        Token token = Current;

        if (token.Type == TokenType.Int)
        {
            Next();
            return result.Success(new IntNode(token).Repos(token.Pos));
        }

        else if (token.Type == TokenType.String)
        {
            Next();
            return result.Success(new StringNode(token).Repos(token.Pos));
        }

        else if (new TokenType[]{ TokenType.Dollar, TokenType.Plus, TokenType.Minus, TokenType.QuesMark }.Contains(token.Type))
        {
            Next();
            var rhs = result.Register(Factor());
            if (result.HasError()) return result;

            return result.Success(
                new UnaryOpNode(token, rhs!).Repos(token.Pos)
            );
        }

        else if (token.Type == TokenType.LParen)
        {
            Next();
            var expr = result.Register(Expr());
            if (result.HasError()) return result;

            if (Current.Type != TokenType.RParen)
            {
                return result.Failure(new StSyntaxError("expected ')'!", Current.Pos));
            }
            Next();

            return result.Success(expr!);
        }

        return result.Failure(new StSyntaxError("expected expression!", token.Pos));
    }

    public ParseResult Term()
    {
        return BinOp(Factor, [TokenType.Mul, TokenType.Div]);
    }

    public ParseResult ArithExpr()
    {
        return BinOp(Term, [TokenType.Plus, TokenType.Minus]);
    }

    public ParseResult AssignExpr()
    {
        return BinOp(ArithExpr, [TokenType.Arrow], Expr);
    }

    public ParseResult Expr()
    {
        ParseResult result = new();
        Token token = Current;

        var node = result.Register(AssignExpr());
        if (result.HasError()) return result;

        return result.Success(node!);
    }

    public ParseResult Body(bool root = false)
    {
        ParseResult result = new();
        List<ASTNode> body = [];
        SourcePos start = Current.Pos;

        void EatLines() { while (Current.Type == TokenType.Newline) Next(); }

        if (Current.Type != TokenType.LBrace && !root) return result.Failure(new StSyntaxError("expected '{'!", Current.Pos));
        if (!root) Next();

        // if its root then run while it isn't EOF otherwise run while it isn't '}'
        while (!(root ? IsEnd : Current.Type != TokenType.RBrace))
        {
            EatLines();
            var node = result.Register(Expr());
            if (result.HasError()) return result;

            body.Add(node!);
            if (Current.Type != TokenType.Newline && Current.Type != TokenType.End) return result.Failure(new StSyntaxError("expected newline!", Current.Pos));
        }

        return result.Success(
            new BodyNode([..body]).Repos(start)
        );
    }
}