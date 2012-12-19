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
    public partial class LoginWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties
        
        private string _serverIP = "";
        public string ServerIP
        {
            get
            {
                return _serverIP;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value) == false)
                {
                    IPAddress ipad = null;
                    if (IPAddress.TryParse(value, out ipad) == true)
                    {
                        ServerIPFG = Brushes.Black;
                        ServerIPOK = true;
                    }
                    else
                    {
                        ServerIPFG = Brushes.Red;
                        ServerIPOK = false;
                    }
                    _serverIP = value.Trim();
                }
                else
                {
                    ServerIPFG = Brushes.Red;
                    ServerIPOK = false;
                    _serverIP = "";
                }
                NotifyPropertyChanged("ServerIP");
            }
        }

        private bool _serverIPOK = false;
        public bool ServerIPOK
        {
            get
            {
                return _serverIPOK;
            }
            set
            {
                _serverIPOK = value;
                NotifyPropertyChanged("ServerIPOK");
                NotifyPropertyChanged("InputOK");
            }
        }

        private Brush _serverIPFG = Brushes.Red;
        public Brush ServerIPFG
        {
            get
            {
                return _serverIPFG;
            }
            set
            {
                _serverIPFG = value;
                NotifyPropertyChanged("ServerIPFG");
            }
        }

        private int _serverPort = Consts.TERM_PORT;
        public int ServerPort
        {
            get
            {
                return _serverPort;
            }
            set
            {
                if (value < Consts.MIN_PORT_NUMBER)
                {
                    ServerPortFG = Brushes.Red;
                    ServerPortOK = false;
                    _serverPort = Consts.MIN_PORT_NUMBER;
                }
                else
                {
                    ServerPortFG = Brushes.Black;
                    ServerPortOK = true;
                    _serverPort = value;
                }
                NotifyPropertyChanged("ServerPort");
                NotifyPropertyChanged("ServerPortString");
            }
        }

        public string ServerPortString
        {
            get
            {
                return ServerPort.ToString();
            }
            set
            {
                int i = Consts.TERM_PORT;
                if (int.TryParse(value, out i) == true)
                {
                    if (i < Consts.MIN_PORT_NUMBER)
                    {
                        ServerPortFG = Brushes.Red;
                        ServerPortOK = false;
                        _serverPort = Consts.MIN_PORT_NUMBER;
                    }
                    else
                    {
                        ServerPortOK = true;
                        ServerPortFG = Brushes.Black;
                        _serverPort = i;
                    }
                }
                else
                {
                    ServerPortFG = Brushes.Red;
                    ServerPortOK = false;
                }
                NotifyPropertyChanged("ServerPort");
                NotifyPropertyChanged("ServerPortString");
            }
        }

        private bool _serverPortOK = false;
        public bool ServerPortOK
        {
            get
            {
                return _serverPortOK;
            }
            set
            {
                _serverPortOK = value;
                NotifyPropertyChanged("ServerPortOK");
                NotifyPropertyChanged("InputOK");
            }
        }

        private Brush _serverPortFG = Brushes.Red;
        public Brush ServerPortFG
        {
            get
            {
                return _serverPortFG;
            }
            set
            {
                _serverPortFG = value;
                NotifyPropertyChanged("ServerPortFG");
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
                    UserNameOK = true;
                    UserNameFG = Brushes.Black;
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
                    PasswordOK = true;
                    PasswordFG = Brushes.Black;
                }
                NotifyPropertyChanged("Password");
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
                bool b = ServerIPOK && ServerPortOK && UserNameOK && PasswordOK;
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

        public LoginWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            Socket s = null;
            IPAddress server = null;
            IPEndPoint iep = null;
            string permission = "2";
            bool logged = false;
            try
            {
                s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server = IPAddress.Parse(ServerIP);
                iep = new IPEndPoint(server, ServerPort);
                s.SendTimeout = Consts.MAN_TIMEOUT;
                s.ReceiveTimeout = Consts.MAN_TIMEOUT;
                s.Connect(iep);
                if(s.Connected)
                {
                    byte[] ba = new byte[Consts.SOCKET_RECEIVING_BUFFER_LENGTH];
                    int len = 0;
                    byte[] bareq = InformationTransferLibrary.Helper.ComposeResponseBytes(Consts.MAN_LOGIN, UserName + "\t" + Password);
                    s.Send(bareq);
                    len = s.Receive(ba);
                    Tuple<string, byte[], string, string> resp = Helper.ExtractSocketResponse(ba, len);
                    if (resp == null)
                    {
                        MessageBox.Show("Cannot login : unknown response from server", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        s.Shutdown(SocketShutdown.Both);
                        s.Disconnect(false);
                        s.Close();
                        s.Dispose();
                    }
                    else
                    {
                        switch (resp.Item1)
                        {
                            default:
                                MessageBox.Show("Cannot login : unknown response from server", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                break;
                            case Consts.MAN_LOGIN_OK:
                                logged = true;
                                permission = resp.Item3;
                                Visibility = System.Windows.Visibility.Collapsed;
                                MainWindow mw = new MainWindow(s, UserName, Password, permission, ServerIP, ServerPort);
                                mw.ShowDialog();
                                break;
                            case Consts.MAN_LOGIN_ERR:
                                logged = true;
                                MessageBox.Show("Cannot login : " + resp.Item3, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                break;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Cannot login : cannot connect server", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                s.Shutdown(SocketShutdown.Both);
                s.Disconnect(false);
                s.Close();
                s.Dispose();
            }
            catch (Exception ex)
            {
                if(logged == false)
                    MessageBox.Show("Cannot login : " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Helper.SafeCloseSocket(s);
            }
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void LoadConfig()
        {
            try
            {
                StreamReader sr = new StreamReader(Consts.DEFAULT_DIRECTORY + @"\manserv.cfg");
                string strLine = null;
                int i = 0;
                while (true)
                {
                    strLine = sr.ReadLine();
                    if (string.IsNullOrWhiteSpace(strLine))
                        break;
                    if (i == 0)
                    {
                        ServerIP = EncryptDecrypt.Decrypt(strLine.Trim());
                    }
                    else if (i == 1)
                    {
                        ServerPortString = EncryptDecrypt.Decrypt(strLine.Trim());
                    }
                    else if (i == 2)
                    {
                        UserName = EncryptDecrypt.Decrypt(strLine.Trim());
                    }
                    else
                        break;

                    i++;
                }
                sr.Close();
                sr.Dispose();
            }
            catch (Exception)
            {
                ServerIP = "127.0.0.1";
                ServerPort = Consts.MAN_PORT;
                UserName = "";
            }
            Password = "";
        }

        private void SaveConfig()
        {
            try
            {
                StreamWriter sw = new StreamWriter(Consts.DEFAULT_DIRECTORY + @"\manserv.cfg");
                sw.WriteLine(EncryptDecrypt.Encrypt(ServerIP));
                sw.WriteLine(EncryptDecrypt.Encrypt(ServerPortString));
                sw.WriteLine(EncryptDecrypt.Encrypt(UserName));
                sw.Close();
                sw.Dispose();
            }
            catch (Exception)
            {
            }
        }

        private void Window_Load(object sender, RoutedEventArgs e)
        {
            LoadConfig();
            txtServerIP.Focus();
        }

        private void Password_Changed(object sender, RoutedEventArgs e)
        {
            Password = pbPassword.Password;
        }
    }
}
