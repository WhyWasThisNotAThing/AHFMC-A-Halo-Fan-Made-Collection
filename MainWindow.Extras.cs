using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Media;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AHFMC;

public partial class MainWindow
{
    private static Brush Br(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;

    private const double EdgeGrip = 8;
    private double _edge;

    private void SetEdge(double edge)
    {
        if (Math.Abs(_edge - edge) < 0.01) return;
        _edge = edge;
        _chatPx = -1;
        UpdateUiScale();
    }

    private void ApplyWindowed()
    {
        Topmost = false;
        SetEdge(EdgeGrip);
        if (ResizeMode == ResizeMode.CanResize) return;
        WindowState = WindowState.Normal;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResize;
        System.Windows.Shell.WindowChrome.SetWindowChrome(this, new System.Windows.Shell.WindowChrome
        {
            CaptionHeight = 0,
            ResizeBorderThickness = new Thickness(EdgeGrip),
            GlassFrameThickness = new Thickness(0),
            UseAeroCaptionButtons = false,
            CornerRadius = new CornerRadius(0)
        });
        var wa = SystemParameters.WorkArea;
        Width = Math.Min(1366, wa.Width - 80);
        Height = Math.Min(860, wa.Height - 80);
        Left = wa.Left + (wa.Width - Width) / 2;
        Top = wa.Top + (wa.Height - Height) / 2;
    }

    private void Window_DragStrip(object sender, MouseButtonEventArgs e)
    {
        if (_config.WindowMode != 1 || WindowState != WindowState.Normal || e.ChangedButton != MouseButton.Left) return;
        if (e.GetPosition(this).Y > 64) return;
        try { DragMove(); } catch { }
    }

    private double _uiScale = 1, _classicZoom = 1;

    private const double ChatLogicalWidth = 640;
    private double _chatPx, _chatScale = 1;

    private void UpdateUiScale()
    {
        double w = ActualWidth, h = ActualHeight;
        if (w < 50 || h < 50) return;

        double cs = Math.Clamp(Math.Min(w / 1600.0, h / 900.0), 0.5, 2.5);
        double px = ChatPane.Visibility == Visibility.Visible ? ChatLogicalWidth * cs : 0;
        if (Math.Abs(px - _chatPx) > 0.5)
        {
            _chatPx = px;
            var m = new Thickness(0, 0, px, 0);
            UiMain.Margin = m; UiTop.Margin = m;
            var wm = new Thickness(_edge, _edge, px + _edge, _edge);
            ClassicPane.Margin = wm; BrowserPane.Margin = wm;
        }
        if (Math.Abs(cs - _chatScale) > 0.002)
        {
            _chatScale = cs;
            ChatPane.LayoutTransform = new ScaleTransform(cs, cs);
            ChatToast.LayoutTransform = new ScaleTransform(cs, cs);
            UiBack.LayoutTransform = new ScaleTransform(cs, cs);
        }
        double availW = Math.Max(300, w - _chatPx);

        double s = Math.Clamp(Math.Min(availW / 1600.0, h / 900.0), 0.5, 2.5);
        if (Math.Abs(s - _uiScale) > 0.002)
        {
            _uiScale = s;
            UiMain.LayoutTransform = new ScaleTransform(s, s);
            UiTop.LayoutTransform = new ScaleTransform(s, s);
        }
        double z = Math.Clamp(Math.Min((availW - 2 * _edge) / 1920.0, (h - 2 * _edge) / 1080.0), 0.4, 3.0);
        if (Math.Abs(z - _classicZoom) > 0.002)
        {
            _classicZoom = z;
            try { ClassicView.ZoomFactor = z; } catch { }
        }
    }

    private void ApplyCursorMode()
    {
        if (_config.CursorMode == 1) { _mouseHidden = true; Mouse.OverrideCursor = Cursors.None; }
        else { _mouseHidden = false; Mouse.OverrideCursor = null; }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DialogPane.Visibility == Visibility.Visible && DialogInput.Visibility == Visibility.Visible)
        {
            if (e.Key == Key.Enter && DialogInput.IsKeyboardFocused) { AnswerDialog(true); e.Handled = true; }
            return;
        }
        FocusNavigationDirection dir;
        switch (e.Key)
        {
            case Key.Up: dir = FocusNavigationDirection.Up; break;
            case Key.Down: dir = FocusNavigationDirection.Down; break;
            case Key.Left: dir = FocusNavigationDirection.Left; break;
            case Key.Right: dir = FocusNavigationDirection.Right; break;
            default: return;
        }
        if (ClassicPane.Visibility == Visibility.Visible) return;
        if (ChatPane.Visibility == Visibility.Visible) return;
        if (BrowserPane.Visibility == Visibility.Visible && DialogPane.Visibility != Visibility.Visible) return;
        HideMouse();
        PadMove(dir);
        e.Handled = true;
    }

    private Button? _newsFirstBtn;

    private void ShowNewsPane()
    {
        NewsList.Children.Clear();
        _newsFirstBtn = null;
        var items = VisibleNews();
        if (items.Count == 0)
        {
            NewsList.Children.Add(new TextBlock
            {
                Text = _newsItems.Count == 0 ? "Couldn't load the latest release notes. Check your internet connection."
                                             : "All news sources are turned off. You can turn them on in Settings.",
                Foreground = Br("#C5D3E3"), FontSize = 16, TextWrapping = TextWrapping.Wrap
            });
        }
        foreach (var x in items)
        {
            var head = new TextBlock { FontSize = 21, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 2) };
            head.Inlines.Add(new System.Windows.Documents.Run(x.Name));
            if (x.IsNew) head.Inlines.Add(new System.Windows.Documents.Run("   NEW") { FontSize = 15, Foreground = Br("#7BD88F") });
            NewsList.Children.Add(head);
            NewsList.Children.Add(new TextBlock { Text = x.Label + (x.Date.Length > 0 ? "   " + x.Date : ""), FontSize = 14, Foreground = Br("#8FA3BA"), TextWrapping = TextWrapping.Wrap });
            NewsList.Children.Add(new TextBlock { Text = CleanNotes(x.Body, 1500), FontSize = 15, LineHeight = 23, Foreground = Br("#C5D3E3"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) });
            var btn = new Button { Style = (Style)FindResource("Pill"), Content = "View on GitHub", Tag = x.Url, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 14, 0, 30) };
            btn.Click += NewsLink_Click;
            NewsList.Children.Add(btn);
            _newsFirstBtn ??= btn;
        }
        NewsPane.Visibility = Visibility.Visible;
        NewsScroll.ScrollToTop();
        Dispatcher.BeginInvoke(new Action(() => (_newsFirstBtn ?? NewsClose).Focus()), DispatcherPriority.Input);
    }

    private async void NewsLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string url }) return;
        NewsPane.Visibility = Visibility.Collapsed;
        await OpenInAppAsync(url, false);
    }

    private void CloseNews_Click(object sender, RoutedEventArgs e) => NewsPane.Visibility = Visibility.Collapsed;

    private void NewsScrim_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, NewsPane)) NewsPane.Visibility = Visibility.Collapsed;
    }

    private void PaneMove(UIElement pane, ScrollViewer sv, FocusNavigationDirection dir, Button fallback)
    {
        if (dir == FocusNavigationDirection.Up) { sv.ScrollToVerticalOffset(Math.Max(0, sv.VerticalOffset - 160)); return; }
        if (dir == FocusNavigationDirection.Down) { sv.ScrollToVerticalOffset(sv.VerticalOffset + 160); return; }
        MoveWithin(pane, dir == FocusNavigationDirection.Left ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next, fallback);
    }

    private static readonly string[] LayoutNames = { "Normal", "Classic Halo CE", "Hybrid", "Big Picture", "Big Picture Hybrid" };

    private Action<int>? _dropPick;

    private void OpenDrop(string title, string[] items, int current, Action<int> pick)
    {
        _dropPick = pick;
        DropTitle.Text = title;
        DropItems.Children.Clear();
        Button? focus = null;
        for (int i = 0; i < items.Length; i++)
        {
            int idx = i;
            bool cur = i == current;
            var b = new Button
            {
                Style = (Style)FindResource("CatRow"), Content = (cur ? "✓   " : "      ") + items[i], Tag = "drop",
                Background = cur ? Br("#CC1B3556") : Brushes.Transparent, BorderBrush = cur ? Br("#662F7FD1") : Brushes.Transparent
            };
            b.Click += (_, _) => { var p = _dropPick; CloseDrop(); if (idx != current) p?.Invoke(idx); };
            DropItems.Children.Add(b);
            if (cur || focus == null) focus = cur ? b : focus ?? b;
        }
        DropLayer.Visibility = Visibility.Visible;
        Dispatcher.BeginInvoke(new Action(() => focus?.Focus()), DispatcherPriority.Input);
    }

    private bool DropOpen => DropLayer.Visibility == Visibility.Visible;

    private void CloseDrop()
    {
        _dropPick = null;
        DropLayer.Visibility = Visibility.Collapsed;
        Dispatcher.BeginInvoke(new Action(() => (_firstSettingBtn ?? CurrentCatBtn()).Focus()), DispatcherPriority.Input);
    }

    private void DropLayer_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, DropLayer)) { CloseDrop(); e.Handled = true; }
    }

    private void DropMove(FocusNavigationDirection dir)
    {
        if (dir != FocusNavigationDirection.Up && dir != FocusNavigationDirection.Down) return;
        var items = DropItems.Children.OfType<Button>().ToList();
        if (items.Count == 0) return;
        int i = items.FindIndex(b => b.IsKeyboardFocused);
        i = i < 0 ? 0 : Wrap(i, dir == FocusNavigationDirection.Down ? 1 : -1, items.Count);
        items[i].Focus();
    }

    private sealed class SettingDef
    {
        public string Label = "", Desc = "", Cat = "", Kind = "choice", On = "";
        public string[]? Btns, Drop;
        public Action<int>? Pick;
        public int Sub;
        public readonly List<Button> SubButtons = new();
        public Func<string> Value = () => "";
        public Action<int> Change = _ => { };
        public TextBlock? ValueText;
        public Action? Refresh;
    }

    private sealed class SettingCat
    {
        public string Name = "";
        public StackPanel Panel = new();
        public Button? Btn, FirstRow;
    }

    private readonly List<SettingCat> _cats = new();
    private SettingCat? _curCat;
    private int _selCat;

    private void ShowCategory(SettingCat c)
    {
        _selCat = Math.Max(0, _cats.IndexOf(c));
        foreach (var k in _cats)
            if (k.Btn != null)
            {
                bool on = ReferenceEquals(k, c);
                k.Btn.Background = on ? Br("#CC1B3556") : Brushes.Transparent;
                k.Btn.BorderBrush = on ? Br("#662F7FD1") : Brushes.Transparent;
            }
        if (!ReferenceEquals(SettingsScroll.Content, c.Panel)) { SettingsScroll.Content = c.Panel; SettingsScroll.ScrollToTop(); }
        _firstSettingBtn = c.FirstRow;
    }

    private Button CurrentCatBtn() => (_cats.Count > 0 ? _cats[Math.Clamp(_selCat, 0, _cats.Count - 1)].Btn : null) ?? SettingsClose;
    private void FocusFirstRow() => Dispatcher.BeginInvoke(new Action(() => (_firstSettingBtn ?? CurrentCatBtn()).Focus()), DispatcherPriority.Input);

    private void SettingsPadMove(FocusNavigationDirection dir)
    {
        if (DropOpen) { DropMove(dir); return; }
        if (Keyboard.FocusedElement is Button f && SettingsPane.IsAncestorOf(f))
        {
            bool vertical = dir == FocusNavigationDirection.Up || dir == FocusNavigationDirection.Down;
            if (vertical && (f.Tag is SettingDef || f.Tag is SettingCat) && _cats.Count > 0)
            {
                var items = f.Tag is SettingCat
                    ? _cats.Select(c => c.Btn).OfType<Button>().ToList()
                    : _cats[Math.Clamp(_selCat, 0, _cats.Count - 1)].Panel.Children.OfType<Button>().ToList();
                int at = items.IndexOf(f);
                if (at >= 0 && items.Count > 0)
                {
                    var next = items[Wrap(at, dir == FocusNavigationDirection.Down ? 1 : -1, items.Count)];
                    next.Focus();
                    next.BringIntoView();
                    return;
                }
            }
            if (f.Tag is SettingDef def && (dir == FocusNavigationDirection.Left || dir == FocusNavigationDirection.Right))
            {
                int delta = dir == FocusNavigationDirection.Right ? 1 : -1;
                if (def.Kind is "btn" or "btns")
                {
                    int count = def.Btns?.Length ?? 1, k = def.Sub + delta;
                    if (k < 0) { CurrentCatBtn().Focus(); return; }
                    def.Sub = Math.Min(k, count - 1);
                    UpdateSub(def, true);
                    PlayMenuSound("cursor");
                    return;
                }
                def.Change(delta);
                RefreshSettings();
                PushClassicState();
                PlayMenuSound("cursor");
                return;
            }
            if (f.Tag is SettingCat)
            {
                if (dir == FocusNavigationDirection.Right) { (_firstSettingBtn ?? f).Focus(); return; }
                if (dir == FocusNavigationDirection.Left) return;
            }
        }
        MoveWithin(SettingsPane, dir, CurrentCatBtn());
    }

    private void UpdateSub(SettingDef d, bool focused)
    {
        for (int i = 0; i < d.SubButtons.Count; i++)
        {
            bool sel = focused && i == d.Sub;
            var b = d.SubButtons[i];
            b.BorderBrush = Br(sel ? "#FFD779" : "#662F7FD1");
            b.BorderThickness = new Thickness(sel ? 2 : 1);
            b.Background = Br(sel ? "#CC2E5A8F" : "#CC1B3556");
        }
    }

    private bool SettingsStepBack()
    {
        if (Keyboard.FocusedElement is Button { Tag: SettingDef } b && SettingsPane.IsAncestorOf(b)) { CurrentCatBtn().Focus(); return true; }
        return false;
    }

    private void ClassicAct(string action, string target)
    {
        switch (action)
        {
            case "setup": ShowSetup(firstTime: false); return;
            case "chatopen": ShowChat(); return;
            case "updatecheck": _ = CheckLauncherUpdateAsync(manual: true); return;
            case "chattest": SendTestNotification(); return;
            case "chatfp": _ = ShowFingerprintAsync(); return;
            case "chatmsgs": _ = ChatAccountActionAsync(AHFMCChat.SettingsAction.DeleteMessages); return;
            case "chatacct": _ = ChatAccountActionAsync(AHFMCChat.SettingsAction.DeleteAccount); return;
            case "chatlogout": _ = ChatAccountActionAsync(AHFMCChat.SettingsAction.Logout); return;
            case "addapp": AddCustomApp(); return;
            case "bgvideo": ChooseBackground(true); return;
            case "bgmusic": ChooseBackground(false); return;
            case "bgvideoclear": ClearBackground(true); return;
            case "bgmusicclear": ClearBackground(false); return;
            case "renameapp": if (FindApp(target) != null) RenameCustomApp(target); return;
            case "locateapp": if (FindApp(target) != null) LocateCustomApp(target); return;
            case "picapp": if (FindApp(target) != null) ChangeAppPicture(target); return;
            case "nopicapp": if (FindApp(target) != null) RemoveAppPicture(target); return;
            case "removeapp": if (FindApp(target) != null) RemoveCustomApp(target); return;
            case "pickmenusound": ChangeMenuSound(target); return;
            case "clearmenusound": ClearMenuSound(target); return;
            case "piclogo": ChangeGameLogo(target); return;
            case "nopiclogo": RemoveGameLogo(target); return;
        }
        bool locate = action == "locate";
        if (!locate && action != "forget") return;
        switch (target)
        {
            case "halo3": if (locate) LocateGame(false); else ForgetOrUninstall("halo3"); break;
            case "reach": if (locate) LocateGame(true); else ForgetOrUninstall("reach"); break;
            case "gravemind": if (locate) LocateGravemind(); else ForgetOrUninstall("gravemind"); break;
            case "raven": if (locate) LocateRaven(); else ForgetOrUninstall("raven"); break;
            case "groundside": if (locate) LocateGroundside(); else ForgetOrUninstall("groundside"); break;
            case "oce": if (locate) LocateOce(); else ForgetOrUninstall("oce"); break;
            default:
                if (target.StartsWith("off:")) { string oid = target[4..]; if (locate) LocateOfficial(oid); else ForgetOfficial(oid); }
                break;
        }
    }

    private readonly List<SettingDef> _settings = new();
    private Button? _firstSettingBtn;

    private static int Wrap(int value, int delta, int count) => ((value + delta) % count + count) % count;

    private void BuildSettings()
    {
        SettingsCats.Children.Clear();
        _cats.Clear();
        _curCat = null;
        _settings.Clear();
        _firstSettingBtn = null;
        _addAppSettingBtn = null;

        void Header(string text)
        {
            var c = new SettingCat { Name = text };
            _curCat = c;
            _cats.Add(c);
            var cb = new Button { Style = (Style)FindResource("CatRow"), Content = text, Tag = c };
            c.Btn = cb;
            cb.Click += (_, _) => { ShowCategory(c); FocusFirstRow(); };
            cb.GotKeyboardFocus += (_, _) => ShowCategory(c);
            SettingsCats.Children.Add(cb);
        }

        void Add(string label, string desc, Func<string> value, Action<int> change, string? btn = null, string[]? btns = null, string? on = null, string[]? drop = null, Action<int>? pick = null)
        {
            var d = new SettingDef { Label = label, Desc = desc, Value = value, Change = change, Cat = _curCat!.Name };
            string first = value();
            bool toggle = btn == null && btns == null && (on != null || first is "Enabled" or "Disabled" or "Show text" or "Hide text");
            on ??= first is "Disabled" or "Hide text" ? (first == "Disabled" ? "Enabled" : "Show text") : first;
            d.Kind = toggle ? "toggle" : btns != null ? "btns" : drop != null ? "drop" : btn != null ? "btn" : "choice";
            d.On = on;
            d.Btns = btns ?? (btn != null ? new[] { btn } : null);
            d.Drop = drop;
            d.Pick = pick;

            TextBlock Arrow(string glyph, int delta)
            {
                var t = new TextBlock
                {
                    Text = glyph, FontSize = 18, Foreground = Br("#5F7690"), Padding = new Thickness(12, 6, 12, 6),
                    Background = Brushes.Transparent, Cursor = Cursors.Hand, VerticalAlignment = VerticalAlignment.Center
                };
                t.MouseEnter += (_, _) => t.Foreground = Br("#FFD779");
                t.MouseLeave += (_, _) => t.Foreground = Br("#5F7690");
                t.PreviewMouseLeftButtonDown += (_, e) => { e.Handled = true; d.Change(delta); RefreshSettings(); };
                return t;
            }

            Button ActionButton(string text, int delta)
            {
                var b = new Button
                {
                    Content = text, FontSize = 16, Padding = new Thickness(18, 7, 18, 7), Margin = new Thickness(8, 0, 0, 0), Foreground = Brushes.White,
                    Background = Br("#CC1B3556"), BorderBrush = Br("#662F7FD1"), BorderThickness = new Thickness(1), Cursor = Cursors.Hand, Focusable = false,
                    Template = BoxButtonTemplate()
                };
                b.Click += (_, e) => { e.Handled = true; d.Change(delta); RefreshSettings(); };
                d.SubButtons.Add(b);
                return b;
            }

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 24, 0) };
            left.Children.Add(new TextBlock { Text = label, FontSize = 19, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap });
            left.Children.Add(new TextBlock { Text = desc, FontSize = 13, Foreground = Br("#8FA3BA"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) });
            Grid.SetColumn(left, 0);

            var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right };
            if (toggle)
            {
                var tick = new TextBlock { Text = "✓", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Br("#FFD779"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                var box = new Border { Width = 34, Height = 34, CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(2), Margin = new Thickness(12, 0, 6, 0), Child = tick };
                d.Refresh = () =>
                {
                    bool isOn = d.Value() == on;
                    tick.Visibility = isOn ? Visibility.Visible : Visibility.Hidden;
                    box.BorderBrush = Br(isOn ? "#FFD779" : "#5F7690");
                    box.Background = Br(isOn ? "#33FFD779" : "#2A1C2E49");
                };
                right.Children.Add(box);
            }
            else if (btns != null)
            {
                d.ValueText = new TextBlock { FontSize = 15, Foreground = Br("#FFD779"), Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap };
                left.Children.Add(d.ValueText);
                right.Children.Add(ActionButton(btns[0], 1));
                if (btns.Length > 1) right.Children.Add(ActionButton(btns[1], -1));
            }
            else if (drop != null)
            {
                d.ValueText = new TextBlock
                {
                    MinWidth = 150, TextAlignment = TextAlignment.Center, FontSize = 17, Foreground = Br("#FFD779"),
                    VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 8, 0)
                };
                var chev = new TextBlock { Text = "▾", FontSize = 18, Foreground = Br("#8FA3BA"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 14, 2) };
                right.Children.Add(new Border
                {
                    CornerRadius = new CornerRadius(14), Background = Br("#2A1C2E49"), BorderBrush = Br("#662F7FD1"), BorderThickness = new Thickness(1), Padding = new Thickness(6, 4, 6, 4),
                    Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { d.ValueText, chev } }
                });
            }
            else if (btn != null)
            {
                d.ValueText = new TextBlock { FontSize = 15, Foreground = Br("#FFD779"), Margin = new Thickness(0, 4, 0, 0), TextWrapping = TextWrapping.Wrap };
                if (first != "-") left.Children.Add(d.ValueText);
                right.Children.Add(ActionButton(btn, 1));
            }
            else
            {
                right.Children.Add(Arrow("◀", -1));
                d.ValueText = new TextBlock
                {
                    MinWidth = 150, TextAlignment = TextAlignment.Center, FontSize = 17, Foreground = Br("#FFD779"),
                    VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0)
                };
                right.Children.Add(new Border { CornerRadius = new CornerRadius(14), Background = Br("#2A1C2E49"), Padding = new Thickness(6, 4, 6, 4), Child = d.ValueText });
                right.Children.Add(Arrow("▶", 1));
            }
            Grid.SetColumn(right, 1);

            grid.Children.Add(left);
            grid.Children.Add(right);

            var row = new Button { Style = (Style)FindResource("SettingRow"), Content = grid, Tag = d };
            row.Click += (_, _) =>
            {
                if (drop != null && pick != null) OpenDrop(label, drop, Array.IndexOf(drop, d.Value()), i => { pick(i); RefreshSettings(); });
                else if (btns != null) { d.Change(d.Sub == 0 ? 1 : -1); RefreshSettings(); }
                else { d.Change(1); RefreshSettings(); }
            };
            row.GotKeyboardFocus += (_, _) => { d.Sub = 0; UpdateSub(d, true); };
            row.LostKeyboardFocus += (_, _) => UpdateSub(d, false);
            row.MouseRightButtonUp += (_, _) => { if (!toggle && btn == null && btns == null) { d.Change(-1); RefreshSettings(); } };
            if (btns != null)
                row.PreviewMouseLeftButtonDown += (_, e) => { if (!HitInnerButton(e.OriginalSource as DependencyObject, row)) e.Handled = true; };
            _curCat!.Panel.Children.Add(row);
            _curCat.FirstRow ??= row;
            _settings.Add(d);
            _firstSettingBtn ??= row;
            if (label == "Add an app") _addAppSettingBtn = row;
        }

        static bool HitInnerButton(DependencyObject? o, Button row)
        {
            while (o != null && !ReferenceEquals(o, row))
            {
                if (o is Button) return true;
                o = o is System.Windows.Media.Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(o) : LogicalTreeHelper.GetParent(o);
            }
            return false;
        }

        static ControlTemplate BoxButtonTemplate()
        {
            var bd = new FrameworkElementFactory(typeof(Border));
            bd.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            bd.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            bd.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            bd.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            bd.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            bd.AppendChild(cp);
            var t = new ControlTemplate(typeof(Button)) { VisualTree = bd };
            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.8));
            t.Triggers.Add(hover);
            return t;
        }

        Header("Launcher");
        Add("Layout",
            "Normal is the usual launcher. Classic Halo CE is the original Halo menu. Hybrid is the usual launcher with a background video and music that you choose yourself (see Background video and Background music below). Big Picture is a big, controller-friendly screen like Steam Big Picture (Big Picture Hybrid adds the Halo video and music).",
            () => LayoutNames[Math.Clamp(_config.Layout, 0, 4)],
            d => SetLayout(Wrap(_config.Layout, d, 5)), drop: LayoutNames, pick: i => SetLayout(Math.Clamp(i, 0, 4)));
        Add("Background video",
            "Hybrid layouts and Classic Halo CE show a background video that you bring yourself. Pick an .mp4 you made or have the right to use. The launcher does not include one.",
            () => BgState(BgVideoPath),
            d => { if (d > 0) ChooseBackground(true); else ClearBackground(true); }, btns: new[] { "Choose...", "Remove" });
        Add("Background music",
            "Optional music to go with the video. Pick an .mp3 you made or have the right to use. The launcher does not include any. If your video has its own sound, the launcher uses that and this is not needed.",
            () => BgMusicState(),
            d => { if (d > 0) ChooseBackground(false); else ClearBackground(false); }, btns: new[] { "Choose...", "Remove" });
        Add("Start screen",
            "Classic Halo CE menu: show \"Press Enter / A to start\" first, with only the music and the background.",
            () => _config.StartScreen ? "Enabled" : "Disabled",
            d => { _config.StartScreen = !_config.StartScreen; SaveConfig(); PushClassicState(); });
        Add("Hide games I don't own",
            "Leave out the games that are not installed.",
            () => _config.HideMissing ? "Enabled" : "Disabled",
            d => { _config.HideMissing = !_config.HideMissing; SaveConfig(); ApplyHideMissing(); PushClassicState(); });
        Add("Hide cursor",
            "Hide the mouse pointer while you use a controller or the arrow keys, always, or never.",
            () => new[] { "With controller", "Always", "Never" }[Math.Clamp(_config.CursorMode, 0, 2)],
            d => { _config.CursorMode = Wrap(_config.CursorMode, d, 3); SaveConfig(); ApplyCursorMode(); PushClassicState(); });
        Add("Blurry background (Big Picture)",
            "Big Picture: the picture behind the screen follows the selected game. Off shows it sharp instead of blurred.",
            () => _config.BigPictureBlur ? "Enabled" : "Disabled",
            d => { _config.BigPictureBlur = !_config.BigPictureBlur; SaveConfig(); PushClassicState(); });
        Add("Window",
            "Fullscreen covers the whole screen, taskbar included. Windowed is a normal window you can move and resize.",
            () => _config.WindowMode == 0 ? "Fullscreen" : "Windowed",
            d => { _config.WindowMode = 1 - _config.WindowMode; SaveConfig(); ApplyFullscreen(); PushClassicState(); });
        Add("Launch countdown",
            "3, 2, 1, GO with sounds while the launcher checks for updates and gets the game ready.",
            () => new[] { "Off", "Fan games only", "All games" }[Math.Clamp(_config.Countdown, 0, 2)],
            d => { _config.Countdown = Wrap(_config.Countdown, d, 3); SaveConfig(); PushClassicState(); });
        Add("Maximize game window",
            "Make the game window big after it starts. Turn this off to leave it as the game opens it.",
            () => _config.MaximizeGame ? "Enabled" : "Disabled",
            d => { _config.MaximizeGame = !_config.MaximizeGame; SaveConfig(); PushClassicState(); });

        Add("Keep running in tray when I close the app",
            "Off: closing exits the launcher. On: it hides in the system tray (chat messages still reach you, Discord stops showing that you use the launcher, and clicking the .exe again brings it back). The tray icon's menu has Exit launcher to really quit.",
            () => _config.CloseToTray ? "Enabled" : "Disabled",
            d => { _config.CloseToTray = !_config.CloseToTray; SaveConfig(); PushClassicState(); });
        Add("Discord Rich Presence",
            "Show what you are playing on your Discord profile (for example \"Playing Reclaimer\"). Off: nothing is shown and the launcher does not connect to Discord.",
            () => _config.DiscordPresence ? "Enabled" : "Disabled",
            d => { _config.DiscordPresence = !_config.DiscordPresence; SaveConfig(); ApplyDiscordSetting(); PushClassicState(); });
        Add("First-time setup",
            "Open the welcome screen again, where you pick your layout and a few settings.",
            () => "-",
            d => { if (d > 0) { CloseSettingsPane(); ShowSetup(firstTime: false); } }, btn: "Open...");

        Header("Updates");
        Add("Launcher version",
            "Look for a newer version of this launcher on GitHub now. An installed copy runs the installer; a portable copy replaces its own .exe. Either way, the launcher restarts by itself.",
            () => LauncherVersionText(),
            d => { if (d > 0) _ = CheckLauncherUpdateAsync(manual: true); }, btn: "Check for update");
        Add("Check for updates on start",
            "Look for a newer launcher every time it starts. You are always asked before anything is downloaded.",
            () => _config.AutoUpdateLauncher ? "Enabled" : "Disabled",
            d => { _config.AutoUpdateLauncher = !_config.AutoUpdateLauncher; SaveConfig(); PushClassicState(); });

        Header("Sound");
        Add("Master volume",
            "How loud everything is: the music, the menu sounds, the countdown and the chat ding. The other volumes are relative to this one.",
            () => VolText(_config.MasterVolume),
            d => { _config.MasterVolume = StepVolume(_config.MasterVolume, d); VolumeChanged("count"); });
        Add("Music volume",
            "The volume of the background music you chose, in the Classic Halo CE and Hybrid layouts (also Big Picture Hybrid).",
            () => VolText(_config.MusicVolume),
            d => { _config.MusicVolume = StepVolume(_config.MusicVolume, d); VolumeChanged(""); });
        Add("Menu sounds volume",
            "The click, move and back sounds of the menus, in every layout.",
            () => VolText(_config.MenuVolume),
            d => { _config.MenuVolume = StepVolume(_config.MenuVolume, d); VolumeChanged(""); });
        foreach (var (mkey, mname) in MenuSoundList)
        {
            string k2 = mkey;
            Add("Menu sound: " + mname,
                "Pick your own short sound (.wav, .mp3 or .ogg) for the menu \"" + mname.ToLowerInvariant() + "\" effect, in every layout. Remove goes back to the default.",
                () => MenuSoundPath(k2) != null ? "Your sound" : "Default",
                d => { if (d > 0) ChangeMenuSound(k2); else ClearMenuSound(k2); }, btns: new[] { "Pick sound...", "Remove" });
        }
        Add("Countdown volume",
            "The sounds of the 3, 2, 1, GO launch countdown, including your own if you pick them below.",
            () => VolText(_config.CountdownVolume),
            d => { _config.CountdownVolume = StepVolume(_config.CountdownVolume, d); VolumeChanged("count"); });
        foreach (var (ckey, cname) in CountSoundList)
        {
            string k3 = ckey;
            Add("Countdown sound: " + cname,
                k3 == "countgo"
                    ? "Pick your own short sound (.wav, .mp3 or .ogg) that plays on GO. Remove goes back to the default."
                    : "Pick your own short sound (.wav, .mp3 or .ogg) for each count of 3, 2, 1. It plays once a second, so keep it short. Remove goes back to the default.",
                () => MenuSoundPath(k3) != null ? "Your sound" : "Default",
                d => { if (d > 0) ChangeMenuSound(k3); else ClearMenuSound(k3); }, btns: new[] { "Pick sound...", "Remove" });
        }
        Add("Chat notification sound",
            "The ding for a new chat message while the chat is not in front. You hear a sample when you change it. Windows default is the ding you had before.",
            () => new[] { "Off", "Windows default", "Halo beep", "Halo respawn" }[Math.Clamp(AHFMCChat.LocalSettings.NotificationSound, 0, 3)],
            d => { AHFMCChat.LocalSettings.NotificationSound = Wrap(AHFMCChat.LocalSettings.NotificationSound, d, 4); VolumeChanged("ding"); });
        Add("Chat notification volume",
            "How loud the chat ding is.",
            () => VolText(AHFMCChat.LocalSettings.NotificationVolume),
            d => { AHFMCChat.LocalSettings.NotificationVolume = StepVolume(AHFMCChat.LocalSettings.NotificationVolume, d); VolumeChanged("ding"); });

        Header("News");
        Add("Launcher news",
            "Include the release notes of this launcher (new versions of AHFMC: A Halo Fan-Made Collection).",
            () => _config.NewsLauncher ? "Enabled" : "Disabled",
            d => { _config.NewsLauncher = !_config.NewsLauncher; SaveConfig(); PushClassicState(); PushNews(false); });
        Add("Show what's new",
            "Open the news when the launcher starts and one of the projects below has a new release.",
            () => _config.ShowNewsOnStart ? "Enabled" : "Disabled",
            d => { _config.ShowNewsOnStart = !_config.ShowNewsOnStart; SaveConfig(); PushClassicState(); });
        Add("Reclaimer news",
            "Include the Reclaimer release notes.",
            () => _config.NewsHalo3 ? "Enabled" : "Disabled",
            d => { _config.NewsHalo3 = !_config.NewsHalo3; SaveConfig(); PushClassicState(); PushNews(false); });
        Add("Halo Reach Renaissance news",
            "Include the Halo Reach Renaissance release notes.",
            () => _config.NewsReach ? "Enabled" : "Disabled",
            d => { _config.NewsReach = !_config.NewsReach; SaveConfig(); PushClassicState(); PushNews(false); });
        Add("Open Community Edition news",
            "Include the Open Community Edition release notes.",
            () => _config.NewsOce ? "Enabled" : "Disabled",
            d => { _config.NewsOce = !_config.NewsOce; SaveConfig(); PushClassicState(); PushNews(false); });
        Add("Halo 2 Gravemind news",
            "Include the Halo 2 Gravemind release notes.",
            () => _config.NewsGravemind ? "Enabled" : "Disabled",
            d => { _config.NewsGravemind = !_config.NewsGravemind; SaveConfig(); PushClassicState(); PushNews(false); });
        Add("Project Raven news",
            "Include the Project Raven release notes.",
            () => _config.NewsRaven ? "Enabled" : "Disabled",
            d => { _config.NewsRaven = !_config.NewsRaven; SaveConfig(); PushClassicState(); PushNews(false); });

        Header("Chat");
        Add("Account",
            "Open the chat. It is built into the launcher and keeps running in the background, so messages still reach you while you play.",
            () => _chatView?.Username ?? "Not signed in",
            d => { if (d > 0) { CloseSettingsPane(); ShowChat(); } }, btn: "Open chat");
        Add("Stay signed in",
            "Sign in to the chat automatically when the launcher starts, without asking for your login or passphrase. Off forgets the saved login on this PC.",
            () => AHFMCChat.LocalSettings.AutoSignIn ? "Enabled" : "Disabled",
            d => ToggleAutoSignIn());
        Add("Notifications",
            "Show a Windows notification for new direct messages, party messages and invitations while the chat panel is closed or you are in a game.",
            () => AHFMCChat.LocalSettings.Notifications ? "Enabled" : "Disabled",
            d => { AHFMCChat.LocalSettings.Notifications = !AHFMCChat.LocalSettings.Notifications; ChatSettingChanged(); });
        Add("Message previews",
            "Show the text of the message in the notification. Off only says that a message arrived.",
            () => AHFMCChat.LocalSettings.ShowPreviews ? "Show text" : "Hide text",
            d => { AHFMCChat.LocalSettings.ShowPreviews = !AHFMCChat.LocalSettings.ShowPreviews; ChatSettingChanged(); });
        Add("Lobby notifications",
            "Also notify for new messages in the public lobby. It can be busy, so it is off by default.",
            () => AHFMCChat.LocalSettings.LobbyNotifications ? "Enabled" : "Disabled",
            d => { AHFMCChat.LocalSettings.LobbyNotifications = !AHFMCChat.LocalSettings.LobbyNotifications; ChatSettingChanged(); });
        Add("Show over games",
            "When a game (or any full screen window) is in front, show a small popup on top of it for new messages. Windows usually hides its own notifications there. It can't draw over a game in true exclusive full screen.",
            () => AHFMCChat.LocalSettings.OverlayInGame ? "Enabled" : "Disabled",
            d => { AHFMCChat.LocalSettings.OverlayInGame = !AHFMCChat.LocalSettings.OverlayInGame; ChatSettingChanged(); });
        Add("Test notification",
            "Send yourself a test. You should see the popup in the corner of the screen.",
            () => "-",
            d => { if (d > 0) SendTestNotification(); }, btn: "Send test");
        Add("Auto-lock",
            "Lock the chat after a while without using it. You need your passphrase to unlock it again.",
            () => AHFMCChat.LocalSettings.AutoLockMinutes switch { 5 => "5 minutes", 10 => "10 minutes", 30 => "30 minutes", _ => "Never" },
            d =>
            {
                int[] mins = { 0, 5, 10, 30 };
                int i = Math.Max(0, Array.IndexOf(mins, AHFMCChat.LocalSettings.AutoLockMinutes));
                AHFMCChat.LocalSettings.AutoLockMinutes = mins[Wrap(i, d, mins.Length)];
                ChatSettingChanged();
            });
        Add("Warn about unverified contacts",
            "Show a warning in a conversation with someone whose safety code you haven't compared yet.",
            () => AHFMCChat.LocalSettings.WarnUnverified ? "Enabled" : "Disabled",
            d => { AHFMCChat.LocalSettings.WarnUnverified = !AHFMCChat.LocalSettings.WarnUnverified; ChatSettingChanged(); });
        Add("Your fingerprint",
            "Show your fingerprint. Your contacts can compare it with theirs to check that it's really you.",
            () => _chatView == null ? "Sign in first" : "Show...",
            d => { if (d > 0) _ = ShowFingerprintAsync(); }, btn: "Show...");
        Add("Delete all my messages",
            "Delete every message you sent. Direct messages disappear for the other person too.",
            () => "-",
            d => { if (d > 0) _ = ChatAccountActionAsync(AHFMCChat.SettingsAction.DeleteMessages); }, btn: "Delete...");
        Add("Delete my account",
            "Permanently delete your chat account, keys, messages and parties.",
            () => "-",
            d => { if (d > 0) _ = ChatAccountActionAsync(AHFMCChat.SettingsAction.DeleteAccount); }, btn: "Delete...");
        Add("Log out",
            "Log out of the chat on this PC.",
            () => "-",
            d => { if (d > 0) _ = ChatAccountActionAsync(AHFMCChat.SettingsAction.Logout); }, btn: "Log out");

        Header("Games");
        Add("Reclaimer location",
            "Locate: pick the game's .exe yourself. Forget / Uninstall: forget your copy, or uninstall the one the launcher downloaded.",
            () => GameLocation(_config.Halo3Exe, _config.Halo3Manual),
            d => { if (d > 0) LocateGame(false); else ForgetOrUninstall("halo3"); }, btns: new[] { "Locate...", "Forget / Uninstall" });
        Add("Halo Reach Renaissance location",
            "Locate: pick \"Reach Renaissance.exe\" yourself. Forget / Uninstall: forget it and let the launcher download the game.",
            () => GameLocation(_config.ReachExe, _config.ReachManual),
            d => { if (d > 0) LocateGame(true); else ForgetOrUninstall("reach"); }, btns: new[] { "Locate...", "Forget / Uninstall" });
        Add("Open Community Edition location",
            "Locate: pick the game's .exe yourself. Forget / Uninstall: forget it and let the launcher download the latest version.",
            () => GameLocation(_config.OceExe, _config.OceManual),
            d => { if (d > 0) LocateOce(); else ForgetOrUninstall("oce"); }, btns: new[] { "Locate...", "Forget / Uninstall" });
        Add("Halo 2 Gravemind location",
            "Locate: pick the game's .exe yourself. Forget / Uninstall: forget it and let the launcher download the game.",
            () => GameLocation(_config.GravemindExe, _config.GravemindManual),
            d => { if (d > 0) LocateGravemind(); else ForgetOrUninstall("gravemind"); }, btns: new[] { "Locate...", "Forget / Uninstall" });
        Add("Project Raven location",
            "Locate: pick the Project Raven .exe yourself. Forget / Uninstall: forget it and let the launcher download it. Project Raven asks for your Fireteam Raven game folder itself the first time it runs.",
            () => GameLocation(_config.RavenExe, _config.RavenManual),
            d => { if (d > 0) LocateRaven(); else ForgetOrUninstall("raven"); }, btns: new[] { "Locate...", "Forget / Uninstall" });
        Add("Project Groundside location",
            "Locate: pick the Project Groundside .exe yourself. Forget / Uninstall: forget it and let the launcher download it.",
            () => GameLocation(_config.GroundsideExe, _config.GroundsideManual),
            d => { if (d > 0) LocateGroundside(); else ForgetOrUninstall("groundside"); }, btns: new[] { "Locate...", "Forget / Uninstall" });

        Header("My Apps");
        Add("Add an app",
            "Pick a program, give it your own name and, if you like, a picture. The launcher comes back when the program closes.",
            () => _config.CustomApps.Count == 0 ? "None yet" : _config.CustomApps.Count + " added",
            d => { if (d > 0) AddCustomApp(); }, btn: "Add...");
        foreach (var app in _config.CustomApps.ToList())
        {
            string aid = app.Id;
            Add(app.Name,
                "Rename: give it a different name. Remove: take it off the launcher.",
                () => FindApp(aid)?.Name ?? "",
                d => { if (d > 0) RenameCustomApp(aid); else RemoveCustomApp(aid); }, btns: new[] { "Rename...", "Remove" });
            Add("    " + app.Name + " file",
                "Pick a different file for this app.",
                () => FindApp(aid) is { } x && File.Exists(x.Path) ? Path.GetFileName(x.Path) : "File missing",
                d => { if (d > 0) LocateCustomApp(aid); }, btn: "Pick file...");
            Add("    " + app.Name + " picture",
                "Pick photo: choose a picture for this app. Remove: go back to the default look.",
                () => FindApp(aid) is { } x && AppImagePath(x) != null ? "Picture set" : "No picture",
                d => { if (d > 0) ChangeAppPicture(aid); else RemoveAppPicture(aid); }, btns: new[] { "Pick photo...", "Remove" });
        }

        Header("Game Logos");
        foreach (var (gid, gname) in AllGameList)
        {
            string lid = gid;
            Add(gname + " logo",
                "Pick Logo: choose your own picture for this game (shown in the Classic and Big Picture layouts). Remove: go back to the default.",
                () => GameLogoPath(lid) != null ? "Your logo" : "Default",
                d => { if (d > 0) ChangeGameLogo(lid); else RemoveGameLogo(lid); }, btns: new[] { "Pick logo...", "Remove" });
        }

        Header("Official Games");
        foreach (var (id, name) in OfficialList)
            Add(name,
                "Locate: pick the game's .exe (adds a \"My copy\" button). Forget: stop using your own copy.",
                () => HasLocated(id) ? "Yours: " + Path.GetFileName(_config.OfficialExe[id]) : "Steam / Store",
                d => { if (d > 0) LocateOfficial(id); else ForgetOfficial(id); }, btns: new[] { "Locate...", "Forget" });

        Header("Credits and contact");
        void Link(string label, string desc, string url, string button = "Open...") =>
            Add(label, desc, () => "-", d => { if (d > 0) OpenSettingsLink(url); }, btn: button);
        Link("Made by WhyWasThisNotAThing", "The launcher is a free, fan-made project. Open the maker's GitHub page.", "https://github.com/WhyWasThisNotAThing");
        Link("This project on GitHub", "Source code, releases and issues.", "https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection");
        Link("Reclaimer", "Made by its own creators. Downloaded from the Project Reclaimer releases.", "https://github.com/ProjectReclaimer/project-reclaimer-releases");
        Link("Halo Reach Renaissance", "Made by its own creators. Downloaded from the NETF1ix releases.", "https://github.com/NETF1ix/reach-renaissance-releases");
        Link("Halo 2 Gravemind", "Made by its own creators. Downloaded from h2gravemind-releases (BriRoz1).", "https://github.com/BriRoz1/h2gravemind-releases/releases/");
        Link("Project Raven", "Made by bryantmh. Downloaded from the Project Raven releases on GitHub.", "https://github.com/bryantmh/project-raven/releases");
        Link("Open Community Edition", "Made by its own creators. Open the Open Community Edition website.", "https://opencommunityedition.com/");
        Link("Project Groundside", "Made by its own creators. Join the Project Groundside Discord server.", GroundsideDiscord, "Join Discord...");
        Add("Contact me",
            "Send an email to the maker of this launcher. Questions, ideas and bug reports are welcome. Never send passwords, passphrases or private messages.",
            () => ContactEmail,
            d => { if (d > 0) OpenMail(); }, btn: "Send email...");

        ShowCategory(_cats[Math.Clamp(_selCat, 0, _cats.Count - 1)]);
        RefreshSettings();
        PushClassicState();
    }

    private const string ContactEmail = "whywasthisnotathing@gmail.com";
    private const string GroundsideDiscord = "https://discord.gg/nRusWYMdYu";

    private void OpenSettingsLink(string url)
    {
        if (SettingsPane.Visibility == Visibility.Visible) CloseSettingsPane();
        _ = OpenInAppAsync(url, false);
    }

    private void OpenMail()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("mailto:" + ContactEmail + "?subject=" + Uri.EscapeDataString("AHFMC launcher")) { UseShellExecute = true });
        }
        catch
        {
            try { System.Windows.Clipboard.SetText(ContactEmail); } catch { }
            _ = ShowDialogAsync("Contact me", "Couldn't open your email app. The address is " + ContactEmail + " (it was copied to the clipboard).", "OK", null);
        }
    }

    private object[] SettingsModel()
    {
        string Safe(Func<string> f) { try { return f(); } catch { return ""; } }
        return _cats.Select(c => (object)new
        {
            name = c.Name,
            items = _settings.Select((d, i) => (d, i)).Where(x => x.d.Cat == c.Name).Select(x =>
            {
                string v = Safe(x.d.Value);
                return new { i = x.i, label = x.d.Label, desc = x.d.Desc, kind = x.d.Kind, value = v, on = x.d.Kind == "toggle" && v == x.d.On, btns = x.d.Btns, drop = x.d.Drop };
            }).ToArray()
        }).ToArray();
    }

    private bool _menuSoundsOn;
    private long _lastMouseDown;
    private readonly Dictionary<string, System.Windows.Media.MediaPlayer> _menuPlayers = new();

    private bool MenuSoundsApply() => _menuSoundsOn && !_counting && ClassicPane.Visibility != Visibility.Visible && ChatPane.Visibility != Visibility.Visible;

    private void InitMenuSounds()
    {
        PreviewMouseDown += (_, _) => _lastMouseDown = Environment.TickCount64;
        EventManager.RegisterClassHandler(typeof(Button), Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler((s, e) =>
        {
            if (s is not Button b || e.OldFocus == null || !MenuSoundsApply()) return;
            if (Environment.TickCount64 - _lastMouseDown < 250) return;
            if (!ReferenceEquals(Window.GetWindow(b), this)) return;
            PlayMenuSound("cursor");
        }));
        EventManager.RegisterClassHandler(typeof(Button), System.Windows.Controls.Primitives.ButtonBase.ClickEvent, new RoutedEventHandler((s, e) =>
        {
            if (s is not Button b || !MenuSoundsApply()) return;
            if (!ReferenceEquals(Window.GetWindow(b), this)) return;
            PlayMenuSound("fwd");
        }));
    }

    private void PlayBackIfPaneOpen()
    {
        if (!MenuSoundsApply()) return;
        if (NewsPane.Visibility == Visibility.Visible || CreditsPane.Visibility == Visibility.Visible || SettingsPane.Visibility == Visibility.Visible
            || DialogPane.Visibility == Visibility.Visible || BrowserPane.Visibility == Visibility.Visible)
            PlayMenuSound("back");
    }

    private void PlayMenuSound(string key)
    {
        try
        {
            if (!MenuSoundsApply() || _config.MasterVolume <= 0 || _config.MenuVolume <= 0) return;
            double gain = AHFMCChat.SoundEngine.Curve(_config.MasterVolume) * AHFMCChat.SoundEngine.Curve(_config.MenuVolume);
            string? custom = MenuSoundPath(key);
            if (custom != null && !custom.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                string id = key + "|" + custom;
                if (!_menuPlayers.TryGetValue(id, out var mp))
                {
                    mp = new System.Windows.Media.MediaPlayer();
                    mp.Open(new Uri(custom));
                    _menuPlayers[id] = mp;
                }
                mp.Stop();
                mp.Volume = Math.Min(1.0, gain);
                mp.Play();
                return;
            }
            string file = key switch { "cursor" => "move", "fwd" => "select", _ => "back" };
            byte[]? wav = custom != null ? File.ReadAllBytes(custom) : AHFMCChat.SoundEngine.Resource("ClassicHalo\\assets\\ui\\sfx-" + file + ".wav");
            if (wav != null) AHFMCChat.SoundEngine.Play("menu", wav, gain);
        }
        catch { }
    }

    private bool HasGravemind => _config.GravemindExe != null && File.Exists(_config.GravemindExe);

    private void UpdateGravemindText()
    {
        GravemindSub.Text = HasGravemind
            ? (_config.GravemindManual ? "Installed: your own copy" : $"Ready to play {_config.GravemindVersion}".TrimEnd())
            : "Not installed: click to download the latest version";
        if (HasGravemind) GravemindSub.Text += PlayedText("Halo 2 Gravemind");
        Install.SetState(GravemindBtn, HasGravemind ? "Installed" : "Not installed");
    }

    private void LocateGravemind()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Find the Halo 2 Gravemind .exe",
            Filter = "Programs (*.exe)|*.exe",
            CheckFileExists = true
        };
        ShowMouse();
        Topmost = false;
        bool ok = dlg.ShowDialog(this) == true;
        Topmost = _config.WindowMode == 0;
        Activate();
        if (!ok) return;
        _config.GravemindExe = dlg.FileName;
        _config.GravemindManual = true;
        SaveConfig();
        UpdateSubtitles();
        RefreshSettings();
    }

    private async void ForgetGravemind()
    {
        if (!_config.GravemindManual) return;
        if (!await ShowDialogAsync("Forget this location?", "The launcher will download the game itself the next time you start it.", "Forget it", "Keep it")) return;
        _config.GravemindExe = null; _config.GravemindManual = false; _config.GravemindVersion = null;
        SaveConfig();
        UpdateSubtitles();
        RefreshSettings();
    }

    private async void Gravemind_Click(object sender, RoutedEventArgs e) => await PlayGravemindAsync();

    private async Task PlayGravemindAsync()
    {
        if (_counting || _busy) return;
        try
        {
            await RunWithCountdownAsync(true, PrepareGravemindAsync);

            if (_config.GravemindExe == null || !File.Exists(_config.GravemindExe))
                throw new FileNotFoundException("Could not find the executable target. Please check file properties.");

            Launch(_config.GravemindExe!, "Halo 2 Gravemind");
        }
        catch (Exception ex) { ShowError(ex); }
        finally { SetBusy(false, " "); }
    }


    private bool HasRaven => _config.RavenExe != null && File.Exists(_config.RavenExe);

    private void UpdateRavenText()
    {
        RavenSub.Text = HasRaven
            ? (_config.RavenManual ? "Installed: your own copy" : $"Ready to play {_config.RavenVersion}".TrimEnd())
            : "Not installed: click to download the latest version";
        if (HasRaven) RavenSub.Text += PlayedText("Project Raven");
        Install.SetState(RavenBtn, HasRaven ? "Installed" : "Not installed");
    }

    private void LocateRaven()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Find the Project Raven .exe",
            Filter = "Programs (*.exe)|*.exe",
            CheckFileExists = true
        };
        ShowMouse();
        Topmost = false;
        bool ok = dlg.ShowDialog(this) == true;
        Topmost = _config.WindowMode == 0;
        Activate();
        if (!ok) return;
        _config.RavenExe = dlg.FileName;
        _config.RavenManual = true;
        SaveConfig();
        UpdateSubtitles();
        RefreshSettings();
    }

    private async void ForgetRaven()
    {
        if (!_config.RavenManual) return;
        if (!await ShowDialogAsync("Forget this location?", "The launcher will download the game itself the next time you start it.", "Forget it", "Keep it")) return;
        _config.RavenExe = null; _config.RavenManual = false; _config.RavenVersion = null;
        SaveConfig();
        UpdateSubtitles();
        RefreshSettings();
    }

    private async void Raven_Click(object sender, RoutedEventArgs e) => await PlayRavenAsync();

    private async Task PlayRavenAsync()
    {
        if (_counting || _busy) return;
        try
        {
            await RunWithCountdownAsync(true, PrepareRavenAsync);

            if (_config.RavenExe == null || !File.Exists(_config.RavenExe))
                throw new FileNotFoundException("Could not find the executable target. Please check file properties.");

            Launch(_config.RavenExe!, "Project Raven");
        }
        catch (Exception ex) { ShowError(ex); }
        finally { SetBusy(false, " "); }
    }

    private const string GroundsideUrl = "https://downloads.projectgroundside.com/launcher/ProjectGroundside.exe";
    private bool HasGroundside => _config.GroundsideExe != null && File.Exists(_config.GroundsideExe);

    private void UpdateGroundsideText()
    {
        GroundsideSub.Text = HasGroundside
            ? (_config.GroundsideManual ? "Installed: your own copy" : "Ready to play")
            : "Not installed: click to install";
        if (HasGroundside) GroundsideSub.Text += PlayedText("Project Groundside");
        Install.SetState(GroundsideBtn, HasGroundside ? "Installed" : "Not installed");
    }

    private void LocateGroundside()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Find the Project Groundside .exe",
            Filter = "Programs (*.exe)|*.exe",
            CheckFileExists = true
        };
        ShowMouse();
        Topmost = false;
        bool ok = dlg.ShowDialog(this) == true;
        Topmost = _config.WindowMode == 0;
        Activate();
        if (!ok) return;
        _config.GroundsideExe = dlg.FileName;
        _config.GroundsideManual = true;
        _config.GroundsideSha = null; _config.GroundsideStamp = null;
        SaveConfig();
        UpdateSubtitles();
        RefreshSettings();
    }

    private async void ForgetGroundside()
    {
        if (!_config.GroundsideManual) return;
        if (!await ShowDialogAsync("Forget this location?", "The launcher will download Project Groundside itself the next time you start it.", "Forget it", "Keep it")) return;
        _config.GroundsideExe = null; _config.GroundsideManual = false; _config.GroundsideSha = null; _config.GroundsideStamp = null;
        SaveConfig();
        UpdateSubtitles();
        RefreshSettings();
    }

    private async void Groundside_Click(object sender, RoutedEventArgs e) => await PlayGroundsideAsync(false);

    private static string? GroundsideStampOf(HttpResponseMessage r) =>
        r.Headers.ETag?.Tag ?? r.Content.Headers.LastModified?.UtcDateTime.ToString("O") ?? r.Content.Headers.ContentLength?.ToString();

    private async Task<string?> GroundsideStampAsync()
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Head, GroundsideUrl);
            req.Headers.Accept.ParseAdd("*/*");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            using var resp = await Http.SendAsync(req, cts.Token);
            return resp.IsSuccessStatusCode ? GroundsideStampOf(resp) : null;
        }
        catch { return null; }
    }

    private async Task<(string Sha, string? Stamp)> GroundsideDownloadAsync(string tmp)
    {
        var ct = _dlCts?.Token ?? default;
        using var req = new HttpRequestMessage(HttpMethod.Get, GroundsideUrl);
        req.Headers.Accept.ParseAdd("*/*");
        using var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        var uri = resp.RequestMessage?.RequestUri;
        if (uri == null || uri.Scheme != Uri.UriSchemeHttps || !uri.Host.EndsWith("projectgroundside.com", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The download was sent somewhere other than projectgroundside.com, so the launcher stopped it.");

        long? total = resp.Content.Headers.ContentLength;
        string? stamp = GroundsideStampOf(resp);
        long read = 0;
        string sha;
        using (var hash = System.Security.Cryptography.SHA256.Create())
        {
            await using var src = await resp.Content.ReadAsStreamAsync(ct);
            await using var fs = File.Create(tmp);
            var buf = new byte[81920];
            int n;
            while ((n = await src.ReadAsync(buf, ct)) > 0)
            {
                await fs.WriteAsync(buf.AsMemory(0, n), ct);
                hash.TransformBlock(buf, 0, n, null, 0);
                read += n;
                if (total > 0) ReportProgress(read * 100.0 / total.Value);
            }
            hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            sha = Convert.ToHexString(hash.Hash!);
        }

        if (total != null && read != total) throw new InvalidDataException("The download did not finish (the file is incomplete).");
        byte[] head = new byte[2];
        using (var f = File.OpenRead(tmp)) { if (f.Read(head, 0, 2) < 2 || head[0] != (byte)'M' || head[1] != (byte)'Z') throw new InvalidDataException("The downloaded file is not a Windows program."); }
        return (sha, stamp);
    }

    private static string GroundsideFileSha(string file)
    {
        using var f = File.OpenRead(file);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(f));
    }

    private static string GroundsideSigner(string file)
    {
        try
        {
#pragma warning disable SYSLIB0057
            var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                System.Security.Cryptography.X509Certificates.X509Certificate.CreateFromSignedFile(file));
#pragma warning restore SYSLIB0057
            string name = cert.GetNameInfo(System.Security.Cryptography.X509Certificates.X509NameType.SimpleName, false);
            using var chain = new System.Security.Cryptography.X509Certificates.X509Chain();
            bool trusted = chain.Build(cert);
            return trusted ? $"Signed by {name} (certificate is valid)" : $"Signed by {name}, but Windows does not trust that certificate";
        }
        catch { return "NOT digitally signed (the publisher can't be checked)"; }
    }

    private async Task PlayGroundsideAsync(bool redownload)
    {
        if (_counting || _busy) return;
        string? tmp = null;
        try
        {
            bool manual = HasGroundside && _config.GroundsideManual;
            bool want = !manual && (redownload || !HasGroundside);

            if (!manual && HasGroundside && !want)
            {
                if (_config.GroundsideSha != null && await Task.Run(() => GroundsideFileSha(_config.GroundsideExe!)) != _config.GroundsideSha)
                {
                    if (!await ShowDialogAsync("Project Groundside changed",
                            "The Project Groundside file on this PC is not the same as the one you installed, so the launcher won't start it.\n\nDownload a fresh copy?",
                            "Download again", "Cancel")) return;
                    want = true;
                }
                else
                {
                    string? now = await GroundsideStampAsync();
                    if (now != null)
                    {
                        if (_config.GroundsideStamp == null) { _config.GroundsideStamp = now; SaveConfig(); }
                        else if (now != _config.GroundsideStamp)
                            want = await AskAsync("A newer version of Project Groundside is available.\n\nUpdate now?", "Update available");
                    }
                }
            }

            if (want)
            {
                string dir = Path.Combine(Root, "ProjectGroundside");
                Directory.CreateDirectory(dir);
                string dest = Path.Combine(dir, "ProjectGroundside.exe");
                tmp = dest + ".download";
                SetBusy(true, "Downloading Project Groundside…", determinate: true);
                var (sha, stamp) = await GroundsideDownloadAsync(tmp);
                SetBusy(false, " ");

                string signer = await Task.Run(() => GroundsideSigner(tmp));
                if (!await ShowDialogAsync("Install Project Groundside?",
                        "Downloaded from downloads.projectgroundside.com. It is a separate program made by the Project Groundside team, not by this launcher.\n\n"
                        + signer + "\nFingerprint (SHA-256): " + sha[..16] + "…\n\nOnly continue if you trust it.",
                        "Install it", "Cancel"))
                { try { File.Delete(tmp); } catch { } tmp = null; return; }

                File.Move(tmp, dest, true); tmp = null;
                _config.GroundsideExe = dest;
                _config.GroundsideManual = false;
                _config.GroundsideSha = sha;
                _config.GroundsideStamp = stamp;
                SaveConfig();
                UpdateSubtitles();
            }

            if (!HasGroundside) return;
            SetBusy(false, " ");
            await RunWithCountdownAsync(true, () => Task.CompletedTask);
            Launch(_config.GroundsideExe!, "Project Groundside");
        }
        catch (Exception ex)
        {
            if (tmp != null) try { File.Delete(tmp); } catch { }
            ShowError(ex);
        }
        finally { SetBusy(false, " "); }
    }

    private static readonly Dictionary<string, (string Folder, string Name)> DownloadedGames = new()
    {
        ["halo3"] = ("Halo3Reclaimer", "Reclaimer"),
        ["reach"] = ("HaloReachRenaissance", "Halo Reach Renaissance"),
        ["oce"] = ("OpenCE", "Open Community Edition"),
        ["gravemind"] = ("Halo2Gravemind", "Halo 2 Gravemind"),
        ["raven"] = ("ProjectRaven", "Project Raven"),
        ["groundside"] = ("ProjectGroundside", "Project Groundside"),
    };

    private (string? Exe, bool Manual) GameFiles(string id) => id switch
    {
        "halo3" => (_config.Halo3Exe, _config.Halo3Manual),
        "reach" => (_config.ReachExe, _config.ReachManual),
        "oce" => (_config.OceExe, _config.OceManual),
        "gravemind" => (_config.GravemindExe, _config.GravemindManual),
        "raven" => (_config.RavenExe, _config.RavenManual),
        "groundside" => (_config.GroundsideExe, _config.GroundsideManual),
        _ => (null, false)
    };

    private bool IsRemovable(string id)
    {
        if (!DownloadedGames.TryGetValue(id, out var g)) return false;
        var (exe, manual) = GameFiles(id);
        if (exe == null || manual || !File.Exists(exe)) return false;
        string dir = Path.GetFullPath(Path.Combine(Root, g.Folder)) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(exe).StartsWith(dir, StringComparison.OrdinalIgnoreCase);
    }

    private void ForgetOrUninstall(string id)
    {
        if (IsRemovable(id)) { _ = UninstallGameAsync(id); return; }
        switch (id)
        {
            case "halo3": ForgetLocation(false); break;
            case "reach": ForgetLocation(true); break;
            case "gravemind": ForgetGravemind(); break;
            case "raven": ForgetRaven(); break;
            case "oce": ForgetOce(); break;
            case "groundside": ForgetGroundside(); break;
        }
    }

    private async Task UninstallGameAsync(string id)
    {
        if (_busy || _counting || !IsRemovable(id)) return;
        if (_gameRunning) { ShowError(new IOException("Close the game first, then uninstall it.")); return; }
        var g = DownloadedGames[id];
        if (!await ShowDialogAsync($"Uninstall {g.Name}?",
                "This deletes the game's files from the launcher's folder, including anything the game saved there (settings, saves).\n\nYou can install it again at any time.",
                "Uninstall", "Cancel")) return;
        try
        {
            string dir = Path.Combine(Root, g.Folder);
            await Task.Run(() => { if (Directory.Exists(dir)) Directory.Delete(dir, true); });
        }
        catch (Exception ex)
        {
            UpdateSubtitles();
            ShowError(new IOException("Not every file could be removed. Close the game (and anything using its folder), then try again.\n\n" + ex.Message));
            return;
        }
        switch (id)
        {
            case "halo3": _config.Halo3Exe = null; _config.Halo3Manual = false; _config.Halo3Version = null; break;
            case "reach": _config.ReachExe = null; _config.ReachManual = false; _config.ReachVersion = null; break;
            case "oce": _config.OceExe = null; _config.OceManual = false; _config.OceVersion = null; break;
            case "gravemind": _config.GravemindExe = null; _config.GravemindManual = false; _config.GravemindVersion = null; break;
            case "raven": _config.RavenExe = null; _config.RavenManual = false; _config.RavenVersion = null; break;
            case "groundside": _config.GroundsideExe = null; _config.GroundsideManual = false; _config.GroundsideSha = null; _config.GroundsideStamp = null; break;
        }
        SaveConfig();
        UpdateSubtitles();
        RefreshSettings();
    }

    private string? _playName;
    private DateTime _playStart;

    private void BeginPlay(string game) { _playName = game; _playStart = DateTime.UtcNow; }

    private void EndPlay()
    {
        if (_playName == null) return;
        string g = StatKey(_playName);
        _playName = null;
        long secs = (long)(DateTime.UtcNow - _playStart).TotalSeconds;
        if (secs < 5) return;
        if (!_config.PlayStats.TryGetValue(g, out var st)) _config.PlayStats[g] = st = new PlayStat();
        st.Seconds += secs;
        st.Last = DateTime.UtcNow;
        SaveConfig();
    }

    private string OfficialPlayed(string id)
    {
        string t = PlayedText(OfficialName(id));
        return t.Length == 0 ? "" : t.Replace(" · ", "");
    }

    private void UpdateOfficialPlayed()
    {
        void Set(TextBlock tb, string id)
        {
            string t = OfficialPlayed(id);
            tb.Text = t;
            tb.Visibility = t.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        Set(PlayedMcc, "mcc"); Set(PlayedForge, "h5forge"); Set(PlayedInfinite, "infinite");
        Set(PlayedCampaignEvolved, "ce2026"); Set(PlayedHwde, "hwde"); Set(PlayedHw2, "hw2");
        Set(PlayedSpartanAssault, "spartanassault"); Set(PlayedSpartanStrike, "spartanstrike");
    }

    private Dictionary<string, string> OfficialPlayedAll() =>
        OfficialList.ToDictionary(x => x.Id, x => OfficialPlayed(x.Id));

    // The game used to be called "Halo 3 Reclaimer". Play time is still saved under that name so nobody loses it.
    private static string StatKey(string name) => name == "Reclaimer" ? "Halo 3 Reclaimer" : name;

    private string PlayedText(string game)
    {
        game = StatKey(game);
        if (!_config.PlayStats.TryGetValue(game, out var st) || st.Seconds < 30) return "";
        var t = TimeSpan.FromSeconds(st.Seconds);
        string played = t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes}m" : $"{Math.Max(1, (int)t.TotalMinutes)} min";
        var last = st.Last.ToLocalTime();
        int days = (DateTime.Now.Date - last.Date).Days;
        string when = days <= 0 ? "today" : days == 1 ? "yesterday" : days < 30 ? $"{days} days ago" : last.ToString("d MMM yyyy");
        return $" · Played {played}, last played {when}";
    }

    private Button? _addAppSettingBtn;

    private CustomApp? FindApp(string id) => _config.CustomApps.FirstOrDefault(a => a.Id == id);

    private string? PickAppFile(string title)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            Filter = "Programs (*.exe;*.lnk;*.bat;*.cmd)|*.exe;*.lnk;*.bat;*.cmd|All files (*.*)|*.*",
            CheckFileExists = true
        };
        ShowMouse();
        Topmost = false;
        bool ok = dlg.ShowDialog(this) == true;
        Topmost = _config.WindowMode == 0;
        Activate();
        return ok ? dlg.FileName : null;
    }

    private void AddApp_Click(object sender, RoutedEventArgs e) => AddCustomApp();

    private async void AddCustomApp()
    {
        var file = PickAppFile("Pick the app or game to add");
        if (file == null) return;
        string name = "";
        try { name = FileVersionInfo.GetVersionInfo(file).ProductName?.Trim() ?? ""; } catch { }
        if (name.Length == 0 || name.Length > 32 || name.Contains("Microsoft") || name.Contains("Windows"))
            name = Path.GetFileNameWithoutExtension(file);
        var chosen = await PromptTextAsync("Name your app", "What should it be called in the launcher?", name);
        if (chosen == null) return;
        var app = new CustomApp { Id = Guid.NewGuid().ToString("N")[..8], Name = chosen, Path = file };
        if (await ShowDialogAsync("Add a picture?", "Pick a photo for " + chosen + " so it has its own picture in the launcher. You can skip this and add or change it later.", "Pick a photo...", "Skip"))
        {
            var img = PickAppImage("Pick a picture for " + chosen);
            if (img != null) SetAppImage(app, img);
        }
        _config.CustomApps.Add(app);
        SaveConfig();
        AfterAppsChanged();
    }

    private static string AppImagesFolder => Path.Combine(Root, "AppImages");

    private string? PickAppImage(string title)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = title,
            Filter = "Pictures (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.ico)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.ico",
            CheckFileExists = true
        };
        ShowMouse();
        Topmost = false;
        bool ok = dlg.ShowDialog(this) == true;
        Topmost = _config.WindowMode == 0;
        Activate();
        return ok ? dlg.FileName : null;
    }

    private void SetAppImage(CustomApp app, string source)
    {
        try
        {
            Directory.CreateDirectory(AppImagesFolder);
            string name = app.Id + "_" + DateTime.UtcNow.Ticks + Path.GetExtension(source).ToLowerInvariant();
            File.Copy(source, Path.Combine(AppImagesFolder, name), true);
            DeleteAppImage(app);
            app.Image = name;
        }
        catch (Exception ex) { ShowError(new Exception("Couldn't use that picture.\n\n" + ex.Message)); }
    }

    private static void DeleteAppImage(CustomApp app)
    {
        try { if (app.Image.Length > 0) File.Delete(Path.Combine(AppImagesFolder, app.Image)); } catch { }
        app.Image = "";
    }

    private static string? AppImagePath(CustomApp app)
    {
        if (app.Image.Length == 0) return null;
        string p = Path.Combine(AppImagesFolder, app.Image);
        return File.Exists(p) ? p : null;
    }

    private static string? AppImageUrl(CustomApp app) => AppImagePath(app) is null ? null : "https://halo.apps/" + Uri.EscapeDataString(app.Image);

    private static ImageSource? LoadAppImage(string path)
    {
        try
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.DecodePixelWidth = 200;
            bi.UriSource = new Uri(path);
            bi.EndInit();
            bi.Freeze();
            return bi;
        }
        catch { return null; }
    }

    private void ChangeAppPicture(string id)
    {
        var app = FindApp(id);
        if (app == null) return;
        var file = PickAppImage("Pick a picture for " + app.Name);
        if (file == null) return;
        SetAppImage(app, file);
        SaveConfig();
        AfterAppsChanged();
    }

    private void RemoveAppPicture(string id)
    {
        var app = FindApp(id);
        if (app == null) return;
        DeleteAppImage(app);
        SaveConfig();
        AfterAppsChanged();
    }

    private static readonly (string Key, string Name)[] MenuSoundList =
    {
        ("cursor", "Move"), ("fwd", "Select"), ("back", "Back")
    };

    private static readonly (string Key, string Name)[] CountSoundList =
    {
        ("countbeep", "3, 2, 1 beep"), ("countgo", "GO")
    };

    private static IEnumerable<(string Key, string Name)> AllSoundList => MenuSoundList.Concat(CountSoundList);

    private static string MenuSoundsFolder => Path.Combine(Root, "MenuSounds");

    private string? MenuSoundPath(string key)
    {
        if (!_config.MenuSounds.TryGetValue(key, out var n) || n.Length == 0) return null;
        string p = Path.Combine(MenuSoundsFolder, n);
        return File.Exists(p) ? p : null;
    }

    private Dictionary<string, string> MenuSoundUrls()
    {
        var d = new Dictionary<string, string>();
        foreach (var (key, _) in MenuSoundList)
            if (MenuSoundPath(key) != null) d[key] = "https://halo.menusounds/" + Uri.EscapeDataString(_config.MenuSounds[key]);
        return d;
    }

    private void ChangeMenuSound(string key)
    {
        var item = AllSoundList.FirstOrDefault(x => x.Key == key);
        if (item.Key == null) return;
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = key.StartsWith("count") ? "Pick a countdown sound for \"" + item.Name + "\" (short .wav, .mp3 or .ogg)" : "Pick a sound for \"" + item.Name + "\" (short .wav, .mp3 or .ogg)",
            Filter = "Sounds (*.wav;*.mp3;*.ogg)|*.wav;*.mp3;*.ogg",
            CheckFileExists = true
        };
        ShowMouse();
        Topmost = false;
        bool ok = dlg.ShowDialog(this) == true;
        Topmost = _config.WindowMode == 0;
        Activate();
        if (!ok) return;
        try
        {
            if (new FileInfo(dlg.FileName).Length > 3 * 1024 * 1024)
            { ShowError(new Exception("That sound is too big. Pick a short one (under 3 MB). These sounds should only last a moment.")); return; }
            Directory.CreateDirectory(MenuSoundsFolder);
            string name = key + "_" + DateTime.UtcNow.Ticks + Path.GetExtension(dlg.FileName).ToLowerInvariant();
            File.Copy(dlg.FileName, Path.Combine(MenuSoundsFolder, name), true);
            RemoveMenuSoundFile(key);
            _config.MenuSounds[key] = name;
        }
        catch (Exception ex) { ShowError(new Exception("Couldn't use that sound.\n\n" + ex.Message)); return; }
        SaveConfig();
        AfterAppsChanged();
        if (key.StartsWith("count")) PlayCountSound(key == "countgo");
    }

    private void RemoveMenuSoundFile(string key)
    {
        foreach (var id in _menuPlayers.Keys.Where(k => k.StartsWith(key + "|")).ToList())
        {
            try { _menuPlayers[id].Close(); } catch { }
            _menuPlayers.Remove(id);
        }
        try { if (_config.MenuSounds.TryGetValue(key, out var n) && n.Length > 0) File.Delete(Path.Combine(MenuSoundsFolder, n)); } catch { }
        _config.MenuSounds.Remove(key);
    }

    private void ClearMenuSound(string key)
    {
        if (!AllSoundList.Any(x => x.Key == key)) return;
        RemoveMenuSoundFile(key);
        SaveConfig();
        AfterAppsChanged();
    }

    private static readonly (string Id, string Name)[] CommunityList =
    {
        ("reach", "Halo Reach Renaissance"), ("halo3", "Reclaimer"), ("gravemind", "Halo 2 Gravemind"),
        ("oce", "Open Community Edition"), ("groundside", "Project Groundside"), ("raven", "Project Raven")
    };

    private static IEnumerable<(string Id, string Name)> AllGameList => OfficialList.Concat(CommunityList);

    private string? GameLogoPath(string id)
    {
        if (!_config.GameLogos.TryGetValue(id, out var n) || n.Length == 0) return null;
        string p = Path.Combine(AppImagesFolder, n);
        return File.Exists(p) ? p : null;
    }

    private void DeleteGameLogoFile(string id)
    {
        try { if (_config.GameLogos.TryGetValue(id, out var n) && n.Length > 0) File.Delete(Path.Combine(AppImagesFolder, n)); } catch { }
        _config.GameLogos.Remove(id);
    }

    private void ChangeGameLogo(string id)
    {
        var game = AllGameList.FirstOrDefault(x => x.Id == id);
        if (game.Id == null) return;
        var file = PickAppImage("Pick a logo for " + game.Name);
        if (file == null) return;
        try
        {
            Directory.CreateDirectory(AppImagesFolder);
            string name = "game_" + id + "_" + DateTime.UtcNow.Ticks + Path.GetExtension(file).ToLowerInvariant();
            File.Copy(file, Path.Combine(AppImagesFolder, name), true);
            DeleteGameLogoFile(id);
            _config.GameLogos[id] = name;
        }
        catch (Exception ex) { ShowError(new Exception("Couldn't use that picture.\n\n" + ex.Message)); return; }
        SaveConfig();
        AfterAppsChanged();
    }

    private void RemoveGameLogo(string id)
    {
        if (!AllGameList.Any(x => x.Id == id)) return;
        DeleteGameLogoFile(id);
        SaveConfig();
        AfterAppsChanged();
    }

    private async void RenameCustomApp(string id)
    {
        var app = FindApp(id);
        if (app == null) return;
        var chosen = await PromptTextAsync("Rename app", "Give " + app.Name + " a new name.", app.Name);
        if (chosen == null) return;
        app.Name = chosen;
        SaveConfig();
        AfterAppsChanged();
    }

    private async Task<string?> PromptTextAsync(string title, string message, string prefill)
    {
        bool ok = await ShowDialogAsync(title, message, "Save", "Cancel", prefill);
        string text = DialogInput.Text.Trim();
        return ok && text.Length > 0 ? text : null;
    }

    private void LocateCustomApp(string id)
    {
        var app = FindApp(id);
        if (app == null) return;
        var file = PickAppFile("Find " + app.Name);
        if (file == null) return;
        app.Path = file;
        SaveConfig();
        AfterAppsChanged();
    }

    private async void RemoveCustomApp(string id)
    {
        var app = FindApp(id);
        if (app == null) return;
        if (!await ShowDialogAsync("Remove this app?", app.Name + " will be taken off the launcher. The program itself is not deleted.", "Remove it", "Keep it")) return;
        DeleteAppImage(app);
        _config.CustomApps.Remove(app);
        SaveConfig();
        AfterAppsChanged();
    }

    private async Task LaunchCustomAppAsync(string id)
    {
        if (_counting || _busy) return;
        var app = FindApp(id);
        if (app == null) return;
        if (!File.Exists(app.Path)) { ShowError(new Exception($"\"{app.Name}\" isn't there any more. Pick it again in Settings > My apps.")); return; }
        try
        {
            await RunWithCountdownAsync(true, () => Task.CompletedTask);
            Launch(app.Path, app.Name, watchFolder: false);
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void CustomApp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string t } && t.StartsWith("app:")) await LaunchCustomAppAsync(t[4..]);
    }

    private void AfterAppsChanged()
    {
        RebuildMyApps();
        BuildSettings();
        PushClassicState();
        if (SettingsPane.Visibility == Visibility.Visible)
            Dispatcher.BeginInvoke(new Action(() => (_addAppSettingBtn ?? SettingsClose).Focus()), DispatcherPriority.Input);
    }

    private void RebuildMyApps()
    {
        MyAppsList.Children.Clear();
        foreach (var app in _config.CustomApps)
        {
            string aid = app.Id;
            bool ok = File.Exists(app.Path);
            var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(new TextBlock { Style = (Style)FindResource("RowTitle"), Text = app.Name });
            sp.Children.Add(new TextBlock { Style = (Style)FindResource("RowSub"), Text = ok ? Path.GetFileName(app.Path) : "File missing: pick it again in Settings" });
            object content = sp;
            if (AppImagePath(app) is { } imgPath && LoadAppImage(imgPath) is { } src)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal };
                row.Children.Add(new Border
                {
                    Width = 64, Height = 64, CornerRadius = new CornerRadius(8), Margin = new Thickness(0, 0, 14, 0), ClipToBounds = true,
                    Background = new ImageBrush(src) { Stretch = Stretch.UniformToFill }
                });
                row.Children.Add(sp);
                content = row;
            }
            var btn = new Button { Style = (Style)FindResource("Card"), Content = content, Tag = "app:" + aid };
            Install.SetState(btn, ok ? "Installed" : "Not installed");
            btn.Click += CustomApp_Click;
            var cm = new ContextMenu();
            var pick = new MenuItem { Header = "Pick a different file..." }; pick.Click += (_, _) => LocateCustomApp(aid);
            var ren = new MenuItem { Header = "Rename..." }; ren.Click += (_, _) => RenameCustomApp(aid);
            var pic = new MenuItem { Header = "Change picture..." }; pic.Click += (_, _) => ChangeAppPicture(aid);
            var nopic = new MenuItem { Header = "Remove picture" }; nopic.Click += (_, _) => RemoveAppPicture(aid);
            var del = new MenuItem { Header = "Remove from launcher" }; del.Click += (_, _) => RemoveCustomApp(aid);
            cm.Items.Add(ren); cm.Items.Add(pic);
            if (app.Image.Length > 0) cm.Items.Add(nopic);
            cm.Items.Add(pick); cm.Items.Add(del);
            btn.ContextMenu = cm;
            MyAppsList.Children.Add(btn);
        }
        MyAppsHint.Visibility = _config.CustomApps.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static readonly (string Id, string Name)[] OfficialList =
    {
        ("mcc", "Halo: The Master Chief Collection"), ("h5forge", "Halo 5: Forge"), ("infinite", "Halo Infinite"),
        ("ce2026", "Halo: Campaign Evolved (2026)"), ("hwde", "Halo Wars: Definitive Edition"), ("hw2", "Halo Wars 2"),
        ("spartanassault", "Halo: Spartan Assault"), ("spartanstrike", "Halo: Spartan Strike")
    };

    private bool HasLocated(string id) => _config.OfficialExe.TryGetValue(id, out var p) && File.Exists(p);

    private Dictionary<string, string> LocatedNames() =>
        _config.OfficialExe.Where(kv => File.Exists(kv.Value)).ToDictionary(kv => kv.Key, kv => Path.GetFileName(kv.Value));

    private string OfficialName(string id) => OfficialList.First(x => x.Id == id).Name;

    private void LocateOfficial(string id)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Find " + OfficialName(id) + " (.exe)",
            Filter = "Programs (*.exe)|*.exe",
            CheckFileExists = true
        };
        ShowMouse();
        Topmost = false;
        bool ok = dlg.ShowDialog(this) == true;
        Topmost = _config.WindowMode == 0;
        Activate();
        if (!ok) return;
        _config.OfficialExe[id] = dlg.FileName;
        SaveConfig();
        AfterLocateChanged();
    }

    private async void ForgetOfficial(string id)
    {
        if (!HasLocated(id) && !_config.OfficialExe.ContainsKey(id)) return;
        if (!await ShowDialogAsync("Forget this location?", OfficialName(id) + " will go back to starting through Steam or the Microsoft Store.", "Forget it", "Keep it")) return;
        _config.OfficialExe.Remove(id);
        SaveConfig();
        AfterLocateChanged();
    }

    private void AfterLocateChanged()
    {
        RefreshOfficialPills();
        PushClassicState();
        RefreshSettings();
    }

    private async Task LaunchLocatedAsync(string id)
    {
        if (_counting || _busy) return;
        if (!HasLocated(id)) { ShowError(new Exception("That file isn't there any more. Locate the game again in Settings.")); return; }
        try
        {
            await RunWithCountdownAsync(false, () => Task.CompletedTask);
            Launch(_config.OfficialExe[id], OfficialName(id));
        }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void MineGame_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag } && tag.StartsWith("mine:")) await LaunchLocatedAsync(tag[5..]);
    }

    private void RefreshOfficialPills()
    {
        MccMineBtn.Visibility = HasLocated("mcc") ? Visibility.Visible : Visibility.Collapsed;

        var idByGame = new Dictionary<string, string>();
        foreach (var kv in OfficialGames)
            foreach (var o in kv.Value)
                if (o.Game.Length > 0) idByGame[o.Game] = kv.Key;

        var panels = new Dictionary<WrapPanel, string>();
        foreach (var b in FindButtons(this).ToList())
            if (b.Tag is string t && t.Contains('|') && b.Parent is WrapPanel wp)
            {
                string game = t[(t.LastIndexOf('|') + 1)..];
                if (idByGame.TryGetValue(game, out var id)) panels[wp] = id;
            }

        foreach (var (wp, id) in panels)
        {
            foreach (var old in wp.Children.OfType<Button>().Where(x => x.Tag is string s && s.StartsWith("mine:")).ToList())
                wp.Children.Remove(old);
            if (!HasLocated(id)) continue;
            var pill = new Button { Style = (Style)FindResource("Pill"), Background = (Brush)FindResource("GoldBrush"), Content = "My copy", Tag = "mine:" + id };
            pill.Click += MineGame_Click;
            Install.SetState(pill, "Installed");
            wp.Children.Add(pill);
        }
        ApplyHideMissing();
    }

    private static string GameLocation(string? exe, bool manual)
    {
        if (exe == null || !File.Exists(exe)) return "Not installed";
        return manual ? "Yours: " + Path.GetFileName(exe) : "Downloaded by launcher";
    }

    private void LocateGame(bool reach)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = reach ? "Find Reach Renaissance.exe" : "Find the Reclaimer .exe",
            Filter = reach ? "Reach Renaissance|Reach Renaissance.exe|Programs (*.exe)|*.exe" : "Programs (*.exe)|*.exe",
            CheckFileExists = true
        };
        ShowMouse();
        Topmost = false;
        bool ok = dlg.ShowDialog(this) == true;
        Topmost = _config.WindowMode == 0;
        Activate();
        if (!ok) return;
        if (reach) { _config.ReachExe = dlg.FileName; _config.ReachManual = true; }
        else { _config.Halo3Exe = dlg.FileName; _config.Halo3Manual = true; }
        SaveConfig();
        UpdateSubtitles();
        RefreshSettings();
    }

    private void LocateOce()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Find the Open Community Edition .exe", Filter = "Programs (*.exe)|*.exe", CheckFileExists = true };
        ShowMouse();
        Topmost = false;
        bool ok = dlg.ShowDialog(this) == true;
        Topmost = _config.WindowMode == 0;
        Activate();
        if (!ok) return;
        _config.OceExe = dlg.FileName; _config.OceManual = true;
        SaveConfig();
        UpdateSubtitles();
        RefreshSettings();
    }

    private async void ForgetOce()
    {
        if (!_config.OceManual) return;
        if (!await ShowDialogAsync("Forget this location?", "The launcher will download the game itself the next time you start it.", "Forget it", "Keep it")) return;
        _config.OceExe = null; _config.OceManual = false; _config.OceVersion = null;
        SaveConfig();
        UpdateSubtitles();
        RefreshSettings();
    }

    private async void ForgetLocation(bool reach)
    {
        bool manual = reach ? _config.ReachManual : _config.Halo3Manual;
        if (!manual) return;
        if (!await ShowDialogAsync("Forget this location?", "The launcher will download the game itself the next time you start it.", "Forget it", "Keep it")) return;
        if (reach) { _config.ReachExe = null; _config.ReachManual = false; _config.ReachVersion = null; }
        else { _config.Halo3Exe = null; _config.Halo3Manual = false; _config.Halo3Version = null; }
        SaveConfig();
        UpdateSubtitles();
        RefreshSettings();
    }

    private bool MoveSpatial(FocusNavigationDirection dir)
    {
        if (Keyboard.FocusedElement is not Button cur) return false;
        var panes = new UIElement[] { NewsPane, CreditsPane, SettingsPane, DialogPane, BrowserPane, ClassicPane, CountdownPane };
        Point Center(Button b) => b.TransformToAncestor(this).Transform(new Point(b.ActualWidth / 2, b.ActualHeight / 2));
        var from = Center(cur);
        Button? best = null;
        double bestScore = double.MaxValue;
        foreach (var b in FindButtons(this))
        {
            if (ReferenceEquals(b, cur) || !b.IsVisible || !b.IsEnabled || !b.Focusable) continue;
            if (panes.Any(p => p.IsAncestorOf(b))) continue;
            var c = Center(b);
            double dx = c.X - from.X, dy = c.Y - from.Y, main, side;
            switch (dir)
            {
                case FocusNavigationDirection.Right: main = dx; side = Math.Abs(dy); break;
                case FocusNavigationDirection.Left: main = -dx; side = Math.Abs(dy); break;
                case FocusNavigationDirection.Down: main = dy; side = Math.Abs(dx); break;
                default: main = -dy; side = Math.Abs(dx); break;
            }
            if (main < 4) continue;
            double score = main + 2 * side;
            if (score < bestScore) { bestScore = score; best = b; }
        }
        return best != null && best.Focus();
    }

    private void RefreshSettings()
    {
        foreach (var d in _settings)
        {
            if (d.ValueText != null) d.ValueText.Text = d.Value();
            d.Refresh?.Invoke();
        }
    }

    private void OpenSettings()
    {
        RefreshSettings();
        SettingsPane.Visibility = Visibility.Visible;
        Dispatcher.BeginInvoke(new Action(() => { SettingsScroll.ScrollToTop(); CurrentCatBtn().Focus(); }), DispatcherPriority.Input);
    }

    private bool _counting;
    private readonly Dictionary<string, byte[]> _sounds = new();

    private bool CountdownApplies(bool fanGame) => _config.Countdown == 2 || (_config.Countdown == 1 && fanGame);

    private async Task RunWithCountdownAsync(bool fanGame, Func<Task> work)
    {
        await work();
        if (!CountdownApplies(fanGame)) return;
        SetBusy(false, " ");

        _counting = true;
        try
        {
            WarmCountSounds();
            for (int n = 3; n >= 1; n--)
            {
                ShowCount(n.ToString(), false);
                PlayCountSound(false);
                await Task.Delay(1000);
            }
            ShowCount("GO", true);
            PlayCountSound(true);
            await Task.Delay(800);
        }
        finally
        {
            HideCount();
            _counting = false;
        }
    }

    private void ShowCount(string text, bool go)
    {
        CountText.Text = text;
        CountText.Foreground = go ? Br("#7BD88F") : Brushes.White;
        CountdownPane.Visibility = Visibility.Visible;
        var pop = new DoubleAnimation(1.5, 1.0, TimeSpan.FromMilliseconds(420)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        CountScale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
        CountScale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
        ClassicSend(new { type = "count", text });
    }

    private void HideCount()
    {
        CountdownPane.Visibility = Visibility.Collapsed;
        CountStatus.Text = " ";
        ClassicSend(new { type = "count", text = "" });
    }

    private void WarmCountSounds()
    {
        try
        {
            foreach (var (key, _) in CountSoundList)
            {
                string? custom = MenuSoundPath(key);
                if (custom == null || custom.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) continue;
                string id = key + "|" + custom;
                if (_menuPlayers.ContainsKey(id)) continue;
                var mp = new System.Windows.Media.MediaPlayer { Volume = 0 };
                mp.Open(new Uri(custom));
                _menuPlayers[id] = mp;
            }
        }
        catch { }
    }

    private void PlayCountSound(bool go)
    {
        string key = go ? "countgo" : "countbeep";
        string fallback = go ? "Sounds\\go.wav" : "Sounds\\beep.wav";
        try
        {
            string? custom = MenuSoundPath(key);
            if (custom == null) { PlaySound(fallback, _config.CountdownVolume); return; }

            double gain = AHFMCChat.SoundEngine.Curve(_config.MasterVolume) * AHFMCChat.SoundEngine.Curve(_config.CountdownVolume);
            if (gain <= 0.0001) return;
            if (custom.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                AHFMCChat.SoundEngine.Play("count", File.ReadAllBytes(custom), gain);
                return;
            }
            string id = key + "|" + custom;
            if (!_menuPlayers.TryGetValue(id, out var mp))
            {
                mp = new System.Windows.Media.MediaPlayer();
                mp.Open(new Uri(custom));
                _menuPlayers[id] = mp;
            }
            mp.Stop();
            mp.Volume = Math.Min(1.0, gain);
            mp.Play();
        }
        catch { PlaySound(fallback, _config.CountdownVolume); }
    }

    private void PlaySound(string resource, int volume)
    {
        try
        {
            var bytes = AHFMCChat.SoundEngine.Resource(resource);
            if (bytes == null) return;
            AHFMCChat.SoundEngine.Play("count", bytes, AHFMCChat.SoundEngine.Curve(_config.MasterVolume) * AHFMCChat.SoundEngine.Curve(volume));
        }
        catch { }
    }

    private static string VolText(int v) => v <= 0 ? "Muted" : v + "%";
    private static int StepVolume(int v, int d) => Wrap((int)Math.Round(Math.Clamp(v, 0, 100) / 10.0), d, 11) * 10;

    private object SoundState() => new
    {
        master = _config.MasterVolume,
        music = _config.MusicVolume,
        menu = _config.MenuVolume,
        count = _config.CountdownVolume,
        nsound = AHFMCChat.LocalSettings.NotificationSound,
        nvol = AHFMCChat.LocalSettings.NotificationVolume
    };

    private void ApplyVolumes()
    {
        AHFMCChat.SoundEngine.Master = Math.Clamp(_config.MasterVolume, 0, 100);
        try { if (_music != null) _music.Volume = HybridMusicVolume(); if (_hybridOn && !HybridVideo.IsMuted) HybridVideo.Volume = HybridMusicVolume(); } catch { }
    }

    private void VolumeChanged(string sample)
    {
        SaveConfig();
        AHFMCChat.LocalSettings.Save();
        ApplyVolumes();
        PushClassicState();
        RefreshSettings();
        if (sample == "count") PlayCountSound(false);
        else if (sample == "ding") AHFMCChat.SoundEngine.PlayDing();
    }

    private void SetVolume(string key, int value)
    {
        int v = Math.Clamp(value, 0, 100);
        switch (key)
        {
            case "master": _config.MasterVolume = v; VolumeChanged("count"); break;
            case "music": _config.MusicVolume = v; VolumeChanged(""); break;
            case "menu": _config.MenuVolume = v; VolumeChanged(""); break;
            case "count": _config.CountdownVolume = v; VolumeChanged("count"); break;
            case "nsound": AHFMCChat.LocalSettings.NotificationSound = value; VolumeChanged("ding"); break;
            case "nvol": AHFMCChat.LocalSettings.NotificationVolume = v; VolumeChanged("ding"); break;
        }
    }

    private TaskCompletionSource<bool>? _dialogTcs;
    private bool _classicHiddenForDialog;

    private Task<bool> AskAsync(string message, string title) => ShowDialogAsync(title, message, "Update now", "Not now");

    private Task<bool> ShowDialogAsync(string title, string message, string yes, string? no, string? prefill = null)
    {
        ShowMouse();
        _dialogTcs?.TrySetResult(false);
        _dialogTcs = new TaskCompletionSource<bool>();

        DialogTitle.Text = title;
        DialogText.Text = message;
        DialogYes.Content = yes;
        DialogNo.Content = no ?? "";
        DialogNo.Visibility = no == null ? Visibility.Collapsed : Visibility.Visible;
        DialogInput.Visibility = prefill == null ? Visibility.Collapsed : Visibility.Visible;
        if (prefill != null) DialogInput.Text = prefill;

        if (ClassicPane.Visibility == Visibility.Visible)
        {
            _classicHiddenForDialog = true;
            ClassicPane.Visibility = Visibility.Collapsed;
            DialogPane.Background = Brushes.Black;
        }
        else DialogPane.Background = Br("#E6050810");

        DialogPane.Visibility = Visibility.Visible;
        Dispatcher.BeginInvoke(new Action(() => { if (prefill != null) { DialogInput.Focus(); DialogInput.SelectAll(); } else DialogYes.Focus(); }), DispatcherPriority.Input);
        return _dialogTcs.Task;
    }

    private void AnswerDialog(bool result)
    {
        DialogPane.Visibility = Visibility.Collapsed;
        DialogInput.Visibility = Visibility.Collapsed;
        if (_classicHiddenForDialog)
        {
            _classicHiddenForDialog = false;
            ClassicPane.Visibility = Visibility.Visible;
            ClassicView.Focus();
        }
        var t = _dialogTcs;
        _dialogTcs = null;
        t?.TrySetResult(result);
    }

    private void DialogYes_Click(object sender, RoutedEventArgs e) => AnswerDialog(true);
    private void DialogNo_Click(object sender, RoutedEventArgs e) => AnswerDialog(false);
}
