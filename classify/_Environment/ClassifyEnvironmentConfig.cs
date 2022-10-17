using System.Collections.Generic;
using System.Linq;

using lingvo.tokenizing;

namespace lingvo.classify
{
    /// <summary>
    /// 
    /// </summary>
    public interface IClassifyEnvironmentConfig
    {
        string URL_DETECTOR_RESOURCES_XML_FILENAME { get; }
        string MODEL_FOLDER { get; }
        IReadOnlyList< string > MODEL_FILENAMES   { get; }
        IReadOnlyList< string > CLASS_INDEX_NAMES { get; }
        NGramsType MODEL_NGRAMS_TYPE { get; }
        int MODEL_ROW_CAPACITY { get; }
        int CLASS_THRESHOLD_PERCENT { get; }

        string ClassIndex2Text( int classIndex );
    }

    /// <summary>
    /// 
    /// </summary>
    public readonly struct classes_t
    {
        public int    class_index { get; init; }
        public string class_name  { get; init; }
        public double cosine      { get; init; }
        public double percent     { get; init; }

        public override string ToString() => $"class: {class_name}, percent: {percent:N2}%";

        public static IList< classes_t > GetClasses( IList< ClassifyInfo > classifyInfos, IClassifyEnvironmentConfig opts )
        {
            var sum = classifyInfos.Sum( ci => ci.Cosine );
            var classes = (from ci in classifyInfos
                           let percent = (ci.Cosine / sum) * 100
                           where (opts.CLASS_THRESHOLD_PERCENT <= percent)
                           select
                           new classes_t()
                           {
                               class_index = ci.ClassIndex,
                               class_name  = opts.ClassIndex2Text( ci.ClassIndex ),
                               cosine      = ci.Cosine,
                               percent     = percent,
                           }
                          ).ToList();
            return (classes);
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public abstract class ClassifyEnvironmentConfigBase : IClassifyEnvironmentConfig
    {
        public abstract string URL_DETECTOR_RESOURCES_XML_FILENAME { get; }
        public abstract string MODEL_FOLDER { get; }
        public abstract IReadOnlyList< string > MODEL_FILENAMES  { get; }
        public abstract IReadOnlyList< string > CLASS_INDEX_NAMES { get; }        
        public abstract NGramsType MODEL_NGRAMS_TYPE { get; }
        public abstract int MODEL_ROW_CAPACITY      { get; }
        public abstract int CLASS_THRESHOLD_PERCENT { get; }        

        public string ClassIndex2Text( int classIndex )
        {
            if ( 0 <= classIndex && classIndex < CLASS_INDEX_NAMES.Count )
            {
                return (CLASS_INDEX_NAMES[ classIndex ]);
            }
            return ($"[class-index: {classIndex}]");
        }
    }
}
