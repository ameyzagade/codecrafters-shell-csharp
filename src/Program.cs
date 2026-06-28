using System.Diagnostics;

public class Program
{
	private sealed record ShellExecutionContext(string StandardOutput, string StandardError);

	private static readonly Dictionary<string, Func<IReadOnlyList<string>, ShellExecutionContext>> BuiltInCommands = new(StringComparer.OrdinalIgnoreCase)
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
			var command = ReadInputAndExtractCommand();
			if (string.IsNullOrEmpty(command.Command))
			{
				continue;
			}

			var context = ExecuteCommand(command);
			RouteStandardOutput(command, context);
			RouteStandardError(context);
		}
	}

	private static void PromptUser() => Console.Write("$ ");

	private static ShellCommand ReadInputAndExtractCommand()
	{
		string? inputLine = Console.ReadLine();
		if (string.IsNullOrWhiteSpace(inputLine))
		{
			return new ShellCommand()
			{
				Command = "",
			};
		}

		var modifiedInputLine = inputLine.Trim();
		var inputTokenStream = new ShellLexer().Tokenize(modifiedInputLine);

		return new ShellTokenParser().Parse(inputTokenStream);
	}

	private static ShellExecutionContext ExecuteCommand(ShellCommand command)
			=> BuiltInCommands.TryGetValue(command.Command, out var action)
				? action(command.Arguments)
				: RunExecutable(command.Command, command.Arguments);

	private static ShellExecutionContext RunExecutable(string executable, IReadOnlyList<string> args)
	{
		if (!TryGetExecutablePath(executable, out string _))
		{
			return new ShellExecutionContext(string.Empty, $"{executable}: not found{Environment.NewLine}");
		}

		var processStartInfo = new ProcessStartInfo
		{
			FileName = Path.GetFileName(executable),
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};

		foreach (var arg in args)
		{
			processStartInfo.ArgumentList.Add(arg);
		}

		ShellExecutionContext context;

		using var process = Process.Start(processStartInfo);
		context = new(process!.StandardOutput.ReadToEnd(), process!.StandardError.ReadToEnd());
		process?.WaitForExit();

		return context;
	}

	private static void RouteStandardOutput(ShellCommand command, ShellExecutionContext context)
	{
		if (command.OutputRedirection)
		{
			File.WriteAllText(Path.GetFullPath(command.OutputRedirectionFilePath), context.StandardOutput);
		}
		else
		{
			Console.Write(context.StandardOutput);
		}
	}

	private static void RouteStandardError(ShellExecutionContext context)
	{
		if (!string.IsNullOrEmpty(context.StandardError))
		{
			Console.Error.Write(context.StandardError);
		}
	}

	private static ShellExecutionContext EchoArguments(IReadOnlyList<string> args) => new(string.Join(" ", args) + Environment.NewLine, string.Empty);

	private static ShellExecutionContext PrintCommandType(IReadOnlyList<string> args)
	{
		if (args.Count == 0)
		{
			return new ShellExecutionContext(string.Empty, $"type: missing operand{Environment.NewLine}");
		}

		var command = args[0];
		if (BuiltInCommands.ContainsKey(command))
		{
			return new ShellExecutionContext($"{command} is a shell builtin{Environment.NewLine}", string.Empty);
		}

		if (TryGetExecutablePath(command, out string executablePath))
		{
			return new ShellExecutionContext($"{command} is {executablePath}{Environment.NewLine}", string.Empty);
		}

		return new ShellExecutionContext(string.Empty, $"{command}: not found{Environment.NewLine}");
	}

	private static ShellExecutionContext PrintCurrentDirectory(IReadOnlyList<string> args) => new(Directory.GetCurrentDirectory() + Environment.NewLine, string.Empty);

	private static ShellExecutionContext ChangeDirectory(IReadOnlyList<string> args)
	{
		if (args.Count == 0)
		{
			return new ShellExecutionContext(string.Empty, $"cd: missing operand{Environment.NewLine}");
		}

		var path = args[0];

		if (path == "~" || path.StartsWith("~/", StringComparison.Ordinal))
		{
			var homeDirectory = Environment.GetEnvironmentVariable("HOME");

			if (string.IsNullOrEmpty(homeDirectory))
			{
				return new ShellExecutionContext(string.Empty, $"cd: HOME not set{Environment.NewLine}");
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
			return new ShellExecutionContext(string.Empty, $"cd: {path}: No such file or directory{Environment.NewLine}");
		}

		Directory.SetCurrentDirectory(path);
		return new ShellExecutionContext(string.Empty, string.Empty);
	}

	private static ShellExecutionContext Exit(IReadOnlyList<string> args)
	{
		Environment.Exit(0);
		return new ShellExecutionContext(string.Empty, string.Empty);
	}

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
			if (File.Exists(filePath) && CheckFileExecutableFlag(filePath))
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
