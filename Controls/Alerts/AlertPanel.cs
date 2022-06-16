using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Charr.Timers_BlishHUD.Controls.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.ComponentModel;
using Blish_HUD.Input;

namespace Charr.Timers_BlishHUD.Controls
{
    public class AlertPanel : FlowPanel, IAlertPanel
    {
        public static int DEFAULT_ALERTPANEL_WIDTH = 320;
        public static int DEFAULT_ALERTPANEL_HEIGHT = 128;

        // Fields with public accessors
        private AsyncTexture2D _icon;

        private readonly Glide.Tween _animFade;
        private bool _shouldDispose = false;

        private float _maxFill;
        private float _currentFill;
        private Glide.Tween _animFill;
        private Color _fillColor = Color.LightGray;

        private string _Text;
        private Color _TextColor = Color.White;

        private string _timerText;
        private Color _timerTextColor = Color.White;

        public string Text {
            get => _Text;
            set => SetProperty(ref _Text, value);
        }

        public Color TextColor {
            get => _TextColor;
            set => SetProperty(ref _TextColor, value);
        }

        public string TimerText {
            get => _timerText;
            set => SetProperty(ref _timerText, value);
        }

        public Color TimerTextColor {
            get => _timerTextColor;
            set => SetProperty(ref _timerTextColor, value);
        }

        public AsyncTexture2D Icon {
            get => _icon;
            set {
                if (_icon != null) {
                    _icon.TextureSwapped -= _textureSwapEventHandler;
                }
                if (SetProperty(ref _icon, value)) {
                    _icon.TextureSwapped += _textureSwapEventHandler;
                    RecalculateLayout();
                }
            }
        }

        public float MaxFill {
            get => _maxFill;
            set {
                if (SetProperty(ref _maxFill, value)) {
                    RecalculateLayout();
                }
            }
        }

        public float CurrentFill {
            get => _currentFill;
            set {
                if (SetProperty(ref _currentFill, Math.Min(value, _maxFill))) {
                    _animFill?.Cancel();
                    _animFill = null;
                    _animFill = Animation.Tweener.Tween(this,
                        new { DisplayedFill = _currentFill },
                        TimersModule.ModuleInstance.Resources.TICKINTERVAL,
                        0, false);
                    RecalculateLayout();
                }
                if (_currentFill >= _maxFill)
                    _scrollEffect?.Enable();
            }
        }

        public Color FillColor {
            get => _fillColor;
            set => SetProperty(ref _fillColor, value);
        }

        public Boolean ShouldShow { get; set; }

        // Fields to store layout information
        private int _iconSize;
        private Rectangle _iconBounds;

        private float _fillPercent;
        private float _fillHeight;
        private Rectangle _topIconSource;
        private Rectangle _topIconDestination;
        private Rectangle _bottomIconSource;
        private Rectangle _bottomIconDestination;
        private Rectangle _fillDestination;
        private Rectangle _fillCrestDestination;
        private Rectangle _timerTextDestination;
        private Rectangle _alertTextDestination;

        // Texture swap event handler
        private readonly EventHandler<ValueChangedEventArgs<Texture2D>> _textureSwapEventHandler;

        private readonly SimpleScrollingHighlightEffect _scrollEffect;

        #region LEAVE_ME_ALONE

        /// <summary>
        /// Do not directly manipulate this property.  It is only public because the animation library requires it to be public.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public float DisplayedFill { get; set; } = 0;

        #endregion

        // Methods
        public AlertPanel() : base() {
            this.Size = new Point(DEFAULT_ALERTPANEL_WIDTH, DEFAULT_ALERTPANEL_HEIGHT);
            _iconSize = DEFAULT_ALERTPANEL_HEIGHT;

            _scrollEffect = new SimpleScrollingHighlightEffect(this) {
                Enabled = false
            };
            this.EffectBehind = _scrollEffect;

            _textureSwapEventHandler = new EventHandler<ValueChangedEventArgs<Texture2D>>((sender, args) => {
                RecalculateLayout();
            });

            Opacity = 0f;
            _animFade = Animation.Tweener.Tween(this, new { Opacity = 1f }, TimersModule.ModuleInstance._alertFadeDelaySetting.Value).Repeat().Reflect();

            _animFade?.OnComplete(() => {
                _animFade.Pause();
                if (Opacity <= 0) {
                    this.Visible = false;
                    _scrollEffect?.Disable();
                }
                else if (_currentFill >= _maxFill) {
                    _scrollEffect?.Enable();
                }

                if (_shouldDispose) {
                    _icon?.Dispose();
                    base.Dispose();
                }
            });

            GameService.Input.Mouse.LeftMouseButtonPressed += delegate (Object sender, MouseEventArgs e) {
                ((AlertContainer)this.Parent).HandleLeftMouseButtonReleased(e);
            };
        }

        protected override CaptureType CapturesInput() {
            if (!TimersModule.ModuleInstance._lockAlertContainerSetting.Value) {
                return base.CapturesInput();
            }

            return CaptureType.None | CaptureType.DoNotBlock;
        }

        public override void RecalculateLayout() {
            // The icon is a square and always takes up the entire height of the alert panel
            _iconSize = Height;
            _iconBounds = new Rectangle(0, 0, _iconSize, _iconSize);

            _fillPercent = TimersModule.ModuleInstance._alertFillDirection.Value
                               ? ((_maxFill > 0) ? (_currentFill / _maxFill) : 1f)
                               : 1f - ((_maxFill > 0) ? (_currentFill / _maxFill) : 1f);
            // The height starting from the bottom of the icon to fill up to
            _fillHeight = _iconSize * _fillPercent;

            // The icon image is split into two parts - (1) image above the fill crest, (2) image below the fill crest
            if (_icon != null) {
                _topIconSource = new Rectangle(0, 0, _icon.Texture.Width,
                    _icon.Texture.Height - (int)(_icon.Texture.Height * _fillPercent));
                _bottomIconSource = new Rectangle(0, _icon.Texture.Height - (int)(_icon.Texture.Height * _fillPercent),
                    _icon.Texture.Width,
                    (int)(_icon.Texture.Height * _fillPercent));
            }

            // Where the icon images will be drawn, image is automatically scaled to fit the destination rectangle size
            _topIconDestination = new Rectangle(0, 0, _iconSize, _iconSize - (int)_fillHeight);
            _bottomIconDestination = new Rectangle(0, _iconSize - (int)_fillHeight, _iconSize, (int)_fillHeight);

            // A colored rectangle to indicate fill
            _fillDestination = new Rectangle(0, (int)(_iconSize - _fillHeight), _iconSize, (int)(_fillHeight));

            // A white "line" at the fill height
            _fillCrestDestination = new Rectangle(0, _iconSize - (int)(_fillHeight), _iconSize, _iconSize);

            // Location for Text that displays time
            _timerTextDestination = new Rectangle(0, 0, _iconSize, (int)(_iconSize * 0.99f));

            // Location for alert text
            _alertTextDestination = new Rectangle(_iconSize + 16, 0, _size.X - _iconSize - 35, this.Height);
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds) {
            if (!Visible || !ShouldShow) return;

            // Draw background
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, bounds, Color.Black * 0.2f);

            // Handle fill
            //if (_maxFill > 0) {
            // Draw icon twice
            if (_icon != null) {
                // Icon above the fill
                if (_fillPercent < 1.0f) {
                    spriteBatch.DrawOnCtrl(this, _icon, _topIconDestination, _topIconSource, Color.DarkGray * 0.4f);
                }

                // Icon below the fill
                if (_fillPercent > 0f) {
                    spriteBatch.DrawOnCtrl(this, _icon, _bottomIconDestination, _bottomIconSource);
                }
            }

            if (_fillPercent > 0) {
                // Draw the fill
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, _fillDestination, _fillColor * 0.3f);

                // Only show the fill crest if it's not completely filled
                if (_fillPercent < 0.99f)
                    spriteBatch.DrawOnCtrl(this, TimersModule.ModuleInstance.Resources.TextureFillCrest,
                        _fillCrestDestination);
            }
            //}
            //else if (_icon != null) {
            // Draw icon without any fill effects
            //spriteBatch.DrawOnCtrl(this, _icon, _iconBounds);
            //}

            // Draw icon vignette (draw with or without the icon to keep a consistent look)
            spriteBatch.DrawOnCtrl(this, TimersModule.ModuleInstance.Resources.TextureVignette, _iconBounds);

            // Draw time text
            if (!string.IsNullOrEmpty(_timerText))
                spriteBatch.DrawStringOnCtrl(this, $"{this._timerText}", Content.DefaultFont32, _timerTextDestination,
                    this.TimerTextColor, false, true, 1, HorizontalAlignment.Center, VerticalAlignment.Middle);

            // Draw alert text
            spriteBatch.DrawStringOnCtrl(this, _Text, TimersModule.ModuleInstance.Resources.Font, _alertTextDestination, this.TextColor, true, true);
        }

        public void Dispose() {
            _shouldDispose = true;
            _animFade?.Resume();
            _animFade?.OnComplete(delegate {
                base.Dispose();
            });
        }
    }
}