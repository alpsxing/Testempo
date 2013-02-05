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
    /// Interaction logic for SinglePulseCount.xaml
    /// </summary>
    public partial class SinglePulseCount : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties

        private int _pulseCount = 8;
        public int PulseCount
        {
            get
            {
                return _pulseCount;
            }
            set
            {
                _pulseCount = value;
                NotifyPropertyChanged("PulseCount");
            }
        }

        #endregion

        public SinglePulseCount()
        {
            InitializeComponent();

            DataContext = this;
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
