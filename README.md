\# Zephyros DatePicker



Custom theme-aware \*\*WinUI 3 DatePicker\*\* and \*\*WheelPicker\*\* controls for modern desktop applications.



A lightweight, stylized date picker with:

\- smooth flyout animation

\- wheel-based month/day/year selection

\- keyboard navigation

\- theme token support

\- dark / light / color theme integration



\---



\## 🚀 Features



\- Custom \*\*WinUI 3 DatePicker\*\*

\- Wheel-based date selection

\- Fully \*\*theme-aware\*\*

\- Token-based styling

\- Supports:

&#x20; - Dark theme

&#x20; - Light theme

&#x20; - Color theme

&#x20; - External custom theme packs

\- Keyboard support:

&#x20; - `↑ / ↓` change date

&#x20; - `Enter` confirm

&#x20; - `Esc` close

\- Smooth flyout animation

\- Designed for modern UI systems



\---



\## 📦 Included Controls



\### `ZephyrosDatePicker`

Main date picker with animated flyout and wheel selection.



\### `ZephyrosWheelColumn`

Reusable wheel column used for month/day/year.



\---



\## 📁 Project Structure





Zephyros-DatePicker/

├─ Themes/

│ ├─ ColorTheme.xaml

│ ├─ DarkTheme.xaml

│ └─ LightTheme.xaml

└─ Zephyros.Controls/

├─ ZephyrosDatePicker.xaml

├─ ZephyrosDatePicker.xaml.cs

├─ ZephyrosWheelColumn.xaml

└─ ZephyrosWheelColumn.xaml.cs





\---



\## ⚙️ Usage



\### 1. Add files to your project



Copy:



\- `ZephyrosDatePicker.xaml`

\- `ZephyrosDatePicker.xaml.cs`

\- `ZephyrosWheelColumn.xaml`

\- `ZephyrosWheelColumn.xaml.cs`



\---



\### 2. Merge themes



```xml

<Application.Resources>

&#x20;   <ResourceDictionary>

&#x20;       <ResourceDictionary.MergedDictionaries>

&#x20;           <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />

&#x20;           <ResourceDictionary Source="Themes/DarkTheme.xaml" />

&#x20;       </ResourceDictionary.MergedDictionaries>

&#x20;   </ResourceDictionary>

</Application.Resources>

3\. Use in XAML

<controls:ZephyrosDatePicker

&#x20;   Height="32"

&#x20;   Width="300"

&#x20;   StartYear="2020"

&#x20;   EndYear="2040"/>



With event:



<controls:ZephyrosDatePicker

&#x20;   Height="32"

&#x20;   Width="300"

&#x20;   StartYear="2020"

&#x20;   EndYear="2040"

&#x20;   SelectedDateChanged="EmbarkationDatePicker\_SelectedDateChanged"/>

🎨 Theme Tokens



The control uses ThemeResource, not hardcoded colors.



Examples:



PrimaryForegroundBrush



MutedText



CardBg



CardBorder



OverlayDimBrush



👉 Fully compatible with Zephyros theme system



⌨️ Keyboard Support



Inside picker:



↑ / ↓ → change date



Enter → confirm



Esc → cancel



Closed picker:



↑ / ↓ → quick change



Enter / Space → open



🎯 Designed For



WinUI 3 apps



Dashboard UIs



Custom design systems



Theme-based applications



🧠 Roadmap



&#x20;Wheel selection



&#x20;Animation



&#x20;Keyboard support



&#x20;Theme tokens



&#x20;Demo app



&#x20;NuGet package



📸 Screenshots



(add later)



📄 License



MIT License

