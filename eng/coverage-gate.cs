// Coverage gate (docs/adr/0026-umbral-de-cobertura-por-proyecto.md).
// Merges every Cobertura report under a directory (a line counts as covered if any test run covered it),
// skips generated code (EF migrations, obj/) and checks line coverage per assembly and in total against
// eng/coverage-thresholds.json.
//
// Usage (from the repository root):
//   dotnet run eng/coverage-gate.cs -- <results-directory> [thresholds-file]
// Exit codes: 0 = all thresholds met; 1 = below a threshold or assembly without threshold; 2 = usage or input error.

using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine("Usage: dotnet run eng/coverage-gate.cs -- <results-directory> [thresholds-file]");
    return 2;
}

var resultsDirectory = args[0];
var thresholdsPath = args.Length == 2 ? args[1] : Path.Combine("eng", "coverage-thresholds.json");

if (!Directory.Exists(resultsDirectory))
{
    Console.Error.WriteLine($"Results directory not found: {resultsDirectory}");
    return 2;
}

if (!File.Exists(thresholdsPath))
{
    Console.Error.WriteLine($"Thresholds file not found: {thresholdsPath}");
    return 2;
}

var reports = Directory.GetFiles(resultsDirectory, "*.cobertura.xml", SearchOption.AllDirectories);
if (reports.Length == 0)
{
    Console.Error.WriteLine($"No *.cobertura.xml reports under {resultsDirectory}. Run the tests with --coverage --coverage-output-format cobertura.");
    return 2;
}

using var thresholdsDocument = JsonDocument.Parse(File.ReadAllText(thresholdsPath));
var thresholdsRoot = thresholdsDocument.RootElement;
var totalThreshold = thresholdsRoot.GetProperty("total").GetDouble();
var assemblyThresholds = thresholdsRoot.GetProperty("assemblies").EnumerateObject()
    .ToDictionary(property => property.Name, property => property.Value.GetDouble(), StringComparer.Ordinal);
var excludedSegments = thresholdsRoot.GetProperty("excludedPathSegments").EnumerateArray()
    .Select(segment => segment.GetString()!)
    .ToHashSet(StringComparer.OrdinalIgnoreCase);

// assembly -> (file, line) -> covered by any run
var coverage = new Dictionary<string, Dictionary<(string File, int Line), bool>>(StringComparer.Ordinal);

foreach (var report in reports)
{
    var document = XDocument.Load(report);
    foreach (var package in document.Descendants("package"))
    {
        var assembly = package.Attribute("name")?.Value ?? "<unknown>";
        if (!coverage.TryGetValue(assembly, out var lines))
        {
            coverage[assembly] = lines = [];
        }

        foreach (var @class in package.Descendants("class"))
        {
            var file = (@class.Attribute("filename")?.Value ?? string.Empty).Replace('\\', '/');
            if (file.Split('/').Any(excludedSegments.Contains))
            {
                continue;
            }

            foreach (var line in @class.Elements("lines").Elements("line"))
            {
                var number = int.Parse(line.Attribute("number")!.Value, CultureInfo.InvariantCulture);
                var hits = long.Parse(line.Attribute("hits")!.Value, CultureInfo.InvariantCulture);
                var key = (file, number);
                lines[key] = (lines.TryGetValue(key, out var covered) && covered) || hits > 0;
            }
        }
    }
}

var failures = new List<string>();
var totalCovered = 0;
var totalValid = 0;

Console.WriteLine($"Line coverage from {reports.Length} report(s):");
Console.WriteLine();
Console.WriteLine($"{"Assembly",-52} {"Covered",9} {"Rate",8} {"Min",6}  Result");

foreach (var (assembly, lines) in coverage.OrderBy(entry => entry.Key, StringComparer.Ordinal))
{
    var valid = lines.Count;
    if (valid == 0)
    {
        continue;
    }

    var covered = lines.Values.Count(isCovered => isCovered);
    totalCovered += covered;
    totalValid += valid;
    var rate = 100.0 * covered / valid;

    if (!assemblyThresholds.TryGetValue(assembly, out var minimum))
    {
        Console.WriteLine($"{assembly,-52} {covered,4}/{valid,-4} {rate,7:F1}% {"-",6}  NO THRESHOLD");
        failures.Add($"{assembly} has no threshold in {thresholdsPath}");
        continue;
    }

    var passed = covered * 100.0 >= minimum * valid;
    Console.WriteLine($"{assembly,-52} {covered,4}/{valid,-4} {rate,7:F1}% {minimum,5:F0}%  {(passed ? "OK" : "BELOW")}");
    if (!passed)
    {
        failures.Add($"{assembly}: {rate:F1}% < {minimum:F0}%");
    }
}

foreach (var missing in assemblyThresholds.Keys.Where(assembly => !coverage.ContainsKey(assembly)))
{
    failures.Add($"{missing} has a threshold but no coverage data (did its tests run?)");
}

if (totalValid == 0)
{
    Console.Error.WriteLine("The reports contain no coverable lines.");
    return 2;
}

var totalRate = 100.0 * totalCovered / totalValid;
var totalPassed = totalCovered * 100.0 >= totalThreshold * totalValid;
Console.WriteLine($"{"TOTAL",-52} {totalCovered,4}/{totalValid,-4} {totalRate,7:F1}% {totalThreshold,5:F0}%  {(totalPassed ? "OK" : "BELOW")}");
if (!totalPassed)
{
    failures.Add($"TOTAL: {totalRate:F1}% < {totalThreshold:F0}%");
}

Console.WriteLine();
if (failures.Count == 0)
{
    Console.WriteLine("Coverage gate passed.");
    return 0;
}

Console.Error.WriteLine("Coverage gate failed:");
foreach (var failure in failures)
{
    Console.Error.WriteLine($"  - {failure}");
}

Console.Error.WriteLine(
    "Add tests, or a threshold for a new assembly. Thresholds are only raised, never lowered " +
    "(docs/adr/0026-umbral-de-cobertura-por-proyecto.md).");
return 1;
