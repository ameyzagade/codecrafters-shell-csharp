using System.Text;

public class ShellTokeniser
{
	private enum State
	{
		Normal,
		SingleQuoted,
		DoubleQuoted,
		Escaped,
		DoubleQuotedEscaped
	}

	private readonly string _input;
	private readonly List<string> _tokens = [];
	private readonly StringBuilder _current = new();

	private State _state = State.Normal;

    public ShellTokeniser(string input) => _input = input;

    public List<string> Parse()
	{
		foreach (var c in _input)
		{
			Process(c);
		}

		FlushToken();
		return _tokens;
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
			case State.Escaped:
				ProcessEscaped(c);
				break;
			case State.DoubleQuotedEscaped:
				ProcessDoubleQuotedEscaped(c);
				break;
		}
	}

    private void ProcessNormal(char c)
    {
		if (c == ' ')
		{
			FlushToken();
		}
		else if (c == '\'' && _state != State.DoubleQuoted)
		{
			_state = State.SingleQuoted;		
		}
		else if (c == '\"' && _state != State.SingleQuoted)
		{
			_state = State.DoubleQuoted;
		}
		else if (c == '\\')
		{
			_state = State.Escaped;
		}
		else
		{
    	    _current.Append(c);
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
		if (c == '\"')
		{
			_state = State.Normal;
		}
		else if (c == '\\')
		{
			_state = State.DoubleQuotedEscaped;
		}
		else
		{
			_current.Append(c);
		}
	}

	private void ProcessEscaped(char c)
	{
		_current.Append(c);
		_state = State.Normal;		
	}

	private void ProcessDoubleQuotedEscaped(char c)
    {
        if (c != '\"' && c != '\\' && c != '$' && c != '`' && c != '\n')
        {
			_current.Append('\\');
        }

        _current.Append(c);
		_state = State.DoubleQuoted;
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
}