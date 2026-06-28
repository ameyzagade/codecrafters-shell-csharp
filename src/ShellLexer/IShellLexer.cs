public interface IShellLexer
{
    IReadOnlyList<Token> Tokenize(string input);
}