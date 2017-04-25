using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;

using lingvo.tokenizing;

namespace lingvo
{
    /// <summary>
    /// 
    /// </summary>
    public static class Config
    {
        static Config()
        {
            var lst = new List< string >();
            for ( var i = 0; ; i++ )
            {
                var value = ConfigurationManager.AppSettings[ "CLASS_INDEX_" + i ];
                if ( string.IsNullOrWhiteSpace( value ) )
                    break;
                lst.Add( value );
            }
            CLASS_INDEX_NAMES = lst.ToArray();


            var model_folder    = ConfigurationManager.AppSettings[ "MODEL_FOLDER"    ];
            var model_filenames = ConfigurationManager.AppSettings[ "MODEL_FILENAMES" ];

            MODEL_FILENAMES = (from raw_model_filename in model_filenames.Split( new[] { ';' }, StringSplitOptions.RemoveEmptyEntries )
                               let model_filename = raw_model_filename.Trim()
                               let full_model_filename = Path.Combine( model_folder, model_filename )
                               select full_model_filename
                              ).ToArray();
        }

        public static readonly NumberFormatInfo NFI = new NumberFormatInfo() { NumberDecimalSeparator = "." };
        public static readonly string           N2  = "N2";
        public static readonly string     URL_DETECTOR_RESOURCES_XML_FILENAME = ConfigurationManager.AppSettings[ "URL_DETECTOR_RESOURCES_XML_FILENAME" ];
        public static readonly string[]   MODEL_FILENAMES;
        public static readonly NGramsType MODEL_NGRAMS_TYPE                   = (NGramsType) Enum.Parse( typeof(NGramsType), ConfigurationManager.AppSettings[ "MODEL_NGRAMS_TYPE" ], true );
        public static readonly int        MODEL_ROW_CAPACITY                  = int.Parse( ConfigurationManager.AppSettings[ "MODEL_ROW_CAPACITY" ] );
        public static readonly int        CLASS_THRESHOLD_PERCENT             = int.Parse( ConfigurationManager.AppSettings[ "CLASS_THRESHOLD_PERCENT" ] );        
        public static readonly string[]   CLASS_INDEX_NAMES;

        public static readonly int    MAX_INPUTTEXT_LENGTH                    = int.Parse( ConfigurationManager.AppSettings[ "MAX_INPUTTEXT_LENGTH" ] );
        public static readonly int    CONCURRENT_FACTORY_INSTANCE_COUNT       = int.Parse( ConfigurationManager.AppSettings[ "CONCURRENT_FACTORY_INSTANCE_COUNT" ] );
        public static readonly int    SAME_IP_INTERVAL_REQUEST_IN_SECONDS     = int.Parse( ConfigurationManager.AppSettings[ "SAME_IP_INTERVAL_REQUEST_IN_SECONDS" ] );
        public static readonly int    SAME_IP_MAX_REQUEST_IN_INTERVAL         = int.Parse( ConfigurationManager.AppSettings[ "SAME_IP_MAX_REQUEST_IN_INTERVAL" ] );        
        public static readonly int    SAME_IP_BANNED_INTERVAL_IN_SECONDS      = int.Parse( ConfigurationManager.AppSettings[ "SAME_IP_BANNED_INTERVAL_IN_SECONDS" ] );
    }
}
