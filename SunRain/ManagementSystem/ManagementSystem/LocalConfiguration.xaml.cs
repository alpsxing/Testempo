using System;
using System.Collections.Generic;
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

using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;

using System.Net;
using System.Net.Sockets;

using System.Threading;
using System.Threading.Tasks;

using InformationTransferLibrary;

namespace ManagementSystem
{
	/// <summary>
	/// Interaction logic for OneLineEditor.xaml
	/// </summary>
	public partial class LocalConfiguration : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public LocalConfiguration(
            int maxLog = Consts.MAX_LOG_COUNT,
            int maxDispLog = Consts.MAX_LOG_DISPLAY_COUNT)
        {
            InitializeComponent();

            DataContext = this;

            MaxLogCount = maxLog;
            MaxLogDisplayCount = maxDispLog;
        }

        private int _maxLogDisplayCount = Consts.MAX_LOG_DISPLAY_COUNT;
        public int MaxLogDisplayCount
        {
            get
            {
                return _maxLogDisplayCount;
            }
            set
            {
                if (value < Consts.MIN_MAX_LOG_DISPLAY_COUNT)
                {
                    MaxLogDisplayCountFG = Brushes.Red;
                    MaxLogDisplayCountOK = false;
                }
                else
                {
                    MaxLogDisplayCountOK = true;
                    MaxLogDisplayCountFG = Brushes.Black;
                    _maxLogDisplayCount = value;
                }
                NotifyPropertyChanged("MaxLogDisplayCount");
                NotifyPropertyChanged("MaxLogDisplayCountString");
            }
        }

        public string MaxLogDisplayCountString
        {
            get
            {
                return MaxLogDisplayCount.ToString();
            }
            set
            {
                int i = Consts.MAX_LOG_DISPLAY_COUNT;
                if (int.TryParse(value, out i) == true)
                {
                    if (i < Consts.MIN_MAX_LOG_DISPLAY_COUNT)
                    {
                        MaxLogDisplayCountFG = Brushes.Red;
                        MaxLogDisplayCountOK = false;
                    }
                    else
                    {
                        MaxLogDisplayCount = i;
                        MaxLogDisplayCountOK = true;
                        MaxLogDisplayCountFG = Brushes.Black;
                    }
                }
                else
                {
                    MaxLogDisplayCountFG = Brushes.Red;
                    MaxLogDisplayCountOK = false;
                }
                NotifyPropertyChanged("MaxLogDisplayCount");
                NotifyPropertyChanged("MaxLogDisplayCountString");
            }
        }

        private bool _maxLogDisplayCountOK = false;
        public bool MaxLogDisplayCountOK
        {
            get
            {
                return _maxLogDisplayCountOK;
            }
            set
            {
                _maxLogDisplayCountOK = value;
                NotifyPropertyChanged("MaxLogDisplayCountOK");
                NotifyPropertyChanged("InputOK");
            }
        }

        private Brush _maxLogDisplayCountFG = Brushes.Black;
        public Brush MaxLogDisplayCountFG
        {
            get
            {
                return _maxLogDisplayCountFG;
            }
            set
            {
                _maxLogDisplayCountFG = value;
                NotifyPropertyChanged("MaxLogDisplayCountFG");
            }
        }

        private int _maxLogCount = Consts.MAX_LOG_COUNT;
        public int MaxLogCount
        {
            get
            {
                return _maxLogCount;
            }
            set
            {
                if (value < Consts.MIN_MAX_LOG_COUNT)
                {
                    MaxLogCountFG = Brushes.Red;
                    MaxLogCountOK = false;
                }
                else
                {
                    _maxLogCount = value;
                    MaxLogCountOK = true;
                    MaxLogCountFG = Brushes.Black;
                }
                NotifyPropertyChanged("MaxLogCount");
                NotifyPropertyChanged("MaxLogCountString");
            }
        }

        public string MaxLogCountString
        {
            get
            {
                return MaxLogCount.ToString();
            }
            set
            {
                int i = Consts.MAX_LOG_COUNT;
                if (int.TryParse(value, out i) == true)
                {
                    if (i < Consts.MIN_MAX_LOG_COUNT)
                    {
                        MaxLogCountFG = Brushes.Red;
                        MaxLogCountOK = false;
                    }
                    else
                    {
                        MaxLogCount = i;
                        MaxLogCountOK = true;
                        MaxLogCountFG = Brushes.Black;
                    }
                }
                else
                {
                    MaxLogCountFG = Brushes.Red;
                    MaxLogCountOK = false;
                }
                NotifyPropertyChanged("MaxLogCount");
                NotifyPropertyChanged("MaxLogCountString");
            }
        }

        private bool _timeoutOK = false;
        public bool MaxLogCountOK
        {
            get
            {
                return _timeoutOK;
            }
            set
            {
                _timeoutOK = value;
                NotifyPropertyChanged("MaxLogCountOK");
                NotifyPropertyChanged("InputOK");
            }
        }

        private Brush _timeoutFG = Brushes.Black;
        public Brush MaxLogCountFG
        {
            get
            {
                return _timeoutFG;
            }
            set
            {
                _timeoutFG = value;
                NotifyPropertyChanged("MaxLogCountFG");
            }
        }

        public bool InputOK
        {
            get
            {
                bool b = MaxLogCountOK && MaxLogDisplayCountOK;
                if (b == true)
                {
                    btnOK.IsDefault = true;
                    btnCancel.IsDefault = false;
                }
                else
                {
                    btnOK.IsDefault = false;
                    btnCancel.IsDefault = true;
                }
                return b;
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

        private void Window_Load(object sender, RoutedEventArgs e)
        {
            txtMaxLog.Focus();
        }
    }
}
