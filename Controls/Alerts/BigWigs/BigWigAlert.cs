using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Charr.Timers_BlishHUD.Controls.BigWigs
{
    public class BigWigAlert : Control, IAlertPanel
    {

        private const int DEFAULT_WIDTH = 336;

        private const int ICON_SIZE = 32;
        private const int TIMER_SIZE = 48;

        private const int TOP_BORDER = 2;
        private const int BOTTOM_BORDER = 1;

        private float _maxFill;
        private float _currentFill;
        private string _text;

        public float MaxFill {
            get => _maxFill;
            set => SetProperty(ref _maxFill, value, true);
        }

        public float CurrentFill {
            get => _currentFill;
            set => SetProperty(ref _currentFill, value, true);
        }


        public string Text {
            get => _text;
            set {
                value = value.Replace("\n", " - ");
                SetProperty(ref _text, value, true);
            }
        }

        public string TimerText { get; set; }
        public Color TextColor { get; set; }
        public Color FillColor { get; set; }
        public Color TimerTextColor { get; set; }
        public bool ShouldShow { get; set; }
        public AsyncTexture2D Icon { get; set; }

        protected override CaptureType CapturesInput() {
            return CaptureType.None | CaptureType.DoNotBlock;
        }

        public BigWigAlert() {
            this.Size = new Point(DEFAULT_WIDTH, ICON_SIZE + TOP_BORDER + BOTTOM_BORDER);
            this.Icon = new AsyncTexture2D(ContentService.Textures.Error);
        }

        // BOUNDS
        private Rectangle _iconBounds = Rectangle.Empty;
        private Rectangle _progressBounds = Rectangle.Empty;
        private Rectangle _filledBounds = Rectangle.Empty;
        private Rectangle _timerBounds = Rectangle.Empty;

        public override void RecalculateLayout() {
            _iconBounds = new Rectangle(TOP_BORDER, TOP_BORDER, ICON_SIZE, ICON_SIZE);
            _timerBounds = new Rectangle(this.Width - TIMER_SIZE, 0, TIMER_SIZE, this.Height);

            int progressLeft = TOP_BORDER + ICON_SIZE + 1;
            int progressFill = (int)(_progressBounds.Width * (this.CurrentFill / this.MaxFill));
            _progressBounds = new Rectangle(progressLeft, TOP_BORDER, this.Width - progressLeft - 1, this.Height - TOP_BORDER - BOTTOM_BORDER);
            _filledBounds = new Rectangle(_progressBounds.X, _progressBounds.Y, TimersModule.ModuleInstance._alertFillDirection.Value
                                                                                    ? progressFill
                                                                                    : _progressBounds.Width - progressFill, _progressBounds.Height);

            base.RecalculateLayout();
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds) {
            if (!ShouldShow) return;

            // Background
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, bounds, Color.Black * 0.75f);

            // Icon
            spriteBatch.DrawOnCtrl(this, this.Icon, _iconBounds);

            // Progress Background
            spriteBatch.DrawOnCtrl(this, TimersModule.ModuleInstance.Resources.BigWigBackground, _progressBounds, _progressBounds.OffsetBy(TimersModule.ModuleInstance.Resources.BigWigBackground.Width / 3, 0), Color.White * 0.7f);
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, _filledBounds, this.FillColor * 0.45f);

            // Text
            spriteBatch.DrawStringOnCtrl(this, this.Text, GameService.Content.DefaultFont18, _progressBounds.OffsetBy(11, -1), Color.Black);
            spriteBatch.DrawStringOnCtrl(this, this.Text, GameService.Content.DefaultFont18, _progressBounds.OffsetBy(10, -2), this.TextColor);

            // Timer
            float remainingTime = this.MaxFill - this.CurrentFill;

            string timerFormat = "0";
            var timerColor = this.TextColor;

            switch (remainingTime) {
                case -1:
                case > 5:
                    spriteBatch.DrawStringOnCtrl(this, remainingTime.ToString(timerFormat), GameService.Content.DefaultFont18, _timerBounds.OffsetBy(1, 1), Color.Black,
                                                 false, HorizontalAlignment.Center);
                    spriteBatch.DrawStringOnCtrl(this, remainingTime.ToString(timerFormat), GameService.Content.DefaultFont18, _timerBounds, timerColor,
                                                 false, HorizontalAlignment.Center);
                    break;
                case > 0:
                    timerFormat = "0.0";
                    timerColor = this.TimerTextColor;
                    goto case -1;
                case <= 0:
                    break;
            }

            // Shine
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(_progressBounds.X, _progressBounds.Y, _progressBounds.Width, 1), Color.White * 0.25f);
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(_progressBounds.X, _progressBounds.Bottom - 1, _progressBounds.Width, 1), Color.White * 0.25f);
        }

    }
}
