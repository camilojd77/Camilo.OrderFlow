using Shouldly;
using System.Text.RegularExpressions;
using Xunit;

namespace Camilo.OrderFlow.ArchitectureTests;

/// <summary>
/// ADR-0017: new production code gets the time from an injected <see cref="TimeProvider"/>, never from
/// DateTime.UtcNow / DateTime.Now / DateTimeOffset.UtcNow. Existing reads are listed in <see cref="KnownUsages"/>
/// with their exact count per file; the list may only shrink as code is migrated.
/// The check scans source files because NetArchTest cannot detect member access on System.DateTime.
/// </summary>
public sealed partial class SystemClockUsageTests
{
    private const string SolutionFile = "Camilo.OrderFlow.slnx";
    private const string ProductionProjectPattern = "Camilo.OrderFlow.*";
    private static readonly string[] SkippedSegments = ["bin", "obj", "Migrations"];

    /// <summary>System clock reads that existed when ADR-0017 was adopted (2026-11-05).</summary>
    private static readonly IReadOnlyDictionary<string, int> KnownUsages =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Camilo.OrderFlow.BuildingBlocks.Infrastructure/Time/SystemDateTimeProvider.cs"] = 1,
            ["Camilo.OrderFlow.Modules.Identity.Domain/Entities/RefreshToken.cs"] = 1,
            ["Camilo.OrderFlow.Modules.Identity.Domain/Entities/User.cs"] = 9,
            ["Camilo.OrderFlow.Modules.Identity.Infrastructure/Auth/AuthService.cs"] = 4,
            ["Camilo.OrderFlow.Modules.Identity.Infrastructure/Auth/JwtTokenService.cs"] = 1,
            ["Camilo.OrderFlow.Modules.Identity.Infrastructure/Persistence/Repositories/RefreshTokenRepository.cs"] = 1,
            ["Camilo.OrderFlow.WebApi/Controllers/AuthController.cs"] = 1,
            ["Camilo.OrderFlow.WebApi/Controllers/InfoController.cs"] = 2,
        };

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
            "(docs/adr/0017-timeprovider-en-codigo-nuevo.md). Archivos con lecturas nuevas: " +
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

        foreach (var project in root.EnumerateDirectories(ProductionProjectPattern))
        {
            foreach (var file in project.EnumerateFiles("*.cs", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(root.FullName, file.FullName).Replace('\\', '/');
                if (relativePath.Split('/').Any(segment => SkippedSegments.Contains(segment, StringComparer.Ordinal)))
                {
                    continue;
                }

                var count = SystemClockRead().Count(File.ReadAllText(file.FullName));
                if (count > 0)
                {
                    usages[relativePath] = count;
                }
            }
        }

        usages.ShouldNotBeEmpty(
            "No se encontró ninguna lectura del reloj: revisa que el escaneo apunta a los proyectos de producción.");
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
