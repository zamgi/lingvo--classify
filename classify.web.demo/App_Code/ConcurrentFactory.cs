using System;
using System.Collections.Concurrent;
using System.Threading;

namespace lingvo.classify
{
    /// <summary>
    /// 
    /// </summary>
	internal sealed class ConcurrentFactory
	{
		private readonly Semaphore                     _Semaphore;
        private readonly ConcurrentStack< Classifier > _Stack;

        public ConcurrentFactory( ClassifierConfig config, IModel model, int instanceCount )
		{
            if ( instanceCount <= 0 ) throw (new ArgumentException("instanceCount"));
            if ( config == null     ) throw (new ArgumentNullException("config"));
            if ( model  == null     ) throw (new ArgumentNullException("model"));
            
            _Semaphore = new Semaphore( instanceCount, instanceCount );
            _Stack = new ConcurrentStack< Classifier >();
            for ( int i = 0; i < instanceCount; i++ )
			{
                _Stack.Push( new Classifier( config, model ) );
			}			
		}

        public ClassifyInfo[] MakeClassify( string text )
		{
			_Semaphore.WaitOne();
			var worker = default(Classifier);
			var result = default(ClassifyInfo[]);
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

        private static T Pop< T >( ConcurrentStack< T > stack )
        {
            var t = default(T);
            if ( stack.TryPop( out t ) )
                return (t);
            return (default(T));
        }
	}
}
