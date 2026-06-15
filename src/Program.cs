using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

public class Program
{
	private const string EXIT_COMMAND = "exit";
	private const string ECHO_COMMAND = "echo";
	private const string TYPE_COMMAND = "type";
	private const string PWD_COMMAND = "pwd";
	private const string CD_COMMAND = "cd";

	private static readonly ImmutableList<string> BuiltInCommands = [EXIT_COMMAND, ECHO_COMMAND, TYPE_COMMAND, PWD_COMMAND, CD_COMMAND];

	public static void Main()
    {
		while (true)
		{
			PromptUser();

			var (command, argString) = ReadAndExtractCommandWithArgumentString();
			if (command.Equals(string.Empty, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (command.Equals(EXIT_COMMAND, StringComparison.OrdinalIgnoreCase))
			{
				break;
			}

			if (IsBuiltInCommand(command))
			{
				RunBuiltIn(command, argString);
			}
			else
			{
				RunExecutable(command, argString);
			}
		}
    }

    private static void PromptUser() => Console.Write("$ ");
   
    private static (string, string) ReadAndExtractCommandWithArgumentString()
    {
        string? inputLine = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(inputLine))
        {
            return (string.Empty, string.Empty);
        }

        List<string> inputTokens = TokeniseInput(inputLine);
		var command = inputTokens[0];
        var args = inputTokens.Count == 0 ? string.Empty : string.Join(' ', inputTokens[1..]);

        return (command, args);
    }

    private static List<string> TokeniseInput(string inputLine)
    {
        var tokens = new List<string>(inputLine.Length / 2);
        var inSingleQuote = false;
        var tokenBuilder = new StringBuilder();

        foreach (var ch in inputLine)
        {
            switch (ch)
            {
                case '\'':
                    inSingleQuote = !inSingleQuote; // toggle the state if you see a single quote
                    if (!inSingleQuote)
                    {
                        if (tokenBuilder.Length == 0) continue; // empty quotes encountered

                        FlushToken(tokens, tokenBuilder);
                    }
                    break;
                case ' ':
                    if (inSingleQuote)
                    {
						SaveToken(tokenBuilder, ch);
                        continue;
                    }

					if (tokenBuilder.Length == 0)
					{
						continue;
					}

                    FlushToken(tokens, tokenBuilder);
                    break;
                default:
					SaveToken(tokenBuilder, ch);
                    break;
            }
        }

        if (inSingleQuote)
        {
            throw new Exception("Quotes not closed");
        }

        if (tokenBuilder.Length > 0)
        {
			FlushToken(tokens, tokenBuilder);
        }

        return tokens;
    }

	private static void SaveToken(StringBuilder tokenBuilder, char token) => tokenBuilder.Append(token);

    private static void FlushToken(List<string> tokens, StringBuilder tokenBuilder)
    {
        tokens.Add(tokenBuilder.ToString());
        tokenBuilder.Clear();
    }

    private static void RunBuiltIn(string command, string argString)
	{
		switch (command)
		{
			case ECHO_COMMAND:	
				EchoArguments(argString);
				break;
			case TYPE_COMMAND:
				PrintCommandType(argString);
				break;
			case PWD_COMMAND:
				PrintCurrentDirectory();
				break;
			case CD_COMMAND:
				ChangeDirectory(argString);
				break;
			default:
				Console.WriteLine($"{command}: command not found");
				break;
		}
	}

	private static void RunExecutable(string executable, string argString)
	{
		if (!TryGetExecutablePath(executable, out string _))
		{
			Console.WriteLine($"{executable}: not found");
			return;
		}

		var processStartInfo = new ProcessStartInfo
		{
			FileName = Path.GetFileName(executable),
			Arguments = argString,
		};

        using var process = Process.Start(processStartInfo);
		process?.WaitForExit();
    }

	private static void EchoArguments(string argString) => Console.WriteLine(argString);

	private static void PrintCommandType(string command)
	{
		if (IsBuiltInCommand(command))
		{
			Console.WriteLine($"{command} is a shell builtin");
			return;
		}

		if (TryGetExecutablePath(command, out string executablePath))
		{
			Console.WriteLine($"{command} is {executablePath}");
			return;
		}

		Console.WriteLine($"{command}: not found");
	}

	private static void PrintCurrentDirectory() => Console.WriteLine(Directory.GetCurrentDirectory());

	private static void ChangeDirectory(string argString)
	{
		var path = argString;
		if (path.StartsWith("~", StringComparison.OrdinalIgnoreCase))
		{
			var homeDirectoryPath = Environment.GetEnvironmentVariable("HOME");
			if (string.IsNullOrEmpty(homeDirectoryPath))
			{
				throw new Exception("Home directory not found!");
			}

			path = path.Equals("~", StringComparison.OrdinalIgnoreCase) ? homeDirectoryPath : Path.Combine(homeDirectoryPath, argString[2..]);
			Directory.SetCurrentDirectory(path);

			return;
		}

		if (!Path.IsPathFullyQualified(path))
		{
			path = Path.Combine(Directory.GetCurrentDirectory(), argString);
		}

		if (!Directory.Exists(path))
		{
			Console.WriteLine($"{CD_COMMAND}: {path}: No such file or directory");
			return;
		}

		Directory.SetCurrentDirectory(path);
	}

	private static bool IsBuiltInCommand(string command) => BuiltInCommands.Contains(command);

	private static bool TryGetExecutablePath(string fileName, out string containingExecutablePath)
	{
		containingExecutablePath = string.Empty;

		var pathVariableValue = Environment.GetEnvironmentVariable("PATH");
		if (string.IsNullOrEmpty(pathVariableValue))
		{
			return false;
		}

		var executablePaths = pathVariableValue.Split(Path.PathSeparator);
		foreach (var executablePath in executablePaths)
		{
			var filePath = Path.Combine(executablePath, fileName);
			if (!File.Exists(filePath))
			{
				continue;
			}

			if (CheckFileExecutableFlag(filePath))
			{
				containingExecutablePath = filePath;
				return true;
			}
		}

		return false;
	}

	private static bool CheckFileExecutableFlag(string filePath)
	{
		if (!(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()))
		{
			return true;
		}

		var fileMode = File.GetUnixFileMode(filePath);
		var executeMask = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
			
		return (fileMode & executeMask) != 0;
	}
}