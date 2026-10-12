using star.ast;
using star.error;
using star.memory;
using star.value;

namespace star.interp;


public class Interpreter
{
    public Memory Memory = new();


    public static T? Expect<T>(StValue value, SourcePos site, out BaseError? error) where T: StValue
    {
        if (value is T v){
            error = null;
            return v;
        }

        string expected = StValue.LabelOf(typeof(T));
        string given = StValue.LabelOf(value.GetType());

        string message = $"expected value of type '{expected}', got '{given}' instead!";
        error = new StRuntimeError(message, site);
        return null;
    }

    public RTResult Visit(ASTNode node)
    {
        RTResult result = new();

        return node switch
        {
            IntNode intNode => VisitInt(intNode),
            StringNode stringNode => VisitString(stringNode),
            BodyNode bodyNode => VisitBody(bodyNode),
            UnaryOpNode unaryOpNode => VisitUnaryOp(unaryOpNode),
            BinOpNode binOpNode => VisitBinOp(binOpNode),
            PrintNode printNode => VisitPrint(printNode),
            ReadNode readNode => VisitRead(readNode),
            IfNode ifNode => VisitIf(ifNode),
            _ => result.Failure(new StUnexpectedNodeError($"unexpected ast node: '{node.GetType().Name}'!", node.Pos))
        };
    }

    public RTResult VisitBody(BodyNode node)
    {
        RTResult result = new();
        StValue finalValue = new StNull().Repos(node.Pos);

        foreach (ASTNode subNode in node.Body)
        {
            StValue value = result.Register(Visit(subNode));
            if (result.HasError()) return result;

            finalValue = value;
        }

        return result.Success(finalValue);
    }

    public RTResult VisitInt(IntNode node)
    {
        return new RTResult().Success(new StInt(node.Value.Value as int? ?? 0).Repos(node.Pos));
    }

    public RTResult VisitString(StringNode node)
    {
        return new RTResult().Success(new StString(node.Value.Value as string ?? "").Repos(node.Pos));
    }

    public RTResult VisitUnaryOp(UnaryOpNode node)
    {
        RTResult result = new();

        StValue rhs = result.Register(Visit(node.Rhs));
        if (result.HasError()) return result;

        Token op = node.OperToken;

        if (op.Type == TokenType.Minus)
        {
            var (value, error) = rhs.MulBy(new StInt(-1));
            if (error is not null) return result.Failure(error);

            return result.Success(value!.Repos(node.Pos));
        }

        else if (op.Type == TokenType.Dollar)
        {
            var index = Expect<StInt>(rhs, rhs.Pos, out var e);
            if (e is not null) return result.Failure(e);

            return result.Success(new StRegister(index!.Value, this).Repos(node.Pos));
        }
        
        else if (op.Type == TokenType.QuesMark)
        {
            var reference = Expect<StReference>(rhs, rhs.Pos, out var e);
            if (e is not null) return result.Failure(e);

            var (value, error) = reference!.Get();
            if (error is not null) return result.Failure(error);

            return result.Success(value!.Repos(node.Pos));
        }

        else if (op.Keyword("not"))
        {
            var (value, error) = rhs.Notted();
            if (error is not null) return result.Failure(error);

            return result.Success(value!.Repos(node.Pos));
        }

        return result.Success(rhs.Repos(node.Pos));
    }

    public RTResult VisitBinOp(BinOpNode node)
    {
        RTResult result = new();

        StValue rhs = result.Register(Visit(node.Rhs));
        if (result.HasError()) return result;

        StValue lhs = result.Register(Visit(node.Lhs));
        if (result.HasError()) return result;

        Token op = node.OperToken;

        RTResult handleOp(Func<StValue, Tuple<StValue?, BaseError?>> factory)
        {
            var (value, error) = factory(rhs);
            if (error is not null) return result.Failure(error);
            return result.Success(value!.Repos(node.Pos));
        }

        RTResult handleCompOp(Func<StValue, bool> factory){
            return result.Success(StInt.FromBool(factory(rhs)).Repos(node.Pos));
        }

        if (op.Type == TokenType.Equals)
        {
            var reference = Expect<StReference>(lhs, lhs.Pos, out var e);
            if (e is not null) return result.Failure(e);

            if (e is not null) return result.Failure(e);

            var error = reference!.Set(rhs!);
            if (error is not null) return result.Failure(error);
            return result.Success(rhs!.Copy().Repos(node.Pos));
        }

        if (op.Type == TokenType.Keyword)
        {
            return (op.Value as string ?? "") switch
            {
                "and" => handleCompOp(lhs.AndBy),
                "or" => handleCompOp(lhs.OrBy),
                _ => result.Failure(new StIllegalOperError($"illegal operation: {StValue.LabelOf(lhs.GetType())} {op} {StValue.LabelOf(rhs.GetType())}", node.Pos))
            };
        }

        return op.Type switch
        {
            TokenType.Plus  => handleOp(lhs.AddTo),
            TokenType.Minus => handleOp(lhs.SubBy),
            TokenType.Mul   => handleOp(lhs.MulBy),
            TokenType.Div   => handleOp(lhs.DivBy),
            TokenType.LT    => handleCompOp(lhs.LessThan),
            TokenType.GT    => handleCompOp(lhs.MoreThan),
            TokenType.LTE   => handleCompOp(lhs.LessOrEqualTo),
            TokenType.GTE   => handleCompOp(lhs.MoreOrEqualTo),
            TokenType.EE    => handleCompOp(lhs.EqualTo),
            _ => result.Failure(new StIllegalOperError($"illegal operation: {StValue.LabelOf(lhs.GetType())} {op} {StValue.LabelOf(rhs.GetType())}", node.Pos))
        };
    }

    public RTResult VisitPrint(PrintNode node)
    {
        RTResult result = new();
        var value = result.Register(Visit(node.Value));
        if (result.HasError()) return result;

        Console.Write(value);
        if (node.Newline) Console.WriteLine();

        return result.Success(value);
    }

    public RTResult VisitRead(ReadNode node)
    {
        RTResult result = new();
        string value = Console.ReadLine() ?? "";

        if (node.Newline)
        {
            return result.Success(
                new StString(value).Repos(node.Pos)
            );
        }

        return result.Success(
            new StString(value.Split(' ')[0]).Repos(node.Pos)
        );
    }

    public RTResult VisitIf(IfNode node)
    {
        RTResult result = new();
        StValue final = new StNull().Repos(node.Pos);
        bool succesfulCaseFound = false;

        foreach (IfCase ifCase in node.Cases)
        {
            var output = result.Register(Visit(ifCase.Expr));
            if (result.HasError()) return result;

            if (!output.IsTrue()) continue;

            final = result.Register(Visit(ifCase.Body));
            if (result.HasError()) return result;

            succesfulCaseFound = true;
            break;
        }

        if (!succesfulCaseFound && node.ElseCase is not null)
        {
            final = result.Register(Visit(node.ElseCase));
            if (result.HasError()) return result;
        }

        return result.Success(final);
    }
}