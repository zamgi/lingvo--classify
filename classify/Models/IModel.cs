using System;
using System.Collections.Generic;

using lingvo.tokenizing;

namespace lingvo.classify
{
    /// <summary>
    /// 
    /// </summary>
    public interface IModel : IDisposable
    {
        NGramsType NGramsType { get; }
        IEnumerable< string > Filenames { get; }        
        int        VectorLength { get; }
        double[]   VectorsSquareLength { get; }
        int        TotalClassCount { get; }

        bool TryGetValue( string ngram, out float[] modelRow );
    }
}
