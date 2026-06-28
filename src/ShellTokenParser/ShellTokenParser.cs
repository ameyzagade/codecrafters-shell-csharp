public sealed class ShellTokenParser : ITokenParser
{
    private enum ParserState
    {
        ExpectCommand,
        ExpectArgumentOrRedirect,
        ExpectRedirectionTarget
    }

    private sealed class ParserContext
    {
        public ShellCommand Command { get; } = new();
        public ParserState State { get; set; }
    }

    public ShellCommand Parse(IReadOnlyList<Token> tokens)
    {
        var context = new ParserContext
        {
            State = ParserState.ExpectCommand,
        };

        if (tokens.Count == 0)
        {
            context.Command.Command = "";
            return context.Command;
        }

        foreach (var token in tokens)
        {
            ProcessToken(context, token);
        }

        ValidateEndState(context);

        return context.Command;
    }

    private void ProcessToken(ParserContext context, Token token)
    {
        switch (context.State)
        {
            case ParserState.ExpectCommand:
                ProcessExpectCommand(context, token);
                break;
            case ParserState.ExpectArgumentOrRedirect:
                ProcessExpectArgumentOrRedirect(context, token);
                break;
            case ParserState.ExpectRedirectionTarget:
                ProcessExpectRedirectionTarget(context, token);
                break;
            default:
                throw new Exception($"Unknown parser state: {Enum.GetName(context.State)}");
        }
    }

    private void ProcessExpectCommand(ParserContext context, Token token)
    {
        if (token.Type != TokenType.Word)
        {
            throw new Exception($"Word expected in first position, got type {Enum.GetName(token.Type)} with value: {token.Value}");
        }

        context.Command.Command = token.Value;
        context.State = ParserState.ExpectArgumentOrRedirect;
    }

    private void ProcessExpectArgumentOrRedirect(ParserContext context, Token token)
    {
        if (token.Type is TokenType.Word)
        {
            context.Command.AddArgument(token.Value);

            return;
        }

        if (token.Type is TokenType.RedirectOut)
        {
            context.State = ParserState.ExpectRedirectionTarget;

            return;
        }

        throw new Exception($"Word or redirect expected after command, got type {Enum.GetName(token.Type)} with value: {token.Value}");
    }

    private void ProcessExpectRedirectionTarget(ParserContext context, Token token)
    {
        if (token.Type != TokenType.Word)
        {
            throw new Exception($"Word expected after redirect operator, got type {Enum.GetName(token.Type)} with value: {token.Value}");
        }

        context.Command.OutputRedirection = true;
        context.Command.OutputRedirectionFilePath = token.Value;

        context.State = ParserState.ExpectArgumentOrRedirect;
    }

    private void ValidateEndState(ParserContext context)
    {
        if (context.State != ParserState.ExpectArgumentOrRedirect)
        {
            throw new Exception($"The input doesn't end with an argument or redirect target");
        }
    }
}