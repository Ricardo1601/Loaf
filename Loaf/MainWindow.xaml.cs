﻿using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Runtime.InteropServices;
using WinRT.Interop;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Loaf
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private bool _isLoafing;
        private WinProc _newWndProc = null;
        private IntPtr _oldWndProc = IntPtr.Zero;
        private const int MIN_WINDOW_WIDTH = 1000;
        private const int MIN_WINDOW_HEIGHT = 680;

        public MainWindow()
        {
            this.InitializeComponent();
            _appWindow = GetAppWindowForCurrentWindow();
            _appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
            _appWindow.SetPresenter(AppWindowPresenterKind.Default);
            Instance = this;
            Root.Loaded += OnLoaded;
            Root.KeyDown += Root_KeyDown;
            Activated += MainWindow_Activated;
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            var control = Frame.Content as Page;
            if (control != null && _isLoafing)
                control.Focus(FocusState.Keyboard);
        }

        public static MainWindow Instance { get; private set; }

        private void Root_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_isLoafing && e.Key == Windows.System.VirtualKey.Escape)
            {
                Frame.GoBack();
                Unloaf();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainView));
            _appWindow = GetAppWindowForCurrentWindow();

            _appWindow.Title = "WinUI ❤️ " + ResourceExtensions.GetLocalized("Loaf");
            SubClassing();
        }

        private AppWindow _appWindow;
        private delegate IntPtr WinProc(IntPtr hWnd, PInvoke.User32.WindowMessage msg, IntPtr wParam, IntPtr lParam);

        public void Loaf()
        {
            RestoreWindow();
            var parent = VisualTreeHelper.GetParent(Root);
            while (parent != null)
            {
                if (parent is FrameworkElement element)
                {
                    element.IsHitTestVisible = false;
                }

                parent = VisualTreeHelper.GetParent(parent);
            }
            Frame.Navigate(typeof(Windows11UpdateView));


            _appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
            ScreenHelper.SetScreenMode(1);
            while (ShowCursor(true) < 0)
            {
                ShowCursor(true); //显示光标
            }
            while (ShowCursor(false) >= 0)
            {
                ShowCursor(false); //隐藏光标
            }
            // 阻止系统睡眠，阻止屏幕关闭。
            SystemSleep.PreventForCurrentThread();
            _isLoafing = true;
        }

        public void Unloaf()
        {
            _isLoafing = false;
            _appWindow.SetPresenter(AppWindowPresenterKind.Default);
            ScreenHelper.SetScreenMode(2);
            var parent = VisualTreeHelper.GetParent(Root);
            while (parent != null)
            {
                if (parent is FrameworkElement element)
                {
                    element.IsHitTestVisible = true;
                }

                parent = VisualTreeHelper.GetParent(parent);
            }
            while (ShowCursor(true) < 0)
            {
                ShowCursor(true); //显示光标
            }
            // 恢复此线程曾经阻止的系统休眠和屏幕关闭。
            SystemSleep.RestoreForCurrentThread();
        }

        [DllImport("user32", EntryPoint = "ShowCursor")]
        public extern static int ShowCursor(bool show);

        [DllImport("user32")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, WinProc newProc);

        [DllImport("user32")]
        private static extern IntPtr SetWindowLongW(IntPtr hWnd, PInvoke.User32.WindowLongIndexFlags nIndex, WinProc newProc);

        [DllImport("user32.dll", EntryPoint = "ShowWindow")]
        public static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport("user32.dll")]
        internal static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, PInvoke.User32.WindowMessage msg, IntPtr wParam, IntPtr lParam);

        private AppWindow GetAppWindowForCurrentWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            return AppWindow.GetFromWindowId(myWndId);
        }

        private void RestoreWindow()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            ShowWindow(hWnd, 9);
        }

        private void SubClassing()
        {
            var windowHandle = WindowNative.GetWindowHandle(this);
            _newWndProc = new WinProc(NewWindowProc);
            if (Environment.Is64BitProcess)
                _oldWndProc = SetWindowLongPtr(windowHandle, PInvoke.User32.WindowLongIndexFlags.GWL_WNDPROC, _newWndProc);
            else
                _oldWndProc = SetWindowLongW(windowHandle, PInvoke.User32.WindowLongIndexFlags.GWL_WNDPROC, _newWndProc);
        }

        private IntPtr NewWindowProc(IntPtr hWnd, PInvoke.User32.WindowMessage msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case PInvoke.User32.WindowMessage.WM_GETMINMAXINFO:
                    {
                        var minMaxInfo = Marshal.PtrToStructure<PInvoke.User32.MINMAXINFO>(lParam);
                        minMaxInfo.ptMinTrackSize.x = GetActualPixel(MIN_WINDOW_WIDTH, hWnd);
                        minMaxInfo.ptMinTrackSize.y = GetActualPixel(MIN_WINDOW_HEIGHT, hWnd);
                        Marshal.StructureToPtr(minMaxInfo, lParam, true);
                        break;
                    }
            }

            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        private static int GetActualPixel(double pixel, IntPtr windowHandle)
        {
            var dpi = PInvoke.User32.GetDpiForWindow(windowHandle);
            return Convert.ToInt32(pixel * (dpi / 96.0));
        }
    }
}
