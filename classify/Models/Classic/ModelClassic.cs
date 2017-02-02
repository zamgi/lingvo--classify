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
    public sealed class ModelClassic : ModelBase, IModel, IDisposable
    {
        #region [.private field's.]
        private Dictionary< string, float[] > _Dictionary;
        #endregion

        #region [.ctor().]
        public ModelClassic( ModelConfig config ) : base( config )
        {
            _Dictionary = ModelClassicLoader.LoadDictionary( config );
            base.Initialize( _Dictionary );

            #region commented
            /*
            var modelDictionaryNative = ModelLoaderNative.LoadDictionaryNative( config );
            var modelDictionary = ModelLoader.LoadDictionary( config );
             
            Debug.Assert( modelDictionary.Count == modelDictionaryNative.Count );

            using ( var e1 = modelDictionary.GetEnumerator() )
            using ( var e2 = modelDictionaryNative.GetEnumerator() )
            {
                for ( ; e1.MoveNext() && e2.MoveNext(); )
                {
                    Debug.Assert( e1.Current.Key == StringsHelper.ToString( e2.Current.Key ) );
                    Debug.Assert( e1.Current.Value.SequenceEqual( e2.Current.Value ) );
                }
            }
            */
            #endregion
        }

        public override void Dispose()
        {
            if ( _Dictionary != null )
            {
                _Dictionary.Clear();
                _Dictionary = null;
            }
        }
        #endregion

        #region [.IClassifierModel.]
        public override bool TryGetValue( string ngram, out float[] modelRow )
        {
            return (_Dictionary.TryGetValue( ngram, out modelRow ));
        } 
        #endregion

        /// <summary>
        /// 
        /// </summary>
        private static class ModelClassicLoader
        {
            private static readonly char[] SPLIT_CHARS = new[] { '\t' };            

            public static Dictionary< string, float[] > LoadDictionary( ModelConfig config )
            {
                var modelDictionary = new Dictionary< string, float[] >( Math.Max( config.RowCapacity, 1000 ) );
                //var totalClassCount = default(int);

                var a_len = -1;
                foreach ( var filename in config.Filenames )
                {
                    using ( var sr = new StreamReader( filename ) )
                    {
                        var line  = default(string);                        
                        while ( (line = sr.ReadLine()) != null ) 
                        {
                            //skip comment & header
                            line = line.Trim();
                            if ( line.StartsWith( "#" ) )
                            {
                                continue;
                            }

                            var a = line.Split( SPLIT_CHARS, StringSplitOptions.RemoveEmptyEntries );
                            if ( (a.Length < 2) || (a.Length != a_len && a_len != -1) )
                            {
                                Debug.WriteLine( "(a.Length < 2) || (a.Length != a_len && a_len != -1)" );
						        continue;                                
                                //throw (new InvalidDataException());
                            }
                            a_len = a.Length;

                            var ngram  = a[ 0 ].Trim().ToUpperInvariant();
                            var row    = (from f in a.Skip( 1 )
                                          select float.Parse( f.Trim(), NS, NFI )
                                         ).ToArray();

                            modelDictionary.Add( ngram, row );
                        }
                        //totalClassCount = a_len - 1;
                    }
                }
                

                return (modelDictionary);
            }
        }
    }

}
