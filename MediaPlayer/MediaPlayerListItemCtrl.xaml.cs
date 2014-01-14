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

namespace MediaPlayer
{
    /// <summary>
    /// Interaction logic for MediaPlayerListItemCtrl.xaml
    /// </summary>
    public partial class MediaPlayerListItemCtrl : UserControl
    {
        private string _fileName;
        private int _index;
        private bool _isSelected;
        private bool _isAlt;

        public event EventHandler ListItemSelect;
        public event EventHandler ListItemHighlight;

        public MediaPlayerListItemCtrl()
        {
            InitializeComponent();

            MouseDown += new MouseButtonEventHandler(MediaPlayerListItemCtrl_MouseDown);

            _fileName = string.Empty;

        }

        void MediaPlayerListItemCtrl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ListItemHighlight != null)
                ListItemHighlight(this, new EventArgs());
        }

        private void gridSelect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ListItemSelect != null)
                ListItemSelect(this, new EventArgs());
        }

        public bool IsAlt
        {
            set
            {
                _isAlt = value;

                if (!_isSelected)
                {
                    imageSelected.Visibility = System.Windows.Visibility.Hidden;
                    rectangleAlt.Visibility = _isAlt ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                    gridSelect.Visibility = System.Windows.Visibility.Hidden;
                }
            }
            get
            {
                return _isAlt;
            }
        }

        public bool IsSelected
        {
            set
            {
                _isSelected = value;

                if (_isSelected)
                {
                    imageSelected.Visibility = System.Windows.Visibility.Visible;
                    rectangleAlt.Visibility = System.Windows.Visibility.Hidden;
                    gridSelect.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    IsAlt = _isAlt;
                }
            }
            get
            {
                return _isSelected;
            }
        }

        public string FileName
        {
            set
            {
                _fileName = value;

                if (_fileName != string.Empty)
                    textBlockTitle.Text = _fileName;
            }
            get
            {
                return _fileName;
            }
        }

        public int Index
        {
            set { _index = value; }
            get { return _index; }
        }
    }
}
