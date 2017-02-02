using System;

using lingvo.tokenizing;

namespace lingvo.classify
{
    /// <summary>
    /// 
    /// </summary>
    public interface IModel : IDisposable
    {
        NGramsType NGramsType { get; }
        string     Filename { get; }        
        int        VectorLength { get; }
        double[]   VectorsSquareLength { get; }
        int        TotalClassCount { get; }

        bool TryGetValue( string ngram, out float[] modelRow );
    }
}
