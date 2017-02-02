using System;
using System.Collections.Generic;

using lingvo.tokenizing;

namespace lingvo.classify
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ModelConfig
    {
        public IEnumerable< string > Filenames
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
