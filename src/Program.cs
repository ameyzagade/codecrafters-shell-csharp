using System.Diagnostics;
using System.Text;

public class Program
{
	private static readonly Dictionary<string, Action<IReadOnlyList<string>>> BuiltInCommands = new(StringComparer.OrdinalIgnoreCase)
	{
		["echo"] = EchoArguments,
		["type"] = PrintCommandType,
		["pwd"] = PrintCurrentDirectory,
		["cd"] = ChangeDirectory,
		["exit"] = Exit,
	};

	public static void Main()
	{
		while (true)
		{
			PromptUser();

			var (command, args) = ReadAndExtractCommandWithArguments();
			if (command.Length == 0)
			{
				continue;
			}

			if (BuiltInCommands.TryGetValue(command, out var action))
			{
				action(args);
			}
			else
			{
				RunExecutable(command, args);
			}
		}
	}

	private static void PromptUser() => Console.Write("$ ");

	private static (string, IReadOnlyList<string>) ReadAndExtractCommandWithArguments()
	{
		string? inputLine = Console.ReadLine();
		if (string.IsNullOrWhiteSpace(inputLine)) return (string.Empty, []);

		var modifiedInputLine = inputLine.Trim();
		var firstSpace = modifiedInputLine.IndexOf(' ');

		return firstSpace < 0
				? (modifiedInputLine, [])
				: (modifiedInputLine[..firstSpace], ExtractArguments(modifiedInputLine[(firstSpace + 1)..]));
	}

	private static IReadOnlyList<string> ExtractArguments(string argumentLine)
	{
		argumentLine = argumentLine.Trim();

		var args = new List<string>();
		var processedArgumentBuilder = new StringBuilder(argumentLine.Length);

		var isEscapeChar = false;
		var inSingleQuote = false;
		var inDoubleQuote = false;

		foreach (var token in argumentLine)
		{
			switch (token)
			{
				case ' ':
					if (isEscapeChar)
					{
						processedArgumentBuilder.Append(token);
						isEscapeChar = false;
					}
					else if (inSingleQuote || inDoubleQuote)
					{
						processedArgumentBuilder.Append(token);
					}
					else
					{
						FlushArgument(processedArgumentBuilder, args);
					}
					break;
				case '\'':
					if (inDoubleQuote)
					{
						processedArgumentBuilder.Append(token);
					}
					else if (!inSingleQuote && isEscapeChar)
					{
						processedArgumentBuilder.Append(token);
						isEscapeChar = false;
					}
					else
					{
						inSingleQuote = !inSingleQuote;
					}

					break;
				case '\"':
					if (inSingleQuote || isEscapeChar)
					{
						processedArgumentBuilder.Append(token);
						isEscapeChar = false;
					}
					else
					{
						inDoubleQuote = !inDoubleQuote;
					}
					break;
				case '\\':
					if (inSingleQuote || (inDoubleQuote && isEscapeChar) || (!inSingleQuote && !inDoubleQuote && isEscapeChar))
					{
						processedArgumentBuilder.Append(token);
						isEscapeChar = false;
					}
					else
					{
						isEscapeChar = !isEscapeChar;
					}
					break;
				default:
					processedArgumentBuilder.Append(token);
					isEscapeChar = false;
					break;
			}
		}

		if (inSingleQuote || inDoubleQuote)
		{
			Console.WriteLine("unterminated quote");
			return [];
		}

		// flush if anything's left
		FlushArgument(processedArgumentBuilder, args);

		return args;
	}

	private static void FlushArgument(StringBuilder argumentBuilder, List<string> args)
	{
		if (argumentBuilder.Length == 0) return;

		args.Add(argumentBuilder.ToString());
		argumentBuilder.Clear();
	}

	private static void RunExecutable(string executable, IReadOnlyList<string> args)
	{
		if (!TryGetExecutablePath(executable, out string executablePath))
		{
			Console.WriteLine($"{executable}: not found");
			return;
		}

		var processStartInfo = new ProcessStartInfo
		{
			FileName = executablePath,
			UseShellExecute = false,
		};

		foreach (var arg in args)
		{
			processStartInfo.ArgumentList.Add(arg);
		}

		using var process = Process.Start(processStartInfo);
		process?.WaitForExit();
	}

	private static void EchoArguments(IReadOnlyList<string> args) => Console.WriteLine(string.Join(" ", args));

	private static void PrintCommandType(IReadOnlyList<string> args)
	{
		if (args.Count == 0)
		{
			Console.WriteLine("type: missing operand");
			return;
		}

		var command = args[0];
		if (BuiltInCommands.ContainsKey(command))
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

	private static void PrintCurrentDirectory(IReadOnlyList<string> _) => Console.WriteLine(Directory.GetCurrentDirectory());

	private static void ChangeDirectory(IReadOnlyList<string> args)
	{
		if (args.Count == 0)
		{
			Console.WriteLine("cd: missing operand");
			return;
		}

		var path = args[0];

		if (path == "~" || path.StartsWith("~/", StringComparison.Ordinal))
		{
			var homeDirectory = Environment.GetEnvironmentVariable("HOME");

			if (string.IsNullOrEmpty(homeDirectory))
			{
				Console.WriteLine("cd: HOME not set");
				return;
			}

			path = path == "~"
				? homeDirectory
				: Path.Combine(homeDirectory, path[2..]);
		}

		if (!Path.IsPathFullyQualified(path))
		{
			path = Path.Combine(Directory.GetCurrentDirectory(), path);
		}

		if (!Directory.Exists(path))
		{
			Console.WriteLine($"cd: {path}: No such file or directory");
			return;
		}

		Directory.SetCurrentDirectory(path);
	}

	private static void Exit(IReadOnlyList<string> _) => Environment.Exit(0);

	private static bool TryGetExecutablePath(string fileName, out string containingExecutablePath)
	{
		containingExecutablePath = string.Empty;

		if (Path.IsPathFullyQualified(fileName) || fileName.Contains(Path.DirectorySeparatorChar))
		{
			if (File.Exists(fileName) && CheckFileExecutableFlag(fileName))
			{
				containingExecutablePath = Path.GetFullPath(fileName);
				return true;
			}

			return false;
		}

		var pathVariableValue = Environment.GetEnvironmentVariable("PATH");
		if (string.IsNullOrEmpty(pathVariableValue))
		{
			return false;
		}

		var executablePaths = pathVariableValue.Split(Path.PathSeparator);
		foreach (var dir in executablePaths)
		{
			var filePath = Path.Combine(dir, fileName);
			if (File.Exists(filePath)
			   && CheckFileExecutableFlag(filePath))
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