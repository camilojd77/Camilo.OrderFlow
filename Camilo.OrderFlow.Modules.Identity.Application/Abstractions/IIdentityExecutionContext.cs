using Camilo.OrderFlow.BuildingBlocks.SharedKernel.Persistence;

namespace Camilo.OrderFlow.Modules.Identity.Application.Abstractions;

/// <summary>
/// Punto de commit del módulo Identity.
///
/// Existe una interfaz por módulo — y no se inyecta <see cref="IExecutionContext"/>
/// directamente — porque <see cref="IExecutionContext"/> vive en SharedKernel y es
/// global: si dos módulos registraran esa misma interfaz, la última registración
/// ganaría y los handlers del otro módulo recibirían el DbContext equivocado.
/// El fallo sería silencioso (CommitAsync no lanzaría, simplemente no persistiría).
///
/// Al añadir un módulo nuevo, replicar este patrón: definir
/// <c>I&lt;Modulo&gt;ExecutionContext : IExecutionContext</c> en su capa Application y
/// registrar esa interfaz en su <c>Add&lt;Modulo&gt;Infrastructure</c>. El test
/// <c>ModuleRegistrationTests</c> falla si un módulo registra la interfaz genérica.
/// </summary>
public interface IIdentityExecutionContext : IExecutionContext;
