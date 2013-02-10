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
                return (_vehicleIDCode == null) ? "" : _vehicleIDCode.Trim();
            }
            set
            {
                _vehicleIDCode = value;
                if (string.IsNullOrWhiteSpace(_vehicleIDCode))
                    VehicleIDCodeForeground = Brushes.Red;
                else
                {
                    Encoding gb = Encoding.GetEncoding("GB2312");
                    byte[] ba = gb.GetBytes(_vehicleIDCode);
                    if (ba == null || ba.Length != 17)
                        VehicleIDCodeForeground = Brushes.Red;
                    else
                        VehicleIDCodeForeground = Brushes.Black;
                }
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
                return (_vehicleNumberCode == null) ? "" : _vehicleNumberCode.Trim();
            }
            set
            {
                _vehicleNumberCode = value;
                if (string.IsNullOrWhiteSpace(_vehicleNumberCode))
                    VehicleNumberCodeForeground = Brushes.Red;
                else
                {
                    Encoding gb = Encoding.GetEncoding("GB2312");
                    byte[] ba = gb.GetBytes(_vehicleNumberCode);
                    if(ba == null || ba.Length < 6 || ba.Length > 9)
                        VehicleNumberCodeForeground = Brushes.Red;
                    else
                        VehicleNumberCodeForeground = Brushes.Black;
                }
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
                return (_vehicleNumberCategory == null) ? "" : _vehicleNumberCategory.Trim();
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
                {
                    Encoding gb = Encoding.GetEncoding("GB2312");
                    byte[] ba = gb.GetBytes(VehicleIDCode);
                    if (ba == null || ba.Length != 17)
                        return false;
                    else
                    {
                        ba = gb.GetBytes(VehicleNumberCode);
                        if (ba == null || ba.Length < 6 || ba.Length > 9)
                            return false;
                        else
                            return true;
                    }
                }
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

        public VehicleInformation(string vehicleIDCode, string vehicleNumberCode, string vehicleNumberCategory)
        {
            InitializeComponent();

            DataContext = this;

            VehicleIDCode = vehicleIDCode;
            VehicleNumberCode = vehicleNumberCode;
            VehicleNumberCategory = vehicleNumberCategory;
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            VehicleNumberCategory = ((ComboBoxItem)cboxVehicleNumberCategory.SelectedItem).Content.ToString();
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
            if (string.IsNullOrWhiteSpace(VehicleNumberCategory))
            {
                VehicleNumberCategory = ((ComboBoxItem)cboxVehicleNumberCategory.Items[0]).Content.ToString().Trim();
                cboxVehicleNumberCategory.SelectedIndex = 0;
            }
            else
            {
                int index = -1;
                for (int i = 0; i < cboxVehicleNumberCategory.Items.Count; i++)
                {
                    if (string.Compare(VehicleNumberCategory.Trim(), 
                        ((ComboBoxItem)cboxVehicleNumberCategory.Items[i]).Content.ToString().Trim(), 
                        true) == 0)
                    {
                        index = i;
                        break;
                    }
                }
                if (index == -1)
                {
                    VehicleNumberCategory = ((ComboBoxItem)cboxVehicleNumberCategory.Items[0]).Content.ToString().Trim();
                    cboxVehicleNumberCategory.SelectedIndex = 0;
                }
                else
                {
                    VehicleNumberCategory = ((ComboBoxItem)cboxVehicleNumberCategory.Items[index]).Content.ToString().Trim();
                    cboxVehicleNumberCategory.SelectedIndex = index;
                }
            }
        }
    }
}
