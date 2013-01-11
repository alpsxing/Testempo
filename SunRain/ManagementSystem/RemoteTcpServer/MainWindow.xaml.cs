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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Specialized;
using System.Threading.Tasks;

using InformationTransferLibrary;

namespace RemoteTcpServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		public void NotifyPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

        private ObservableCollection<SentReceivedItem> _sentReceivedOc = new ObservableCollection<SentReceivedItem>();
        public ObservableCollection<SentReceivedItem> SentReceivedOc
        {
            get
            {
                return _sentReceivedOc;
            }
        }

        private int _serverPort = Consts.DTU_PORT;
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
                int i = Consts.DTU_PORT;
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

        private bool _serverPortOK = true;
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
                NotifyPropertyChanged("StartOK");
            }
        }

        private Brush _serverPortFG = Brushes.Black;
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
                NotifyPropertyChanged("StartOK");
            }
        }

        public bool NotInRun
        {
            get
            {
                return !_inRun;
            }
        }

        public bool StartOK
        {
            get
            {
                return NotInRun && ServerPortOK;
            }
        }

        private bool? _doSendOrNot = false;
        public bool? DoSendOrNot
        {
            get
            {
                return _doSendOrNot;
            }
            set
            {
                _doSendOrNot = value;
                DoSendOrNotString = (_doSendOrNot == true) ? "Send Response" : "No Response";
                NotifyPropertyChanged("DoSendOrNot");
            }
        }

        private string _doSendOrNotString = "No Response";
        public string DoSendOrNotString
        {
            get
            {
                return _doSendOrNotString;
            }
            set
            {
                _doSendOrNotString = value;
                NotifyPropertyChanged("DoSendOrNotString");
            }
        }

        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _mainTask = null;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            dgSentReceived.DataContext = SentReceivedOc;
            SentReceivedOc.CollectionChanged += new NotifyCollectionChangedEventHandler(SentReceivedOc_CollectionChanged);
        }

        private void SentReceivedOc_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //if (_dataAutoScrolling == false)
            //    return;

            //lock (ObjLock)
            {
                if (SentReceivedOc.Count < 1)
                    return;
                var border = VisualTreeHelper.GetChild(dgSentReceived, 0) as Decorator;
                if (border != null)
                {
                    var scroll = border.Child as ScrollViewer;
                    if (scroll != null) scroll.ScrollToEnd();
                }
            }
        }

        public void PostLog(string msg)
        {
            PostLog(msg, Brushes.Black);
        }

        public void PostLog(string msg, SolidColorBrush scb)
        {
            Dispatcher.Invoke((ThreadStart)delegate()
            {
                if (fldocLog.Blocks.Count > 100)
                    fldocLog.Blocks.Remove(fldocLog.Blocks.FirstBlock);
                Run rch = new Run(msg);
                Paragraph pch = new Paragraph(rch);
                pch.Foreground = scb;
                fldocLog.Blocks.Add(pch);
                rtxtLog.ScrollToEnd();
            }, null);
        }

        private void ClearQueue_ButtonClick(object sender, RoutedEventArgs e)
        {
            SentReceivedOc.Clear();
        }

        private void ClearLog_ButtonClick(object sender, RoutedEventArgs e)
        {
            fldocLog.Blocks.Clear();
        }

        private void Start_ButtonClick(object sender, RoutedEventArgs e)
        {
            InRun = true;
            _mainTask = new Task(
                () =>
                {
                    StartServerTask();
                }, _cts.Token);
            _mainTask.Start();
        }

        private void StartServerTask()
        {
            IPAddress local = IPAddress.Any;
            IPEndPoint iep = new IPEndPoint(local, ServerPort);
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(iep);
            PostLog("Start listening...");
            server.Listen(100);
            PostLog("Listen started.");
            while (true)
            {
                try
                {
                    PostLog("Start accepting...");
                    Socket oneServer = server.Accept();
                    PostLog("Accepted.");
                    Task.Factory.StartNew(
                        () =>
                        {
                            SingleServerService(oneServer);
                        }, _cts.Token
                    );
                }
                catch (Exception ex)
                {
                    PostLog("Accepting exception : " + ex.Message, Brushes.Red);
                    break;
                }
            }
            InRun = false;
        }

        public void SingleServerService(Socket server)
        {
            string ip = ((IPEndPoint)server.RemoteEndPoint).Address.ToString();
            byte[] bytes = new byte[1024 * 16];
            int len = 0;
            try
            {
                while (true)
                {
                    len = server.Receive(bytes);
                    if (len < 1)
                    {
                        PostLog("Connection to DTU (" + ip + ") is broken.", Brushes.Red);
                        break;
                    }
                    else
                    {
                        SentReceivedItem sri = null;
                        Dispatcher.Invoke((ThreadStart)delegate()
                        {
                            sri = new SentReceivedItem();
                        }, null);
                        byte[] ba = new byte[len];
                        for (int i = 0; i < len; i++)
                        {
                            ba[i] = bytes[i];
                        }

                        string rbrs = "";
                        string rbs = "";
                        Dispatcher.Invoke((ThreadStart)delegate()
                        {
                            sri.Index = (SentReceivedOc.Count + 1).ToString();
                            sri.ReceivedBytes = ba;
                            rbrs = sri.ReceivedBytesRawString;
                            rbs = sri.ReceivedBytesString;
                        }, null);

                        PostLog("Received Raw : " + rbrs);
                        PostLog("Received : " + rbs);

                        if (DoSendOrNot == true)
                        {
                            bytes = Encoding.UTF8.GetBytes("Response : " + rbs);
                            PostLog("Trying sending response : Response " + rbs);
                            server.Send(bytes);
                            PostLog("Sent.");
                            Dispatcher.Invoke((ThreadStart)delegate()
                            {
                                sri.Sent = "Response : " + rbs;
                            }, null);
                        }

                        Dispatcher.Invoke((ThreadStart)delegate()
                        {
                            SentReceivedOc.Add(sri);
                        }, null);
                    }
                }
            }
            catch (Exception ex)
            {
                PostLog(ex.Message);
            }
            PostLog("Finalize connection to (" + ip + ")...");
            Helper.SafeCloseSocket(server);
            PostLog("Done.");
        }
    }
}
