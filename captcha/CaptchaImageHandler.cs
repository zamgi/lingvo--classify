using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace captcha
{
    /// <summary>
    /// 
    /// </summary>
    public static class CaptchaImageHandler
    {
        public static async Task ProcessRequest( HttpContext context )
        {
            string key = context.Request.Query[ "guid" ];
            CaptchaImage image = null;
            if ( !string.IsNullOrEmpty( key ) )
            {
                if ( string.IsNullOrEmpty( context.Request.Query[ "s" ] ) )
                {
                    image = (CaptchaImage) MemoryCache.Default.Get( key );
                }
                else
                {
                    image = (CaptchaImage) context.Items[ key ];
                }
            }

            if ( image == null )
            {
                if ( key == "xz" )
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode  = 200;
                    await context.Response.WriteAsJsonAsync( new { key = "xz" } );                    
                }
                else
                {
                    context.Response.StatusCode = 404;
                }
            }
            else
            {
                using ( Bitmap bitmap = image.RenderImage() )
                {
                    bitmap.Save( context.Response.Body, ImageFormat.Jpeg );
                }
                context.Response.ContentType = "image/jpeg";
                context.Response.StatusCode  = 200;
            }
        }
    }
}
