﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Indirect.Utilities;
using InstagramAPI;
using InstagramAPI.Classes;
using InstagramAPI.Classes.Direct;
using InstagramAPI.Classes.User;
using Microsoft.Toolkit.Collections;

namespace Indirect.Wrapper
{
    /// Wrapper of <see cref="DirectThread"/> with Observable lists
    class InstaDirectInboxThreadWrapper : DirectThread, INotifyPropertyChanged, IIncrementalSource<InstaDirectInboxItemWrapper>
    {
        private readonly Instagram _instaApi;

        public event PropertyChangedEventHandler PropertyChanged;

        public ReversedIncrementalLoadingCollection<InstaDirectInboxThreadWrapper, InstaDirectInboxItemWrapper> ObservableItems { get; set; }
        public new ObservableCollection<InstaUser> Users { get; } = new ObservableCollection<InstaUser>();

        private InstaDirectInboxThreadWrapper(Instagram api)
        {
            ObservableItems = new ReversedIncrementalLoadingCollection<InstaDirectInboxThreadWrapper, InstaDirectInboxItemWrapper>(this);
            _instaApi = api;
        }

        /// <summary>
        /// Only use this constructor to make empty placeholder thread.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="api"></param>
        public InstaDirectInboxThreadWrapper(InstaUser user, Instagram api) : this(api)
        {
            Users.Add(user);
            Title = user.Username;
            if (Users.Count == 0) Users.Add(new InstaUser());
        }

        public InstaDirectInboxThreadWrapper(RankedRecipientThread rankedThread, Instagram api) : this(api)
        {
            Canonical = rankedThread.Canonical;
            Named = rankedThread.Named;
            Pending = rankedThread.Pending;
            Title = rankedThread.ThreadTitle;
            ThreadId = rankedThread.ThreadId;
            ThreadType = DirectThreadType.Private;
            ViewerId = rankedThread.ViewerId;
            foreach (var user in rankedThread.Users)
            {
                Users.Add(user);
            }
            if (Users.Count == 0) Users.Add(new InstaUser());
        }

        public InstaDirectInboxThreadWrapper(DirectThread source, Instagram api) : this(api)
        {
            Canonical = source.Canonical;
            HasNewer = source.HasNewer;
            HasOlder = source.HasOlder;
            IsSpam = source.IsSpam;
            Muted = source.Muted;
            Named = source.Named;
            Pending = source.Pending;
            ViewerId = source.ViewerId;
            LastActivity = source.LastActivity;
            ThreadId = source.ThreadId;
            OldestCursor = source.OldestCursor;
            IsGroup = source.IsGroup;
            IsPin = source.IsPin;
            ValuedRequest = source.ValuedRequest;
            VCMuted = source.VCMuted;
            ReshareReceiveCount = source.ReshareReceiveCount;
            ReshareSendCount = source.ReshareSendCount;
            ExpiringMediaReceiveCount = source.ExpiringMediaReceiveCount;
            ExpiringMediaSendCount = source.ExpiringMediaSendCount;
            NewestCursor = source.NewestCursor;
            ThreadType = source.ThreadType;
            Title = source.Title;
            MentionsMuted = source.MentionsMuted;

            Inviter = source.Inviter;
            LastPermanentItem = source.LastPermanentItem;
            LeftUsers = source.LeftUsers;
            LastSeenAt = source.LastSeenAt;

            foreach (var instaUserShortFriendship in source.Users)
            {
                Users.Add(instaUserShortFriendship);
            }
            if (Users.Count == 0) Users.Add(new InstaUser());
            UpdateItemList(source.Items);
        }

        // This does not update thread's metadata. Better run Inbox.Update() after this.
        public void AddItem(DirectItem item)
        {
            UpdateItemList(new List<DirectItem> {item});
        }

        public void Update(DirectThread source, bool fromInbox = false)
        {
            UpdateExcludeItemList(source);
            if (fromInbox) return;  // Items from GetInbox request will interfere with GetPagedItemsAsync
            UpdateItemList(source.Items);
        }

        private void UpdateExcludeItemList(DirectThread source)
        {
            Canonical = source.Canonical;
            //HasNewer = source.HasNewer;
            //HasOlder = source.HasOlder;
            IsSpam = source.IsSpam;
            Muted = source.Muted;
            Named = source.Named;
            Pending = source.Pending;
            ViewerId = source.ViewerId;
            LastActivity = source.LastActivity;
            ThreadId = source.ThreadId;
            IsGroup = source.IsGroup;
            IsPin = source.IsPin;
            ValuedRequest = source.ValuedRequest;
            VCMuted = source.VCMuted;
            ReshareReceiveCount = source.ReshareReceiveCount;
            ReshareSendCount = source.ReshareSendCount;
            ExpiringMediaReceiveCount = source.ExpiringMediaReceiveCount;
            ExpiringMediaSendCount = source.ExpiringMediaSendCount;
            ThreadType = source.ThreadType;
            Title = source.Title;
            MentionsMuted = source.MentionsMuted;

            Inviter = source.Inviter;
            LastPermanentItem = source.LastPermanentItem?.Timestamp > LastPermanentItem?.Timestamp ?
                source.LastPermanentItem : LastPermanentItem;
            LeftUsers = source.LeftUsers;
            LastSeenAt = source.LastSeenAt;

            if (string.IsNullOrEmpty(OldestCursor) || 
                string.Compare(OldestCursor, source.OldestCursor, StringComparison.Ordinal) > 0)
            {
                OldestCursor = source.OldestCursor;
                HasOlder = source.HasOlder;
            }

            if (string.IsNullOrEmpty(NewestCursor) || 
                string.Compare(NewestCursor, source.NewestCursor, StringComparison.Ordinal) < 0)
            {
                NewestCursor = source.NewestCursor;
                HasNewer = HasNewer;
            }

            UpdateUserList(source.Users);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

        private void UpdateItemList(ICollection<DirectItem> source)
        {
            if (source == null) return;
            var convertedSource = source.Select(x => new InstaDirectInboxItemWrapper(x, this, _instaApi)).ToList();
            DecorateItems(convertedSource);
            if (ObservableItems.Count == 0)
            {
                foreach (var item in convertedSource)
                    ObservableItems.Add(item);
                return;
            }

            foreach (var item in convertedSource)
            {
                var existingItem = ObservableItems.SingleOrDefault(x => x.Equals(item));
                var existed = existingItem != null;

                if (existed)
                {
                    if (item.Reactions != null)
                    {
                        existingItem.Reactions.Update(item.Reactions, Users);
                    }
                    continue;
                }
                for (var i = ObservableItems.Count-1; i >= 0; i--)
                {
                    if (item.Timestamp > ObservableItems[i].Timestamp)
                    {
                        ObservableItems.Insert(i+1, item);
                        break;
                    }

                    if (i == 0)
                    {
                        ObservableItems.Insert(0, item);
                    }
                }
            }
        }

        private void UpdateUserList(List<UserWithFriendship> users)
        {
            if (users == null || users.Count == 0) return;
            var toBeAdded = users.Where(p2 => Users.All(p1 => !p1.Equals(p2)));
            var toBeDeleted = Users.Where(p1 => users.All(p2 => !p1.Equals(p2)));
            foreach (var user in toBeAdded)
            {
                Users.Add(user);
            }
            foreach (var user in toBeDeleted)
            {
                Users.Remove(user);
            }
        }

        public async Task<IEnumerable<InstaDirectInboxItemWrapper>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = new CancellationToken())
        {
            // Without ThreadId we cant fetch thread items.
            if (string.IsNullOrEmpty(ThreadId) || !(HasOlder ?? true)) return new List<InstaDirectInboxItemWrapper>(0);
            var pagesToLoad = pageSize / 20;
            if (pagesToLoad < 1) pagesToLoad = 1;
            var pagination = PaginationParameters.MaxPagesToLoad(pagesToLoad);
            pagination.StartFromMaxId(OldestCursor);
            var result = await _instaApi.GetThreadAsync(ThreadId, pagination);
            if (result.Status != ResultStatus.Succeeded || result.Value.Items == null || result.Value.Items.Count == 0) return new List<InstaDirectInboxItemWrapper>(0);
            UpdateExcludeItemList(result.Value);
            var wrappedItems = result.Value.Items.Select(x => new InstaDirectInboxItemWrapper(x, this, _instaApi)).ToList();
            DecorateItems(wrappedItems);
            return wrappedItems;
        }

        // Decide whether item should show timestamp header, name header etc...
        private void DecorateItems(IEnumerable<InstaDirectInboxItemWrapper> wrappedItems)
        {
            var lastItem = ObservableItems.FirstOrDefault();
            var itemList = wrappedItems.ToList();
            var refItem = itemList.Last();
            if (lastItem != null)
            {
                if (refItem.Timestamp <= lastItem.Timestamp)
                {
                    lastItem.ShowTimestampHeader = !IsCloseEnough(lastItem.Timestamp, refItem.Timestamp);
                    lastItem.ShowNameHeader = lastItem.UserId != refItem.UserId && !lastItem.FromMe && Users.Count > 1;
                }
                else
                {
                    // New item added to the top
                    var latestItem = ObservableItems.Last();
                    refItem.ShowTimestampHeader = !IsCloseEnough(latestItem.Timestamp, refItem.Timestamp);
                    refItem.ShowNameHeader = latestItem.UserId != refItem.UserId && !latestItem.FromMe && Users.Count > 1;
                }
            }

            for (int i = itemList.Count - 1; i >= 1; i--)
            {
                itemList[i].ShowTimestampHeader = !IsCloseEnough(itemList[i].Timestamp, itemList[i - 1].Timestamp);
                itemList[i].ShowNameHeader = itemList[i].UserId != itemList[i - 1].UserId && !itemList[i].FromMe && Users.Count > 1;
            }
        }

        private const int TimestampClosenessThreshold = 3; // hours
        private static bool IsCloseEnough(DateTimeOffset x, DateTimeOffset y)
        {
            return TimeSpan.FromHours(-TimestampClosenessThreshold) < x - y &&
                   x - y < TimeSpan.FromHours(TimestampClosenessThreshold);
        }

        private void FixItemList()
        {
            // Somehow item list got messed up and items are not unique anymore
            var duplicates = ObservableItems.GroupBy(x => x.ItemId).Where(g => g.Count() > 1)
                .Select(y => y);
            foreach (var duplicateGroup in duplicates)
            {
                var duplicate = duplicateGroup.First();
                ObservableItems.Remove(duplicate);
            }
        }
    }
}
