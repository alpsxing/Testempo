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
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties

        private string _userName = "admin";
        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                _userName = value;
                NotifyPropertyChanged("UserName");
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
                NotifyPropertyChanged("OKEnabled");
                NotifyPropertyChanged("OKDefault");
                NotifyPropertyChanged("CancelDefault");
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

        public Login()
        {
            InitializeComponent();

            DataContext = this;
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.Compare(UserName, "admin", true) == 0 ||
                string.Compare(UserName, "user", true) == 0 ||
                string.Compare(UserName, "super", true) == 0)
            {
                if (string.IsNullOrWhiteSpace(pwboxPassword.Password))
                {
                    MessageBox.Show("密码为空.", "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (string.Compare(pwboxPassword.Password, "123456", false) == 0)
                {
                    Hide();
                    MainWindow.RunMode rm = MainWindow.RunMode.User;
                    switch (UserName.ToUpper())
                    {
                        default:
                        case "USER":
                            rm = MainWindow.RunMode.User;
                            break;
                        case "SUPER":
                            rm = MainWindow.RunMode.Super;
                            break;
                        case "ADMIN":
                            rm = MainWindow.RunMode.Admin;
                            break;
                    }
                    MainWindow mw = new MainWindow(rm);
                    mw.ShowDialog();
                }
                else
                {
                    MessageBox.Show("密码错误.", "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("用户名错误.", "登录失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("退出\"GB/T 19056-2013数据分析软件\"吗?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                System.Environment.Exit(0);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UserName))
                txtUserName.Focus();
            else
                pwboxPassword.Focus();

            if (UserName.Length < 1 || pwboxPassword.Password.Length < 1)
                OKEnabled = false;
            else
                OKEnabled = true;
        }

        private void UserName_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (UserName.Length < 1 || pwboxPassword.Password.Length < 1)
                OKEnabled = false;
            else
                OKEnabled = true;
        }

        private void Password_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (UserName.Length < 1 || pwboxPassword.Password.Length < 1)
                OKEnabled = false;
            else
                OKEnabled = true;
        }
    }
}
