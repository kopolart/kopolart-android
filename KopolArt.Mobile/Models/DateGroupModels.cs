using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using KopolArt.Mobile.Helpers;
using KopolArt.Mobile.Presentation;

namespace KopolArt.Mobile.Models
{
    public interface IDateGroup<in T>
    {
        int Count { get; }
        bool Contains(T item);
        bool Remove(T item);
    }

    public abstract class DateGroupBase<T> : ObservableCollection<T>, IDateGroup<T>
    {
        public string GroupTitle { get; set; } = string.Empty;
        public string GroupIcon { get; set; } = string.Empty;
        public int ItemCount => Count;
        public virtual string CountBadgeText => $"{Count.ToPersianDigits()} مورد";

        protected DateGroupBase(string groupTitle, string groupIcon, IEnumerable<T> items) : base(items)
        {
            GroupTitle = groupTitle;
            GroupIcon = groupIcon;
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(ItemCount)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CountBadgeText)));
        }
    }

    public class HistoryDateGroup : DateGroupBase<HistoryDisplayItem>
    {
        public override string CountBadgeText => $"{Count.ToPersianDigits()} صفحه";

        public HistoryDateGroup(string groupTitle, string groupIcon, IEnumerable<HistoryDisplayItem> items)
            : base(groupTitle, groupIcon, items)
        {
        }
    }

    public class BookmarkDateGroup : DateGroupBase<BookmarkDisplayItem>
    {
        public override string CountBadgeText => $"{Count.ToPersianDigits()} نشان";

        public BookmarkDateGroup(string groupTitle, string groupIcon, IEnumerable<BookmarkDisplayItem> items)
            : base(groupTitle, groupIcon, items)
        {
        }
    }

    public class DownloadDateGroup : DateGroupBase<DownloadDisplayItem>
    {
        public override string CountBadgeText => $"{Count.ToPersianDigits()} فایل";

        public DownloadDateGroup(string groupTitle, string groupIcon, IEnumerable<DownloadDisplayItem> items)
            : base(groupTitle, groupIcon, items)
        {
        }
    }
}
