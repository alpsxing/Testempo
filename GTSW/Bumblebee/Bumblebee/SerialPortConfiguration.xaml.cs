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

        public SerialPortConfiguration(string port, string baud, string parity,
            string dataBit, string startBit, string stopBit)
        {
            InitializeComponent();

            DataContext = this;
            cboxSerialPort.ItemsSource = _localPortOc;
            cboxBaud.ItemsSource = MainWindow._bauds;
            cboxParity.ItemsSource = MainWindow._parities;
            cboxDataBit.ItemsSource = MainWindow._dataBits;
            cboxStopBit.ItemsSource = MainWindow._stopBits;

            SelectedSerialPort = port;
            SelectedBaud = baud;
            SelectedParity = parity;
            SelectedDataBit = dataBit;
            SelectedStartBit = startBit;
            SelectedStopBit = stopBit;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string[] ps = SerialPort.GetPortNames();
            foreach (string s in ps)
            {
                _localPortOc.Add(s);
            }

            int index = -1;
            if (_localPortOc.Count > 0)
            {
                index = Helper.FindIndex<string>(ps, SelectedSerialPort);
                if (index < 0)
                {
                    SelectedSerialPort = ps[0];
                    cboxSerialPort.SelectedIndex = 0;
                }
                else
                {
                    cboxSerialPort.SelectedIndex = index;
                }
            }
            else
            {
                cboxSerialPort.IsEnabled = false;
                SelectedSerialPort = "";
                btnOK.IsEnabled = false;
            }

            index = Helper.FindIndex<string>(MainWindow._bauds, SelectedBaud);
            if (index < 0)
            {
                SelectedBaud = MainWindow._bauds[0];
                cboxBaud.SelectedIndex = 0;
            }
            else
            {
                cboxBaud.SelectedIndex = index;
            }

            index = Helper.FindIndex<string>(MainWindow._parities, SelectedParity);
            if (index < 0)
            {
                SelectedParity = MainWindow._parities[0];
                cboxParity.SelectedIndex = 0;
            }
            else
            {
                cboxParity.SelectedIndex = index;
            }

            index = Helper.FindIndex<string>(MainWindow._dataBits, SelectedDataBit);
            if (index < 0)
            {
                SelectedDataBit = MainWindow._dataBits[0];
                cboxDataBit.SelectedIndex = 0;
            }
            else
            {
                cboxDataBit.SelectedIndex = index;
            }

            SelectedStartBit = "1";

            index = Helper.FindIndex<string>(MainWindow._stopBits, SelectedStopBit);
            if (index < 0)
            {
                SelectedStopBit = MainWindow._stopBits[0];
                cboxStopBit.SelectedIndex = 0;
            }
            else
            {
                cboxStopBit.SelectedIndex = index;
            }
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            string[] ps = SerialPort.GetPortNames();
            if(ps == null || ps.Length < 1)
            {
                SelectedSerialPort= "";
            }
            else
            {
                SelectedSerialPort = _localPortOc[cboxSerialPort.SelectedIndex];
            }
            SelectedBaud = MainWindow._bauds[cboxBaud.SelectedIndex];
            SelectedParity = MainWindow._parities[cboxParity.SelectedIndex];
            SelectedDataBit = MainWindow._dataBits[cboxDataBit.SelectedIndex];
            SelectedStartBit = "1";
            SelectedStopBit = MainWindow._stopBits[cboxStopBit.SelectedIndex];

            DialogResult = true;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
