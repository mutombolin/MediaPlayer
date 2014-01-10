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
            PortableDeviceHelper.Instance.LoadedCompleted += new EventHandler(Instance_LoadedCompleted);
        }

        void MediaPlayerListCtrl_Loaded(object sender, RoutedEventArgs e)
        {
            PortableDeviceHelper.Instance.LoadPortableDevice();
/*
            foreach (string deviceName in PortableDeviceHelper.Instance.DeviceList)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Device Firendly Name = {0}", deviceName));
            }
*/
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
            ShowContent();
        }

        private void ShowContent()
        {
            _selectedIndex = -1;
            _totalItems = PortableDeviceHelper.Instance.VideoList.Count;

            if (_totalItems <= 0)
                return;

            textBlockFileCount.Visibility = System.Windows.Visibility.Visible;

            scrollViewerItems.Visibility = System.Windows.Visibility.Visible;
            textBlockMessage.Visibility = System.Windows.Visibility.Hidden;
            stackPanelItems.Children.Clear();

            bool isAlt = false;
            int index = 0;
            foreach (string name in PortableDeviceHelper.Instance.MusicList)
            {
                MediaPlayerListItemCtrl listItem = new MediaPlayerListItemCtrl();
                listItem.FileName = name;
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
            }
        }

        void listItem_ListItemSelect(object sender, EventArgs e)
        {
            if (sender is MediaPlayerListItemCtrl)
            {
                MediaPlayerListItemCtrl listItem = sender as MediaPlayerListItemCtrl;
            }
        }

        public void StopControl()
        {
            PortableDeviceHelper.Instance.LoadedCompleted -= Instance_LoadedCompleted;
            PortableDeviceHelper.Instance.Stop();
        }
    }
}
