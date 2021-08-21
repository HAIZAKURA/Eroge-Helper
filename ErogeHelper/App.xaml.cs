﻿using CommunityToolkit.WinUI.Notifications;
using ErogeHelper.Common;
using ErogeHelper.Common.Contracts;
using ErogeHelper.Common.Exceptions;
using ErogeHelper.Common.Functions;
using ErogeHelper.Model.Services.Interface;
using Ookii.Dialogs.Wpf;
using ReactiveUI;
using Splat;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ErogeHelper
{
    public partial class App : IEnableLogger
    {
        private const string UniqueAppEventName = "{d3bcc592-a3bb-4c26-99c4-23c750ddcf77}";

        public App()
        {
            // Note: This application would throw some exceptions that wasn't bugs when initializing
            // System.Threading.WaitHandleCannotBeOpenedException for active singleton app
            // https://github.com/reactiveui/ReactiveUI/issues/2395
            // System.IO.FileNotFoundException 5 times cause reactiveUI is scanning Drawing, XamForms, Winforms, etc
            // System.Runtime.InteropServices.COMException when access ToastComponent first time
            try
            {
                SetupExceptionHandling();
                SetI18NLanguageDictionary();
                SingleInstanceWatcher();

                ShutdownMode = ShutdownMode.OnExplicitShutdown;
                var currentDirectory = Path.GetDirectoryName(AppContext.BaseDirectory);
                Directory.SetCurrentDirectory(currentDirectory ??
                    throw new ArgumentNullException(nameof(currentDirectory)));

                DependencyInject.Register();

                this.Events().Startup
                    .Select(startupEvent => startupEvent.Args)
                    .Subscribe(args =>
                    {
                        ToastManagement.Register();
                        ToastManagement.AdminModeTipToast();
                        DependencyInject.UpdateEhDatabase();

                        var startupService = DependencyInject.GetService<IStartupService>();

                        if (args.Length == 0)
                        {
                            ModernWpf.MessageBox.Show("Can't run ErogeHelper directly", "Eroge Helper");
                            Terminate();
                            return;
                        }

                        // EH already exit, but toast is clicked. This one shouldn't happen 
                        if (args.Contains("-ToastActivated") || args.Contains("-Embedding"))
                        {
                            Terminate(-1);
                        }

                        if (args[0].Equals(Environment.ProcessPath, StringComparison.Ordinal))
                        {
                            ModernWpf.MessageBox.Show("Can't run ErogeHelper itself", "Eroge Helper");
                            Terminate();
                            return;
                        }

                        startupService.StartFromCommandLine(args[0], args.Any(arg => arg is "/le" or "-le"));
                    });
            }
            catch (AppExistedException)
            {
                Current.Shutdown();
            }
            catch (Exception ex)
            {
                this.Log().Error(ex);
                ShowErrorDialog("AppCtor", ex);
                Terminate(-1);
            }
        }

        public static void Terminate(int exitCode = 0)
        {
            Current.Shutdown(exitCode);

            if (Utils.IsOSWindows8OrNewer)
            {
                ToastNotificationManagerCompat.History.Clear();
            }

            if (exitCode != 0)
            {
                Environment.Exit(exitCode);
            }
        }

        private static void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                if (Dispatcher.FromThread(Thread.CurrentThread) is null ||
                    Dispatcher.CurrentDispatcher.Thread == Thread.CurrentThread)
                    return;

                var ex = args.ExceptionObject as Exception ??
                         new ArgumentNullException("AppDomain.CurrentDomain.UnhandledException");

                LogHost.Default.Fatal(ex, "non-UI thread error occurrent");
                ShowErrorDialog("Fatal", ex);
                Terminate(-1);
            };

            Current.DispatcherUnhandledException += (_, args) =>
            {
                var ex = args.Exception;

                if (Current.Windows.Count != 0)
                {
                    args.Handled = true;
                    LogHost.Default.Error(ex, "UI thread error occurrent");
                    ShowErrorDialog("UI", ex);
                }
                else
                {
                    LogHost.Default.Fatal(ex, "UI thread error occurrent");
                    ShowErrorDialog("UI-Fatal", ex);
                    Terminate(-1);
                }
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                args.SetObserved();
                LogHost.Default.Error(args.Exception, "TPL error occurrent");
                ShowErrorDialog("Task", args.Exception);
            };
        }

        private static void SetI18NLanguageDictionary() =>
            Language.Strings.Culture = Thread.CurrentThread.CurrentCulture.ToString() switch
            {
                "zh-Hans" => new System.Globalization.CultureInfo("zh-Hans"),
                "zh" => new System.Globalization.CultureInfo("zh-Hans"),
                "zh-CN" => new System.Globalization.CultureInfo("zh-Hans"),
                "zh-SG" => new System.Globalization.CultureInfo("zh-Hans"),
                _ => new System.Globalization.CultureInfo(""),
            };

        private static void SingleInstanceWatcher()
        {
            var eventWaitHandle =
                new EventWaitHandle(false, EventResetMode.AutoReset, UniqueAppEventName, out bool createdNew);

            if (!createdNew)
            {
                eventWaitHandle.Set();
                throw new AppExistedException();
            }

            var toastLifetimeTimer = new System.Diagnostics.Stopwatch();

            Observable.Create<object>(observer =>
                {
                    while (eventWaitHandle.WaitOne())
                    {
                        observer.OnNext(new object());
                    }
                    return Disposable.Empty;
                })
                .SubscribeOn(RxApp.TaskpoolScheduler)
                .Subscribe(async _ =>
                {
                    await ToastManagement
                        .ShowAsync("ErogeHelper is running!", toastLifetimeTimer)
                        .ConfigureAwait(false);
                });
        }

        private static void ShowErrorDialog(string errorLevel, Exception ex)
        {
            using var dialog = new TaskDialog
            {
                WindowTitle = $"ErogeHelper v{EHContext.AppVersion ?? "?.?.?.?"} - {errorLevel} Error",
                MainInstruction = $"{ex.GetType().FullName}: {ex.Message}",
                Content = Language.Strings.ErrorDialog_Content,
                ExpandedInformation = $"OS Version: {Utils.GetOSInfo()}\r\nCaused by source `{ex.Source}`\r\n" +
                                      ex.StackTrace +
                                      (ex.InnerException is null ?
                                          string.Empty :
                                          $"\r\nThis Exception caused with in another Exception: {ex.InnerException}"),
                Width = 300,
            };

            TaskDialogButton clipboardButton = new(Language.Strings.OokiiDialog_ClipboardLabel);
            TaskDialogButton okButton = new(ButtonType.Ok);
            dialog.Buttons.Add(clipboardButton);
            dialog.Buttons.Add(okButton);
            var clicked = dialog.ShowDialog();
            if (clicked == clipboardButton)
            {
                var errorInfo = dialog.WindowTitle + "\r\n" + dialog.MainInstruction + "\r\n" + dialog.ExpandedInformation;
                Clipboard.SetText(errorInfo);
            }
        }
    }
}
