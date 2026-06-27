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
		var parsedCommand = new ShellTokeniser(modifiedInputLine).Parse();

		return (parsedCommand.Command, parsedCommand.Arguments);
	}

	private static void RunExecutable(string executable, IReadOnlyList<string> args)
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

	private static void EchoArguments(IReadOnlyList<string> args)
	{
		if (args.Count == 0)
		{
			return;
		}

		var redirectIndex = GetRedirectIndex(args);
		if (redirectIndex == -1)
		{
			Console.WriteLine(string.Join(" ", args));
			return;
		}

		if (redirectIndex == args.Count - 1)
		{
			Console.WriteLine("echo: missing file operand");
		}

		var inputContent = string.Join(" ", args.Take(redirectIndex));
		var fileName = args[redirectIndex + 1];

		File.WriteAllText(Path.GetFullPath(fileName), inputContent);
	}

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

	private static void PrintCurrentDirectory(IReadOnlyList<string> args) => Console.WriteLine(Directory.GetCurrentDirectory());

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

	private static void Exit(IReadOnlyList<string> args) => Environment.Exit(0);

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

	private static int GetRedirectIndex(IReadOnlyList<string> args)
	{
		const string StandardOutput = ">";
		const string StandardOutput1 = ">1";

		for (int position = 0; position < args.Count; position++)
		{
            if (args[position] is StandardOutput or StandardOutput1)
			{
				return position;
			}
		}

		return -1;
	}
}
