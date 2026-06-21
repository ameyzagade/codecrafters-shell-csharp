using System.ComponentModel.Design;
using System.Text;

public class ShellTokeniser
{
	private enum State
	{
		Normal,
		SingleQuoted,
		DoubleQuoted,
	}

	private readonly string _input;
	private readonly List<string> _tokens = [];
	private readonly StringBuilder _current = new();

	private State _state = State.Normal;
	private bool _escapeNextCharacter = false;

	public ShellTokeniser(string input) => _input = input;

	public ParsedCommand Parse()
	{
		foreach (var c in _input)
		{
			Process(c);
		}

		FlushToken();
		ValidateEndState();

		var command = _tokens[0];
		var args = _tokens.Count > 1 ? _tokens[1..] : [];

		return new ParsedCommand(command, args);
	}

	private void Process(char c)
	{
		switch (_state)
		{
			case State.Normal:
				ProcessNormal(c);
				break;
			case State.SingleQuoted:
				ProcessSingleQuoted(c);
				break;
			case State.DoubleQuoted:
				ProcessDoubleQuoted(c);
				break;
		}
	}

	private void ProcessNormal(char c)
	{
		if (_escapeNextCharacter)
		{
			_escapeNextCharacter = false;
			_current.Append(c);
			return;
		}

		switch (c)
		{
			case ' ':
				FlushToken();
				break;
			case '\'':
				_state = State.SingleQuoted;
				break;
			case '\"':
				_state = State.DoubleQuoted;
				break;
			case '\\':
				_escapeNextCharacter = true;
				break;
			default:
				_current.Append(c);
				break;
		}
	}

	private void ProcessSingleQuoted(char c)
	{
		if (c == '\'')
		{
			_state = State.Normal;
		}
		else
		{
			_current.Append(c);
		}
	}

	private void ProcessDoubleQuoted(char c)
	{
		if (_escapeNextCharacter)
		{
			if (c is '"' or '\\' or '$' or '`' or '\n')
			{
				_current.Append(c);
			}
			else
			{
				_current.Append('\\');
				_current.Append(c);
			}

			_escapeNextCharacter = false;
			return;
		}

		switch (c)
		{
			case '\"':
				_state = State.Normal;
				break;
			case '\\':
				_escapeNextCharacter = true;
				break;
			default:
				_current.Append(c);
				break;
		}
	}

	private void FlushToken()
	{
		if (_current.Length < 1)
		{
			return;
		}

		_tokens.Add(_current.ToString());
		_current.Clear();
	}

	private void ValidateEndState()
	{
		if (_state is State.SingleQuoted or State.DoubleQuoted)
		{
			throw new InvalidOperationException("unterminated quote");
		}
	}
}