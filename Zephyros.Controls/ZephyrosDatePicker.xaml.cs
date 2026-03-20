using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.System;

namespace Zephyros.Controls
{
    public sealed partial class ZephyrosDatePicker : UserControl
    {
        private readonly List<string> _months = new()
        {
            "January",
            "February",
            "March",
            "April",
            "May",
            "June",
            "July",
            "August",
            "September",
            "October",
            "November",
            "December"
        };

        private readonly List<int> _years = new();
        private readonly List<int> _days = new();

        private bool _isInternalUpdate;
        private bool _isClosingAnimated;
        private DateTime? _previousDateBeforeOpen;

        public ZephyrosDatePicker()
        {
            InitializeComponent();

            BuildYears();
            Loaded += ZephyrosDatePicker_Loaded;

            PointerEntered += ZephyrosDatePicker_PointerEntered;
            PointerExited += ZephyrosDatePicker_PointerExited;
        }

        public event EventHandler<DateTime?>? SelectedDateChanged;

        public DateTime? SelectedDate
        {
            get => (DateTime?)GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }

        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register(
                nameof(SelectedDate),
                typeof(DateTime?),
                typeof(ZephyrosDatePicker),
                new PropertyMetadata(null, OnSelectedDateChanged));

        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        public static readonly DependencyProperty PlaceholderTextProperty =
            DependencyProperty.Register(
                nameof(PlaceholderText),
                typeof(string),
                typeof(ZephyrosDatePicker),
                new PropertyMetadata("month / day / year", OnPlaceholderTextChanged));

        public int StartYear
        {
            get => (int)GetValue(StartYearProperty);
            set => SetValue(StartYearProperty, value);
        }

        public static readonly DependencyProperty StartYearProperty =
            DependencyProperty.Register(
                nameof(StartYear),
                typeof(int),
                typeof(ZephyrosDatePicker),
                new PropertyMetadata(2020, OnYearRangeChanged));

        public int EndYear
        {
            get => (int)GetValue(EndYearProperty);
            set => SetValue(EndYearProperty, value);
        }

        public static readonly DependencyProperty EndYearProperty =
            DependencyProperty.Register(
                nameof(EndYear),
                typeof(int),
                typeof(ZephyrosDatePicker),
                new PropertyMetadata(2040, OnYearRangeChanged));

        public CornerRadius PickerCornerRadius
        {
            get => (CornerRadius)GetValue(PickerCornerRadiusProperty);
            set => SetValue(PickerCornerRadiusProperty, value);
        }

        public static readonly DependencyProperty PickerCornerRadiusProperty =
            DependencyProperty.Register(
                nameof(PickerCornerRadius),
                typeof(CornerRadius),
                typeof(ZephyrosDatePicker),
                new PropertyMetadata(new CornerRadius(6), OnCornerRadiusChanged));

        public DateTime? MinDate
        {
            get => (DateTime?)GetValue(MinDateProperty);
            set => SetValue(MinDateProperty, value);
        }

        public static readonly DependencyProperty MinDateProperty =
            DependencyProperty.Register(
                nameof(MinDate),
                typeof(DateTime?),
                typeof(ZephyrosDatePicker),
                new PropertyMetadata(null, OnMinDateChanged));

        private void ZephyrosDatePicker_Loaded(object sender, RoutedEventArgs e)
        {
            if (OuterBorder != null)
                OuterBorder.CornerRadius = PickerCornerRadius;

            ApplyWheelOrderByCulture();
            WireWheelEvents();
            InitializeWheels();
            UpdateDisplayText();
        }

        private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ZephyrosDatePicker picker)
                return;

            picker.CoerceSelectedDateToRange();
            picker.UpdateDisplayText();
            picker.InitializeWheels();
            picker.SelectedDateChanged?.Invoke(picker, picker.SelectedDate);
        }

        private static void OnPlaceholderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZephyrosDatePicker picker)
                picker.UpdateDisplayText();
        }

        private static void OnYearRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ZephyrosDatePicker picker)
                return;

            picker.BuildYears();
            picker.CoerceSelectedDateToRange();
            picker.InitializeWheels();
        }

        private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZephyrosDatePicker picker && picker.OuterBorder != null)
                picker.OuterBorder.CornerRadius = picker.PickerCornerRadius;
        }

        private static void OnMinDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ZephyrosDatePicker picker)
                return;

            picker.CoerceSelectedDateToRange();
            picker.InitializeWheels();
            picker.UpdateDisplayText();
        }

        private void WireWheelEvents()
        {
            MonthWheel.SelectedIndexChanged -= MonthWheel_SelectedIndexChanged;
            DayWheel.SelectedIndexChanged -= DayWheel_SelectedIndexChanged;
            YearWheel.SelectedIndexChanged -= YearWheel_SelectedIndexChanged;

            MonthWheel.SelectedIndexChanged += MonthWheel_SelectedIndexChanged;
            DayWheel.SelectedIndexChanged += DayWheel_SelectedIndexChanged;
            YearWheel.SelectedIndexChanged += YearWheel_SelectedIndexChanged;
        }

        private void BuildYears()
        {
            _years.Clear();

            int start = StartYear;
            int end = EndYear;

            if (end < start)
                (start, end) = (end, start);

            for (int year = start; year <= end; year++)
                _years.Add(year);
        }

        private void BuildDays(int year, int month)
        {
            _days.Clear();

            int count = DateTime.DaysInMonth(year, month);
            for (int day = 1; day <= count; day++)
                _days.Add(day);
        }

        private void InitializeWheels()
        {
            if (MonthWheel == null || DayWheel == null || YearWheel == null)
                return;

            _isInternalUpdate = true;

            DateTime baseDate = SelectedDate ?? DateTime.Today;
            baseDate = ClampToRange(baseDate);

            BuildDays(baseDate.Year, baseDate.Month);

            MonthWheel.ItemsSource = _months;
            MonthWheel.SelectedIndex = baseDate.Month - 1;

            DayWheel.ItemsSource = _days.Select(x => x.ToString()).ToList();
            DayWheel.SelectedIndex = Math.Max(0, baseDate.Day - 1);

            YearWheel.ItemsSource = _years.Select(x => x.ToString()).ToList();
            YearWheel.SelectedIndex = Math.Max(0, _years.IndexOf(baseDate.Year));

            _isInternalUpdate = false;
        }

        private void UpdateDisplayText()
        {
            if (DisplayTextBlock == null)
                return;

            if (SelectedDate.HasValue)
            {
                DisplayTextBlock.Text = FormatDisplayDate(SelectedDate.Value);
                DisplayTextBlock.Foreground = GetBrush("DatePickerTextBrush", "PrimaryForegroundBrush");
            }
            else
            {
                string placeholder = PlaceholderText;

                if (string.IsNullOrWhiteSpace(placeholder) ||
                    string.Equals(placeholder, "month / day / year", StringComparison.OrdinalIgnoreCase))
                {
                    placeholder = BuildLocalizedPlaceholder();
                }

                DisplayTextBlock.Text = placeholder;
                DisplayTextBlock.Foreground = GetBrush("DatePickerPlaceholderBrush", "MutedText");
            }

            if (CalendarIcon != null)
                CalendarIcon.Foreground = GetBrush("DatePickerIconBrush", "MutedText");
        }

        private string BuildLocalizedPlaceholder()
        {
            string[] order = GetDatePartsOrder();

            if (order.Length == 3)
                return $"{order[0]} / {order[1]} / {order[2]}";

            return "day / month / year";
        }

        private string FormatDisplayDate(DateTime date)
        {
            string monthName = _months[date.Month - 1];
            string day = date.Day.ToString("00", CultureInfo.InvariantCulture);
            string year = date.Year.ToString(CultureInfo.InvariantCulture);

            string[] order = GetDatePartsOrder();

            if (order.Length == 3)
            {
                var values = new Dictionary<string, string>
                {
                    ["day"] = day,
                    ["month"] = monthName,
                    ["year"] = year
                };

                return $"{values[order[0]]} {values[order[1]]} {values[order[2]]}";
            }

            return $"{day} {monthName} {year}";
        }

        private string[] GetDatePartsOrder()
        {
            string pattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.ToLowerInvariant();

            int dayIndex = pattern.IndexOf('d');
            int monthIndex = pattern.IndexOf('m');
            int yearIndex = pattern.IndexOf('y');

            var parts = new List<(string Part, int Index)>
            {
                ("day", dayIndex),
                ("month", monthIndex),
                ("year", yearIndex)
            };

            return parts
                .Where(x => x.Index >= 0)
                .OrderBy(x => x.Index)
                .Select(x => x.Part)
                .ToArray();
        }

        private void ApplyWheelOrderByCulture()
        {
            if (MonthWheel == null || DayWheel == null || YearWheel == null ||
                Separator1 == null || Separator2 == null ||
                PartColumn1 == null || PartColumn2 == null || PartColumn3 == null)
            {
                return;
            }

            string[] order = GetDatePartsOrder();

            var map = new Dictionary<string, FrameworkElement>
            {
                ["day"] = DayWheel,
                ["month"] = MonthWheel,
                ["year"] = YearWheel
            };

            if (order.Length != 3 ||
                !map.ContainsKey(order[0]) ||
                !map.ContainsKey(order[1]) ||
                !map.ContainsKey(order[2]))
            {
                order = new[] { "day", "month", "year" };
            }

            Grid.SetColumn(map[order[0]], 0);
            Grid.SetColumn(Separator1, 1);
            Grid.SetColumn(map[order[1]], 2);
            Grid.SetColumn(Separator2, 3);
            Grid.SetColumn(map[order[2]], 4);

            ApplyColumnWidthForPart(PartColumn1, order[0]);
            ApplyColumnWidthForPart(PartColumn2, order[1]);
            ApplyColumnWidthForPart(PartColumn3, order[2]);

            MonthWheel.ItemTextAlignment = TextAlignment.Left;
            DayWheel.ItemTextAlignment = TextAlignment.Center;
            YearWheel.ItemTextAlignment = TextAlignment.Left;
        }

        private void ApplyColumnWidthForPart(ColumnDefinition column, string part)
        {
            if (column == null)
                return;

            switch (part)
            {
                case "day":
                    column.Width = new GridLength(0.95, GridUnitType.Star);
                    break;

                case "month":
                    column.Width = new GridLength(2.25, GridUnitType.Star);
                    break;

                case "year":
                    column.Width = new GridLength(1.10, GridUnitType.Star);
                    break;

                default:
                    column.Width = new GridLength(1.0, GridUnitType.Star);
                    break;
            }
        }

        private void PickerButton_Click(object sender, RoutedEventArgs e)
        {
            _previousDateBeforeOpen = SelectedDate;
            InitializeWheels();
        }

        private void MonthWheel_SelectedIndexChanged(object? sender, int index)
        {
            if (_isInternalUpdate)
                return;

            UpdateDayWheelFromMonthYear(preserveCurrentDay: true);
        }

        private void DayWheel_SelectedIndexChanged(object? sender, int index)
        {
            if (_isInternalUpdate)
                return;
        }

        private void YearWheel_SelectedIndexChanged(object? sender, int index)
        {
            if (_isInternalUpdate)
                return;

            UpdateDayWheelFromMonthYear(preserveCurrentDay: true);
        }

        private void UpdateDayWheelFromMonthYear(bool preserveCurrentDay)
        {
            if (MonthWheel == null || DayWheel == null || YearWheel == null)
                return;

            int month = MonthWheel.SelectedIndex + 1;
            if (month <= 0)
                month = DateTime.Today.Month;

            int year = GetSelectedYear();
            int currentDay = preserveCurrentDay ? GetSelectedDay() : 1;

            BuildDays(year, month);

            if (currentDay > _days.Count)
                currentDay = _days.Count;

            if (currentDay < 1)
                currentDay = 1;

            DateTime candidate = new DateTime(year, month, currentDay);
            candidate = ClampToRange(candidate);

            _isInternalUpdate = true;

            DayWheel.ItemsSource = _days.Select(x => x.ToString()).ToList();
            DayWheel.SelectedIndex = candidate.Day - 1;

            if (MonthWheel.SelectedIndex != candidate.Month - 1)
                MonthWheel.SelectedIndex = candidate.Month - 1;

            int yearIndex = _years.IndexOf(candidate.Year);
            if (yearIndex >= 0 && YearWheel.SelectedIndex != yearIndex)
                YearWheel.SelectedIndex = yearIndex;

            _isInternalUpdate = false;
        }

        private int GetSelectedYear()
        {
            if (YearWheel?.SelectedIndex >= 0 && YearWheel.SelectedIndex < _years.Count)
                return _years[YearWheel.SelectedIndex];

            return SelectedDate?.Year ?? DateTime.Today.Year;
        }

        private int GetSelectedDay()
        {
            if (DayWheel?.SelectedIndex >= 0)
                return DayWheel.SelectedIndex + 1;

            return SelectedDate?.Day ?? DateTime.Today.Day;
        }

        private async void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            int month = (MonthWheel?.SelectedIndex ?? 0) + 1;
            int day = (DayWheel?.SelectedIndex ?? 0) + 1;
            int year = GetSelectedYear();

            int maxDay = DateTime.DaysInMonth(year, month);
            if (day > maxDay)
                day = maxDay;

            SelectedDate = ClampToRange(new DateTime(year, month, day));
            await HideFlyoutAnimatedAsync();
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedDate = _previousDateBeforeOpen;
            await HideFlyoutAnimatedAsync();
        }

        private async System.Threading.Tasks.Task HideFlyoutAnimatedAsync()
        {
            if (_isClosingAnimated || PickerFlyout == null || FlyoutRoot == null)
            {
                PickerFlyout?.Hide();
                return;
            }

            _isClosingAnimated = true;
            await StartFlyoutCloseAnimationAsync();
            PickerFlyout.Hide();
            _isClosingAnimated = false;
        }

        private void PickerFlyout_Opened(object sender, object e)
        {
            StartFlyoutOpenAnimation();

            DispatcherQueue.TryEnqueue(() =>
            {
                FlyoutContentRoot?.Focus(FocusState.Programmatic);
            });
        }

        private void PickerFlyout_Closed(object sender, object e)
        {
            if (FlyoutRoot == null || FlyoutScaleTransform == null || FlyoutTranslateTransform == null)
                return;

            FlyoutRoot.Opacity = 0;
            FlyoutScaleTransform.ScaleX = 0.985;
            FlyoutScaleTransform.ScaleY = 0.985;
            FlyoutTranslateTransform.Y = -6;
        }

        private void StartFlyoutOpenAnimation()
        {
            if (FlyoutRoot == null || FlyoutScaleTransform == null || FlyoutTranslateTransform == null)
                return;

            FlyoutRoot.Opacity = 0;
            FlyoutScaleTransform.ScaleX = 0.985;
            FlyoutScaleTransform.ScaleY = 0.985;
            FlyoutTranslateTransform.Y = -6;

            var fade = new DoubleAnimation
            {
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(150)),
                EnableDependentAnimation = true
            };

            var scaleX = new DoubleAnimation
            {
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(150)),
                EnableDependentAnimation = true
            };

            var scaleY = new DoubleAnimation
            {
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(150)),
                EnableDependentAnimation = true
            };

            var translateY = new DoubleAnimation
            {
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(150)),
                EnableDependentAnimation = true
            };

            var sb = new Storyboard();

            sb.Children.Add(fade);
            sb.Children.Add(scaleX);
            sb.Children.Add(scaleY);
            sb.Children.Add(translateY);

            Storyboard.SetTarget(fade, FlyoutRoot);
            Storyboard.SetTargetProperty(fade, "Opacity");

            Storyboard.SetTarget(scaleX, FlyoutScaleTransform);
            Storyboard.SetTargetProperty(scaleX, "ScaleX");

            Storyboard.SetTarget(scaleY, FlyoutScaleTransform);
            Storyboard.SetTargetProperty(scaleY, "ScaleY");

            Storyboard.SetTarget(translateY, FlyoutTranslateTransform);
            Storyboard.SetTargetProperty(translateY, "Y");

            sb.Begin();
        }

        private async System.Threading.Tasks.Task StartFlyoutCloseAnimationAsync()
        {
            if (FlyoutRoot == null || FlyoutScaleTransform == null || FlyoutTranslateTransform == null)
                return;

            var fade = new DoubleAnimation
            {
                To = 0,
                Duration = new Duration(TimeSpan.FromMilliseconds(110)),
                EnableDependentAnimation = true
            };

            var scaleX = new DoubleAnimation
            {
                To = 0.985,
                Duration = new Duration(TimeSpan.FromMilliseconds(110)),
                EnableDependentAnimation = true
            };

            var scaleY = new DoubleAnimation
            {
                To = 0.985,
                Duration = new Duration(TimeSpan.FromMilliseconds(110)),
                EnableDependentAnimation = true
            };

            var translateY = new DoubleAnimation
            {
                To = -6,
                Duration = new Duration(TimeSpan.FromMilliseconds(110)),
                EnableDependentAnimation = true
            };

            var sb = new Storyboard();
            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

            void Completed(object? s, object? e)
            {
                sb.Completed -= Completed;
                tcs.TrySetResult(true);
            }

            sb.Completed += Completed;

            sb.Children.Add(fade);
            sb.Children.Add(scaleX);
            sb.Children.Add(scaleY);
            sb.Children.Add(translateY);

            Storyboard.SetTarget(fade, FlyoutRoot);
            Storyboard.SetTargetProperty(fade, "Opacity");

            Storyboard.SetTarget(scaleX, FlyoutScaleTransform);
            Storyboard.SetTargetProperty(scaleX, "ScaleX");

            Storyboard.SetTarget(scaleY, FlyoutScaleTransform);
            Storyboard.SetTargetProperty(scaleY, "ScaleY");

            Storyboard.SetTarget(translateY, FlyoutTranslateTransform);
            Storyboard.SetTargetProperty(translateY, "Y");

            sb.Begin();
            await tcs.Task;
        }

        private void CalendarIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            JumpToToday();
            e.Handled = true;
        }

        private void JumpToToday()
        {
            DateTime today = ClampToRange(DateTime.Today);

            SelectedDate = today;
            _previousDateBeforeOpen = today;
            InitializeWheels();
            UpdateDisplayText();
        }

        private void PickerButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Up)
            {
                AdjustDateByDays(-1);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Down)
            {
                AdjustDateByDays(1);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
            {
                _previousDateBeforeOpen = SelectedDate;
                InitializeWheels();
                PickerButton?.Flyout?.ShowAt(PickerButton);
                e.Handled = true;
            }
        }

        private async void FlyoutRoot_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                SelectedDate = _previousDateBeforeOpen;
                await HideFlyoutAnimatedAsync();
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Enter)
            {
                int month = (MonthWheel?.SelectedIndex ?? 0) + 1;
                int day = (DayWheel?.SelectedIndex ?? 0) + 1;
                int year = GetSelectedYear();

                int maxDay = DateTime.DaysInMonth(year, month);
                if (day > maxDay)
                    day = maxDay;

                SelectedDate = ClampToRange(new DateTime(year, month, day));
                await HideFlyoutAnimatedAsync();
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Up)
            {
                AdjustDateByDays(-1);
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Down)
            {
                AdjustDateByDays(1);
                e.Handled = true;
            }
        }

        private void AdjustDateByDays(int deltaDays)
        {
            DateTime baseDate = SelectedDate ?? ClampToRange(DateTime.Today);
            DateTime adjusted = ClampToRange(baseDate.AddDays(deltaDays));

            SelectedDate = adjusted;
            InitializeWheels();
            UpdateDisplayText();
        }

        private void ZephyrosDatePicker_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (OuterBorder != null)
                OuterBorder.BorderBrush = GetBrush("DatePickerHoverBorderBrush", "InputBorderHoverBrush");
        }

        private void ZephyrosDatePicker_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (OuterBorder != null)
                OuterBorder.BorderBrush = GetBrush("DatePickerBorderBrush", "CardBorder");
        }

        private void CoerceSelectedDateToRange()
        {
            if (!SelectedDate.HasValue)
                return;

            DateTime clamped = ClampToRange(SelectedDate.Value);

            if (SelectedDate.Value.Date != clamped.Date)
                SelectedDate = clamped;
        }

        private DateTime ClampToRange(DateTime value)
        {
            DateTime result = value.Date;

            if (MinDate.HasValue && result < MinDate.Value.Date)
                result = MinDate.Value.Date;

            return result;
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