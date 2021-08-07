using System;
using System.Diagnostics;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Charr.Timers_BlishHUD.Controls {
    // Effectively a copy of WindowBase with some features removed and added.

    class AlertWindow : Container {
        private const int TITLEBAR_HEIGHT = 32;
        private const int TITLE_OFFSET = 80;
        private const int MAX_EMBLEM_WIDTH = 80;
        private const int COMMON_MARGIN = 16;
        private const int SUBTITLE_OFFSET = 20;

        protected string _title = "No Title";

        public string Title {
            get => _title;
            set => SetProperty(ref _title, value, true);
        }

        protected string _subtitle = "";

        public string Subtitle {
            get => _subtitle;
            set {
                if (SetProperty(ref _subtitle, value)) {
                    RecalculateLayout();
                }
            }
        }

        protected Texture2D _emblem = null;

        public Texture2D Emblem {
            get => _emblem;
            set {
                if (SetProperty(ref _emblem, value)) {
                    RecalculateLayout();
                }
            }
        }

        protected bool _onlyShowChildren = false;

        public bool OnlyShowChildren {
            get => _onlyShowChildren;
            set => SetProperty(ref _onlyShowChildren, value);
        }

        protected bool Dragging = false;
        protected Point DragStart = Point.Zero;

        protected Rectangle _windowBackgroundBounds;
        protected Rectangle _titleBarBounds;
        protected Rectangle _emblemBounds;

        private Rectangle _layoutLeftTitleBarBounds;
        private Rectangle _layoutRightTitleBarBounds;
        private Rectangle _layoutSubtitleBounds;
        private Rectangle _layoutWindowCornerBounds;

        protected bool MouseOverTitleBar = false;

        public Texture2D WindowTitleBarLeft;
        public Texture2D WindowTitleBarRight;
        public Texture2D WindowTitleBarLeftActive;
        public Texture2D WindowTitleBarRightActive;
        public Texture2D WindowCorner;
        public Texture2D WindowBackground;

        public AlertWindow() {
            WindowTitleBarLeft = TimersModule.ModuleInstance.Resources.WindowTitleBarLeft;
            WindowTitleBarRight = TimersModule.ModuleInstance.Resources.WindowTitleBarRight;
            WindowTitleBarLeftActive = TimersModule.ModuleInstance.Resources.WindowTitleBarLeftActive;
            WindowTitleBarRightActive = TimersModule.ModuleInstance.Resources.WindowTitleBarRightActive;
            WindowCorner = TimersModule.ModuleInstance.Resources.WindowCorner;
            WindowBackground = TimersModule.ModuleInstance.Resources.WindowBackground;

            this.ZIndex = Screen.WINDOW_BASEZINDEX;

            Input.Mouse.LeftMouseButtonReleased += delegate { Dragging = false; };

            this.Padding = Thickness.Zero;
            _titleBarBounds = new Rectangle(0, 0, Width, TITLEBAR_HEIGHT);
            _windowBackgroundBounds = new Rectangle(0,
                0,
                Width,
                Height - TITLEBAR_HEIGHT);
        }

        public Rectangle ValidChildRegion() {
            return new Rectangle(0, Math.Max(_layoutLeftTitleBarBounds.Bottom, _emblemBounds.Bottom),
                this.ContentRegion.Width, this.ContentRegion.Bottom - Math.Max(_layoutLeftTitleBarBounds.Bottom, _emblemBounds.Bottom));
        }

        public override void RecalculateLayout() {
            if (_emblem != null) {
                float emblemHWRatio = _emblem.Height / _emblem.Width;
                float emblemWidth = Math.Max(_emblem.Width, MAX_EMBLEM_WIDTH);
                _emblemBounds = new Rectangle(0, 0, (int) emblemWidth, (int) (emblemWidth * emblemHWRatio));
            }
            else {
                _emblemBounds = Rectangle.Empty;
            }

            _titleBarBounds = new Rectangle(0, 0, Width, TITLEBAR_HEIGHT);

            // Align the title bar image with the top of the title bar bounds
            int titleBarDrawOffset = _titleBarBounds.Y -
                                     (WindowTitleBarLeft.Height / 2 -
                                      _titleBarBounds.Height / 2);
            // If there is an emblem, we place the title bar at the center of the emblem image
            titleBarDrawOffset += (_emblem != null) ? (_emblemBounds.Height / 2 - WindowTitleBarLeft.Height / 2) : 0;

            // Part of the right title bar image is transparent. Determine the actual width of the right title bar by subtracting it
            int titleBarRightWidth = WindowTitleBarRight.Width - COMMON_MARGIN;

            _layoutLeftTitleBarBounds = new Rectangle(
                _titleBarBounds.X,
                titleBarDrawOffset,
                _titleBarBounds.Right - titleBarRightWidth,
                WindowTitleBarLeft.Height);

            _layoutRightTitleBarBounds = new Rectangle(
                _titleBarBounds.Right - titleBarRightWidth,
                titleBarDrawOffset,
                WindowTitleBarRight.Width,
                WindowTitleBarRight.Height);

            // Title bar text bounds
            if (!string.IsNullOrEmpty(_title) && !string.IsNullOrEmpty(_subtitle)) {
                int titleTextWidth = (int) Content.DefaultFont32.MeasureString(_title).Width;
                int titleOffset = (_emblem == null) ? TITLE_OFFSET : _emblemBounds.Width + 5;
                _layoutSubtitleBounds =
                    _layoutLeftTitleBarBounds.OffsetBy(titleOffset + titleTextWidth + SUBTITLE_OFFSET, 0);
            }

            // Corner edge bounds
            _layoutWindowCornerBounds = new Rectangle(
                _layoutRightTitleBarBounds.Right - WindowCorner.Width - COMMON_MARGIN,
                this.ContentRegion.Bottom - WindowCorner.Height + COMMON_MARGIN,
                WindowCorner.Width,
                WindowCorner.Height);

            _windowBackgroundBounds = new Rectangle(0 - WindowBackground.Width/4,
                (_layoutLeftTitleBarBounds.Bottom) / 2,
                _titleBarBounds.Width + _layoutRightTitleBarBounds.Width + WindowBackground.Width / 4,
                this.ContentRegion.Bottom);
        }

        protected override void OnMouseMoved(MouseEventArgs e) {
            MouseOverTitleBar = false;
            if (this.RelativeMousePosition.Y < _layoutLeftTitleBarBounds.Bottom)
                MouseOverTitleBar = true;

            base.OnMouseMoved(e);
        }

        protected override void OnMouseLeft(MouseEventArgs e) {
            MouseOverTitleBar = false;
            base.OnMouseLeft(e);
        }

        protected override CaptureType CapturesInput() {
            if (OnlyShowChildren) {
                return CaptureType.None;
            }
            return CaptureType.Mouse | CaptureType.MouseWheel | CaptureType.Filter;
        }

        protected override void OnLeftMouseButtonPressed(MouseEventArgs e) {
            if (MouseOverTitleBar) {
                Dragging = true;
                DragStart = Input.Mouse.Position;
            }

            base.OnLeftMouseButtonPressed(e);
        }

        protected override void OnLeftMouseButtonReleased(MouseEventArgs e) {
            Dragging = false;
            base.OnLeftMouseButtonReleased(e);
        }

        public void ToggleWindow() {
            if (_visible) Hide();
            else Show();
        }

        public override void Show() {
            if (_visible) return;
            this.Location = new Point(
                Math.Max(0, _location.X),
                Math.Max(0, _location.Y));
            this.Opacity = 1;
            this.Visible = true;
        }

        public override void Hide() {
            if (!this.Visible) return;
            this.Opacity = 0;
            this.Visible = false;
        }

        public override void UpdateContainer(GameTime gameTime) {
            if (Dragging) {
                var nOffset = Input.Mouse.Position - DragStart;
                Location += nOffset;
                DragStart = Input.Mouse.Position;
            }
        }

        protected void PaintWindowBackground(SpriteBatch spriteBatch, Rectangle bounds) {
            spriteBatch.DrawOnCtrl(this,
                WindowBackground,
                bounds,
                null);
        }

        protected void PaintTitleBar(SpriteBatch spriteBatch, Rectangle bounds) {
            // Titlebar
            if (_mouseOver && MouseOverTitleBar) {
                spriteBatch.DrawOnCtrl(this, WindowTitleBarLeftActive, _layoutLeftTitleBarBounds);
                spriteBatch.DrawOnCtrl(this, WindowTitleBarLeftActive, _layoutLeftTitleBarBounds);
                spriteBatch.DrawOnCtrl(this, WindowTitleBarRightActive, _layoutRightTitleBarBounds);
                spriteBatch.DrawOnCtrl(this, WindowTitleBarRightActive, _layoutRightTitleBarBounds);
            }
            else {
                spriteBatch.DrawOnCtrl(this, WindowTitleBarLeft, _layoutLeftTitleBarBounds);
                spriteBatch.DrawOnCtrl(this, WindowTitleBarLeft, _layoutLeftTitleBarBounds);
                spriteBatch.DrawOnCtrl(this, WindowTitleBarRight, _layoutRightTitleBarBounds);
                spriteBatch.DrawOnCtrl(this, WindowTitleBarRight, _layoutRightTitleBarBounds);
            }

            // Title & Subtitle
            if (!string.IsNullOrEmpty(_title)) {
                int titleOffset = (_emblem == null) ? TITLE_OFFSET : _emblemBounds.Width + 5;
                spriteBatch.DrawStringOnCtrl(this,
                    _title,
                    Content.DefaultFont32,
                    _layoutLeftTitleBarBounds.OffsetBy(titleOffset, 0),
                    ContentService.Colors.ColonialWhite);

                if (!string.IsNullOrEmpty(_subtitle)) {
                    spriteBatch.DrawStringOnCtrl(this,
                        _subtitle,
                        Content.DefaultFont16,
                        _layoutSubtitleBounds,
                        ContentService.Colors.ColonialWhite);
                }
            }
        }

        protected void PaintEmblem(SpriteBatch spriteBatch, Rectangle bounds) {
            if (_emblem != null) {
                spriteBatch.DrawOnCtrl(this,
                    _emblem,
                    _emblemBounds);
            }
        }

        protected void PaintCorner(SpriteBatch spriteBatch, Rectangle bounds) {
            spriteBatch.DrawOnCtrl(this,
                WindowCorner,
                _layoutWindowCornerBounds);
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds) {
            if (_onlyShowChildren) {
                return;
            }

            PaintWindowBackground(spriteBatch, _windowBackgroundBounds);
            PaintTitleBar(spriteBatch, bounds);
        }

        public override void PaintAfterChildren(SpriteBatch spriteBatch, Rectangle bounds) {
            if (_onlyShowChildren) {
                return;
            }

            PaintEmblem(spriteBatch, bounds);
            PaintCorner(spriteBatch, bounds);
        }
    }
}