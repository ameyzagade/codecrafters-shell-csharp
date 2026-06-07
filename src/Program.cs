using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

public class Program
{
	private const string EXIT_COMMAND = "exit";
	private const string ECHO_COMMAND = "echo";
	private const string TYPE_COMMAND = "type";

	private static readonly ImmutableList<string> BuiltInCommands = [EXIT_COMMAND, ECHO_COMMAND, TYPE_COMMAND];

	public static void Main()
    {
		while (true)
		{
			PromptUser();

			var (command, args) = ReadAndExtractCommandWithArguments();
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
   
    private static (string, ReadOnlyCollection<string>) ReadAndExtractCommandWithArguments()
    {
		string inputLine = Console.ReadLine();
		if (string.IsNullOrEmpty(inputLine))
		{
			throw new Exception("No input received!");
		}

		var input = inputLine.Split(' ');
		var command = input[0];
		var args = input.Length > 1 ? input[1..]?.AsReadOnly() : [];

		return (command, args);
    }

	private static void RunBuiltIn(string command, ReadOnlyCollection<string> args)
	{
		switch (command)
		{
			case ECHO_COMMAND:	
				EchoArguments(args);
				break;
			case TYPE_COMMAND:
				PrintCommandType(command);
				break;
			default:
				Console.WriteLine($"{command}: command not found");
				break;
		}
	}

	private static void RunExecutable(string executable, ReadOnlyCollection<string> args)
	{
		if (!TryGetExecutablePath(executable, out string executablePath))
		{
			Console.WriteLine($"{executable}: not found");
			return;
		}

		var builder = new StringBuilder();
		foreach (var arg in args)
		{
			builder.Append(arg);
		}

		var processStartInfo = new ProcessStartInfo
		{
			FileName = executablePath,
			Arguments = builder.ToString(),
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

        using var process = Process.Start(processStartInfo);
		process?.WaitForExit();
		
    }

	private static void EchoArguments(ReadOnlyCollection<string> args)
	{
		foreach (var arg in args)
		{
			Console.Write(arg);
			Console.Write(' ');
		}

		Console.WriteLine();
	}

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