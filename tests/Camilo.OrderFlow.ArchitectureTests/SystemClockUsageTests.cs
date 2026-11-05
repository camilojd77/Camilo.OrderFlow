using Shouldly;
using System.Text.RegularExpressions;
using Xunit;

namespace Camilo.OrderFlow.ArchitectureTests;

/// <summary>
/// ADR-0025: production code gets the time from an injected <see cref="TimeProvider"/>, never from
/// DateTime.UtcNow / DateTime.Now / DateTimeOffset.UtcNow. Every existing read was migrated on 2026-11-05, so
/// <see cref="KnownUsages"/> is empty; an entry may only be added with an ADR that justifies it.
/// The check scans source files because NetArchTest cannot detect member access on System.DateTime.
/// </summary>
public sealed partial class SystemClockUsageTests
{
    private const string SolutionFile = "Camilo.OrderFlow.slnx";
    private const string ProductionProjectPattern = "Camilo.OrderFlow.*";
    private static readonly string[] SkippedSegments = ["bin", "obj", "Migrations"];

    /// <summary>Allowed system clock reads per file. Empty since the migration to TimeProvider (2026-11-05).</summary>
    private static readonly IReadOnlyDictionary<string, int> KnownUsages =
        new Dictionary<string, int>(StringComparer.Ordinal);

    [GeneratedRegex(@"\bDateTime(Offset)?\.(UtcNow|Now|Today)\b", RegexOptions.CultureInvariant)]
    private static partial Regex SystemClockRead();

    [Fact(DisplayName = "Production code must not add system clock reads")]
    public void ProductionCode_ShouldNot_Add_SystemClockReads()
    {
        var offenders = CountUsages()
            .Where(usage => usage.Value > KnownUsages.GetValueOrDefault(usage.Key))
            .Select(usage => $"{usage.Key} ({usage.Value}, permitidos {KnownUsages.GetValueOrDefault(usage.Key)})")
            .ToList();

        offenders.ShouldBeEmpty(
            "El código nuevo obtiene la hora de un TimeProvider inyectado, no de DateTime.UtcNow/Now " +
            "(docs/adr/0025-timeprovider-en-todo-el-codigo.md). Archivos con lecturas nuevas: " +
            string.Join(", ", offenders));
    }

    [Fact(DisplayName = "Known system clock reads list must not be stale")]
    public void KnownUsages_Should_Match_Code()
    {
        var actual = CountUsages();
        var stale = KnownUsages
            .Where(known => actual.GetValueOrDefault(known.Key) < known.Value)
            .Select(known => $"{known.Key} (lista {known.Value}, código {actual.GetValueOrDefault(known.Key)})")
            .ToList();

        stale.ShouldBeEmpty(
            "Se retiraron lecturas del reloj del sistema: baja el contador (o quita la entrada) en " +
            "SystemClockUsageTests.KnownUsages para que no vuelvan a entrar. Entradas: " +
            string.Join(", ", stale));
    }

    private static Dictionary<string, int> CountUsages()
    {
        var root = FindRepositoryRoot();
        var usages = new Dictionary<string, int>(StringComparer.Ordinal);
        var scannedFiles = 0;

        foreach (var project in root.EnumerateDirectories(ProductionProjectPattern))
        {
            foreach (var file in project.EnumerateFiles("*.cs", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(root.FullName, file.FullName).Replace('\\', '/');
                if (relativePath.Split('/').Any(segment => SkippedSegments.Contains(segment, StringComparer.Ordinal)))
                {
                    continue;
                }

                scannedFiles++;
                var count = SystemClockRead().Count(File.ReadAllText(file.FullName));
                if (count > 0)
                {
                    usages[relativePath] = count;
                }
            }
        }

        scannedFiles.ShouldBeGreaterThan(0,
            "No se escaneó ningún archivo: revisa que el escaneo apunta a los proyectos de producción.");
        return usages;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFile)))
            {
                return directory;
            }
        }

        throw new InvalidOperationException(
            $"No se encontró {SolutionFile} subiendo desde {AppContext.BaseDirectory}; el test necesita el código fuente.");
    }
}
