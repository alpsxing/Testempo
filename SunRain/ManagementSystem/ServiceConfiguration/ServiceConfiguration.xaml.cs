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

        private bool _bInNormalClose = false;

        private CancellationTokenSource _cts = null;
        private Task _logTask = null;
        private int _logIndex = 1;
        private Queue<LogMessage> _logQueue = new Queue<LogMessage>();
        private ObservableCollection<LogMessage> _logOc = new ObservableCollection<LogMessage>();
        private object _logLock = new object();
        //private Timer _pulseTimer = null;
        //private Timer _serviceTimer = null;
        private Timer _userTimer = null;
        private Timer _dtuTimer = null;

        private object _reqLock = new object();
        private object _dtuLock = new object();

        private int _selectedUserIndex = -1;
        private int _selectedDtuIndex = -1;

        private Socket _manSocket = null;

        #endregion

        #region Properties

        private Queue<Tuple<string, string>> _requestQueue = new Queue<Tuple<string,string>>();
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
                Title = "Service Configuration - " + _userName + " ( " + UserPermissionDislay + " ) - " + ReadyString;
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
                Title = "Service Configuration - " + _userName + " ( " + UserPermissionDislay + " ) - " + ReadyString;
                NotifyPropertyChanged("UserPermission");
                NotifyPropertyChanged("UserPermissionDislay");
            }
        }

        public string UserPermissionDislay
        {
            get
            {
                if (UserPermission == "0")
                    return "Super User";
                else if (UserPermission == "1")
                    return "Manage User";
                else
                    return "Common User";
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

        private string _readyString = "Disconnected";
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

        public MainWindow(Socket soc, string username, string password, string userPerm, string serverip, int serverport)
		{
			InitializeComponent();

            DataContext = this;

            _manSocket = soc;

            UserName = username;
            Password = password;
            UserPermission = userPerm;
            ServerIP = serverip;
            ServerPort = serverport;

            dgUser.DataContext = _userInfoOc;
            dgLog.DataContext = _logOc;
            dgDtu.DataContext = _dtuInfoOc;
            _logOc.CollectionChanged += new NotifyCollectionChangedEventHandler(LogOc_CollectionChanged);

            Connected = true;
        }

        #region Window Exit

        private void Window_Exit(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure to quit \"Service Conciguration\"?", "Comfirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _bInNormalClose = true;

            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_bInNormalClose == false)
            {
                if (MessageBox.Show("Are you sure to quit \"Service Conciguration\"?", "Comfirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    e.Cancel = true;
            }

            if (e.Cancel == false)
                PutRequest(new Tuple<string, string>(Consts.MAN_LOGOUT, ""));

            base.OnClosing(e);
        }

        #endregion

        private void LogOc_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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

        }

        private void Window_Load(object sender, RoutedEventArgs e)
        {
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
                ReadyString = "Connected";
            }, null);

            lock (_reqLock)
            {
                ReqQueue.Clear();
            }

            if(reLogin == true)
                PutRequest(new Tuple<string,string>(Consts.MAN_LOGIN, UserName + "\t" + Password));

            _userTimer = new Timer(new TimerCallback(UserTimerCallBack), null,
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
                                AddLog("Unknown request : " + req.Item1 + req.Item2, state: LogMessage.State.Fail, flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_ADD_DTU:
                                AddLog("Request : add DTU", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_GET_ALL_DTU:
                                AddLog("Request : gat all DTUs", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_DELETE_DTU:
                                AddLog("Request : delete DTU", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_MODIFY_DTU:
                                AddLog("Request : modify DTU", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_GET_ALL_USER:
                                AddLog("Request : get all users", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_ADD_USER:
                                AddLog("Request : add user", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_DELETE_USER:
                                AddLog("Request : delete user", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_LOGIN:
                                AddLog("Request : login", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_LOGOUT:
                                AddLog("Request : logout", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_KICK_DTU:
                                AddLog("Request : disconnect DTU", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_KICK_USER:
                                AddLog("Request : disconnect user", flow: LogMessage.Flow.Request);
                                break;
                            case Consts.MAN_UNCTRL_DTU:
                                AddLog("Request : release DTU", flow: LogMessage.Flow.Request);
                                break;
                        }
                        _manSocket.Send(Encoding.ASCII.GetBytes(req.Item1 + req.Item2));
                        byte[] bytes = new byte[Consts.SOCKET_RECEIVING_BUFFER_LENGTH_MAN];
                        int length = _manSocket.Receive(bytes);
                        if (length < 1)
                        {
                            AddLog("Connection to server is broken.", state: LogMessage.State.Error, flow: LogMessage.Flow.Response);
                            _userTimer.Change(Timeout.Infinite, Consts.MAN_TASK_TIMER_INTERVAL);
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
                AddLog("Exception when communicate with server : " + ex.Message, state: LogMessage.State.Error);
            }
            Dispatcher.Invoke((ThreadStart)delegate
            {
                ReadyString = "Disconnected";
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
            catch (Exception ex)
            {
                AddLog("Exception when disposing DTU timer : " + ex.Message, state: LogMessage.State.Error);
            }
            try
            {
                _userTimer.Dispose();
            }
            catch (Exception ex)
            {
                AddLog("Exception when disposing user timer : " + ex.Message, state: LogMessage.State.Error);
            }
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
                AddLog("Unknown response : " + System.Text.Encoding.ASCII.GetString(bytes, 0, len), state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                return;
            }

            if (reLogin == true)
            {
                if (data.Item1 != Consts.MAN_LOGIN_OK && data.Item1 != Consts.MAN_LOGIN_ERR)
                {
                    AddLog("Unknown response : " + data.Item1 + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    Helper.SafeCloseSocket(_manSocket);
                    _manSocket = null;
                    return;
                }
                if (data.Item1 == Consts.MAN_LOGIN_OK)
                {
                    AddLog("Reconnect ok.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    Connected = true;
                    return;
                }
                else
                {
                    AddLog("Reconnect error : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    Helper.SafeCloseSocket(_manSocket);
                    _manSocket = null;
                    Connected = false;
                    return;
                }
            }

            switch (data.Item1)
            {
                default:
                    AddLog("Unknown response : " + data.Item1 + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_INVALID_REQUEST:
                    AddLog("Invalid request : " + data.Item1 + data.Item3, state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_GET_ALL_USER_OK:
                    AddLog("Get all user ok.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    ProcessAllUser(data.Item3);
                    break;
                case Consts.MAN_GET_ALL_DTU_OK:
                    AddLog("Get all DTUs ok.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    ProcessAllDTU(data.Item3);
                    break;
                case Consts.MAN_GET_ALL_USER_ERR:
                    AddLog("Get all users error : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_ADD_USER_OK:
                    AddLog("Add user ok.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_USER, ""));
                    break;
                case Consts.MAN_ADD_USER_ERR:
                    AddLog("Add user error : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_DELETE_USER_OK:
                    AddLog("Delete user ok.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_USER, ""));
                    break;
                case Consts.MAN_DELETE_USER_ERR:
                    AddLog("Delete user error : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_ADD_DTU_OK:
                    AddLog("Add DTU ok.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_DTU, ""));
                    break;
                case Consts.MAN_ADD_DTU_ERR:
                    AddLog("Add DTU error : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_DELETE_DTU_OK:
                    AddLog("Delete DTU ok.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_DTU, ""));
                    break;
                case Consts.MAN_DELETE_DTU_ERR:
                    AddLog("Delete DTU error : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_MODIFY_DTU_OK:
                    AddLog("Modify DTU ok.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_DTU, ""));
                    break;
                case Consts.MAN_MODIFY_DTU_ERR:
                    AddLog("Modify DTU error : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_KICK_DTU_OK:
                    AddLog("Disconnect DTU ok.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_DTU, ""));
                    break;
                case Consts.MAN_KICK_DTU_ERR:
                    AddLog("Disconnect DTU error : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_KICK_USER_OK:
                    AddLog("Disconnect user ok.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_USER, ""));
                    break;
                case Consts.MAN_KICK_USER_ERR:
                    AddLog("Disconnect user error : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_UNCTRL_DTU_OK:
                    AddLog("Release DTU ok.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_DTU, ""));
                    break;
                case Consts.MAN_UNCTRL_DTU_ERR:
                    AddLog("Release DTU error : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
                    break;
                case Consts.MAN_LOGIN_OK:
                    AddLog("Login ok.", state: LogMessage.State.OK, flow: LogMessage.Flow.Response);
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_USER, ""));
                    PutRequest(new Tuple<string, string>(Consts.MAN_GET_ALL_DTU, ""));
                    break;
                case Consts.MAN_LOGIN_ERR:
                    AddLog("Login error : " + data.Item3, state: LogMessage.State.Fail, flow: LogMessage.Flow.Response);
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

                            while (_logOc.Count > MaxLogDisplayCount)
                                _logOc.RemoveAt(0);

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
                MessageBox.Show("Common user has no permission to add a new user.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("Common user has no permission to disconnect a user.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            tcServiceConfig.SelectedIndex = MainWindow.TAB_USER_INDEX;

            if (_userInfoOc.Count < 1)
            {
                MessageBox.Show("No user can be disconnected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int index = dgUser.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("No selected user.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            UserInfo ui = _userInfoOc[index];

            if (string.Compare(UserName.Trim(), ui.UserName.Trim(), true) == 0)
            {
                MessageBox.Show("\"" + ui.UserName + "\" is not allowed to disconnect itself.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (ui.Online == false)
            {
                MessageBox.Show("\"" + ui.UserName + "\" is offline.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("Are you sure to disconnect \"" + ui.UserName + "\".", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            PutRequest(new Tuple<string, string>(Consts.MAN_KICK_USER, ui.UserName));
        }

        private void UserRemove_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (UserPermission != "0" && UserPermission != "1")
            {
                MessageBox.Show("Common user has no permission to delete a user.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            tcServiceConfig.SelectedIndex = MainWindow.TAB_USER_INDEX;

            if (_userInfoOc.Count < 1)
            {
                MessageBox.Show("No user can be deleted.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int index = dgUser.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("No selected user.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            UserInfo ui = _userInfoOc[index];

            if (string.Compare(UserName.Trim(), ui.UserName.Trim(), true) == 0)
            {
                MessageBox.Show("\"" + ui.UserName + "\" is not allowed to delete itself.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.Compare(ui.UserName, "admin", true) == 0)
            {
                MessageBox.Show("\"admin\" cannot be deleted.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (UserPermission == "1" && (ui.Permission == "0" || ui.Permission == "1"))
            {
                MessageBox.Show("Super user is required to delete \"" + ui.UserName + "\".", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("Are you sure to delete \"" + ui.UserName + "\".", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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
                MessageBox.Show("No DTU can be deleted.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int index = dgDtu.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("No selected DTU.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("No DTU can be deleted.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int index = dgDtu.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("No selected DTU.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("No DTU can be released.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int index = dgDtu.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("No selected DTU.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_dtuInfoOc[index].Online == false)
            {
                MessageBox.Show("DTU (" + _dtuInfoOc[index].DtuId + ") is offline.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_dtuInfoOc[index].Controller == null)
            {
                MessageBox.Show("DTU (" + _dtuInfoOc[index].DtuId + ") is already released.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("Are you sure to release DTU ?\nDTU ID : " + _dtuInfoOc[index].DtuId + "\nSIM ID : " + _dtuInfoOc[index].SimId,
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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
                MessageBox.Show("No DTU can be disconnected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int index = dgDtu.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("No selected DTU.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_dtuInfoOc[index].Online == false)
            {
                MessageBox.Show("DTU (" + _dtuInfoOc[index].DtuId + ") is offline.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("Are you sure to disconnected DTU ?\nDTU ID : " + _dtuInfoOc[index].DtuId + "\nSIM ID : " + _dtuInfoOc[index].SimId,
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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
                MessageBox.Show("No DTU can be deleted.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            int index = dgDtu.SelectedIndex;
            if (index < 0)
            {
                MessageBox.Show("No selected DTU.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("Are you sure to delete DTU ?\nDTU ID : " + _dtuInfoOc[index].DtuId + "\nSIM ID : " + _dtuInfoOc[index].SimId, 
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
                MessageBox.Show("Cannot reconnect server : " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Helper.SafeCloseSocket(_manSocket);
                _manSocket = null;
            }
        }
    }
}
