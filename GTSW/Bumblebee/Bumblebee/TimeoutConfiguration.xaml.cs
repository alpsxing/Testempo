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

namespace Bumblebee
{
    /// <summary>
    /// Interaction logic for TimeoutConfiguration.xaml
    /// </summary>
    public partial class TimeoutConfiguration : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties

        private int _timeout = 1000;
        public int TimeOut
        {
            get
            {
                return _timeout;
            }
            set
            {
                _timeout = value;
                NotifyPropertyChanged("TimeOut");
            }
        }

        #endregion

        public TimeoutConfiguration(string timeout)
        {
            InitializeComponent();

            DataContext = this;

            int value = -1;
            if (int.TryParse(timeout, out value) == false)
            {
                TimeOut = 1000;
            }
            else
            {
                if (value < 1000)
                    value = 1000;
                if (value > 120000)
                    value = 120000;
                TimeOut = value;
            }
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
