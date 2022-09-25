using System;
using System.Collections.Generic;

namespace lingvo.core
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        public static void ThrowIfNull( this object obj, string paramName )
        {
            if ( obj == null ) throw (new ArgumentNullException( paramName ));
        }
        public static void ThrowIfNullOrWhiteSpace( this string text, string paramName )
        {
            if ( string.IsNullOrWhiteSpace( text ) ) throw (new ArgumentNullException( paramName ));
        }
        public static void ThrowIfNullOrWhiteSpaceAnyElement( this IEnumerable<string> sequence, string paramName )
        {
            if ( sequence == null )
                throw (new ArgumentNullException( paramName ));

            foreach ( var c in sequence )
            {
                if ( string.IsNullOrWhiteSpace( c ) )
                    throw (new ArgumentNullException( paramName + " => some collection element is NULL-or-WhiteSpace" ));
            }
        }

        public static bool IsNullOrWhiteSpace( this string text ) => string.IsNullOrWhiteSpace( text );
        public static bool IsNullOrEmpty( this string text ) => string.IsNullOrEmpty( text );

        public static List< T > ToList< T >( this IEnumerable< T > seq, int capacity )
        {
            var lst = new List< T >( capacity );
            foreach ( var t in seq )
            {
                lst.Add( t );
            }
            return (lst);
        }
        /*public static T[] ToArray< T >( this IEnumerable< T > seq, int size )
        {
            var array = new T[ size ];
            var i = 0;
            foreach ( var t in seq )
            {
                array[ i++ ] = t;
            }
            return (array);
        }*/
    }
}
