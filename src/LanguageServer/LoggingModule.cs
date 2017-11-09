using Autofac;
using OmniSharp.Extensions.LanguageServer;
using Serilog;
using Serilog.Events;
using System;

using MSLogging = Microsoft.Extensions.Logging;

namespace MSBuildProjectTools.LanguageServer
{
    using Logging;
    
    using LanguageServer = OmniSharp.Extensions.LanguageServer.LanguageServer;

    /// <summary>
    ///     Registration logic for logging components.
    /// </summary>
    public class LoggingModule
        : Module
    {
        /// <summary>
        ///     Create a new <see cref="LoggingModule"/>.
        /// </summary>
        public LoggingModule()
        {
        }

        /// <summary>
        ///     Configure logging components.
        /// </summary>
        /// <param name="builder">
        ///     The container builder to configure.
        /// </param>
        protected override void Load(ContainerBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            
            builder.Register(CreateLogger)
                .SingleInstance()
                .As<ILogger>();

            builder.RegisterType<MSLogging.LoggerFactory>()
                .As<MSLogging.ILoggerFactory>()
                .SingleInstance()
                .OnActivated(activation =>
                {
                    activation.Instance.AddSerilog(
                        logger: activation.Context.Resolve<ILogger>().ForContext<LanguageServer>()
                    );
                });
        }

        /// <summary>
        ///     Create the application logger.
        /// </summary>
        /// <param name="componentContext">
        ///     The current component context.
        /// </param>
        /// <returns>
        ///     The logger.
        /// </returns>
        static ILogger CreateLogger(IComponentContext componentContext)
        {
            if (componentContext == null)
                throw new ArgumentNullException(nameof(componentContext));
            
            Configuration configuration = componentContext.Resolve<Configuration>();
            ConfigureSeq(configuration.Logging.Seq);

            // Override default log level.
            if (Environment.GetEnvironmentVariable("MSBUILD_PROJECT_TOOLS_VERBOSE_LOGGING") == "1")
            {
                configuration.Logging.LevelSwitch.MinimumLevel = LogEventLevel.Verbose;
                configuration.Logging.Seq.LevelSwitch.MinimumLevel = LogEventLevel.Verbose;
            }

            ILanguageServer languageServer = componentContext.Resolve<ILanguageServer>();

            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.WithCurrentActivityId()
                .Enrich.FromLogContext();

            if (!String.IsNullOrWhiteSpace(configuration.Logging.Seq.Url))
            {
                loggerConfiguration = loggerConfiguration.WriteTo.Seq(configuration.Logging.Seq.Url,
                    apiKey: configuration.Logging.Seq.ApiKey,
                    controlLevelSwitch: configuration.Logging.Seq.LevelSwitch
                );
            }

            string logFilePath = Environment.GetEnvironmentVariable("MSBUILD_PROJECT_TOOLS_LOG_FILE");
            if (!String.IsNullOrWhiteSpace(logFilePath))
            {
                loggerConfiguration = loggerConfiguration.WriteTo.File(
                    path: logFilePath,
                    levelSwitch: configuration.Logging.LevelSwitch,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}/{Operation}] {Message}{NewLine}{Exception}",
                    flushToDiskInterval: TimeSpan.FromSeconds(1)
                );
            }

            loggerConfiguration = loggerConfiguration.WriteTo.LanguageServer(languageServer, configuration.Logging.LevelSwitch);

            ILogger logger = loggerConfiguration.CreateLogger();
            Log.Logger = logger;

            Log.Verbose("Logger initialised.");

            return logger;
        }

        /// <summary>
        ///     Configure SEQ logging from environment variables.
        /// </summary>
        /// <param name="configuration">
        ///     The language server's Seq logging configuration.
        /// </param>
        static void ConfigureSeq(SeqLoggingConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));
            
            // We have to use environment variables here since at configuration time there's no LSP connection yet.
            configuration.Url = Environment.GetEnvironmentVariable("MSBUILD_PROJECT_TOOLS_SEQ_URL");
            configuration.ApiKey = Environment.GetEnvironmentVariable("MSBUILD_PROJECT_TOOLS_SEQ_API_KEY");
        }
    }
}
