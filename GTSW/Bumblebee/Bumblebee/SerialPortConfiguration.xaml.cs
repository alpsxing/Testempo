using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
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
    /// Interaction logic for SerialPortConfiguration.xaml
    /// </summary>
    public partial class SerialPortConfiguration : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Variables

        private ObservableCollection<string> _localPortOc = new ObservableCollection<string>();

        #endregion

        #region Properties

        private string _selectedSerialPort = null;
        public string SelectedSerialPort 
        {
            get
            {
                return _selectedSerialPort;
            }
            set
            {
                _selectedSerialPort = value;
                NotifyPropertyChanged("SelectedSerialPort");
            }
        }

        private string _selectedBaud = null;
        public string SelectedBaud
        {
            get
            {
                return _selectedBaud;
            }
            set
            {
                _selectedBaud = value;
                NotifyPropertyChanged("SelectedBaud");
            }
        }

        private string _selectedParity = null;
        public string SelectedParity
        {
            get
            {
                return _selectedParity;
            }
            set
            {
                _selectedParity = value;
                NotifyPropertyChanged("SelectedParity");
            }
        }

        private string _selectedDataBit = null;
        public string SelectedDataBit
        {
            get
            {
                return _selectedDataBit;
            }
            set
            {
                _selectedDataBit = value;
                NotifyPropertyChanged("SelectedDataBit");
            }
        }

        private string _selectedStartBit = null;
        public string SelectedStartBit
        {
            get
            {
                return _selectedStartBit;
            }
            set
            {
                _selectedStartBit = value;
                NotifyPropertyChanged("SelectedStartBit");
            }
        }

        private string _selectedStopBit = null;
        public string SelectedStopBit
        {
            get
            {
                return _selectedStopBit;
            }
            set
            {
                _selectedStopBit = value;
                NotifyPropertyChanged("SelectedStopBit");
            }
        }

        #endregion

        public SerialPortConfiguration()
        {
            InitializeComponent();

            DataContext = this;
            cboxSerialPort.ItemsSource = _localPortOc;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (string s in SerialPort.GetPortNames())
            {
                _localPortOc.Add(s);
            }

            if (_localPortOc.Count > 0)
                cboxSerialPort.SelectedIndex = 0;
            else
                btnOK.IsEnabled = false;
        }
    }
}
