using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using lingvo.core;
using lingvo.tokenizing;

namespace lingvo.classify
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class ModelBase : IModel, IDisposable
    {
        protected const NumberStyles NS = NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign;
        protected static readonly NumberFormatInfo NFI = new NumberFormatInfo() { NumberDecimalSeparator = "." };

        protected ModelBase( ModelConfig config )
        {
            config.ThrowIfNull( "config" );
            config.Filenames.ThrowIfNullOrWhiteSpaceAnyElement( "config.Filenames" );

            NGramsType = config.NGramsType;
            Filenames  = config.Filenames.ToArray();
        }

        public NGramsType NGramsType
        {
            get;
            protected set;
        }
        public IEnumerable< string > Filenames
        {
            get;
            protected set;
        }
        public int        TotalClassCount
        {
            get;
            protected set;
        }
        public int        VectorLength
        {
            get;
            protected set;
        }
        /// <summary>
        /// use as readonly in 'Classifier'
        /// </summary>
        public double[]   VectorsSquareLength
        {
            get;
            protected set;
        }

        public abstract bool TryGetValue( string ngram, out float[] modelRow );
        public abstract void Dispose();

        protected void Initialize< TKey >( Dictionary< TKey, float[] > modelDictionary )
        {
            if ( modelDictionary == null || modelDictionary.Count == 0 )
            {
                throw (new ArgumentNullException( "modelDictionary" ));
            }

            TotalClassCount = modelDictionary.First().Value.Length;
            VectorLength    = modelDictionary.Count;

            VectorsSquareLength = new double[ TotalClassCount ];
            var values = modelDictionary.Values;
            Parallel.For( 0, TotalClassCount,
                classIndex => VectorsSquareLength[ classIndex ] = VectorsArithmetic.VectorSquareLength( values, classIndex ) 
            );
        }
    }
}
