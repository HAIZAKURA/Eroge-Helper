﻿using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using ReactiveUI;
using Splat;

namespace ErogeHelper.View.Pages;

public partial class AboutPage
{
    private new ViewModel.Pages.AboutViewModel ViewModel => base.ViewModel!;

    public AboutPage()
    {
        InitializeComponent();
        AppVersion.Text = App.EHVersion;

        this.WhenActivated(d =>
        {
            this.WhenAnyValue(x => x.AppVersion.Text)
                .BindTo(this, x => x.ViewModel.AppVersion);

            HandleActivation();

            this.OneWayBind(ViewModel,
                vm => vm.VersionBrushColor,
                v => v.VersionBorder.BorderBrush,
                color => color.ToNativeBrush()).DisposeWith(d);
            this.OneWayBind(ViewModel,
                vm => vm.VersionBrushColor,
                v => v.VersionForeground.Foreground,
                color => color.ToNativeBrush()).DisposeWith(d);
            this.OneWayBind(ViewModel,
                vm => vm.CanJumpRelease,
                v => v.VersionBorder.IsEnabled).DisposeWith(d);
            this.OneWayBind(ViewModel,
                vm => vm.UpdateStatusTip,
                v => v.UpdateStatusTip.Text).DisposeWith(d);
            this.Bind(ViewModel,
                vm => vm.AcceptedPreviewVersion,
                v => v.PreviewCheckBox.IsChecked).DisposeWith(d);

            this.OneWayBind(ViewModel,
                vm => vm.ShowUpdateButton,
                v => v.UpdateButton.Visibility,
                value => value ? Visibility.Visible : Visibility.Collapsed).DisposeWith(d);
            this.BindCommand(ViewModel,
                vm => vm.Update,
                v => v.UpdateButton).DisposeWith(d);
        });
    }

    private void HandleActivation() => ViewModel.CheckUpdate.Execute().Subscribe();
}
