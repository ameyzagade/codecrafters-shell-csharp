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

		if (inputLine.StartsWith("echo", StringComparison.OrdinalIgnoreCase))
		{
			Console.WriteLine($"{inputLine[5..]}");
		}
		else
		{
			Console.WriteLine($"{inputLine}: command not found");
		}

	}
    }
}
