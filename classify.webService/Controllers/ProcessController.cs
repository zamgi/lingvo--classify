using System;

using Microsoft.AspNetCore.Mvc;
#if DEBUG
using Microsoft.Extensions.Logging;
#endif

namespace classify.webService.Controllers
{
    [ApiController, Route("[controller]")]
    public sealed class ProcessController : ControllerBase
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

        [HttpPost, Route("Run")] public IActionResult Run( [FromBody] InitParamsVM m )
        {
            try
            {
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
                return Ok( new ResultVM( m, ex ) ); //---return StatusCode( 500, new ResultVM( m, ex ) ); //Internal Server Error
            }
        }
    }
}
