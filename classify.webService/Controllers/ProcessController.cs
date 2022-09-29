using System;
using System.Threading;

using Microsoft.AspNetCore.Mvc;
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

        [HttpPost] public IActionResult Run( [FromBody] InitParamsVM m )
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
                var p = _ConcurrentFactory.Run( m.Text );
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

        private const string CAPTCHA_PAGE_LOCATION = "~/Views/Captcha.cshtml";
        [ HttpGet] public IActionResult Captcha()
        {
            Thread.Sleep( 1000 );

            var antiBot = HttpContext.ToAntiBot( _ConcurrentFactory.Config );
            if ( antiBot.IsRequestValid() )
            {
                return Redirect( "~/" );
            }

            var m = new CaptchaVM()
            {
                WaitRemainSeconds    = antiBot.GetWaitRemainSeconds(),
                CaptchaImageUniqueId = CaptchaProcessor.CreateNewCaptcha(),
            };
            return View( CAPTCHA_PAGE_LOCATION, m );
        }
        [HttpGet] public IActionResult Captcha_Image()
        {
            if ( CaptchaProcessor.TryGetCaptchaImage( HttpContext, out var bytes, out var contentType ) )
            {
                return File( bytes, contentType );
            }
            return NotFound();
        }
        [HttpPost] public IActionResult Captcha_Process( [FromForm] Captcha_ProcessVM m )
        {
            const string MAGIC_WORD = "12qwQW12";
            
            var p = new CaptchaProcessor.ValidateCaptchaParams()
            {
                CaptchaImageUniqueId = m.CaptchaImageUniqueId,
                CaptchaUserText      = m.CaptchaUserText,
            };
            var antiBot = HttpContext.ToAntiBot( _ConcurrentFactory.Config );
            if ( CaptchaProcessor.ValidateCaptcha( p, out var errorMessage ) || (m.CaptchaUserText == MAGIC_WORD) )
            {
                antiBot.MakeAllowRequests();
                return Redirect( m.RedirectLocation ?? "~/" );
            }
            else
            {
                var resp_model = new CaptchaVM()
                {
                    WaitRemainSeconds    = antiBot.GetWaitRemainSeconds(),                    
                    CaptchaImageUniqueId = CaptchaProcessor.CreateNewCaptcha(), //-- OR SAME IMAGE (NEED TURN-OFF Removing Him from Cache when Bad check-attempt)-- //CaptchaImageUniqueId = m.CaptchaImageUniqueId,
                    ErrorMessage         = errorMessage,
                };
                return View( CAPTCHA_PAGE_LOCATION, resp_model );
            }
        }
    }
}
