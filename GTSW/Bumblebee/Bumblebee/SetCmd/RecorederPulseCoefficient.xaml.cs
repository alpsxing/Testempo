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
    /// Interaction logic for RecorederPulseCoefficient.xaml
    /// </summary>
    public partial class RecorederPulseCoefficient : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties

        private int _pulseCoefficient = 800;
        public int PulseCoefficient
        {
            get
            {
                return _pulseCoefficient;
            }
            set
            {
                _pulseCoefficient = value;
                NotifyPropertyChanged("PulseCoefficient");
            }
        }

        #endregion

        public RecorederPulseCoefficient(int pulseCoefficient)
        {
            InitializeComponent();

            DataContext = this;

            PulseCoefficient = pulseCoefficient;
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
