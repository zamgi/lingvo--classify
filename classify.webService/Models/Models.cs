using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Http;

using captcha;
using lingvo.classify;
using JP = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace classify.webService
{
    /// <summary>
    /// 
    /// </summary>
    public struct InitParamsVM
    {
        public string Text { get; set; }

#if DEBUG
        public override string ToString() => Text;
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    internal readonly struct ResultVM
    {
        /// <summary>
        /// 
        /// </summary>
        public struct classify_info
        {
            [JP("i")] public int    class_index { get; set; }
            [JP("n")] public string class_name  { get; set; }
            [JP("p")] public string percent     { get; set; }
        }

        public ResultVM( in InitParamsVM m, Exception ex ) : this() => (init_params, exception_message) = (m, ex.Message);
        public ResultVM( in InitParamsVM m, IList< ClassifyInfo > classifyInfos, IConfig cfg ) : this()
        {
            init_params = m;
            if ( classifyInfos != null && classifyInfos.Count != 0 )
            {
                var sum = classifyInfos.Sum( ci => ci.Cosine );
                classify_infos = (from ci in classifyInfos
                                  let percent = (ci.Cosine / sum) * 100
                                  where (cfg.CLASS_THRESHOLD_PERCENT <= percent)
                                  select
                                    new classify_info()
                                    {
                                        class_index = ci.ClassIndex,
                                        class_name  = cfg.ClassIndex2Text( ci.ClassIndex ),
                                        percent     = percent.ToString( "N2" ),
                                    }
                                 ).ToList();
            }
        }

        [JP("ip")     ] public InitParamsVM                   init_params       { get; }
        [JP("classes")] public IReadOnlyList< classify_info > classify_infos    { get; }
        [JP("err")    ] public string                         exception_message { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class CaptchaVM
    {
        public int    WaitRemainSeconds    { get; init; }
        public string AllowContinueUrl     { get; init; } = Startup.INDEX_PAGE_PATH; //"/index.html";
        public string CaptchaImageUniqueId { get; init; }
        
        
        public string ErrorMessage { get; init; }
        public bool HasError => (ErrorMessage != null);
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class Captcha_ProcessVM
    {
        public string CaptchaUserText      { get; set; }
        public string CaptchaImageUniqueId { get; set; }
        public string RedirectLocation     { get; set; } = '~' + Startup.INDEX_PAGE_PATH; //"~/index.html";
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class AntiBotHelper
    {
        private const string LOAD_MODEL_DUMMY_TEXT = "_dummy_";

        public static AntiBot ToAntiBot( this HttpContext httpContext, IConfig config )
        {
            var cfg = new AntiBotConfig() 
            { 
                //HttpContext                    = httpContext, 
                RemoteIpAddress                = httpContext.Connection.RemoteIpAddress?.ToString(),
                SameIpBannedIntervalInSeconds  = config.SAME_IP_BANNED_INTERVAL_IN_SECONDS,
                SameIpIntervalRequestInSeconds = config.SAME_IP_INTERVAL_REQUEST_IN_SECONDS,
                SameIpMaxRequestInInterval     = config.SAME_IP_MAX_REQUEST_IN_INTERVAL,
            };
            var antiBot = new AntiBot( cfg );
            return (antiBot);
        }

        public static void MarkRequestEx( this AntiBot antiBot, string text )
        {
            if ( text != LOAD_MODEL_DUMMY_TEXT )
            {
                antiBot.MarkRequest();
            }
        }
    }
}
