using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using InformationTransferLibrary;

namespace ServiceConfiguration
{
    /// <summary>
    /// Interaction logic for DTUConfiguration.xaml
    /// </summary>
    public partial class DTUConfiguration : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties

        private string _dtuId = "";
        public string DtuId
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_dtuId))
                    return "";
                else
                    return _dtuId.Trim();
            }
            set
            {
                _dtuId = value;
                if (string.IsNullOrWhiteSpace(_dtuId))
                {
                    DtuIdFG = Brushes.Red;
                    DtuIdOK = false;
                }
                else
                {
                    _dtuId = _dtuId.Trim();
                    if (_dtuId.Length < Consts.DTU_CONFIG_MIN_LENGTH ||
                        _dtuId.Length > Consts.DTU_CONFIG_MAX_LENGTH)
                    {
                        DtuIdFG = Brushes.Red;
                        DtuIdOK = false;
                    }
                    else
                    {
                        DtuIdOK = true;
                        DtuIdFG = Brushes.Black;
                    }
                }
                NotifyPropertyChanged("DtuId");
            }
        }

        private bool _dtuIdOK = false;
        public bool DtuIdOK
        {
            get
            {
                return _dtuIdOK;
            }
            set
            {
                _dtuIdOK = value;
                NotifyPropertyChanged("DtuIdOK");
                NotifyPropertyChanged("InputOK");
            }
        }

        private Brush _dtuIdFG = Brushes.Red;
        public Brush DtuIdFG
        {
            get
            {
                return _dtuIdFG;
            }
            set
            {
                _dtuIdFG = value;
                NotifyPropertyChanged("DtuIdFG");
            }
        }

        private bool _dtuIdEnabled = false;
        public bool DtuIdEnabled
        {
            get
            {
                return _dtuIdEnabled;
            }
            set
            {
                _dtuIdEnabled = value;
                NotifyPropertyChanged("DtuIdEnabled");
            }
        }

        private string _simId = "";
        public string SimId
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_simId))
                    return "";
                else
                    return _simId.Trim();
            }
            set
            {
                _simId = value;
                if (string.IsNullOrWhiteSpace(_simId))
                    _simId = "";
                else
                    _simId = _simId.Trim();
                NotifyPropertyChanged("SimId");
            }
        }

        private bool _simIdEnabled = false;
        public bool SimIdEnabled
        {
            get
            {
                return _simIdEnabled;
            }
            set
            {
                _simIdEnabled = value;
                NotifyPropertyChanged("SimIdEnabled");
            }
        }

        private string _userName = "";
        public string UserName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_userName))
                    return "";
                else
                    return _userName.Trim();
            }
            set
            {
                _userName = value;
                if (string.IsNullOrWhiteSpace(_userName))
                {
                    UserNameFG = Brushes.Red;
                    UserNameOK = false;
                }
                else
                {
                    _userName = _userName.Trim();
                    if (_userName.Length < Consts.DTU_CONFIG_MIN_LENGTH ||
                        _userName.Length > Consts.DTU_CONFIG_MAX_LENGTH * 2)
                    {
                        UserNameFG = Brushes.Red;
                        UserNameOK = false;
                    }
                    else
                    {
                        UserNameOK = true;
                        UserNameFG = Brushes.Black;
                    }
                }
                NotifyPropertyChanged("UserName");
            }
        }

        private bool _userNameOK = false;
        public bool UserNameOK
        {
            get
            {
                return _userNameOK;
            }
            set
            {
                _userNameOK = value;
                NotifyPropertyChanged("UserNameOK");
                NotifyPropertyChanged("InputOK");
            }
        }

        private Brush _userNameFG = Brushes.Red;
        public Brush UserNameFG
        {
            get
            {
                return _userNameFG;
            }
            set
            {
                _userNameFG = value;
                NotifyPropertyChanged("UserNameFG");
            }
        }

        private bool _userNameEnabled = false;
        public bool UserNameEnabled
        {
            get
            {
                return _userNameEnabled;
            }
            set
            {
                _userNameEnabled = value;
                NotifyPropertyChanged("UserNameEnabled");
            }
        }

        private string _userTel = "";
        public string UserTel
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_userTel))
                    return "";
                else
                    return _userTel.Trim();
            }
            set
            {
                _userTel = value;
                if (string.IsNullOrWhiteSpace(_userTel))
                    _userTel = "";
                else
                    _userTel = _simId.Trim();
                NotifyPropertyChanged("UserTel");
            }
        }

        private bool _userTelEnabled = false;
        public bool UserTelEnabled
        {
            get
            {
                return _userTelEnabled;
            }
            set
            {
                _userTelEnabled = value;
                NotifyPropertyChanged("UserTelEnabled");
            }
        }

        public bool InputOK
        {
            get
            {
                bool b = DtuIdOK && UserNameOK;
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

        #endregion

        public enum OpenState
        {
            New,
            Modify,
            View
        }

        public DTUConfiguration(OpenState os = OpenState.New, string dtuId = "",
            string simId = "", string userName = "", string userTel = "")
        {
            InitializeComponent();

            DataContext = this;

            DtuId = dtuId;
            SimId = simId;
            UserName = userName;
            UserTel = userTel;
            switch (os)
            {
                default:
                case OpenState.New:
                    DtuIdEnabled = true;
                    SimIdEnabled = true;
                    UserNameEnabled = true;
                    UserTelEnabled = true;
                    break;
                case OpenState.Modify:
                    DtuIdEnabled = false;
                    SimIdEnabled = true;
                    UserNameEnabled = true;
                    UserTelEnabled = true;
                    break;
                case OpenState.View:
                    DtuIdEnabled = false;
                    SimIdEnabled = false;
                    UserNameEnabled = false;
                    UserTelEnabled = false;
                    break;
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
            txtDtuId.Focus();
        }
    }
}
