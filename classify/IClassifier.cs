using System;
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

        public override string ToString()
        {
            return ("class-index: '" + ClassIndex + "', svm-cosine: '" +
                    Cosine.ToString( new NumberFormatInfo() { NumberDecimalSeparator = "." } )
                   );
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IClassifier
    {
        int TotalClassCount { get; }

        ClassifyInfo[] MakeClassify( string text );
    }
}
