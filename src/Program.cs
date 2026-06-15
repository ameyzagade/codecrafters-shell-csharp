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
	private const char WHITESPACE = ' ';
	private const char SINGLE_QUOTE = '\'';
	private const char SEED_CHAR = '\0';

	private static readonly ImmutableList<string> BuiltInCommands = [EXIT_COMMAND, ECHO_COMMAND, TYPE_COMMAND, PWD_COMMAND, CD_COMMAND];

	public static void Main()
    {
		while (true)
		{
			PromptUser();

			var (command, args) = ReadAndExtractCommandWithArgumentString();
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
				RunBuiltIn(command, args);
			}
			else
			{
				RunExecutable(command, args);
			}
		}
    }

    private static void PromptUser() => Console.Write("$ ");
   
    private static (string, List<string>) ReadAndExtractCommandWithArgumentString()
    {
        string? inputLine = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(inputLine))
        {
            return (string.Empty, []);
        }

		var modifiedInputLine = inputLine.Trim();
		var commandKeywordEndIndex = modifiedInputLine.IndexOf(' ', 0);

		return commandKeywordEndIndex == -1
				? (inputLine, [])
				: (inputLine[..commandKeywordEndIndex], ExtractArguments(inputLine[(commandKeywordEndIndex + 1)..]));
    }

	private static List<string> ExtractArguments(string argumentLine)
	{
		argumentLine = argumentLine.Trim();
		
		var args = new List<string>(argumentLine.Length / 2);
		var processedArgumentBuilder = new StringBuilder(0, argumentLine.Length);
		var previousChar = SEED_CHAR;
		var inSingleQuote = false;
	
		foreach (var token in argumentLine)
		{
			switch (token)
			{
				case WHITESPACE:
					if (inSingleQuote)
					{
						processedArgumentBuilder.Append(WHITESPACE);
					}
					break;
				case SINGLE_QUOTE:
					inSingleQuote = !inSingleQuote;
					if (inSingleQuote)
					{
						if (previousChar.Equals(SINGLE_QUOTE))
						{
							inSingleQuote = false;
						}

						if (previousChar.Equals(WHITESPACE))
						{
							processedArgumentBuilder.Append(WHITESPACE);
						}
					}

					if (!inSingleQuote)
					{
						FlushArgument(processedArgumentBuilder, args);
					}

					break;
				default:
					if (!inSingleQuote && char.IsWhiteSpace(previousChar))
					{
						processedArgumentBuilder.Append(WHITESPACE);
						FlushArgument(processedArgumentBuilder, args);
					}

					AppendToken(processedArgumentBuilder, token);
					break;
			}

			// update previous char state
			previousChar = token;			
		}

		// flush if anything's left
		FlushArgument(processedArgumentBuilder, args);

		return args;
	}

	private static void AppendToken(StringBuilder argumentBuilder, char token) => argumentBuilder.Append(token);

	private static void FlushArgument(StringBuilder argumentBuilder, List<string> args)
	{
		if (argumentBuilder.Length == 0)
		{
			return;
		}

		args.Add(argumentBuilder.ToString());
		argumentBuilder.Clear();
	}

    private static void RunBuiltIn(string command, List<string> args)
	{
		switch (command)
		{
			case ECHO_COMMAND:	
				EchoArguments(args);
				break;
			case TYPE_COMMAND:
				PrintCommandType(args[0]);
				break;
			case PWD_COMMAND:
				PrintCurrentDirectory();
				break;
			case CD_COMMAND:
				ChangeDirectory(args[0]);
				break;
			default:
				Console.WriteLine($"{command}: command not found");
				break;
		}
	}

	private static void RunExecutable(string executable, List<string> args)
	{
		if (!TryGetExecutablePath(executable, out string _))
		{
			Console.WriteLine($"{executable}: not found");
			return;
		}

		var processStartInfo = new ProcessStartInfo
		{
			FileName = Path.GetFileName(executable),
			UseShellExecute = false,
		};

		foreach (var arg in args)
		{
			processStartInfo.ArgumentList.Add(arg);
		}

        using var process = Process.Start(processStartInfo);
		process?.WaitForExit();
    }

	private static void EchoArguments(List<string> args) => Console.WriteLine(string.Join(" ", args));

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