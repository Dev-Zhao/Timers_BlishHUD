using Blish_HUD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Blish_HUD.Content;

namespace Charr.Timers_BlishHUD.Pathing.Entities
{
    public class TrailPathable : Entity
    {
        public Vector3 PointA { get; set; }
        public Vector3 PointB { get; set; }
        public AsyncTexture2D TrailTexture { get; set; }
        public float AnimationSpeed { get; set; } = 1f;
        public float FadeNear { get; set; } = 5000;
        public float FadeFar { get; set; } = 5000;
        public int TrailResolution { get; set; } = 20;
        public Color TintColor { get; set; } = Color.White;
        public float TrailWidth { get; set; } = 20 * 0.0254f;
        public float FadeRadius { get; set; } = 1f;
        public bool ShouldShow { get; set; }

        private VertexPositionColorTexture[] _vertexData;
        private VertexBuffer _vertexBuffer;

        public float TrailLength => Vector3.Distance(this.PointA, this.PointB);

        #region Load Static

        private static readonly TrailEffect _sharedTrailEffect;
        private static readonly Texture2D _fadeTexture;

        static TrailPathable()
        {
            _sharedTrailEffect = new TrailEffect(GameService.Content.ContentManager.Load<Effect>("effects\\trail"));
            _fadeTexture = TimersModule.ModuleInstance.Resources.TextureFade;
        }

        #endregion

        public override void HandleRebuild(GraphicsDevice graphicsDevice) { /* NOOP */ }

        private static BuildTrailWithResolution(List<Vector3> points, float pointResolution)
        {
            if (!points.Any())
            {
                return new List<Vector3>(0);
            }

            List<Vector3> tempPoints = new List<Vector3>();

            var lstPoint = points[0];

            for (int i = 0; i<points.Count; i++)
            {
                var dist = Vector3.Distance(lstPoint, points[i]);

                var s   = dist / pointResolution;
                var inc = 1    / s;

                for (float v = inc; v <s - inc; v += inc)
                {
                    var nPoint = Vector3.Lerp(lstPoint, points[i], v / s);

                    tempPoints.Add(nPoint);
                }

                tempPoints.Add(points[i]);

                lstPoint = points[i];
            }

            return tempPoints;
        }

        // TODO: When the time comes, this update method has plenty of places where easy optimizations can be made.
        public override void Update(GameTime gameTime) {
            if (!Visible || !ShouldShow) return;

            List <Vector3> trailPoints = BuildTrailWithResolution(new List<Vector3>(new Vector3[] { PointA, PointB }), this.TrailResolution);

            _vertexData = new VertexPositionColorTexture[trailPoints.Count * 2];

            float pastDistance = this.TrailLength;

            var offsetDirection = new Vector3(0, 0, -1);

            var currPoint = trailPoints[0];
            var offset = Vector3.Zero;

            for (int i = 0; i < trailPoints.Count - 1; i++)
            {
                var nextPoint = trailPoints[i + 1];

                var pathDirection = nextPoint - currPoint;

                offset = Vector3.Cross(pathDirection, offsetDirection);

                offset.Normalize();

                var leftPoint = currPoint + (offset * this.TrailWidth);
                var rightPoint = currPoint + (offset * -this.TrailWidth);
                float f = pastDistance / (this.TrailWidth * 2) - 1;

                _vertexData[i * 2 + 1] = new VertexPositionColorTexture(leftPoint, Color.White, new Vector2(0f, pastDistance / (this.TrailWidth * 2) - 1));
                _vertexData[i * 2] = new VertexPositionColorTexture(rightPoint, Color.White, new Vector2(1f, pastDistance / (this.TrailWidth * 2) - 1));

                pastDistance -= Vector3.Distance(currPoint, nextPoint);

                currPoint = nextPoint;
            }
            
            var fleftPoint = currPoint + (offset * this.TrailWidth);
            var frightPoint = currPoint + (offset * -this.TrailWidth);

            _vertexData[trailPoints.Count * 2 - 1] = new VertexPositionColorTexture(fleftPoint, Color.White, new Vector2(0f, pastDistance / (this.TrailWidth * 4) - 1));
            _vertexData[trailPoints.Count * 2 - 2] = new VertexPositionColorTexture(frightPoint, Color.White, new Vector2(1f, pastDistance / (this.TrailWidth * 4) - 1));


            _vertexBuffer?.Dispose();
            _vertexBuffer = new VertexBuffer(GameService.Graphics.GraphicsDevice, VertexPositionColorTexture.VertexDeclaration, _vertexData.Length, BufferUsage.WriteOnly);
            _vertexBuffer.SetData(_vertexData);
        }

        public override void Draw(GraphicsDevice graphicsDevice) {
            if (!Visible || !ShouldShow) return;
            if (this.TrailTexture == null || _vertexData == null || _vertexData.Length < 3) return;
            
            _sharedTrailEffect.SetEntityState(this.TrailTexture,
                                   this.AnimationSpeed,
                                   this.FadeNear,
                                   this.FadeFar,
                                   this.Opacity,
                                   this.FadeRadius,
                                   true,
                                   _fadeTexture,
                                   this.TintColor);

            graphicsDevice.SetVertexBuffer(_vertexBuffer, 0);

            foreach (EffectPass trailPass in _sharedTrailEffect.CurrentTechnique.Passes)
            {
                trailPass.Apply();

                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, _vertexBuffer.VertexCount - 2);
            }
        }

    }
}