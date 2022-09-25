using System;
using System.Collections.Generic;
using System.Linq;

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
}
