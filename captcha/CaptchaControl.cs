using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Web.UI;
using System.Web.UI.WebControls;

using BackgroundNoiseLevel = captcha.CaptchaImage.BackgroundNoiseLevel;
using FontWarpFactor       = captcha.CaptchaImage.FontWarpFactor;
using LineNoiseLevel       = captcha.CaptchaImage.LineNoiseLevel;

namespace captcha
{
    /// <summary>
    /// 
    /// </summary>
    [DefaultProperty("Text")]
    public class CaptchaControl : WebControl, INamingContainer, IPostBackDataHandler, IValidator
    {
        /// <summary>
        /// 
        /// </summary>
        public enum CacheType
        {
            HttpRuntime,
            Session
        }

        public const string CAPTCHA_IMAGE_HANDLER_URL = "CaptchaImage.axd";

        private Color        _BackColor    = Color.White;
        private CacheType    _CacheStrategy;
        private CaptchaImage _CaptchaImage = new CaptchaImage();
        private string       _ErrorMessage = string.Empty;
        private string       _Font         = string.Empty;
        private Color        _FontColor    = Color.Black;
        private Color        _LineColor    = Color.Black;
        private Color        _NoiseColor   = Color.Black;
        private string       _PrevGuid;        
        private int          _TimeoutSecondsMax = 90;
        private int          _TimeoutSecondsMin = 3;
        private bool         _UserValidated     = true;
        private string       _CustomValidatorErrorMessage;
        private string       _ValidationGroup;
        //private string       _Text = "Enter the code shown:";

        public CaptchaControl() => CaptchaImageHandlerUrl = CAPTCHA_IMAGE_HANDLER_URL;

        // Methods
        private string CssStyle()
        {
            var buf = new StringBuilder();
            buf.Append( " style='" );
            if ( BorderWidth.ToString().Length > 0 )
            {
                buf.Append( "border-width:" );
                buf.Append( BorderWidth.ToString() );
                buf.Append( ";" );
            }
            if ( BorderStyle != BorderStyle.NotSet )
            {
                buf.Append( "border-style:" );
                buf.Append( BorderStyle.ToString() );
                buf.Append( ";" );
            }
            string str = HtmlColor( BorderColor );
            if ( str.Length > 0 )
            {
                buf.Append( "border-color:" );
                buf.Append( str );
                buf.Append( ";" );
            }
            str = HtmlColor( BackColor );
            if ( str.Length > 0 )
            {
                buf.Append( "background-color:" + str + ";" );
            }
            str = HtmlColor( ForeColor );
            if ( str.Length > 0 )
            {
                buf.Append( "color:" + str + ";" );
            }
            if ( Font.Bold )
            {
                buf.Append( "font-weight:bold;" );
            }
            if ( Font.Italic )
            {
                buf.Append( "font-style:italic;" );
            }
            if ( Font.Underline )
            {
                buf.Append( "text-decoration:underline;" );
            }
            if ( Font.Strikeout )
            {
                buf.Append( "text-decoration:line-through;" );
            }
            if ( Font.Overline )
            {
                buf.Append( "text-decoration:overline;" );
            }
            if ( Font.Size.ToString().Length > 0 )
            {
                buf.Append( "font-size:" + Font.Size.ToString() + ";" );
            }
            if ( Font.Names.Length > 0 )
            {
                buf.Append( "font-family:" );
                foreach ( string str2 in Font.Names )
                {
                    buf.Append( str2 );
                    buf.Append( "," );
                }
                buf.Length--;
                buf.Append( ";" );
            }
            if ( Height.ToString() != string.Empty )
            {
                buf.Append( "height:" + Height.ToString() + ";" );
            }
            if ( Width.ToString() != string.Empty )
            {
                buf.Append( "width:" + Width.ToString() + ";" );
            }
            buf.Append( "'" );
            if ( buf.ToString() == " style=''" )
            {
                return string.Empty;
            }
            return buf.ToString();
        }

        private void GenerateNewCaptcha()
        {
            if ( !IsDesignMode )
            {
                if ( _CacheStrategy == CacheType.HttpRuntime )
                {
                    HttpRuntime.Cache.Add( _CaptchaImage.UniqueId, _CaptchaImage, null, DateTime.Now.AddSeconds( Convert.ToDouble( (CaptchaMaxTimeout == 0) ? 90 : CaptchaMaxTimeout ) ), TimeSpan.Zero, CacheItemPriority.NotRemovable, null );
                }
                else
                {
                    HttpContext.Current.Session.Add( _CaptchaImage.UniqueId, _CaptchaImage );
                }
            }
        }

        private CaptchaImage GetCachedCaptcha( string guid )
        {
            if ( _CacheStrategy == CacheType.HttpRuntime )
            {
                return (CaptchaImage) HttpRuntime.Cache.Get( guid );
            }
            return (CaptchaImage) HttpContext.Current.Session[ guid ];
        }

        private string HtmlColor( Color color )
        {
            if ( color.IsEmpty )
            {
                return string.Empty;
            }
            if ( color.IsNamedColor )
            {
                return color.ToKnownColor().ToString();
            }
            if ( color.IsSystemColor )
            {
                return color.ToString();
            }
            return ("#" + color.ToArgb().ToString( "x" ).Substring( 2 ));
        }

        protected override void LoadControlState( object state )
        {
            if ( state != null )
            {
                _PrevGuid = (string) state;
            }
        }
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            Page.RegisterRequiresControlState( this );
            Page.Validators.Add( this );
        }
        protected override void OnPreRender( EventArgs e )
        {
            if ( Visible )
            {
                GenerateNewCaptcha();
            }
            base.OnPreRender( e );
        }
        protected override void OnUnload( EventArgs e )
        {
            if ( Page != null )
            {
                Page.Validators.Remove( this );
            }
            base.OnUnload( e );
        }

        private void RemoveCachedCaptcha( string guid )
        {
            if ( _CacheStrategy == CacheType.HttpRuntime )
            {
                HttpRuntime.Cache.Remove( guid );
            }
            else
            {
                HttpContext.Current.Session.Remove( guid );
            }
        }

        protected override void Render( HtmlTextWriter Output )
        {
            Output.Write( "<div" );
            if ( CssClass != string.Empty )
            {
                Output.Write( " class='" + CssClass + "'" );
            }
            Output.Write( CssStyle() );
            Output.Write( ">" );
            Output.Write( "<img src=\"" + CaptchaImageHandlerUrl );
            if ( !IsDesignMode )
            {
                Output.Write( "?guid=" + Convert.ToString( _CaptchaImage.UniqueId ) );
            }
            if ( CacheStrategy == CacheType.Session )
            {
                Output.Write( "&s=1" );
            }
            Output.Write( "\" border='0'" );
            if ( ToolTip.Length > 0 )
            {
                Output.Write( " alt='" + ToolTip + "'" );
            }
            Output.Write( " width="  + _CaptchaImage.Width );
            Output.Write( " height=" + _CaptchaImage.Height );
            Output.Write( ">" );
            Output.Write( "</div>" );
        }
        protected override object SaveControlState() => _CaptchaImage.UniqueId;

        bool IPostBackDataHandler.LoadPostData( string PostDataKey, NameValueCollection Values )
        {
            ValidateCaptcha( Convert.ToString( Values[ UniqueID ] ) );
            return false;
        }
        void IPostBackDataHandler.RaisePostDataChangedEvent() { }
        void IValidator.Validate() { }

        public bool ValidateCaptcha( string userEntry )
        {
            if ( !Visible | !Enabled )
            {
                _UserValidated = true;
            }
            else
            {
                CaptchaImage cachedCaptcha = GetCachedCaptcha( _PrevGuid );
                if ( cachedCaptcha == null )
                {
                    ((IValidator) this).ErrorMessage = "Код вводился слишком долго, его срок истек"; // после " + CaptchaMaxTimeout + " секунд.";
                    //"The code you typed has expired after " + CaptchaMaxTimeout + " seconds.";
                    _UserValidated = false;
                }
                else if ( (CaptchaMinTimeout > 0) && (cachedCaptcha.RenderedAt.AddSeconds( (double) CaptchaMinTimeout ) > DateTime.Now) )
                {
                    _UserValidated = false;
                    ((IValidator) this).ErrorMessage = "Код был введен слишком быстро. Ожидайте по крайней мере " + CaptchaMinTimeout + " секунд.";
                    //"Code was typed too quickly. Wait at least " + CaptchaMinTimeout + " seconds.";
                    RemoveCachedCaptcha( _PrevGuid );
                }
                else if ( string.Compare( userEntry, cachedCaptcha.Text, CaptchaIgnoreCase ) != 0 )
                {
                    ((IValidator) this).ErrorMessage = "Код, который Вы ввели, не соответствует коду на изображении.";
                    //"The code you typed does not match the code in the image.";
                    _UserValidated = false;
                    RemoveCachedCaptcha( _PrevGuid );
                }
                else
                {
                    _UserValidated = true;
                    RemoveCachedCaptcha( _PrevGuid );
                }
            }

            if ( !_UserValidated && string.IsNullOrEmpty( ((IValidator) this).ErrorMessage ) )
            {
                ((IValidator) this).ErrorMessage = "Введен неверный код.";
            }
            return (_UserValidated);
        }

        public Color BackColor
        {
            get => _BackColor;
            set
            {
                _BackColor = value;
                _CaptchaImage.BackColor = _BackColor;
            }
        }

        [Category("Captcha"), Description("Determines if CAPTCHA codes are stored in HttpRuntime (fast, but local to current server) or Session (more portable across web farms)." ), DefaultValue( typeof(CacheType), "HttpRuntime")]
        public CacheType CacheStrategy
        {
            get => _CacheStrategy;
            set => _CacheStrategy = value;
        }

        [Category("Captcha"), DefaultValue( typeof(BackgroundNoiseLevel), "Low" ), Description("Amount of background noise to generate in the CAPTCHA image")]
        public BackgroundNoiseLevel CaptchaBackgroundNoise
        {
            get => _CaptchaImage.BackgroundNoise;
            set => _CaptchaImage.BackgroundNoise = value;
        }

        [Category("Captcha"), DefaultValue("ABCDEFGHJKLMNPQRSTUVWXYZ23456789"), Description( "Characters used to render CAPTCHA text. A character will be picked randomly from the string." )]
        public string CaptchaChars
        {
            get => _CaptchaImage.TextChars;
            set => _CaptchaImage.TextChars = value;
        }

        [Category("Captcha"), DefaultValue(""), Description("Font used to render CAPTCHA text. If font name is blank, a random font will be chosen.")]
        public string CaptchaFont
        {
            get => _Font;
            set
            {
                _Font = value;
                _CaptchaImage.Font = _Font;
            }
        }

        [Category("Captcha"), DefaultValue( typeof(FontWarpFactor), "Low" ), Description( "Amount of random font warping used on the CAPTCHA text" )]
        public FontWarpFactor CaptchaFontWarping
        {
            get => _CaptchaImage.FontWarp;
            set => _CaptchaImage.FontWarp = value;
        }

        [Category("Captcha"), DefaultValue( 50 ), Description( "Height of generated CAPTCHA image." )]
        public int CaptchaHeight
        {
            get => _CaptchaImage.Height;
            set => _CaptchaImage.Height = value;
        }

        [Category("Captcha"), DefaultValue( 5 ), Description( "Number of CaptchaChars used in the CAPTCHA text" )]
        public int CaptchaLength
        {
            get => _CaptchaImage.TextLength;
            set => _CaptchaImage.TextLength = value;
        }

        [Category("Captcha"), Description( "Add line noise to the CAPTCHA image" ), DefaultValue( typeof(LineNoiseLevel), "None" )]
        public LineNoiseLevel CaptchaLineNoise
        {
            get => _CaptchaImage.LineNoise;
            set => _CaptchaImage.LineNoise = value;
        }

        [Category("Captcha"), DefaultValue( 90 ), Description( "Maximum number of seconds CAPTCHA will be cached and valid. If you're too slow, you may be a CAPTCHA hack attempt. Set to zero to disable." )]
        public int CaptchaMaxTimeout
        {
            get => _TimeoutSecondsMax;
            set
            {
                if ( (value < 15) & (value != 0) )
                {
                    throw new ArgumentOutOfRangeException( "CaptchaTimeout", "Timeout must be greater than 15 seconds. Humans can't type that fast!" );
                }
                _TimeoutSecondsMax = value;
            }
        }

        [Category("Captcha"), DefaultValue( 2 ), Description( "Minimum number of seconds CAPTCHA must be displayed before it is valid. If you're too fast, you must be a robot. Set to zero to disable." )]
        public int CaptchaMinTimeout
        {
            get => _TimeoutSecondsMin;
            set
            {
                if ( value > 15 )
                {
                    throw new ArgumentOutOfRangeException( "CaptchaTimeout", "Timeout must be less than 15 seconds. Humans aren't that slow!" );
                }
                _TimeoutSecondsMin = value;
            }
        }

        [Category("Captcha"), DefaultValue( 180 ), Description( "Width of generated CAPTCHA image." )]
        public int CaptchaWidth
        {
            get => _CaptchaImage.Width;
            set => _CaptchaImage.Width = value;
        }

        public string CustomValidatorErrorMessage
        {
            get => _CustomValidatorErrorMessage;
            set => _CustomValidatorErrorMessage = value;
        }

        public override bool Enabled
        {
            get => base.Enabled;
            set
            {
                base.Enabled = value;
                if ( !value )
                {
                    _UserValidated = true;
                }
            }
        }

        public Color FontColor
        {
            get => _FontColor;
            set
            {
                _FontColor = value;
                _CaptchaImage.FontColor = _FontColor;
            }
        }

        private bool IsDesignMode => (HttpContext.Current == null);

        public Color LineColor
        {
            get => _LineColor;
            set
            {
                _LineColor = value;
                _CaptchaImage.LineColor = _LineColor;
            }
        }

        public Color NoiseColor
        {
            get => _NoiseColor;
            set
            {
                _NoiseColor = value;
                _CaptchaImage.NoiseColor = _NoiseColor;
            }
        }

        [Category("Appearance"), Description( "Message to display in a Validation Summary when the CAPTCHA fails to validate." ), Browsable( false ), Bindable( true ), DefaultValue( "The text you typed does not match the text in the image." )]
        string IValidator.ErrorMessage
        {
            get
            {
                if ( !_UserValidated )
                {
                    return _ErrorMessage;
                }
                return string.Empty;
            }
            set => _ErrorMessage = value;
        }

        bool IValidator.IsValid
        {
            get => _UserValidated;
            set { }
        }

        [Category("Captcha"), Description( "Returns True if the user was CAPTCHA validated after a postback." )]
        public bool UserValidated => _UserValidated;

        public string ValidationGroup
        {
            get => _ValidationGroup;
            set => _ValidationGroup = value;
        }

        public string CaptchaImageUniqueId => ((_CaptchaImage != null) ? _CaptchaImage.UniqueId : null);

        [Category("Captcha"), DefaultValue( false ), Description( "Ignore case when compare CAPTCHA image text." )]
        public bool CaptchaIgnoreCase { get; set; }

        [Category("Captcha"), DefaultValue( CAPTCHA_IMAGE_HANDLER_URL ), Description( "CAPTCHA-IMAGE-HANDLER-URL." )]
        public string CaptchaImageHandlerUrl { get; set; }
    }
}
