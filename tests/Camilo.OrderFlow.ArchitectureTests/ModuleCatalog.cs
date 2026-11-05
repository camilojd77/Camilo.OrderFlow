using System.Reflection;

namespace Camilo.OrderFlow.ArchitectureTests;

/// <summary>
/// Descubre los módulos presentes en la solución **por convención de nombre**, en vez de
/// listarlos a mano. Así, un módulo nuevo (<c>Camilo.OrderFlow.Modules.Catalog.*</c>) queda
/// cubierto por las reglas de arquitectura en cuanto se agrega, sin tocar los tests.
///
/// Los ensamblados se leen del directorio de salida del proyecto de tests: WebApi referencia
/// la Infrastructure de cada módulo, y esta a su vez Application y Domain, así que las tres
/// capas de todo módulo registrado en el host acaban copiadas junto a los tests.
/// </summary>
internal static class ModuleCatalog
{
    private const string Prefix = "Camilo.OrderFlow.Modules.";

    public const string WebApiAssembly = "Camilo.OrderFlow.WebApi";

    /// <summary>Las tres capas de un módulo. Cualquiera puede faltar si el módulo aún no la tiene.</summary>
    internal sealed record Module(string Name, Assembly? Domain, Assembly? Application, Assembly? Infrastructure)
    {
        public string DomainAssemblyName => $"{Prefix}{Name}.Domain";
        public string ApplicationAssemblyName => $"{Prefix}{Name}.Application";
        public string InfrastructureAssemblyName => $"{Prefix}{Name}.Infrastructure";
    }

    private static readonly Lazy<IReadOnlyDictionary<string, Module>> _modules = new(Discover);

    public static IReadOnlyDictionary<string, Module> Modules => _modules.Value;

    /// <summary>Nombres de módulo, en el formato que consumen los <c>[Theory]</c> vía <c>MemberData</c>.</summary>
    public static IEnumerable<object[]> ModuleNames() =>
        Modules.Keys.OrderBy(n => n, StringComparer.Ordinal).Select(n => new object[] { n });

    public static Module Get(string name) => Modules[name];

    private static IReadOnlyDictionary<string, Module> Discover()
    {
        var layers = new Dictionary<string, Dictionary<string, Assembly>>(StringComparer.Ordinal);

        foreach (var path in Directory.EnumerateFiles(AppContext.BaseDirectory, $"{Prefix}*.dll"))
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            var parts = fileName[Prefix.Length..].Split('.');
            if (parts.Length != 2)
                continue;

            var (moduleName, layer) = (parts[0], parts[1]);
            if (layer is not ("Domain" or "Application" or "Infrastructure"))
                continue;

            // Assembly.LoadFrom: si ya está cargado devuelve la misma instancia, así que
            // NetArchTest ve exactamente los tipos que ejecuta el resto de la suite.
            var assembly = Assembly.LoadFrom(path);

            if (!layers.TryGetValue(moduleName, out var byLayer))
                layers[moduleName] = byLayer = new Dictionary<string, Assembly>(StringComparer.Ordinal);

            byLayer[layer] = assembly;
        }

        return layers.ToDictionary(
            kv => kv.Key,
            kv => new Module(
                kv.Key,
                kv.Value.GetValueOrDefault("Domain"),
                kv.Value.GetValueOrDefault("Application"),
                kv.Value.GetValueOrDefault("Infrastructure")),
            StringComparer.Ordinal);
    }
}
