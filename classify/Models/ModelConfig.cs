using System;

using lingvo.tokenizing;

namespace lingvo.classify
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ModelConfig
    {
        public string     Filename
        {
            get;
            set;
        }
        public int        RowCapacity
        {
            get;
            set;
        }
        public NGramsType NGramsType
        {
            get;
            set;
        }
    }
}
