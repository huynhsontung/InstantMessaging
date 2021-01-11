﻿using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using NeoSmart.Unicode;

namespace Indirect.Controls
{
    public partial class EmojiPicker : Control
    {
        private static Flyout openFlyout;

        private int skinToneIndex = 0;
        private Border highlightBorder;
        private Button skinToneButton;
        private ListViewBase emojiPresenter;
        private Button[] categoryButtons;
        private Button closeButton;
        private TextBox searchBox;
        private string selectedEmoji;
        private SingleEmoji[] activeEmoji;
        private ObservableCollection<SingleEmoji> allEmoji;
        private bool searchMode;

        public EmojiPicker()
        {
            this.DefaultStyleKey = typeof(EmojiPicker);
            this.categoryButtons = new Button[6];
        }

        public static async Task<string> ShowAsync(FrameworkElement placementTarget, FlyoutShowOptions showOptions)
        {
            var picker = new EmojiPicker();

            openFlyout = new Flyout
            {
                Content = picker
            };

            openFlyout.ShowAt(placementTarget, showOptions);
            
            picker.Focus(FocusState.Programmatic);

            var tcs = new TaskCompletionSource<string>();

            openFlyout.Closed += (_, __) => tcs.SetResult(picker.selectedEmoji);

            return await tcs.Task;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.highlightBorder = (Border) this.GetTemplateChild("HighlightBorder");
            this.skinToneButton = (Button) this.GetTemplateChild("SkinToneButton");
            this.emojiPresenter = (ListViewBase) this.GetTemplateChild("EmojiPresenter");

            this.categoryButtons[0] = (Button) this.GetTemplateChild("SmilesButton");
            this.categoryButtons[1] = (Button) this.GetTemplateChild("PeopleButton");
            this.categoryButtons[2] = (Button) this.GetTemplateChild("BalloonButton");
            this.categoryButtons[3] = (Button) this.GetTemplateChild("PizzaButton");
            this.categoryButtons[4] = (Button) this.GetTemplateChild("CarButton");
            this.categoryButtons[5] = (Button) this.GetTemplateChild("HeartButton");

            this.closeButton = (Button) this.GetTemplateChild("CloseButton");
            this.searchBox = (TextBox) this.GetTemplateChild("SearchBox");

            this.skinToneButton.Click += this.SkinToneButtonClick;
            this.closeButton.Click += this.CloseButtonClick;
            this.emojiPresenter.SelectionChanged += this.EmojiSelected;
            this.searchBox.TextChanged += SearchBoxTextChanged;

            foreach (var button in this.categoryButtons)
            {
                button.Click += this.ChangeCategoryClick;
            }

            this.SetCurrentEmoji(0);
            this.allEmoji = new ObservableCollection<SingleEmoji>();
        }

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab)
            {
                var shift = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift) == CoreVirtualKeyStates.Down;
                var current = Grid.GetColumn(this.highlightBorder);
                var i = this.PositiveModulo(shift ? current - 2 : current, 6);
                this.SetCurrentEmoji(i);
                Grid.SetColumn(this.highlightBorder, i + 1);
            }
        }

        private void RefreshSearch(string phrase)
        {
            if (phrase.Length == 0)
            {
                VisualStateManager.GoToState(this, "NormalState", true);
                this.searchMode = false;
                this.SetCurrentEmoji(0);
                Grid.SetColumn(this.highlightBorder, 1);
                this.skinToneButton.Visibility = Visibility.Collapsed;
                this.allEmoji.Clear();
            }
            else
            {
                VisualStateManager.GoToState(this, "SearchState", true);
                this.searchMode = true;
                this.skinToneButton.Visibility = Visibility.Visible;

                this.UpdateSearchResults(phrase);

                this.emojiPresenter.ItemsSource = this.allEmoji;
            }
        }

        private void UpdateSearchResults(string phrase)
        {
            var skinToneName = SkinTones[this.skinToneIndex].Name;

            // remove emoji which don't satisfy current search phrase
            this.allEmoji
                .Where(e => !e.SearchTerms.Any(s => s.StartsWith(phrase)))
                .ToList()
                .ForEach(s => this.allEmoji.Remove(s));

            // add emoji which satisfy current search phrase
            AllEmoji()
                .Where(e => e.SearchTerms.Any(s => s.StartsWith(phrase)))
                .Where(e => !this.allEmoji.Contains(e))
                .ToList()
                .ForEach(s => this.allEmoji.Add(s));
        }

        private void SearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshSearch(searchBox.Text);
        }

        private void EmojiSelected(object sender, SelectionChangedEventArgs e)
        {
            this.selectedEmoji = ((SingleEmoji)e.AddedItems.First()).ToString();
            openFlyout.Hide();
        }

        private void ChangeCategoryClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var tag = int.Parse(button.Tag.ToString());
            this.SetCurrentEmoji(tag);
            this.skinToneButton.Visibility = tag == 1 ? Visibility.Visible : Visibility.Collapsed;
            Grid.SetColumn(this.highlightBorder, tag + 1);
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            openFlyout.Hide();
        }

        private int PositiveModulo(int x, int m)
        {
            return (x % m + m) % m;
        }

        private void SetCurrentEmoji(int id)
        {
            this.activeEmoji = EmojiGroups[id];
            this.emojiPresenter.ItemsSource = activeEmoji;
        }

        private void SkinToneButtonClick(object sender, RoutedEventArgs e)
        {
            this.skinToneIndex = (this.skinToneIndex + 1) % 6;
            var skinTone = SkinTones[this.skinToneIndex];
            EmojiGroups[1] = skinTone.SkinEmoji;

            this.skinToneButton.Content = skinTone.Emoji;

            if (!this.searchMode)
            {
                this.SetCurrentEmoji(1);
            }
            else
            {
                this.allEmoji.Clear();
                this.RefreshSearch(this.searchBox.Text);
            }
        }
    }
}
