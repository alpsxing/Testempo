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

namespace Bumblebee.ExtSetCmd
{
    /// <summary>
    /// Interaction logic for StateConfiguration.xaml
    /// </summary>
    public partial class StateConfiguration : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties

        private string _d2Label = "自定义";
        public string D2Label
        {
            get
            {
                return _d2Label;
            }
            set
            {
                _d2Label = value;
                NotifyPropertyChanged("D2Label");
            }
        }

        private string _d1Label = "自定义";
        public string D1Label
        {
            get
            {
                return _d1Label;
            }
            set
            {
                _d1Label = value;
                NotifyPropertyChanged("D1Label");
            }
        }

        private string _d0Label = "自定义";
        public string D0Label
        {
            get
            {
                return _d0Label;
            }
            set
            {
                _d0Label = value;
                NotifyPropertyChanged("D0Label");
            }
        }

        private string _d7 = "高有效";
        public string D7
        {
            get
            {
                return _d7;
            }
            set
            {
                _d7 = value;
                NotifyPropertyChanged("D7");
            }
        }

        private string _d6 = "高有效";
        public string D6
        {
            get
            {
                return _d6;
            }
            set
            {
                _d6 = value;
                NotifyPropertyChanged("D6");
            }
        }

        private string _d5 = "高有效";
        public string D5
        {
            get
            {
                return _d5;
            }
            set
            {
                _d5 = value;
                NotifyPropertyChanged("D5");
            }
        }

        private string _d4 = "高有效";
        public string D4
        {
            get
            {
                return _d4;
            }
            set
            {
                _d4 = value;
                NotifyPropertyChanged("D4");
            }
        }

        private string _d3 = "高有效";
        public string D3
        {
            get
            {
                return _d3;
            }
            set
            {
                _d3 = value;
                NotifyPropertyChanged("D3");
            }
        }

        private string _d2 = "高有效";
        public string D2
        {
            get
            {
                return _d2;
            }
            set
            {
                _d2 = value;
                NotifyPropertyChanged("D2");
            }
        }

        private string _d1 = "高有效";
        public string D1
        {
            get
            {
                return _d1;
            }
            set
            {
                _d1 = value;
                NotifyPropertyChanged("D1");
            }
        }

        private string _d0 = "高有效";
        public string D0
        {
            get
            {
                return _d0;
            }
            set
            {
                _d0 = value;
                NotifyPropertyChanged("D0");
            }
        }

        #endregion

        public StateConfiguration(string d2Label = "自定义", string d1Label = "自定义", string d0Label = "自定义")
        {
            InitializeComponent();

            DataContext = this;

            D2Label = d2Label;
            D1Label = d1Label;
            D0Label = d0Label;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

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
