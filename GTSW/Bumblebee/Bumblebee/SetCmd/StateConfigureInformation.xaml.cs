using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Bumblebee.SetCmd
{
    /// <summary>
    /// Interaction logic for StateConfigureInformation.xaml
    /// </summary>
    public partial class StateConfigureInformation : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties

        private string _d2 = "自定义";
        public string D2
        {
            get
            {
                return (_d2 == null) ? "" : _d2.Trim();
            }
            set
            {
                _d2 = value;
                if (string.IsNullOrWhiteSpace(_d2))
                    D2Foreground = Brushes.Red;
                else
                    D2Foreground = Brushes.Black;
                NotifyPropertyChanged("D2");
                NotifyPropertyChanged("OKEnabled");
                NotifyPropertyChanged("OKDefault");
                NotifyPropertyChanged("CancelDefault");
            }
        }

        private SolidColorBrush _d2Foreground = Brushes.Red;
        public SolidColorBrush D2Foreground
        {
            get
            {
                return _d2Foreground;
            }
            set
            {
                _d2Foreground = value;
                NotifyPropertyChanged("D2Foreground");
            }
        }

        private string _d1 = "自定义";
        public string D1
        {
            get
            {
                return (_d1 == null) ? "" : _d1.Trim();
            }
            set
            {
                _d1 = value;
                if (string.IsNullOrWhiteSpace(_d1))
                    D1Foreground = Brushes.Red;
                else
                    D1Foreground = Brushes.Black;
                NotifyPropertyChanged("D1");
                NotifyPropertyChanged("OKEnabled");
                NotifyPropertyChanged("OKDefault");
                NotifyPropertyChanged("CancelDefault");
            }
        }

        private SolidColorBrush _d1Foreground = Brushes.Red;
        public SolidColorBrush D1Foreground
        {
            get
            {
                return _d1Foreground;
            }
            set
            {
                _d1Foreground = value;
                NotifyPropertyChanged("D1Foreground");
            }
        }

        private string _d0 = "自定义";
        public string D0
        {
            get
            {
                return (_d0 == null) ? "" : _d0.Trim();
            }
            set
            {
                _d0 = value;
                if (string.IsNullOrWhiteSpace(_d0))
                    D0Foreground = Brushes.Red;
                else
                    D0Foreground = Brushes.Black;
                NotifyPropertyChanged("D0");
                NotifyPropertyChanged("OKEnabled");
                NotifyPropertyChanged("OKDefault");
                NotifyPropertyChanged("CancelDefault");
            }
        }

        private SolidColorBrush _d0Foreground = Brushes.Red;
        public SolidColorBrush D0Foreground
        {
            get
            {
                return _d0Foreground;
            }
            set
            {
                _d0Foreground = value;
                NotifyPropertyChanged("D0Foreground");
            }
        }

        public bool OKEnabled
        {
            get
            {
                if (string.IsNullOrWhiteSpace(D2) ||
                    string.IsNullOrWhiteSpace(D1) ||
                    string.IsNullOrWhiteSpace(D0))
                    return false;
                else
                    return true;
            }
        }

        public bool OKDefault
        {
            get
            {
                return OKEnabled;
            }
        }

        public bool CancelDefault
        {
            get
            {
                return !OKEnabled;
            }
        }

        #endregion

        public StateConfigureInformation(string d2, string d1, string d0)
        {
            InitializeComponent();

            DataContext = this;

            D2 = d2;
            D1 = d1;
            D0 = d0;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(D2))
                txtD2.Focus();
            else if (string.IsNullOrWhiteSpace(D1))
                txtD1.Focus();
            else if (string.IsNullOrWhiteSpace(D0))
                txtD0.Focus();
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
