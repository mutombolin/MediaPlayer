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

using MediaPlayer.Common;
using MediaPlayer.Managers;

namespace MediaPlayer
{
    /// <summary>
    /// Interaction logic for MediaPlayerListCtrl.xaml
    /// </summary>
    public partial class MediaPlayerListCtrl : UserControl
    {
        private int _selectedIndex;
        private int _totalItems;

        public delegate void ItemSelection(string filename);
        public event ItemSelection ItemSelectionHandler;

        public MediaPlayerListCtrl()
        {
            InitializeComponent();

            _selectedIndex = -1;
            _totalItems = 0;

            Loaded += new RoutedEventHandler(MediaPlayerListCtrl_Loaded);

            MediaContentManager.Instance.MediaFound += new EventHandler(Instance_MediaFound);            
        }

        void MediaPlayerListCtrl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        void Instance_MediaFound(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new EventHandler(SAFE_Instance_MediaFound), sender, e);
        }

        void SAFE_Instance_MediaFound(object sender, EventArgs e)
        {
            MediaContentManager.Instance.Stop();
            ShowAudio();
        }

        void Instance_LoadedCompleted(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new EventHandler(SAFE_Instance_LoadedCompleted), sender, e);
        }

        void SAFE_Instance_LoadedCompleted(object sender, EventArgs e)
        {
            ShowAudio();        
        }

        private void ShowAudio()
        {
            if (MediaContentManager.Instance.IsMusicFilesFound)
                ShowContent(MediaContentManager.Instance.MusicPlayList);
        }

        private void ShowContent(List<PortableDevice.PortableDeviceObject> PlayList)
        {
            _selectedIndex = -1;
            _totalItems = PlayList.Count;

            if (_totalItems <= 0)
                return;

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

                double offset = (scrollViewerItems.VerticalOffset / scrollViewerItems.ViewportHeight) * scrollViewerItems.ViewportHeight;

                scrollViewerItems.ScrollToVerticalOffset(offset);

                System.Diagnostics.Debug.WriteLine(string.Format("ViewportHeight = {0}", scrollViewerItems.ViewportHeight));
            }
        }

        void listItem_ListItemSelect(object sender, EventArgs e)
        {
            if (sender is MediaPlayerListItemCtrl)
            {
                MediaPlayerListItemCtrl listItem = sender as MediaPlayerListItemCtrl;

                if (ItemSelectionHandler != null)
                    ItemSelectionHandler(listItem.FileName);

                double offset = (scrollViewerItems.VerticalOffset / scrollViewerItems.ViewportHeight) * scrollViewerItems.ViewportHeight;

                scrollViewerItems.ScrollToVerticalOffset(offset);
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
                ItemSelectionHandler(listItem.FileName);

            int multiple = (int)(_selectedIndex * listItem.ActualHeight) / (int)(400 - imageTitle.ActualHeight);
            double offset = multiple * (400 - imageTitle.ActualHeight);

            scrollViewerItems.ScrollToVerticalOffset(offset);
            System.Diagnostics.Debug.WriteLine(string.Format("ViewportHeight = {0}", stackPanelItems.VerticalOffset));
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
        
        }
    }
}
