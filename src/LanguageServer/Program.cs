﻿using Autofac;
using OmniSharp.Extensions.LanguageServer;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using LSP = OmniSharp.Extensions.LanguageServer;

namespace MSBuildProjectTools.LanguageServer
{
    using Documents;
    using Handlers;
    using Logging;
    using Utilities;

    /// <summary>
    ///     The MSBuild language server.
    /// </summary>
    static class Program
    {
        /// <summary>
        ///     The main program entry-point.
        /// </summary>
        static void Main()
        {
            SynchronizationContext.SetSynchronizationContext(
                new SynchronizationContext()
            );

            try
            {
                AutoDetectExtensionDirectory();

                AsyncMain().Wait();
            }
            catch (AggregateException aggregateError)
            {
                foreach (Exception unexpectedError in aggregateError.Flatten().InnerExceptions)
                {
                    Console.WriteLine(unexpectedError);
                }
            }
            catch (Exception unexpectedError)
            {
                Console.WriteLine(unexpectedError);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        ///     The asynchronous program entry-point.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing program execution.
        /// </returns>
        static async Task AsyncMain()
        {
            string serverVersion = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            using (ActivityCorrelationManager.BeginActivityScope())
            using (IContainer container = BuildContainer())
            {
                var server = container.Resolve<LSP.Server.LanguageServer>();

                Log.Verbose("Language server v{ServerVersion} starting in process {ProcessId}.",
                    serverVersion ?? "???",
                    Process.GetCurrentProcess().Id
                );

                await server.Initialize();
                await server.WasShutDownOrParentProcessTerminated();

                Log.Information("Server is shutting down...");

                // AF: Temporary fix for tintoy/msbuild-project-tools-vscode#36
                //
                //     The server hangs while waiting for LSP's ProcessScheduler thread to terminate so, after a timeout has elapsed, we forcibly terminate this process.
#pragma warning disable CS4014 // Fire-and-forget.
                Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(_ =>
                {
                    Log.Warning("Server failed to shut down cleanly after 5 seconds; process will now be forcibly terminated.");
                    Log.CloseAndFlush();

                    Environment.Exit(1);
                });
#pragma warning restore CS4014
            }

            Log.Information("Server has shut down.");
        }

        /// <summary>
        ///     Build a container for language server components.
        /// </summary>
        /// <returns>
        ///     The container.
        /// </returns>
        static IContainer BuildContainer()
        {
            ContainerBuilder builder = new ContainerBuilder();
            
            builder.RegisterModule<LoggingModule>();
            builder.RegisterModule<LanguageServerModule>();

            return builder.Build();
        }

        /// <summary>
        ///     Auto-detect the directory containing the extension's files.
        /// </summary>
        static void AutoDetectExtensionDirectory()
        {
            string extensionDir = Environment.GetEnvironmentVariable("MSBUILD_PROJECT_TOOLS_DIR");
            if (String.IsNullOrWhiteSpace(extensionDir))
            {
                extensionDir = Path.Combine(
                    AppContext.BaseDirectory, "..", ".."
                );
            }
            extensionDir = Path.GetFullPath(extensionDir);
            Environment.SetEnvironmentVariable("MSBUILD_PROJECT_TOOLS_DIR", extensionDir);
        }
    }
}
