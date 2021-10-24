﻿using ErogeHelper.Common.Contracts;
using ErogeHelper.Model.DataServices.Interface;
using ErogeHelper.Model.Repositories.Interface;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Vanara.PInvoke;
using WindowsInput.Events;

namespace ErogeHelper.ViewModel.Controllers
{
    public class TouchToolBoxViewModel : ReactiveObject, IEnableLogger
    {
        public TouchToolBoxViewModel(
            IGameInfoRepository? gameInfoRepository = null,
            IEhConfigRepository? ehConfigRepository = null,
            IGameDataService? gameDataService = null)
        {
            gameInfoRepository ??= DependencyResolver.GetService<IGameInfoRepository>();
            ehConfigRepository ??= DependencyResolver.GetService<IEhConfigRepository>();
            gameDataService ??= DependencyResolver.GetService<IGameDataService>();

            TouchToolBoxVisible = ehConfigRepository.UseTouchToolBox && gameInfoRepository.GameInfo!.IsLoseFocus;

            var holdEnterSubj = new Subject<bool>();

            var interval = Observable
                .Interval(TimeSpan.FromMilliseconds(ConstantValues.KeyboardLagTime))
                .TakeUntil(holdEnterSubj.Where(on => !on));

            holdEnterSubj
                .DistinctUntilChanged()
                .Where(on => on)
                .SelectMany(interval)
                .Subscribe(async _ =>
                    await WindowsInput.Simulate.Events()
                        .Click(KeyCode.Return)
                        .Invoke().ConfigureAwait(false));

            Esc = ReactiveCommand.CreateFromTask(async () =>
                await WindowsInput.Simulate.Events()
                    .Click(KeyCode.Escape)
                    .Invoke().ConfigureAwait(false));
            Ctrl = ReactiveCommand.CreateFromTask(async () =>
                await WindowsInput.Simulate.Events()
                    .Hold(KeyCode.Control)
                    .Invoke().ConfigureAwait(false));
            CtrlRelease = ReactiveCommand.CreateFromTask(async () =>
                await WindowsInput.Simulate.Events()
                    .Release(KeyCode.Control)
                    .Invoke().ConfigureAwait(false));

            var enterIsHolded = false;
            Enter = ReactiveCommand.CreateFromTask(async () =>
            {
                // XXX: Have no idea to improve this
                enterIsHolded = true;
                await WindowsInput.Simulate.Events()
                    .Click(KeyCode.Enter)
                    .Wait(ConstantValues.PressFirstKeyLagTime)
                    .Invoke().ConfigureAwait(false);
                if (enterIsHolded)
                {
                    holdEnterSubj.OnNext(true);
                }
            });
            EnterRelease = ReactiveCommand.Create(() =>
            {
                holdEnterSubj.OnNext(false);
                enterIsHolded = false;
            });

            Space = ReactiveCommand.CreateFromTask(async () =>
                await WindowsInput.Simulate.Events()
                    .Click(KeyCode.Space)
                    .Invoke().ConfigureAwait(false));
            PageUp = ReactiveCommand.CreateFromTask(async () =>
                await WindowsInput.Simulate.Events()
                    .Click(KeyCode.PageUp)
                    .Invoke().ConfigureAwait(false));
            PageDown = ReactiveCommand.CreateFromTask(async () =>
                await WindowsInput.Simulate.Events()
                    .Click(KeyCode.PageDown)
                    .Invoke().ConfigureAwait(false));
            UpArrow = ReactiveCommand.CreateFromTask(async () =>
                await WindowsInput.Simulate.Events()
                    .Click(KeyCode.Up)
                    .Invoke().ConfigureAwait(false));
            LeftArrow = ReactiveCommand.CreateFromTask(async () =>
                await WindowsInput.Simulate.Events()
                    .Click(KeyCode.Left)
                    .Invoke().ConfigureAwait(false));
            DownArrow = ReactiveCommand.CreateFromTask(async () =>
                await WindowsInput.Simulate.Events()
                    .Click(KeyCode.Down)
                    .Invoke().ConfigureAwait(false));
            RightArrow = ReactiveCommand.CreateFromTask(async () =>
                await WindowsInput.Simulate.Events()
                    .Click(KeyCode.Right)
                    .Invoke().ConfigureAwait(false));
            ScrollUp = ReactiveCommand.CreateFromTask(async () =>
            {
                ScrollUpVisible = false;
                ScrollDownVisible = false;
                // Certain focus by mouse position
                var executeResult = await WindowsInput.Simulate.Events()
                    .Wait(ConstantValues.MinimumLagTime)
                    .Scroll(ButtonCode.VScroll, ButtonScrollDirection.Up)
                    .Wait(ConstantValues.MinimumLagTime)
                    .Invoke().ConfigureAwait(true);
                ScrollUpVisible = true;
                ScrollDownVisible = true;
                return executeResult;
            });
            ScrollDown = ReactiveCommand.CreateFromTask(async () =>
            {
                ScrollDownVisible = false;
                var executeResult = await WindowsInput.Simulate.Events()
                    .Wait(ConstantValues.MinimumLagTime)
                    .Scroll(ButtonCode.VScroll, ButtonScrollDirection.Down)
                    .Wait(ConstantValues.MinimumLagTime)
                    .Invoke().ConfigureAwait(true);
                ScrollDownVisible = true;
                return executeResult;
            });
        }

        [Reactive]
        public bool TouchToolBoxVisible { get; set; }


        [Reactive]
        public bool ScrollUpVisible { get; set; } = true;
        [Reactive]
        public bool ScrollDownVisible { get; set; } = true;

        public ReactiveCommand<Unit, bool> Esc { get; }
        public ReactiveCommand<Unit, bool> Ctrl { get; }
        public ReactiveCommand<Unit, bool> CtrlRelease { get; }
        public ReactiveCommand<Unit, Unit> Enter { get; }
        public ReactiveCommand<Unit, Unit> EnterRelease { get; }
        public ReactiveCommand<Unit, bool> Space { get; }
        public ReactiveCommand<Unit, bool> PageUp { get; }
        public ReactiveCommand<Unit, bool> PageDown { get; }
        public ReactiveCommand<Unit, bool> UpArrow { get; }
        public ReactiveCommand<Unit, bool> LeftArrow { get; }
        public ReactiveCommand<Unit, bool> DownArrow { get; }
        public ReactiveCommand<Unit, bool> RightArrow { get; }
        public ReactiveCommand<Unit, bool> ScrollUp { get; }
        public ReactiveCommand<Unit, bool> ScrollDown { get; }
    }
}
