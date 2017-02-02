//#define XLAT_CHARTYPE_MAP
//#define XLAT_UPPER_INVARIANT_MAP
//#define XLAT_WHITESPACE_CHARS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using lingvo.core;
using lingvo.urls;

namespace lingvo.tokenizing
{
    /// <summary>
    /// 
    /// </summary>
    public enum NGramsType
    {
        NGram_1,
        NGram_2,
        NGram_3,
        NGram_4,
    }

    /// <summary>
    /// 
    /// </summary>
    unsafe internal class classify_tokenizer : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        private sealed class UnsafeConst
        {
            #region [.static & xlat table's.]
            public static readonly char*   MAX_PTR = (char*) (0xffffffffFFFFFFFF);
            private const string           INCLUDE_INTERPRETE_AS_WHITESPACE = "¥©¤¦§®¶€™<>";
            private static readonly char[] EXCLUDE_INTERPRETE_AS_WHITESPACE = new char[] { '\u0026', /* 0x26   , 38   , '&' */
                                                                                           '\u0027', /* 0x27   , 39   , ''' */
                                                                                           '\u002D', /* 0x2D   , 45   , '-' */
                                                                                           '\u002E', /* 0x2E   , 46   , '.' */
                                                                                           '\u005F', /* 0x5F   , 95   , '_' */
                                                                                           '\u00AD', /* 0xAD   , 173  , '­' */
                                                                                           '\u055A', /* 0x55A  , 1370 , '՚' */
                                                                                           '\u055B', /* 0x55B  , 1371 , '՛' */
                                                                                           '\u055D', /* 0x55D  , 1373 , '՝' */
                                                                                           '\u2012', /* 0x2012 , 8210 , '‒' */
                                                                                           '\u2013', /* 0x2013 , 8211 , '–' */
                                                                                           '\u2014', /* 0x2014 , 8212 , '—' */
                                                                                           '\u2015', /* 0x2015 , 8213 , '―' */
                                                                                           '\u2018', /* 0x2018 , 8216 , '‘' */
                                                                                           '\u2019', /* 0x2019 , 8217 , '’' */
                                                                                           '\u201B', /* 0x201B , 8219 , '‛' */
                                                                                         };
            private static readonly string INCLUDE_DIGIT_WORD_CHARS = ";,:./\\- –〃´°";
            #endregion

            public readonly bool* _INTERPRETE_AS_WHITESPACE;
            public readonly bool* _DIGIT_WORD_CHARS;

            private UnsafeConst()
            {
                //-1-//
                var INTERPRETE_AS_WHITESPACE = new bool[ char.MaxValue - char.MinValue ];
                fixed ( bool* iaw_base = INTERPRETE_AS_WHITESPACE )        
                {
                    for ( var c = char.MinValue; c < char.MaxValue; c++ )
                    {
                        *(iaw_base + c) = /*char.IsWhiteSpace( c ) ||*/ char.IsPunctuation( c );
                    }

                    foreach ( var c in INCLUDE_INTERPRETE_AS_WHITESPACE )
                    {
                        *(iaw_base + c) = true;
                    }

                    foreach ( var c in EXCLUDE_INTERPRETE_AS_WHITESPACE )
                    {
                        *(iaw_base + c) = false;
                    }
                }
                var INTERPRETE_AS_WHITESPACE_GCHandle = GCHandle.Alloc( INTERPRETE_AS_WHITESPACE, GCHandleType.Pinned );
                _INTERPRETE_AS_WHITESPACE = (bool*) INTERPRETE_AS_WHITESPACE_GCHandle.AddrOfPinnedObject().ToPointer();

                //-2-//
                var DIGIT_WORD_CHARS = new bool[ char.MaxValue - char.MinValue ];
                for ( var c = char.MinValue; c < char.MaxValue; c++ )
                {
                    DIGIT_WORD_CHARS[ c ] = char.IsDigit( c );
                }
                foreach ( var c in INCLUDE_DIGIT_WORD_CHARS )
                {
                    DIGIT_WORD_CHARS[ c ] = true;
                }

                var DIGIT_WORD_CHARS_GCHandle = GCHandle.Alloc( DIGIT_WORD_CHARS, GCHandleType.Pinned );
                _DIGIT_WORD_CHARS = (bool*) DIGIT_WORD_CHARS_GCHandle.AddrOfPinnedObject().ToPointer();
            }

            public static readonly UnsafeConst Inst = new UnsafeConst();
        }

        #region [.private field's.]
        private const int DEFAULT_WORDCAPACITY      = 1000;
        private const int DEFAULT_WORDTOUPPERBUFFER = 100;

        private readonly UrlDetector    _UrlDetector;
        private readonly List< string > _Words;
        private readonly StringBuilder  _NgramsSB;
        private readonly CharType*      _CTM;
        private readonly char*          _UIM;
        private readonly bool*          _DWC;
        private readonly bool*          _IAW;
        private char*                   _BASE;
        private char*                   _Ptr;
        private int                     _StartIndex;
        private int                     _Length;
        //private char[]                  _WordToUpperBuffer;
        private int                     _WordToUpperBufferSize;
        private GCHandle                _WordToUpperBufferGCHandle;
        private char*                   _WordToUpperBufferPtrBase;
        private Action< string >        _AddWordtoListAction;
        #endregion

        #region [.ctor().]
        public classify_tokenizer( UrlDetectorModel urlModel ) : this( urlModel, DEFAULT_WORDCAPACITY )
        {
        }
        public classify_tokenizer( UrlDetectorModel urlModel, int wordCapacity )
        {
            var urlConfig = new UrlDetectorConfig()
            {
                Model          = urlModel,
                UrlExtractMode = UrlDetector.UrlExtractModeEnum.Position,
            };
            _UrlDetector = new UrlDetector( urlConfig );
            _Words       = new List< string >( Math.Max( DEFAULT_WORDCAPACITY, wordCapacity ) );
            _NgramsSB    = new StringBuilder();
            _AddWordtoListAction = new Action< string >( addWordtoList );

            _UIM = xlat_Unsafe.Inst._UPPER_INVARIANT_MAP;
            _CTM = xlat_Unsafe.Inst._CHARTYPE_MAP;
            _IAW = UnsafeConst.Inst._INTERPRETE_AS_WHITESPACE;
            _DWC = UnsafeConst.Inst._DIGIT_WORD_CHARS;

            //--//
            ReAllocWordToUpperBuffer( DEFAULT_WORDTOUPPERBUFFER );
        }

        private void ReAllocWordToUpperBuffer( int newBufferSize )
        {
            DisposeNativeResources();

            _WordToUpperBufferSize = newBufferSize;
            var wordToUpperBuffer  = new char[ _WordToUpperBufferSize ];
            _WordToUpperBufferGCHandle = GCHandle.Alloc( wordToUpperBuffer, GCHandleType.Pinned );
            _WordToUpperBufferPtrBase  = (char*) _WordToUpperBufferGCHandle.AddrOfPinnedObject().ToPointer();
        }

        ~classify_tokenizer()
        {
            DisposeNativeResources();
        }
        public void Dispose()
        {
            DisposeNativeResources();

            GC.SuppressFinalize( this );
        }
        private void DisposeNativeResources()
        {
            if ( _WordToUpperBufferPtrBase != null )
            {
                _WordToUpperBufferGCHandle.Free();
                _WordToUpperBufferPtrBase = null;
            }
        }
        #endregion

        public IList< string > run( string text )
        {
            _Words.Clear();
            run( text, _AddWordtoListAction );
            return (_Words);
        }
        private void addWordtoList( string word )
        {
            _Words.Add( word );
        }

        public void run( string text, Action< string > processWordAction )
        {
            _StartIndex = 0;
            _Length     = 0;

            var word = default(string);

            fixed ( char* _base = text )
            {
                _BASE = _base;

                var urls = _UrlDetector.AllocateUrls( _base );
                var urlIndex = 0;
                var startUrlPtr = (urlIndex < urls.Count) ? urls[ urlIndex ].startPtr : UnsafeConst.MAX_PTR;

                #region [.main.]
                for ( _Ptr = _base; *_Ptr != '\0'; _Ptr++ )
                {
                    #region [.skip allocated url's.]
                    if ( startUrlPtr <= _Ptr )
                    {
                        if ( _Length != 0 )
                        {
                            //word
                            if ( !IsSkipedDigit() )
                            {
                                //word
                                if ( (word = CreateWord()) != null )
                                    processWordAction( word );
                            }
                        }

                        _Ptr = startUrlPtr + urls[ urlIndex ].length - 1;
                        urlIndex++;
                        startUrlPtr = (urlIndex < urls.Count) ? urls[ urlIndex ].startPtr : UnsafeConst.MAX_PTR;

                        _StartIndex = (int) (_Ptr - _BASE + 1);
                        _Length     = 0;
                        continue;
                    }
                    #endregion

                    var ct = *(_CTM + *_Ptr); //*(ctm + *(_base + i)); //
                    if ( (ct & CharType.IsWhiteSpace) == CharType.IsWhiteSpace )
                    {
                        if ( _Length != 0 )
                        {
                            //word
                            if ( !IsSkipedDigit() )
                            {
                                if ( (word = CreateWord()) != null )
                                    processWordAction( word );
                            }

                            _StartIndex += _Length;
                            _Length      = 0;
                        }

                        _StartIndex++;
                    }
                    else
                    if ( *(_IAW + *_Ptr) )
                    {
                        if ( _Length != 0 )
                        {
                            //if ( SkipIfItUrl() )
                                //continue;

                            //word
                            if ( (word = CreateWord()) != null )
                                processWordAction( word );

                            _StartIndex += _Length;
                        }

                        if ( IsLetterPrevAndNextChar() )
                        {
                            _Length = 0;
                            _StartIndex++;
                        }
                        else
                        {
                            #region [.fusking punctuation.]
                            _Length = 1;
                            //merge punctuation (with white-space's)
                            _Ptr++;
                            for ( ; *_Ptr != '\0'; _Ptr++ ) 
                            {
                                ct = *(_CTM + *_Ptr);
                                if ( (ct & CharType.IsPunctuation) != CharType.IsPunctuation &&
                                     (ct & CharType.IsWhiteSpace ) != CharType.IsWhiteSpace )
                                {
                                    break;
                                }
                                _Length++;

                                if (*_Ptr == '\0')
                                    break;
                            }
                            if ( *_Ptr == '\0' )
                            {
                                if (_Length == 1)
                                    _Length = 0;
                                break;
                            }
                            _Ptr--;
                            #endregion

                            //skip punctuation
                            #region commented
                            /*
                            if ( !IsSkipedPunctuation() )
                            {
                                //word
                                processTermAction( CreateWord() );
                            }
                            */
                            #endregion

                            _StartIndex += _Length;
                            _Length      = 0;
                        }
                    }
                    else
                    {
                        _Length++;
                    }
                }

                //last word
                if ( _Length != 0 )
                {
                    if ( !IsSkipedDigit() )
                    {
                        //word
                        if ( (word = CreateWord()) != null )
                            processWordAction( word );
                    }
                }
                #endregion
            }            
        }

        private bool IsLetterPrevAndNextChar()
        {
            if ( _Ptr == _BASE )
                return (false);

            var ch = *(_Ptr - 1);
            var ct = *(_CTM + ch);
            if ( (ct & CharType.IsLetter) != CharType.IsLetter )
                return (false);

            ch = *(_Ptr + 1);
            if ( ch == 0 )
                return (false);
            ct = *(_CTM + ch);
            if ( (ct & CharType.IsLetter) != CharType.IsLetter )
                return (false);

            return (true);
        }
        /*private bool SkipIfItUrl()
        {
            var url = _UrlDetector.AllocateSingleUrl( _Ptr );
            if ( url != null )
            {
#if DEBUG
var xxx = new string ( _BASE, url.startIndex, url.length );
#endif
                _Ptr = _BASE + url.startIndex + url.length - 1;
                _StartIndex = (int)(_Ptr - _BASE + 1);
                _Length = 0;
                return (true);
            }
            return (false);
        }
        */
        private void CreateWordAndPutToList()
        {
            var w = CreateWord();
            if ( w != null )
            {
                _Words.Add( w );
            }
        }
        private string CreateWord()
        {
            var len_minus_1 = _Length - 1;

            char* ptr = _BASE + _StartIndex;
            var start = 0;
            for ( ; start <= len_minus_1; start++ )
            {
                var ct = *(_CTM + *(ptr + start));
                if ( (ct & CharType.IsLetter) == CharType.IsLetter ||
                     (ct & CharType.IsDigit ) == CharType.IsDigit )
                    break;
            }

            var end = len_minus_1;
            for ( ; start < end; end-- )
            {
                var ct = *(_CTM + *(ptr + end));
                if ( (ct & CharType.IsLetter) == CharType.IsLetter ||
                     (ct & CharType.IsDigit ) == CharType.IsDigit )
                    break; 
            }

            if ( start != 0 || end != len_minus_1 )
            {
                if ( end <= start )
                {
                    return (null);
                }

#if NOT_USE_UPPER_INVARIANT_CONVERTION
                var w = new string( ptr, start, end - start + 1 );
                return (w);
#else
                var len = end - start + 1;
                if ( _WordToUpperBufferSize < len )
                {
                    ReAllocWordToUpperBuffer( len );
                }
                for ( int i = 0;  i < len; i++ )
                {
                    *(_WordToUpperBufferPtrBase + i) = *(_UIM + *(ptr + start + i));
                }
                var w = new string( _WordToUpperBufferPtrBase, 0, len );
                return (w);
#endif
            }
            else
            {
#if NOT_USE_UPPER_INVARIANT_CONVERTION
                var w = new string( ptr, 0, _Length );
                return (w);
#else
                if ( _WordToUpperBufferSize < _Length )
                {
                    ReAllocWordToUpperBuffer( _Length );
                }
                for ( int i = 0; i < _Length; i++ )
                {
                    *(_WordToUpperBufferPtrBase + i) = *(_UIM + *(ptr + i));
                }
                var w = new string( _WordToUpperBufferPtrBase, 0, _Length );
                return (w);
#endif
            }            
        }
        private bool IsSkipedDigit()
        {
            for ( int i = _StartIndex, len = _StartIndex + _Length; i <= len; i++ )
            {
                if ( !(*(_DWC + *(_BASE + i))) )
                {
                    return (false);
                }
            }
            return (true);
        }

        #region [.ngrams creating.]
        public HashSet< string > ToHashset( string text, NGramsType ngramsType )
        {
            var hs = new HashSet< string >();
            FillHashset( hs, text, ngramsType );
            return (hs);
        }
        public void FillHashset( HashSet< string > hs, string text, NGramsType ngramsType )
        {
            var terms = run( text ); //Tokenizer.ParseText( text );

            hs.Clear();
            //NGramsType.NGram_1:
            foreach ( var term in terms )
            {
                if ( term != null )
                {
                    hs.Add( term );
                }
            }

            var ngrams = default(IEnumerable< string >);
            switch ( ngramsType )
            {
                case NGramsType.NGram_2:
                    ngrams = GetNGrams_2( terms );
                break;
                case NGramsType.NGram_3:
                    ngrams = GetNGrams_2( terms ).Concat( GetNGrams_3( terms ) );
                break;
                case NGramsType.NGram_4:
                    ngrams = GetNGrams_2( terms ).Concat( GetNGrams_3( terms ) ).Concat( GetNGrams_4( terms ) );
                break;
            }

            if ( ngrams != null )
            {
                foreach ( var ngram in ngrams )
                {
                    hs.Add( ngram );
                }
                ngrams = null;
            }
            terms = null;
        }
		
        private IEnumerable< string > GetNGrams_2( IList< string > terms )
        {
            var t1 = default(string);
            var t2 = default(string);
            for ( int i = 0, len = terms.Count - 1; i < len; i++ )
            {
                while (true)
                {
                    t1 = terms[ i ];
                    if ( t1 != null )
                        break;
                    i++;
                    if ( len <= i )
                        yield break;
                }

                while (true)
                {
                    t2 = terms[ i + 1 ];
                    if ( t2 != null )
                        break;
                    i++;
                    if ( len <= i )
                        yield break;
                }

                _NgramsSB.Clear().Append( t1 ).Append( ' ' ).Append( t2 );
                yield return (_NgramsSB.ToString());
            }
        }
        private IEnumerable< string > GetNGrams_3( IList< string > terms )
        {
            var t1 = default(string);
            var t2 = default(string);
            var t3 = default(string);
            for ( int i = 0, len = terms.Count - 2; i < len; i++ )
            {
                while (true)
                {
                    t1 = terms[ i ];
                    if ( t1 != null )
                        break;
                    i++;
                    if ( len <= i )
                        yield break;
                }

                while (true)
                {
                    t2 = terms[ i + 1 ];
                    if ( t2 != null )
                        break;
                    i++;
                    if ( len <= i )
                        yield break;
                }

                while (true)
                {
                    t3 = terms[ i + 2 ];
                    if ( t3 != null )
                        break;
                    i++;
                    if ( len <= i )
                        yield break;
                }

                _NgramsSB.Clear().Append( t1 ).Append( ' ' ).Append( t2 ).Append( ' ' ).Append( t3 );
                yield return (_NgramsSB.ToString());
            }
        }
        private IEnumerable< string > GetNGrams_4( IList< string > terms )
        {
            var t1 = default(string);
            var t2 = default(string);
            var t3 = default(string);
            var t4 = default(string);
            for ( int i = 0, len = terms.Count - 3; i < len; i++ )
            {
                while (true)
                {
                    t1 = terms[ i ];
                    if ( t1 != null )
                        break;
                    i++;
                    if ( len <= i )
                        yield break;
                }

                while (true)
                {
                    t2 = terms[ i + 1 ];
                    if ( t2 != null )
                        break;
                    i++;
                    if ( len <= i )
                        yield break;
                }

                while (true)
                {
                    t3 = terms[ i + 2 ];
                    if ( t3 != null )
                        break;
                    i++;
                    if ( len <= i )
                        yield break;
                }

                while (true)
                {
                    t4 = terms[ i + 3 ];
                    if ( t4 != null )
                        break;
                    i++;
                    if ( len <= i )
                        yield break;
                }

                _NgramsSB.Clear().Append( t1 ).Append( ' ' ).Append( t2 ).Append( ' ' ).Append( t3 ).Append( ' ' ).Append( t4 );
                yield return (_NgramsSB.ToString());
            }
        }

		public void Fill_TF_Dictionary( Dictionary< string, int > tfDictionary, string text, NGramsType ngramsType )
        {
            var terms = run( text );

            tfDictionary.Clear();
            var count = default(int);
            //NGramsType.NGram_1:
            foreach ( var term in terms )
            {
                //if ( term != null )
                //{
				if ( tfDictionary.TryGetValue( term, out count ) )
                {
					tfDictionary[ term ] = count + 1;
				}
				else 
                {
			        tfDictionary.Add( term, 1 );
				}
                //}
            }

            var ngrams = default(IEnumerable< string >);
            switch ( ngramsType )
            {
                case NGramsType.NGram_2:
                    ngrams = GetNGrams_2( terms );
                break;
                case NGramsType.NGram_3:
                    ngrams = GetNGrams_2( terms ).Concat( GetNGrams_3( terms ) );
                break;
                case NGramsType.NGram_4:
                    ngrams = GetNGrams_2( terms ).Concat( GetNGrams_3( terms ) ).Concat( GetNGrams_4( terms ) );
                break;
            }

            if ( ngrams != null )
            {
                foreach ( var ngram in ngrams )
                {
				    if ( tfDictionary.TryGetValue( ngram, out count ) )
                    {
					    tfDictionary[ ngram ] = count + 1;
				    }
				    else 
                    {
			            tfDictionary.Add( ngram, 1 );
				    }
                }
                ngrams = null;
            }
            terms = null;
        }
        #endregion
    }
}
