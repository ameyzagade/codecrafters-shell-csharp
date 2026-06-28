using System.Text;

public sealed class ShellLexer : IShellLexer
{
	private enum LexerState
	{
		Normal,
		SingleQuoted,
		DoubleQuoted
	}

	private sealed class LexerContext
	{
		public List<Token> Tokens { get; } = [];
		public StringBuilder LexemeBuffer { get; } = new();
		public LexerState State { get; set; }
		public bool EscapeNextCharacter { get; set; }
		public bool TokenStarted { get; set; }
		public string InputBuffer { get; init; }
		public int CurrentInputBufferIndex { get; set; } = 0;
	}

	public IReadOnlyList<Token> Tokenize(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			return [];
		}

		var context = new LexerContext
		{
			InputBuffer = input
		};

		for (context.CurrentInputBufferIndex = 0; context.CurrentInputBufferIndex < input.Length; context.CurrentInputBufferIndex++)
		{
			Process(context, input[context.CurrentInputBufferIndex]);
		}

		FlushToken(context);
		ValidateEndState(context);

		return context.Tokens;
	}

	private void Process(LexerContext context, char c)
	{
		switch (context.State)
		{
			case LexerState.Normal:
				ProcessNormal(context, c);
				break;
			case LexerState.SingleQuoted:
				ProcessSingleQuoted(context, c);
				break;
			case LexerState.DoubleQuoted:
				ProcessDoubleQuoted(context, c);
				break;
		}
	}

	private void ProcessNormal(LexerContext context, char c)
	{
		if (context.EscapeNextCharacter)
		{
			ConsumeEscapedCharacter(context, c);
			return;
		}

		if (char.IsWhiteSpace(c))
		{
			FlushToken(context);
			return;
		}

		switch (c)
		{
			case '>':
				EmitOperator(context, TokenType.RedirectStdOut, ">");
				break;
			case '\'':
				context.TokenStarted = true;
				context.State = LexerState.SingleQuoted;
				break;
			case '"':
				context.TokenStarted = true;
				context.State = LexerState.DoubleQuoted;
				break;
			case '\\':
				context.EscapeNextCharacter = true;
				break;
			case '1' or '2':
				var nextCharacter = PeekNextCharacter(context.InputBuffer, context.CurrentInputBufferIndex);
				if (nextCharacter != null && nextCharacter == '>')
				{
					var tokenType = c == '1' ? TokenType.RedirectStdOut : TokenType.RedirectStdErr;
					EmitOperator(context, tokenType, $"{c}>");

					context.CurrentInputBufferIndex++; // Skip reading >
				}
				else
				{
					context.TokenStarted = true;
					context.LexemeBuffer.Append(c);
				}
				break;
			default:
				context.TokenStarted = true;
				context.LexemeBuffer.Append(c);
				break;
		}
	}

	private void ProcessSingleQuoted(LexerContext context, char c)
	{
		if (c == '\'')
		{
			context.State = LexerState.Normal;
		}
		else
		{
			context.LexemeBuffer.Append(c);
		}
	}

	private void ProcessDoubleQuoted(LexerContext context, char c)
	{
		if (context.EscapeNextCharacter)
		{
			if (c is '"' or '\\' or '$' or '`' or '\n')
			{
				context.LexemeBuffer.Append(c);
			}
			else
			{
				context.LexemeBuffer.Append('\\');
				context.LexemeBuffer.Append(c);
			}

			context.EscapeNextCharacter = false;
			return;
		}

		switch (c)
		{
			case '"':
				context.State = LexerState.Normal;
				break;
			case '\\':
				context.EscapeNextCharacter = true;
				break;
			default:
				context.LexemeBuffer.Append(c);
				break;
		}
	}

	private void ConsumeEscapedCharacter(LexerContext context, char c)
	{
		context.EscapeNextCharacter = false;
		context.TokenStarted = true;
		context.LexemeBuffer.Append(c);
	}

	private void FlushToken(LexerContext context)
	{
		if (!context.TokenStarted)
		{
			return;
		}

		var token = context.LexemeBuffer.ToString();
		context.Tokens.Add(new Token(TokenType.Word, token));

		context.TokenStarted = false;
		context.LexemeBuffer.Clear();
	}

	private void EmitOperator(LexerContext context, TokenType type, string value)
	{
		FlushToken(context);
		context.Tokens.Add(new Token(type, value));
	}

	private char? PeekNextCharacter(string inputBuffer, int currentInputBufferIndex)
	{
		var inputBufferLength = inputBuffer.Length - 1;
		var nextCharacterIndex = currentInputBufferIndex + 1;

		return nextCharacterIndex > inputBufferLength ? null : inputBuffer[nextCharacterIndex];
	}

	private void ValidateEndState(LexerContext context)
	{
		if (context.State is LexerState.SingleQuoted or LexerState.DoubleQuoted)
		{
			throw new InvalidOperationException("unterminated quote");
		}

		if (context.EscapeNextCharacter)
		{
			throw new InvalidOperationException("unterminated escape");
		}
	}
}