public class Program
{
	private static readonly ShellExecutor Executor = new();
	private static readonly ShellOutputRouter OutputRouter = new();

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

			OutputRouter.Route(command, context);
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
}