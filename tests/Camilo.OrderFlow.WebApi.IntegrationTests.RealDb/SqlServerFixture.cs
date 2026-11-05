using Testcontainers.MsSql;
using Xunit;

namespace Camilo.OrderFlow.WebApi.IntegrationTests.RealDb;

/// <summary>
/// Boots a real SQL Server 2022 container once per collection, then builds a
/// <see cref="RealDbWebApiFactory"/> bound to it. Tests share the same container
/// AND the same host so the fixed startup cost (container pull + migration apply)
/// is paid once per test run, not once per class.
///
/// Each test is responsible for its own data isolation by suffixing inputs with
/// a fresh GUID.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    // Testcontainers 4.14+ exige la imagen en el constructor; el ctor sin parametros
    // y WithImage(...) quedaron obsoletos.
    private readonly MsSqlContainer _container =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU27-GDR1-ubuntu-22.04").Build();

    public RealDbWebApiFactory Factory { get; private set; } = default!;

    // xunit.v3: IAsyncLifetime.InitializeAsync devuelve ValueTask y el cierre llega
    // por IAsyncDisposable, no por un DisposeAsync propio de la interfaz.
    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        Factory = new RealDbWebApiFactory(_container.GetConnectionString());

        // Force the host to build now so migrations run before any test does.
        _ = Factory.Server;
    }

    public async ValueTask DisposeAsync()
    {
        if (Factory is not null)
            await Factory.DisposeAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(nameof(SqlServerCollection))]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    // Marker type — xUnit uses this to share the fixture across the collection.
}
