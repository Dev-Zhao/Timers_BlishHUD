using Blish_HUD.Controls;
using Blish_HUD.Input;
using Glide;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using Blish_HUD;

namespace Charr.Timers_BlishHUD.Controls
{
    public enum AlertFlowDirection
    {
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop
    }

    public class AlertContainer : Panel
    {
        #region Properties

        public EventHandler<EventArgs> ContainerDragged;

        protected Vector2 _controlPadding = Vector2.Zero;
        public Vector2 ControlPadding {
            get => _controlPadding;
            set => SetProperty(ref _controlPadding, value, true);
        }

        protected Vector2 _outerControlPadding = Vector2.Zero;
        public Vector2 OuterControlPadding {
            get => _outerControlPadding;
            set => SetProperty(ref _outerControlPadding, value, true);
        }

        protected bool _padLeftBeforeControl = false;
        [Obsolete("Use OuterControlPadding instead.")]
        public bool PadLeftBeforeControl {
            get => _padLeftBeforeControl;
            set => SetProperty(ref _padLeftBeforeControl, value, true);
        }

        protected bool _padTopBeforeControl = false;
        [Obsolete("Use OuterControlPadding instead.")]
        public bool PadTopBeforeControl {
            get => _padTopBeforeControl;
            set => SetProperty(ref _padTopBeforeControl, value, true);
        }

        protected AlertFlowDirection _flowDirection = AlertFlowDirection.TopToBottom;
        /// <summary>
        /// The method / direction that should be used when flowing controls.
        /// </summary>
        public AlertFlowDirection FlowDirection {
            get => _flowDirection;
            set => SetProperty(ref _flowDirection, value, true);
        }

        protected bool _locationLock = false;
        public bool LocationLock {
            get => _locationLock;
            set {
                if (SetProperty(ref _locationLock, value)) {
                    BackgroundColor = (_locationLock) ? Color.Transparent : new Color(Color.Black, 0.3f);
                    BasicTooltipText = _locationLock ? "" : TOOLTIP_TEXT;
                }
            }
        }

        protected bool _mouseDragging = false;
        protected Point _dragStart = Point.Zero;

        private Dictionary<Control, Glide.Tween> _childTweens = new Dictionary<Control, Tween>();
        private const string TOOLTIP_TEXT = "Drag to move alert container.\nYou can lock it in place by going to the settings panel.";

        #endregion

        public AlertContainer() {
            GameService.Input.Mouse.LeftMouseButtonReleased += (sender, args) => {
                HandleLeftMouseButtonReleased(args);
            };

            TimersModule.ModuleInstance._alertContainerLocationSetting.SettingChanged += (sender, args) => {
                Location = args.NewValue;
            };

            TimersModule.ModuleInstance._hideAlertsSetting.SettingChanged += (sender, args) => {
                if (!TimersModule.ModuleInstance._hideAlertsSetting.Value && _children.Count > 0) {
                    this.Show();
                }
                else {
                    this.Hide();
                }
            };

            BasicTooltipText = _locationLock ? "" : TOOLTIP_TEXT;
            Visible = false;
        }

        #region Child Handling
        protected override void OnChildAdded(ChildChangedEventArgs e) {
            OnChildrenChanged(e.ResultingChildren);
            base.OnChildAdded(e);

            e.ChangedChild.Resized += ChangedChildOnResized;

            float outerPadX = _padLeftBeforeControl ? _controlPadding.X : _outerControlPadding.X;
            float outerPadY = _padTopBeforeControl ? _controlPadding.Y : _outerControlPadding.Y;

            // Place the child at the desired position with an offset (based on its width / height).
            // This way new children added have an animation moving to its actual desired position.
            e.ChangedChild.Top = (int)outerPadY;
            e.ChangedChild.Left = (int)outerPadX;
            switch (FlowDirection) {
                case AlertFlowDirection.LeftToRight:
                    e.ChangedChild.Left = (int)(this.Width + e.ChangedChild.Width);
                    break;
                case AlertFlowDirection.RightToLeft:
                    e.ChangedChild.Right = (int)(-e.ChangedChild.Width);
                    break;
                case AlertFlowDirection.TopToBottom:
                    e.ChangedChild.Top = (int)(this.Height + e.ChangedChild.Height);
                    break;
                case AlertFlowDirection.BottomToTop:
                    e.ChangedChild.Bottom = (int)(-e.ChangedChild.Height);
                    break;
            }
        }

        protected override void OnChildRemoved(ChildChangedEventArgs e) {
            OnChildrenChanged(e.ResultingChildren);
            base.OnChildRemoved(e);

            e.ChangedChild.Resized -= ChangedChildOnResized;
        }

        private void ChangedChildOnResized(object sender, ResizedEventArgs e) {
            OnChildrenChanged(_children.ToArray());
        }

        private void OnChildrenChanged(IEnumerable<Control> resultingChildren) {
            //if (this.IsLayoutSuspended) {
            //Invalidate();
            //}
            //else {
            RecalculateLayout();
            //}
        }

        /// <summary>
        /// Filters children of the flow panel by setting those
        /// that don't match the provided filter function to be
        /// not visible.
        /// </summary>
        public void FilterChildren<TControl>(Func<TControl, bool> filter) where TControl : Control {
            _children.Cast<TControl>().ToList().ForEach(tc => tc.Visible = filter(tc));
            this.Invalidate();
        }

        /// <summary>
        /// Sorts children of the flow panel using the provided
        /// comparison function.
        /// </summary>
        /// <typeparam name="TControl"></typeparam>
        /// <param name="comparison"></param>
        public void SortChildren<TControl>(Comparison<TControl> comparison) where TControl : Control {
            var tempChildren = _children.Cast<TControl>().ToList();
            tempChildren.Sort(comparison);

            _children = new ControlCollection<Control>(tempChildren);

            this.Invalidate();
        }

        private void UpdateSizeToFitChildren() {
            if (_children.Count == 0) {
                return;
            }

            float outerPadX = _padLeftBeforeControl ? _controlPadding.X : _outerControlPadding.X;
            float outerPadY = _padTopBeforeControl ? _controlPadding.Y : _outerControlPadding.Y;

            int newWidth = (int)(_children.Count * _children[0].Width + (_children.Count - 1) * _controlPadding.X + outerPadX * 2);
            int newHeight = (int)(_children.Count * _children[0].Height + (_children.Count - 1) * _controlPadding.Y + outerPadY * 2);

            var previousSize = this.Size;
            if (FlowDirection == AlertFlowDirection.LeftToRight ||
                FlowDirection == AlertFlowDirection.RightToLeft) {
                Width = newWidth;
                Height = (int)(_children[0].Height + outerPadY * 2);
            }
            else if (FlowDirection == AlertFlowDirection.TopToBottom ||
                     FlowDirection == AlertFlowDirection.BottomToTop) {
                Width = (int)(_children[0].Width + outerPadX * 2);
                Height = newHeight;
            }

            // Restore the location that the container should be at, which can change when the container resizes
            switch (TimersModule.ModuleInstance._alertDisplayOrientationSetting.Value) {
                case AlertFlowDirection.LeftToRight:
                case AlertFlowDirection.TopToBottom:
                    Location = TimersModule.ModuleInstance._alertContainerLocationSetting.Value;
                    break;
                case AlertFlowDirection.RightToLeft:
                    this.Right = TimersModule.ModuleInstance._alertContainerLocationSetting.Value.X + TimersModule.ModuleInstance._alertContainerSizeSetting.Value.X;
                    this.Top = TimersModule.ModuleInstance._alertContainerLocationSetting.Value.Y;
                    break;
                case AlertFlowDirection.BottomToTop:
                    this.Left = TimersModule.ModuleInstance._alertContainerLocationSetting.Value.X;
                    this.Bottom = TimersModule.ModuleInstance._alertContainerLocationSetting.Value.Y + TimersModule.ModuleInstance._alertContainerSizeSetting.Value.Y;
                    break;
            }

            // Restore the location that the children should be at, which can change when the container resizes
            if (FlowDirection == AlertFlowDirection.RightToLeft) {
                foreach (var child in _children) {
                    child.Right += Size.X - previousSize.X;
                }
            }
            else if (FlowDirection == AlertFlowDirection.BottomToTop) {
                foreach (var child in _children) {
                    child.Bottom += Size.Y - previousSize.Y;
                }
            }
        }

        private void ReflowChildLayout(Control[] allChildren) {
            float outerPadX = _padLeftBeforeControl ? _controlPadding.X : _outerControlPadding.X;
            float outerPadY = _padTopBeforeControl ? _controlPadding.Y : _outerControlPadding.Y;

            int directionX = 0, directionY = 0;
            if (FlowDirection == AlertFlowDirection.LeftToRight ||
                FlowDirection == AlertFlowDirection.RightToLeft) {
                directionX = FlowDirection == AlertFlowDirection.LeftToRight ? 1 : -1;
            }
            if (FlowDirection == AlertFlowDirection.TopToBottom ||
                FlowDirection == AlertFlowDirection.BottomToTop) {
                directionY = FlowDirection == AlertFlowDirection.TopToBottom ? 1 : -1;
            }

            // Initial location to place children depends on the flow direction and outer padding.
            float startLocationX = FlowDirection != AlertFlowDirection.RightToLeft ? outerPadX : this.Width - outerPadX;
            float startLocationY = FlowDirection != AlertFlowDirection.BottomToTop ? outerPadY : this.Height - outerPadY;

            float nextLocationX = startLocationX;
            float nextLocationY = startLocationY;

            foreach (var child in allChildren) {
                // Cancel existing tweens
                Glide.Tween childTween;
                if (_childTweens.TryGetValue(child, out childTween)) {
                    childTween.Cancel();
                    _childTweens.Remove(child);
                }

                // Tween the child moving to the desired location
                object tweenValue = new { };
                switch (FlowDirection) {
                    case AlertFlowDirection.LeftToRight:
                        tweenValue = new {
                            Left = (int)nextLocationX
                        };
                        child.Top = (int)nextLocationY;
                        break;
                    case AlertFlowDirection.RightToLeft:
                        tweenValue = new {
                            Right = (int)nextLocationX
                        };
                        child.Top = (int)nextLocationY;
                        break;
                    case AlertFlowDirection.TopToBottom:
                        tweenValue = new {
                            Top = (int)nextLocationY
                        };
                        child.Left = (int)nextLocationX;
                        break;
                    case AlertFlowDirection.BottomToTop:
                        tweenValue = new {
                            Bottom = (int)nextLocationY
                        };
                        child.Left = (int)nextLocationX;
                        break;
                }
                childTween = Animation.Tweener.Tween(child,
                    tweenValue,
                    TimersModule.ModuleInstance._alertMoveDelaySetting.Value,
                    0);
                childTween.OnComplete(() => {
                    _childTweens.Remove(child);
                });
                _childTweens.Add(child, childTween);

                // Update location to where the next child should be placed
                nextLocationX += (child.Width + _controlPadding.X) * directionX;
                nextLocationY += (child.Height + _controlPadding.Y) * directionY;
            }
        }

        #endregion

        public override void RecalculateLayout() {
            if (_children.Count == 0 || TimersModule.ModuleInstance._hideAlertsSetting.Value) {
                this.Hide();
            }
            else {
                this.Show();
            }

            UpdateSizeToFitChildren();
            ReflowChildLayout(_children.ToArray());

            base.RecalculateLayout();
        }

        #region Mouse Handling
        protected override CaptureType CapturesInput() {
            if (LocationLock || !_visible) {
                return CaptureType.None | CaptureType.DoNotBlock;
            }

            return base.CapturesInput();
        }

        protected override void OnLeftMouseButtonPressed(MouseEventArgs e) {
            if (!LocationLock) {
                _dragStart = this.RelativeMousePosition;
                _mouseDragging = true;
            }
            base.OnLeftMouseButtonPressed(e);
        }

        private void HandleLeftMouseButtonReleased(MouseEventArgs e) {
            if (TimersModule.ModuleInstance == null) {
                return;
            }

            if (_mouseDragging) {
                TimersModule.ModuleInstance._alertContainerLocationSetting.Value = Location;
                TimersModule.ModuleInstance._alertContainerSizeSetting.Value = Size;

                _mouseDragging = false;
            }

            ContainerDragged?.Invoke(this, EventArgs.Empty);
        }

        public override void UpdateContainer(GameTime gameTime) {
            if (_mouseDragging) {
                var newLocation = Input.Mouse.Position - _dragStart;
                Location = newLocation;
                _dragStart = this.RelativeMousePosition;
            }
        }

        #endregion
    }
}