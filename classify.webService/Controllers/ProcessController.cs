using System;

using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
#if DEBUG
using Microsoft.Extensions.Logging;
#endif

using captcha;

namespace classify.webService.Controllers
{
    public sealed class ProcessController : Controller
    {
        #region [.ctor().]
        private readonly ConcurrentFactory _ConcurrentFactory;
#if DEBUG
        private readonly ILogger< ProcessController > _Logger;
#endif
#if DEBUG
        public ProcessController( ConcurrentFactory concurrentFactory, ILogger< ProcessController > logger )
        {
            _ConcurrentFactory = concurrentFactory;
            _Logger            = logger;
        }
#else
        public ProcessController( ConcurrentFactory concurrentFactory ) => _ConcurrentFactory = concurrentFactory;
#endif
        #endregion

        [HttpPost] public async Task< IActionResult > Run( [FromBody] InitParamsVM m )
        {
            try
            {
                #region [.anti-bot.]
                var antiBot = HttpContext.ToAntiBot( _ConcurrentFactory.Config );
                if ( antiBot.IsNeedRedirectOnCaptchaIfRequestNotValid() )
                {
                    return Json( AntiBot.CreateGotoOnCaptchaResponseObj() );
                }
                #endregion

                #region [.anti-bot.]
                antiBot.MarkRequestEx( m.Text );
                #endregion
#if DEBUG
                _Logger.LogInformation( $"start Find '{m.Text}'..." );
#endif
                var p = await _ConcurrentFactory.Run( m.Text );
                var result = new ResultVM( m, p, _ConcurrentFactory.Config );
#if DEBUG
                _Logger.LogInformation( $"end Find '{m.Text}'." );
#endif
                return Ok( result );
            }
            catch ( Exception ex )
            {
#if DEBUG
                _Logger.LogError( $"Error while find: '{m.Text}' => {ex}" );
#endif
                return Ok( new ResultVM( m, ex ) );
            }
        }
    }
}
