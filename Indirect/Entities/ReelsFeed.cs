﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Indirect.Entities.Wrappers;
using Indirect.Utilities;
using InstagramAPI;
using InstagramAPI.Classes;
using InstagramAPI.Utils;

namespace Indirect.Entities
{
    class ReelsFeed
    {
        public readonly ObservableCollection<ReelWrapper> Reels = new ObservableCollection<ReelWrapper>();

        private CancellationTokenSource _reelsUpdateLoop;

        public async Task UpdateReelsFeedAsync(ReelsTrayFetchReason fetchReason = ReelsTrayFetchReason.ColdStart)
        {
            if (Debouncer.Throttle(nameof(UpdateReelsFeed), TimeSpan.FromSeconds(10)) ||
                fetchReason == ReelsTrayFetchReason.ColdStart)
            {
                await UpdateReelsFeed(fetchReason);
            }
        }

        private async Task UpdateReelsFeed(ReelsTrayFetchReason fetchReason)
        {
            var result = await ((App)App.Current).ViewModel.InstaApi.GetReelsTrayFeed(fetchReason);
            if (!result.IsSucceeded) return;
            await CoreApplication.MainView.CoreWindow.Dispatcher.QuickRunAsync(() =>
            {
                try
                {
                    SyncReels(result.Value);
                }
                catch (ArgumentOutOfRangeException e)
                {
                    // TODO: investigate origin of ArgumentOutOfRangeException
                    DebugLogger.LogException(e);
                }
            }, fetchReason == ReelsTrayFetchReason.PullToRefresh ? CoreDispatcherPriority.Normal : CoreDispatcherPriority.Low);
        }

        private void SyncReels(Reel[] target)
        {
            if (target.Length == 0) return;
            target = target.Where(x => x.ReelType == "user_reel").ToArray();

            lock (Reels)
            {
                // Remove existing reels that are not in the target
                for (int i = 0; i < Reels.Count; i++)
                {
                    var existingReel = Reels[i];
                    if (target.All(x => !x.Id.Equals(existingReel.Id)))
                    {
                        Reels.RemoveAt(i);
                        i--;
                    }
                }

                // Add new reels from target and also update existing ones
                for (int i = 0; i < target.Length; i++)
                {
                    var reel = target[i];
                    Reel equivalent = null;
                    var equivalentIndex = -1;
                    for (int j = 0; j < Reels.Count; j++)
                    {
                        if (Reels[j].Id.Equals(reel.Id))
                        {
                            equivalent = Reels[j];
                            equivalentIndex = j;
                            break;
                        }
                    }
                    if (equivalent != null)
                    {
                        PropertyCopier<Reel, Reel>.Copy(reel, equivalent);
                        if (i == equivalentIndex) continue;
                        Reels.RemoveAt(equivalentIndex);
                        Reels.Insert(i > Reels.Count ? Reels.Count : i, new ReelWrapper(equivalent));
                    }
                    else
                    {
                        Reels.Insert(i > Reels.Count ? Reels.Count : i, new ReelWrapper(reel));
                    }
                }
            }
        }

        public async Task<FlatReelsContainer> PrepareFlatReelsContainer(ReelWrapper selected)
        {
            FlatReelsContainer flatReelsContainer;
            int selectedIndex;
            lock (Reels)
            {
                selectedIndex = Reels.IndexOf(selected);
                if (selectedIndex == -1) return null;
                flatReelsContainer = new FlatReelsContainer(Reels, selectedIndex);
            }
            await flatReelsContainer.UpdateUserIndex(selectedIndex);
            return flatReelsContainer;
        }

        public async void StartReelsFeedUpdateLoop()
        {
            CancellationTokenSource tokenSource;
            _reelsUpdateLoop?.Cancel();
            lock (this)
            {
                tokenSource = _reelsUpdateLoop = new CancellationTokenSource();
            }

            try
            {
                while (!tokenSource.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5), tokenSource.Token);
                        await UpdateReelsFeedAsync();
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                }
            }
            finally
            {
                lock (this)
                {
                    if (_reelsUpdateLoop == tokenSource)
                    {
                        _reelsUpdateLoop = null;
                    }
                }
                
                tokenSource.Dispose();
            }
        }

        public void StopReelsFeedUpdateLoop(bool clear = false)
        {
            _reelsUpdateLoop?.Cancel();
            if (clear)
            {
                lock (Reels)
                {
                    Reels.Clear();
                }
            }
        }
    }
}
