public sealed class ShellOutputRouter
{
    public void Route(ShellCommand command, ShellExecutionContext context)
    {
        RouteStandardOutput(context, command.Redirect.Type, command.Redirect.Target);
        RouteStandardError(context, command.Redirect.Type, command.Redirect.Target);
    }

    private void RouteStandardOutput(ShellExecutionContext context, RedirectType redirectType, string target)
    {
        switch (redirectType)
        {
            case RedirectType.StdOut:
                File.WriteAllText(Path.GetFullPath(target), context.StandardOutput);
                break;

            case RedirectType.AppendStdOut:
                File.AppendAllText(Path.GetFullPath(target), context.StandardOutput);
                break;

            default:
                Console.Write(context.StandardOutput);
                break;
        }
    }

    private void RouteStandardError(ShellExecutionContext context, RedirectType redirectType, string target)
    {
        switch (redirectType)
        {
            case RedirectType.StdErr:
                File.WriteAllText(Path.GetFullPath(target), context.StandardError);
                break;

            case RedirectType.AppendStdErr:
                File.AppendAllText(Path.GetFullPath(target), context.StandardError);
                break;

            default:
                Console.Error.Write(context.StandardError);
                break;
        }
    }
}