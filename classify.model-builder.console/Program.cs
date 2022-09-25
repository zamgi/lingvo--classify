﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;

using lingvo.core.algorithm;
using lingvo.tokenizing;
using lingvo.urls;

namespace lingvo.classify.modelbuilder
{
    /// <summary>
    /// 
    /// </summary>
    internal enum MethodEnum { tfidf, bm25, R_tfidf }
    /// <summary>
    /// 
    /// </summary>
    internal enum BuildModeEnum { single_model, all_models_by_method, all_possible_models }

    /// <summary>
    /// 
    /// </summary>
    internal static class Config
    {
		static Config()
		{
            var inputFiles = _INPUT_FILES_.Split( new[] { ';' }, StringSplitOptions.RemoveEmptyEntries );
			for ( var i = 0; i < inputFiles.Length; i++ )
			{
				var inf = inputFiles[ i ].Trim( ' ', '\r', '\n', '\t' );
				inputFiles[ i ] = inf;
			}
			INPUT_FILES = inputFiles.Where( _ => !string.IsNullOrWhiteSpace( _ ) ).ToArray();
		}

        #region [.config.]
		public static readonly bool          USE_BOOST_PRIORITY = bool.Parse( ConfigurationManager.AppSettings[ "USE_BOOST_PRIORITY" ] );
        public static readonly BuildModeEnum BUILD_MODE         = (BuildModeEnum) Enum.Parse( typeof(BuildModeEnum), ConfigurationManager.AppSettings[ "BUILD_MODE" ], true );

        public static readonly string URL_DETECTOR_RESOURCES_XML_FILENAME = ConfigurationManager.AppSettings[ "URL_DETECTOR_RESOURCES_XML_FILENAME" ];

        public static readonly NGramsEnum  NGARMS  = (NGramsEnum)  Enum.Parse( typeof(NGramsEnum ), ConfigurationManager.AppSettings[ "NGARMS"  ], true );
        public static readonly D_ParamEnum D_PARAM = (D_ParamEnum) Enum.Parse( typeof(D_ParamEnum), ConfigurationManager.AppSettings[ "D_PARAM" ], true );
        public static readonly MethodEnum  METHOD  = (MethodEnum)  Enum.Parse( typeof(MethodEnum), ConfigurationManager.AppSettings[ "METHOD" ], true );

        public  static readonly string[] INPUT_FILES;
        private static readonly string   _INPUT_FILES_  = ConfigurationManager.AppSettings[ "INPUT_FILES" ];
        public  static readonly string   INPUT_FOLDER   = ConfigurationManager.AppSettings[ "INPUT_FOLDER"  ];
        public  static readonly Encoding INPUT_ENCODING = Encoding.GetEncoding( ConfigurationManager.AppSettings[ "INPUT_ENCODING" ] );

        public static readonly string   OUTPUT_FILE_PATTERN = ConfigurationManager.AppSettings[ "OUTPUT_FILE_PATTERN" ];
        public static readonly Encoding OUTPUT_ENCODING     = Encoding.GetEncoding( ConfigurationManager.AppSettings[ "OUTPUT_ENCODING" ] );
        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
		private static IEnumerable< Tuple< MethodEnum, NGramsEnum, D_ParamEnum > > GetProcessParams()
		{
			foreach ( var method in Enum.GetValues( typeof( MethodEnum ) ).Cast< MethodEnum >() )
			{
				for ( var ngarms = NGramsEnum.ngram_1; ngarms <= NGramsEnum.ngram_4; ngarms++ )
				{
					for ( var d = D_ParamEnum.d0; d <= D_ParamEnum.d2; d++ )
					{
						yield return (Tuple.Create( method, ngarms, d ));
					}
				}
			}
		}
		private static IEnumerable< Tuple< MethodEnum, NGramsEnum, D_ParamEnum > > GetProcessParams( MethodEnum method )
		{
			for ( var ngarms = NGramsEnum.ngram_1; ngarms <= NGramsEnum.ngram_4; ngarms++ )
			{
				for ( var d = D_ParamEnum.d0; d <= D_ParamEnum.d2; d++ )
				{
					yield return (Tuple.Create( method, ngarms, d ));
				}
			}
		}

        private static void Main( string[] args )
        {
            var wasErrors = false;
            try
            {
                #region [.print to console config.]
                Console.WriteLine(Environment.NewLine + "----------------------------------------------");
                Console.WriteLine( "USE_BOOST_PRIORITY: '" + Config.USE_BOOST_PRIORITY + "'" );
                Console.WriteLine( "BUILD_MODE        : '" + Config.BUILD_MODE + "'");
                switch ( Config.BUILD_MODE )
                {
                    case BuildModeEnum.single_model:
                Console.WriteLine( "METHOD            : '" + Config.METHOD + "'" );
				Console.WriteLine( "NGARMS            : '" + Config.NGARMS  + "'" );
				Console.WriteLine( "D_PARAM           : '" + Config.D_PARAM + "'" );				
                    break;

                    case BuildModeEnum.all_models_by_method:
                Console.WriteLine( "METHOD            : '" + Config.METHOD + "'" );
                    break;
                }
				Console.WriteLine( "INPUT_FILES       : '" + string.Join( "'; '", Config.INPUT_FILES )    + "'" );
				Console.WriteLine( "INPUT_FOLDER      : '" + Config.INPUT_FOLDER    + "'" );
				Console.WriteLine( "INPUT_ENCODING    : '" + Config.INPUT_ENCODING.WebName  + "'" );
				Console.WriteLine( "OUTPUT_FILE       : '" + Config.OUTPUT_FILE_PATTERN   + "'" );
				Console.WriteLine( "OUTPUT_ENCODING   : '" + Config.OUTPUT_ENCODING.WebName + "'" );
				Console.WriteLine("----------------------------------------------" + Environment.NewLine);
                #endregion

                #region [.GC.]
                GCSettings.LatencyMode = GCLatencyMode.LowLatency;
                if ( GCSettings.LatencyMode != GCLatencyMode.LowLatency )
                {
                    GCSettings.LatencyMode = GCLatencyMode.Batch;
                }
                #endregion

                #region [.use boost priority.]
                if ( Config.USE_BOOST_PRIORITY )
				{
                    using ( var p = Process.GetCurrentProcess() )
                    {
                        p.PriorityClass        = ProcessPriorityClass.RealTime;
                        p.PriorityBoostEnabled = true;
                        Thread.CurrentThread.Priority = ThreadPriority.Highest;
                    }
				}
                #endregion

                #region [.url-detector.]
                var urlDetectorModel = new UrlDetectorModel( Config.URL_DETECTOR_RESOURCES_XML_FILENAME );
                #endregion

                #region [.build model's.]
                if ( Config.BUILD_MODE == BuildModeEnum.single_model )
                {
                    var bp = new build_params_t()
                    {
                        UrlDetectorModel      = urlDetectorModel,
                        InputFolder           = Config.INPUT_FOLDER,
                        InputFilenames        = Config.INPUT_FILES,
                        Method                = Config.METHOD,
                        Ngrams                = Config.NGARMS,
                        D_param               = Config.D_PARAM,
                        OutputFilenamePattern = Config.OUTPUT_FILE_PATTERN,
                    };
                    var sw = Stopwatch.StartNew();
                    Build( bp );
                    sw.Stop();

                    Console.WriteLine( "'" + Config.METHOD + "; " + Config.NGARMS + "; " + Config.D_PARAM + "' - success, elapsed: " + sw.Elapsed );
                }
                else
                {
                    var tuples = (Config.BUILD_MODE == BuildModeEnum.all_models_by_method) ? GetProcessParams( Config.METHOD ) : GetProcessParams();

                    #region [.build model's.]
                    var sw_total = Stopwatch.StartNew();
                    foreach ( var t in tuples )
                    {
                        var bp = new build_params_t()
                        {
                            UrlDetectorModel      = urlDetectorModel,
                            InputFolder           = Config.INPUT_FOLDER,
                            InputFilenames        = Config.INPUT_FILES,
                            Method                = t.Item1,
                            Ngrams                = t.Item2,
                            D_param               = t.Item3,
                            OutputFilenamePattern = Config.OUTPUT_FILE_PATTERN,
                        };
                        try
                        {
						    var sw = Stopwatch.StartNew();
						    Build( bp );
						    sw.Stop();

                            Console.WriteLine( "'" + bp.Method + "; " + bp.Ngrams + "; " + bp.D_param + "' - success, elapsed: " + sw.Elapsed );
                        }
                        catch ( Exception ex )
                        {
                            var fc = Console.ForegroundColor; Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine( "'" + bp.Method + "; " + bp.Ngrams + "; " + bp.D_param + "' - " +  ex.GetType() + ": " + ex.Message );
                            Console.ForegroundColor = fc;
                            wasErrors = true;
                        }
                    }
                    sw_total.Stop();

                    Console.WriteLine( "total elapsed: " + sw_total.Elapsed );
                    #endregion
                }
                #endregion
            }
            catch ( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( Environment.NewLine + ex + Environment.NewLine );
                Console.ResetColor();
                wasErrors = true;
            }

            Console.WriteLine( Environment.NewLine + "[.....finita fusking comedy (push ENTER 4 exit).....]" );
            if ( wasErrors )
            {
                Console.ReadLine();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private struct build_params_t
        {
            public UrlDetectorModel UrlDetectorModel;
            public string           InputFolder;
            public string[]         InputFilenames;
            public MethodEnum       Method;
            public NGramsEnum       Ngrams;
            public D_ParamEnum      D_param;
            public string           OutputFilenamePattern;

            public override string ToString() => ("'" + Method + "; " + Ngrams + "; " + D_param + "'");
        }

		private static void Build( build_params_t bp )
		{
            Console.WriteLine( "start process: '" + bp.ToString() + "'..." );

            #region [.-0-.]
            var _tfidf    = new tfidf( bp.Ngrams, bp.D_param );
            var tokenizer = new ClassifyTokenizer( bp.UrlDetectorModel );
            #endregion

            #region [.-1-.]
            foreach ( var inputFilename in bp.InputFilenames )
            {
                Console.Write( "start process file: '" + new FileInfo( inputFilename ).Name + "'..." );

                var fileName = Path.Combine( bp.InputFolder, inputFilename );
                var text = File.ReadAllText( fileName, Config.INPUT_ENCODING );
                #region comm. xml
                /*
				var sents = (from doc in XDocument.Load( fileName ).Descendants( "document" )
				                    //.Take( 10 )
					            from sent in doc.Elements( "sent" )
				                    //.Take( 10 )
					            select sent.Value
						    )
							.ToArray();
				var text = string.Join( Environment.NewLine, sents );
                sents = null;
                */
                #endregion
                if ( string.IsNullOrWhiteSpace( text ) )
				{
					throw (new InvalidDataException("input text is-null-or-white-space, filename: '" + fileName + '\''));
				}
                
                _tfidf.BeginAddDocument();
                tokenizer.Run( text, (word) =>
                {
                    _tfidf.AddDocumentWord( word );
                });
                _tfidf.EndAddDocument();

                text = null;

                GCCollect();

                #region comm.
                /*
                var words = tokenizer.run( text );
                text = null;
                GC.Collect();

				_tfidf.AddDocument( words );
                words = null;
                GC.Collect();
                */
                #endregion

                Console.WriteLine( "end process file" );
            }
            #endregion
			
			#region [.-2-.]
            Console.Write( "start process TFiDF..." );

            var _tfidf_result = default(tfidf.result);
            switch ( bp.Method )
            {
                case MethodEnum.tfidf:
                    _tfidf_result = _tfidf.Process();
                break;

                case MethodEnum.bm25:
                    _tfidf_result = _tfidf.Process_BM25();
                break;

                case MethodEnum.R_tfidf:
                    _tfidf_result = _tfidf.Process_R();
                break;
            }                
			_tfidf = null;
			GCCollect();

            Console.WriteLine( "end process TFiDF" );
            #endregion

            #region [.-3-.]
            Console.Write( "start write result..." );
            var fi = new FileInfo( bp.OutputFilenamePattern );
            if ( !fi.Directory.Exists ) fi.Directory.Create();           
			var outputFile = Path.Combine( fi.DirectoryName, fi.Name.Substring( 0, fi.Name.Length - fi.Extension.Length ) +
                                           "-(" + bp.Method + "-" + bp.Ngrams + "-" + bp.D_param + ")" + fi.Extension );

            var sb  = new StringBuilder();
            var nfi = new NumberFormatInfo() { NumberDecimalSeparator = "." };
            using ( var sw = new StreamWriter( outputFile, false, Config.OUTPUT_ENCODING ) )
            {
				var header = "#\t'" + string.Join( "'\t'", Config.INPUT_FILES ) + '\'';
				sw.WriteLine( header );				
				
                for ( int i = 0, len = _tfidf_result.TFiDF.Length; i < len; i++ )
                {
					var values = _tfidf_result.TFiDF[ i ];
					//if ( values.Sum() != 0 )
                    if ( !AllValuesAreEquals( values ) )
					{
	                    var w = _tfidf_result.Words[ i ];
	                    sb.Clear().Append( w ).Append( '\t' );
	                    for ( int j = 0, values_len = values.Length; j < values_len; j++ )
	                    {
	                        sb.Append( values[ j ].ToString( nfi ) ).Append( '\t' );
	                    }
	                    sb.Remove( sb.Length - 1, 1 );
	
	                    sw.WriteLine( sb.ToString() );
					}
                }
            }
            Console.WriteLine( "end write result" + Environment.NewLine );
            #endregion
		}

        private static bool AllValuesAreEquals( float[] values )
        {
            var v1 = values[ 0 ];
            for ( int i = 1, len = values.Length; i < len; i++ )
            {
                if ( v1 != values[ i ] )
                {
                    return (false);
                }
            }
            return (true);
        }
        private static void GCCollect()
        {
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            GC.WaitForPendingFinalizers();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class TxtModelSplitterBySize
    {
        public static IList< string > SplitBySize( string modelFileName, string outputFolder, int maxFileSizeInBytes )
        {
            var lines = File.ReadAllLines( modelFileName );
            var fileNumber = 0;
            var outputFileNames = new List< string >();
            for ( var i = 0; i < lines.Length; i++ )
            {
                var outputFileName = Path.Combine( outputFolder, 
                                                   Path.GetFileNameWithoutExtension( modelFileName ) + "--" + 
                                                   (++fileNumber) + Path.GetExtension( modelFileName ) );
                outputFileNames.Add( outputFileName );

                using ( var sw = new StreamWriter( outputFileName ) )
                {
                    for ( ; i < lines.Length; i++ )
                    {
                        var line = lines[ i ];
                        sw.WriteLine( line );
                        if ( maxFileSizeInBytes <= sw.BaseStream.Length )
                        {
                            break;
                        }
                    }
                }
            }
            return (outputFileNames);
        }
    }
}
