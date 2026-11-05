// PreToolUse hook for the read-only reviewer subagent (.claude/agents/revisor-arquitectura.md).
// Reads the hook JSON from stdin and allows only read-only git commands.
// Exit 0 = allow; exit 2 = block (the reason on stderr is shown to Claude).
// Run with: dotnet run .claude/hooks/git-solo-lectura.cs  (.NET 10 file-based app)

using System.Text.Json;
using System.Text.RegularExpressions;

string input;
try
{
    input = Console.In.ReadToEnd();
}
catch (Exception ex)
{
    return Block($"could not read the hook input ({ex.GetType().Name}).");
}

string? command = null;
try
{
    using var document = JsonDocument.Parse(input);
    if (document.RootElement.ValueKind == JsonValueKind.Object
        && document.RootElement.TryGetProperty("tool_input", out var toolInput)
        && toolInput.ValueKind == JsonValueKind.Object
        && toolInput.TryGetProperty("command", out var commandElement)
        && commandElement.ValueKind == JsonValueKind.String)
    {
        command = commandElement.GetString();
    }
}
catch (JsonException)
{
    return Block("the hook input is not valid JSON.");
}

if (string.IsNullOrWhiteSpace(command))
{
    return Block("no command found in tool_input.command.");
}

command = command.Trim();

const string ShellOperators = ";&|<>`$(){}\r\n";
if (command.IndexOfAny(ShellOperators.ToCharArray()) >= 0)
{
    return Block("shell operators, redirections and substitutions are not allowed.");
}

string[] dangerousOptions =
[
    " -c ", "--output", "--ext-diff", "--textconv", "--exec", "--upload-pack", "--open-files-in-pager",
];
foreach (var option in dangerousOptions)
{
    if (command.Contains(option, StringComparison.Ordinal))
    {
        return Block($"option '{option.Trim()}' is not allowed.");
    }
}

var readOnlyGit = new Regex(
    @"^git (diff|log|show|status|merge-base|rev-parse|ls-files|blame)(\s|$)",
    RegexOptions.CultureInvariant);

if (readOnlyGit.IsMatch(command) || command == "git branch --show-current")
{
    return 0;
}

return Block(
    "only read-only git is allowed: git diff|log|show|status|merge-base|rev-parse|ls-files|blame "
    + "or git branch --show-current.");

static int Block(string reason)
{
    Console.Error.WriteLine($"git-solo-lectura: blocked. {reason}");
    return 2;
}
