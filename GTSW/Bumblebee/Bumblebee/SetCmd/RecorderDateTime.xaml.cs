using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Interaction logic for RecorderDateTime.xaml
    /// </summary>
    public partial class RecorderDateTime : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Variables

        private Timer _systemModeTimer = null;

        #endregion

        #region Properties

        private DateTime _systemModeDateTime = DateTime.Now;
        public DateTime SystemModeDateTime
        {
            get
            {
                return _systemModeDateTime;
            }
            set
            {
                _systemModeDateTime = value;
                NotifyPropertyChanged("SystemModeDateTime");
                NotifyPropertyChanged("SystemModeDateTimeString");
            }
        }

        public string SystemModeDateTimeString
        {
            get
            {
                return _systemModeDateTime.Year.ToString() + "-" + _systemModeDateTime.Month.ToString() + "-" + _systemModeDateTime.Day.ToString() + " " +
                    _systemModeDateTime.Hour.ToString() + ":" + _systemModeDateTime.Minute.ToString() + ":" + _systemModeDateTime.Second.ToString();
            }
        }

        private DateTime _userModeDateTime = DateTime.Now;
        public DateTime UserModeDateTime
        {
            get
            {
                return _userModeDateTime;
            }
            set
            {
                _userModeDateTime = value;
                NotifyPropertyChanged("UserModeDateTime");
            }
        }

        private bool _isSystemModeDateTime = true;
        public bool IsSystemModeDateTime
        {
            get
            {
                return _isSystemModeDateTime;
            }
            set
            {
                _isSystemModeDateTime = value;
                NotifyPropertyChanged("IsSystemModeDateTime");
                NotifyPropertyChanged("SystemModeDateTimeChecked");
                NotifyPropertyChanged("UserModeDateTimeChecked");
            }
        }

        public bool? SystemModeDateTimeChecked
        {
            get
            {
                return IsSystemModeDateTime;
            }
            set
            {
                if (value == true)
                    IsSystemModeDateTime = true;
            }
        }

        public bool? UserModeDateTimeChecked
        {
            get
            {
                return !IsSystemModeDateTime;
            }
            set
            {
                if (value == true)
                    IsSystemModeDateTime = false;
            }
        }

        #endregion

        public RecorderDateTime(bool isSystemModeDateTime, DateTime userDateTime)
        {
            InitializeComponent();

            DataContext = this;
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            _systemModeTimer.Dispose();
            DialogResult = true;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            _systemModeTimer.Dispose();
            DialogResult = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _systemModeTimer = new Timer(new TimerCallback(SystemModeTimerCallback), null, 0, 1000);
        }

        private void SystemModeTimerCallback(object obj)
        {
            SystemModeDateTime = DateTime.Now;
        }
    }
}
