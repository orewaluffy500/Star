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
        SourcePos start = Current.Pos;
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

        return result.Success(lhs!.Repos(start));
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

        
        else if (token.Keyword("rd") || token.Keyword("rdn"))
        {
            Next();
            return result.Success(
                new ReadNode(token.Keyword("rdn")).Repos(token.Pos)
            );
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

    public ParseResult CompExpr()
    {
        ParseResult result = new();
        Token token = Current;

        if (token.Keyword("not"))
        {
            Next();
            var rhs = result.Register(CompExpr());
            if (result.HasError()) return result;

            return result.Success(new UnaryOpNode(token, rhs!).Repos(token.Pos));
        }

        return BinOp(ArithExpr, [TokenType.LT, TokenType.GT, TokenType.LTE, TokenType.GTE, TokenType.EE]);
    }

    public ParseResult LogicalExpr()
    {
        ParseResult result = new();
        SourcePos start = Current.Pos;
        var lhs = result.Register(CompExpr());
        if (result.HasError()) return result;

        string[] keywords = ["and", "or"];

        while (Current.Type == TokenType.Keyword && keywords.Contains(Current.Value))
        {
            Token operToken = Current;
            Next();

            var rhs = result.Register(CompExpr());
            if (result.HasError()) return result;

            lhs = new BinOpNode(operToken, rhs!, lhs!);
        }

        return result.Success(lhs!.Repos(start));
    }

    public ParseResult KeywordExpr()
    {
        ParseResult result = new();
        Token token = Current;

        if (token.Keyword("wrn") || token.Keyword("wr"))
        {
            Next();
            var value = result.Register(Expr());
            if (result.HasError()) return result;

            return result.Success(
                new PrintNode(token.Keyword("wrn"), value!).Repos(token.Pos)
            );
        }

        return LogicalExpr();
    }

    public ParseResult Expr()
    {
        ParseResult result = new();
        Token token = Current;

        if (token.Type == TokenType.LBrace)
        {
            Next();
            var node = result.Register(Body());
            if (result.HasError()) return result;

            return result.Success(node!.Repos(token.Pos));
        }
        return BinOp(KeywordExpr, [TokenType.Equals], Expr);
    }

    public ParseResult Body() => PlainBody(TokenType.RBrace);

    public ParseResult PlainBody(TokenType end = TokenType.End)
    {
        ParseResult result = new();
        List<ASTNode> body = [];
        SourcePos start = Current.Pos;

        while (Current.Type != end)
        {
            EatLines();
            if (Current.Type == end) break;
            var node = result.Register(Expr());
            if (result.HasError()) return result;

            body.Add(node!);
            if (Current.Type != TokenType.Newline && Current.Type != end) return result.Failure(new StSyntaxError("expected newline!", Current.Pos));
        }

        Next();

        return result.Success(
            new BodyNode([..body]).Repos(start)
        );
    }
    
    
    
    void EatLines() { while (Current.Type == TokenType.Newline) Next(); }
}