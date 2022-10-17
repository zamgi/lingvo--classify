using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

using lingvo.tokenizing;

namespace lingvo.classify
{
    /// <summary>
    /// 
    /// </summary>
    public class ClassifyEnvironmentConfigImpl : ClassifyEnvironmentConfigBase
    {
        public ClassifyEnvironmentConfigImpl()
        {
            URL_DETECTOR_RESOURCES_XML_FILENAME = ConfigurationManager.AppSettings[ "URL_DETECTOR_RESOURCES_XML_FILENAME"  ];
            MODEL_NGRAMS_TYPE                   = Enum.Parse< NGramsType >( ConfigurationManager.AppSettings[ "MODEL_NGRAMS_TYPE" ], true );
            MODEL_ROW_CAPACITY                  = int.Parse( ConfigurationManager.AppSettings[ "MODEL_ROW_CAPACITY" ] );
            CLASS_THRESHOLD_PERCENT             = int.Parse( ConfigurationManager.AppSettings[ "CLASS_THRESHOLD_PERCENT" ] );

            var lst = new List< string >();
            for ( var i = 0; ; i++ )
            {
                var value = ConfigurationManager.AppSettings[ "CLASS_INDEX_" + i ];
                if ( string.IsNullOrWhiteSpace( value ) )
                    break;
                lst.Add( value );
            }
            CLASS_INDEX_NAMES = lst;


            MODEL_FOLDER = ConfigurationManager.AppSettings[ "MODEL_FOLDER"    ];
            var model_filenames = ConfigurationManager.AppSettings[ "MODEL_FILENAMES" ];

            MODEL_FILENAMES = (from raw_model_filename in model_filenames.Split( new[] { ';' }, StringSplitOptions.RemoveEmptyEntries )
                               let model_filename = raw_model_filename.Trim()
                               let full_model_filename = Path.Combine( MODEL_FOLDER, model_filename )
                               select full_model_filename
                              ).ToList();
        }

        public override string URL_DETECTOR_RESOURCES_XML_FILENAME  { get; }
        public override string MODEL_FOLDER { get; }
        public override IReadOnlyList< string > MODEL_FILENAMES   { get; }
        public override IReadOnlyList< string > CLASS_INDEX_NAMES { get; }
        public override NGramsType MODEL_NGRAMS_TYPE { get; }
        public override int MODEL_ROW_CAPACITY { get; }
        public override int CLASS_THRESHOLD_PERCENT { get; }
    }
}
