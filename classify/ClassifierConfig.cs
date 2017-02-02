using System;

using lingvo.urls;

namespace lingvo.classify
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ClassifierConfig
    {
        public ClassifierConfig( string urlDetectorResourcesXmlFilename )
        {
            UrlDetectorModel = new UrlDetectorModel( urlDetectorResourcesXmlFilename );
        }

        public UrlDetectorModel UrlDetectorModel
        {
            get;
            set;
        }
    }
}
