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
    /// Interaction logic for VehicleInformation.xaml
    /// </summary>
    public partial class VehicleInformation : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties

        private string _vehicleIDCode = "";
        public string VehicleIDCode
        {
            get
            {
                return _vehicleIDCode;
            }
            set
            {
                _vehicleIDCode = value;
                if (string.IsNullOrWhiteSpace(VehicleIDCode))
                    VehicleIDCodeForeground = Brushes.Red;
                else
                    VehicleIDCodeForeground = Brushes.Black;
                NotifyPropertyChanged("VehicleIDCode");
                NotifyPropertyChanged("OKEnabled");
                NotifyPropertyChanged("OKDefault");
                NotifyPropertyChanged("CancelDefault");
            }
        }

        private string _vehicleNumberCode = "";
        public string VehicleNumberCode
        {
            get
            {
                return _vehicleNumberCode;
            }
            set
            {
                _vehicleNumberCode = value;
                if (string.IsNullOrWhiteSpace(VehicleNumberCode))
                    VehicleNumberCodeForeground = Brushes.Red;
                else
                    VehicleNumberCodeForeground = Brushes.Black;
                NotifyPropertyChanged("VehicleNumberCode");
                NotifyPropertyChanged("OKEnabled");
                NotifyPropertyChanged("OKDefault");
                NotifyPropertyChanged("CancelDefault");
            }
        }

        private string _vehicleNumberCategory = "";
        public string VehicleNumberCategory
        {
            get
            {
                return _vehicleNumberCategory;
            }
            set
            {
                _vehicleNumberCategory = value;
                NotifyPropertyChanged("VehicleNumberCategory");
            }
        }

        private Brush _vehicleIDCodeForground = Brushes.Red;
        public Brush VehicleIDCodeForeground
        {
            get
            {
                return _vehicleIDCodeForground;
            }
            set
            {
                _vehicleIDCodeForground = value;
                NotifyPropertyChanged("VehicleIDCodeForeground");
            }
        }

        private Brush _vehicleNumberCodeForground = Brushes.Red;
        public Brush VehicleNumberCodeForeground
        {
            get
            {
                return _vehicleNumberCodeForground;
            }
            set
            {
                _vehicleNumberCodeForground = value;
                NotifyPropertyChanged("VehicleNumberCodeForeground");
            }
        }

        public bool OKEnabled
        {
            get
            {
                if (string.IsNullOrWhiteSpace(VehicleIDCode) ||
                    string.IsNullOrWhiteSpace(VehicleNumberCode))
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

        public VehicleInformation()
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(VehicleIDCode))
                txtVehicleIDCode.Focus();
            else if (string.IsNullOrWhiteSpace(VehicleNumberCode))
                txtVehicleNumberCode.Focus();
        }
    }
}
