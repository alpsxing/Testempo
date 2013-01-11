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

        #region Consts

        //public const int TAB_SERVER_INDEX = 0;
        public const int TAB_USER_INDEX = 0;
        public const int TAB_DTU_INDEX = 1;
        public const int TAB_LOG_INDEX = 2;

        #endregion

        #region Variables

        private ObservableCollection<UserInfo> _userInfoOc = new ObservableCollection<UserInfo>();
        private ObservableCollection<DTUInfo> _dtuInfoOc = new ObservableCollection<DTUInfo>();
        private ObservableCollection<Tuple<string, string, string>> _serverLogOc = new ObservableCollection<Tuple<string, string, string>>();

        private bool _bInNormalClose = false;

        private CancellationTokenSource _cts = null;
        private Task _logTask = null;
        private int _logIndex = 1;
        private Queue<LogMessage> _logQueue = new Queue<LogMessage>();
        private ObservableCollection<LogMessage> _logOc = new ObservableCollection<LogMessage>();
        private ObservableCollection<LogMessage> _logDispOc = new ObservableCollection<LogMessage>();
        private object _logLock = new object();
        //private Timer _pulseTimer = null;
        //private Timer _serviceTimer = null;
        private Timer _userTimer = null;
        private Timer _serverLlogTimer = null;
        private Timer _dtuTimer = null;

        private object _reqLock = new object();
        private object _dtuLock = new object();
        private object _serverLogLock = new object();

        private int _selectedUserIndex = -1;
        private int _selectedDtuIndex = -1;

        private Socket _manSocket = null;

        #endregion

        #region Properties

        private Queue<Tuple<string, string>> _requestQueue = new Queue<Tuple<string, string>>();
        public Queue<Tuple<string, string>> ReqQueue
        {
            get
            {
                return _requestQueue;
            }
            set
            {
                _requestQueue = value;
            }
        }

        private bool _connected = false;
        public bool Connected
        {
            get
            {
                return _connected;
            }
            set
            {
                _connected = value;
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
                Title = "服务管理 - " + _userName + " ( " + UserPermissionDislay + " ) - " + ReadyString;
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

        private string _userPermission = "";
        public string UserPermission
        {
            get
            {
                return _userPermission;
            }
            set
            {
                _userPermission = value;
                Title = "服务管理 - " + _userName + " ( " + UserPermissionDislay + " ) - " + ReadyString;
                NotifyPropertyChanged("UserPermission");
                NotifyPropertyChanged("UserPermissionDislay");
            }
        }

        public string UserPermissionDislay
        {
            get
            {
                if (UserPermission == "0")
                    return "超级用户";
                else if (UserPermission == "1")
                    return "管理用户";
                else
                    return "普通用户";
            }
        }

        private LogMessage.State _readyState = LogMessage.State.None;
        public LogMessage.State ReadyState
        {
            get
            {
                return _readyState;
            }
            set
            {
                _readyState = value;
                _readyStateImage = new BitmapImage();
                _readyStateImage.BeginInit();
                switch (_readyState)
                {
                    default:
                    case LogMessage.State.None:
                        _readyStateImage.UriSource = new Uri("pack://application:,,,/serviceconfiguration;component/resources/status_none.png");
                        break;
                    case LogMessage.State.Infomation:
                        _readyStateImage.UriSource = new Uri("pack://application:,,,/serviceconfiguration;component/resources/status_info.png");
                        break;
                    case LogMessage.State.OK:
                        _readyStateImage.UriSource = new Uri("pack://application:,,,/serviceconfiguration;component/resources/status_ok.png");
                        break;
                    case LogMessage.State.Fail:
                        _readyStateImage.UriSource = new Uri("pack://application:,,,/serviceconfiguration;component/resources/status_error.png");
                        break;
                    case LogMessage.State.Error:
                        _readyStateImage.UriSource = new Uri("pack://application:,,,/serviceconfiguration;component/resources/status_ques.png");
                        break;
                }
                _readyStateImage.EndInit();
                NotifyPropertyChanged("ReadyState");
                NotifyPropertyChanged("ReadyStateImage");
            }
        }

        private BitmapImage _readyStateImage = null;
        public BitmapImage ReadyStateImage
        {
            get
            {
                return _readyStateImage;
            }
            set
            {
                _readyStateImage = value;
                NotifyPropertyChanged("ReadyStateImage");
            }
        }

        private string _readyString = "未连接";
        public string ReadyString
        {
            get
            {
                return _readyString;
            }
            set
            {
                _readyString = value;
                Title = "Service Configuration - " + _userName + " ( " + UserPermissionDislay + " ) - " + ReadyString;
                NotifyPropertyChanged("ReadyString");
            }
        }

        private string _serverIP = "";
        public string ServerIP
        {
            get
            {
                return _serverIP;
            }
            set
            {
                _serverIP = value;
                NotifyPropertyChanged("ServerIP");
            }
        }

        private int _serverPort = Consts.MAN_PORT;
        public int ServerPort
        {
            get
            {
                return _serverPort;
            }
            set
            {
                _serverPort = value;
                NotifyPropertyChanged("ServerPort");
            }
        }

        private int _serverWebPort = Consts.MAN_WEB_PORT;
        public int ServerWebPort
        {
            get
            {
                return _serverWebPort;
            }
            set
            {
                _serverWebPort = value;
                NotifyPropertyChanged("ServerWebPort");
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

        #endregion

        public MainWindow(Socket soc, 
            string username, string password, string userPerm, 
            string serverip, int serverport, int serverwebport,
            int maxLogCount = Consts.MAX_LOG_COUNT,
            int maxLogDispLog = Consts.MAX_LOG_DISPLAY_COUNT)
        {
            string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            if (Directory.Exists(folder + @"\COMWAY") == false)
                Directory.CreateDirectory(folder + @"\COMWAY");
            folder = folder + @"\COMWAY";
            if (Directory.Exists(folder + @"\ServiceConfiguration") == false)
                Directory.CreateDirectory(folder + @"\ServiceConfiguration");
            if (Directory.Exists(folder + @"\ServiceConfiguration\config") == false)
                Directory.CreateDirectory(folder + @"\ServiceConfiguration\config");
            if (Directory.Exists(folder+ @"\ServiceConfiguration\log") == false)
                Directory.CreateDirectory(folder + @"\ServiceConfiguration\log");

            InitializeComponent();

            DataContext = this;

            _manSocket = soc;

            UserName = username;
            Password = password;
            UserPermission = userPerm;
            ServerIP = serverip;
            ServerPort = serverport;
            ServerWebPort = serverwebport;

            MaxLogCount = maxLogCount;
            MaxLogDisplayCount = maxLogDispLog;

            dgUser.DataContext = _userInfoOc;
            dgLog.DataContext = _logDispOc;
            dgDtu.DataContext = _dtuInfoOc;
            _logDispOc.CollectionChanged += new NotifyCollectionChangedEventHandler(LogDispOc_CollectionChanged);

            Connected = true;
        }

        #region Window Exit

        private void Window_Exit(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确认退出\"服务管理\"?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _bInNormalClose = true;

            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_bInNormalClose == false)
            {
                if (MessageBox.Show("确认退出\"服务管理\"?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    e.Cancel = true;
            }

            if (e.Cancel == false)
            {
                PutRequest(new Tuple<string, string>(Consts.MAN_LOGOUT, ""));
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

        private void LogDispOc_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (LogAutoScrolling == false)
                return;

            //lock (_logLock) // Lock will lead to dead lock, why?
            if (tcServiceConfig.SelectedIndex == MainWindow.TAB_LOG_INDEX)
            {
                Dispatcher.Invoke((ThreadStart)delegate
                {
                    if (_logOc.Count < 1)
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

        private void About_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            About ab = new About("服务管理系统", "版权 @ 2012");
            ab.ShowDialog();
        }

        private void Window_Load(object sender, RoutedEventArgs e)
        {
            if (EncryptDecryptLibrary.EncryptDecryptLibrary.CheckRunOrNot() == false)
            {
                MessageBox.Show("无有效许可.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Environment.Exit(0);
            }

            _cts = new CancellationTokenSource();
            Task.Factory.StartNew(
                () =>
                {
                    ManTask();
                }, _cts.Token);
            _logTask = Task.Factory.StartNew(
                () =>
                {
                    DisplayLog();
                }, _cts.Token
            );
        }

        private void ManTask(bool reLogin = false)
        {
            Dispatcher.Invoke((ThreadStart)delegate
            {
                ReadyString = "连接";
            }, null);

            lock (_reqLock)
            {
                ReqQueue.Clear();
            }

            if (reLogin == true)
                PutRequest(new Tuple<string, string>(Consts.MAN_LOGIN, UserName + "\t" + Password));

            _userTimer = new Timer(new TimerCallback(UserTimerCallBack), null,
                                0,
                                Consts.MAN_TASK_TIMER_INTERVAL);

            _serverLlogTimer = new Timer(new TimerCallback(ServerLogTimerCallBack), null,
                                0,
                                Consts.MAN_TASK_TIMER_INTERVAL);

            _dtuTimer = new Timer(new TimerCallback(DtuTimerCallBack), null,
                                0,
                                Consts.MAN_TASK_TIMER_INTERVAL);

            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    Tuple<string, string> req = GetRequest();
                    if (req != null)
                    {
                        switch (req.Item1)
                        {
                            default:
                                AddLog("未知请求 : " + req.Item1 + req.Item2, state: LogMessage.State.Fail, flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_ADD_DTU:
                                AddLog("请求 : 添加DTU", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_GET_ALL_DTU:
                                AddLog("请求 : 获取所有DTU信息", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_DELETE_DTU:
                                AddLog("请求 : 删除DTU", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_MODIFY_DTU:
                                AddLog("请求 : 修改DTU信息", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_GET_ALL_USER:
                                AddLog("请求 : 获取所有用户信息", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_ADD_USER:
                                AddLog("请求 : 添加用户", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_MODIFY_USER:
                                AddLog("请求 : 修改用户密码", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_DELETE_USER:
                                AddLog("请求 : 删除用户", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_LOGIN:
                                AddLog("请求 : 登录", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_LOGOUT:
                                AddLog("请求 : 登出", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_KICK_DTU:
                                AddLog("请求 : 断开DTU连接", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_KICK_USER:
                                AddLog("请求 : 断开用户连接", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_UNCTRL_DTU:
                                AddLog("请求 : 释放DTU控制", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_GET_LOG_INFO:
                                AddLog("请求 : 获取所有服务器日志用户", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_DEL_LOG_USER:
                                AddLog("请求 : 删除用户服务器日志", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_DEL_LOG_DATE:
                                AddLog("请求 : 删除给定日期前服务器日志", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_DEL_LOG_USER_DATE:
                                AddLog("请求 : 删除给定日期前用户服务器日志用户", flow: LogMessage.Flow.Request);
                                break;
                        }
                        _manSocket.Send(Encoding.UTF8.GetBytes(req.Item1 + req.Item2));
                        byte[] bytes = new byte[Consts.SOCKET_RECEIVING_BUFFER_LENGTH_MAN];
                        int length = _manSocket.Receive(bytes);
                        if (length < 1)
                        {
                            AddLog("失去和服务器的连接", state: LogMessage.State.Error, flow: LogMessage.Flow.Response);
                            _userTimer.Change(Timeout.Infinite, Consts.MAN_TASK_TIMER_INTERVAL);
                            _serverLlogTimer.Change(Timeout.Infinite, Consts.MAN_TASK_TIMER_INTERVAL);
                            _dtuTimer.Change(Timeout.Infinite, Consts.MAN_TASK_TIMER_INTERVAL);
                            lock (_reqLock)
                            {
                                if (ReqQueue.Count > 0)
                                    ReqQueue.Clear();
                            }
                            break;
                        }
                        else
                        {
                            ProcessServerResponse(bytes, length, reLogin);
                            reLogin = false;
                        }
                    }
                    Thread.Sleep(Consts.TERM_TASK_REQUEST_SLEEP_TIME);
                }
            }
            catch (Exception ex)
            {
                AddLog("与服务器通信出现错误 : " + ex.Message, state: LogMessage.State.Error);
            }
            Dispatcher.Invoke((ThreadStart)delegate
            {
                ReadyString = "未连接";
                foreach (UserInfo uii in _userInfoOc)
                {
                    uii.Online = false;
                }
                foreach (DTUInfo dii in _dtuInfoOc)
                {
                    dii.Online = false;
                }
            }, null);
            try
            {
                _dtuTimer.Dispose();
            }
            catch (Exception) { }
            try
            {
                _serverLlogTimer.Dispose();
            }
            catch (Exception) { }
            try
            {
                _userTimer.Dispose();
            }
            catch (Exception) { }
            lock (_reqLock)
            {
                ReqQueue.Clear();
            }
            Helper.SafeCloseSocket(_manSocket);
            _manSocket = null;
            Connected = false;
        }

        /// <summary>
        /// The response is string
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="len"></param>
        /// <param name="reLogin"></param>
        private void ProcessServerResponse(byte[] bytes, int len, bool reLogin = false)
        {
            Tuple<string, byte[], string, string> data = Helper.ExtractSocketResponse(bytes, len);
            if (data == null)
            {
                AddLog("空的服务器响应 : " + Encoding.UTF8.GetString(bytes, 0, len), state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                return;
            }

            if (reLogin == true)
            {
                if (data.Item1 != Consts.MAN_LOGIN_OK && data.Item1 != Consts.MAN_LOGIN_ERR)
                {
                    AddLog("位置的服务器响应 : " + data.Item1 + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    Helper.SafeCloseSocket(_manSocket);
                    _manSocket = null;
                    return;
                }
                if (data.Item1 == Consts.MAN_LOGIN_OK)
                {
                    AddLog("重新连接成功.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    Connected = true;
                    return;
                }
                else
                {
                    AddLog("重新连接错误 : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    Helper.SafeCloseSocket(_manSocket);
                    _manSocket = null;
                    Connected = false;
                    return;
                }
            }

            switch (data.Item1)
            {
                default:
                    AddLog("位置的服务器响应 : " + data.Item1 + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_INVALID_REQUEST:
                    AddLog("无效的请求 : " + data.Item1 + data.Item3, state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_GET_ALL_USER_OK:
                    AddLog("成功获得所有用户信息.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    ProcessAllUser(data.Item3);
                    break;
                case Consts.MAN_GET_ALL_DTU_OK:
                    AddLog("成功获得所有DTU信息.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    ProcessAllDTU(data.Item3);
                    break;
                case Consts.MAN_GET_ALL_USER_ERR:
                    AddLog("无法获得所有用户信息 : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_ADD_USER_OK:
                    AddLog("成功添加用户.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_USER, ""));
                    break;
                case Consts.MAN_ADD_USER_ERR:
                    AddLog("无法添加用户 : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_MODIFY_USER_OK:
                    AddLog("成功修改用户密码.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_USER, ""));
                    break;
                case Consts.MAN_MODIFY_USER_ERR:
                    AddLog("无法修改用户密码 : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_DELETE_USER_OK:
                    AddLog("成功删除用户.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_USER, ""));
                    break;
                case Consts.MAN_DELETE_USER_ERR:
                    AddLog("无法删除用户 : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_ADD_DTU_OK:
                    AddLog("成功添加DTU.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_DTU, ""));
                    break;
                case Consts.MAN_ADD_DTU_ERR:
                    AddLog("无法添加DTU : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_DELETE_DTU_OK:
                    AddLog("成功删除DTU.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_DTU, ""));
                    break;
                case Consts.MAN_DELETE_DTU_ERR:
                    AddLog("无法删除DTU : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_MODIFY_DTU_OK:
                    AddLog("成功修改DTU.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_DTU, ""));
                    break;
                case Consts.MAN_MODIFY_DTU_ERR:
                    AddLog("无法修改DTU : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_KICK_DTU_OK:
                    AddLog("成功断开DTU连接.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_DTU, ""));
                    break;
                case Consts.MAN_KICK_DTU_ERR:
                    AddLog("无法断开DTU连接 : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_KICK_USER_OK:
                    AddLog("成功断开用户连接.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_USER, ""));
                    break;
                case Consts.MAN_KICK_USER_ERR:
                    AddLog("无法断开用户连接 : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_UNCTRL_DTU_OK:
                    AddLog("成功释放DTU控制.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_DTU, ""));
                    break;
                case Consts.MAN_UNCTRL_DTU_ERR:
                    AddLog("无法释放DTU控制 : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_LOGIN_OK:
                    AddLog("登录成功.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_USER, ""));
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_DTU, ""));
                    break;
                case Consts.MAN_LOGIN_ERR:
                    AddLog("登录失败 : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_GET_LOG_INFO_OK:
                    AddLog("成功获得所有服务器日志用户信息.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    lock (_serverLogLock)
                    {
                        _serverLogOc.Clear();
                        try
                        {
                            string[] sa = data.Item3.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string si in sa)
                            {
                                string[] sia = si.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                if (sia != null && sia.Length == 4)
                                {
                                    DateTime dt;
                                    string dtstr = Helper.ConvertDateTime(sia[1], Helper.DateTimeType.Date) + " " + Helper.ConvertDateTime(sia[2], Helper.DateTimeType.Time);
                                    if (DateTime.TryParse(dtstr, out dt) == true)
                                        _serverLogOc.Add(new Tuple<string, string, string>(sia[0].Trim(), sia[1].Trim() + " " + sia[2].Trim(), sia[3].Trim()));
                                }
                            }
                        }
                        catch (Exception) { }
                    }
                   break;
                case Consts.MAN_GET_LOG_INFO_ERR:
                    AddLog("无法获得所有服务器日志用户信息 : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    lock (_serverLogLock)
                    {
                        _serverLogOc.Clear();
                    }
                    break;
                case Consts.MAN_DEL_LOG_USER_OK:
                    AddLog("删除用户日志成功 : " + data.Item3, state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_DEL_LOG_USER_ERR:
                    AddLog("删除用户日志失败 : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_DEL_LOG_DATE_OK:
                    AddLog("删除给定日期日志成功 : " + data.Item3, state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_DEL_LOG_DATE_ERR:
                    AddLog("删除给定日期日志失败 : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_DEL_LOG_USER_DATE_OK:
                    AddLog("删除给定日期用户日志成功 : " + data.Item3, state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_DEL_LOG_USER_DATE_ERR:
                    AddLog("删除给定日期用户日志失败 : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
            }
        }

        private void ProcessAllUser(string content)
        {
            Dispatcher.Invoke((ThreadStart)delegate
            {
                _userInfoOc.Clear();
                if (string.IsNullOrWhiteSpace(content))
                    return;
                content = content.Trim(new char[] { '\0' });
                string[] sa = content.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (sa == null || sa.Length < 1)
                    return;
                foreach (string sai in sa)
                {
                    string[] saia = sai.Split(new string[] { "\t" }, StringSplitOptions.None);
                    if (saia == null || (saia.Length != Consts.USER_INFO_ITEM_COUNT))
                        continue;
                    bool online = false;
                    if (string.Compare(saia[2].Trim(), "y", true) == 0)
                        online = true;
                    _userInfoOc.Add(new UserInfo()
                    {
                        UserName = saia[0].Trim(),
                        Permission = saia[1].Trim(),
                        Online = online,
                        DtLogString = saia[3].Trim(),
                        Information = saia[4].Trim()
                    });
                }
                if (_userInfoOc.Count > 0 && _selectedUserIndex > -1)
                {
                    if (_userInfoOc.Count <= _selectedUserIndex)
                        _selectedUserIndex = _userInfoOc.Count - 1;
                    if (tcServiceConfig.SelectedIndex == MainWindow.TAB_USER_INDEX)
                    {
                        dgUser.SelectedIndex = _selectedUserIndex;
                        dgUser.ScrollIntoView(dgUser.Items[_selectedUserIndex]);
                    }
                }
            }, null);
        }

        private void ProcessAllDTU(string content)
        {
            Dispatcher.Invoke((ThreadStart)delegate
            {
                lock (_dtuLock)
                {
                    Helper.FillDTUInfoOC(_dtuInfoOc, content);
                }
                if (_dtuInfoOc.Count > 0 && _selectedDtuIndex > -1)
                {
                    if (_dtuInfoOc.Count <= _selectedDtuIndex)
                        _selectedDtuIndex = _dtuInfoOc.Count - 1;
                    if (tcServiceConfig.SelectedIndex == MainWindow.TAB_DTU_INDEX)
                    {
                        dgDtu.SelectedIndex = _selectedDtuIndex;
                        dgDtu.ScrollIntoView(dgDtu.Items[_selectedDtuIndex]);
                    }
                }
            }, null);
        }

        private void UserTimerCallBack(object obj)
        {
            PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_USER, ""));
        }

        private void ServerLogTimerCallBack(object obj)
        {
            PutRequest(new Tuple<string, string>(Consts.MAN_GET_LOG_INFO, ""));
        }

        private void DtuTimerCallBack(object obj)
        {
            PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_DTU, ""));
        }

        private void PutRequest(Tuple<string, string> req)
        {
            lock (_reqLock)
            {
                ReqQueue.Enqueue(req);
            }
        }

        private void AddLog(string msg = "", string ip = "", LogMessage.State state = LogMessage.State.None, LogMessage.Flow flow = LogMessage.Flow.None)
        {
            Dispatcher.Invoke((ThreadStart)delegate
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

                            while (_logDispOc.Count > MaxLogDisplayCount)
                                _logDispOc.RemoveAt(0);

                            _logDispOc.Add(lm);

                            if (MaxLogCount > 0 && _logOc.Count > MaxLogCount)
                                SaveLog();

                            _logOc.Add(lm);
                        }, null);
                    }
                }

                Thread.Sleep(Consts.TASK_THREAD_SLEEP_INTERVAL);
            }
        }

        private Tuple<string, string> GetRequest()
        {
            lock (_reqLock)
            {
                if (ReqQueue.Count > 0)
                    return ReqQueue.Dequeue();
                else
                    return null;
            }
        }

        private void User_DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_userInfoOc.Count < 1)
                return;
            _selectedUserIndex = dgUser.SelectedIndex;
        }

        private void DTU_DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_dtuInfoOc.Count < 1)
                return;
            _selectedDtuIndex = dgDtu.SelectedIndex;
        }

        private void UserAdd_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (UserPermission != "0" && UserPermission != "1")
            {
                MessageBox.Show("普通用户没有添加新用户的权限.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            tcServiceConfig.SelectedIndex = MainWindow.TAB_USER_INDEX;

            NewUser nu = new NewUser(UserPermission);
            bool? b = nu.ShowDialog();
            if (b == false)
                return;

            PutRequest(new Tuple<string, string>(Consts.MAN_ADD_USER, nu.UserName + "\t" + nu.Password + "\t" + nu.NewPermission));
        }

        private void UserDisconnect_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (UserPermission != "0" && UserPermission != "1")
            {
                MessageBox.Show("普通用户没有断开用户连接的权限.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            tcServiceConfig.SelectedIndex = MainWindow.TAB_USER_INDEX;

            if (_userInfoOc.Count < 1)
            {
                MessageBox.Show("无用户在线.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int index = dgUser.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("无选中用户.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            UserInfo ui = _userInfoOc[index];

            if (string.Compare(UserName.Trim(), ui.UserName.Trim(), true) == 0)
            {
                MessageBox.Show("\"" + ui.UserName + "\"不允许断开自己的连接.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (ui.Online == false)
            {
                MessageBox.Show("\"" + ui.UserName + "\"已经不在线.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("确认断开\"" + ui.UserName + "\"的连接?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            PutRequest(new Tuple<string, string>(Consts.MAN_KICK_USER, ui.UserName));
        }

        private void UserEdit_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            tcServiceConfig.SelectedIndex = MainWindow.TAB_USER_INDEX;

            if (_userInfoOc.Count < 1)
            {
                MessageBox.Show("无用户.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int index = dgUser.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("无选中用户.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            UserInfo ui = _userInfoOc[index];

            // if target is "admin"

            if (string.Compare("admin", ui.UserName.Trim(), true) == 0 || ui.Permission.Trim() == "0")
            {
                MessageBox.Show("超级用户不接受修改.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // if operator is common user

            if (UserPermission == "2" && (ui.Permission != "2" || string.Compare(UserName.Trim(), ui.UserName.Trim(), true) != 0))
            {
                MessageBox.Show("普通用户没有权限修改\"" + ui.UserName + "\".", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // if operator is management user

            if (UserPermission == "1" && ui.Permission == "1" && string.Compare(UserName.Trim(), ui.UserName.Trim(), true) != 0)
            {
                MessageBox.Show("管理用户没有权限修改\"" + ui.UserName + "\".", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            NewUser nu = new NewUser(ui.Permission, false, ui.UserName);
            bool? b = nu.ShowDialog();
            if (b != true)
                return;

            PutRequest(new Tuple<string, string>(Consts.MAN_MODIFY_USER, nu.UserName + "\t" + nu.Password));
        }

        private void UserRemove_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (UserPermission != "0" && UserPermission != "1")
            {
                MessageBox.Show("普通用户没有权限删除用户.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            tcServiceConfig.SelectedIndex = MainWindow.TAB_USER_INDEX;

            if (_userInfoOc.Count < 1)
            {
                MessageBox.Show("无用户.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int index = dgUser.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("无选中用户.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            UserInfo ui = _userInfoOc[index];

            if (string.Compare(UserName.Trim(), ui.UserName.Trim(), true) == 0)
            {
                MessageBox.Show("\"" + ui.UserName + "\"不允许删除自己.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.Compare(ui.UserName.Trim(), "admin", true) == 0)
            {
                MessageBox.Show("\"admin\"不能够被删除.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (UserPermission == "1" && (ui.Permission == "0" || ui.Permission == "1"))
            {
                MessageBox.Show("只有超级用户才可以删除\"" + ui.UserName + "\".", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("确认删除\"" + ui.UserName + "\".", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            PutRequest(new Tuple<string, string>(Consts.MAN_DELETE_USER, ui.UserName));
        }

        private void Configuration_TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void DtuAdd_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            tcServiceConfig.SelectedIndex = MainWindow.TAB_DTU_INDEX;

            DTUConfiguration dc = new DTUConfiguration();
            bool? b = dc.ShowDialog();
            if (b != true)
                return;

            PutRequest(new Tuple<string, string>(
                Consts.MAN_ADD_DTU,
                dc.DtuId + "\t" + dc.SimId + "\t" + dc.UserName + "\t" + dc.UserTel));
        }

        private void DtuModify_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            tcServiceConfig.SelectedIndex = MainWindow.TAB_DTU_INDEX;

            if (_dtuInfoOc.Count < 1)
            {
                MessageBox.Show("无DTU.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int index = dgDtu.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("无选中DTU.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DTUConfiguration dc = new DTUConfiguration(
                DTUConfiguration.OpenState.Modify,
                _dtuInfoOc[index].DtuId,
                _dtuInfoOc[index].SimId,
                _dtuInfoOc[index].UserName,
                _dtuInfoOc[index].UserTel);
            bool? b = dc.ShowDialog();
            if (b != true)
                return;

            PutRequest(new Tuple<string, string>(
                Consts.MAN_MODIFY_DTU,
                dc.DtuId + "\t" + dc.SimId + "\t" + dc.UserName + "\t" + dc.UserTel));
        }

        private void DtuView_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            tcServiceConfig.SelectedIndex = MainWindow.TAB_DTU_INDEX;

            if (_dtuInfoOc.Count < 1)
            {
                MessageBox.Show("无DTU.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int index = dgDtu.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("无选中DTU.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DTUConfiguration dc = new DTUConfiguration(
                DTUConfiguration.OpenState.View,
                _dtuInfoOc[index].DtuId,
                _dtuInfoOc[index].SimId,
                _dtuInfoOc[index].UserName,
                _dtuInfoOc[index].UserTel);
            bool? b = dc.ShowDialog();
            if (b != true)
                return;
        }

        private void DtuUncontrol_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            tcServiceConfig.SelectedIndex = MainWindow.TAB_DTU_INDEX;

            if (_dtuInfoOc.Count < 1)
            {
                MessageBox.Show("无DTU.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int index = dgDtu.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("无选中DTU.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_dtuInfoOc[index].Online == false)
            {
                MessageBox.Show("DTU(" + _dtuInfoOc[index].DtuId + ")已经不在线.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_dtuInfoOc[index].Controller == null)
            {
                MessageBox.Show("DTU(" + _dtuInfoOc[index].DtuId + ")的控制已经被释放.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("确认释放DTU的控制?\nDTU ID : " + _dtuInfoOc[index].DtuId + "\nSIM ID : " + _dtuInfoOc[index].SimId,
                "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            PutRequest(new Tuple<string, string>(
                Consts.MAN_UNCTRL_DTU,
                "admin\t" + _dtuInfoOc[index].DtuId));
        }

        private void DtuDisconnect_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            tcServiceConfig.SelectedIndex = MainWindow.TAB_DTU_INDEX;

            if (_dtuInfoOc.Count < 1)
            {
                MessageBox.Show("无DTU.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int index = dgDtu.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("无选中DTU.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_dtuInfoOc[index].Online == false)
            {
                MessageBox.Show("DTU(" + _dtuInfoOc[index].DtuId + ")已经不在线.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("确认断开DTU的连接?\nDTU ID : " + _dtuInfoOc[index].DtuId + "\nSIM ID : " + _dtuInfoOc[index].SimId,
                "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            PutRequest(new Tuple<string, string>(
                Consts.MAN_KICK_DTU,
                _dtuInfoOc[index].DtuId));
        }

        private void DtuRemove_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            tcServiceConfig.SelectedIndex = MainWindow.TAB_DTU_INDEX;

            if (_dtuInfoOc.Count < 1)
            {
                MessageBox.Show("无DTU.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int index = dgDtu.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("无选中DTU.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("确认删除DTU?\nDTU ID : " + _dtuInfoOc[index].DtuId + "\nSIM ID : " + _dtuInfoOc[index].SimId,
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            PutRequest(new Tuple<string, string>(
                Consts.MAN_DELETE_DTU,
                _dtuInfoOc[index].DtuId));
        }

        private void Reconnect_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            IPAddress server = null;
            IPEndPoint iep = null;
            try
            {
                _manSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server = IPAddress.Parse(ServerIP);
                iep = new IPEndPoint(server, ServerPort);
                _manSocket.SendTimeout = Consts.MAN_TIMEOUT;
                _manSocket.ReceiveTimeout = Consts.MAN_TIMEOUT;
                _manSocket.Connect(iep);
                if (_manSocket.Connected)
                {
                    Task.Factory.StartNew(
                        () =>
                        {
                            ManTask(true);
                        }, _cts.Token);
                }
                else
                {
                    Helper.SafeCloseSocket(_manSocket);
                    _manSocket = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法重新连接 : " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Helper.SafeCloseSocket(_manSocket);
                _manSocket = null;
            }
        }

        private void SaveLog_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveLog();
        }

        private void ClearLog_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_logOc.Count > 0 && MessageBox.Show("需要保存日志吗?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                SaveLog();
            else
                SaveLog(false);
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
                        StreamWriter sw = new StreamWriter(folder + @"\COMWAY\ServiceConfiguration\log\" + sdt + ".log");
                        StringBuilder sb = new StringBuilder();
                        foreach (LogMessage lm in _logOc)
                        {
                            sb.Append(lm.IndexString
                                + "\t" + lm.MsgDateTime
                                + "\t" + lm.FlowType.ToString()
                                + "\t" + lm.StateType.ToString()
                                + "\t" + (string.IsNullOrWhiteSpace(lm.IPAddr) ? "(NA)" : lm.IPAddr)
                                + "\t" + lm.Message + "\n");
                        }
                        sw.Write(sb.ToString());
                        sw.Flush();
                        sw.Close();
                        sw.Dispose();

                        _logOc.Clear();
                        //_logDispOc.Clear();
                    }
                    catch (Exception ex)
                    {
                        _logOc.Clear();
                        //_logDispOc.Clear();

                        AddLog("保存日志出现错误 : " + ex.Message, "", state: LogMessage.State.Error);
                    }
                }
                else
                {
                    _logOc.Clear();
                    _logDispOc.Clear();
                }
            }
        }

        private void SaveConfig()
        {
            try
            {
                string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
                StreamWriter sw = new StreamWriter(folder + @"\COMWAY\ServiceConfiguration\config\manserv.cfg");
                sw.WriteLine(EncryptDecrypt.Encrypt(ServerIP));
                sw.WriteLine(EncryptDecrypt.Encrypt(ServerPort.ToString()));
                sw.WriteLine(EncryptDecrypt.Encrypt(UserName));
                sw.WriteLine(EncryptDecrypt.Encrypt(MaxLogCount.ToString()));
                sw.WriteLine(EncryptDecrypt.Encrypt(MaxLogDisplayCount.ToString()));
                sw.WriteLine(EncryptDecrypt.Encrypt(ServerWebPort.ToString()));
                sw.Close();
                sw.Dispose();
            }
            catch (Exception)
            {
            }
        }

        private void LocalConfig_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            LocalConfiguration lc = new LocalConfiguration(MaxLogCount, MaxLogDisplayCount);
            if (lc.ShowDialog() != true)
                return;

            MaxLogCount = lc.MaxLogCount;
            MaxLogDisplayCount = lc.MaxLogDisplayCount;
        }

        private void LogDelete_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<string> sloc = new ObservableCollection<string>();
            lock (_serverLogLock)
            {
                foreach (Tuple<string, string, string> si in _serverLogOc)
                {
                    bool find = false;
                    foreach (string sloci in sloc)
                    {
                        if (string.Compare(sloci.Trim(), si.Item1.Trim(), true) == 0)
                        {
                            find = true;
                            break;
                        }
                    }
                    if (find == false)
                        sloc.Add(si.Item1.Trim());

                }
            }

            DeleteServerLog dsl = new DeleteServerLog(sloc);
            bool? b = dsl.ShowDialog();
            if (b == true)
            {
                string s = "";
                if (string.IsNullOrWhiteSpace(dsl.DeleteUser) == false && string.IsNullOrWhiteSpace(dsl.DeleteDate) == false)
                {
                    s = dsl.DeleteUser + "\t" + dsl.DeleteDate;
                    PutRequest(new Tuple<string,string>(Consts.MAN_DEL_LOG_USER_DATE, s));
                }
                else if (string.IsNullOrWhiteSpace(dsl.DeleteUser) == true && string.IsNullOrWhiteSpace(dsl.DeleteDate) == false)
                {
                    s = dsl.DeleteDate;
                    PutRequest(new Tuple<string,string>(Consts.MAN_DEL_LOG_DATE, s));
                }
                else if (string.IsNullOrWhiteSpace(dsl.DeleteUser) == false && string.IsNullOrWhiteSpace(dsl.DeleteDate) == true)
                {
                    s = dsl.DeleteUser;
                    PutRequest(new Tuple<string,string>(Consts.MAN_DEL_LOG_USER, s));
                }
            }
        }

        private void LogView_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<string> sloc = new ObservableCollection<string>();
            lock (_serverLogLock)
            {
                foreach (Tuple<string, string, string> si in _serverLogOc)
                {
                    bool find = false;
                    foreach (string sloci in sloc)
                    {
                        if (string.Compare(sloci.Trim(), si.Item1.Trim(), true) == 0)
                        {
                            find = true;
                            break;
                        }
                    }
                    if (find == false)
                        sloc.Add(si.Item1.Trim());

                }
            }

            DeleteServerLog dsl = new DeleteServerLog(sloc, DeleteServerLog.ServerLogType.Select);
            bool? b = dsl.ShowDialog();
            if (b == true)
            {
                ObservableCollection<Tuple<string, string, string>> slfoc = new ObservableCollection<Tuple<string, string, string>>();
                if (string.IsNullOrWhiteSpace(dsl.DeleteUser) == false && string.IsNullOrWhiteSpace(dsl.DeleteDate) == false)
                {
                    DateTime dt;
                    if (DateTime.TryParse(dsl.DeleteDate, out dt) == true)
                    {
                        foreach (Tuple<string, string, string> si in _serverLogOc)
                        {
                            if (string.Compare(dsl.DeleteUser.Trim(), "all", true) == 0 ||
                                string.Compare(dsl.DeleteUser.Trim(), si.Item1.Trim(), true) == 0)
                            {
                                DateTime dtf;
                                if (DateTime.TryParse(si.Item2.Trim(), out dtf) == true)
                                {
                                    if (DateTime.Compare(dt, dtf) <= 0)
                                        slfoc.Add(si);
                                }
                            }
                        }
                    }
                }
                else if (string.IsNullOrWhiteSpace(dsl.DeleteUser) == true && string.IsNullOrWhiteSpace(dsl.DeleteDate) == false)
                {
                    DateTime dt;
                    if (DateTime.TryParse(dsl.DeleteDate, out dt) == true)
                    {
                        foreach (Tuple<string, string, string> si in _serverLogOc)
                        {
                            DateTime dtf;
                            if (DateTime.TryParse(si.Item2.Trim(), out dtf) == true)
                            {
                                if (DateTime.Compare(dt, dtf) <= 0)
                                    slfoc.Add(si);
                            }
                        }
                    }
                }
                else if (string.IsNullOrWhiteSpace(dsl.DeleteUser) == false && string.IsNullOrWhiteSpace(dsl.DeleteDate) == true)
                {
                    foreach (Tuple<string, string, string> si in _serverLogOc)
                    {
                        if (string.Compare(dsl.DeleteUser.Trim(), "all", true) == 0 ||
                            string.Compare(dsl.DeleteUser.Trim(), si.Item1.Trim(), true) == 0)
                            slfoc.Add(si);
                    }
                }

                ViewUserMsgLog vuml = new ViewUserMsgLog(ServerIP, ServerWebPort, slfoc);
                vuml.ShowDialog();
            }
        }
    }
}
