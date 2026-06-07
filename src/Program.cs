class Program
{
    static void Main()
    {
	while (true)
	{
        	Console.Write("$ ");
		
		string inputLine = Console.ReadLine();

		if (inputLine.Equals("exit", StringComparison.OrdinalIgnoreCase))
		{
			break;
		}
		else if (inputLine.StartsWith("echo", StringComparison.OrdinalIgnoreCase))
		{
			Console.WriteLine($"{inputLine[5..]}");
		}
		else if (inputLine.StartsWith("type", StringComparison.OrdinalIgnoreCase))
		{
			var command = inputLine[5..];
			if (command.Equals("echo", StringComparison.OrdinalIgnoreCase)
			    || command.Equals("exit", StringComparison.OrdinalIgnoreCase)
			    || command.Equals("type", StringComparison.OrdinalIgnoreCase))
			{
				Console.WriteLine($"{command} is a shell builtin");
			}
			else
			{
				Console.WriteLine($"{command}: command not found");
			}
		}
		else
		{
			Console.WriteLine($"{inputLine}: command not found");
		}
	}
    }
}
