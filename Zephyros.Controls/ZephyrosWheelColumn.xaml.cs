using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Zephyros.Controls
{
    public sealed partial class ZephyrosWheelColumn : UserControl
    {
        private const int VisibleCount = 9;
        private const int BufferItemsPerSide = 1;
        private const int RenderHalfRange = (VisibleCount / 2) + BufferItemsPerSide;
        private const double ItemHeight = 38.0;

        private const int StepAnimationMs = 50;
        private const double WheelNotchDelta = 120.0;
        private const double BaseImpulsePerNotch = 1.0;
        private const double RapidBoostSoft = 0.20;
        private const double RapidBoostHard = 0.45;
        private const double VelocityDecay = 0.86;
        private const double ResidualThreshold = 0.10;
        private const double MaxVelocity = 10.0;

        private List<string> _items = new();
        private int _selectedIndex;

        private bool _isAnimating;
        private double _velocity;
        private DateTime _lastWheelEventUtc = DateTime.MinValue;

        private bool _isHovered;

        public ZephyrosWheelColumn()
        {
            InitializeComponent();
            Loaded += ZephyrosWheelColumn_Loaded;
        }

        public event EventHandler<int>? SelectedIndexChanged;

        public IList<string> ItemsSource
        {
            get => (IList<string>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IList<string>),
                typeof(ZephyrosWheelColumn),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set => SetValue(SelectedIndexProperty, value);
        }

        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register(
                nameof(SelectedIndex),
                typeof(int),
                typeof(ZephyrosWheelColumn),
                new PropertyMetadata(0, OnSelectedIndexChanged));

        public bool IsCircular
        {
            get => (bool)GetValue(IsCircularProperty);
            set => SetValue(IsCircularProperty, value);
        }

        public static readonly DependencyProperty IsCircularProperty =
            DependencyProperty.Register(
                nameof(IsCircular),
                typeof(bool),
                typeof(ZephyrosWheelColumn),
                new PropertyMetadata(false));

        public TextAlignment ItemTextAlignment
        {
            get => (TextAlignment)GetValue(ItemTextAlignmentProperty);
            set => SetValue(ItemTextAlignmentProperty, value);
        }

        public static readonly DependencyProperty ItemTextAlignmentProperty =
            DependencyProperty.Register(
                nameof(ItemTextAlignment),
                typeof(TextAlignment),
                typeof(ZephyrosWheelColumn),
                new PropertyMetadata(TextAlignment.Left, OnAppearanceChanged));

        private void ZephyrosWheelColumn_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadItems();
            RenderItems();
            UpdateArrowVisibility();
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ZephyrosWheelColumn control)
                return;

            control.ReloadItems();
            control.CoerceSelectedIndex();
            control.RenderItems();
            control.UpdateArrowVisibility();
        }

        private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ZephyrosWheelColumn control)
                return;

            control.CoerceSelectedIndex();
            control.RenderItems();
            control.UpdateArrowVisibility();
            control.SelectedIndexChanged?.Invoke(control, control._selectedIndex);
        }

        private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ZephyrosWheelColumn control)
                return;

            control.RenderItems();
        }

        private void ReloadItems()
        {
            _items = ItemsSource?.ToList() ?? new List<string>();
        }

        private void CoerceSelectedIndex()
        {
            if (_items.Count == 0)
            {
                _selectedIndex = 0;
                return;
            }

            int index = SelectedIndex;

            if (IsCircular)
            {
                while (index < 0)
                    index += _items.Count;

                while (index >= _items.Count)
                    index -= _items.Count;
            }
            else
            {
                if (index < 0)
                    index = 0;

                if (index >= _items.Count)
                    index = _items.Count - 1;
            }

            _selectedIndex = index;

            if (SelectedIndex != index)
                SetValue(SelectedIndexProperty, index);
        }

        private void RenderItems()
        {
            if (ItemsHost == null)
                return;

            ItemsHost.Children.Clear();

            if (_items.Count == 0)
                return;

            Brush rowTransparentBrush = GetBrush("TransparentBrush", "CardBg");
            Brush textBrush = GetBrush("PrimaryForegroundBrush", "DatePickerTextBrush");

            for (int offset = -RenderHalfRange; offset <= RenderHalfRange; offset++)
            {
                int resolvedIndex = ResolveIndex(_selectedIndex + offset);
                string text = resolvedIndex >= 0 && resolvedIndex < _items.Count
                    ? _items[resolvedIndex]
                    : string.Empty;

                double opacity;
                if (offset == 0)
                    opacity = 1.0;
                else if (Math.Abs(offset) == 1)
                    opacity = 0.96;
                else if (Math.Abs(offset) == 2)
                    opacity = 0.88;
                else if (Math.Abs(offset) == 3)
                    opacity = 0.78;
                else if (Math.Abs(offset) == 4)
                    opacity = 0.62;
                else
                    opacity = 0.40;

                var rowBorder = new Border
                {
                    Height = ItemHeight,
                    Background = rowTransparentBrush,
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(4, 0, 4, 0),
                    Tag = resolvedIndex,
                    IsHitTestVisible = offset != 0 && resolvedIndex >= 0
                };

                if (offset != 0 && resolvedIndex >= 0)
                {
                    rowBorder.PointerEntered += RowBorder_PointerEntered;
                    rowBorder.PointerExited += RowBorder_PointerExited;
                    rowBorder.Tapped += RowBorder_Tapped;
                }

                var contentGrid = new Grid
                {
                    Height = ItemHeight,
                    VerticalAlignment = VerticalAlignment.Stretch
                };

                var tb = new TextBlock
                {
                    Text = text,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = ItemTextAlignment,
                    FontSize = 14,
                    Foreground = textBrush,
                    Opacity = opacity,
                    Margin = new Thickness(12, 0, 12, 0)
                };

                contentGrid.Children.Add(tb);
                rowBorder.Child = contentGrid;
                ItemsHost.Children.Add(rowBorder);
            }

            ItemsTranslate.Y = 0;
        }

        private int ResolveIndex(int index)
        {
            if (_items.Count == 0)
                return -1;

            if (IsCircular)
            {
                while (index < 0)
                    index += _items.Count;

                while (index >= _items.Count)
                    index -= _items.Count;

                return index;
            }

            if (index < 0 || index >= _items.Count)
                return -1;

            return index;
        }

        private async void RootGrid_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (_items.Count == 0)
                return;

            int delta = e.GetCurrentPoint(RootGrid).Properties.MouseWheelDelta;
            if (delta == 0)
                return;

            int direction = delta > 0 ? -1 : 1;
            double rawNotches = Math.Abs(delta) / WheelNotchDelta;
            if (rawNotches < 1.0)
                rawNotches = 1.0;

            double impulse = rawNotches * BaseImpulsePerNotch;

            var now = DateTime.UtcNow;
            if (_lastWheelEventUtc != DateTime.MinValue)
            {
                double ms = (now - _lastWheelEventUtc).TotalMilliseconds;

                if (ms < 45)
                    impulse += RapidBoostHard * rawNotches;
                else if (ms < 85)
                    impulse += RapidBoostSoft * rawNotches;
            }

            _lastWheelEventUtc = now;

            _velocity += direction * impulse;

            if (_velocity > MaxVelocity)
                _velocity = MaxVelocity;
            else if (_velocity < -MaxVelocity)
                _velocity = -MaxVelocity;

            e.Handled = true;

            if (_isAnimating)
                return;

            _isAnimating = true;
            await ProcessMomentumAsync();
            _isAnimating = false;
            UpdateArrowVisibility();
        }

        private async Task ProcessMomentumAsync()
        {
            while (Math.Abs(_velocity) > ResidualThreshold)
            {
                if (Math.Abs(_velocity) < 1.0)
                    break;

                int step = _velocity > 0 ? 1 : -1;

                if (!CanMove(step))
                {
                    _velocity = 0;
                    break;
                }

                await AnimateStepAsync(step);

                _velocity -= step;
                _velocity *= VelocityDecay;
            }

            _velocity = 0;
        }

        private bool CanMove(int step)
        {
            if (IsCircular)
                return true;

            if (_items.Count == 0)
                return false;

            if (_selectedIndex == 0 && step < 0)
                return false;

            if (_selectedIndex == _items.Count - 1 && step > 0)
                return false;

            return true;
        }

        private async Task AnimateStepAsync(int step)
        {
            double toY = step > 0 ? -ItemHeight : ItemHeight;

            var animation = new DoubleAnimation
            {
                To = toY,
                Duration = new Duration(TimeSpan.FromMilliseconds(StepAnimationMs)),
                EnableDependentAnimation = true,
                EasingFunction = new QuadraticEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, ItemsTranslate);
            Storyboard.SetTargetProperty(animation, "Y");

            var tcs = new TaskCompletionSource<bool>();

            void OnCompleted(object? s, object? e)
            {
                storyboard.Completed -= OnCompleted;
                tcs.TrySetResult(true);
            }

            storyboard.Completed += OnCompleted;
            storyboard.Begin();

            await tcs.Task;

            SelectedIndex = _selectedIndex + step;
            ItemsTranslate.Y = 0;
        }

        private async Task AnimateToIndexAsync(int targetIndex)
        {
            if (_items.Count == 0 || _isAnimating)
                return;

            int stepCount;

            if (IsCircular)
            {
                int forward = targetIndex - _selectedIndex;
                if (forward < 0)
                    forward += _items.Count;

                int backward = _selectedIndex - targetIndex;
                if (backward < 0)
                    backward += _items.Count;

                stepCount = forward <= backward ? forward : -backward;
            }
            else
            {
                stepCount = targetIndex - _selectedIndex;
            }

            if (stepCount == 0)
                return;

            _isAnimating = true;

            int direction = stepCount > 0 ? 1 : -1;
            int count = Math.Abs(stepCount);

            for (int i = 0; i < count; i++)
            {
                if (!CanMove(direction))
                    break;

                await AnimateStepAsync(direction);
            }

            _isAnimating = false;
            UpdateArrowVisibility();
        }

        private void RootGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _isHovered = true;
            UpdateArrowVisibility();
        }

        private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isHovered = false;
            ClearAllRowHoverBackgrounds();
            UpdateArrowVisibility();
        }

        private void UpdateArrowVisibility()
        {
            if (!_isHovered)
            {
                TopArrowButton.Visibility = Visibility.Collapsed;
                BottomArrowButton.Visibility = Visibility.Collapsed;
                return;
            }

            if (IsCircular)
            {
                TopArrowButton.Visibility = Visibility.Visible;
                BottomArrowButton.Visibility = Visibility.Visible;
                return;
            }

            bool canUp = _selectedIndex > 0;
            bool canDown = _selectedIndex < _items.Count - 1;

            TopArrowButton.Visibility = canUp ? Visibility.Visible : Visibility.Collapsed;
            BottomArrowButton.Visibility = canDown ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void TopArrowButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimating || !CanMove(-1))
                return;

            _isAnimating = true;
            await AnimateStepAsync(-1);
            _isAnimating = false;
            UpdateArrowVisibility();
        }

        private async void BottomArrowButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimating || !CanMove(1))
                return;

            _isAnimating = true;
            await AnimateStepAsync(1);
            _isAnimating = false;
            UpdateArrowVisibility();
        }

        private void RowBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border)
                border.Background = GetBrush("WheelItemHoverBackgroundBrush", "RowHoverBackground");
        }

        private void RowBorder_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border)
                border.Background = GetBrush("TransparentBrush", "CardBg");
        }

        private async void RowBorder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is not Border border)
                return;

            if (border.Tag is not int targetIndex || targetIndex < 0)
                return;

            await AnimateToIndexAsync(targetIndex);
        }

        private void ClearAllRowHoverBackgrounds()
        {
            Brush transparentBrush = GetBrush("TransparentBrush", "CardBg");

            foreach (var child in ItemsHost.Children)
            {
                if (child is Border border)
                    border.Background = transparentBrush;
            }
        }

        private Brush GetBrush(string primaryKey, string fallbackKey)
        {
            if (Application.Current?.Resources.TryGetValue(primaryKey, out object primary) == true &&
                primary is Brush primaryBrush)
            {
                return primaryBrush;
            }

            if (Application.Current?.Resources.TryGetValue(fallbackKey, out object fallback) == true &&
                fallback is Brush fallbackBrush)
            {
                return fallbackBrush;
            }

            if (Application.Current?.Resources.TryGetValue("PrimaryForegroundBrush", out object finalFallback) == true &&
                finalFallback is Brush finalFallbackBrush)
            {
                return finalFallbackBrush;
            }

            return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
        }
    }
}