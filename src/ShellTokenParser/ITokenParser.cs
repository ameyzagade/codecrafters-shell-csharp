public interface ITokenParser
{
    ShellCommand Parse(IReadOnlyList<Token> tokens);
}
