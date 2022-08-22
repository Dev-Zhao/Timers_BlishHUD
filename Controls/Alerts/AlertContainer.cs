using Blish_HUD.Controls;
using Blish_HUD.Input;
using Glide;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
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

        protected bool _lock = false;
        public bool Lock {
            get => _lock;
            set {
                if (SetProperty(ref _lock, value)) {
                    BackgroundColor = (_lock) ? Color.Transparent : new Color(Color.Black, 0.3f);
                }
            }
        }

        protected bool _mouseDragging = false;
        protected Point _dragStart = Point.Zero;

        private Dictionary<Control, Glide.Tween> _childTweens = new Dictionary<Control, Tween>();


        #endregion

        public AlertContainer() {
            GameService.Input.Mouse.LeftMouseButtonReleased += (sender, args) => {
                HandleLeftMouseButtonReleased(args);
            };

            TimersModule.ModuleInstance._alertContainerLocationSetting.SettingChanged += (sender, args) => {
                if (Location != args.NewValue) {
                    Location = args.NewValue;
                }
            };

            this.Resized += (sender, args) => { HandleResized(args); };
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
            ReflowChildLayout(resultingChildren.ToArray());
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
            float outerPadX = _padLeftBeforeControl ? _controlPadding.X : _outerControlPadding.X;
            float outerPadY = _padTopBeforeControl ? _controlPadding.Y : _outerControlPadding.Y;

            int newWidth = (int)(_children.Count * _children[0].Width + (_children.Count - 1) * _controlPadding.X + outerPadX * 2);
            int newHeight = (int)(_children.Count * _children[0].Height + (_children.Count - 1) * _controlPadding.Y + outerPadY * 2);

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
            if (_children.Count == 0) {
                return;
            }

            UpdateSizeToFitChildren();
            ReflowChildLayout(_children.ToArray());

            base.RecalculateLayout();
        }

        protected void HandleResized(ResizedEventArgs args) {
            // Restore the location that the container should be at, which can change when the container resizes
            switch (TimersModule.ModuleInstance._alertDisplayOrientationSetting.Value) {
                case AlertFlowDirection.LeftToRight:
                case AlertFlowDirection.TopToBottom:
                    this.Left = TimersModule.ModuleInstance._alertContainerLocationSetting.Value.X;
                    this.Top = TimersModule.ModuleInstance._alertContainerLocationSetting.Value.Y;
                    break;
                case AlertFlowDirection.RightToLeft:
                    this.Right = TimersModule.ModuleInstance._alertContainerLocationSetting.Value.X;
                    this.Top = TimersModule.ModuleInstance._alertContainerLocationSetting.Value.Y;
                    break;
                case AlertFlowDirection.BottomToTop:
                    this.Left = TimersModule.ModuleInstance._alertContainerLocationSetting.Value.X;
                    this.Bottom = TimersModule.ModuleInstance._alertContainerLocationSetting.Value.Y;
                    break;
            }

            // Restore the location that the children should be at, which can change when the container resizes
            if (FlowDirection == AlertFlowDirection.RightToLeft) {
                foreach (var child in _children) {
                    child.Right += args.CurrentSize.X - args.PreviousSize.X;
                }
            } else if (FlowDirection == AlertFlowDirection.BottomToTop) {
                foreach (var child in _children) {
                    child.Bottom += args.CurrentSize.Y - args.PreviousSize.Y;
                }
            }
        }

        #region Mouse Handling
        protected override CaptureType CapturesInput() {
            if (Lock || !Visible) {
                return CaptureType.None | CaptureType.DoNotBlock;
            }

            return base.CapturesInput();
        }

        protected override void OnLeftMouseButtonPressed(MouseEventArgs e) {
            if (!Lock) {
                _dragStart = this.RelativeMousePosition;
                _mouseDragging = true;
            }
            base.OnLeftMouseButtonPressed(e);
        }

        public void HandleLeftMouseButtonReleased(MouseEventArgs e) {
            if (TimersModule.ModuleInstance == null) {
                return;
            }

            if (_mouseDragging) {
                switch (TimersModule.ModuleInstance._alertDisplayOrientationSetting.Value) {
                    case AlertFlowDirection.LeftToRight:
                    case AlertFlowDirection.TopToBottom:
                        TimersModule.ModuleInstance._alertContainerLocationSetting.Value = this.Location;
                        break;
                    case AlertFlowDirection.RightToLeft:
                        TimersModule.ModuleInstance._alertContainerLocationSetting.Value = new Point(this.Right, this.Top);
                        break;
                    case AlertFlowDirection.BottomToTop:
                        TimersModule.ModuleInstance._alertContainerLocationSetting.Value = new Point(this.Left, this.Bottom);
                        break;
                }

                _mouseDragging = false;
            }

            ContainerDragged?.Invoke(this, EventArgs.Empty);
        }

        public override void UpdateContainer(GameTime gameTime) {
            if (_mouseDragging) {
                var newLocation = Input.Mouse.Position - _dragStart;
                var offset = newLocation - Location;
                Location = newLocation;
                _dragStart = this.RelativeMousePosition;
            }
        }

        #endregion
    }
}