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

namespace MediaPlayer.Controls
{
    /// <summary>
    /// Interaction logic for ThreeStateButton.xaml
    /// </summary>
    public partial class ThreeStateButton : UserControl
    {
        private bool _isDisabled;
        private bool _isSelectedDisabled;
        private bool _autoRepeat;
        private System.Timers.Timer _autoRepeatTimer;
        private int _autoRepeatCount;
        private bool _isToggleMode;
        private bool _toggleModeSelected;
        private bool _isDown;
        private Thickness _touchMargin;
        
        public event EventHandler ButtonClick;

        public ThreeStateButton()
        {
            InitializeComponent();

            _isDisabled = false;
            _isSelectedDisabled = false;
            Tag = null;
            _autoRepeat = false;
            _autoRepeatTimer = null;
            _autoRepeatCount = 0;
            _isToggleMode = false;
            _toggleModeSelected = false;
            _isDown = false;
           
            MouseDown += new MouseButtonEventHandler(ThreeStateButton_MouseDown);
            MouseUp += new MouseButtonEventHandler(ThreeStateButton_MouseUp);
            MouseLeave += new MouseEventHandler(ThreeStateButton_MouseLeave);
            Loaded += new RoutedEventHandler(ThreeStateButton_Loaded);
        }

        void ThreeStateButton_Loaded(object sender, RoutedEventArgs e)
        {
            if (textBlockText.ActualWidth > (imageUp.ActualWidth - 12))
            {
                textBlockText.Text = textBlockText.Text.Replace(' ', '\n');
                textBlockText.TextAlignment = TextAlignment.Center;
            }
        }

        void ThreeStateButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDisabled || _isSelectedDisabled || !_isDown)
                return;

            imageUp.Visibility = System.Windows.Visibility.Visible;
            imageDown.Visibility = System.Windows.Visibility.Hidden;

            if (_autoRepeatTimer != null)
                _autoRepeatTimer.Stop();

            _isDown = false;
            e.Handled = true;
        }

        void ThreeStateButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDisabled || _isSelectedDisabled || !_isDown)
                return;

            if (!_isToggleMode)
            {
                imageUp.Visibility = System.Windows.Visibility.Visible;
                imageDown.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                _toggleModeSelected = !_toggleModeSelected;

                imageUp.Visibility = _toggleModeSelected ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                imageDown.Visibility = _toggleModeSelected ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Visible;
            }

            if (_autoRepeatTimer != null)
                _autoRepeatTimer.Stop();
            
            if (ButtonClick != null)
                ButtonClick(this, new ThreeStateButtonArgs(false, _autoRepeatCount));

            _isDown = false;
            e.Handled = true;
        }

        void ThreeStateButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_isDisabled || _isSelectedDisabled)
                return;

            // Check within touch margin
            Point point = e.GetPosition(this);
            if (point.X < _touchMargin.Left)
                return;
            if (point.X > (ActualWidth - _touchMargin.Left))
                return;
            if (point.Y < _touchMargin.Top)
                return;
            if (point.Y > (ActualHeight - _touchMargin.Bottom))
                return;

            if (!_isToggleMode)
            {
                imageUp.Visibility = System.Windows.Visibility.Hidden;
                imageDown.Visibility = System.Windows.Visibility.Visible;
            }

            if (_autoRepeatTimer != null)
            {
                _autoRepeatCount = 0;
                _autoRepeatTimer.Interval = 1000;
                _autoRepeatTimer.Start();                
            }

            _isDown = true;
            e.Handled = true;
        }

        public bool IsSelectedDisabled
        {
            set
            {
                _isToggleMode = false;
                _isSelectedDisabled = value;
                imageUp.Visibility = _isSelectedDisabled ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Visible;
                imageDown.Visibility = _isSelectedDisabled ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                imageDisabled.Visibility = _isSelectedDisabled ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Hidden;
            }
            get
            {
                return _isSelectedDisabled;
            }
        }

        public string UpImage
        {
            set
            {
                if (value == null)
                    return;

                if (System.IO.File.Exists(value))
                {
                    try
                    {
                        imageUp.Source = BitmapImageHelper.NewBitmapImage(new Uri(value, UriKind.RelativeOrAbsolute));
                    }
                    catch { }
                }
            }
        }

        public string DownImage
        {
            set
            {
                if (value == null)
                    return;

                if (System.IO.File.Exists(value))
                {
                    try
                    {
                        imageDown.Source = BitmapImageHelper.NewBitmapImage(new Uri(value, UriKind.RelativeOrAbsolute));
                    }
                    catch { }
                }
            }
        }

        public string DisabledImage
        {
            set
            {
                if (value == null)
                    return;

                if (System.IO.File.Exists(value))
                {
                    try
                    {
                        imageDisabled.Source = BitmapImageHelper.NewBitmapImage(new Uri(value, UriKind.RelativeOrAbsolute));
                    }
                    catch { }
                }
            }
        }

        public bool IsDisabled
        {
            set
            {
                _isDisabled = value;
                _isToggleMode = false;
                imageUp.Visibility = _isDisabled ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Visible;
                imageDown.Visibility = _isDisabled ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Hidden;
                imageDisabled.Visibility = _isDisabled ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            }
            get
            {
                return _isDisabled;
            }
        }

        public bool IsToggleMode
        {
            set
            {
                _isToggleMode = value;
                _isDisabled = false;
                _isSelectedDisabled = false;
                _toggleModeSelected = false;
                imageUp.Visibility = _isDisabled ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Visible;
                imageDown.Visibility = _isDisabled ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Hidden;
                imageDisabled.Visibility = _isDisabled ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            }
            get
            {
                return _isDisabled;
            }
        }

        public string Text
        {
            set
            {
                if (value == null)
                    return;

                textBlockText.Text = value;
            }
        }

        public Thickness TextMargin
        {
            set
            {
                textBlockText.Margin = value;
            }
        }

        public FontFamily TextFontFamily
        {
            set
            {
                textBlockText.FontFamily = value;
            }
        }

        public double TextFontSize
        {
            set
            {
                textBlockText.FontSize = value;
            }
        }

        public Brush TextForeground
        {
            set
            {
                textBlockText.Foreground = value;
            }
        }

        public TextBlock TextBlock
        {
            get
            {
                return textBlockText;
            }
        }

        public TextWrapping TextWrapping
        {
            set
            {
                textBlockText.TextWrapping = value;
            }
        }

        public TextAlignment TextAlignment
        {
            set
            {
                textBlockText.TextAlignment = value;
            }
        }

        public bool AutoRepeat
        {
            set
            {
                _autoRepeat = value;

                if (_autoRepeat)
                {
                    _autoRepeatTimer = new System.Timers.Timer();
                    _autoRepeatTimer.Interval = 1000;
                    _autoRepeatTimer.Elapsed += new System.Timers.ElapsedEventHandler(_autoRepeatTimer_Elapsed);
                }
                else
                {
                    _autoRepeatTimer = null;
                }
            }
            get
            {
                return _autoRepeat;
            }
        }

        void _autoRepeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Timers.ElapsedEventHandler(SAFE_autoRepeatTimer_Elapsed), sender, e);
        }

        void SAFE_autoRepeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (ButtonClick != null)
                ButtonClick(this, new ThreeStateButtonArgs(true, _autoRepeatCount));

            if (_autoRepeatCount == 0)
            {
                _autoRepeatTimer.Stop();
                _autoRepeatTimer.Interval = 500;
                _autoRepeatTimer.Start();
            }
            else if(_autoRepeatCount == 4)
            {
                _autoRepeatTimer.Stop();
                _autoRepeatTimer.Interval = 100;
                _autoRepeatTimer.Start();
            }

            _autoRepeatCount++;
        }

        public bool IsToggleSelected
        {
            get
            {
                return _toggleModeSelected;
            }
            set
            {
                _toggleModeSelected = value;

                imageUp.Visibility = _toggleModeSelected ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                imageDown.Visibility = _toggleModeSelected ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Visible;
            }
        }

        public bool StretchImages
        {
            set
            {
                imageUp.Stretch = value ? Stretch.Fill : Stretch.Uniform;
                imageDown.Stretch = value ? Stretch.Fill : Stretch.Uniform;
                imageDisabled.Stretch = value ? Stretch.Fill : Stretch.Uniform;
            }
        }

        public Thickness TouchMargin
        {
            get { return _touchMargin; }
            set { _touchMargin = value; }
        }

        public string TextBlockStyle
        {
            set
            {
                textBlockText.SetResourceReference(DataContextProperty, value);
            }
        }

        public new object Tag { get; set; }
    }

    public class ThreeStateButtonArgs : EventArgs
    {
        public ThreeStateButtonArgs(bool isAutoRepeat, int autoRepeatCount)
        {
            IsAutoRepeat = isAutoRepeat;
            AutoRepeatCount = autoRepeatCount;
        }

        public bool IsAutoRepeat { get; set; }
        public int AutoRepeatCount { get; set; }
    }
}
