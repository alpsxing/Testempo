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

namespace ManagementSystem
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

        #region Const

        private const string THE_TITLE = "Terminal Mananagement System";

        #endregion

        #region Variables

        private bool _bInNormalClose = false;

        private object _logLock = new object();
        private object _tiLock = new object();
        private object _reqLock = new object();

        private CancellationTokenSource _cts = null;
        private Task _logTask = null;
        private int _logIndex = 1;
        private Queue<LogMessage> _logQueue = new Queue<LogMessage>();

        private Timer _timerPBar = null;

        private Socket _mainSocket = null;
        private Timer _timerPulse = null;

        #endregion

        #region Properties

        private int _statusPbarMax = 1;
        public int StatusPbarMax
        {
            get
            {
                return _statusPbarMax;
            }
            set
            {
                _statusPbarMax = value;
                NotifyPropertyChanged("StatusPbarMax");
            }
        }

        private int _statusPbarValue = 0;
        public int StatusPbarValue
        {
            get
            {
                return _statusPbarValue;
            }
            set
            {
                _statusPbarValue = value;
                NotifyPropertyChanged("StatusPbarValue");
            }
        }

        private ObservableCollection<DTUInfo> _dtuInfoOc = new ObservableCollection<DTUInfo>();
        public ObservableCollection<DTUInfo> DTUInfoOC
        {
            get
            {
                return _dtuInfoOc;
            }
        }

        private ObservableCollection<LogMessage> _logMsgOc = new ObservableCollection<LogMessage>();
        public ObservableCollection<LogMessage> LogMsgOc
        {
            get
            {
                return _logMsgOc;
            }
        }

        private ObservableCollection<LogMessage> _logMsgDispOc = new ObservableCollection<LogMessage>();
        public ObservableCollection<LogMessage> LogMsgDispOc
        {
            get
            {
                return _logMsgDispOc;
            }
        }

        private ObservableCollection<TerminalInformation> _termInfoOc = new ObservableCollection<TerminalInformation>();
        public ObservableCollection<TerminalInformation> TermInfoOc
        {
            get
            {
                return _termInfoOc;
            }
        }

        public ObservableCollection<string> TermIPAddressOc
        {
            get
            {
                ObservableCollection<string> oc = new ObservableCollection<string>();
                foreach (TerminalInformation tii in TermInfoOc)
                {
                    oc.Add(tii.TerminalIPString);
                }
                return oc;
            }
        }

        private int _maxLogDispCount = Consts.MAX_LOG_DISPLAY_COUNT;
        public int MaxLogDisplayCount
        {
            get
            {
                return _maxLogDispCount;
            }
            set
            {
                _maxLogDispCount = value;
                if (_maxLogDispCount < Consts.MIN_MAX_LOG_DISPLAY_COUNT)
                    _maxLogDispCount = value;
                NotifyPropertyChanged("MaxLogDisplayCount");
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
                _maxLogCount = value;
                if (_maxLogCount < Consts.MIN_MAX_LOG_COUNT)
                    _maxLogCount = value;
                NotifyPropertyChanged("MaxLogCount");
            }
        }

        private string _serverIp = "";
        public string ServerIP
        {
            get
            {
                return _serverIp;
            }
            set
            {
                _serverIp = value;
                Title = THE_TITLE + " - " + ServerIP + ":" + ServerPort.ToString() + " ( " + ServerTimeout.ToString() + "ms / " + RemoteTimeout.ToString() + "ms )";
                NotifyPropertyChanged("ServerIP");
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
                _serverPort = value;
                Title = THE_TITLE + " - " + ServerIP + ":" + ServerPort.ToString() + " ( " + ServerTimeout.ToString() + "ms / " + RemoteTimeout.ToString() + "ms )";
                NotifyPropertyChanged("ServerPort");
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
                _serverTimeout = value;
                Title = THE_TITLE + " - " + ServerIP + ":" + ServerPort.ToString() + " ( " + ServerTimeout.ToString() + "ms / " + RemoteTimeout.ToString() + "ms )";
                NotifyPropertyChanged("ServerTimeout");
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
                _remoteTimeout = value;
                Title = THE_TITLE + " - " + ServerIP + ":" + ServerPort.ToString() + " ( " + ServerTimeout.ToString() + "ms / " + RemoteTimeout.ToString() + "ms )";
                NotifyPropertyChanged("RemoteTimeout");
            }
        }

        private string _readyString = "Ready";
        public string ReadyString
        {
            get
            {
                return _readyString;
            }
            set
            {
                _readyString = value;
                NotifyPropertyChanged("ReadyString");
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

        private bool? _logAutoScrolling = true;
        public bool? LogAutoScrolling
        {
            get
            {
                return _logAutoScrolling;
            }
            set
            {
                _logAutoScrolling = value;
                NotifyPropertyChanged("LogAutoScrolling");
            }
        }

        private string _userName = "";
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

        #endregion

        public MainWindow(Socket soc, string servIp, int servPort, string userName)
		{
			InitializeComponent();

            DataContext = this;

            _mainSocket = soc;
            UserName = userName;
            ServerIP = servIp;
            ServerPort = servPort;

            dgLog.DataContext = LogMsgDispOc;
            LogMsgDispOc.CollectionChanged += new NotifyCollectionChangedEventHandler(LogMsgDispOc_CollectionChanged);

            string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (Directory.Exists(folder + @"\COMWAY") == false)
                Directory.CreateDirectory(folder + @"\COMWAY");
            folder = folder + @"\COMWAY";
            if (Directory.Exists(folder + @"\DTUManagement") == false)
                Directory.CreateDirectory(folder + @"\DTUManagement");
            if (Directory.Exists(folder + @"\DTUManagement\config") == false)
                Directory.CreateDirectory(folder + @"\DTUManagement\config");
            if (Directory.Exists(folder + @"\DTUManagement\log") == false)
                Directory.CreateDirectory(folder + @"\DTUManagement\log");

            _cts = new CancellationTokenSource();
            _logTask = Task.Factory.StartNew(
                () =>
                {
                    DisplayLog();
                }, _cts.Token
            );
        }

        #region Window Exit

        private void Window_Exit(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure to quit \"Management System\"?", "Comfirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _bInNormalClose = true;

            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_bInNormalClose == false)
            {
                if (MessageBox.Show("Are you sure to quit \"Management System\"?", "Comfirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    e.Cancel = true;
            }

            if (e.Cancel == false)
            {
                Helper.DoSendReceive(_mainSocket, Consts.TERM_LOGOUT + UserName, false);
                try
                {
                    TerminateAllTerminals();
                }
                catch (Exception) { }
                try
                {
                    SaveConfig();
                }
                catch (Exception) { }
                try
                {
                    SaveLog();
                }
                catch (Exception) { }
            }

            base.OnClosing(e);

            System.Environment.Exit(0);
        }

        #endregion

        #region Event Handlers

        private void LogMsgDispOc_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (LogAutoScrolling == false)
                return;

            //lock (_logLock) // Lock will lead to dead lock, why?
            {
                Dispatcher.Invoke((ThreadStart)delegate
                {
                    if (LogMsgDispOc.Count < 1)
                        return;
                    var border = VisualTreeHelper.GetChild(dgLog, 0) as Decorator;
                    if (border != null)
                    {
                        var scroll = border.Child as ScrollViewer;
                        if (scroll != null) scroll.ScrollToEnd();
                    }
                }, null);
            }
        }

        private void ServerConfig_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            ServerConfiguration sc = new ServerConfiguration(ServerIP, ServerPort, ServerTimeout, RemoteTimeout);
            if (sc.ShowDialog() != true)
                return;

            ServerIP = sc.ServerIPAddress;
            ServerPort = sc.ServerPort;
            ServerTimeout = sc.ServerTimeout;
            RemoteTimeout = sc.RemoteTimeout;
            Title = THE_TITLE + " - " + ServerIP + ":" + ServerPort.ToString() + " ( " + ServerTimeout.ToString() + "ms / " + RemoteTimeout.ToString() + "ms )";

            SaveConfig();
        }

        private void LocalConfig_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            LocalConfiguration lc = new LocalConfiguration(MaxLogCount, MaxLogDisplayCount);
            if (lc.ShowDialog() != true)
                return;

            MaxLogCount = lc.MaxLogCount;
            MaxLogDisplayCount = lc.MaxLogDisplayCount;
        }

        private void AddDTU_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Socket soc = null;
            try
            {
                soc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress local = IPAddress.Parse(ServerIP);
                IPEndPoint iep = new IPEndPoint(local, ServerPort);
                soc.SendTimeout = Consts.TERM_TIMEOUT;
                soc.ReceiveTimeout = Consts.TERM_TIMEOUT;
                soc.Connect(iep);
                if (soc.Connected)
                {
                    byte[] bytes = Helper.DoSendReceive(soc, Consts.TERM_GET_ALL_DTU);
                    if (bytes.Length < 1)
                    {
                        MessageBox.Show("Connection to server is broken.", "Add DTU Fails.", MessageBoxButton.OK, MessageBoxImage.Error);
                        Helper.SafeCloseSocket(soc);
                    }
                    else
                    {
                        Tuple<string, byte[], string, string> resp = Helper.ExtractSocketResponse(bytes, bytes.Length);
                        if (resp.Item1 != Consts.TERM_GET_ALL_DTU_OK)
                        {
                            MessageBox.Show("Cannot get all DTU : " + resp.Item3, "Add DTU Fails.", MessageBoxButton.OK, MessageBoxImage.Error);
                            Helper.SafeCloseSocket(soc);
                        }
                        else
                        {
                            Helper.FillDTUInfoOC(DTUInfoOC, resp.Item3);
                            SelectDTU sdtu = new SelectDTU(soc, DTUInfoOC);
                            //sdtu.DoSocketSendReceive += new SelectDTU.DoSocketSendReceiveDelegate(Helper.DoSendReceive);
                            bool? b = sdtu.ShowDialog();
                            //sdtu.DoSocketSendReceive -= new SelectDTU.DoSocketSendReceiveDelegate(Helper.DoSendReceive);
                            if (b == true)
                            {
                                bool dupDtu = false;
                                foreach (TerminalInformation tii in TermInfoOc)
                                {
                                    if (string.Compare(tii.CurrentDTU.DtuId, _dtuInfoOc[sdtu.DTUSelectedIndex].DtuId, true) == 0)
                                    {
                                        MessageBox.Show("DTU (" + _dtuInfoOc[sdtu.DTUSelectedIndex].DtuId + ") has already been in control.", "Add DTU Fails.", MessageBoxButton.OK, MessageBoxImage.Error);
                                        Helper.SafeCloseSocket(soc);
                                        dupDtu = true;
                                        break;
                                    }
                                }

                                if (dupDtu == false)
                                {
                                    _timerPulse.Change(Timeout.Infinite, Consts.TERM_TASK_TIMER_PULSE);
                                    bytes = Helper.DoSendReceive(soc, Consts.TERM_ADD_DTU + UserName + "\t" + _dtuInfoOc[sdtu.DTUSelectedIndex].DtuId);
                                    _timerPulse.Change(Consts.TERM_TASK_TIMER_PULSE, Consts.TERM_TASK_TIMER_PULSE);
                                    resp = Helper.ExtractSocketResponse(bytes, bytes.Length);
                                    if (resp.Item1 != Consts.TERM_ADD_DTU_OK)
                                    {
                                        MessageBox.Show("Cannot add DTU (" + _dtuInfoOc[sdtu.DTUSelectedIndex].DtuId + "): " + resp.Item3, "Add DTU Fails.", MessageBoxButton.OK, MessageBoxImage.Error);
                                        Helper.SafeCloseSocket(soc);
                                    }
                                    else
                                    {
                                        soc.SendTimeout = -1;
                                        soc.ReceiveTimeout = -1;

                                        IPAddress ipad = ((IPEndPoint)soc.RemoteEndPoint).Address;
                                        TerminalInformation ti = null;
                                        Dispatcher.Invoke((ThreadStart)delegate()
                                        {
                                            ti = new TerminalInformation()
                                            {
                                                ServerIP = IPAddress.Parse(ServerIP),
                                                ServerIPString = ServerIP,
                                                TerminalIP = ipad,
                                                CurrentDTU = DTUInfoOC[sdtu.DTUSelectedIndex],
                                                TerminalSocket = soc,
                                                State = TerminalInformation.TiState.Connected,
                                            };
                                            ti.InitUI();
                                            lock (_tiLock)
                                            {
                                                TermInfoOc.Add(ti);
                                            }
                                            UpdateTerminalInforView(ti);
                                            tcTerminal.SelectedIndex = tcTerminal.Items.Count;
                                        }, null);
                                    }
                                }
                            }
                            else
                            {
                                Helper.SafeCloseSocket(soc);
                            }
                        }
                    }
                }   
                else
                {
                    Helper.SafeCloseSocket(soc);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Socket error : " + ex.Message, "Test Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                Helper.SafeCloseSocket(soc);
            }
        }

        private void RefreshDTU_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not defined yet.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void DeleteDTU_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (tvTerminal.Items.Count < 1)
            {
                MessageBox.Show("No DTU can be removed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (tvTerminal.SelectedItem == null)
            {
                MessageBox.Show("No selected DTU will be removed.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TreeViewItem tvi = tvTerminal.SelectedItem as TreeViewItem;
            if (tvi.Parent is TreeViewItem)
                tvi = (TreeViewItem)tvi.Parent;
            TerminalInformation ti = null;
            lock (_tiLock)
            {
                ti = Helper.FindTermInfo(tvi, TermInfoOc);
            }
            if (MessageBox.Show("Are you sure to remove DTU (" + ti.CurrentDTU.DtuId + ")?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _timerPulse.Change(Timeout.Infinite, Consts.TERM_TASK_TIMER_PULSE);
            byte[] bytes = Helper.DoSendReceive(_mainSocket, Consts.MAN_UNCTRL_DTU + UserName + "\t" + ti.CurrentDTU.DtuId);
            _timerPulse.Change(Consts.TERM_TASK_TIMER_PULSE, Consts.TERM_TASK_TIMER_PULSE);
            Tuple<string, byte[], string, string> resp = Helper.ExtractSocketResponse(bytes, bytes.Length);
            if (resp.Item1 != Consts.MAN_UNCTRL_DTU_OK)
                MessageBox.Show("Error when deleting DTU (" + ti.CurrentDTU.DtuId + ") : " + resp.Item3, "Delete DTU error.", MessageBoxButton.OK, MessageBoxImage.Error);

            TermInfoOc.Remove(ti);
            tvTerminal.Items.Remove(ti.CurrentTvItem);
            Helper.SafeCloseSocket(ti.TerminalSocket);
            ti.TerminalSocket = null;
            tcTerminal.Items.Remove(ti.CurrentTabItem);
        }

        private void About_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            About ab = new About("Management System", "Copyright @ 2012");
            ab.ShowDialog();
        }

        private void Window_Load(object sender, RoutedEventArgs e)
        {
            if (EncryptDecryptLibrary.EncryptDecryptLibrary.CheckRunOrNot() == false)
            {
                MessageBox.Show("No valid license.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Environment.Exit(0);
            }

            LoadConfig();

            InitUserName();

            _timerPBar = new Timer(new TimerCallback(PBarTimerCallBackHandler), null, Timeout.Infinite, 1000);
            _timerPulse = new Timer(new TimerCallback(PulseTimerCallBackHandler), null, Consts.TERM_TASK_TIMER_PULSE, Consts.TERM_TASK_TIMER_PULSE);
        }

        private void InitUserName()
        {
            bool initOK = true;
            try
            {
                byte[] bytes = Helper.DoSendReceive(_mainSocket, Consts.TERM_INIT_USER + UserName);
                if (bytes.Length < 1)
                {
                    MessageBox.Show("Connection to server is broken.", "Init User Name Fails.", MessageBoxButton.OK, MessageBoxImage.Error);
                    TerminateAllTerminals();
                    initOK = false;
                }
                else
                {
                    Tuple<string, byte[], string, string> resp = Helper.ExtractSocketResponse(bytes, bytes.Length);
                    if (resp.Item1 != Consts.TERM_INIT_USER_OK)
                    {
                        MessageBox.Show("Error response : " + resp.Item1 + resp.Item3, "Init User Name Fails.", MessageBoxButton.OK, MessageBoxImage.Error);
                        TerminateAllTerminals();
                        initOK = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot get initializing user name response : " + ex.Message, "Init User Name Fails.", MessageBoxButton.OK, MessageBoxImage.Error);
                TerminateAllTerminals();
                initOK = false;
            }

            if (initOK == false)
            {
                MessageBox.Show("DTU management system exits.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Environment.Exit(1);
            }
        }

        private void PBarTimerCallBackHandler(object obj)
        {
            if ((StatusPbarValue + 1 * 1000) < StatusPbarMax)
                StatusPbarValue = StatusPbarValue + 1 * 1000;
        }

        private void PulseTimerCallBackHandler(object obj)
        {
            try
            {
                byte[] bytes = Helper.DoSendReceive(_mainSocket, Consts.TERM_PULSE_REQ);
                if (bytes.Length < 1)
                {
                    AddLog("Connection to server is broken.", "", LogMessage.State.Error, LogMessage.Flow.None);
                    TerminateAllTerminals();
                }
                else
                {
                    Tuple<string, byte[], string, string> resp = Helper.ExtractSocketResponse(bytes, bytes.Length);
                    if (resp.Item1 != Consts.TERM_PULSE_REQ_OK)
                    {
                        AddLog("Pulse fails : " + resp.Item3, "", LogMessage.State.Error, LogMessage.Flow.Response);
                        TerminateAllTerminals();
                    }
                    else
                        AddLog("Pulse OK.", "", LogMessage.State.Infomation, LogMessage.Flow.Response);
                }
            }
            catch (Exception ex)
            {
                AddLog("Pulse exception : " + ex.Message, "", LogMessage.State.Error, LogMessage.Flow.None);
                TerminateAllTerminals();
            }
        }

        private void TerminateAllTerminals()
        {
            _timerPulse.Change(Timeout.Infinite, Consts.TERM_TASK_TIMER_PULSE);
            lock (_tiLock)
            {
                foreach (TerminalInformation tii in TermInfoOc)
                {
                    tii.CTS.Cancel();
                    tii.State = TerminalInformation.TiState.Disconnected;
                    tii.CurrentDTU = null;
                    Helper.SafeCloseSocket(tii.TerminalSocket);
                    tii.TerminalSocket = null;
                }
            }
            _cts.Cancel();
            Helper.SafeCloseSocket(_mainSocket);
        }

        private void SaveLog_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveLog();
        }

        private void ClearLog_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (LogMsgOc.Count > 0 && MessageBox.Show("Do you want to save the log first?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                SaveLog();
            else
                SaveLog(false);                
        }

        #endregion

        #region Methods

        private void TerminateDTUInfo(TerminalInformation ti)
        {
            try
            {
                ti.TerminalSocket.Shutdown(SocketShutdown.Both);
                ti.TerminalSocket.Disconnect(false);
                ti.TerminalSocket.Close();
                ti.TerminalSocket.Dispose();
            }
            catch (Exception)// ex)
            {
                //AddLog("Disconnect server error : " + ex.Message, ip, state: LogMessage.State.Error);
                Helper.SafeCloseSocket(ti.TerminalSocket);
            }
            if (ti.CurrentTvItem != null)
            {
                Dispatcher.Invoke((ThreadStart)delegate
                {
                    ti.State = TerminalInformation.TiState.Unknown;
                    foreach (TabItem tii in tcTerminal.Items)
                    {
                        StackPanel sp = tii.Header as StackPanel;
                        if (sp == null)
                            continue;
                        Image img = sp.Children[0] as Image;
                        Label lbl = sp.Children[1] as Label;
                        string s = lbl.Content as string;
                        if (s == null)
                            continue;
                        if (s == ti.TerminalIPString)
                        {
                            img.Source = new BitmapImage(new Uri("pack://application:,,,/managementsystem;component/resources/dtuunknown.ico"));
                            lbl.Foreground = Brushes.Red;
                            sp.ToolTip = ti.TerminalIPString + " : Unknown";
                        }
                    }

                }, null);
            }
        }

        private void ProcessServerResponse(TerminalInformation ti, byte[] respBytes, int length)
        {
            Tuple<string, byte[], string, string> resp = Helper.ExtractSocketResponse(respBytes, length);

            switch (resp.Item1)
            {
                default:
                    AddLog("Unknown response : " + resp, ti.CurrentDTU.DtuId, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                //case Consts.TEST_CONNECTION_RESP:
                //    break;
                case Consts.TERM_INVALID_REQUEST:
                    AddLog("Invalid request : " + resp, ti.CurrentDTU.DtuId, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.TERM_ADD_DTU_OK:
                    AddLog("Add dtu ok : " + resp, ti.CurrentDTU.DtuId, state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    Dispatcher.Invoke((ThreadStart)delegate
                    {
                        ti.State = TerminalInformation.TiState.Connected;
                        UpdateTerminalInforView(ti);
                    }, null);
                    //Dispatcher.Invoke((ThreadStart)delegate
                    //{
                    //    //TerminalInformationUC tiuc = new TerminalInformationUC(ti);
                    //    //TabItem tabi = new TabItem();
                    //    //StackPanel sp = new StackPanel();
                    //    //sp.Orientation = Orientation.Horizontal;
                    //    //sp.Margin = new Thickness(0, 0, 0, 0);//-4, -4, -4, -4);
                    //    //Label lbl = new Label();
                    //    //lbl.Content = ti.CurrentDTU.DtuId;
                    //    //sp.Children.Add(lbl);
                    //    //Image img = new Image();
                    //    //switch (ti.State)
                    //    //{
                    //    //    default:
                    //    //    case TerminalInformation.TiState.Unknown:
                    //    //        img.Source = new BitmapImage(new Uri("pack://application:,,,/managementsystem;component/resources/dtuunknown.ico"));
                    //    //        lbl.Foreground = Brushes.Red;
                    //    //        sp.ToolTip = ti.CurrentDTU.DtuId + " : Unknown";
                    //    //        break;
                    //    //    case TerminalInformation.TiState.Connected:
                    //    //        img.Source = new BitmapImage(new Uri("pack://application:,,,/managementsystem;component/resources/dtuok.ico"));
                    //    //        lbl.Foreground = Brushes.Black;
                    //    //        sp.ToolTip = ti.CurrentDTU.DtuId + " : Connected";
                    //    //        break;
                    //    //    case TerminalInformation.TiState.Disconnected:
                    //    //        img.Source = new BitmapImage(new Uri("pack://application:,,,/managementsystem;component/resources/dtuerror.ico"));
                    //    //        lbl.Foreground = Brushes.Red;
                    //    //        sp.ToolTip = ti.CurrentDTU.DtuId + " : Disconnected";
                    //    //        break;
                    //    //}
                    //    //img.Width = 16;
                    //    //img.Height = 16;
                    //    ////img.Opacity = 0.75;
                    //    //sp.Children.Insert(0, img);
                    //    //tabi.Header = sp;
                    //    //tabi.Content = tiuc;
                    //    //tcTerminal.Items.Add(tabi);
                    //    //tcTerminal.SelectedIndex = tcTerminal.Items.Count - 1;
                    //}, null);
                    break;
                case Consts.TERM_ADD_DTU_ERR:
                    AddLog("Add dtu error : " + resp, ti.CurrentDTU.DtuId, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    Dispatcher.Invoke((ThreadStart)delegate()
                    {
                        TermInfoOc.Remove(ti);
                        UpdateTerminalInforView(ti, false);
                        Helper.SafeCloseSocket(ti.TerminalSocket);
                        ti.TerminalSocket = null;
                    }, null);
                    break;
                case Consts.TERM_PULSE_REQ_OK:
                    AddLog("Pulse ok : " + resp, ti.CurrentDTU.DtuId, state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    break;
            }
        }

        private void OnSocketStateChanged(object sender, TerminalInformationEventArgs args)
        {
        }

        private TerminalInformation FindTIbyDTUID(string dtuID)
        {
            if(string.IsNullOrWhiteSpace(dtuID))
                return null;
            dtuID = dtuID.Trim();
            foreach (TerminalInformation tii in TermInfoOc)
            {
                if (string.Compare(dtuID, tii.CurrentDTU.DtuId.Trim(), true) == 0)
                    return tii;
            }
            return null;
        }

        private void UpdateTerminalInforView(TerminalInformation ti, bool isAdd = true)
        {
            if (ti == null)
                return;

            if (isAdd == true)
            {
                if(ti.CurrentTvItem != null && tvTerminal.Items.Contains(ti.CurrentTvItem) == false)
                    tvTerminal.Items.Add(ti.CurrentTvItem);
                if (ti.CurrentTabItem != null && tcTerminal.Items.Contains(ti.CurrentTabItem) == false)
                {
                    tcTerminal.Items.Add(ti.CurrentTabItem);
                    tcTerminal.SelectedIndex = tcTerminal.Items.Count - 1;
                }
            }
            else
            {
                if (ti.CurrentTvItem != null && tvTerminal.Items.Contains(ti.CurrentTvItem) == true)
                    tvTerminal.Items.Remove(ti.CurrentTvItem);
                if (ti.CurrentTabItem != null && tcTerminal.Items.Contains(ti.CurrentTabItem) == true)
                    tcTerminal.Items.Add(ti.CurrentTabItem);
            }
        }

        //private void ConnectServer()
        //{
        //}

        //private void DisconnectServer()
        //{
        //}

        private void AddLog(string msg = "", string ip = "", LogMessage.State state = LogMessage.State.None, LogMessage.Flow flow = LogMessage.Flow.None)
        {
            Dispatcher.Invoke((ThreadStart)delegate
            {
                if (_cts.Token.IsCancellationRequested == false)//lock (_logLock)
                {
                    _logQueue.Enqueue(new LogMessage()
                    {
                        Index = _logIndex++,
                        TimeStamp = DateTime.Now,
                        FlowType = flow,
                        StateType = state,
                        IPAddr = ip,
                        Message = msg
                    });
                }
            }, null);
        }

        private void DisplayLog()
        {
            while (_cts.Token.IsCancellationRequested == false)
            {
                lock (_logLock)
                {
                    if (_logQueue.Count > 0)
                    {
                        Dispatcher.Invoke((ThreadStart)delegate
                        {
                            LogMessage lm = _logQueue.Dequeue();

                            while (LogMsgDispOc.Count > MaxLogDisplayCount)
                                LogMsgDispOc.RemoveAt(0);

                            LogMsgDispOc.Add(lm);

                            if (MaxLogCount > 0 && LogMsgOc.Count > MaxLogCount)
                                SaveLog();

                            LogMsgOc.Add(lm);
                        }, null);
                    }
                }

                Thread.Sleep(Consts.TASK_THREAD_SLEEP_INTERVAL);
            }
        }

        /// <summary>
        /// Auto Clear
        /// </summary>
        private void SaveLog(bool doSave = true)
        {
            lock (_logLock)
            {
                if (doSave == true)
                {
                    try
                    {
                        DateTime dt = DateTime.Now;
                        string sdt = dt.Year.ToString() + "_" + dt.Month.ToString() + "_" + dt.Day.ToString()
                            + "." + dt.Hour.ToString() + "_" + dt.Minute.ToString() + "_" + dt.Second.ToString()
                             + "." + dt.Millisecond.ToString();
                        string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        StreamWriter sw = new StreamWriter(folder + @"\COMWAY\DTUManagement\log\" + sdt + ".cfg");
                        StringBuilder sb = new StringBuilder();
                        foreach(LogMessage lm in LogMsgOc)
                        {
                            sb.Append(lm.IndexString
                                + "\t" + lm.MsgDateTime
                                + "\t" + lm.FlowType.ToString()
                                + "\t" + lm.StateType.ToString()
                                + "\t" + (string.IsNullOrWhiteSpace(lm.IPAddr)? "(NA)" : lm.IPAddr)
                                + "\t" + lm.Message + "\n");
                        }
                        sw.Write(sb.ToString());
                        sw.Flush();
                        sw.Close();
                        sw.Dispose();

                        LogMsgOc.Clear();
                        //LogMsgDispOc.Clear();
                    }
                    catch (Exception ex)
                    {
                        LogMsgOc.Clear();
                        //LogMsgDispOc.Clear();

                        AddLog("Exception when saving log : " + ex.Message, "", state: LogMessage.State.Error);
                    }
                }
                else
                {
                    LogMsgOc.Clear();
                    LogMsgDispOc.Clear();
                }
            }
        }

        private void LoadConfig()
        {
            try
            {
                string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (File.Exists(folder + @"\COMWAY\DTUManagement\config\mansys.cfg") == false)
                {
                    //AddLog("No predefined configuration.");
                    return;
                }

                StreamReader sr = new StreamReader(folder + @"\COMWAY\DTUManagement\config\mansys.cfg");
                string strLine = null;
                int i = 0;
                while (true)
                {
                    strLine = sr.ReadLine();
                    if (string.IsNullOrWhiteSpace(strLine))
                        break;
                    if (i == 0)
                    {
                        int iv = Consts.TERM_TIMEOUT;
                        if (int.TryParse(EncryptDecrypt.Decrypt(strLine.Trim()), out iv) == false)
                            iv = Consts.TERM_TIMEOUT;
                        ServerTimeout = iv;
                    }
                    else if (i == 1)
                    {
                        int iv = Consts.DTU_TIMEOUT;
                        if (int.TryParse(EncryptDecrypt.Decrypt(strLine.Trim()), out iv) == false)
                            iv = Consts.DTU_TIMEOUT;
                        RemoteTimeout = iv;
                    }
                    else if (i == 2)
                    {
                        int iv = Consts.MAX_LOG_COUNT;
                        if (int.TryParse(EncryptDecrypt.Decrypt(strLine.Trim()), out iv) == false)
                            iv = Consts.MAX_LOG_COUNT;
                        else if (iv < Consts.MIN_MAX_LOG_COUNT)
                            iv = Consts.MIN_MAX_LOG_COUNT;
                        MaxLogCount = iv;
                    }
                    else if (i == 3)
                    {
                        int iv = Consts.MAX_LOG_DISPLAY_COUNT;
                        if (int.TryParse(EncryptDecrypt.Decrypt(strLine.Trim()), out iv) == false)
                            iv = Consts.MAX_LOG_DISPLAY_COUNT;
                        else if (iv < Consts.MIN_MAX_LOG_DISPLAY_COUNT)
                            iv = Consts.MIN_MAX_LOG_DISPLAY_COUNT;
                        MaxLogDisplayCount = iv;
                    }
                    else
                        break;

                    i++;
                }
                sr.Close();
                sr.Dispose();
            }
            catch (Exception)// ex)
            {
                //AddLog("Cannot load configuration : " + ex.Message, state: LogMessage.State.Fail);
            }
        }

        private void SaveConfig()
        {
            try
            {
                string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                StreamWriter sw = new StreamWriter(folder + @"\COMWAY\DTUManagement\config\mansys.cfg");
                sw.WriteLine(EncryptDecrypt.Encrypt(ServerTimeout.ToString()));
                sw.WriteLine(EncryptDecrypt.Encrypt(RemoteTimeout.ToString()));
                sw.WriteLine(EncryptDecrypt.Encrypt(MaxLogCount.ToString()));
                sw.WriteLine(EncryptDecrypt.Encrypt(MaxLogDisplayCount.ToString()));
                sw.Close();
                sw.Dispose();
            }
            catch (Exception)// ex)
            {
                //AddLog("Cannot save configuration : " + ex.Message, state: LogMessage.State.Fail);
            }
        }

        public static string GetValidDatabaseName(string src)
        {
            if (string.IsNullOrWhiteSpace(src))
                return "";
            src = src.Trim().ToLower();
            string dest = "";
            while (src.Length > 0)
            {
                string c = src.Substring(0, 1);
                src = src.Substring(1);
                if (Regex.Match(c, "[a-zA-Z0-9]") == Match.Empty)
                    continue;
                dest = dest + c;
            }
            return dest;
        }

        private bool CheckServerIPValid(string src)
        {
            IPAddress ipad = null;
            if (IPAddress.TryParse(src, out ipad) == false)
                return false;
            return true;
        }

        #endregion

        private void Reconnect_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            IPAddress server = null;
            IPEndPoint iep = null;
            try
            {
                _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server = IPAddress.Parse(ServerIP);
                iep = new IPEndPoint(server, ServerPort);
                _mainSocket.SendTimeout = Consts.TERM_TIMEOUT;
                _mainSocket.ReceiveTimeout = Consts.TERM_TIMEOUT;
                _mainSocket.Connect(iep);
                if (_mainSocket.Connected)
                {
                    Task.Factory.StartNew(
                        () =>
                        {
                            //ManTask(true);
                        }, _cts.Token);
                }
                else
                {
                    Helper.SafeCloseSocket(_mainSocket);
                    _mainSocket = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot reconnect server : " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Helper.SafeCloseSocket(_mainSocket);
                _mainSocket = null;
            }
        }
    }
}
