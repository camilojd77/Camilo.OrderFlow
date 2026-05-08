using OpenTelemetry;
using System.Diagnostics;

namespace Company.Product.WebApi.Observability.Processors;

/// <summary>
/// Strips SQL statement attributes from completed spans when statement capture is disabled.
/// Attach this processor to the tracing pipeline whenever SQL client instrumentation is active
/// so that production traces never expose raw query text, while still retaining duration,
/// dependency metadata, and error information.
/// </summary>
internal sealed class DbStatementRedactionProcessor : BaseProcessor<Activity>
{
    // Semantic Convention attributes emitted by OpenTelemetry.Instrumentation.SqlClient v1.x+.
    private const string DbQueryText = "db.query.text";
    private const string DbQuerySummary = "db.query.summary";
    private const string DbStatementLegacy = "db.statement";

    private readonly bool _captureStatements;

    internal DbStatementRedactionProcessor(bool captureStatements)
    {
        _captureStatements = captureStatements;
    }

    public override void OnEnd(Activity activity)
    {
        if (!_captureStatements && activity is not null)
        {
            // Only touch spans that actually look like DB spans
            var hasDbQuery =
                activity.GetTagItem(DbQueryText) is not null ||
                activity.GetTagItem(DbQuerySummary) is not null ||
                activity.GetTagItem(DbStatementLegacy) is not null;

            if (hasDbQuery)
            {
                activity.SetTag(DbQueryText, null);
                activity.SetTag(DbQuerySummary, null);
                activity.SetTag(DbStatementLegacy, null);
            }
        }

        base.OnEnd(activity!);
    }
}
