class Program
{
    static void Main()
    {
        // TODO: Uncomment the code below to pass the first stage
	while (true)
	{
        	Console.Write("$ ");
		
		string inputLine = Console.ReadLine();

		if (inputLine.Equals("exit", StringComparison.OrdinalIgnoreCase))
		{
			break;
		}
		
		Console.WriteLine($"{inputLine}: command not found");

	}
    }
}
