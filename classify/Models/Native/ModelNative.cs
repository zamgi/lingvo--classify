using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using lingvo.core;

namespace lingvo.classify
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ModelNative : ModelNativeBase, IModel, IDisposable
    {
        #region [.private field's.]
        private Dictionary< IntPtr, IntPtr > _DictionaryNative;
        private float[] _ModelRow;
        #endregion

        #region [.ctor().]
        public ModelNative( ModelConfig config ) : base( config )
        {
            _DictionaryNative = ModelNativeLoader.LoadDictionaryNative( config );
            //---base.Initialize( _DictionaryNative );
            InitializeNative( _DictionaryNative );
        }
        ~ModelNative()
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
                foreach ( var p in _DictionaryNative )
                {
                    Marshal.FreeHGlobal( p.Key   );
                    Marshal.FreeHGlobal( p.Value );
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
                IntPtr weightClassesPtr;
                if ( _DictionaryNative.TryGetValue( (IntPtr) ngramPtr, out weightClassesPtr ) )
                {
                    var weightClassesBytePtr = ((byte*) weightClassesPtr);
                    var countWeightClasses = *weightClassesBytePtr++;
                    var weightClassesFloatPtr = (float*) weightClassesBytePtr;
                    Debug.Assert( countWeightClasses == _ModelRow.Length, "[countWeightClasses != _ModelRow.Length]" );
                    fixed ( float* modelRowPtr = _ModelRow )
                    {
                        for ( var i = 0; i < countWeightClasses; i++ )
                        {
                            modelRowPtr[ i ] = weightClassesFloatPtr[ i ];
                        }
                    }
                    modelRow = _ModelRow;
                    return (true);
                }

                modelRow = null;
                return (false);
            }
        }
        #endregion

        unsafe private void InitializeNative( Dictionary< IntPtr, IntPtr > modelDictionary )
        {
            if ( modelDictionary == null || modelDictionary.Count == 0 )
            {
                throw (new ArgumentNullException( "modelDictionary" ));
            }

            var weightClassesPtr = modelDictionary.First().Value;
            TotalClassCount = *((byte*) weightClassesPtr);
            VectorLength    = modelDictionary.Count;

            _ModelRow = new float[ TotalClassCount ];

            VectorsSquareLength = new double[ TotalClassCount ];
            var values = _DictionaryNative.Values;
            Parallel.For( 0, TotalClassCount,
                classIndex => VectorsSquareLength[ classIndex ] = VectorsArithmetic.VectorSquareLength( values, classIndex ) 
            );
        }

        /// <summary>
        /// 
        /// </summary>
        unsafe private static class ModelNativeLoader
        {
            /// <summary>
            /// 
            /// </summary>
            private struct ModelRow
            {
                public char* TextPtr;
                public int   TextLength;
                public IList< float > WeightClasses;
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

            public static Dictionary< IntPtr, IntPtr > LoadDictionaryNative( ModelConfig config )
            {
                var modelDictionaryNative = new Dictionary< IntPtr, IntPtr >( Math.Max( config.RowCapacity, 1000 ), 
                                                                              default(IntPtrEqualityComparer) );

                foreach ( var filename in config.Filenames )
                {
                    LoadModelFilenameContentMMF( filename, delegate( ref ModelRow row ) 
                    {
                        var textPtr = AllocHGlobalAndCopy( row.TextPtr, row.TextLength );
                        //!!! -= MUST BE EQUALS IN ALL RECORDS =- !!!!
                        byte countWeightClasses = (byte) row.WeightClasses.Count;
                        var weightClassesPtr = Marshal.AllocHGlobal( sizeof(byte) + countWeightClasses * sizeof(float) );
                        var weightClassesBytePtr = (byte*) weightClassesPtr;
                        *weightClassesBytePtr++ = countWeightClasses;
                        var weightClassesFloatPtr = (float*) weightClassesBytePtr;
                        for ( var i = 0; i < countWeightClasses; i++ )
                        {
                            weightClassesFloatPtr[ i ] = row.WeightClasses[ i ];
                        }
                        modelDictionaryNative.Add( textPtr, weightClassesPtr );
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
