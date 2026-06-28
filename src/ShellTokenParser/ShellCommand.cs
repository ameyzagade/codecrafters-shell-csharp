public class ShellCommand
{
    private readonly List<string> _arguments = [];

    public string Command { get; set; }
    public IReadOnlyList<string> Arguments => _arguments;
    public Redirect Redirect { get; set; } = new Redirect(RedirectType.None, string.Empty);

    public void AddArgument(string argument) => _arguments.Add(argument);
}