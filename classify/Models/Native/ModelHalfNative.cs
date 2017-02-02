using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using lingvo.core;

namespace lingvo.classify
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ModelHalfNative : ModelNativeBase, IModel, IDisposable
    {
        #region [.private field's.]
        private Dictionary< IntPtr, float[] > _DictionaryNative; 
        #endregion

        #region [.ctor().]
        public ModelHalfNative( ModelConfig config ) : base( config )
        {
            _DictionaryNative = ModelHalfNativeLoader.LoadDictionaryNative( config );
            base.Initialize( _DictionaryNative );
        }
        ~ModelHalfNative()
        {
            DisposeNativeResources();
        }

        public override void Dispose()
        {
            DisposeNativeResources();

            GC.SuppressFinalize( this );
        }
        private void DisposeNativeResources()
        {
            if ( _DictionaryNative != null )
            {
                foreach ( var ptr in _DictionaryNative.Keys )
                {
                    Marshal.FreeHGlobal( ptr );
                }
                _DictionaryNative = null;
            }
        } 
        #endregion

        #region [.IClassifierModel.]
        unsafe public override bool TryGetValue( string ngram, out float[] modelRow )
        {
            fixed ( char* ngramPtr = ngram )
            {
                return (_DictionaryNative.TryGetValue( (IntPtr) ngramPtr, out modelRow ));
            }
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        unsafe private static class ModelHalfNativeLoader
        {
            /// <summary>
            /// 
            /// </summary>
            private struct ModelRow
            {
                public char* TextPtr;
                public int   TextLength;
                public ICollection< float > WeightClasses;
#if DEBUG
                public override string ToString()
                {
                    return (StringsHelper.ToString( TextPtr ) + ", {" + string.Join( "; ", WeightClasses ) + '}');
                }  
#endif
            }

            /// <summary>
            /// 
            /// </summary>
            private delegate void LoadModelFilenameContentMMFCallback( ref ModelRow row );

            public static Dictionary< IntPtr, float[] > LoadDictionaryNative( ModelConfig config )
            {
                var modelDictionaryNative = new Dictionary< IntPtr, float[] >( Math.Max( config.RowCapacity, 1000 ), 
                                                                               default(IntPtrEqualityComparer) );

                foreach ( var filename in config.Filenames )
                {
                    LoadModelFilenameContentMMF( filename, delegate( ref ModelRow row )  
                    {
                        var textPtr = AllocHGlobalAndCopy( row.TextPtr, row.TextLength );
                        var weightClasses = row.WeightClasses.ToArray();
                        modelDictionaryNative.Add( textPtr, weightClasses );
                    } );
                }

                return (modelDictionaryNative);
            }

            private static void LoadModelFilenameContentMMF( string modelFilename, LoadModelFilenameContentMMFCallback callbackAction )
            {
                using ( var emmf = EnumeratorMMF.Create( modelFilename ) )
                {
                    var lineCount = 0;
                    var text      = default(string);
                    var weight    = default(float);
                    var row       = new ModelRow();
                    var weightClasses = new List< float >( 100 );
                    var weightClassesLen = -1;

                    #region [.move to first line.]
                    if ( !emmf.MoveNext() )
                    {
                        return;
                    } 
                    #endregion

                    #region [.skip beginning comments.]
                    for ( ; ; )
                    {
                        lineCount++;

                        #region [.check on comment.]
                        if ( *emmf.Current.Start != '#' )
                        {
                            break;
                        }
                        #endregion

                        #region [.move to next line.]
                        if ( !emmf.MoveNext() )
                        {
                            return;
                        }
                        #endregion
                    } 
                    #endregion

                    #region [.read all lines.]
                    for ( ; ; )
                    {
                        lineCount++;

                        var ns = emmf.Current;

                        #region [.skip comment.]
                        if ( *ns.Start == '#' )
                        {
                            #region [.move to next line.]
                            if ( !emmf.MoveNext() )
                            {
                                break;
                            }
                            #endregion
                            continue;
                        }
                        #endregion

                        #region [.first-value in string.]
                        int startIndex_1  = 0;
                        int finishIndex_2 = ns.Length - 1;

                        //search '\t'
                        int startIndex_2  = 0;
                        int finishIndex_1 = 0;
                        for ( ; ; )
                        {
                            if ( ns.Start[ finishIndex_1 ] == TABULATION )
                            {
                                startIndex_2 = finishIndex_1 + 1;
                                finishIndex_1--;
                                break;
                            }
                            //not found '\t'
                            if ( finishIndex_2 <= ++finishIndex_1 )
                            {
                                throw (new InvalidDataException( string.Format( INVALIDDATAEXCEPTION_FORMAT_MESSAGE, modelFilename, lineCount, ns.ToString() ) ));
                            }
                        }
                        //skip ends white-spaces
                        for ( ; ; )
                        {
                            if ( ((_CTM[ ns.Start[ finishIndex_1 ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                                 (--finishIndex_1 <= startIndex_1)
                               )
                            {
                                break;
                            }
                        }

                        if ( finishIndex_1 < startIndex_1 )
                        {
                            throw (new InvalidDataException( string.Format( INVALIDDATAEXCEPTION_FORMAT_MESSAGE, modelFilename, lineCount, ns.ToString() ) ));
                        }
                        #endregion

                        #region [.second-value in string.]
                        //tokinize weight-of-classes
                        int len;
                        for ( ; startIndex_2 <= finishIndex_2; startIndex_2++ )
                        {
                            //skip starts white-spaces
                            if ( ((_CTM[ ns.Start[ startIndex_2 ] ] & CharType.IsWhiteSpace) == CharType.IsWhiteSpace) )
                            {
                                continue;
                            }

                            //search end of weight-value
                            for ( var si = startIndex_2; ; )
                            {
                                if ( ((_CTM[ ns.Start[ startIndex_2 ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) )
                                {
                                    if ( finishIndex_2 == startIndex_2 )
                                    {
                                        startIndex_2++;
                                    }
                                    else
                                    {
                                        startIndex_2++;
                                        continue;
                                    }
                                }

                                //try parse weight-value
                                len = (startIndex_2 - si);// +1;
                                text = StringsHelper.ToString( ns.Start + si, len );

                                if ( !float.TryParse( text, NS, NFI, out weight ) ) //if ( !Number.TryParseSingle( text, NS, NFI, out weight ) )
                                {
                                    throw (new InvalidDataException( string.Format( INVALIDDATAEXCEPTION_FORMAT_MESSAGE, modelFilename, lineCount, ns.ToString() ) ));
                                }
                                weightClasses.Add( weight );
                                si = startIndex_2 + 1;

                                break;
                            }
                        }
                        #endregion

                        #region [.fill 'ModelRow' & calling 'callbackAction()'.]
                        if ( weightClassesLen == -1 )
                        {
                            if ( weightClasses.Count == 0 )
                            {
                                throw (new InvalidDataException( string.Format( INVALIDDATAEXCEPTION_FORMAT_MESSAGE, modelFilename, lineCount, ns.ToString() ) + " => classes weightes not found" ));
                            }
                            weightClassesLen = weightClasses.Count;
                        }
                        else if ( weightClassesLen != weightClasses.Count )
                        {
                            Debug.WriteLine( string.Format( INVALIDDATAEXCEPTION_FORMAT_MESSAGE, modelFilename, lineCount, ns.ToString() ) + " => different count of classes weightes" );
                            continue;
                            //throw (new InvalidDataException( string.Format( INVALIDDATAEXCEPTION_FORMAT_MESSAGE, modelFilename, lineCount, ns.ToString() ) + " => different count of classes weightes" ));
                        }

                        row.TextLength = (finishIndex_1 - startIndex_1) + 1;
                        var textPtr = ns.Start + startIndex_1;
                        textPtr[ row.TextLength ] = '\0';
                        StringsHelper.ToUpperInvariantInPlace( textPtr, row.TextLength );

                        row.TextPtr       = textPtr;
                        row.WeightClasses = weightClasses;                        

                        callbackAction( ref row );

                        //clear weight-classes temp-buffer
                        weightClasses.Clear();
                        #endregion

                        #region [.move to next line.]
                        if ( !emmf.MoveNext() )
                        {
                            break;
                        }
                        #endregion
                    }
                    #endregion
                }
            }            
        }
    }
}
