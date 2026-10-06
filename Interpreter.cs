using star.ast;
using star.error;
using star.memory;
using star.value;

namespace star.interp;


public class Interpreter
{
    public Memory Memory = new();


    public RTResult Expect<T>(StValue value) where T: StValue
    {
        RTResult result = new();
        if (value is T v) return result.Success(v);

        string expected = StValue.LabelOf(typeof(T));
        string given = StValue.LabelOf(value.GetType());

        string message = $"expected value of type '{expected}', got '{given}' instead!";
        return result.Failure(new StRuntimeError(message, value.Pos));
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
            StInt index = (StInt) result.Register(Expect<StInt>(rhs));
            if (result.HasError()) return result;

            return result.Success(new StRegister(index.Value, this).Repos(node.Pos));
        }
        
        else if (op.Type == TokenType.QuesMark)
        {
            StReference reference = (StReference) result.Register(Expect<StReference>(rhs));
            if (result.HasError()) return result;

            var (value, error) = reference.Get();
            if (error is not null) return result.Failure(error);

            return result.Success(value!.Repos(node.Pos));
        }

        return result.Success(rhs);
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

        if (op.Type == TokenType.Arrow)
        {
            StReference reference = (StReference) result.Register(Expect<StReference>(lhs));
            if (result.HasError()) return result;

            var error = reference.Set(rhs);
            if (error is not null) return result.Failure(error);
            return result.Success(new StNull().Repos(node.Pos));
        }

        return op.Type switch
        {
            TokenType.Plus => handleOp(lhs.AddTo),
            TokenType.Minus => handleOp(lhs.SubBy),
            TokenType.Mul => handleOp(lhs.MulBy),
            TokenType.Div => handleOp(lhs.DivBy),
            _ => result.Failure(new StIllegalOperError($"illegal operation: {StValue.LabelOf(lhs.GetType())} {op} {StValue.LabelOf(rhs.GetType())}", node.Pos))
        };
    }
}