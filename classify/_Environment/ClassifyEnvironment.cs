using System;
using System.Diagnostics;

namespace lingvo.classify
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ClassifyEnvironment : IDisposable
    {
        private ClassifyEnvironment() { }
        public void Dispose()
        {
            if ( Model != null )
            {
                Model.Dispose();
                Model = null;
            }
        }

        public IClassifyEnvironmentConfig ClassifyEnvironmentConfig { get; private set; }
        public ClassifierConfig ClassifierConfig { get; private set; }
        private IModel Model { get; set; }

        public Classifier CreateClassifier() => new Classifier( ClassifierConfig, Model );

        public static ClassifyEnvironment Create( IClassifyEnvironmentConfig opts, bool print2Console = true )
        {
            var sw = default(Stopwatch);
            if ( print2Console )
            {
                sw = Stopwatch.StartNew();
                Console.Write( "init classify-environment..." );
            }

            var modelConfig = new ModelConfig()
            {
                Filenames   = opts.MODEL_FILENAMES,
                RowCapacity = opts.MODEL_ROW_CAPACITY, 
                NGramsType  = opts.MODEL_NGRAMS_TYPE 
            };
            var model  = new ModelNative( modelConfig ); //new ModelHalfNative( modelConfig ); //new ModelClassic( modelConfig );
            var config = new ClassifierConfig( opts.URL_DETECTOR_RESOURCES_XML_FILENAME );

            var env = new ClassifyEnvironment() { ClassifierConfig = config, Model = model, ClassifyEnvironmentConfig = opts };

            if ( print2Console )
            {
                sw.Stop();
                Console.WriteLine( $"end, (elapsed: {sw.Elapsed}).\r\n----------------------------------------------------\r\n" );
            }

            return (env);
        }
        public static ClassifyEnvironment Create( bool print2Console = true ) => Create( new ClassifyEnvironmentConfigImpl(), print2Console );
    }
}
