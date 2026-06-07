using System.Collections.Immutable;
using System.Collections.ObjectModel;

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

			switch (command)
			{
				case ECHO_COMMAND:	
					EchoArguments(args);
					break;
				case TYPE_COMMAND:
					Console.WriteLine(args[0]);
					PrintCommandType(command);
					break;
				default:
					Console.WriteLine($"{command}: command not found");
					break;
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

		var input = inputLine.Split(" ");
		var command = input[0];
		var args = input.Length > 1 ? input[1..].AsReadOnly() : [];

		return (command, args);
    }

	private static void EchoArguments(ReadOnlyCollection<string> args)
	{
		foreach (var arg in args)
		{
			Console.Write(arg);
			Console.Write(" ");
		}

		Console.WriteLine();
	}

	private static void PrintCommandType(string command)
	{
		if (!BuiltInCommands.Contains(command))
		{
			Console.WriteLine($"{command}: not found");

			return;
		}
		
		Console.WriteLine($"{command} is a shell builtin");
	}
}
