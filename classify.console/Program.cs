using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

using lingvo.tokenizing;
using static System.Net.Mime.MediaTypeNames;

namespace lingvo.classify
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Config
    {
        static Config()
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

        public const string N2 = "N2";
        public static NumberFormatInfo NFI { get; } = new NumberFormatInfo() { NumberDecimalSeparator = "." };

        public static string[] MODEL_FILENAMES   { get; }
        public static string[] CLASS_INDEX_NAMES { get; }

        public static string     URL_DETECTOR_RESOURCES_XML_FILENAME { get; } = ConfigurationManager.AppSettings[ "URL_DETECTOR_RESOURCES_XML_FILENAME" ];        
        public static NGramsType MODEL_NGRAMS_TYPE       { get; } = (NGramsType) Enum.Parse( typeof(NGramsType), ConfigurationManager.AppSettings[ "MODEL_NGRAMS_TYPE" ], true );
        public static int        MODEL_ROW_CAPACITY      { get; } = int.Parse( ConfigurationManager.AppSettings[ "MODEL_ROW_CAPACITY" ] );
        public static int        CLASS_THRESHOLD_PERCENT { get; } = int.Parse( ConfigurationManager.AppSettings[ "CLASS_THRESHOLD_PERCENT" ] );        

        public static string ClassIndex2Text( int classIndex )
        {
            if ( 0 <= classIndex && classIndex < CLASS_INDEX_NAMES.Length )
            {
                return (CLASS_INDEX_NAMES[ classIndex ]);
            }
            return ("[class-index: " + classIndex + "]");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        private static void Main( string[] args )
        {
            try
            {
                Run_1();
                //Run_2( "C:\\" );
            }
            catch ( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( ex );
                Console.ResetColor();
            }
            Console.WriteLine( Environment.NewLine + "[.....finita fusking comedy.....]" );
            Console.ReadLine();
        }

        /// <summary>
        /// 
        /// </summary>
        private sealed class environment : IDisposable
        {
            private ModelNative _Model;
            public void Dispose() => _Model.Dispose();

            public Classifier Classifier { get; init; }
            public static environment Create( bool print2Console = true )
            {
                var sw = default(Stopwatch);
                if ( print2Console ) 
                {
                    sw = Stopwatch.StartNew();
                    Console.Write( "init classifier..." ); 
                }

                var modelConfig = new ModelConfig()
                {
                    Filenames   = Config.MODEL_FILENAMES,
                    RowCapacity = Config.MODEL_ROW_CAPACITY, 
                    NGramsType  = Config.MODEL_NGRAMS_TYPE 
                };
                var model  = new ModelNative( modelConfig ); //new ModelHalfNative( modelConfig ); //new ModelClassic( modelConfig );
                var config = new ClassifierConfig( Config.URL_DETECTOR_RESOURCES_XML_FILENAME );

                var classifier = new Classifier( config, model );

                if ( print2Console )
                {
                    sw.Stop();
                    Console.WriteLine( $"end, (elapsed: {sw.Elapsed}).\r\n----------------------------------------------------\r\n" );
                }

                return (new environment() { _Model = model, Classifier = classifier });
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private readonly struct classes_t
        {
            public int    class_index  { get; init; }
            public string class_name   { get; init; }
            public double cosine       { get; init; }
            public double percent      { get; init; }

            public override string ToString() => $"class: {class_name}, percent: {percent.ToString( Config.N2, Config.NFI )}";

            public static IList< classes_t > GetClasses( IList< ClassifyInfo > classifyInfos )
            {
                var sum = classifyInfos.Sum( ci => ci.Cosine );
                var classes = (from ci in classifyInfos
                                let percent = (ci.Cosine / sum) * 100
                                where (Config.CLASS_THRESHOLD_PERCENT <= percent)
                                select
                                new classes_t()
                                {
                                    class_index  = ci.ClassIndex,
                                    class_name   = Config.ClassIndex2Text( ci.ClassIndex ),
                                    cosine       = ci.Cosine,
                                    percent      = percent,
                                }
                              ).ToList();
                return (classes);
            }
        }

        private static void Run_1()
        {
            using var env = environment.Create();

            var text = "Напомню, что, как правило, поисковые системы работают с так называемым обратным индексом, отличной метафорой которого будет алфавитный указатель в конце книги: все использованные термины приведены в нормальной форме и упорядочены лексикографически — проще говоря, по алфавиту, и после каждого указан номер страницы, на которой этот термин встречается. Разница только в том, что такая координатная информация в поисковиках, как правило, значительно подробнее. Например, корпоративный поиск МойОфис (рабочее название — baalbek), для каждого появления слова в документе хранит, кроме порядкового номера, ещё и его грамматическую форму и привязку к разметке.";
            var classifyInfos = env.Classifier.MakeClassify( text );

            var classes = classes_t.GetClasses( classifyInfos );

            classes.Print2Console( text );
        }
        private static void Run_2( string path )
        {
            using var env = environment.Create();

            var n = 0;
            foreach ( var fn in EnumerateAllFiles( path ) )
            {
                var text = File.ReadAllText( fn );

                var classifyInfos = env.Classifier.MakeClassify( text );
                var classes = classes_t.GetClasses( classifyInfos );

                Console_Write( $"{++n}.) ", ConsoleColor.DarkGray );
                classes.Print2Console( text );
            }
        }
        private static void Print2Console( this IList< classes_t > classes, string text )
        {
            Console.Write( $"text: " );
            Console_WriteLine( $"'{text.Cut().Norm()}'", ConsoleColor.DarkGray );
            if ( classes.Any() )
                Console.WriteLine( "  " + string.Join( "\r\n  ", classes ) );
            else
                Console_WriteLine( "  [text class is not defined]", ConsoleColor.DarkRed );
            Console.WriteLine();
        }

        private static IEnumerable< string > EnumerateAllFiles( string path, string searchPattern = "*.txt" )
        {
            try
            {
                var seq = Directory.EnumerateDirectories( path ).SafeWalk()
                                   .SelectMany( _path => EnumerateAllFiles( _path ) );
                return (seq.Concat( Directory.EnumerateFiles( path, searchPattern )/*.SafeWalk()*/ ));
            }
            catch ( Exception ex )
            {
                Debug.WriteLine( ex.GetType().Name + ": '" + ex.Message + '\'' );
                return (Enumerable.Empty< string >());
            }
        }

        private static void Console_Write( string msg, ConsoleColor color )
        {
            var fc = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write( msg );
            Console.ForegroundColor = fc;
        }
        private static void Console_WriteLine( string msg, ConsoleColor color )
        {
            var fc = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine( msg );
            Console.ForegroundColor = fc;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        public static string Cut( this string s, int max_len = 150 ) => (max_len < s.Length) ? s.Substring( 0, max_len ) + "..." : s;
        public static string Norm( this string s ) => s.Replace( '\n', ' ' ).Replace( '\r', ' ' ).Replace( '\t', ' ' ).Replace( "  ", " " );
        public static bool IsNullOrEmpty( this string value ) => string.IsNullOrEmpty( value );
        public static IEnumerable< T > SafeWalk< T >( this IEnumerable< T > source )
        {
            using ( var enumerator = source.GetEnumerator() )
            {
                for ( ; ; )
                {
                    try
                    {
                        if ( !enumerator.MoveNext() )
                            break;
                    }
                    catch ( Exception ex )
                    {
                        Debug.WriteLine( ex.GetType().Name + ": '" + ex.Message + '\'' );
                        continue;
                    }

                    yield return (enumerator.Current);
                }
            }
        }
    }
}
