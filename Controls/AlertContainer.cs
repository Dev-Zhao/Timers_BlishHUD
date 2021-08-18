using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Glide;
using Microsoft.Xna.Framework;

namespace Charr.Timers_BlishHUD.Controls {
    public class AlertContainer : Panel {
        protected bool _lock = false;

        public bool Lock {
            get => _lock;
            set {
                if (SetProperty(ref _lock, value)) {
                    BackgroundColor = (_lock) ? Color.Transparent : new Color(Color.Black, 0.3f);
                }
            }
        }

        protected bool Dragging = false;
        protected Point DragStart = Point.Zero;
        protected bool MouseOverPanel = false;

        private int _newHeight;
        private int _newWidth;
        private Point _anchor = Point.Zero;
        private Glide.Tween _animSizeChange;

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

        protected ControlFlowDirection _flowDirection = ControlFlowDirection.LeftToRight;

        /// <summary>
        /// The method / direction that should be used when flowing controls.
        /// </summary>
        public ControlFlowDirection FlowDirection {
            get => _flowDirection;
            set => SetProperty(ref _flowDirection, value, true);
        }

        protected override void OnChildAdded(ChildChangedEventArgs e) {
            base.OnChildAdded(e);
            OnChildrenChanged(e);

            e.ChangedChild.Resized += ChangedChildOnResized;
        }

        protected override void OnChildRemoved(ChildChangedEventArgs e) {
            base.OnChildRemoved(e);
            OnChildrenChanged(e);

            e.ChangedChild.Resized -= ChangedChildOnResized;
        }

        private void ChangedChildOnResized(object sender, ResizedEventArgs e) {
            ReflowChildLayout(_children.ToArray());
        }

        private void OnChildrenChanged(ChildChangedEventArgs e) {
            ReflowChildLayout(e.ResultingChildren.ToArray());
        }

        public override void RecalculateLayout() {
            ReflowChildLayout(_children.ToArray());

            base.RecalculateLayout();
        }

        /// <summary>
        /// Filters children of the flow panel by setting those
        /// that don't match the provided filter function to be
        /// not visible.
        /// </summary>
        public void FilterChildren<TControl>(Func<TControl, bool> filter) where TControl : Control {
            _children.Cast<TControl>().ToList().ForEach(tc => tc.Visible = filter(tc));
            ReflowChildLayout(_children.ToArray());
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

            _children.Select(_children.Remove);

            _children.AddRange(tempChildren);

            ReflowChildLayout(_children.ToArray());
        }

        private void ReflowChildLayoutLeftToRight(IEnumerable<Control> allChildren) {
            float outerPadX = _padLeftBeforeControl ? _controlPadding.X : _outerControlPadding.X;
            float outerPadY = _padTopBeforeControl ? _controlPadding.Y : _outerControlPadding.Y;

            float nextBottom = outerPadY;
            float currentBottom = outerPadY;
            float lastRight = outerPadX;

            foreach (var child in allChildren.Where(c => c.Visible)) {
                // Need to flow over to the next row
                if (child.Width >= this.Width - lastRight) {
                    currentBottom = nextBottom + _controlPadding.Y;
                    lastRight = outerPadX;
                }

                child.Location = new Point((int) lastRight, (int) currentBottom);

                lastRight = child.Right + _controlPadding.X;

                // Ensure rows don't overlap
                nextBottom = Math.Max(nextBottom, child.Bottom);
            }
        }

        private void ReflowChildLayoutRightToLeft(IEnumerable<Control> allChildren) {
            float outerPadX = _padLeftBeforeControl ? _controlPadding.X : _outerControlPadding.X;
            float outerPadY = _padTopBeforeControl ? _controlPadding.Y : _outerControlPadding.Y;

            float nextBottom = outerPadY;
            float currentBottom = outerPadY;
            float lastLeft = (_anchor == Point.Zero) ? Width - outerPadX : _anchor.X - outerPadX;

            foreach (var child in allChildren.Where(c => c.Visible)) {
                // Need to flow over to the next row
                if (outerPadX > lastLeft - child.Width) {
                    currentBottom = nextBottom + _controlPadding.Y;
                    lastLeft = this.Width - outerPadX;
                }

                child.Location = new Point((int) (lastLeft - child.Width), (int) currentBottom);

                lastLeft = child.Left - _controlPadding.X;

                // Ensure rows don't overlap
                nextBottom = Math.Max(nextBottom, child.Bottom);
            }
        }

        private void ReflowChildLayoutTopToBottom(IEnumerable<Control> allChildren) {
            float outerPadX = _padLeftBeforeControl ? _controlPadding.X : _outerControlPadding.X;
            float outerPadY = _padTopBeforeControl ? _controlPadding.Y : _outerControlPadding.Y;

            float nextRight = outerPadX;
            float currentRight = outerPadX;
            float lastBottom = outerPadY;

            foreach (var child in allChildren.Where(c => c.Visible)) {
                // Need to flow over to the next column
                if (child.Height >= this.Height - lastBottom) {
                    currentRight = nextRight + _controlPadding.X;
                    lastBottom = outerPadY;
                }

                child.Location = new Point((int) currentRight, (int) lastBottom);

                lastBottom = child.Bottom + _controlPadding.Y;

                // Ensure columns don't overlap
                nextRight = Math.Max(nextRight, child.Right);
            }
        }

        private void ReflowChildLayoutBottomToTop(IEnumerable<Control> allChildren) {
            float outerPadX = _padLeftBeforeControl ? _controlPadding.X : _outerControlPadding.X;
            float outerPadY = _padTopBeforeControl ? _controlPadding.Y : _outerControlPadding.Y;

            float nextRight = outerPadX;
            float currentRight = outerPadX;
            float lastTop = (_anchor == Point.Zero) ? this.Height - outerPadY : _anchor.Y - outerPadY;

            foreach (var child in allChildren.Where(c => c.Visible)) {
                // Need to flow over to the next column
                if (outerPadY > lastTop - child.Height) {
                    currentRight = nextRight + _controlPadding.X;
                    lastTop = this.Height - outerPadY;
                }

                child.Location = new Point((int) currentRight, (int) (lastTop - child.Height));

                lastTop = child.Top - _controlPadding.Y;

                // Ensure columns don't overlap
                nextRight = Math.Max(nextRight, child.Right);
            }
        }

        private Dictionary<Control, Glide.Tween> _animMoves = new Dictionary<Control, Tween>();
        private Dictionary<Control, Point> _newLocations = new Dictionary<Control, Point>();
        private Dictionary<Control, Point> _previousLocations = new Dictionary<Control, Point>();

        private void ReflowChildLayoutSingleLeftToRight(IEnumerable<Control> allChildren) {
            float outerPadX = _padLeftBeforeControl ? _controlPadding.X : _outerControlPadding.X;
            float outerPadY = _padTopBeforeControl ? _controlPadding.Y : _outerControlPadding.Y;

            var lastLeft = outerPadX;

            foreach (var child in allChildren) {
                if (!_newLocations.ContainsKey(child)) {
                    _newLocations.Add(child, new Point((int)lastLeft, (int)outerPadY));
                    _previousLocations.Add(child, Point.Zero);
                }
                else {
                    _previousLocations[child] = _newLocations[child];
                    _newLocations[child] = new Point((int)lastLeft, (int)outerPadY);
                }
                lastLeft = _newLocations[child].X + child.Width + _controlPadding.X;
            }

            foreach (var child in allChildren) {
                if (noAnim) {
                    child.Location = _newLocations[child];
                    _animMoves.Clear();
                }
                else if (_previousLocations[child] != _newLocations[child]) {
                    if (child.Location.X > _newLocations[child].X) {
                        Glide.Tween animMove;
                        if (_animMoves.TryGetValue(child, out animMove)) {
                            animMove.Cancel();
                            _animMoves.Remove(child);
                        }

                        animMove = Animation.Tweener.Tween(child,
                            new { Location = _newLocations[child] },
                            TimersModule.ModuleInstance._alertFadeDelaySetting.Value,
                            0).Ease(Ease.CubeInOut);
                        animMove.OnComplete(() => { _animMoves.Remove(child); });
                        _animMoves.Add(child, animMove);
                    }
                    else {
                        child.Location = _newLocations[child];
                    }
                }
            }

            canChangeSize = true;
        }

        private void ReflowChildLayoutSingleRightToLeft(IEnumerable<Control> allChildren) {
            float outerPadX = _padLeftBeforeControl ? _controlPadding.X : _outerControlPadding.X;
            float outerPadY = _padTopBeforeControl ? _controlPadding.Y : _outerControlPadding.Y;

            if (SizeChanging) {
                int offset = 0;
                foreach (var child in allChildren) {
                    if (child == allChildren.First()) {
                        offset = _newLocations[child].X - (Width - (int)outerPadX);
                    }
                    child.Right = _newLocations[child].X - offset;
                }
                return;
            }

            var lastRight = Width - outerPadX;
            foreach (var child in allChildren) {
                if (!_newLocations.ContainsKey(child)) {
                    _newLocations.Add(child, new Point((int)(lastRight), (int)outerPadY));
                    _previousLocations.Add(child, child.Location);
                }
                else {
                    _previousLocations[child] = _newLocations[child];
                    _newLocations[child] = new Point((int)(lastRight), (int)outerPadY);
                }

                lastRight = _newLocations[child].X - child.Width - _controlPadding.X;
            }

            foreach (var child in allChildren) {
                Glide.Tween animMove;
                if (noAnim) {
                    child.Location = new Point(child.Location.X, _newLocations[child].Y);
                    child.Right = _newLocations[child].X;
                    _animMoves.Clear();
                }
                else {
                    if (_animMoves.TryGetValue(child, out animMove)) {
                        animMove.Cancel();
                        _animMoves.Remove(child);
                    }
                    //child.Location = new Point(child.Location.X, _newLocations[child].Y);
                    animMove = Animation.Tweener.Tween(child,
                        new {
                            Right = _newLocations[child].X
                        },
                        TimersModule.ModuleInstance._alertFadeDelaySetting.Value,
                        0).Ease(Ease.CubeInOut);
                    animMove.OnComplete(() => {
                        _animMoves.Remove(child);
                        child.Right = _newLocations[child].X;
                    });
                    _animMoves.Add(child, animMove);
                }
            }

            canChangeSize = true;
        }

        private void ReflowChildLayoutSingleTopToBottom(IEnumerable<Control> allChildren) {
            float outerPadX = _padLeftBeforeControl ? _controlPadding.X : _outerControlPadding.X;
            float outerPadY = _padTopBeforeControl ? _controlPadding.Y : _outerControlPadding.Y;

            var lastBottom = outerPadY;

            foreach (var child in allChildren) {
                if (!_newLocations.ContainsKey(child)) {
                    _newLocations.Add(child, new Point((int)outerPadX, (int)lastBottom));
                    _previousLocations.Add(child, Point.Zero);
                }
                else {
                    _previousLocations[child] = _newLocations[child];
                    _newLocations[child] = new Point((int)outerPadX, (int)lastBottom);
                }

                lastBottom = _newLocations[child].Y + child.Height + _controlPadding.Y;
            }

            foreach (var child in allChildren) {
                if (noAnim) {
                    child.Location = _newLocations[child];
                    _animMoves.Clear();
                }
                else if (_previousLocations[child] != _newLocations[child]) {
                    if (child.Location.Y > _newLocations[child].Y) {
                        Glide.Tween animMove;
                        if (_animMoves.TryGetValue(child, out animMove)) {
                            animMove.Cancel();
                            _animMoves.Remove(child);
                        }

                        animMove = Animation.Tweener.Tween(child,
                            new { Location = _newLocations[child] },
                            TimersModule.ModuleInstance._alertFadeDelaySetting.Value,
                            0).Ease(Ease.CubeInOut);
                        animMove.OnComplete(() => { _animMoves.Remove(child); });
                        _animMoves.Add(child, animMove);
                    }
                    else {
                        child.Location = _newLocations[child];
                    }
                }
            }

            canChangeSize = true;
        }

        private void ReflowChildLayoutSingleBottomToTop(IEnumerable<Control> allChildren) {
            float outerPadX = _padLeftBeforeControl ? _controlPadding.X : _outerControlPadding.X;
            float outerPadY = _padTopBeforeControl ? _controlPadding.Y : _outerControlPadding.Y;

            if (SizeChanging) {
                int offset = 0;
                foreach (var child in allChildren) {
                    if (child == allChildren.First()) {
                        offset = _newLocations[child].Y - (Height - (int)outerPadY);
                    }
                    child.Bottom = _newLocations[child].Y - offset;
                }
                return;
            }

            var lastBottom = Height - outerPadY;

            foreach (var child in allChildren) {
                if (!_newLocations.ContainsKey(child)) {
                    _newLocations.Add(child, new Point((int)outerPadX, (int)lastBottom));
                    _previousLocations.Add(child, child.Location);
                }
                else {
                    _previousLocations[child] = _newLocations[child];
                    _newLocations[child] = new Point((int)outerPadX, (int)lastBottom);
                }

                lastBottom = _newLocations[child].Y - child.Height - _controlPadding.Y;
            }

            foreach (var child in allChildren) {
                Glide.Tween animMove;
                if (noAnim) {
                    child.Location = new Point(_newLocations[child].X, child.Location.Y);
                    child.Bottom = _newLocations[child].Y;
                    _animMoves.Clear();
                }
                else {
                    if (_animMoves.TryGetValue(child, out animMove)) {
                        animMove.Cancel();
                        _animMoves.Remove(child);
                    }
                    //child.Location = new Point(_newLocations[child].X, child.Location.Y);
                    animMove = Animation.Tweener.Tween(child,
                        new {
                            Bottom = _newLocations[child].Y
                        },
                        TimersModule.ModuleInstance._alertFadeDelaySetting.Value,
                        0).Ease(Ease.CubeInOut);
                    animMove.OnComplete(() => {
                        _animMoves.Remove(child);
                        child.Bottom = _newLocations[child].Y;
                    });
                    _animMoves.Add(child, animMove);
                }
            }

            canChangeSize = true;
        }

        private void ReflowChildLayout(Control[] allChildren) {
            var filteredChildren = allChildren.Where(c => c.GetType() != typeof(Scrollbar) && c.Visible);
            switch (_flowDirection) {
                case ControlFlowDirection.LeftToRight:
                    ReflowChildLayoutLeftToRight(filteredChildren);
                    break;
                case ControlFlowDirection.RightToLeft:
                    ReflowChildLayoutRightToLeft(filteredChildren);
                    break;
                case ControlFlowDirection.TopToBottom:
                    ReflowChildLayoutTopToBottom(filteredChildren);
                    break;
                case ControlFlowDirection.BottomToTop:
                    ReflowChildLayoutBottomToTop(filteredChildren);
                    break;
                case ControlFlowDirection.SingleLeftToRight:
                    ReflowChildLayoutSingleLeftToRight(filteredChildren);
                    break;
                case ControlFlowDirection.SingleRightToLeft:
                    ReflowChildLayoutSingleRightToLeft(filteredChildren);
                    break;
                case ControlFlowDirection.SingleTopToBottom:
                    ReflowChildLayoutSingleTopToBottom(filteredChildren);
                    break;
                case ControlFlowDirection.SingleBottomToTop:
                    ReflowChildLayoutSingleBottomToTop(filteredChildren);
                    break;
            }
        }

        private bool canChangeSize = false;

        public AlertContainer() : base() {
            Input.Mouse.LeftMouseButtonReleased += delegate { Dragging = false; };
            _animSizeChange = null;
            ChildAdded += delegate {
                canChangeSize = false;
                _animSizeChange?.Cancel();
                UpdateDisplay();
            };
            ChildRemoved += delegate { canChangeSize = false; _animSizeChange?.Cancel(); UpdateDisplay(); };
        }

        protected override CaptureType CapturesInput() {
            if (Lock || !Visible) {
                return CaptureType.None;
            }

            return CaptureType.Mouse | CaptureType.MouseWheel | CaptureType.Filter;
        }

        protected override void OnMouseMoved(MouseEventArgs e) {
            MouseOverPanel = false;
            if (this.RelativeMousePosition.Y < this.ContentRegion.Bottom)
                MouseOverPanel = true;

            base.OnMouseMoved(e);
        }

        protected override void OnMouseLeft(MouseEventArgs e) {
            MouseOverPanel = false;
            base.OnMouseLeft(e);
        }

        protected override void OnLeftMouseButtonPressed(MouseEventArgs e) {
            if (MouseOverPanel) {
                Dragging = true;
                DragStart = Input.Mouse.Position;
            }

            base.OnLeftMouseButtonPressed(e);
        }

        protected override void OnLeftMouseButtonReleased(MouseEventArgs e) {
            Dragging = false;
            base.OnLeftMouseButtonReleased(e);
        }

        public int GetChildrenMaxHeight() {
            int maxHeight = 0;
            foreach (Control child in Children) {
                if (child.Height > maxHeight &&
                    child.Height <= TimersModule.ModuleInstance.Resources.MAX_ALERT_HEIGHT) {
                    maxHeight = child.Height;
                }
            }

            return maxHeight;
        }

        public int GetChildrenMaxWidth() {
            int maxWidth = 0;
            foreach (Control child in Children) {
                if (child.Width > maxWidth && child.Width <= TimersModule.ModuleInstance.Resources.MAX_ALERT_WIDTH) {
                    maxWidth = child.Width;
                }
            }

            return maxWidth;
        }

        private ControlFlowDirection _previousFlowDirection = ControlFlowDirection.LeftToRight;
        private bool noAnim = false;
        private bool SizeChanging = false;
        public bool AutoShow = false;

        public void UpdateDisplay() {
            if (FlowDirection == ControlFlowDirection.TopToBottom) {
                FlowDirection = ControlFlowDirection.SingleTopToBottom;
            }
            else if (FlowDirection == ControlFlowDirection.BottomToTop) {
                FlowDirection = ControlFlowDirection.SingleBottomToTop;
            }
            else if (FlowDirection == ControlFlowDirection.LeftToRight) {
                FlowDirection = ControlFlowDirection.SingleLeftToRight;
            }
            else if (FlowDirection == ControlFlowDirection.RightToLeft) {
                FlowDirection = ControlFlowDirection.SingleRightToLeft;
            }

            if (_previousFlowDirection != FlowDirection) {
                switch (FlowDirection) {
                    case ControlFlowDirection.SingleLeftToRight:
                    case ControlFlowDirection.SingleTopToBottom:
                        _anchor = Point.Zero;
                        break;
                    case ControlFlowDirection.SingleRightToLeft:
                        _anchor = new Point(Right, Location.Y);
                        break;
                    case ControlFlowDirection.SingleBottomToTop:
                        _anchor = new Point(Location.X, Bottom);
                        break;
                }
                SizeChanging = false;
                noAnim = true;
                _newLocations.Clear();
                _previousLocations.Clear();
                _animMoves.Clear();
                ReflowChildLayout(Children.ToArray());
            }
            _previousFlowDirection = FlowDirection;

            int maxHeight = GetChildrenMaxHeight();
            int maxWidth = GetChildrenMaxWidth();
            int previousHeight = _newHeight;
            int previousWidth = _newWidth;
            _newHeight = Height;
            _newWidth = Width;
            switch (FlowDirection) {
                case ControlFlowDirection.SingleTopToBottom:
                case ControlFlowDirection.SingleBottomToTop:
                    _newHeight = maxHeight * Children.Count + (int) ControlPadding.Y * (Children.Count + 1);
                    _newWidth = maxWidth + (int) ControlPadding.X * 2;
                    break;
                case ControlFlowDirection.SingleLeftToRight:
                case ControlFlowDirection.SingleRightToLeft:
                    _newHeight = maxHeight + (int) ControlPadding.Y * 2;
                    _newWidth = maxWidth * Children.Count + (int) ControlPadding.X * (Children.Count + 1);
                    break;
            }

            /*
            if (Height < _newHeight) {
                Height = _newHeight;
                SizeChanging = false;
                noAnim = true;
                ReflowChildLayout(Children.ToList());
            } else if (Width < _newWidth) {
                Width = _newWidth;
                noAnim = true;
                SizeChanging = false;
                ReflowChildLayout(Children.ToList());
            }*/

            if (_anchor != Point.Zero) {
                if (FlowDirection == ControlFlowDirection.SingleRightToLeft) {
                    Right = _anchor.X;
                }
                else if (FlowDirection == ControlFlowDirection.SingleBottomToTop) {
                    Bottom = _anchor.Y;
                }
            }

            if (!canChangeSize) {
                SizeChanging = false;
                ReflowChildLayout(Children.ToArray());
            }
            else if (previousWidth != _newWidth || previousHeight != _newHeight) {
                _animSizeChange?.Cancel();
                switch (FlowDirection) {
                    case ControlFlowDirection.SingleTopToBottom:
                    case ControlFlowDirection.SingleBottomToTop:
                        Width = _newWidth;
                        SizeChanging = false;
                        noAnim = false;
                        ReflowChildLayout(Children.ToArray());
                        _animSizeChange = Animation.Tweener.Tween(this,
                                new { Height = _newHeight },
                                TimersModule.ModuleInstance._alertMoveDelaySetting.Value,
                                (previousHeight < _newHeight) ? 0 : TimersModule.ModuleInstance._alertMoveDelaySetting.Value).Ease(Ease.CubeInOut);
                        break;
                    case ControlFlowDirection.SingleLeftToRight:
                    case ControlFlowDirection.SingleRightToLeft:
                        Height = _newHeight;
                        SizeChanging = false;
                        noAnim = false;
                        ReflowChildLayout(Children.ToArray());
                        _animSizeChange = Animation.Tweener.Tween(this,
                            new { Width = _newWidth },
                            TimersModule.ModuleInstance._alertMoveDelaySetting.Value,
                            (previousWidth < _newWidth) ? 0 : TimersModule.ModuleInstance._alertMoveDelaySetting.Value).Ease(Ease.CubeInOut);
                        break;
                }

                _animSizeChange?.OnBegin(() => {
                    if (!canChangeSize) {
                        _animSizeChange.Cancel();
                        _animSizeChange = null;
                    }
                    SizeChanging = true;
                });

                _animSizeChange?.OnUpdate(() => {
                    if (!canChangeSize) {
                        _animSizeChange.Cancel();
                        _animSizeChange = null;
                    }
                    SizeChanging = true;
                    ReflowChildLayout(Children.ToArray());
                });

                _animSizeChange?.OnComplete(() => {
                    _animSizeChange = null;
                    if (FlowDirection == ControlFlowDirection.SingleRightToLeft) {
                        Right = _anchor.X;
                    }
                    else if (FlowDirection == ControlFlowDirection.SingleBottomToTop) {
                        Bottom = _anchor.Y;
                    }

                    SizeChanging = false;
                    ReflowChildLayout(Children.ToArray());
                    ContainerDragged?.Invoke(this, EventArgs.Empty);
                });
            }

            if (Children.Count == 0) {
                Hide();
                _animSizeChange?.Cancel();
                _animSizeChange = null;
                Height = 0;
                Width = 0;
            }
            else if (AutoShow) {
                Show();
            }
        }

        public override void UpdateContainer(GameTime gameTime) {
            if (Dragging) {
                var nOffset = Input.Mouse.Position - DragStart;
                Location += nOffset;
                DragStart = Input.Mouse.Position;
                if (_anchor != Point.Zero) {
                    if (FlowDirection == ControlFlowDirection.SingleRightToLeft ||
                        FlowDirection == ControlFlowDirection.SingleBottomToTop) {
                        _anchor += nOffset;
                    }
                }

                ContainerDragged?.Invoke(this, EventArgs.Empty);
            }
            UpdateDisplay();
        }
    }
}