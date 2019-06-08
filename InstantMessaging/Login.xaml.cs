﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using InstaSharper.Classes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace InstantMessaging
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Login : Page
    {
        ApiContainer _viewModel;
        public Login()
        {
            this.InitializeComponent();
        }

        private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter)
                Button_Click(sender, e);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameBox.Text;
            var password = PasswordBox.Password;
            if (username.Length <= 0 || password.Length <= 0) return;
            _viewModel = await ApiContainer.Factory(username, password);
            var result = await _viewModel.Login();
            if (!result.Succeeded || result.Value != InstaLoginResult.Success)
                return;
            Frame.Navigate(typeof(MainPage), _viewModel);

        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _viewModel = await ApiContainer.Factory();
            if (_viewModel.IsUserAuthenticated)
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Frame.Navigate(typeof(MainPage), _viewModel);
                });
        }
    }
}