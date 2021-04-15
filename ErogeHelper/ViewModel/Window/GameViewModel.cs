﻿using Caliburn.Micro;
using ErogeHelper.Common;
using ErogeHelper.Common.Extention;
using ErogeHelper.Common.Messenger;
using ErogeHelper.Model.Repository;
using ErogeHelper.Model.Service.Interface;
using ErogeHelper.View.Window;
using ErogeHelper.ViewModel.Control;
using ErogeHelper.ViewModel.Entity.NotifyItem;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WindowsInput.Events;
using ErogeHelper.Model.Entity.Table;

namespace ErogeHelper.ViewModel.Window
{
    public class GameViewModel : PropertyChangedBase, IHandle<InsideViewTextVisibleMessage>
    {
        public GameViewModel(
            IGameDataService dataService,
            IWindowManager windowManager,
            IEventAggregator eventAggregator,
            ITouchConversionHooker touchConversionHooker,
            EhConfigRepository ehConfigRepository,
            GameRuntimeDataRepo gameRuntimeDataRepo,
            TextViewModel textViewModel,
            IGameWindowHooker gameWindowHooker,
            EhDbRepository ehDbRepository)
        {
            _touchHooker = touchConversionHooker;
            _windowManager = windowManager;
            _eventAggregator = eventAggregator;
            _ehConfigRepository = ehConfigRepository;
            _gameRuntimeDataRepo = gameRuntimeDataRepo;
            TextControl = textViewModel;
            _gameWindowHooker = gameWindowHooker;
            _ehDbRepository = ehDbRepository;

            var gameInfo = _ehDbRepository.GetGameInfoTable();
            _isLoseFocus = gameInfo?.IsLoseFocus ?? false;
            _isTouchToMouse = gameInfo?.IsEnableTouchToMouse ?? false;
            _eventAggregator.SubscribeOnUIThread(this);
            if (_ehConfigRepository.UseOutsideWindow)
                HandleAsync(new InsideViewTextVisibleMessage { IsShowed = false }, CancellationToken.None);
            _fontSize = _ehConfigRepository.FontSize;
            dataService.SourceTextReceived += text =>
            {
                SourceTextArchiver.Enqueue(text);
                TextControl.CardControl.TotalText = text;
            };
            dataService.BindableTextItem += textItem => TextControl.SourceTextCollection = textItem;
            dataService.AppendTextReceived += (text, tip) => AppendTextList.Add(new AppendTextItem
            {
                Message = text,
                ExtraInfo = tip,
            });
            dataService.AppendTextsRefresh += _ => AppendTextList.Clear();
        }

        private readonly IWindowManager _windowManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly EhConfigRepository _ehConfigRepository;
        private readonly GameRuntimeDataRepo _gameRuntimeDataRepo;
        private readonly ITouchConversionHooker _touchHooker;
        private readonly IGameWindowHooker _gameWindowHooker;
        private readonly EhDbRepository _ehDbRepository;

        private bool _assistiveTouchIsVisible = true;
        private double _fontSize;
        private bool _isLoseFocus;
        private bool _isTouchToMouse;

        public TextViewModel TextControl { get; set; }
        public BindableCollection<AppendTextItem> AppendTextList { get; set; } = new();

        // TODO: OutsideView chrome window and scroll able text
        public ConcurrentCircularBuffer<string> SourceTextArchiver = new(30);

        public bool AssistiveTouchIsVisible
        {
            get => _assistiveTouchIsVisible;
            set { _assistiveTouchIsVisible = value; NotifyOfPropertyChange(() => AssistiveTouchIsVisible); }
        }

        public double FontSize
        {
            get => _fontSize;
            set
            {
                _fontSize = value;
                _ehConfigRepository.FontSize = value;
                NotifyOfPropertyChange(() => FontSize);
            }
        }

        public void ZoomIn()
        {
            FontSize += 2;
            NotifyOfPropertyChange(() => CanZoomOut);
        }

        public bool CanZoomOut => FontSize > 3;
        public void ZoomOut()
        {
            FontSize -= 2;
            NotifyOfPropertyChange(() => CanZoomOut);
        }

        public static async void VolumeUp() => await WindowsInput.Simulate.Events()
            .Click(KeyCode.VolumeUp).Invoke().ConfigureAwait(false);
        public static async void VolumeDown() => await WindowsInput.Simulate.Events()
            .Click(KeyCode.VolumeDown).Invoke().ConfigureAwait(false);

        public async void SwitchGameScreen()
        {
            var handle = _gameRuntimeDataRepo.MainProcess.MainWindowHandle;
            NativeMethods.BringWindowToTop(handle);
            await WindowsInput.Simulate.Events()
                .ClickChord(KeyCode.Alt, KeyCode.Enter)
                .Invoke()
                .ConfigureAwait(false);
        }

        #region TextControl Pin

        private Visibility _pinSourceTextToggleVisibility;
        public Visibility PinSourceTextToggleVisibility
        {
            get => _pinSourceTextToggleVisibility;
            set
            {
                _pinSourceTextToggleVisibility = value;
                NotifyOfPropertyChange(() => PinSourceTextToggleVisibility);
            }
        }

        private bool _isSourceTextPined = true;
        public bool IsSourceTextPined
        {
            get => _isSourceTextPined;
            set { _isSourceTextPined = value; NotifyOfPropertyChange(() => IsSourceTextPined); }
        }

        public void PinSourceTextToggle()
        {
            if (IsSourceTextPined)
            {
                TriggerBarVisibility = Visibility.Collapsed;
                TextControlVisibility = Visibility.Visible;
                TextControl.Background = new SolidColorBrush();
            }
            else
            {
                TriggerBarVisibility = Visibility.Visible;
                TextControlVisibility = Visibility.Collapsed;
                TextControl.Background = new SolidColorBrush(Colors.Black) { Opacity = 0.5 };
            }
        }

        private Visibility _triggerBarVisibility = Visibility.Collapsed;
        public Visibility TriggerBarVisibility
        {
            get => _triggerBarVisibility;
            set
            {
                _triggerBarVisibility = value;
                NotifyOfPropertyChange(() => TriggerBarVisibility);
            }
        }

        private Visibility _textControlVisibility = Visibility.Visible;

        public Visibility TextControlVisibility
        {
            get => _textControlVisibility;
            set
            {
                _textControlVisibility = value;
                NotifyOfPropertyChange(() => TextControlVisibility);
            }
        }

        public void TriggerBarEnter()
        {
            if (IsSourceTextPined)
                return;

            TextControlVisibility = Visibility.Visible;
            TriggerBarVisibility = Visibility.Collapsed;
        }

        public void TextControlLeave()
        {
            if (IsSourceTextPined)
                return;

            TextControlVisibility = Visibility.Collapsed;
            TriggerBarVisibility = Visibility.Visible;
        }

        #endregion

        public bool IsLoseFocus
        {
            get => _isLoseFocus;
            set { _isLoseFocus = value; NotifyOfPropertyChange(() => IsLoseFocus); }
        }
        public async void FocusToggle()
        {
            if (IsLoseFocus)
            {
                await _eventAggregator.PublishOnUIThreadAsync(new LoseFocusMessage { Status = true });
            }
            else
            {
                await _eventAggregator.PublishOnUIThreadAsync(new LoseFocusMessage { Status = false });
            }

            var gameInfo = await _ehDbRepository.GetGameInfoAsync() ?? new GameInfoTable();
            gameInfo.IsLoseFocus = IsLoseFocus;
            await _ehDbRepository.SetGameInfoAsync(gameInfo);
        }

        public bool IsTouchToMouse
        {
            get => _isTouchToMouse;
            set { _isTouchToMouse = value; NotifyOfPropertyChange(() => IsTouchToMouse); }
        }
        public async void TouchToMouseToggle()
        {
            _touchHooker.Enable = IsTouchToMouse;

            var gameInfo = await _ehDbRepository.GetGameInfoAsync() ?? new GameInfoTable();
            gameInfo.IsEnableTouchToMouse = IsTouchToMouse;
            await _ehDbRepository.SetGameInfoAsync(gameInfo);
        }

        public static async void TaskbarNotifyArea() => await WindowsInput.Simulate.Events()
            .ClickChord(KeyCode.LWin, KeyCode.A).Invoke().ConfigureAwait(false);

        public static async void TaskView() => await WindowsInput.Simulate.Events()
            .ClickChord(KeyCode.LWin, KeyCode.Tab).Invoke().ConfigureAwait(false);

        public async void ScreenShot()
        {
            AssistiveTouchIsVisible = false;

            await WindowsInput.Simulate.Events()
                .Click(KeyCode.Escape).Invoke().ConfigureAwait(false);

            // Wait for CommandBarFlyout hide
            await Task.Delay(500).ConfigureAwait(false);

            await WindowsInput.Simulate.Events()
                .ClickChord(KeyCode.LWin, KeyCode.Shift, KeyCode.S).Invoke().ConfigureAwait(false);

            await Task.Delay(3000).ConfigureAwait(false);

            AssistiveTouchIsVisible = true;
        }

        public void ResetInsideView() => _gameWindowHooker.ResetWindowHandler();

        public async void OpenPreference()
        {
            var window = Application.Current.Windows.OfType<PreferenceView>().SingleOrDefault();
            if (window is null)
            {
                await _windowManager.ShowWindowFromIoCAsync<PreferenceViewModel>().ConfigureAwait(false);
            }
            else
            {
                window.Activate();
            }
        }

        public static async void PressSkip() => await WindowsInput.Simulate.Events()
            .Hold(KeyCode.Control).Invoke().ConfigureAwait(false);
        public static async void PressSkipRelease() => await WindowsInput.Simulate.Events()
            .Release(KeyCode.Control).Invoke().ConfigureAwait(false);

        public Task HandleAsync(InsideViewTextVisibleMessage message, CancellationToken cancellationToken)
        {
            if (message.IsShowed)
            {
                IsSourceTextPined = true;
                PinSourceTextToggle();
                PinSourceTextToggleVisibility = Visibility.Visible;
            }
            else
            {
                TextControlVisibility = Visibility.Collapsed;
                PinSourceTextToggleVisibility = Visibility.Collapsed;
            }
            return Task.CompletedTask;
        }

        ~GameViewModel() => _eventAggregator.Unsubscribe(this);

#pragma warning disable CS8618
        public GameViewModel() { }
    }
}