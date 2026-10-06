using star.constants;
using star.error;

namespace star;


public class Lexer(string source, string fileName)
{
    public string Source = source;
    public SourcePos Pos = new(fileName);
    public char Current = '\0';
    public bool IsEnd { get => Current == '\0'; }
    public void Next()
    {
        Pos.Next(Current == '\n');
        Current = Pos.Index < Source.Length ? Source[Pos.Index] : '\0';
    }

    public Tuple<Token[], BaseError?> Tokenize()
    {
        List<Token> tokens = [];
        Next(); // update once

        while (!IsEnd)
        {
            switch (Current)
            {
                case char c when " \t\v\r".Contains(c):
                    Next();
                    break;
                
                case '~':
                    while (!IsEnd && Current != '\n') Next();
                    break;
                
                case '+':
                    tokens.Add(new(TokenType.Plus, Pos));
                    Next();
                    break;
                
                case '-':
                    SourcePos start = Pos;
                    Next();
                    if (Current == '>')
                    {
                        Next();
                        tokens.Add(new(TokenType.Arrow, start));
                    } else {
                        tokens.Add(new(TokenType.Minus, start));
                    }
                    break;
                
                case '*':
                    tokens.Add(new(TokenType.Mul, Pos));
                    Next();
                    break;
                
                case '/':
                    tokens.Add(new(TokenType.Div, Pos));
                    Next();
                    break;
                
                case '.':
                    tokens.Add(new(TokenType.Dot, Pos));
                    Next();
                    break;
                
                case '#':
                    tokens.Add(new(TokenType.Hash, Pos));
                    Next();
                    break;
                
                case '$':
                    tokens.Add(new(TokenType.Dollar, Pos));
                    Next();
                    break;
                
                case '^':
                    tokens.Add(new(TokenType.Pow, Pos));
                    Next();
                    break;
                
                case '?':
                    tokens.Add(new(TokenType.QuesMark, Pos));
                    Next();
                    break;
                
                case ':':
                    tokens.Add(new(TokenType.Colon, Pos));
                    Next();
                    break;
                
                case ',':
                    tokens.Add(new(TokenType.Comma, Pos));
                    Next();
                    break;
                
                case '%':
                    tokens.Add(new(TokenType.Mod, Pos));
                    Next();
                    break;
                
                case '(':
                    tokens.Add(new(TokenType.LParen, Pos));
                    Next();
                    break;
                
                case ')':
                    tokens.Add(new(TokenType.RParen, Pos));
                    Next();
                    break;
                
                case '{':
                    tokens.Add(new(TokenType.LBrace, Pos));
                    Next();
                    break;
                
                case '}':
                    tokens.Add(new(TokenType.RBrace, Pos));
                    Next();
                    break;
                
                case '[':
                    tokens.Add(new(TokenType.LSqBrace, Pos));
                    Next();
                    break;
                
                case ']':
                    tokens.Add(new(TokenType.RSqBrace, Pos));
                    Next();
                    break;
                
                case '\n':
                    tokens.Add(new(TokenType.Newline, Pos));
                    Next();
                    break;
                
                case '=':
                    Next();
                    if (Current == '=')
                    {
                        tokens.Add(new(TokenType.EE, Pos));
                        Next();
                    } else {
                        tokens.Add(new(TokenType.Equals, Pos));
                    }
                    break;
                
                case '<':
                    Next();
                    if (Current == '=')
                    {
                        tokens.Add(new(TokenType.LTE, Pos));
                        Next();
                    } else {
                        tokens.Add(new(TokenType.LT, Pos));
                    }
                    break;
                
                case '>':
                    Next();
                    if (Current == '=')
                    {
                        tokens.Add(new(TokenType.GTE, Pos));
                        Next();
                    } else {
                        tokens.Add(new(TokenType.GT, Pos));
                    }
                    break;
                
                case '!':
                    Next();
                    if (Current == '=')
                    {
                        tokens.Add(new(TokenType.NE, Pos));
                        Next();
                    } else {
                        tokens.Add(new(TokenType.ExclMark, Pos));
                    }
                    break;
                
                case '"':
                    var (tok, err) = TokenizeString();
                    if (err is not null) return new([], err);

                    tokens.Add(tok!);
                    break;
                
                case char c when char.IsDigit(c):
                    tokens.Add(TokenizeInteger()); 
                    break;

                case char c when c == '_' || char.IsLetter(c):
                    tokens.Add(TokenizeIdentifier());
                    break;
                
                default:
                    return new([], new StSyntaxError($"unexpected character '{Current}'!", Pos));
            }
        }

        tokens.Add(new Token(TokenType.End, Pos));
        return new([..tokens], null);
    }   

    public Tuple<Token?, BaseError?> TokenizeString()
    {
        string s = "";
        SourcePos start = Pos;
        Next(); // go past left quote

        while (!IsEnd)
        {
            if (Current == '\n') return new(null, new StSyntaxError("unterminated string!", Pos));

            // handle escape chars
            else if (Current == '\\')
            {
                Next();
                switch (Current)
                {
                    case 'n':
                        s += '\n';
                        Next();
                        break;
                    
                    case 't':
                        s += '\t';
                        Next();
                        break;
                    
                    case '\\':
                        s += '\\';
                        Next();
                        break;
                    
                    case '"':
                        s += '\"';
                        Next();
                        break;
                    case '\'':
                        s += '\'';
                        Next();
                        break;
                    default:
                        return new(null, new StSyntaxError($"unexpected escape char '\\{Current}'!", Pos));
                }
            }

            if (Current == '"') break;

            s += Current;
            Next();
        }

        Next(); // go past right quote
        return new(new(TokenType.String, start, s), null);
    }

    public Token TokenizeInteger()
    {
        string i = "";
        SourcePos start = Pos;

        while (!IsEnd && (char.IsDigit(Current) || Current == '_'))
        {
            if (Current == '_')
            {
                Next();
                continue;
            }

            i += Current;
            Next();
        }

        return new(TokenType.Int, start, Convert.ToInt32(i));
    }

    public Token TokenizeIdentifier()
    {
        string s = "";
        SourcePos start = Pos;

        while (!IsEnd && (char.IsLetterOrDigit(Current) || Current == '_'))
        {
            s += Current;
            Next();
        }

        TokenType type = KeywordClass.Keywords.Contains(s) ? TokenType.Keyword : TokenType.Ident;
        return new(type, start, s);
    }
}