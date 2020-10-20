﻿using ErogeHelper.Common;
using ErogeHelper.Model;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using log4net;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace ErogeHelper.View
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GameView : Window
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(GameView));

        public IntPtr gameHWnd = IntPtr.Zero;
        private bool textPanelPin = false;

        public GameView()
        {
            log.Info("Initialize");
            Messenger.Default.Register<NotificationMessage>(this, NotificationMessageReceived);

            InitializeComponent(); // VM Initialize -> Component Initialize

            SetGameWindowHook();
        }

        private void NotificationMessageReceived(NotificationMessage obj)
        {
            if (obj.Notification == "MakeTextPanelPin")
            {
                log.Info("Set TextPanel Pin");
                textPanelPin = true;
            }
            if (obj.Notification == "OpenCard")
            {
                WordCard.IsOpen = true;
            }
            if (obj.Notification == "CloseCard")
            {
                WordCard.IsOpen = false;
            }
        }

        #region Window Follow Game Initialize
        protected Hook.WinEventDelegate WinEventDelegate;
        private static GCHandle GCSafetyHandle;
        private IntPtr hWinEventHook;
        private void SetGameWindowHook()
        {
            WinEventDelegate = new Hook.WinEventDelegate(WinEventCallback);
            GCSafetyHandle = GCHandle.Alloc(WinEventDelegate);

            var gameInfo = (GameInfo)SimpleIoc.Default.GetInstance(typeof(GameInfo));
            var targetProc = gameInfo.HWndProc;

            targetProc.EnableRaisingEvents = true;
            targetProc.Exited += new EventHandler(Window_Closed);
            Closed += Window_Closed;

            gameHWnd = targetProc.MainWindowHandle;
            uint targetThreadId = Hook.GetWindowThread(gameHWnd);
            dpi = VisualTreeHelper.GetDpi(this).DpiScaleX;

            if (gameHWnd != IntPtr.Zero)
            {
                // 调用 SetWinEventHook 传入 WinEventDelegate 回调函数
                hWinEventHook = Hook.WinEventHookOne(Hook.SWEH_Events.EVENT_OBJECT_LOCATIONCHANGE,
                                                     WinEventDelegate,
                                                     (uint)targetProc.Id,
                                                     targetThreadId);
                // 首次设置窗口位置
                SetLocation();
                log.Info("Begin to follow the window");
                return;
            }

        }

        private double winShadow;
        private double dpi;

        private void SetLocation()
        {
            var rect = Hook.GetWindowRect(gameHWnd, dpi);
            var rectClient = Hook.GetClientRect(gameHWnd, dpi);
            // 再把字体除以dpi好了嘛 不解决窗口大小随着字体变化，两个事情

            Width = rect.Right - rect.Left;  // rectClient.Right + shadow*2
            Height = rect.Bottom - rect.Top; // rectClient.Bottom + shadow + title

            winShadow = (Width - rectClient.Right) / 2;

            var wholeHeight = rect.Bottom - rect.Top;
            var winTitleHeight = wholeHeight - rectClient.Bottom - winShadow;

            ClientArea.Margin = new Thickness(winShadow, winTitleHeight, winShadow, winShadow);

            Left = rect.Left;
            Top = rect.Top;
        }

        protected override void OnDpiChanged(DpiScale oldDpiScaleInfo, DpiScale newDpiScaleInfo)
        {
            dpi = VisualTreeHelper.GetDpi(this).DpiScaleX;
            log.Info($"Current screen dpi {dpi*100}%");
        }

        protected void WinEventCallback(IntPtr hWinEventHook,
                                    Hook.SWEH_Events eventType,
                                    IntPtr hWnd,
                                    Hook.SWEH_ObjectId idObject,
                                    long idChild,
                                    uint dwEventThread,
                                    uint dwmsEventTime)
        {
            // 仅游戏窗口获取焦点时会调用
            //if (hWnd == GameInfo.Instance.hWnd &&
            //    eventType == Hook.SWEH_Events.EVENT_OBJECT_FOCUS)
            //{
            //    log.Info("Game window get foucus");
            //}
            // 更新窗口信息
            if (hWnd == gameHWnd &&
                eventType == Hook.SWEH_Events.EVENT_OBJECT_LOCATIONCHANGE &&
                idObject == (Hook.SWEH_ObjectId)Hook.SWEH_CHILDID_SELF)
            {
                SetLocation();
            }
        }
        #endregion

        private void Window_Closed(object sender, EventArgs e)
        {
            log.Info("Detected quit event");
            GCSafetyHandle.Free();
            Hook.WinEventUnhook(hWinEventHook);
            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                Closed -= Window_Closed;
                Application.Current.Shutdown();
            });
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Do upward compatible
            var NoFocusFlag = false;
            try
            {
                var ret = EHConfig.GetValue(EHNode.NoFocus);
                NoFocusFlag = bool.Parse(ret);
            }
            catch (NullReferenceException)
            {
                // create the node
                EHConfig.SetValue(EHNode.NoFocus, NoFocusFlag.ToString());
            }

            if (NoFocusFlag)
            {
                var interopHelper = new WindowInteropHelper(this);
                int exStyle = Hook.GetWindowLong(interopHelper.Handle, Hook.GWL_EXSTYLE);
                Hook.SetWindowLong(interopHelper.Handle, Hook.GWL_EXSTYLE, exStyle | Hook.WS_EX_NOACTIVATE);
            }

            DispatcherTimer timer = new DispatcherTimer();
            var pointer = new WindowInteropHelper(this);
            timer.Tick += (sender, _) =>
            {
                if (pointer.Handle == IntPtr.Zero)
                {
                    timer.Stop();
                }
                // Still get a little bad exprience with right click taskbar icon
                if (gameHWnd == Hook.GetForegroundWindow())
                {
                    Hook.BringWindowToTop(pointer.Handle);
                }
            };

            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Start();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WinArea.SetValue(StyleProperty, null);
            ClientArea.SetValue(StyleProperty, null);
            if (textPanelPin == true)
            {
                TriggerPopupBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void TriggerPopupBorder_MouseEnter(object sender, MouseEventArgs e)
        {
            if (textPanelPin == false)
            {
                TextArea.Visibility = Visibility.Visible;
                TriggerPopupBorder.Visibility = Visibility.Collapsed;
            }
        }

        private void TextArea_MouseLeave(object sender, MouseEventArgs e)
        {
            if (textPanelPin == false)
            {
                TextArea.Visibility = Visibility.Collapsed;
                TriggerPopupBorder.Visibility = Visibility.Visible;
            }
        }
    }
}