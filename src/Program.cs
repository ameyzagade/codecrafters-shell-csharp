using System.Diagnostics;

public class Program
{


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

			var context = new ShellExecutor().Execute(command);
			
			RouteStandardOutput(command, context);
			RouteStandardError(command, context);
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


	private static void RouteStandardOutput(ShellCommand command, ShellExecutionContext context)
	{
		if (command.Redirect.Type == RedirectType.StdOut)
		{
			File.WriteAllText(Path.GetFullPath(command.Redirect.Target), context.StandardOutput);
		}
		else
		{
			Console.Write(context.StandardOutput);
		}
	}

	private static void RouteStandardError(ShellCommand command, ShellExecutionContext context)
	{
		if (command.Redirect.Type == RedirectType.StdErr)
		{
			File.WriteAllText(Path.GetFullPath(command.Redirect.Target), context.StandardError);
		}
		else
		{
			Console.Error.Write(context.StandardError);
		}
	}
}
