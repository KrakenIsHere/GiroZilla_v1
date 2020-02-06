using System.Windows;
using PyroSquidUniLib.Extensions;
using PyroSquidUniLib.FileSystem;
using Serilog;
using Serilog.Events;


namespace GiroZilla
{
    public partial class App
    {
        private string CompanyName()
        {
            var address = PropertiesExtension.Get<string>("AddressLine");

            var temp = address.Split('+')[0];

            if (string.IsNullOrWhiteSpace(temp))
            {
                temp = "No Company";
            }

            return temp;
        }

        private void OnApplicationStartup(object sender, StartupEventArgs e)
        {
            switch (string.IsNullOrWhiteSpace(PropertiesExtension.Get<string>("LogsPath")))
            {
                case true:
                    {
                        PropertiesExtension.Set("LogsPath", $@"{DefaultDirectories.AppData}\GiroZilla\Logs");
                        break;
                    }
            }

            //Log formats
            const string outputTemplate = "{Timestamp:HH:mm:ss zzz}{NewLine}{Level} | Thread: {ThreadId} | Source: {SourceContext} | Message: {Message}{NewLine}{Exception}{NewLine}";
            //const string summaryFormat = "{Timestamp:dd/MM/yyyy} [{Level}] {Message}";
            //const string descriptionFormat = "{Timestamp:HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}";

            //var file = File.CreateText($@"{DefaultDirectories.CurrentUserDesktop}\Serilog.log");                                  // Create a new file for SeriLoggers 'SelfLog'
            //Serilog.Debugging.SelfLog.Enable(TextWriter.Synchronized(file));                                                      // Debug serilog and create a new log file for information.

            Log.Logger = new LoggerConfiguration()                                                                                  // Logging configuration for serilog.
                .MinimumLevel.Debug()                                                                                               // Serilog implements the common concept of a 'minimum level' for log event processing.
                .Enrich.WithThreadId()                                                                                              // Adds a ThreadID to the log events 
                .Enrich.FromLogContext()                                                                                            // Adds properties from "LogContext" to the event log.
                .Enrich.WithProperty("Customer", CompanyName())

                //.WriteTo.Console(                                                                                                 // Sink configured to the console.
                //   LogEventLevel.Information,                                                                                     // The minimum level for events passed through the sink.
                //   outputTemplate)                                                                                                // A message template describing the format used to write to the sink.

                .WriteTo.File($@"{PropertiesExtension.Get<string>("LogsPath")}\GiroZilla_.log",                                     // Sink configured for physical files.
                    LogEventLevel.Information,                                                                                      // The minimum level for events passed through the sink.
                    outputTemplate,                                                                                                 // A message template describing the format used to write to the sink.
                    rollingInterval: RollingInterval.Day)                                                                           // The interval which logging will roll over to a new file.

                .WriteTo.MySQL(PropertiesExtension.Get<string>("LicenseConnString"),                                                // Sink configured for database entries.
                    restrictedToMinimumLevel: LogEventLevel.Warning)                                                                // The minimum level for events passed through the sink.


                .CreateLogger();                                                                                                    // Create the logger using the configured minimum level, enrichers & sinks.
        }
    }
}
