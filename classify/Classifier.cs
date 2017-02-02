using System;
using System.Collections.Generic;
using System.Linq;

using lingvo.core;
using lingvo.tokenizing;

namespace lingvo.classify
{
    /*
    /// <summary>
    /// 
    /// </summary>
    internal sealed class ClassifierModel
    {
        public ClassifierModel( List< string > ngrams, float[][] matrix )
        {
            if ( ngrams.Count == 0 || matrix.Length == 0 )
            {
                throw (new InvalidDataException());    
            }

            NGrams = ngrams.ToArray();
            Matrix = matrix;

            if ( Matrix.Any( m => m.Length != NGrams.Length ) )
            {
                throw (new InvalidDataException());
            }

            TotalClassCount = Matrix.Length;
            VectorLength    = Matrix[ 0 ].Length;
            VectorsSquareLength = new double[ TotalClassCount ];
            for ( var i = 0; i < TotalClassCount; i++ )
            {
                VectorsSquareLength[ i ] = VectorsArithmetic.VectorSquareLength( Matrix[ i ] );
            }
        }

        public string[] NGrams
        {
            get;
            private set;
        }
        public float[][] Matrix
        {
            get;
            private set;
        }

        public int TotalClassCount
        {
            get;
            private set;
        }
        public int VectorLength
        {
            get;
            private set;
        }

        public double[] VectorsSquareLength
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class Classifier : IClassifier
    {
        #region [.const's & static's.]
        private const           int              UNKNOWN_CLASS        = -1;
        private static readonly int[]            UNKNOWN_CLASSES      = new int[ 0 ];
        private static readonly ClassifyInfo[]   UNKNOWN_CLASSIFYINFO = new ClassifyInfo[ 0 ];
        private static readonly char[]           SPLIT_CHARS          = new[] { '\t' };
        private static readonly NumberFormatInfo NFI                  = new NumberFormatInfo() { NumberDecimalSeparator = "." };
        private const           NumberStyles     NS                   = NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign;
        #endregion

        #region [.private field's.]
        private readonly ClassifierModel  _ClassifierModel;
        private readonly ClassifierConfig _Config;
        private readonly ThreadLocal< TLS > _TLS;
        #endregion

        #region [.ctor().]
        public Classifier( ClassifierConfig config )
        {
            config.ThrowIfNull( "config" );
            config.ModelFilename.ThrowIfNullOrWhiteSpace( "modelFilename" );            

            var ngrams = new List< string  >( Math.Max( config.ModelRowCapacity, 1000 ) );
            var matrix = new List< float[] >( Math.Max( config.ModelRowCapacity, 1000 ) );
            var totalClassCount = default(int);

            using ( var sr = new StreamReader( config.ModelFilename ) )
            {
                var line  = default(string);
                var a_len = -1;
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
						continue;
                        //throw (new InvalidDataException());
                    }
                    a_len = a.Length;

                    var ngram  = a[ 0 ].Trim().ToUpperInvariant();
                    var row    = (from f in a.Skip( 1 )
                                  select float.Parse( f.Trim(), NS, NFI )
                                 ).ToArray();

                    ngrams.Add( ngram );
                    matrix.Add( row   );
                }
                totalClassCount = a_len - 1;
            }

            var matrixRowCount = matrix.Count;
            var matrix2 = new float[ totalClassCount ][];
            for ( var i = 0; i < totalClassCount; i++ )
            {
                matrix2[ i ] = new float[ matrixRowCount ];
            }
            for ( var j = 0; j < matrixRowCount; j++ )
            {
                var row = matrix[ j ];
                for ( var i = 0; i < totalClassCount; i++ )
                {
                    matrix2[ i ][ j ] = row[ i ];
                }
            }
            matrix = null;

            _ClassifierModel = new ClassifierModel( ngrams, matrix2 );
            _Config          = config;
            _TLS             = new ThreadLocal< TLS >( CreateTLS );

            ngrams  = null;
            matrix2 = null;
        }
        #endregion

        #region [.public properties.]
        public string     ModelFilename
        {
            get { return (_Config.ModelFilename); }
        }
        public NGramsType NGramsType
        {
            get { return (_Config.NGramsType); }
            set { _Config.NGramsType = value; }
        }
        public int        VectorLength
        {
            get { return (_ClassifierModel.VectorLength); }
        }
        #endregion

        #region [.private classes & method's.]
        /// <summary>
        /// thread local storage data
        /// </summary>
        private class TLS
        {
            public TLS( ClassifierModel classifierModel )
            {
                var totalClassCount = classifierModel.TotalClassCount;
                var vectorLength    = classifierModel.VectorLength;

                Vector = new float[ totalClassCount ][];
                for ( var j = 0; j < totalClassCount; j++ )
                {
                    Vector[ j ] = new float[ vectorLength ];
                }

                ClassInfo = new ClassifyInfo[ totalClassCount ];
                Hashset   = new HashSet< string >();
                Tokenizer = new classify_tokenizer();
            }

            public float[][] Vector
            {
                get;
                private set;
            }
            public ClassifyInfo[] ClassInfo
            {
                get;
                private set;
            }
            public HashSet< string > Hashset
            {
                get;
                private set;
            }
            public classify_tokenizer Tokenizer
            {
                get;
                private set;
            }
        }

        private TLS CreateTLS()
        {
            var tls = new TLS( _ClassifierModel );
            return (tls);
        }
        #endregion

        #region [.IClassifier.]
        public int TotalClassCount
        {
            get { return (_ClassifierModel.TotalClassCount); }
        }

        public ClassifyInfo[] MakeClassify( string text )
        {
            #region [.-local var's-.]
            var tls       = _TLS.Value;
            var hs        = tls.Hashset;
            var tokenizer = tls.Tokenizer;
            #endregion

            #region [.-prepare-.]
            tokenizer.FillHashset( hs, text, _Config.NGramsType );
            if ( hs.Count == 0 )
            {
                return (UNKNOWN_CLASSIFYINFO);
            }
            #endregion

            #region [.-local var's-.]
            var vec = tls.Vector;
            var cis = tls.ClassInfo;

            var totalClassCount = _ClassifierModel.TotalClassCount;
            var ngrams          = _ClassifierModel.NGrams;
            var matrix          = _ClassifierModel.Matrix;
            var vecSquareLength = _ClassifierModel.VectorsSquareLength;
            #endregion

            #region [.-fill vector-.]
            for ( int i = 0, len = ngrams.Length; i < len; i++ )
            {
                if ( hs.Contains( ngrams[ i ] ) )
                {
                    for ( var j = 0; j < totalClassCount; j++ )
                    {
                        vec[ j ][ i ] = matrix[ j ][ i ];
                    }
                }
                else
                {
                    for ( var j = 0; j < totalClassCount; j++ )
                    {
                        vec[ j ][ i ] = 0;
                    }
                }
            }
            #endregion

            #region [.-result-.]
            for ( var j = 0; j < totalClassCount; j++ )
            {
                var cos = VectorsArithmetic.CosBetweenVectors( matrix[ j ], vecSquareLength[ j ], vec[ j ] );
                cis[ j ].ClassIndex = j;
                cis[ j ].Cosine     = cos;
            }

            var classifyInfo = cis.OrderByDescending( ci => ci.Cosine )
                                  .Where( ci => !double.IsNaN( ci.Cosine ) )
                                  .ToArray();

            return (classifyInfo);
            #endregion
        }
        #endregion
    }
    */
    //==================================================//

    /// <summary>
    /// 
    /// </summary>
    unsafe public sealed class Classifier : IClassifier
    {
        #region [.const's & static's.]
        private const int TEXT_TF_DICTIONARY_CAPACITY = 1000;
        private static readonly ClassifyInfo[] UNKNOWN_CLASSIFYINFO = new ClassifyInfo[ 0 ];
        private static Func< ClassifyInfo, double > _GetClassifyInfoCosineFunc   = new Func< ClassifyInfo, double >( GetClassifyInfoCosine );
        private static Func< ClassifyInfo, bool > _IsClassifyInfoCosineValidFunc = new Func< ClassifyInfo, bool >( IsClassifyInfoCosineValid );
        #endregion

        #region [.private field's.]
        private readonly IModel          _Model;
        private readonly double[]                  _ScalarProducts;
        private readonly ClassifyInfo[]            _ClassInfo;
        private readonly Dictionary< string, int > _TextTFDictionary;
        private readonly classify_tokenizer        _Tokenizer;
        #endregion

        #region [.ctor().]
        public Classifier( ClassifierConfig config, IModel model )
        {
            #region [.check config.]
            config.ThrowIfNull( "config" );            
            config.UrlDetectorModel.ThrowIfNull( "config.UrlDetectorConfig" );
            model.ThrowIfNull( "model" );
            #endregion

            _Model            = model;
            _Tokenizer        = new classify_tokenizer( config.UrlDetectorModel );
            _ScalarProducts   = new double[ _Model.TotalClassCount ];            
            _TextTFDictionary = new Dictionary< string, int >( TEXT_TF_DICTIONARY_CAPACITY );
            _ClassInfo        = new ClassifyInfo[ _Model.TotalClassCount ];
            for ( int i = 0, len = _Model.TotalClassCount; i < len; i++ )
            {
                _ClassInfo[ i ].ClassIndex = i;
            }
        }
        #endregion

        #region [.public properties.]
        public int        VectorLength
        {
            get { return (_Model.VectorLength); }
        }
        public IEnumerable< string > ModelFilenames
        {
            get { return (_Model.Filenames); }
        }
        public NGramsType NGramsType
        {
            get { return (_Model.NGramsType); }
            //set { _Model.NGramsType = value; }
        }
        #endregion

        #region [.IClassifier.]
        public int TotalClassCount
        {
            get { return (_Model.TotalClassCount); }
        }

        public ClassifyInfo[] MakeClassify( string text )
        {
            #region [.-prepare-.]
            var model = _Model;

            _Tokenizer.Fill_TF_Dictionary( _TextTFDictionary, text, model.NGramsType );
            if ( _TextTFDictionary.Count == 0 )
            {
                return (UNKNOWN_CLASSIFYINFO);
            }
            #endregion

            #region [.-fill vector-&-result-.]
            fixed ( double* scalarProductsPtrBase  = _ScalarProducts )
            fixed ( double* vecSquareLengthPtrBase = model.VectorsSquareLength )
            {
                var totalClassCount = model.TotalClassCount;

                //zeroize
			    for ( var i = 0; i < totalClassCount; i++ ) 
                {
				    scalarProductsPtrBase[ i ] = 0;
			    }

			    float[] modelRow;
			    double  textVectorLength = 0;
			    // Вычисляем все необходимое для рассчета косинусов между векторами
                foreach ( var p in _TextTFDictionary ) 
                {
                    if ( model.TryGetValue( p.Key, out modelRow ) ) 
                    {
                        fixed ( float* modelRowPtrBase = modelRow )
                        {
					        for ( var i = 0; i < totalClassCount; i++ ) 
                            {
                                scalarProductsPtrBase[ i ] += modelRowPtrBase[ i ] * p.Value;
					        }
                        }
				    }
				    textVectorLength += p.Value * p.Value;
			    }

                fixed ( ClassifyInfo* classInfoPtrBase = _ClassInfo )
                {
                    for ( var i = 0; i < totalClassCount; i++ )
                    {
                        var cosine = scalarProductsPtrBase[ i ] / Math.Sqrt( vecSquareLengthPtrBase[ i ] * textVectorLength );
                        var classInfoPtr = &classInfoPtrBase[ i ];
                        classInfoPtr->Cosine = cosine;
                        //classInfoPtr->ClassIndex = i;
                    }
                }

                var classifyInfo = _ClassInfo.OrderByDescending( _GetClassifyInfoCosineFunc )
                                             .Where( _IsClassifyInfoCosineValidFunc )
                                             .ToArray();
                return (classifyInfo);
            }
            #endregion
        }
        #endregion

        private static double GetClassifyInfoCosine( ClassifyInfo ci )
        {
            return (ci.Cosine);
        }
        private static bool IsClassifyInfoCosineValid( ClassifyInfo ci )
        {
            return (!double.IsNaN( ci.Cosine ) && (ci.Cosine != 0));
        }
    }
}
