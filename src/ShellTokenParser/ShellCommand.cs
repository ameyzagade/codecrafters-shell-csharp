public class ShellCommand
{
    private readonly List<string> _arguments = [];

    public string Command { get; set; }
    public IReadOnlyList<string> Arguments => _arguments;
    public bool OutputRedirection { get; set; }
    public string OutputRedirectionFilePath { get; set; }

    public void AddArgument(string argument) => _arguments.Add(argument);
}