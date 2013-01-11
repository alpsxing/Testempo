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

        private const string THE_TITLE = "DTU管理系统";

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
        private Timer _timerDTU = null;

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
                Title = THE_TITLE + " - " + ServerIP + ":" + ServerPort.ToString();// +" ( " + ServerTimeout.ToString() + "ms / " + RemoteTimeout.ToString() + "ms )";
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
                Title = THE_TITLE + " - " + ServerIP + ":" + ServerPort.ToString();// +" ( " + ServerTimeout.ToString() + "ms / " + RemoteTimeout.ToString() + "ms )";
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
                Title = THE_TITLE + " - " + ServerIP + ":" + ServerPort.ToString();// +" ( " + ServerTimeout.ToString() + "ms / " + RemoteTimeout.ToString() + "ms )";
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
                Title = THE_TITLE + " - " + ServerIP + ":" + ServerPort.ToString();// +" ( " + ServerTimeout.ToString() + "ms / " + RemoteTimeout.ToString() + "ms )";
                NotifyPropertyChanged("RemoteTimeout");
            }
        }

        private string _readyString = "已连接";
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

        private bool _connected = true;
        public bool Connected
        {
            get
            {
                return _connected;
            }
            set
            {
                _connected = value;
                if (_connected == true)
                    ReadyString = "已连接";
                else
                    ReadyString = "失去链接";
                NotifyPropertyChanged("Connected");
                NotifyPropertyChanged("NotConnected");
            }
        }

        public bool NotConnected
        {
            get
            {
                return !_connected;
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
                NotifyPropertyChanged("Password");
            }
        }

        private bool _view1DTUEnabled = false;
        public bool View1DTUEnabled
        {
            get
            {
                return _view1DTUEnabled;
            }
            set
            {
                _view1DTUEnabled = value;
                NotifyPropertyChanged("View1DTUEnabled");
            }
        }

        private bool _view2DTUsEnabled = false;
        public bool View2DTUsEnabled
        {
            get
            {
                return _view2DTUsEnabled;
            }
            set
            {
                _view2DTUsEnabled = value;
                NotifyPropertyChanged("View2DTUsEnabled");
            }
        }

        private bool _view4DTUsEnabled = false;
        public bool View4DTUsEnabled
        {
            get
            {
                return _view4DTUsEnabled;
            }
            set
            {
                _view4DTUsEnabled = value;
                NotifyPropertyChanged("View4DTUsEnabled");
            }
        }

        #endregion

        public MainWindow(Socket soc, string servIp, int servPort, string userName, string password)
		{
			InitializeComponent();

            DataContext = this;

            _mainSocket = soc;
            UserName = userName;
            Password = password;
            ServerIP = servIp;
            ServerPort = servPort;

            dgLog.DataContext = LogMsgDispOc;
            LogMsgDispOc.CollectionChanged += new NotifyCollectionChangedEventHandler(LogMsgDispOc_CollectionChanged);

            //TermInfoOc.CollectionChanged += new NotifyCollectionChangedEventHandler(TermInfoOc_CollectionChanged);

            string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
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
            if (MessageBox.Show("确认退出\"DTU管理系统\"?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _bInNormalClose = true;

            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_bInNormalClose == false)
            {
                if (MessageBox.Show("确认退出\"DTU管理系统\"?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    e.Cancel = true;
            }

            if (e.Cancel == false)
            {
                try
                {
                    Helper.DoSendReceive(_mainSocket, Consts.TERM_LOGOUT + UserName, false);
                }
                catch (Exception) { }
                try
                {
                    TerminateAllTerminals(false);
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
            Title = THE_TITLE + " - " + ServerIP + ":" + ServerPort.ToString();// +" ( " + ServerTimeout.ToString() + "ms / " + RemoteTimeout.ToString() + "ms )";

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
            if (TermInfoOc.Count >= 4)
            {
                MessageBox.Show("已经同时控制4个DTU.", "添加DTU失败.", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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
                        MessageBox.Show("失去与服务器的连接.", "添加DTU失败.", MessageBoxButton.OK, MessageBoxImage.Error);
                        Helper.SafeCloseSocket(soc);
                    }
                    else
                    {
                        Tuple<string, byte[], string, string> resp = Helper.ExtractSocketResponse(bytes, bytes.Length);
                        if (resp.Item1 != Consts.TERM_GET_ALL_DTU_OK)
                        {
                            MessageBox.Show("无法获得所有DTU信息 : " + resp.Item3, "添加DTU失败.", MessageBoxButton.OK, MessageBoxImage.Error);
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
                                TerminalInformation curTi = null;
                                foreach (TerminalInformation tii in TermInfoOc)
                                {
                                    if (string.Compare(tii.OldDTUID, _dtuInfoOc[sdtu.DTUSelectedIndex].DtuId, true) == 0)
                                    {
                                        if (tii.State == TerminalInformation.TiState.Connected)
                                        {
                                            MessageBox.Show("DTU(" + _dtuInfoOc[sdtu.DTUSelectedIndex].DtuId + ")已经被你控制.", "添加DTU失败.", MessageBoxButton.OK, MessageBoxImage.Error);
                                            Helper.SafeCloseSocket(soc);
                                            dupDtu = true;
                                        }
                                        else
                                            curTi = tii;

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
                                        MessageBox.Show("无法添加DTU(" + _dtuInfoOc[sdtu.DTUSelectedIndex].DtuId + ") : " + resp.Item3, "添加DTU失败.", MessageBoxButton.OK, MessageBoxImage.Error);
                                        Helper.SafeCloseSocket(soc);
                                    }
                                    else
                                    {
                                        soc.SendTimeout = -1;
                                        soc.ReceiveTimeout = -1;

                                        IPAddress ipad = ((IPEndPoint)soc.RemoteEndPoint).Address;
                                        if (curTi != null)
                                        {
                                            Dispatcher.Invoke((ThreadStart)delegate()
                                            {
                                                curTi.TerminalSocket = soc;
                                                curTi.State = TerminalInformation.TiState.Connected;
                                                curTi.InitUI(true);
                                            }, null);
                                        }
                                        else
                                        {
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
                MessageBox.Show("Socket错误 : " + ex.Message, "添加DTU失败", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("无DTU.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (tvTerminal.SelectedItem == null)
            {
                MessageBox.Show("无选中DTU.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (MessageBox.Show("确认删除DTU(" + ti.OldDTUID + ")?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            byte[] bytes = null;
            _timerPulse.Change(Timeout.Infinite, Consts.TERM_TASK_TIMER_PULSE);
            try
            {
                bytes = Helper.DoSendReceive(_mainSocket, Consts.MAN_UNCTRL_DTU + UserName + "\t" + ti.OldDTUID);
            }
            catch (Exception ex)
            {
                AddLog("释放DTU发生错误 : " + ex.Message, ti.OldDTUID, LogMessage.State.Error, LogMessage.Flow.Request);
                bytes = null;
            }
            _timerPulse.Change(Consts.TERM_TASK_TIMER_PULSE, Consts.TERM_TASK_TIMER_PULSE);
            if (bytes != null)
            {
                Tuple<string, byte[], string, string> resp = Helper.ExtractSocketResponse(bytes, bytes.Length);
                if (resp.Item1 != Consts.MAN_UNCTRL_DTU_OK)
                    MessageBox.Show("删除DTU(" + ti.OldDTUID + ")时发现服务器端DTU状态异常 : " + resp.Item3, "删除DTU信息", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            View1DTU_MenuItem_Click(null, null);

            TermInfoOc.Remove(ti);
            tvTerminal.Items.Remove(ti.CurrentTvItem);
            Helper.SafeCloseSocket(ti.TerminalSocket);
            ti.TerminalSocket = null;
            tcTerminal.Items.Remove(ti.CurrentTabItem);
            AdjustDTUEnable();
        }

        private void About_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            About ab = new About("DTU管理系统", "版权@2012");
            ab.ShowDialog();
        }

        private void Window_Load(object sender, RoutedEventArgs e)
        {
            if (EncryptDecryptLibrary.EncryptDecryptLibrary.CheckRunOrNot() == false)
            {
                MessageBox.Show("无有效许可.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Environment.Exit(0);
            }

            LoadConfig();

            InitUserName();

            _timerPBar = new Timer(new TimerCallback(PBarTimerCallBackHandler), null, Timeout.Infinite, 1000);
            _timerPulse = new Timer(new TimerCallback(PulseTimerCallBackHandler), null, Consts.TERM_TASK_TIMER_PULSE, Consts.TERM_TASK_TIMER_PULSE);
            _timerDTU = new Timer(new TimerCallback(DtuTimerCallBackHandler), null, Consts.TERM_TASK_TIMER_DTU, Consts.TERM_TASK_TIMER_DTU);
        }

        private void InitUserName()
        {
            bool initOK = true;
            try
            {
                byte[] bytes = Helper.DoSendReceive(_mainSocket, Consts.TERM_INIT_USER + UserName);
                if (bytes.Length < 1)
                {
                    MessageBox.Show("失去与服务器的连接.", "初始化使用者失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    TerminateAllTerminals();
                    initOK = false;
                }
                else
                {
                    Tuple<string, byte[], string, string> resp = Helper.ExtractSocketResponse(bytes, bytes.Length);
                    if (resp.Item1 != Consts.TERM_INIT_USER_OK)
                    {
                        MessageBox.Show("错误的服务器响应 : " + resp.Item1 + resp.Item3, "初始化使用者失败", MessageBoxButton.OK, MessageBoxImage.Error);
                        TerminateAllTerminals(false);
                        initOK = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法获得初始化使用者响应 : " + ex.Message, "初始化使用者失败", MessageBoxButton.OK, MessageBoxImage.Error);
                TerminateAllTerminals(false);
                initOK = false;
            }

            if (initOK == false)
            {
                MessageBox.Show("退出DTU管理系统.", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Environment.Exit(1);
            }
        }

        private void PBarTimerCallBackHandler(object obj)
        {
            if ((StatusPbarValue + 1 * 1000) < StatusPbarMax)
                StatusPbarValue = StatusPbarValue + 1 * 1000;
        }

        private void DtuTimerCallBackHandler(object obj)
        {
            lock (_reqLock)
            {
                try
                {
                    string dtus = "";
                    lock (_tiLock)
                    {
                        foreach (TerminalInformation tii in _termInfoOc)
                        {
                            if (string.IsNullOrWhiteSpace(dtus) == true)
                                dtus = tii.OldDTUID.Trim();
                            else
                                dtus = dtus + "\t" + tii.OldDTUID.Trim();
                        }
                    }
                    if (string.IsNullOrWhiteSpace(dtus) == false)
                    {
                        byte[] bytes = Helper.DoSendReceive(_mainSocket, Consts.TERM_CHECK_DTU + dtus);
                        if (bytes.Length < 1)
                        {
                            AddLog("失去与服务器的连接.", "", LogMessage.State.Error, LogMessage.Flow.None);
                            TerminateAllTerminals();
                        }
                        else
                        {
                            Tuple<string, byte[], string, string> resp = Helper.ExtractSocketResponse(bytes, bytes.Length);
                            if (resp.Item1 != Consts.TERM_CHECK_DTU_OK)
                            {
                                AddLog("DTU检查错误 : " + resp.Item3, "", LogMessage.State.Error, LogMessage.Flow.Response);
                                TerminateAllTerminals();
                            }
                            else
                            {
                                AddLog("DTU检查成功.", "", LogMessage.State.Infomation, LogMessage.Flow.Response);
                                string s = resp.Item3;
                                string[] sa = s.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                                List<TerminalInformation> tiList = new List<TerminalInformation>();
                                lock (_tiLock)
                                {
                                    foreach (TerminalInformation tii in _termInfoOc)
                                    {
                                        bool find = false;
                                        foreach (string sai in sa)
                                        {
                                            if (string.Compare(tii.OldDTUID.Trim(), sai.Trim(), true) == 0)
                                            {
                                                find = true;
                                                break;
                                            }
                                        }
                                        if (find == false)
                                            tiList.Add(tii);
                                    }
                                }
                                foreach (TerminalInformation tii in tiList)
                                {
                                    if (tii.State != TerminalInformation.TiState.Disconnected)
                                    {
                                        tii.State = TerminalInformation.TiState.Disconnected;
                                        tii.ReqQueue.Clear();
                                        tii.CTS.Cancel();
                                        Helper.SafeCloseSocket(tii.TerminalSocket);
                                        AddLog("DTU连接失效.", tii.OldDTUID, LogMessage.State.Infomation, LogMessage.Flow.None);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddLog("DTU检查失败 : " + ex.Message, "", LogMessage.State.Error, LogMessage.Flow.None);
                    TerminateAllTerminals();
                }
            }
        }

        private void PulseTimerCallBackHandler(object obj)
        {
            lock (_reqLock)
            {
                try
                {
                    byte[] bytes = Helper.DoSendReceive(_mainSocket, Consts.TERM_PULSE_REQ);
                    if (bytes.Length < 1)
                    {
                        AddLog("失去与服务器的连接.", "", LogMessage.State.Error, LogMessage.Flow.None);
                        TerminateAllTerminals();
                    }
                    else
                    {
                        Tuple<string, byte[], string, string> resp = Helper.ExtractSocketResponse(bytes, bytes.Length);
                        if (resp.Item1 != Consts.TERM_PULSE_REQ_OK)
                        {
                            AddLog("脉搏错误 : " + resp.Item3, "", LogMessage.State.Error, LogMessage.Flow.Response);
                            TerminateAllTerminals();
                        }
                        else
                            AddLog("脉搏成功.", "", LogMessage.State.Infomation, LogMessage.Flow.Response);
                    }
                }
                catch (Exception ex)
                {
                    AddLog("脉搏失败 : " + ex.Message, "", LogMessage.State.Error, LogMessage.Flow.None);
                    TerminateAllTerminals();
                }
            }
        }

        private void TerminateAllTerminals(bool showMsg = true)
        {
            Connected = false;
            _timerPulse.Change(Timeout.Infinite, Consts.TERM_TASK_TIMER_PULSE);
            _timerDTU.Change(Timeout.Infinite, Consts.TERM_TASK_TIMER_DTU);
            lock (_tiLock)
            {
                Dispatcher.Invoke((ThreadStart)delegate()
                {
                    foreach (TerminalInformation tii in TermInfoOc)
                    {
                        tii.CTS.Cancel();
                        tii.State = TerminalInformation.TiState.Disconnected;
                        tii.CurrentDTU = null;
                        Helper.SafeCloseSocket(tii.TerminalSocket);
                        tii.TerminalSocket = null;
                    }
                }, null);
            }
            _cts.Cancel();
            if (showMsg == true)
                AddLog("失去与服务器的连接", "", LogMessage.State.Error, LogMessage.Flow.None);
            Helper.SafeCloseSocket(_mainSocket);
        }

        private void SaveLog_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveLog();
        }

        private void ClearLog_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (LogMsgOc.Count > 0 && MessageBox.Show("是否需要保存日志?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
                            sp.ToolTip = ti.TerminalIPString + " : 未知";
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
                    AddLog("未知的响应 : " + resp, ti.CurrentDTU.DtuId, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                //case Consts.TEST_CONNECTION_RESP:
                //    break;
                case Consts.TERM_INVALID_REQUEST:
                    AddLog("非法请求 : " + resp, ti.CurrentDTU.DtuId, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.TERM_ADD_DTU_OK:
                    AddLog("成功添加DTU : " + resp, ti.CurrentDTU.DtuId, state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
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
                    AddLog("添加DTU错误 : " + resp, ti.CurrentDTU.DtuId, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    Dispatcher.Invoke((ThreadStart)delegate()
                    {
                        TermInfoOc.Remove(ti);
                        UpdateTerminalInforView(ti, false);
                        Helper.SafeCloseSocket(ti.TerminalSocket);
                        ti.TerminalSocket = null;
                    }, null);
                    break;
                case Consts.TERM_PULSE_REQ_OK:
                    AddLog("脉搏成功 : " + resp, ti.CurrentDTU.DtuId, state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
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

        private void UpdateTerminalInforView(TerminalInformation ti, bool isAdd = true, bool noTv = false)
        {
            if (ti == null)
                return;

            if (isAdd == true)
            {
                if (noTv == false)
                {
                    if (ti.CurrentTvItem != null && tvTerminal.Items.Contains(ti.CurrentTvItem) == false)
                        tvTerminal.Items.Add(ti.CurrentTvItem);
                }
                if (ti.CurrentTabItem != null && tcTerminal.Items.Contains(ti.CurrentTabItem) == false)
                {
                    tcTerminal.Items.Add(ti.CurrentTabItem);
                    tcTerminal.SelectedIndex = tcTerminal.Items.Count - 1;
                    AdjustDTUEnable();
                }
            }
            else
            {
                if (noTv == false)
                {
                    if (ti.CurrentTvItem != null && tvTerminal.Items.Contains(ti.CurrentTvItem) == true)
                        tvTerminal.Items.Remove(ti.CurrentTvItem);
                }
                if (ti.CurrentTabItem != null && tcTerminal.Items.Contains(ti.CurrentTabItem) == true)
                {
                    tcTerminal.Items.Remove(ti.CurrentTabItem);
                    AdjustDTUEnable();
                }
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
                        string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
                        StreamWriter sw = new StreamWriter(folder + @"\COMWAY\DTUManagement\log\" + sdt + ".log");
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

                        AddLog("保存日志失败 : " + ex.Message, "", state: LogMessage.State.Error);
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
                string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
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
                string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
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
            try
            {
                _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress server = IPAddress.Parse(ServerIP);
                IPEndPoint iep = new IPEndPoint(server, ServerPort);
                _mainSocket.SendTimeout = Consts.TERM_TIMEOUT;
                _mainSocket.ReceiveTimeout = Consts.TERM_TIMEOUT;
                _mainSocket.Connect(iep);
                if (_mainSocket.Connected)
                {
                    byte[] ba = Helper.DoSendReceive(_mainSocket, Consts.TERM_LOGIN + UserName + "\t" + Password);
                    Tuple<string, byte[], string, string> resp = Helper.ExtractSocketResponse(ba, ba.Length);
                    if (resp == null)
                    {
                        MessageBox.Show("登录失败 : 空的服务器响应.", "重新连接", MessageBoxButton.OK, MessageBoxImage.Error);
                        TerminateAllTerminals();
                    }
                    else
                    {
                        switch (resp.Item1)
                        {
                            default:
                                MessageBox.Show("登录失败 : 未知的服务器响应 - " + resp.Item1 + resp.Item3, "重新连接", MessageBoxButton.OK, MessageBoxImage.Error);
                                TerminateAllTerminals();
                                break;
                            case Consts.TERM_LOGIN_OK:
                                try
                                {
                                    ba = Helper.DoSendReceive(_mainSocket, Consts.TERM_INIT_USER + UserName);
                                    resp = Helper.ExtractSocketResponse(ba, ba.Length);
                                    if (resp == null)
                                    {
                                        MessageBox.Show("登录初始化用户失败 : 空的服务器响应.", "重新连接", MessageBoxButton.OK, MessageBoxImage.Error);
                                        TerminateAllTerminals();
                                    }
                                    else
                                    {
                                        if (resp.Item1 != Consts.TERM_INIT_USER_OK)
                                        {
                                            MessageBox.Show("登录初始化用户失败 : 错误的服务器响应 - " + resp.Item1 + resp.Item3, "重新连接", MessageBoxButton.OK, MessageBoxImage.Error);
                                            TerminateAllTerminals();
                                        }
                                        else
                                        {
                                            #region Reconnect OK

                                            _timerPulse = new Timer(new TimerCallback(PulseTimerCallBackHandler), null, Consts.TERM_TASK_TIMER_PULSE, Consts.TERM_TASK_TIMER_PULSE);
                                            _timerDTU = new Timer(new TimerCallback(DtuTimerCallBackHandler), null, Consts.TERM_TASK_TIMER_DTU, Consts.TERM_TASK_TIMER_DTU);
                                            _cts = new CancellationTokenSource();
                                            _logTask = Task.Factory.StartNew(
                                                () =>
                                                {
                                                    DisplayLog();
                                                }, _cts.Token
                                            );

                                            AddLog("重新登录成功", state: LogMessage.State.OK);

                                            Connected = true;

                                            #endregion
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("无法获得初始化使用者响应 : " + ex.Message, "重新连接", MessageBoxButton.OK, MessageBoxImage.Error);
                                    TerminateAllTerminals();
                                }
                                break;
                            case Consts.TERM_LOGIN_ERR:
                                MessageBox.Show("登录失败 : " + resp.Item3, "重新连接", MessageBoxButton.OK, MessageBoxImage.Error);
                                TerminateAllTerminals();
                                break;
                        }
                    }
                }
                else
                {
                    MessageBox.Show("登录失败 : 服务器连接错误", "重新连接", MessageBoxButton.OK, MessageBoxImage.Error);
                    TerminateAllTerminals(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("登录失败 : " + ex.Message, "重新连接", MessageBoxButton.OK, MessageBoxImage.Error);
                TerminateAllTerminals(true);
            }
        }

        private void View1DTU_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (tcTerminal.Items.Count == (TermInfoOc.Count + 1))
                return;
            for (int i = tcTerminal.Items.Count - 1; i > 0; i--)
            {
                object content = ((TabItem)(tcTerminal.Items[i])).Content;
                if (content is View2DTUsUC)
                {
                    ((View2DTUsUC)content).TabControl0.Items.Clear();
                    ((View2DTUsUC)content).TabControl1.Items.Clear();
                }
                if (content is View4DTUsUC)
                {
                    ((View4DTUsUC)content).TabControl00.Items.Clear();
                    ((View4DTUsUC)content).TabControl01.Items.Clear();
                    ((View4DTUsUC)content).TabControl10.Items.Clear();
                    ((View4DTUsUC)content).TabControl11.Items.Clear();
                }
                tcTerminal.Items.RemoveAt(i);
            }

            foreach (TerminalInformation ti in TermInfoOc)
            {
                UpdateTerminalInforView(ti, noTv: true);
            }

            tcTerminal.SelectedIndex = 1;

            //View1DTUEnabled = false;
            //if (tcTerminal.Items.Count > 1)
            //    View2DTUsEnabled = true;
            //if (tcTerminal.Items.Count == 4)
            //    View4DTUsEnabled = true;

            AdjustDTUEnable();
        }

        private void View2DTUs_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (TermInfoOc.Count == 0 || TermInfoOc.Count == 1)
                return;
            if (TermInfoOc.Count == 2 && tcTerminal.Items.Count != 3)
                return;
            if (TermInfoOc.Count == 3 && tcTerminal.Items.Count != 4)
                return;
            if (TermInfoOc.Count == 4 && (tcTerminal.Items.Count != 5 && tcTerminal.Items.Count != 2 && tcTerminal.Items.Count != 4))
                return;

            for (int i = tcTerminal.Items.Count - 1; i > 0; i--)
            {
                object content = ((TabItem)(tcTerminal.Items[i])).Content;
                if (content is View2DTUsUC)
                {
                    ((View2DTUsUC)content).TabControl0.Items.Clear();
                    ((View2DTUsUC)content).TabControl1.Items.Clear();
                }
                if (content is View4DTUsUC)
                {
                    ((View4DTUsUC)content).TabControl00.Items.Clear();
                    ((View4DTUsUC)content).TabControl01.Items.Clear();
                    ((View4DTUsUC)content).TabControl10.Items.Clear();
                    ((View4DTUsUC)content).TabControl11.Items.Clear();
                }
                tcTerminal.Items.RemoveAt(i);
            }

            if (TermInfoOc.Count == 2)
            {
                View2DTUsUC v2uc = new View2DTUsUC();
                v2uc.TabControl0.Items.Add(TermInfoOc[0].CurrentTabItem);
                v2uc.TabControl1.Items.Add(TermInfoOc[1].CurrentTabItem);

                TabItem ti = new TabItem();
                ti.Header = TermInfoOc[0].CurrentDTU.DtuId + ", " +
                    TermInfoOc[1].CurrentDTU.DtuId;
                ti.Content = v2uc;

                tcTerminal.Items.Add(ti);
            }
            else if (TermInfoOc.Count == 3)
            {
                View2DTUsUC v2uc = new View2DTUsUC();
                v2uc.TabControl0.Items.Add(TermInfoOc[0].CurrentTabItem);
                v2uc.TabControl1.Items.Add(TermInfoOc[1].CurrentTabItem);

                TabItem ti = new TabItem();
                ti.Header = TermInfoOc[0].CurrentDTU.DtuId + ", " +
                    TermInfoOc[1].CurrentDTU.DtuId;
                ti.Content = v2uc;

                tcTerminal.Items.Add(ti);
 
                UpdateTerminalInforView(TermInfoOc[2], noTv: true);
            }
            else
            {
                View2DTUsUC v2uc = new View2DTUsUC();
                v2uc.TabControl0.Items.Add(TermInfoOc[0].CurrentTabItem);
                v2uc.TabControl1.Items.Add(TermInfoOc[1].CurrentTabItem);

                TabItem ti = new TabItem();
                ti.Header = TermInfoOc[0].CurrentDTU.DtuId + ", " +
                    TermInfoOc[1].CurrentDTU.DtuId;
                ti.Content = v2uc;

                tcTerminal.Items.Add(ti);

                View2DTUsUC v2uc0 = new View2DTUsUC();
                v2uc0.TabControl0.Items.Add(TermInfoOc[2].CurrentTabItem);
                v2uc0.TabControl1.Items.Add(TermInfoOc[3].CurrentTabItem);

                TabItem ti0 = new TabItem();
                ti0.Header = TermInfoOc[2].CurrentDTU.DtuId + ", " +
                    TermInfoOc[3].CurrentDTU.DtuId;
                ti0.Content = v2uc0;

                tcTerminal.Items.Add(ti0);
            }

            tcTerminal.SelectedIndex = 1;

            AdjustDTUEnable();
        }

        private void View4DTUs_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (TermInfoOc.Count == 0 || TermInfoOc.Count == 1 || TermInfoOc.Count == 2 || TermInfoOc.Count == 3)
                return;
            if (TermInfoOc.Count == 4 && (tcTerminal.Items.Count != 5 && tcTerminal.Items.Count != 3 && tcTerminal.Items.Count != 4))
                return;

            for (int i = tcTerminal.Items.Count - 1; i > 0; i--)
            {
                object content = ((TabItem)(tcTerminal.Items[i])).Content;
                if (content is View2DTUsUC)
                {
                    ((View2DTUsUC)content).TabControl0.Items.Clear();
                    ((View2DTUsUC)content).TabControl1.Items.Clear();
                }
                if (content is View4DTUsUC)
                {
                    ((View4DTUsUC)content).TabControl00.Items.Clear();
                    ((View4DTUsUC)content).TabControl01.Items.Clear();
                    ((View4DTUsUC)content).TabControl10.Items.Clear();
                    ((View4DTUsUC)content).TabControl11.Items.Clear();
                }
                tcTerminal.Items.RemoveAt(i);
            }

            View4DTUsUC v4uc = new View4DTUsUC();
            v4uc.TabControl00.Items.Add(TermInfoOc[0].CurrentTabItem);
            v4uc.TabControl01.Items.Add(TermInfoOc[1].CurrentTabItem);
            v4uc.TabControl10.Items.Add(TermInfoOc[2].CurrentTabItem);
            v4uc.TabControl11.Items.Add(TermInfoOc[3].CurrentTabItem);

            TabItem ti = new TabItem();
            ti.Header = TermInfoOc[0].CurrentDTU.DtuId + ", " +
                TermInfoOc[1].CurrentDTU.DtuId + ", " +
                TermInfoOc[2].CurrentDTU.DtuId + ", " +
                TermInfoOc[3].CurrentDTU.DtuId;
            ti.Content = v4uc;

            tcTerminal.Items.Add(ti);
            tcTerminal.SelectedIndex = 1;

            AdjustDTUEnable();
        }

        private void AdjustDTUEnable()
        {
            switch (TermInfoOc.Count)
            {
                default:
                    View1DTUEnabled = true;
                    View2DTUsEnabled = false;
                    View4DTUsEnabled = false;
                    break;
                case 0:
                case 1:
                    View1DTUEnabled = false;
                    View2DTUsEnabled = false;
                    View4DTUsEnabled = false;
                    break;
                case 2:
                    if (tcTerminal.Items.Count == 2)
                    {
                        View1DTUEnabled = true;
                        View2DTUsEnabled = false;
                        View4DTUsEnabled = false;
                    }
                    else if (tcTerminal.Items.Count == 3)
                    {
                        View1DTUEnabled = false;
                        View2DTUsEnabled = true;
                        View4DTUsEnabled = false;
                    }
                    else
                    {
                        View1DTUEnabled = false;
                        View2DTUsEnabled = false;
                        View4DTUsEnabled = false;
                    }
                    break;
                case 3:
                    if (tcTerminal.Items.Count == 3)
                    {
                        View1DTUEnabled = true;
                        View2DTUsEnabled = false;
                        View4DTUsEnabled = false;
                    }
                    else if (tcTerminal.Items.Count == 4)
                    {
                        View1DTUEnabled = false;
                        View2DTUsEnabled = true;
                        View4DTUsEnabled = false;
                    }
                    else
                    {
                        View1DTUEnabled = false;
                        View2DTUsEnabled = false;
                        View4DTUsEnabled = false;
                    }
                    break;
                case 4:
                    if (tcTerminal.Items.Count == 2)
                    {
                        View1DTUEnabled = true;
                        View2DTUsEnabled = true;
                        View4DTUsEnabled = false;
                    }
                    else if (tcTerminal.Items.Count == 3)
                    {
                        View1DTUEnabled = true;
                        View2DTUsEnabled = false;
                        View4DTUsEnabled = true;
                    }
                    else if (tcTerminal.Items.Count == 4)
                    {
                        View1DTUEnabled = true;
                        View2DTUsEnabled = true;
                        View4DTUsEnabled = true;
                    }
                    else if (tcTerminal.Items.Count == 5)
                    {
                        View1DTUEnabled = false;
                        View2DTUsEnabled = true;
                        View4DTUsEnabled = true;
                    }
                    else
                    {
                        View1DTUEnabled = false;
                        View2DTUsEnabled = false;
                        View4DTUsEnabled = false;
                    }
                    break;
            }
        }
    }
}
