using OptionsEdge.API.Common.Time;
using Serilog.Core;
using Serilog.Events;

namespace OptionsEdge.API.Infrastructure.Logging;

public class IstTimestampEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var istTimestamp = IndiaTime.ToIst(logEvent.Timestamp);
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("IstTimestamp", istTimestamp));
    }
}
