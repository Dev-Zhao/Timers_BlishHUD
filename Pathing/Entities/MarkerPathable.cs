using System;
using Blish_HUD;
using Blish_HUD.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Charr.Timers_BlishHUD.Pathing.Entities
{

    public enum BillboardVerticalConstraint
    {
        CameraPosition,
        PlayerPosition,
    }

    public class MarkerPathable : IEntity
    {

        private static readonly Logger Logger = Logger.GetLogger<MarkerPathable>();

        #region Load Static

        private static readonly MarkerEffect _sharedMarkerEffect;
        private static readonly Texture2D _fadeTexture;

        static MarkerPathable()
        {
            _sharedMarkerEffect = new MarkerEffect(GameService.Content.ContentManager.Load<Effect>(@"effects\marker"));
            _fadeTexture = TimersModule.ModuleInstance.Resources.TextureFade;
        }

        #endregion

        private VertexPositionTexture[] _verts;
        private Texture2D _texture;

        private BillboardVerticalConstraint _verticalConstraint = BillboardVerticalConstraint.CameraPosition;
        private Vector2 _size = Vector2.One;
        private float _scale = 1f;
        private float _fadeNear = 5000;
        private float _fadeFar = 5000;
        private float _playerFadeRadius = 0.25f;
        private bool _fadeCenter = true;
        private bool _autoResize = true;
        private Color _tintColor = Color.White;

        /// <summary>
        /// If set to true, the <see cref="Size"/> will automatically
        /// update if a new <see cref="Texture"/> is set.
        /// </summary>
        public bool AutoResize
        {
            get => _autoResize;
            set => SetProperty(ref _autoResize, value);
        }

        public BillboardVerticalConstraint VerticalConstraint
        {
            get => _verticalConstraint;
            set => SetProperty(ref _verticalConstraint, value);
        }

        public Vector2 Size
        {
            get => _size;
            set
            {
                if (SetProperty(ref _size, value))
                    RecalculateSize(_size, _scale);
            }
        }

        /// <summary>
        /// Scales the render size of the <see cref="Billboard"/>.
        /// </summary>
        public float Scale
        {
            get => _scale;
            set
            {
                if (SetProperty(ref _scale, value))
                    RecalculateSize(_size, _scale);
            }
        }

        public float FadeNear
        {
            get => Math.Min(_fadeNear, _fadeFar);
            set => SetProperty(ref _fadeNear, value);
        }

        public float FadeFar
        {
            get => Math.Max(_fadeNear, _fadeFar);
            set => SetProperty(ref _fadeFar, value);
        }

        public bool FadeCenter
        {
            get => _fadeCenter;
            set => SetProperty(ref _fadeCenter, value);
        }

        public float PlayerFadeRadius
        {
            get => _playerFadeRadius;
            set => SetProperty(ref _playerFadeRadius, value);
        }

        public Color TintColor
        {
            get => _tintColor;
            set => SetProperty(ref _tintColor, value);
        }

        public Texture2D Texture
        {
            get => _texture;
            set
            {
                if (SetProperty(ref _texture, value) && _texture != null)
                {
                    this.VerticalConstraint = _texture.Height == _texture.Width
                                                  ? BillboardVerticalConstraint.CameraPosition
                                                  : BillboardVerticalConstraint.PlayerPosition;

                    if (_autoResize)
                    {
                        this.Size = new Vector2(WorldUtil.GameToWorldCoord(_texture.Width),
                                                WorldUtil.GameToWorldCoord(_texture.Height));
                    }
                }
            }
        }

        public bool ShouldShow { get; set; }

        private DynamicVertexBuffer _vertexBuffer;

        public MarkerPathable() : this(null, Vector3.Zero, Vector2.Zero) { /* NOOP */ }

        public MarkerPathable(Texture2D texture) : this(texture, Vector3.Zero) { /* NOOP */ }

        public MarkerPathable(Texture2D texture, Vector3 position) : this(texture, position, Vector2.Zero) { /* NOOP */ }

        public MarkerPathable(Texture2D texture, Vector3 position, Vector2 size)
        {
            Initialize();

            _autoResize = (size == Vector2.Zero);
            this.Position = position;
            this.Size = size;
            this.Texture = texture;

            //GameService.Input.MouseMoved += InputOnMouseMoved;
        }

        private void Initialize()
        {
            _verts = new VertexPositionTexture[4];
            _vertexBuffer = new DynamicVertexBuffer(GameService.Graphics.GraphicsDevice, typeof(VertexPositionTexture), 4, BufferUsage.WriteOnly);
        }

        private void RecalculateSize(Vector2 newSize, float scale)
        {
            _verts[0] = new VertexPositionTexture(new Vector3(0, 0, 0), new Vector2(1, 1));
            _verts[1] = new VertexPositionTexture(new Vector3(newSize.X * scale, 0, 0), new Vector2(0, 1));
            _verts[2] = new VertexPositionTexture(new Vector3(0, newSize.Y * scale, 0), new Vector2(1, 0));
            _verts[3] = new VertexPositionTexture(new Vector3(newSize.X * scale, newSize.Y * scale, 0), new Vector2(0, 0));

            _vertexBuffer.SetData(_verts);
        }

        /// <inheritdoc />
        public override void HandleRebuild(GraphicsDevice graphicsDevice)
        {
            RecalculateSize(_size, _scale);
        }

        public override void Draw(GraphicsDevice graphicsDevice) {
            if (!Visible || !ShouldShow) return;
            if (_texture == null) return;

            var modelMatrix = Matrix.CreateTranslation(_size.X / -2, _size.Y / -2, 0)
                            * Matrix.CreateScale(_scale);

            if (this.Rotation == Vector3.Zero)
            {
                modelMatrix *= Matrix.CreateBillboard(this.Position + this.RenderOffset,
                                                      new Vector3(GameService.Gw2Mumble.PlayerCamera.Position.X,
                                                                  GameService.Gw2Mumble.PlayerCamera.Position.Y,
                                                                  _verticalConstraint == BillboardVerticalConstraint.CameraPosition
                                                                      ? GameService.Gw2Mumble.PlayerCamera.Position.Z
                                                                      : GameService.Gw2Mumble.PlayerCharacter.Position.Z),
                                                      new Vector3(0, 0, 1),
                                                      GameService.Gw2Mumble.PlayerCamera.Forward);
            }
            else
            {
                modelMatrix *= Matrix.CreateRotationX(MathHelper.ToRadians(this.Rotation.X))
                             * Matrix.CreateRotationY(MathHelper.ToRadians(this.Rotation.Y))
                             * Matrix.CreateRotationZ(MathHelper.ToRadians(this.Rotation.Z))
                             * Matrix.CreateTranslation(this.Position + this.RenderOffset);
            }

            _sharedMarkerEffect.SetEntityState(modelMatrix,
                                               _texture,
                                               _opacity,
                                               _fadeNear,
                                               _fadeFar,
                                               _playerFadeRadius,
                                               _fadeCenter,
                                               _fadeTexture,
                                               _tintColor);

            graphicsDevice.SetVertexBuffer(_vertexBuffer);

            foreach (var pass in _sharedMarkerEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }
        }

        private bool _mouseOver = false;

    }
}