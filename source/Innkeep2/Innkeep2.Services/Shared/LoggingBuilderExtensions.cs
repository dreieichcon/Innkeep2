using Microsoft.Extensions.Logging;
using Serilog;

namespace Innkeep2.Services.Shared;

public static class LoggingBuilderExtensions
{
	/// <summary>
	/// Routes all logging of the host through the Serilog logger configured by <see cref="AppSetup.SetupLogging" />.
	/// </summary>
	/// <param name="logging">The logging builder of the host.</param>
	public static ILoggingBuilder UseSerilogOnly(this ILoggingBuilder logging)
	{
		logging.ClearProviders();

		return logging.AddSerilog(Log.Logger);
	}
}
