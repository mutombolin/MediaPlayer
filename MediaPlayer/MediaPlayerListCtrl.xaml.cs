using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;

using MediaPlayer.Common;
using MediaPlayer.Managers;
using MediaPlayer.Windows;

namespace MediaPlayer
{
    /// <summary>
    /// Interaction logic for MediaPlayerListCtrl.xaml
    /// </summary>
    public partial class MediaPlayerListCtrl : UserControl
    {
        private const string SELECTED_IMAGE = "c:\\apps\\themes\\picard\\images\\apps\\mediaplayer\\mediaplayer_selected.png";
        private const string UNSELECTED_IMAGE = "c:\\apps\\themes\\picard\\images\\apps\\mediaplayer\\mediaplayer_unselected.png";

        private bool _inAudio = false;
        private int _selectedIndex;
        private int _totalItems;

        public delegate void ItemSelection(PortableDevice.PortableDeviceObject obj, MediaFileType mediaFileType);
        public event ItemSelection ItemSelectionHandler;

        public MediaPlayerListCtrl()
        {
            InitializeComponent();

            _selectedIndex = -1;
            _totalItems = 0;

            Loaded += new RoutedEventHandler(MediaPlayerListCtrl_Loaded);

            MediaContentManager.Instance.ScanningMedia += new EventHandler(Instance_ScanningMedia);
            MediaContentManager.Instance.NoMediaFound += new EventHandler(Instance_NoMediaFound);
            MediaContentManager.Instance.MediaFound += new EventHandler(Instance_MediaFound);
            MediaContentManager.Instance.UpdateMedia += new EventHandler(Instance_UpdateMedia);
        }

        void MediaPlayerListCtrl_Loaded(object sender, RoutedEventArgs e)
        {
            HwndSource hwnd = (HwndSource)HwndSource.FromVisual(this);
#if VLC
            VLCPlayer.Instance.HWNDParent = hwnd.Handle;
#else
            WinMediaPlayer.Instance.HWNDParent = hwnd.Handle;
#endif
        }

        void Instance_ScanningMedia(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new EventHandler(SAFE_Instance_ScanningMedia), sender, e);
        }

        private void SAFE_Instance_ScanningMedia(object sender, EventArgs e)
        {
            ShowMessage("Media found, scanning drive...");
        }

        void Instance_NoMediaFound(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new EventHandler(SAFE_Instance_NoMediaFound), sender, e);
        }

        private void SAFE_Instance_NoMediaFound(object sender, EventArgs e)
        {
            ShowMessage("Searching for media...");
        }

        void Instance_MediaFound(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new EventHandler(SAFE_Instance_MediaFound), sender, e);
        }

        void SAFE_Instance_MediaFound(object sender, EventArgs e)
        {
//            MediaContentManager.Instance.Stop();
            System.Diagnostics.Debug.WriteLine("SAFE_Instance_MediaFound");

            if (MediaContentManager.Instance.MusicPlayList.Count > 0)
                _inAudio = true;
            else
                _inAudio = false;

            if (_inAudio)
                ShowAudio();
            else
                ShowVideo();
        }

        void Instance_UpdateMedia(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new EventHandler(SAFE_Instance_UpdateMedia), sender, e);
        }

        private void SAFE_Instance_UpdateMedia(object sender, EventArgs e)
        {
            ContentClear();
            ShowMessage("Searching for media...");
        }

        private void ContentClear()
        {
            stackPanelItems.Children.Clear();
        }

        private void ShowAudio()
        {
            _inAudio = true;
            SwapImage(imageVideo, UNSELECTED_IMAGE);
            SwapImage(imageAudio, SELECTED_IMAGE);

            if (MediaContentManager.Instance.IsSearching)
                ShowMessage("Please wait, searching for audio...");
            else if (MediaContentManager.Instance.IsMusicFilesFound)
                ShowContent(MediaContentManager.Instance.MusicPlayList);
            else
                ShowMessage("Searching for media...");
        }

        private void ShowVideo()
        {
            _inAudio = false;
            SwapImage(imageAudio, UNSELECTED_IMAGE);
            SwapImage(imageVideo, SELECTED_IMAGE);

            if (MediaContentManager.Instance.IsSearching)
                ShowMessage("Please wait, searching for video...");
            else if (MediaContentManager.Instance.IsVideoFilesFound)
                ShowContent(MediaContentManager.Instance.VideoPlayList);
            else
                ShowMessage("Searching for media...");
        }

        private void ShowMessage(string message)
        {
            textBlockMessage.Text = message;
            textBlockMessage.Visibility = System.Windows.Visibility.Visible;

            scrollViewerItems.Visibility = System.Windows.Visibility.Hidden;
            textBlockFileCount.Visibility = System.Windows.Visibility.Hidden;
        }

        private void ShowContent(List<PortableDevice.PortableDeviceObject> PlayList)
        {
            _selectedIndex = -1;
            _totalItems = PlayList.Count;

            if (_totalItems <= 0)
            {
                ShowMessage("No files found");
                return;
            }
            textBlockFileCount.Visibility = System.Windows.Visibility.Visible;

            scrollViewerItems.Visibility = System.Windows.Visibility.Visible;
            textBlockMessage.Visibility = System.Windows.Visibility.Hidden;
            stackPanelItems.Children.Clear();

            bool isAlt = false;
            int index = 0;

            foreach (PortableDevice.PortableDeviceObject obj in PlayList)
            {
                MediaPlayerListItemCtrl listItem = new MediaPlayerListItemCtrl();
                listItem.FileName = obj.Name;
                listItem.ListItemSelect += new EventHandler(listItem_ListItemSelect);
                listItem.ListItemHighlight += new EventHandler(listItem_ListItemHighlight);
                listItem.IsAlt = isAlt;
                listItem.Index = index++;
                isAlt = !isAlt;
                listItem.PortableObject = obj;

                stackPanelItems.Children.Add(listItem);
            }
        }

        void listItem_ListItemHighlight(object sender, EventArgs e)
        {
            if (sender is MediaPlayerListItemCtrl)
            {
                MediaPlayerListItemCtrl listItem = sender as MediaPlayerListItemCtrl;

                if (listItem.IsSelected)
                    return;

                if (_selectedIndex != -1)
                {
                    MediaPlayerListItemCtrl selectedListItem = (MediaPlayerListItemCtrl)stackPanelItems.Children[_selectedIndex];
                    selectedListItem.IsSelected = false;
                }

                listItem.IsSelected = true;
                _selectedIndex = listItem.Index;

                ScrollToItem(listItem);
            }
        }

        void listItem_ListItemSelect(object sender, EventArgs e)
        {
            if (sender is MediaPlayerListItemCtrl)
            {
                MediaPlayerListItemCtrl listItem = sender as MediaPlayerListItemCtrl;

                if (ItemSelectionHandler != null)
                    ItemSelectionHandler(listItem.PortableObject, _inAudio ? MediaFileType.Audio : MediaFileType.Video);
            }
        }

        public void MoveNext()
        {
            UpdateContent(true);
        }

        public void MovePrevious()
        {
            UpdateContent(false);
        }

        private void UpdateContent(bool next)
        {
            if (_totalItems <= 0)
                return;

            if (_selectedIndex != -1)
            {
                MediaPlayerListItemCtrl selectedListItem = (MediaPlayerListItemCtrl)stackPanelItems.Children[_selectedIndex];
                selectedListItem.IsSelected = false;
            }

            if (_selectedIndex == -1)
            {
                _selectedIndex = 0;
            }
            else
            {
                if (next)
                    _selectedIndex++;
                else
                    _selectedIndex--;
            }

            if (_selectedIndex >= _totalItems)
                _selectedIndex = 0;

            if (_selectedIndex < 0)
                _selectedIndex = _totalItems - 1;

            MediaPlayerListItemCtrl listItem = (MediaPlayerListItemCtrl)stackPanelItems.Children[_selectedIndex];
            listItem.IsSelected = true;

            if (ItemSelectionHandler != null)
                ItemSelectionHandler(listItem.PortableObject, _inAudio ? MediaFileType.Audio : MediaFileType.Video);

            ScrollToItem(listItem);
        }

        public void StopControl()
        {
            MediaContentManager.Instance.MediaFound -= Instance_MediaFound;
            MediaContentManager.Instance.Stop();
//            PortableDeviceHelper.Instance.LoadedCompleted -= Instance_LoadedCompleted;
//            PortableDeviceHelper.Instance.Stop();
        }

        private void ScrollToItem(MediaPlayerListItemCtrl item)
        {
            var point = item.TranslatePoint(new Point(), stackPanelItems);

            double itemOffsetTop = point.Y;
            double itemOffsetBottom = point.Y + item.ActualHeight;
            double offset = 0;
            bool needUpdate = false;

            if (itemOffsetBottom > (scrollViewerItems.ContentVerticalOffset + scrollViewerItems.ViewportHeight - item.ActualHeight))
            {
                offset = itemOffsetBottom - scrollViewerItems.ViewportHeight + item.ActualHeight;
                needUpdate = true;
            }
            else if (itemOffsetTop < scrollViewerItems.ContentVerticalOffset)
            {
                offset = itemOffsetTop;
                needUpdate = true;
            }

            if (needUpdate)
                scrollViewerItems.ScrollToVerticalOffset(offset);
        }

        private void gridAudio_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_inAudio)
                return;

            ShowAudio();
        }

        private void gridVideo_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_inAudio)
                return;

            ShowVideo();
        }

        private void SwapImage(Image sourceImage, string imagePath)
        {
            if (System.IO.File.Exists(imagePath))
            {
                sourceImage.Source = BitmapImageHelper.NewBitmapImage(new Uri(imagePath, UriKind.RelativeOrAbsolute));
            }
        }
    }
}
