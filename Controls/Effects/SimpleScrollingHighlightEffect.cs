using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Charr.Timers_BlishHUD.Controls.Effects {

    /// <summary>
    /// Used to show the "scrolling" highlight used by many menu items and buttons throughout the game.
    /// Should be applied as <see cref="Control.EffectBehind"/>.
    /// </summary>
    public class SimpleScrollingHighlightEffect : ControlEffect {

        private const string SPARAM_MASK = "Mask";
        private const string SPARAM_OVERLAY = "Overlay";
        private const string SPARAM_ROLLER = "Roller";
        private const string SPARAM_OPACITY = "Opacity";
        private const float DEFAULT_ANIMATION_DURATION = 1.0f;

        private float _scrollRoller = 0;
        public float ScrollRoller {
            get => _scrollRoller;
            set {
                _scrollRoller = value;

                if (_forceActive) return;

                _scrollEffect.Parameters[SPARAM_ROLLER].SetValue(_scrollRoller);
            }
        }

        private float _duration = DEFAULT_ANIMATION_DURATION;
        /// <summary>
        /// The duration of the wipe effect when the mouse enters the control.
        /// </summary>
        public float Duration {
            get => _duration;
            set => _duration = value;
        }

        private bool _forceActive;
        /// <summary>
        /// If enabled, the effect will stay on full (used to show that the control or menu item is active).
        /// </summary>
        public bool ForceActive {
            get => _forceActive;
            set {
                _forceActive = value;

                if (_forceActive) {
                    _shaderAnim?.Cancel();

                    _scrollEffect.Parameters[SPARAM_ROLLER].SetValue(1f);
                }
            }
        }

        private readonly Effect _scrollEffect;

        private Glide.Tween _shaderAnim;
        private bool _active = false;

        public SimpleScrollingHighlightEffect(Control assignedControl) : base(assignedControl) {
            _scrollEffect = TimersModule.ModuleInstance.Resources.MasterScrollEffect.Clone();
            _scrollEffect.Parameters[SPARAM_MASK].SetValue(GameService.Content.GetTexture("156072"));
            _scrollEffect.Parameters[SPARAM_OVERLAY].SetValue(GameService.Content.GetTexture("156071"));
            _scrollEffect.Parameters[SPARAM_OPACITY].SetValue(assignedControl.Opacity);
        }

        protected override SpriteBatchParameters GetSpriteBatchParameters() {
            return new SpriteBatchParameters(SpriteSortMode.Immediate,
                                             BlendState.AlphaBlend,
                                             SamplerState.LinearWrap,
                                             null,
                                             null,
                                             _scrollEffect,
                                             GameService.Graphics.UIScaleTransform);
        }

        protected override void OnEnable() {
            if (!_enabled || _forceActive) return;

            _scrollEffect.Parameters[SPARAM_OPACITY].SetValue(this.AssignedControl.Opacity);

            this.ScrollRoller = 0f;

            _shaderAnim = GameService.Animation
                                     .Tweener
                                     .Tween(this,
                                            new { ScrollRoller = 1f },
                                            _duration);

            _active = true;
        }

        protected override void OnDisable() {
            _shaderAnim?.Cancel();
            _shaderAnim = null;

            this.ScrollRoller = 0;

            _active = false;
        }

        public override void PaintEffect(SpriteBatch spriteBatch, Rectangle bounds) {
            if (_active || _forceActive)
                spriteBatch.DrawOnCtrl(this.AssignedControl, ContentService.Textures.Pixel, bounds, Color.Transparent);
        }
    }
}
