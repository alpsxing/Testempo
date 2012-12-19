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
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class NewUser : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties

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
                    if (Helper.CheckValidChar(_userName, Helper.CheckMethod.CharNum) == true)
                    {
                        if (_userName.Length < Consts.USER_NAME_MIN_LENGTH ||
                            _userName.Length > Consts.USER_NAME_PASSWORD_MAX_LENGTH)
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
                    else
                    {
                        UserNameFG = Brushes.Red;
                        UserNameOK = false;
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

        private string _password = "";
        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                _password = value;
                if (string.IsNullOrEmpty(_password))
                {
                    PasswordFG = Brushes.Red;
                    PasswordOK = false;
                }
                else
                {
                    if (Helper.CheckValidChar(_password, Helper.CheckMethod.CharNum) == true)
                    {
                        if (string.Compare(_password, _passwordAgain, false) == 0)
                        {
                            if (_password.Length < Consts.USER_PASSWORD_MIN_LENGTH ||
                                _password.Length > Consts.USER_NAME_PASSWORD_MAX_LENGTH)
                            {
                                PasswordOK = false;
                                PasswordFG = Brushes.Red;
                            }
                            else
                            {
                                PasswordOK = true;
                                PasswordFG = Brushes.Black;
                            }
                        }
                        else
                        {
                            PasswordOK = false;
                            PasswordFG = Brushes.Red;
                        }
                    }
                    else
                    {
                        PasswordOK = false;
                        PasswordFG = Brushes.Red;
                    }
                }
                NotifyPropertyChanged("Password");
            }
        }

        private string _passwordAgain = "";
        public string PasswordAgain
        {
            get
            {
                return _passwordAgain;
            }
            set
            {
                _passwordAgain = value;
                if (string.IsNullOrEmpty(_passwordAgain))
                {
                    PasswordFG = Brushes.Red;
                    PasswordOK = false;
                }
                else
                {
                    if (Helper.CheckValidChar(_password, Helper.CheckMethod.CharNum) == true)
                    {
                        if (string.Compare(_password, _passwordAgain, false) == 0)
                        {
                            if (_passwordAgain.Length < Consts.USER_PASSWORD_MIN_LENGTH ||
                                _passwordAgain.Length > Consts.USER_NAME_PASSWORD_MAX_LENGTH)
                            {
                                PasswordOK = false;
                                PasswordFG = Brushes.Red;
                            }
                            else
                            {
                                PasswordOK = true;
                                PasswordFG = Brushes.Black;
                            }
                        }
                        else
                        {
                            PasswordFG = Brushes.Red;
                            PasswordOK = false;
                        }
                    }
                    else
                    {
                        PasswordFG = Brushes.Red;
                        PasswordOK = false;
                    }
                }
                NotifyPropertyChanged("PasswordAgain");
            }
        }

        private bool _passwordOK = false;
        public bool PasswordOK
        {
            get
            {
                return _passwordOK;
            }
            set
            {
                _passwordOK = value;
                NotifyPropertyChanged("PasswordOK");
                NotifyPropertyChanged("InputOK");
            }
        }

        private Brush _passwordFG = Brushes.Red;
        public Brush PasswordFG
        {
            get
            {
                return _passwordFG;
            }
            set
            {
                _passwordFG = value;
                NotifyPropertyChanged("PasswordFG");
            }
        }

        public bool InputOK
        {
            get
            {
                bool b = UserNameOK && PasswordOK;
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

        private string _newPermision = "2";
        public string NewPermission
        {
            get
            {
                return _newPermision;
            }
            set
            {
                _newPermision = value;
            }
        }

        private string _permission = "";
        public string Permission
        {
            get
            {
                return _permission;
            }
            set
            {
                _permission = value;
                if (string.IsNullOrWhiteSpace(_permission))
                {
                    _permission = "2";
                }
                else
                {
                    if(_permission != "0" && _permission != "1" &&_permission != "2")
                        _permission = "2";
                }
                NotifyPropertyChanged("Permission");
                NotifyPropertyChanged("PermissionEnabled");
            }
        }

        public bool PermissionEnabled
        {
            get
            {
                if (_permission == "0")
                    return true;
                else
                    return false;
            }
        }

        #endregion

        public NewUser(string permission)
        {
            InitializeComponent();

            DataContext = this;

            Permission = permission;
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            if (cbPermission.SelectedIndex == 0)
                NewPermission = "2";
            else
                NewPermission = "1";
            DialogResult = true;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_Load(object sender, RoutedEventArgs e)
        {
            txtUserName.Focus();
        }

        private void Password_Changed(object sender, RoutedEventArgs e)
        {
            Password = pbPassword.Password;
        }

        private void PasswordAgain_Changed(object sender, RoutedEventArgs e)
        {
            PasswordAgain = pbPasswordAgain.Password;
        }
    }
}
