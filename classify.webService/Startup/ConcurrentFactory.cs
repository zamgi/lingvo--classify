using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using lingvo.classify;

namespace classify.webService
{
    /// <summary>
    /// 
    /// </summary>
	public sealed class ConcurrentFactory
	{
		private readonly SemaphoreSlim                 _Semaphore;
        private readonly ConcurrentStack< Classifier > _Stack;

        public ConcurrentFactory( ClassifierConfig config, IModel model, IConfig cfg )
		{            
            if ( config == null     ) throw (new ArgumentNullException( nameof(config) ));
            if ( model  == null     ) throw (new ArgumentNullException( nameof(model) ));
            
			Config = cfg ?? throw (new ArgumentNullException( nameof(cfg) ));
			var instanceCount = cfg.CONCURRENT_FACTORY_INSTANCE_COUNT;
            if ( instanceCount <= 0 ) throw (new ArgumentException( nameof(instanceCount) ));

            _Semaphore = new SemaphoreSlim( instanceCount, instanceCount );
            _Stack     = new ConcurrentStack< Classifier >();
            for ( int i = 0; i < instanceCount; i++ )
			{
                _Stack.Push( new Classifier( config, model ) );
			}			
		}

		public IConfig Config { get; }

        public async Task< IList< ClassifyInfo > > Run( string text )
		{
			await _Semaphore.WaitAsync().ConfigureAwait( false );
			var worker = default(Classifier);
			var result = default(IList< ClassifyInfo >);
			try
			{
                worker = Pop( _Stack );
                result = worker.MakeClassify( text );
			}
			finally
			{
                if ( worker != null )
				{
                    _Stack.Push( worker );
				}
				_Semaphore.Release();
			}
			return (result);
		}

        private static T Pop< T >( ConcurrentStack< T > stack ) => stack.TryPop( out var t ) ? t : default;
	}
}
