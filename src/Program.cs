using System.Diagnostics;

public class Program
{
	private static readonly ShellExecutor Executor = new();

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

			var context = Executor.Execute(command);

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
		switch (command.Redirect.Type)
		{
			case RedirectType.StdOut:
				File.WriteAllText(Path.GetFullPath(command.Redirect.Target), context.StandardOutput);
				break;

			case RedirectType.AppendStdOut:
				File.AppendAllText(Path.GetFullPath(command.Redirect.Target), context.StandardOutput);
				break;

			default:
				Console.Write(context.StandardOutput);
				break;
		}
	}

	private static void RouteStandardError(ShellCommand command, ShellExecutionContext context)
	{
		switch (command.Redirect.Type)
		{
			case RedirectType.StdErr:
				File.WriteAllText(Path.GetFullPath(command.Redirect.Target), context.StandardError);
				break;

			case RedirectType.AppendStdErr:
				File.AppendAllText(Path.GetFullPath(command.Redirect.Target), context.StandardError);
				break;

			default:
				Console.Error.Write(context.StandardError);
				break;
		}
	}
}