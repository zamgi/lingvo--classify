using System;
using System.Runtime.Caching;
using System.Threading;

namespace captcha
{
    /// <summary>
    /// 
    /// </summary>
    public readonly struct AntiBotConfig
    {
        public const int    SAME_IP_BANNED_INTERVAL_IN_SECONDS  = 120;
		public const int    SAME_IP_INTERVAL_REQUEST_IN_SECONDS = 10;
		public const int    SAME_IP_MAX_REQUEST_IN_INTERVAL     = 3;

        //public HttpContext HttpContext { get; init; }
        public string RemoteIpAddress                { get; init; }
        public int    SameIpBannedIntervalInSeconds  { get; init; }
        public int    SameIpIntervalRequestInSeconds { get; init; }
        public int    SameIpMaxRequestInInterval     { get; init; }
    }

    /// <summary>
    /// 
    /// </summary>
    public struct AntiBot
    {
        /// <summary>
        /// 
        /// </summary>
        private sealed class RequestMarker
        {
            private const int FALSE = 0;
            private const int TRUE  = ~FALSE;

            public RequestMarker()
            {
                _DateTimeTicks = DateTime.Now.Ticks;
                _Count         = 1;
                _IsBanned      = FALSE; 
            }

            private int  _Count;
            private int  _IsBanned;
            private long _DateTimeTicks;

            public DateTime DateTime => new DateTime( _DateTimeTicks );
            public int      Count    => _Count;
            public bool     IsBanned => (_IsBanned != FALSE); 

            public void CountIncrement() => Interlocked.Increment( ref _Count );
            public void Banned()
            {
                Interlocked.Exchange( ref _IsBanned, TRUE );
                Interlocked.Exchange( ref _DateTimeTicks, DateTime.Now.Ticks );
            }
            public int  GetWaitRemainSeconds( in AntiBotConfig config )
            {
                var passSeconds = (DateTime.Now - this.DateTime).TotalSeconds;
                int result;
                if ( this.IsBanned )
                {
                    result = config.SameIpBannedIntervalInSeconds - Convert.ToInt32( passSeconds ); // +1;
                }
                else
                {
                    result = config.SameIpIntervalRequestInSeconds - Convert.ToInt32( passSeconds ) + 1;
                }
                return ((0 < result) ? result : 0);
            }
        }

        private const string KEY_CACHE_KEY = "SAME_IP_REQUEST";

        private AntiBotConfig _Config;
        public AntiBot( AntiBotConfig config ) => _Config = config;

        private string CACHE_KEY => (KEY_CACHE_KEY + ',' + _Config.RemoteIpAddress); 
        private bool TryGetCurrentRequestMarker( out RequestMarker requestMarker ) 
        {
            requestMarker = MemoryCache.Default/*_Config.HttpContext.Cache*/[ CACHE_KEY ] as RequestMarker;
            return (requestMarker != null);
        }
        private void AddCurrentRequestMarker2Cache( RequestMarker requestMarker, int absoluteExpirationInCacheInSeconds )
        {
            if ( requestMarker == null ) throw (new ArgumentNullException( nameof(requestMarker) ));

            MemoryCache.Default.Remove( CACHE_KEY );
            MemoryCache.Default.Add( CACHE_KEY, requestMarker, new CacheItemPolicy()
            {
                AbsoluteExpiration = requestMarker.DateTime.AddSeconds( Convert.ToDouble( absoluteExpirationInCacheInSeconds ) ),
                SlidingExpiration  = TimeSpan.Zero,
                Priority           = CacheItemPriority.NotRemovable,
            });
        }
        private void RemoveCurrentRequestMarkerFromCache() => MemoryCache.Default/*_Config.HttpContext.Cache*/.Remove( CACHE_KEY );
        private void BannedRequest( RequestMarker requestMarker )
        {
            requestMarker.Banned();

            AddCurrentRequestMarker2Cache( requestMarker, _Config.SameIpBannedIntervalInSeconds + 1 );
        }

        public bool IsRequestValid() => TryGetCurrentRequestMarker( out var requestMarker ) ? (requestMarker.Count < _Config.SameIpMaxRequestInInterval) : true; 
        public bool IsNeedRedirectOnCaptchaIfRequestNotValid()
        {
            if ( !IsRequestValid() && TryGetCurrentRequestMarker( out var requestMarker ) )
            {
                //if ( requestMarker.IsBanned )
                //{
                    //---------------------------//
                    //requestMarker.CountIncrement();
                    //BannedRequest( requestMarker );
                    //---------------------------//

                    //_Config.HttpContext.Response.Redirect( _Config.CaptchaPageUrl, true );
                //}
                //else
                //{
                    var waitRemainSeconds = requestMarker.GetWaitRemainSeconds( _Config );
                    if ( 3 < waitRemainSeconds )  
                    {
                        BannedRequest( requestMarker );

                        return (true); // _Config.HttpContext.Response.Redirect( _Config.CaptchaPageUrl, true );
                    }
                    else
                    {
                        Thread.Sleep( Math.Max( 1, waitRemainSeconds ) * 1000 );
                    }
                //}
            }
            return (false);
        }
        public void MarkRequest()
        {
            if ( TryGetCurrentRequestMarker( out var requestMarker ) )
            {
                requestMarker.CountIncrement();
            }
            else
            {
                requestMarker = new RequestMarker();

                AddCurrentRequestMarker2Cache( requestMarker, _Config.SameIpIntervalRequestInSeconds );
            }
        }
        public void MakeAllowRequests() => RemoveCurrentRequestMarkerFromCache();
        public int GetWaitRemainSeconds() => TryGetCurrentRequestMarker( out var requestMarker ) ? requestMarker.GetWaitRemainSeconds( _Config ) : 0;

        public static object CreateGotoOnCaptchaResponseObj() => new { err = "goto-on-captcha" };
    }
}
