﻿using ErogeHelper.View.Window;
using ErogeHelper.ViewModel.Window;
using System.Linq;
using System.Windows;

namespace ErogeHelper.View.Page
{
    /// <summary>
    /// GeneralPage.xaml 的交互逻辑
    /// </summary>
    public partial class GeneralPage
    {
        public GeneralPage()
        {
            InitializeComponent();
            DataContext =
                ((PreferenceViewModel)Application.Current.Windows.OfType<PreferenceView>().Single().DataContext)
                .GeneralViewModel;
        }
    }
}
