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
	public partial class ServerConfiguration : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public ServerConfiguration(string ipAddress = "",
            int port = Consts.TERM_PORT,
            int timeout = Consts.TERM_TIMEOUT,
            int remoteTimeout = Consts.DTU_TIMEOUT)
        {
            InitializeComponent();

            DataContext = this;

            ServerIPAddress = ipAddress;
            ServerPort = port;
            ServerTimeout = timeout;
            RemoteTimeout = remoteTimeout;
        }

        private string _serverIPAddress = "";
        public string ServerIPAddress
        {
            get
            {
                return _serverIPAddress;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value) == false)
                {
                    IPAddress ipad = null;
                    if (IPAddress.TryParse(value, out ipad) == true)
                    {
                        IPAddressFG = Brushes.Black;
                        IPAddressOK = true;
                    }
                    else
                    {
                        IPAddressFG = Brushes.Red;
                        IPAddressOK = false;
                    }
                    _serverIPAddress = value.Trim();
                }
                else
                {
                    IPAddressFG = Brushes.Red;
                    IPAddressOK = false;
                    _serverIPAddress = "";
                }
                NotifyPropertyChanged("ServerIPAddress");
            }
        }

        private bool _ipAddressOK = false;
        public bool IPAddressOK
        {
            get
            {
                return _ipAddressOK;
            }
            set
            {
                _ipAddressOK = value;
                NotifyPropertyChanged("IPAddressOK");
                NotifyPropertyChanged("InputOK");
            }
        }

        private Brush _ipAddressFG = Brushes.Red;
        public Brush IPAddressFG
        {
            get
            {
                return _ipAddressFG;
            }
            set
            {
                _ipAddressFG = value;
                NotifyPropertyChanged("IPAddressFG");
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
                    PortFG = Brushes.Red;
                    PortOK = false;
                    _serverPort = Consts.TERM_PORT;
                }
                else
                {
                    PortFG = Brushes.Black;
                    PortOK = true;
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
                        PortFG = Brushes.Red;
                        PortOK = false;
                    }
                    else
                    {
                        ServerPort = i;
                        PortOK = true;
                        PortFG = Brushes.Black;
                    }
                }
                else
                {
                    PortFG = Brushes.Red;
                    PortOK = false;
                }
                NotifyPropertyChanged("ServerPort");
                NotifyPropertyChanged("ServerPortString");
            }
        }

        private bool _portOK = false;
        public bool PortOK
        {
            get
            {
                return _portOK;
            }
            set
            {
                _portOK = value;
                NotifyPropertyChanged("PortOK");
                NotifyPropertyChanged("InputOK");
            }
        }

        private Brush _portFG = Brushes.Red;
        public Brush PortFG
        {
            get
            {
                return _portFG;
            }
            set
            {
                _portFG = value;
                NotifyPropertyChanged("PortFG");
            }
        }

        private int _remoteTimeout = Consts.DTU_TIMEOUT;
        public int RemoteTimeout
        {
            get
            {
                return _remoteTimeout;
            }
            set
            {
                if (value < Consts.MIN_TIME_OUT)
                {
                    RemoteFG = Brushes.Red;
                    RemoteOK = false;
                    _remoteTimeout = Consts.DTU_TIMEOUT;
                }
                else
                {
                    RemoteOK = true;
                    RemoteFG = Brushes.Black;
                    _remoteTimeout = value;
                }
                NotifyPropertyChanged("RemoteTimeout");
                NotifyPropertyChanged("RemoteTimeoutString");
            }
        }

        public string RemoteTimeoutString
        {
            get
            {
                return RemoteTimeout.ToString();
            }
            set
            {
                int i = Consts.DTU_TIMEOUT;
                if (int.TryParse(value, out i) == true)
                {
                    if (i < Consts.MIN_TIME_OUT)
                    {
                        RemoteFG = Brushes.Red;
                        RemoteOK = false;
                    }
                    else
                    {
                        RemoteTimeout = i;
                        RemoteOK = true;
                        RemoteFG = Brushes.Black;
                    }
                }
                else
                {
                    RemoteFG = Brushes.Red;
                    RemoteOK = false;
                }
                NotifyPropertyChanged("RemoteTimeout");
                NotifyPropertyChanged("RemoteTimeoutString");
            }
        }

        private bool _remoteOK = false;
        public bool RemoteOK
        {
            get
            {
                return _remoteOK;
            }
            set
            {
                _remoteOK = value;
                NotifyPropertyChanged("RemoteOK");
                NotifyPropertyChanged("InputOK");
            }
        }

        private Brush _remoteFG = Brushes.Black;
        public Brush RemoteFG
        {
            get
            {
                return _remoteFG;
            }
            set
            {
                _remoteFG = value;
                NotifyPropertyChanged("RemoteFG");
            }
        }

        private int _serverTimeout = Consts.TERM_TIMEOUT;
        public int ServerTimeout
        {
            get
            {
                return _serverTimeout;
            }
            set
            {
                if (value < Consts.MIN_TIME_OUT)
                {
                    TimeoutFG = Brushes.Red;
                    TimeoutOK = false;
                    _serverTimeout = Consts.TERM_TIMEOUT;
                }
                else
                {
                    _serverTimeout = value;
                    TimeoutOK = true;
                    TimeoutFG = Brushes.Black;
                }
                NotifyPropertyChanged("ServerTimeout");
                NotifyPropertyChanged("ServerTimeoutString");
            }
        }

        public string ServerTimeoutString
        {
            get
            {
                return ServerTimeout.ToString();
            }
            set
            {
                int i = Consts.TERM_TIMEOUT;
                if (int.TryParse(value, out i) == true)
                {
                    if (i < Consts.MIN_TIME_OUT)
                    {
                        TimeoutFG = Brushes.Red;
                        TimeoutOK = false;
                    }
                    else
                    {
                        ServerTimeout = i;
                        TimeoutOK = true;
                        TimeoutFG = Brushes.Black;
                    }
                }
                else
                {
                    TimeoutFG = Brushes.Red;
                    TimeoutOK = false;
                }
                NotifyPropertyChanged("ServerTimeout");
                NotifyPropertyChanged("ServerTimeoutString");
            }
        }

        private bool _timeoutOK = false;
        public bool TimeoutOK
        {
            get
            {
                return _timeoutOK;
            }
            set
            {
                _timeoutOK = value;
                NotifyPropertyChanged("TimeoutOK");
                NotifyPropertyChanged("InputOK");
            }
        }

        private Brush _timeoutFG = Brushes.Black;
        public Brush TimeoutFG
        {
            get
            {
                return _timeoutFG;
            }
            set
            {
                _timeoutFG = value;
                NotifyPropertyChanged("TimeoutFG");
            }
        }

        public bool InputOK
        {
            get
            {
                bool b = IPAddressOK && PortOK && TimeoutOK && RemoteOK;
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

        private bool _inRun = false;
        public bool InRun
        {
            get
            {
                return _inRun;
            }
            set
            {
                _inRun = value;
                NotifyPropertyChanged("InRun");
                NotifyPropertyChanged("NotInRun");
            }
        }

        public bool NotInRun
        {
            get
            {
                return !_inRun;
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

        private void Test_Button_Click(object sender, RoutedEventArgs e)
        {
            InRun = true;
            Task.Factory.StartNew(
                () =>
                {
                    TestTask();
                });
        }

        private void TestTask()
        {
            IPAddress ipad = null;
            if (string.IsNullOrWhiteSpace(ServerIPAddress) || IPAddress.TryParse(ServerIPAddress, out ipad) == false)
            {
                Dispatcher.Invoke((ThreadStart)delegate
                {
                    MessageBox.Show("Invalid server IP address.", "Test Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }, null);
            }
            else
            {
                Socket client = null;
                try
                {
                    client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    //IPAddress local = IPAddress.Parse(ServerIPAddress);
                    IPEndPoint iep = new IPEndPoint(ipad, ServerPort);
                    client.SendTimeout = Consts.TERM_TIMEOUT;
                    client.ReceiveTimeout = Consts.TERM_TIMEOUT;
                    client.Connect(iep);
                    if (client.Connected)
                    {
                        client.Send(Encoding.ASCII.GetBytes(Consts.TERM_TEST_CONN));
                        byte[] bytes = new byte[Consts.SOCKET_RECEIVING_BUFFER_LENGTH];
                        int length = client.Receive(bytes);
                        if (length < 1)
                        {
                            Dispatcher.Invoke((ThreadStart)delegate
                            {
                                MessageBox.Show("Connection to server is broken.", "Test Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                            }, null);
                        }
                        else
                        {
                            string resp = System.Text.Encoding.ASCII.GetString(bytes, 0, length);
                            resp = resp.Trim(new char[] { '\0' });
                            if (resp == Consts.TERM_TEST_CONN_OK)
                            {
                                Dispatcher.Invoke((ThreadStart)delegate
                                {
                                    MessageBox.Show("The server can be successfully tested.", "Test Passed", MessageBoxButton.OK, MessageBoxImage.Information);
                                }, null);
                            }
                            else
                            {
                                Dispatcher.Invoke((ThreadStart)delegate
                                {
                                    MessageBox.Show("The server cannot be successfully tested.", "Test Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                                }, null);
                            }
                        }
                        client.Shutdown(SocketShutdown.Both);
                        client.Disconnect(false);
                    }
                    client.Close();
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke((ThreadStart)delegate
                    {
                        MessageBox.Show(this, "Socket error : " + ex.Message, "Test Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }, null);
                    Helper.SafeCloseSocket(client);
                }
            }
            InRun = false;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (InRun == true)
                e.Cancel = true;

            base.OnClosing(e);
        }

        private void Window_Load(object sender, RoutedEventArgs e)
        {
            txtIPAddress.Focus();
        }
    }
}
