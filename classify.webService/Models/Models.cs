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
        public readonly struct classify_info
        {
            [JP("i")] public int    class_index { get; init; }
            [JP("n")] public string class_name  { get; init; }
            [JP("p")] public string percent     { get; init; }
        }

        public ResultVM( in InitParamsVM m, Exception ex ) : this() => (init_params, exception_message) = (m, ex.Message);
        public ResultVM( in InitParamsVM m, IList< classes_t > classes ) : this()
        {
            init_params = m;            
            if ( (classes != null) && (classes.Count != 0) )
            {
                classify_infos = (from ci in classes
                                  select
                                    new classify_info()
                                    {
                                        class_index = ci.class_index,
                                        class_name  = ci.class_name,
                                        percent     = ci.percent.ToString("N2"),
                                    }
                                 ).ToList();
            }
        }

        [JP("ip")     ] public InitParamsVM                   init_params       { get; }
        [JP("classes")] public IReadOnlyList< classify_info > classify_infos    { get; }
        [JP("err")    ] public string                         exception_message { get; }
    }
}
