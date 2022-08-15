using Blish_HUD;
using Blish_HUD.Content;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Charr.Timers_BlishHUD.Controls.ResetButton
{
    class ResetButton : Container
    {
        bool _dragging;
        bool _resizing;
        Keys ModifierKey = Keys.LeftControl;

        private const int RESIZEHANDLE_SIZE = 16;
        protected Rectangle ResizeHandleBounds { get; private set; } = Rectangle.Empty;
        bool MouseOverResizeHandle;
        public Point MaxSize = new Point(499, 499);

        Point _resizeStart;
        Point _dragStart;
        Point _draggingStart;
        Rectangle _resizeCorner
        {
            get => new Rectangle(LocalBounds.Right - 15, LocalBounds.Bottom - 15, 15, 15);
        }
        public Color TintColor = Color.Black * 0.5f;
        public bool TintOnHover;

        //With v. 0.11.8 simplify using the commented
        //AsyncTexture2D _resizeTexture = AsyncTexture2D.FromAssetId(156009);
        //AsyncTexture2D _resizeTextureHovered = AsyncTexture2D.FromAssetId(156010);

        AsyncTexture2D _resizeTexture = TimersModule.ModuleInstance.ContentsManager.GetTexture(@"textures\156009.png");
        AsyncTexture2D _resizeTextureHovered = TimersModule.ModuleInstance.ContentsManager.GetTexture(@"textures\156010.png");
        StandardButton Button;
        Image ImageButton;
        public event EventHandler ButtonClicked;
        public event EventHandler BoundsChanged;

        public ResetButton()
        {
            Button = new StandardButton()
            {
                Location = new Point(2, 2),
                Text = "Reset Timer",
                Parent = this,
                BasicTooltipText = String.Format("Reset Active Timers" + Environment.NewLine + "Hold {0} to move and resize.", ModifierKey.ToString()),
                Visible = false,
            };

            ImageButton = new Image()
            {
                Location = new Point(2, 2),
                Parent = this,
                BasicTooltipText = String.Format("Reset Active Timers" + Environment.NewLine + "Hold {0} to move and resize.", ModifierKey.ToString()),
                Texture = TimersModule.ModuleInstance.ContentsManager.GetTexture(@"textures\resetbutton_big.png"),
            };

            ImageButton.Click += Button_Click;
            Button.Click += Button_Click;
        }

        private void Button_Click(object sender, MouseEventArgs e)
        {
            this.ButtonClicked?.Invoke(sender, e);
        }

        public void ToggleVisibility()
        {
            Visible = !Visible;
        }

        protected override void OnLeftMouseButtonReleased(MouseEventArgs e)
        {
            base.OnLeftMouseButtonReleased(e);
            _dragging = false;
            _resizing = false;
        }

        protected override void OnLeftMouseButtonPressed(MouseEventArgs e)
        {
            base.OnLeftMouseButtonPressed(e);

            if (MouseOver && Input.Keyboard.KeysDown.ToList().Contains(ModifierKey))
            {
                _resizing = _resizeCorner.Contains(e.MousePosition);
                _resizeStart = this.Size;
                _dragStart = Input.Mouse.Position;

                _dragging = !_resizing;
                _draggingStart = _dragging ? RelativeMousePosition : Point.Zero;
            }
        }

        public override void UpdateContainer(GameTime gameTime)
        {
            base.UpdateContainer(gameTime);

            _dragging = _dragging && MouseOver;
            _resizing = _resizing && MouseOver;
            MouseOverResizeHandle = MouseOverResizeHandle && MouseOver;

            if (_dragging)
            {
                this.Location = Input.Mouse.Position + new Point(-_draggingStart.X, -_draggingStart.Y);
                this.BoundsChanged?.Invoke(null, null);
            }

            if (_resizing)
            {
                var nOffset = Input.Mouse.Position - _dragStart;
                var newSize = _resizeStart + nOffset;
                var sqrSize = Math.Min(newSize.X, newSize.Y);

                this.Size = new Point(MathHelper.Clamp(sqrSize, 50, MaxSize.X), MathHelper.Clamp(sqrSize, 25, MaxSize.Y));
                this.BoundsChanged?.Invoke(null, null);
            }
        }

        protected virtual Point HandleWindowResize(Point newSize)
        {
            return new Point(MathHelper.Clamp(newSize.X, this.ContentRegion.X, 1024),
                             MathHelper.Clamp(newSize.Y, this.ContentRegion.Y, 1024));
        }

        protected override void OnMouseMoved(MouseEventArgs e)
        {
            ResetMouseRegionStates();

            if (this.ResizeHandleBounds.Contains(this.RelativeMousePosition)
                  && this.RelativeMousePosition.X > this.ResizeHandleBounds.Right - RESIZEHANDLE_SIZE
                  && this.RelativeMousePosition.Y > this.ResizeHandleBounds.Bottom - RESIZEHANDLE_SIZE)
            {
                this.MouseOverResizeHandle = true;
            }

            base.OnMouseMoved(e);
        }

        private void ResetMouseRegionStates()
        {
            this.MouseOverResizeHandle = false;
        }

        public override void RecalculateLayout()
        {
            base.RecalculateLayout();

            //With v. 0.11.8
            //this.ResizeHandleBounds = new Rectangle(this.Width - _resizeTexture.Width,
            //                                        this.Height - _resizeTexture.Height,
            //                                        _resizeTexture.Width,
            //                                        _resizeTexture.Height);

            this.ResizeHandleBounds = new Rectangle(this.Width - _resizeTexture.Texture.Width,
                                                    this.Height - _resizeTexture.Texture.Height,
                                                    _resizeTexture.Texture.Width,
                                                    _resizeTexture.Texture.Height);

            var sqSize = new Point(Math.Min(Width, Height) - 5, Math.Min(Width, Height) - 5);
            if (Button != null) this.Button.Size = this.Size - new Point(5, 5);
            if (ImageButton != null) this.ImageButton.Size = sqSize;
        }

        public override void PaintAfterChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            base.PaintAfterChildren(spriteBatch, bounds);

            if (MouseOver && Input.Keyboard.KeysDown.ToList().Contains(ModifierKey))
            {
                if (!TintOnHover || MouseOver)
                {
                    spriteBatch.DrawOnCtrl(this,
                            ContentService.Textures.Pixel,
                            bounds,
                            Rectangle.Empty,
                            TintColor,
                            0f,
                            default);
                }

                if (_resizeTexture != null)
                {
                    //With v. 0.11.8
                    //spriteBatch.DrawOnCtrl(this,
                    //        _resizing || MouseOverResizeHandle ? _resizeTextureHovered : _resizeTexture,
                    //        new Rectangle(bounds.Right - _resizeTexture.Width - 1, bounds.Bottom - _resizeTexture.Height - 1, _resizeTexture.Width, _resizeTexture.Height),
                    //        _resizeTexture.Bounds,
                    //       Color.White,
                    //        0f,
                    //       default);

                    spriteBatch.DrawOnCtrl(this,
                            _resizing || MouseOverResizeHandle ? _resizeTextureHovered : _resizeTexture,
                            new Rectangle(bounds.Right - _resizeTexture.Texture.Width - 1, bounds.Bottom - _resizeTexture.Texture.Height - 1, _resizeTexture.Texture.Width, _resizeTexture.Texture.Height),
                            _resizeTexture.Texture.Bounds,
                            Color.White,
                            0f,
                            default);
                }

                var color = MouseOver ? ContentService.Colors.ColonialWhite : Color.Transparent;

                //Top
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.Left, bounds.Top, bounds.Width, 2), Rectangle.Empty, color * 0.5f);
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.Left, bounds.Top, bounds.Width, 1), Rectangle.Empty, color * 0.6f);

                //Bottom
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.Left, bounds.Bottom - 2, bounds.Width, 2), Rectangle.Empty, color * 0.5f);
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.Left, bounds.Bottom - 1, bounds.Width, 1), Rectangle.Empty, color * 0.6f);

                //Left
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.Left, bounds.Top, 2, bounds.Height), Rectangle.Empty, color * 0.5f);
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.Left, bounds.Top, 1, bounds.Height), Rectangle.Empty, color * 0.6f);

                //Right
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.Right - 2, bounds.Top, 2, bounds.Height), Rectangle.Empty, color * 0.5f);
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(bounds.Right - 1, bounds.Top, 1, bounds.Height), Rectangle.Empty, color * 0.6f);
            }
        }
    }
}
