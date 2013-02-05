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
    /// Interaction logic for GetCmdConfiguration.xaml
    /// </summary>
    public partial class GetCmdConfiguration : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties

        private int _unitData = 0;
        public int UnitData
        {
            get
            {
                return _unitData;
            }
            set
            {
                _unitData = value;
                NotifyPropertyChanged("UnitData");
            }
        }

        public string UnitDataRange
        {
            get
            {
                return "取值范围 : " + intudUnitData.Minimum.ToString() + " - " + intudUnitData.Maximum.ToString();
            }
        }

        private DateTime _startDateTime = DateTime.Now;
        public DateTime StartDateTime
        {
            get
            {
                return _startDateTime;
            }
            set
            {
                _startDateTime = value;
                if (DateTime.Compare(StartDateTime, StopDateTime) >= 0)
                    OKEnabled = false;
                else
                    OKEnabled = true;
                NotifyPropertyChanged("StartDateTime");
            }
        }

        private DateTime _stopDateTime = DateTime.Now;
        public DateTime StopDateTime
        {
            get
            {
                return _stopDateTime;
            }
            set
            {
                _stopDateTime = value;
                if (DateTime.Compare(StartDateTime, StopDateTime) >= 0)
                    OKEnabled = false;
                else
                    OKEnabled = true;
                NotifyPropertyChanged("StopDateTime");
            }
        }

        private bool _okEnabled = false;
        public bool OKEnabled
        {
            get
            {
                return _okEnabled;
            }
            set
            {
                _okEnabled = value;
                if (_okEnabled == true)
                    DateTimeForeground = Brushes.Black;
                else
                    DateTimeForeground = Brushes.Red;
                NotifyPropertyChanged("OKEnabled");
                NotifyPropertyChanged("OKDefault");
                NotifyPropertyChanged("CancelDefault");
                NotifyPropertyChanged("DateTimeForeground");
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

        private SolidColorBrush _dateTimeForeground = Brushes.Red;
        public SolidColorBrush DateTimeForeground
        {
            get
            {
                return _dateTimeForeground;
            }
            set
            {
                _dateTimeForeground = value;
                NotifyPropertyChanged("DateTimeForeground");
            }
        }

        #endregion

        public GetCmdConfiguration(string title, DateTime start, DateTime stop, 
            int min, int max, int current)
        {
            InitializeComponent();

            DataContext = this;

            if (title != null)
            {
                if (title.Length > 6)
                    Title = title.Substring(6);
                else
                    Title = title;
            }

            if (DateTime.Compare(start, stop) > 0)
                throw new Exception("start > stop");
            StartDateTime = start;
            StopDateTime = stop;
            if (min > max)
                throw new Exception("min > max");
            if (current < min)
                throw new Exception("current < min");
            if (current > max)
                throw new Exception("current > max");
            intudUnitData.Minimum = min;
            intudUnitData.Maximum = max;
            UnitData = current;
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
