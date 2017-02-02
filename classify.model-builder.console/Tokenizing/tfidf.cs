using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace lingvo.core.algorithm
{
    /// <summary>
    /// 
    /// </summary>
    internal class word_t
    {
        public string Value;
        public int    Count;

        public override string ToString()
        {
            return (Value + ":" + Count);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal class word_t_comparer : IComparer< word_t >
    {
        public static readonly word_t_comparer Instance = new word_t_comparer();
        private word_t_comparer() { }

        public int Compare( word_t x, word_t y )
        {
            //return (y.Count - x.Count);
            
            var d = y.Count - x.Count;            
            if ( d != 0 )
                return (d);

            return (string.CompareOrdinal( x.Value, y.Value ));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal enum D_ParamEnum
    {
        d0 = 0,
        d1 = 1,
        d2 = 2
    }

    /// <summary>
    /// 
    /// </summary>
    internal enum NGramsEnum
    {
        ngram_1 = 1,
        ngram_2 = 2,
        ngram_3 = 3,
        ngram_4 = 4,
    }

    /// <summary>
    /// 
    /// </summary>
    internal class tfidf
    {
        /// <summary>
        /// 
        /// </summary>
        public class result
        {
            public string[]  Words { get; internal set; }
            public float[][] TFiDF { get; internal set; }                        
        }
        
        private readonly NGramsEnum               _Ngrams;
        private readonly D_ParamEnum              _D_param;
        private readonly List< int >              _WordsCountByDocList;
        private HashSet< string >                 _WordsByDocsHashset;
        private List< Dictionary< string, int > > _DocWordsList;
        private readonly StringBuilder            _Sb;
        
        public tfidf( NGramsEnum ngrams, D_ParamEnum d_param )
        {
            _Ngrams  = ngrams;
            _D_param = d_param;
            
            _WordsByDocsHashset = new HashSet< string >();
            _DocWordsList       = new List< Dictionary< string, int > >();

            _WordsCountByDocList = new List< int >();
            _Sb                  = new StringBuilder();
        }
        
        public void AddDocument( IList< string > words )
        {
            //-1-
            var dict = FillByNgrams( words );
            
            //-2-
            foreach ( var p in dict )
            {
                _WordsByDocsHashset.Add( p.Key );
            }
            
            _DocWordsList.Add( dict );

            _WordsCountByDocList.Add( words.Count );
        }

        #region [.begin-end by words.]
        public void BeginAddDocumentWords()
        {
            _DocumentNgrams_1 = new Dictionary< string, int >();
        }
        public void AddDocumentWords( Dictionary< string, int > dict )
        {
            //-2-
            foreach ( var p in dict )
            {
                _WordsByDocsHashset.Add( p.Key );
                _DocumentNgrams_1.Add( p.Key, p.Value );
            }        
        }
        public void AddDocumentWords( SortedSet< word_t > words )
        {
            //-2-
            foreach ( var word in words )
            {
                _WordsByDocsHashset.Add( word.Value );

                _DocumentNgrams_1.Add( word.Value, word.Count );
            }      
        }
        public void EndAddDocumentWords( int wordsCount )
        {
            _DocWordsList.Add( _DocumentNgrams_1 );
            _DocumentNgrams_1 = null;

            _WordsCountByDocList.Add( wordsCount );
        }
        #endregion

        #region [.begin-end by single-word.]
        private int _DocumentWordCount;
        private Dictionary< string, int > _DocumentNgrams_1;
        private Dictionary< string, int > _DocumentNgrams_2;
        private Dictionary< string, int > _DocumentNgrams_3;
        private Dictionary< string, int > _DocumentNgrams_4;
        private string _Word_prev1;
        private string _Word_prev2;
        private string _Word_prev3;

        public void BeginAddDocument()
        {
            _DocumentWordCount = 0;
            _DocumentNgrams_1  = new Dictionary< string, int >( /*1000000*/ );
            _Word_prev1 = _Word_prev2 = _Word_prev3 = null;

            switch ( _Ngrams )
            {
                case NGramsEnum.ngram_4:
                    _DocumentNgrams_4 = new Dictionary< string, int >();
                goto case NGramsEnum.ngram_3;

                case NGramsEnum.ngram_3:
                    _DocumentNgrams_3 = new Dictionary< string, int >();
                goto case NGramsEnum.ngram_2;

                case NGramsEnum.ngram_2:
                    _DocumentNgrams_2 = new Dictionary< string, int >();
                break;
            }
        }
        public void AddDocumentWord( string word )
        {
            _DocumentWordCount++;

            _DocumentNgrams_1.AddOrUpdate( word );

            switch ( _Ngrams )
            {
                case NGramsEnum.ngram_4:
                    if ( _Word_prev3 != null )
                    {
                        _DocumentNgrams_4.AddOrUpdate( _Sb.Clear()
                                                          .Append( _Word_prev3 ).Append( ' ' )
                                                          .Append( _Word_prev2 ).Append( ' ' )
                                                          .Append( _Word_prev1 ).Append( ' ' )
                                                          .Append( word )
                                                          .ToString() 
                                                     );                        
                    }
                    _Word_prev3 = _Word_prev2;
                goto case NGramsEnum.ngram_3;

                case NGramsEnum.ngram_3:
                    if ( _Word_prev2 != null )
                    {
                        _DocumentNgrams_3.AddOrUpdate( _Sb.Clear()
                                                          .Append( _Word_prev2 ).Append( ' ' )
                                                          .Append( _Word_prev1 ).Append( ' ' )
                                                          .Append( word )
                                                          .ToString() 
                                                     );                        
                    }
                    _Word_prev2 = _Word_prev1;
                goto case NGramsEnum.ngram_2;

                case NGramsEnum.ngram_2:
                    if ( _Word_prev1 != null )
                    {
                        _DocumentNgrams_2.AddOrUpdate( _Sb.Clear()
                                                          .Append( _Word_prev1 ).Append( ' ' )
                                                          .Append( word )
                                                          .ToString() 
                                                     );                        
                    }
                    _Word_prev1 = word;
                break;
            }
        }
        public void EndAddDocument()
        {
            //-1-
            CutDictionaryIfNeed( _DocumentNgrams_1, NGramsEnum.ngram_1 );

            switch ( _Ngrams )
            {
                case NGramsEnum.ngram_4:
                    CutDictionaryIfNeed( _DocumentNgrams_4, NGramsEnum.ngram_4 );
                    _DocumentNgrams_1.AppendDictionary( _DocumentNgrams_4 );
                    _DocumentNgrams_4 = null;
                goto case NGramsEnum.ngram_3;

                case NGramsEnum.ngram_3:
                    CutDictionaryIfNeed( _DocumentNgrams_3, NGramsEnum.ngram_3 );
                    _DocumentNgrams_1.AppendDictionary( _DocumentNgrams_3 );
                    _DocumentNgrams_3 = null;
                goto case NGramsEnum.ngram_2;

                case NGramsEnum.ngram_2:
                    CutDictionaryIfNeed( _DocumentNgrams_2, NGramsEnum.ngram_2 );
                    _DocumentNgrams_1.AppendDictionary( _DocumentNgrams_2 );
                    _DocumentNgrams_2 = null;
                break;
            }

            //-2-
            foreach ( var p in _DocumentNgrams_1 )
            {
                _WordsByDocsHashset.Add( p.Key );
            }

            _DocWordsList.Add( _DocumentNgrams_1 );

            _WordsCountByDocList.Add( _DocumentWordCount );

            //-3-
            _DocumentWordCount = 0;
            _DocumentNgrams_1  = null;
        }

        public bool CurrentDocumentHasWords
        {
            get { return (_DocumentWordCount != 0); }
        }
        #endregion

        unsafe public result Process()
        {
            var docCount  = _DocWordsList.Count;
            var wordCount = _WordsByDocsHashset.Count;
            
            var tf_matrix = Create2DemensionMatrix( wordCount, docCount );
            var tfidf_words = new string[ wordCount ];
            var idf_matrix  = new float [ wordCount ];            
            var count       = default(int);
            int wordNumber  = 0;            
            foreach ( var word in _WordsByDocsHashset )
            {
                int countWordInDoc = 0;
                fixed ( float* tf_matrix__ptr = tf_matrix[ wordNumber ] )
                {                    
                    for ( var i = 0; i < docCount; i++ )
                    {
                        var dict = _DocWordsList[ i ];
                        if ( dict.TryGetValue( word, out count ) )
                        {
                            tf_matrix__ptr[ i ] = (1.0f * count) / dict.Count;

                            countWordInDoc++;
                        }
                        else
                        {
                            tf_matrix__ptr[ i ] = 0;
                        }
                    }
                }

                #region [.commented.]
                /*
                int countWordInDoc = 0;
                for ( var i = 0; i < docCount; i++ )
                {
                    var dict = _DocWordsList[ i ];
                    if ( dict.TryGetValue( word, out count ) )
                    {
                        tf_matrix[ wordNumber ][ i ] = (1.0f * count) / dict.Count;

                        countWordInDoc++;
                    }
                    else
                    {
                        tf_matrix[ wordNumber ][ i ] = 0;
                    }
                }
                */                
                #endregion
                
                idf_matrix [ wordNumber ] = (float) Math.Log( (1.0f * countWordInDoc) / docCount );
                tfidf_words[ wordNumber ] = word;

                wordNumber++;
            }

            _WordsByDocsHashset.Clear(); _WordsByDocsHashset = null;
            _DocWordsList      .Clear(); _DocWordsList       = null;
            GC.Collect();

            //check
            if ( idf_matrix.Length != tf_matrix.Length ) 
            {
                throw (new InvalidDataException( "Something fusking strange: size of vector iDF and matrix TF is not equal!" ));
            }

            //calculate TFiDF-matrix
            var tfidf_matrix = Create2DemensionMatrix( wordCount, docCount );
            for ( var i = 0; i < wordCount; i++ ) 
            {
                var idf = idf_matrix[ i ];

                fixed ( float* tfidf_matrix_row__ptr = tfidf_matrix[ i ] )
                fixed ( float* tf_matrix_row__ptr    = tf_matrix   [ i ] )
                {
                    for ( var j = 0; j < docCount; j++ )
                    {
                        tfidf_matrix_row__ptr[ j ] = -1 * tf_matrix_row__ptr[ j ] * idf;
                    }
                }

                #region [.commented.]
                /*
                var tfidf_matrix_row = tfidf_matrix[ i ];
                var tf_matrix_row    = tf_matrix   [ i ];
                var idf              = idf_matrix  [ i ];

                for ( var j = 0; j < docCount; j++ ) 
                {
                    tfidf_matrix_row[ j ] = -1 * tf_matrix_row[ j ] * idf;
                }
                */                
                #endregion
            }

            tf_matrix  = null;
            idf_matrix = null;
            GC.Collect();

            //result
            var result = new result() 
            { 
                Words = tfidf_words, 
                TFiDF = tfidf_matrix 
            };
            return (result);
        }
        public Dictionary< string, float > ProcessOneDimension( int realDocCount )
        {
            var docCount  = _DocWordsList.Count;
            var wordCount = _WordsByDocsHashset.Count;
            
            var tf_matrix = Create2DemensionMatrix( wordCount, docCount );
            var tfidf_words = new string[ wordCount ];
            var idf_matrix  = new float [ wordCount ];            
            var count       = default(int);
            int wordNumber  = 0;            
            foreach ( var w in _WordsByDocsHashset )
            {
                int countWordInDoc = 0;
                for ( var i = 0; i < docCount; i++ )
                {
                    var dict = _DocWordsList[ i ];
                    if ( dict.TryGetValue( w, out count ) )
                    {
                        tf_matrix[ wordNumber ][ i ] = (1.0f * count) / dict.Count;

                        countWordInDoc++;
                    }
                    else
                    {
                        tf_matrix[ wordNumber ][ i ] = 0;
                    }
                }
                
                idf_matrix [ wordNumber ] = (float) Math.Log( (1.0f * countWordInDoc) / realDocCount/*docCount*/ );
                tfidf_words[ wordNumber ] = w;

                wordNumber++;
            }

            _WordsByDocsHashset.Clear(); _WordsByDocsHashset = null;
            _DocWordsList      .Clear(); _DocWordsList       = null;
            GC.Collect();

            //check
            if ( idf_matrix.Length != tf_matrix.Length ) 
            {
                throw (new InvalidDataException( "Something fusking strange: size of vector iDF and matrix TF is not equal!" ));
            }

            //calculate TFiDF-matrix
            var tfidf_matrix = Create2DemensionMatrix( wordCount, docCount );
            for ( var i = 0; i < wordCount; i++ ) 
            {
                var tfidf_matrix_row = tfidf_matrix[ i ];
                var tf_matrix_row    = tf_matrix   [ i ];
                var idf              = idf_matrix  [ i ];

                for ( var j = 0; j < docCount; j++ ) 
                {
                    tfidf_matrix_row[ j ] = -1 * tf_matrix_row[ j ] * idf;
                }
            }

            tf_matrix  = null;
            idf_matrix = null;
            GC.Collect();

            //result
            var resultDict = new Dictionary< string, float >( tfidf_words.Length );
            for ( int i = 0, len = tfidf_words.Length; i < len; i++ )
            {
                resultDict.Add( tfidf_words[ i ], tfidf_matrix[ i ][ 0 ] );
            }
            tfidf_words  = null;
            tfidf_matrix = null;
            GC.Collect();

            return (resultDict);
        }
        public result Process_BM25()
        {
            /*
            Создать функцию bool tfidf_TFiDF_BM25_cpp, аналогичную bool tfidf_TFiDF_cpp за исключением расчета самого веса 
                TFiDF = TF * iDF
                TF  = 3 * tf /[2 * (0.25 + 0.75 * (dl/dl_avg)) + tf]
                iDF = log [(N – df + 0.5)/(df + 0.5)]
                где 
                tf     – частота терма в документе,
                dl     – длинна документа в словах,
                dl_avg – средняя арифметическая длинна документа в коллекции,
                df     – кол-во документов, содержащих данный терм,
                N      – кол-во документов в коллекции.
            */
            var dlAvg = _WordsCountByDocList.Average();

            var docCount  = _DocWordsList.Count;
            var wordCount = _WordsByDocsHashset.Count;
            
            var tf_matrix = Create2DemensionMatrix( wordCount, docCount );
            var tfidf_words = new string[ wordCount ];
            var idf_matrix         = new float [ wordCount ];            
            var count       = default(int);
            int wordNumber  = 0;            
            foreach ( var w in _WordsByDocsHashset )
            {
                int countWordInDoc = 0;
                for ( var i = 0; i < docCount; i++ )
                {
                    var dict = _DocWordsList[ i ];
                    if ( dict.TryGetValue( w, out count ) )
                    {
                        var dl = _WordsCountByDocList[ i ];
                        tf_matrix[ wordNumber ][ i ] = (float) ((3.0f * count) / (2.0f * (0.25f + 0.75f * (dl / dlAvg)) + count));

                        countWordInDoc++;
                    }
                    else
                    {
                        tf_matrix[ wordNumber ][ i ] = 0;
                    }
                }
                
                idf_matrix [ wordNumber ] = (float) Math.Log( (docCount - countWordInDoc + 0.5f) / (countWordInDoc + 0.5f) );
                tfidf_words[ wordNumber ] = w;

                wordNumber++;
            }

            _WordsByDocsHashset.Clear(); _WordsByDocsHashset = null;
            _DocWordsList      .Clear(); _DocWordsList       = null;
            GC.Collect();

            //check
            if ( idf_matrix.Length != tf_matrix.Length ) 
            {
                throw (new InvalidDataException( "Something fusking strange: size of vector iDF and matrix TF is not equal!" ));
            }

            //calculate TFiDF-matrix
            var tfidf_matrix = Create2DemensionMatrix( wordCount, docCount );
            for ( var i = 0; i < wordCount; i++ ) 
            {
                var tfidf_matrix_row = tfidf_matrix[ i ];
                var tf_matrix_row    = tf_matrix   [ i ];
                var idf              = idf_matrix  [ i ];

                for ( var j = 0; j < docCount; j++ ) 
                {
                    tfidf_matrix_row[ j ] = tf_matrix_row[ j ] * idf;
                }
            }

            tf_matrix  = null;
            idf_matrix = null;
            GC.Collect();
            
            //result
            return (new result() { Words = tfidf_words, TFiDF = tfidf_matrix });
        }
        public result Process_R()
        {
            /*
            Создать функцию bool tfidf_RTFiDF_cpp, аналогичную bool tfidf_TFiDF_cpp за исключением расчета самого веса 
                RTFiDF = iDF – TF = -log( df/N ) + log( 1 - exp(-cf/N) ),
                iDF = -log( df/N )
                TF  = -log( 1 - exp(-cf/N) )
                  где 
                cf – суммарная частота слова в коллекции.
                df – кол-во документов, содержащих данный терм,
                N  – кол-во документов в коллекции.
            */

            var docCount  = _DocWordsList.Count;
            var wordCount = _WordsByDocsHashset.Count;
            
            var tf_matrix = Create2DemensionMatrix( wordCount, docCount );
            var tfidf_words = new string[ wordCount ];
            var idf_matrix         = new float [ wordCount ];            
            var count       = default(int);
            int wordNumber  = 0;            
            foreach ( var w in _WordsByDocsHashset )
            {
                var wordCountInAllDocs = 0;
                for ( var i = 0; i < docCount; i++ )
                {
                    var dict = _DocWordsList[ i ];
                    if ( dict.TryGetValue( w, out count ) )
                    {
                        wordCountInAllDocs += count;
                    }
                }

                int countWordInDoc = 0;
                for ( var i = 0; i < docCount; i++ )
                {
                    var dict = _DocWordsList[ i ];
                    if ( dict.ContainsKey( w ) )
                    {
                        tf_matrix[ wordNumber ][ i ] = (float) -Math.Log( 1.0f - Math.Exp( (-wordCountInAllDocs * 1.0f) / docCount ) );

                        countWordInDoc++;
                    }
                    else
                    {
                        tf_matrix[ wordNumber ][ i ] = 0;
                    }
                }
                
                idf_matrix [ wordNumber ] = (float) -Math.Log( (1.0f * countWordInDoc) / docCount );
                tfidf_words[ wordNumber ] = w;

                wordNumber++;
            }

            _WordsByDocsHashset.Clear(); _WordsByDocsHashset = null;
            _DocWordsList      .Clear(); _DocWordsList       = null;
            GC.Collect();

            //check
            if ( idf_matrix.Length != tf_matrix.Length ) 
            {
                throw (new InvalidDataException( "Something fusking strange: size of vector iDF and matrix TF is not equal!" ));
            }

            //calculate TFiDF-matrix
            var tfidf_matrix = Create2DemensionMatrix( wordCount, docCount );
            for ( var i = 0; i < wordCount; i++ ) 
            {
                var tfidf_matrix_row = tfidf_matrix[ i ];
                var tf_matrix_row    = tf_matrix   [ i ];
                var idf              = idf_matrix  [ i ];

                for ( var j = 0; j < docCount; j++ ) 
                {
                    tfidf_matrix_row[ j ] = idf - tf_matrix_row[ j ];
                }
            }

            tf_matrix  = null;
            idf_matrix = null;
            GC.Collect();

            //result
            return (new result() { Words = tfidf_words, TFiDF = tfidf_matrix });
        }
            
        private Dictionary< string, int > FillByNgrams( IList< string > words )
        {
            var dict = new Dictionary< string, int >();

            //-NGramsEnum.ngram_1-
            dict.Append( words );
            CutDictionaryIfNeed( dict, NGramsEnum.ngram_1 );

            switch ( _Ngrams )
            {
                case NGramsEnum.ngram_4:
                #region
                {
                    var ngrams_dict = CreateNGramsDictionary( words, NGramsEnum.ngram_4 );
                    dict.AppendDictionary( ngrams_dict );
                }
                #endregion
                goto case NGramsEnum.ngram_3;

                case NGramsEnum.ngram_3:
                #region
                {
                    var ngrams_dict = CreateNGramsDictionary( words, NGramsEnum.ngram_3 );
                    dict.AppendDictionary( ngrams_dict );
                }
                #endregion
                goto case NGramsEnum.ngram_2;

                case NGramsEnum.ngram_2:
                #region
                {
                    var ngrams_dict = CreateNGramsDictionary( words, NGramsEnum.ngram_2 );
                    dict.AppendDictionary( ngrams_dict );
                }
                #endregion
                break;
            }

            return (dict);
        }
        private Dictionary< string, int > CreateNGramsDictionary( IList< string > words , NGramsEnum dictType )
        {
            switch ( dictType )
            {
                case NGramsEnum.ngram_2:
                #region
                {
                    var ngramDict = new Dictionary< string, int >();
                    for ( int p = 0, len = words.Count - 1; p < len; p++ ) 
                    {
                        var next = words[ p + 1 ];
                        var curr = words[ p     ];

                        _Sb.Clear().Append( curr ).Append( ' ' ).Append( next );

                        ngramDict.AddOrUpdate( _Sb.ToString() );
                    }

                    CutDictionaryIfNeed( ngramDict, dictType );

                    return (ngramDict);
                }
                #endregion

                case NGramsEnum.ngram_3:
                #region
                {
                    var ngramDict = new Dictionary< string, int >();
                    for ( int p = 0, len = words.Count - 2; p < len; p++ )
                    {
                        var curr   = words[ p     ];
                        var next1  = words[ p + 1 ];
                        var next2  = words[ p + 2 ];

                        _Sb.Clear().Append( curr ).Append( ' ' ).Append( next1 ).Append( ' ' ).Append( next2 );

                        ngramDict.AddOrUpdate( _Sb.ToString() );
                    }

                    CutDictionaryIfNeed( ngramDict, dictType );

                    return (ngramDict);
                }
                #endregion

                case NGramsEnum.ngram_4:
                #region
                {
                    var ngramDict = new Dictionary< string, int >();
                    for ( int p = 0, len = words.Count - 3; p < len; p++ )
                    {
                        var curr  = words[ p     ];
                        var next1 = words[ p + 1 ];
                        var next2 = words[ p + 2 ];
                        var next3 = words[ p + 3 ];

                        _Sb.Clear().Append( curr ).Append( ' ' ).Append( next1 ).Append( ' ' ).Append( next2 ).Append( ' ' ).Append( next3 );

                        ngramDict.AddOrUpdate( _Sb.ToString() );
                    }

                    CutDictionaryIfNeed( ngramDict, dictType );

                    return (ngramDict);
                }
                #endregion

                default: //case NGramsEnum.ngram_1:
                    return (null);
            }
        }
        public void CutDictionaryIfNeed( Dictionary< string, int > dict, NGramsEnum dictType )
        {
            var percent = GetCutPercent( dictType, _D_param );

            if ( percent.HasValue )
            {
                var ss = new SortedSet< word_t >( word_t_comparer.Instance );
                var sum = 0;
                foreach ( var p in dict )
                {
                    sum += p.Value;
                    var word = new word_t() { Value = p.Key, Count = p.Value };
                    ss.Add( word );
                }

                var threshold = sum * percent.Value / 100.0f;
                var threshold_current = 0;

                dict.Clear();
                foreach ( var word in ss )
                {
                    threshold_current += word.Count;
                    if (threshold < threshold_current)
                        break;

                    dict.Add( word.Value, word.Count );
                }
                ss = null;
            }
        }
        public SortedSet< word_t > CreateSortedSetAndCutIfNeed( IEnumerable< word_t > words, NGramsEnum dictType, int sum )
        {
            var ss = new SortedSet< word_t >( word_t_comparer.Instance );

            var percent = GetCutPercent( dictType, _D_param );

            if ( percent.HasValue )
            {
                var threshold = sum * percent.Value / 100.0f;
                var threshold_current = 0;

                foreach ( var word in words )
                {
                    threshold_current += word.Count;
                    if (threshold < threshold_current)
                        break;

                    ss.Add( word );
                }
            }
            else
            {
                foreach ( var word in words )
                {
                    ss.Add( word );
                }                    
            }

            return (ss);
        }

        private static float? GetCutPercent( NGramsEnum ngrams, D_ParamEnum d_param )
        {
            switch( d_param ) 
            {
                //case TDiDF_d_enum.d0: return (null);
                case D_ParamEnum.d1:
                {
                    switch ( ngrams ) {
                        case NGramsEnum.ngram_1: return (100 - 5);  
                        case NGramsEnum.ngram_2: return (100 - 50); 
                        case NGramsEnum.ngram_3: return (100 - 85);
                        case NGramsEnum.ngram_4: return (100 - 95); 
                        //default: return (null);
                    }
                }
                break;
                case D_ParamEnum.d2:
                {
                    switch ( ngrams ) {
                        case NGramsEnum.ngram_1: return (100 - 50); 
                        case NGramsEnum.ngram_2: return (100 - 85); 
                        case NGramsEnum.ngram_3: return (100 - 95); 
                        case NGramsEnum.ngram_4: return (100 - 98); 
                        //default: return (null);
                    }
                }
                break;
                //default: return (null);
            }
            return (null);
        }
        private static float[][] Create2DemensionMatrix( int rowsCount, int columnsCount )
        {
            var matrix = new float[ rowsCount ][];
            for ( var i = 0; i < rowsCount; i++ )
            {
                matrix[ i ] = new float[ columnsCount ];
            }
            return (matrix);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class tfidfExt
    {
        public static void AddOrUpdate( this Dictionary< string, int > dict, string key )
        {
            int count;
            if ( dict.TryGetValue( key, out count ) )
            {
                dict[ key ] = count + 1;
            }
            else
            {
                dict.Add( key, 1 );
            }
        }
        public static void AddOrUpdate( this Dictionary< string, int > dict, string key, int countValue )
        {
            int count;
            if ( dict.TryGetValue( key, out count ) )
            {
                dict[ key ] = count + countValue;
            }
            else
            {
                dict.Add( key, countValue );
            }
        }
        public static void Append( this Dictionary< string, int > dict, IList< string > words )
        {
            foreach ( var word in words )
            {
                dict.AddOrUpdate( word );
            }
        }
        public static void AppendDictionary( this Dictionary< string, int > masterDict, Dictionary< string, int > slaveDict )
        {
            foreach ( var p in slaveDict )
            {
                masterDict.AddOrUpdate( p.Key, p.Value );
            }
        }
    }
}
