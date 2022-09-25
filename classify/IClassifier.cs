using System.Collections.Generic;
using System.Globalization;

namespace lingvo.classify
{
    /// <summary>
    /// 
    /// </summary>
    public struct ClassifyInfo
    {
        public int    ClassIndex;
        public double Cosine;

        public override string ToString() => ("class-index: '" + ClassIndex + "', svm-cosine: '" +
                                              Cosine.ToString( new NumberFormatInfo() { NumberDecimalSeparator = "." } )
                                             );
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IClassifier
    {
        int TotalClassCount { get; }
        IList< ClassifyInfo > MakeClassify( string text );
    }
}
