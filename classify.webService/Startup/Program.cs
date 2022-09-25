using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

using lingvo.classify;

namespace classify.webService
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        public const string SERVICE_NAME = "classify.webService";

        private static async Task Main( string[] args )
        {
            var hostApplicationLifetime = default(IHostApplicationLifetime);
            var logger                  = default(ILogger);
            try
            {
                Encoding.RegisterProvider( CodePagesEncodingProvider.Instance );

                //---------------------------------------------------------------//
                var cfg = new Config();
                var modelConfig = new ModelConfig()
                {
                    Filenames   = cfg.MODEL_FILENAMES,
                    RowCapacity = cfg.MODEL_ROW_CAPACITY, 
                    NGramsType  = cfg.MODEL_NGRAMS_TYPE 
                };
                using var model  = new ModelNative( modelConfig ); //new ModelHalfNative( modelConfig ); //new ModelClassic( modelConfig );
                var config = new ClassifierConfig( cfg.URL_DETECTOR_RESOURCES_XML_FILENAME );

                var concurrentFactory = new ConcurrentFactory( config, model, cfg );
                //---------------------------------------------------------------//

                var host = Host.CreateDefaultBuilder( args )
                               .ConfigureLogging( loggingBuilder => loggingBuilder.ClearProviders().AddDebug().AddConsole().AddEventSourceLogger()
                                                              .AddEventLog( new EventLogSettings() { LogName = SERVICE_NAME, SourceName = SERVICE_NAME } ) )
                               //---.UseWindowsService()
                               .ConfigureServices( (hostContext, services) => services.AddSingleton< IConfig >( cfg ).AddSingleton( concurrentFactory ) )
                               .ConfigureWebHostDefaults( webBuilder => webBuilder.UseStartup< Startup >() )
                               .Build();
                hostApplicationLifetime = host.Services.GetService< IHostApplicationLifetime >();
                logger                  = host.Services.GetService< ILoggerFactory >()?.CreateLogger( SERVICE_NAME );
                await host.RunAsync();
            }
            catch ( OperationCanceledException ex ) when ((hostApplicationLifetime?.ApplicationStopping.IsCancellationRequested).GetValueOrDefault())
            {
                Debug.WriteLine( ex ); //suppress
            }
            catch ( Exception ex ) when (logger != null)
            {
                logger.LogCritical( ex, "Global exception handler" );
            }
        }
    }
}
