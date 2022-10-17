using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace lingvo.classify
{
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

        private static void Run_1()
        {
            using var env = ClassifyEnvironment.Create();
            var classifier = env.CreateClassifier();

            var text = "Напомню, что, как правило, поисковые системы работают с так называемым обратным индексом, отличной метафорой которого будет алфавитный указатель в конце книги: все использованные термины приведены в нормальной форме и упорядочены лексикографически — проще говоря, по алфавиту, и после каждого указан номер страницы, на которой этот термин встречается. Разница только в том, что такая координатная информация в поисковиках, как правило, значительно подробнее. Например, корпоративный поиск МойОфис (рабочее название — baalbek), для каждого появления слова в документе хранит, кроме порядкового номера, ещё и его грамматическую форму и привязку к разметке.";
            var classifyInfos = classifier.MakeClassify( text );

            var classes = classes_t.GetClasses( classifyInfos, env.ClassifyEnvironmentConfig );

            classes.Print2Console( text );
        }
        private static void Run_2( string path )
        {
            using var env = ClassifyEnvironment.Create();
            var classifier = env.CreateClassifier();

            var n = 0;
            foreach ( var fn in EnumerateAllFiles( path ) )
            {
                var text = File.ReadAllText( fn );

                var classifyInfos = classifier.MakeClassify( text );
                var classes = classes_t.GetClasses( classifyInfos, env.ClassifyEnvironmentConfig );

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
