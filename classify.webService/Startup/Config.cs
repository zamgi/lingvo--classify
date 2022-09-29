using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

using captcha;
using lingvo.tokenizing;

namespace classify.webService
{
    /// <summary>
    /// 
    /// </summary>
    public interface IConfig
    {
        int CLASS_THRESHOLD_PERCENT { get; }
        int CONCURRENT_FACTORY_INSTANCE_COUNT { get; }
        NGramsType MODEL_NGRAMS_TYPE { get; }
        int MODEL_ROW_CAPACITY { get; }
        string URL_DETECTOR_RESOURCES_XML_FILENAME { get; }

        string[] CLASS_INDEX_NAMES { get; }
        string[] MODEL_FILENAMES { get; }

        string ClassIndex2Text( int classIndex );


        int SAME_IP_INTERVAL_REQUEST_IN_SECONDS  { get; }
        int SAME_IP_MAX_REQUEST_IN_INTERVAL      { get; }
        int SAME_IP_BANNED_INTERVAL_IN_SECONDS   { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class Config : IConfig
    {
        public Config()
        {
            var lst = new List<string>();
            for ( var i = 0; ; i++ )
            {
                var value = ConfigurationManager.AppSettings[ "CLASS_INDEX_" + i ];
                if ( string.IsNullOrWhiteSpace( value ) )
                    break;
                lst.Add( value );
            }
            CLASS_INDEX_NAMES = lst.ToArray();


            var model_folder = ConfigurationManager.AppSettings[ "MODEL_FOLDER" ];
            var model_filenames = ConfigurationManager.AppSettings[ "MODEL_FILENAMES" ];

            MODEL_FILENAMES = (from raw_model_filename in model_filenames.Split( new[] { ';' }, StringSplitOptions.RemoveEmptyEntries )
                               let model_filename = raw_model_filename.Trim()
                               let full_model_filename = Path.Combine( model_folder, model_filename )
                               select full_model_filename
                              ).ToArray();
        }

        public string[] MODEL_FILENAMES { get; }
        public string[] CLASS_INDEX_NAMES { get; }

        public string URL_DETECTOR_RESOURCES_XML_FILENAME { get; } = ConfigurationManager.AppSettings[ "URL_DETECTOR_RESOURCES_XML_FILENAME" ];
        public NGramsType MODEL_NGRAMS_TYPE { get; } = (NGramsType) Enum.Parse( typeof( NGramsType ), ConfigurationManager.AppSettings[ "MODEL_NGRAMS_TYPE" ], true );
        public int MODEL_ROW_CAPACITY { get; } = int.Parse( ConfigurationManager.AppSettings[ "MODEL_ROW_CAPACITY" ] );
        public int CLASS_THRESHOLD_PERCENT { get; } = int.Parse( ConfigurationManager.AppSettings[ "CLASS_THRESHOLD_PERCENT" ] );

        public int CONCURRENT_FACTORY_INSTANCE_COUNT { get; } = int.Parse( ConfigurationManager.AppSettings[ "CONCURRENT_FACTORY_INSTANCE_COUNT" ] );

        public int SAME_IP_INTERVAL_REQUEST_IN_SECONDS  { get; } = int.TryParse( ConfigurationManager.AppSettings[ "SAME_IP_INTERVAL_REQUEST_IN_SECONDS" ], out var i ) ? i : AntiBotConfig.SAME_IP_BANNED_INTERVAL_IN_SECONDS;
        public int SAME_IP_MAX_REQUEST_IN_INTERVAL      { get; } = int.TryParse( ConfigurationManager.AppSettings[ "SAME_IP_MAX_REQUEST_IN_INTERVAL"     ], out var i ) ? i : AntiBotConfig.SAME_IP_MAX_REQUEST_IN_INTERVAL;
        public int SAME_IP_BANNED_INTERVAL_IN_SECONDS   { get; } = int.TryParse( ConfigurationManager.AppSettings[ "SAME_IP_BANNED_INTERVAL_IN_SECONDS"  ], out var i ) ? i : AntiBotConfig.SAME_IP_BANNED_INTERVAL_IN_SECONDS;

        public string ClassIndex2Text( int classIndex )
        {
            if ( 0 <= classIndex && classIndex < CLASS_INDEX_NAMES.Length )
            {
                return (CLASS_INDEX_NAMES[ classIndex ]);
            }
            return ("[class-index: " + classIndex + "]");
        }
    }
}
