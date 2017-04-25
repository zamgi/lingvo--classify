using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Web;

using Newtonsoft.Json;
using lingvo.tokenizing;
using System.Text;

namespace lingvo.classify
{
    /// <summary>
    /// Summary description for RESTProcessHandler
    /// </summary>
    public sealed class RESTProcessHandler : IHttpHandler
    {
        /// <summary>
        /// 
        /// </summary>
        private struct result
        {
            /// <summary>
            /// 
            /// </summary>
            public struct classify_info
            {
                [JsonProperty(PropertyName="i")] public int    class_index
                {
                    get;
                    set;
                }
                [JsonProperty(PropertyName="n")] public string class_name
                {
                    get;
                    set;
                }
                [JsonProperty(PropertyName="p")] public string percent
                {
                    get;
                    set;
                }
            }

            public result( Exception ex ) : this()
            {
                exception_message = ex.Message;
            }
            public result( ClassifyInfo[] classifyInfos, double classThresholdPercent ) : this()
            {
                if ( classifyInfos != null && classifyInfos.Length != 0 )
                {
                    var sum = classifyInfos.Sum( ci => ci.Cosine );
                    classify_infos = (from ci in classifyInfos
                                        let percent = (ci.Cosine / sum) * 100
                                        where ( classThresholdPercent <= percent )
                                      select
                                        new classify_info()
                                        {
                                            class_index = ci.ClassIndex,
                                            class_name  = ClassIndex2Text( ci.ClassIndex ),
                                            percent     = percent.ToString( Config.N2, Config.NFI ),
                                        }
                                     ).ToArray();
                }
            }

            [JsonProperty(PropertyName="classes")]
            public classify_info[] classify_infos
            {
                get;
                private set;
            }
            [JsonProperty(PropertyName="err")]
            public string          exception_message
            {
                get;
                private set;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private static class http_context_data
        {
            private static readonly object _SyncLock = new object();
        
            private static ConcurrentFactory _ConcurrentFactory;

            public static ConcurrentFactory GetConcurrentFactory()
            {
                var f = _ConcurrentFactory;
                if ( f == null )
                {
                    lock ( _SyncLock )
                    {
                        f = _ConcurrentFactory;
                        if ( f == null )
                        {
                            {
                                var modelConfig = new ModelConfig()
                                {
                                    Filenames   = Config.MODEL_FILENAMES,
                                    RowCapacity = Config.MODEL_ROW_CAPACITY, 
                                    NGramsType  = Config.MODEL_NGRAMS_TYPE 
                                };
                                var model  = new ModelNative( modelConfig ); //new ModelHalfNative( modelConfig ); //new ModelClassic( modelConfig );
                                var config = new ClassifierConfig( Config.URL_DETECTOR_RESOURCES_XML_FILENAME );

                                f = new ConcurrentFactory( config, model, Config.CONCURRENT_FACTORY_INSTANCE_COUNT );
                                _ConcurrentFactory = f;
                            }
                            {
                                GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
                                GC.WaitForPendingFinalizers();
                                GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
                            }
                        }
                    }
                }
                return (f);
            }
        }

        static RESTProcessHandler()
        {
            Environment.CurrentDirectory = HttpContext.Current.Server.MapPath( "~/" );

            GCSettings.LatencyMode = GCLatencyMode.LowLatency;
            if ( GCSettings.LatencyMode != GCLatencyMode.LowLatency )
            {
                GCSettings.LatencyMode = GCLatencyMode.Batch;
            }
        }

        public bool IsReusable
        {
            get { return (true); }
        }

        public void ProcessRequest( HttpContext context )
        {
            #region [.log.]
            if ( Log.ProcessViewCommand( context ) )
            {
                return;
            }
            #endregion

            var text = default(string);
            try
            {
                #region [.anti-bot.]
                var antiBot = context.ToAntiBot();
                if ( antiBot.IsNeedRedirectOnCaptchaIfRequestNotValid() )
                {
                    antiBot.SendGotoOnCaptchaJsonResponse();
                    return;
                }                
                #endregion

                text = context.GetRequestStringParam( "text", Config.MAX_INPUTTEXT_LENGTH );

                #region [.anti-bot.]
                antiBot.MarkRequestEx( text );
                #endregion

                var classifyInfos = http_context_data.GetConcurrentFactory().MakeClassify( text );

                Log.Info( context, text );
                SendJsonResponse( context, classifyInfos, Config.CLASS_THRESHOLD_PERCENT );                
            }
            catch ( Exception ex )
            {
                Log.Error( context, text, ex );
                SendJsonResponse( context, ex );
            }
        }

        private static void SendJsonResponse( HttpContext context, ClassifyInfo[] classifyInfos, double classThresholdPercent )
        {
            SendJsonResponse( context, new result( classifyInfos, classThresholdPercent ) );
        }
        private static void SendJsonResponse( HttpContext context, Exception ex )
        {
            SendJsonResponse( context, new result( ex ) );
        }
        private static void SendJsonResponse( HttpContext context, result result )
        {
            context.Response.ContentType = "application/json";
            //---context.Response.Headers.Add( "Access-Control-Allow-Origin", "*" );

            var json = JsonConvert.SerializeObject( result );
            context.Response.Write( json );
        }

        private static string ClassIndex2Text( int classIndex )
        {
            if ( 0 <= classIndex && classIndex < Config.CLASS_INDEX_NAMES.Length )
            {
                return (Config.CLASS_INDEX_NAMES[ classIndex ]);
            }
            return ("[class-index: " + classIndex + "]");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        public static string GetRequestStringParam( this HttpContext context, string paramName, int maxLength )
        {
            var value = context.Request[ paramName ];
            if ( (value != null) && (maxLength < value.Length) && (0 < maxLength) )
            {
                return (value.Substring( 0, maxLength ));
            }
            return (value);
        }
    }
}