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
using Microsoft.Win32;

using InformationTransferLibrary;

using System.IO.Ports;

namespace TerminalConfiguration
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

        #region Variables

        private bool _bInNormalClose = false;

        private Queue<DTUConfigLogMessage> _dtuLMQueue = new Queue<DTUConfigLogMessage>();
        private ObservableCollection<DTUConfigLogMessage> _dtuLMOc = new ObservableCollection<DTUConfigLogMessage>();
        private ObservableCollection<DTUCommand> _dtuCmdOc = new ObservableCollection<DTUCommand>();

        private ObservableCollection<string> _localPortOc = new ObservableCollection<string>();
        private ObservableCollection<string> _localBundOc = new ObservableCollection<string>();
        private ObservableCollection<string> _localParityOc = new ObservableCollection<string>();

        //private ObservableCollection<string> _dtuBundOc = new ObservableCollection<string>();
        private ObservableCollection<string> _dtuDataOc = new ObservableCollection<string>();
        //private ObservableCollection<string> _dtuCheckOc = new ObservableCollection<string>();
        private ObservableCollection<string> _dtuStopOc = new ObservableCollection<string>();

        private bool _inInit = false;

        private object _logLock = new object();

        private CancellationTokenSource _cts = null;
        private Task _logTask = null;

        private SerialPort _sPort = null;

        //private ObservableCollection<string> _cmdList = new ObservableCollection<string>();

        private object _saveCfgLock = new object();

        private Timer _timerPBar = null;

        private CancellationTokenSource _ctsIO = new CancellationTokenSource();

        #endregion

        #region Properties

        private Visibility _columnIsVisible = Visibility.Collapsed;
        public Visibility ColumnIsVisible
        {
            get
            {
                return _columnIsVisible;
            }
            set
            {
                _columnIsVisible = value;
                int count = dgCmd.Columns.Count;
                for (int i = 2; i < count - 2; i++)
                {
                    dgCmd.Columns[i].Visibility = _columnIsVisible;
                }
                NotifyPropertyChanged("ColumnIsVisible");
            }
        }

        private bool _isModified = false;
        public bool IsModified
        {
            get
            {
                return _isModified;
            }
            set
            {
                if (_inInit == false)
                {
                    _isModified = value;
                    SetWindowTitle();
                }
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

        private string _cfgPath = "";
        public string CFGPath
        {
            get
            {
                return _cfgPath;
            }
            set
            {
                _cfgPath = value;
                NotifyPropertyChanged("CFGPath");
            }
        }

        private string _cfgFile = "";
        public string CFGFile
        {
            get
            {
                return _cfgFile;
            }
            set
            {
                _cfgFile = value;

                SetWindowTitle();
                NotifyPropertyChanged("CFGFile");
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
                NotifyPropertyChanged("InputOK");
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

        private bool? _powerCycleNeeded = true;
        public bool? PowerCycleNeeded
        {
            get
            {
                return _powerCycleNeeded;
            }
            set
            {
                _powerCycleNeeded = value;
                PowerCycled = false;
                NotifyPropertyChanged("PowerCycleNeeded");
            }
        }

        private bool _powerCycled = false;
        public bool PowerCycled
        {
            get
            {
                return _powerCycled;
            }
            set
            {
                _powerCycled = value;
                NotifyPropertyChanged("PowerCycled");
            }
        }

        private string _serverIP = "";
        public string ServerIP
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_serverIP))
                    return "";
                else
                    return _serverIP;
            }
            set
            {
                _serverIP = value;
                if (string.IsNullOrWhiteSpace(_serverIP))
                    _serverIP = "";
                else
                {
                    ServerIPOK = true;
                    ServerIPFG = Brushes.Black;
                }
                IsModified = true;                
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
            }
        }

        private SolidColorBrush _serverIPFG = Brushes.Red;
        public SolidColorBrush ServerIPFG
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
                    _serverPort = Consts.TERM_PORT;
                }
                else if (value > Consts.MAX_PORT_NUMBER)
                {
                    ServerPortFG = Brushes.Red;
                    ServerPortOK = false;
                    _serverPort = Consts.MAX_PORT_NUMBER;
                }
                else
                {
                    ServerPortFG = Brushes.Black;
                    ServerPortOK = true;
                    _serverPort = value;
                }

                IsModified = true;
 
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
                    else if (i > Consts.MAX_PORT_NUMBER)
                    {
                        ServerPortFG = Brushes.Red;
                        ServerPortOK = false;
                        _serverPort = Consts.MAX_PORT_NUMBER;
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

                IsModified = true;
                
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
                NotifyPropertyChanged("InputOK");
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

        private string _registerPackage = "";
        public string RegisterPackage
        {
            get
            {
                return _registerPackage;
            }
            set
            {
                _registerPackage = value;
                if (string.IsNullOrWhiteSpace(_registerPackage))
                {
                    RegisterPackageOK = false;
                    RegisterPackageFG = Brushes.Red;
                }
                else
                {
                    _registerPackage = _registerPackage.Trim();
                    if (_registerPackage.Length != Consts.DTU_REGISTER_CONTENT_LENGTH)
                    {
                        RegisterPackageOK = false;
                        RegisterPackageFG = Brushes.Red;
                    }
                    else
                    {
                        if (_registerPackage.IndexOf("?") > -1)
                        {
                            RegisterPackageOK = false;
                            RegisterPackageFG = Brushes.Red;
                        }
                        else
                        {
                            RegisterPackageOK = true;
                            RegisterPackageFG = Brushes.Black;
                        }
                    }
                }

                IsModified = true;

                NotifyPropertyChanged("RegisterPackage");
                NotifyPropertyChanged("RegisterPackageHex");
            }
        }

        public string RegisterPackageHEX
        {
            get
            {
                char[] ca = RegisterPackage.ToArray();
                string ret = "";
                foreach (char ci in ca)
                {
                    ret = ret + ((int)ci).ToString("X");
                }
                return ret;
            }
            set
            {
                string s = value;
                if (s.Length != Consts.DTU_REGISTER_CONTENT_LENGTH * 2)
                {
                    RegisterPackageOK = false;
                    RegisterPackageFG = Brushes.Red;
                    _registerPackage = "???????";
                }
                else
                {
                    string sHex = "";
                    string sRet = "";
                    string sSrc = s;
                    while (sSrc.Length > 0)
                    {
                        sHex = sSrc.Substring(0, 2);
                        sSrc = sSrc.Substring(2);
                        sRet = sRet + CalcCharFromHEX(sHex);
                    }
                    _registerPackage = sRet;
                }
                if (_registerPackage.IndexOf("?") > -1)
                {
                    RegisterPackageOK = false;
                    RegisterPackageFG = Brushes.Red;
                }
                else
                {
                    RegisterPackageOK = true;
                    RegisterPackageFG = Brushes.Black;
                }

                IsModified = true;

                NotifyPropertyChanged("RegisterPackage");
                NotifyPropertyChanged("RegisterPackageHex");
            }
        }

        private bool _registerPackageOK = false;
        public bool RegisterPackageOK
        {
            get
            {
                return _registerPackageOK;
            }
            set
            {
                _registerPackageOK = value;
                NotifyPropertyChanged("RegisterPackageOK");
                NotifyPropertyChanged("InputOK");
            }
        }

        private Brush _registerPackageFG = Brushes.Red;
        public Brush RegisterPackageFG
        {
            get
            {
                return _registerPackageFG;
            }
            set
            {
                _registerPackageFG = value;
                NotifyPropertyChanged("RegisterPackageFG");
            }
        }

        private string _simID = "18611600000";
        public string SIMID
        {
            get
            {
                return _simID;
            }
            set
            {
                _simID = value;
                if (Helper.CheckValidChar(_simID, Helper.CheckMethod.Tel) == true)
                {
                    SIMIDOK = true;
                    SIMIDFG = Brushes.Black;
                }
                else
                {
                    SIMIDOK = false;
                    SIMIDFG = Brushes.Red;
                }

                IsModified = true;

                NotifyPropertyChanged("SIMID");
            }
        }

        private bool _simIDOK = true;
        public bool SIMIDOK
        {
            get
            {
                return _simIDOK;
            }
            set
            {
                _simIDOK = value;
                NotifyPropertyChanged("SIMIDOK");
            }
        }

        private SolidColorBrush _simIDFG = Brushes.Black;
        public SolidColorBrush SIMIDFG
        {
            get
            {
                return _simIDFG;
            }
            set
            {
                _simIDFG = value;
                NotifyPropertyChanged("SIMIDFG");
            }
        }

        public bool InputOK
        {
            get
            {
                return ServerIPOK && ServerPortOK && RegisterPackageOK && NotInRun;
            }
        }

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

        #endregion

        public MainWindow()
        {
            _inInit = true;

            InitializeComponent();

            string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            if (!Directory.Exists(folder + @"\COMWAY"))
                Directory.CreateDirectory(folder + @"\COMWAY");
            folder = folder + @"\COMWAY";
            if (!Directory.Exists(folder + @"\DTUConfiguration"))
                Directory.CreateDirectory(folder + @"\DTUConfiguration");

            DataContext = this;

            dgCmd.DataContext = _dtuCmdOc;
            dgLog.DataContext = _dtuLMOc;
            _dtuLMOc.CollectionChanged += new NotifyCollectionChangedEventHandler(DTULogMessageOc_CollectionChanged);

            cboxLocalPort.ItemsSource = _localPortOc;
            cboxLocalBund.ItemsSource = _localBundOc;
            cboxLocalParity.ItemsSource = _localParityOc;

            cboxDtuBund.ItemsSource = _localBundOc;// _dtuBundOc;
            cboxDtuData.ItemsSource = _dtuDataOc;
            cboxDtuParity.ItemsSource = _localParityOc;// _dtuCheckOc;
            cboxDtuStop.ItemsSource = _dtuStopOc;

            _cts = new CancellationTokenSource();
            _logTask = Task.Factory.StartNew(
                () =>
                {
                    DisplayLog();
                }, _cts.Token
            );
        }

        private void Window_Load(object sender, RoutedEventArgs e)
        {
            if (EncryptDecryptLibrary.EncryptDecryptLibrary.CheckRunOrNot() == false)
            {
                MessageBox.Show("无有效许可.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Environment.Exit(0);
            }

            foreach (string s in SerialPort.GetPortNames())
            {
                _localPortOc.Add(s);
            }
            if (_localPortOc.Count > 0)
                cboxLocalPort.SelectedIndex = 0;
            else
                LogMessage("无效端口.", DTUConfigLogMessage.MessageState.Error);

            int i = 300;
            while (i <= 115200)
            {
                _localBundOc.Add(i.ToString());
                i = i * 2;
                if (i == 19200)
                {
                    i = 28800;
                }
                else if (i == 28800)
                {
                    i = 38400;
                }
                else if (i == 38400)
                {
                    i = 57600;
                }
            }
            cboxLocalBund.SelectedIndex = _localBundOc.Count - 1;

            _localParityOc.Add(Parity.None.ToString());
            _localParityOc.Add(Parity.Odd.ToString());
            _localParityOc.Add(Parity.Even.ToString());
            _localParityOc.Add(Parity.Space.ToString());
            _localParityOc.Add(Parity.Mark.ToString());
            cboxLocalParity.SelectedIndex = 0;

            cboxDtuBund.SelectedIndex = _localBundOc.Count - 1;

            _dtuDataOc.Add("5");
            _dtuDataOc.Add("6");
            _dtuDataOc.Add("7");
            _dtuDataOc.Add("8");
            cboxDtuData.SelectedIndex = _dtuDataOc.Count - 1;

            cboxDtuParity.SelectedIndex = 0;

            _dtuStopOc.Add("1");
            _dtuStopOc.Add("2");
            _dtuStopOc.Add("1.5");
            cboxDtuStop.SelectedIndex = 0;

            OpenSerialPort();

            SetWindowTitle();

            IsModified = false;

            ColumnIsVisible = Visibility.Collapsed;

            _timerPBar = new Timer(new TimerCallback(PBarTimerCallBackHandler), null, Timeout.Infinite, 1000);

            _inInit = false;
        }

        private void SetWindowTitle()
        {
            if (IsModified == true)
            {
                if (string.IsNullOrWhiteSpace(CFGFile))
                    Title = "DTU配置 - (NA) *";
                else
                    Title = "DTU配置 - " + CFGFile + " *";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(CFGFile))
                    Title = "DTU配置 - (NA)";
                else
                    Title = "DTU配置 - " + CFGFile;
            }
        }

        private void DTULogMessageOc_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (LogAutoScrolling == false)
                return;

            Dispatcher.Invoke((ThreadStart)delegate
            {
                if (_dtuLMOc.Count < 1)
                    return;
                var border = VisualTreeHelper.GetChild(dgLog, 0) as Decorator;
                if (border != null)
                {
                    var scroll = border.Child as ScrollViewer;
                    if (scroll != null) scroll.ScrollToEnd();
                }
            }, null);
        }

        #region Window Exit

        private void Window_Exit(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure to quit \"DTU Configuration\"?", "Comfirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _bInNormalClose = true;

            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_bInNormalClose == false)
            {
                if (MessageBox.Show("Are you sure to quit \"DTU Configuration\"?", "Comfirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    e.Cancel = true;
            }

            if (e.Cancel == false)
            {
                if (IsModified == true)
                {
                    bool doSave = false;
                    if (MessageBox.Show("Do you want to save the current configuration first?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        doSave = true;

                    if (doSave == true)
                        SaveConfig_MenuItem_Click(null, null);
                }

                CloseSerialPort();
                _cts.Cancel();
            }

            base.OnClosing(e);
        }

        #endregion

        private void About_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            About ab = new About("DTU配置", "Copyright @ 2012");
            ab.ShowDialog();
        }

        private void InitiateDTU_MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LocalConfig_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_inInit == true)
                return;

            IsModified = true;

            //if (MessageBox.Show("Do you want to refresh the serial port?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            //    return;

            CloseSerialPort();
            OpenSerialPort();
        }

        private void OpenSerialPort()
        {
            if (_localPortOc.Count < 1)
                return;

            try
            {
                _sPort = new SerialPort(
                    (string)_localPortOc[cboxLocalPort.SelectedIndex],
                    int.Parse((string)_localBundOc[cboxLocalBund.SelectedIndex]),
                    (Parity)Enum.Parse(typeof(Parity), (string)_localParityOc[cboxLocalParity.SelectedIndex]));
                _sPort.WriteTimeout = Consts.DTU_CFG_TIMEOUT;
                _sPort.ReadTimeout = Consts.DTU_CFG_TIMEOUT;
                _sPort.Open();
                if (_sPort.IsOpen)
                    LogMessage((string)_localPortOc[cboxLocalPort.SelectedIndex] + "被成功打开.", DTUConfigLogMessage.MessageState.Infomation);
                else
                    LogMessage("不能成功打开" + (string)_localPortOc[cboxLocalPort.SelectedIndex] + ".", DTUConfigLogMessage.MessageState.Fail);
            }
            catch (Exception ex)
            {
                LogMessage("不能成功打开" + (string)_localPortOc[cboxLocalPort.SelectedIndex] + " : " + ex.Message, DTUConfigLogMessage.MessageState.Error);
            }
        }

        private void CloseSerialPort()
        {
            if (_localPortOc.Count < 1)
                return;
            
            try
            {
                _sPort.Close();
                LogMessage((string)_localPortOc[cboxLocalPort.SelectedIndex] + " is closed.", DTUConfigLogMessage.MessageState.Infomation);
            }
            catch (Exception ex)
            {
                LogMessage("不能成功关闭" + (string)_localPortOc[cboxLocalPort.SelectedIndex] + " : " + ex.Message, DTUConfigLogMessage.MessageState.Error);
            }

            try
            {
                _sPort.Dispose();
                LogMessage((string)_localPortOc[cboxLocalPort.SelectedIndex] + "被成功清除.", DTUConfigLogMessage.MessageState.Infomation);
            }
            catch (Exception ex)
            {
                LogMessage("不能成功清除" + (string)_localPortOc[cboxLocalPort.SelectedIndex] + " : " + ex.Message, DTUConfigLogMessage.MessageState.Error);
            }

            _sPort = null;
        }

        private void LogMessage(string msg, DTUConfigLogMessage.MessageState state = DTUConfigLogMessage.MessageState.None,
            DTUConfigLogMessage.MessageFlow flow = DTUConfigLogMessage.MessageFlow.None)
        {
            Dispatcher.Invoke((ThreadStart)delegate
            {
                _dtuLMQueue.Enqueue(new DTUConfigLogMessage()
                {
                    State = state,
                    Flow = flow,
                    Message = msg
                });
            }, null);
        }

        private void LogMessageToDTU(string msg, DTUConfigLogMessage.MessageState state = DTUConfigLogMessage.MessageState.None)
        {
            LogMessage(msg, state, DTUConfigLogMessage.MessageFlow.ToDTU);
        }

        private void LogMessageFromDTU(string msg, DTUConfigLogMessage.MessageState state = DTUConfigLogMessage.MessageState.None)
        {
            LogMessage(msg, state, DTUConfigLogMessage.MessageFlow.FromDTU);
        }

        private void DisplayLog()
        {
            while (_cts.Token.IsCancellationRequested == false)
            {
                lock (_logLock)
                {
                    if (_dtuLMQueue.Count > 0)
                    {
                        Dispatcher.Invoke((ThreadStart)delegate
                        {
                            DTUConfigLogMessage lm = _dtuLMQueue.Dequeue();
                            _dtuLMOc.Add(lm);
                        }, null);
                    }
                }

                Thread.Sleep(Consts.TASK_THREAD_SLEEP_INTERVAL);
            }
        }

        private void OpenConfig_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsModified == true)
            {
                bool doSave = false;
                if (MessageBox.Show("需要保存当前的配置吗?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    doSave = true;

                if (doSave == true)
                    SaveConfig_MenuItem_Click(sender, e);
            }

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;
            ofd.Filter = "Config (.cfg)|*.cfg";
            string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            ofd.InitialDirectory = folder + @"\COMWAY\DTUConfiguration";
            ofd.Title = "Select a config";
            bool? bv = ofd.ShowDialog();
            if (bv != true)
                return;

            string sv = ofd.FileName;
            int iv = sv.LastIndexOf(@"\");
            CFGPath = sv.Substring(0, iv);
            CFGFile = sv.Substring(iv + 1);
            LoadConfig(CFGPath + @"\" + CFGFile);

            //ComposeCommandList();

            IsModified = false;
        }

        private void SaveConfig_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CFGFile))
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName = "Term"; // Default file name 
                sfd.DefaultExt = ".cfg"; // Default file extension 
                sfd.Filter = "Config (.cfg)|*.cfg"; // Filter files by extension 
                sfd.AddExtension = true;
                sfd.OverwritePrompt = true;
                sfd.CheckPathExists = true;
                sfd.Title = "Save Configuration As...";
                string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
                sfd.InitialDirectory = folder + @"\COMWAY\DTUConfiguration";
                bool? b = sfd.ShowDialog();
                if (b != true)
                    return;
                string s = sfd.FileName;
                int i = s.LastIndexOf(@"\");
                CFGPath = s.Substring(0, i);
                CFGFile = s.Substring(i + 1);
            }
            SaveConfig(CFGPath + @"\" + CFGFile);

            IsModified = false;
        }

        private void SaveConfigAs_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "Term"; // Default file name 
            sfd.DefaultExt = ".cfg"; // Default file extension 
            sfd.Filter = "Config (.cfg)|*.cfg"; // Filter files by extension 
            sfd.AddExtension = true;
            sfd.OverwritePrompt = true;
            sfd.CheckPathExists = true;
            sfd.Title = "Save Configuration As...";
            string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            sfd.InitialDirectory = folder + @"\COMWAY\DTUConfiguration";
            bool? b = sfd.ShowDialog();
            if (b != true)
                return;
            string s = sfd.FileName;
            int i = s.LastIndexOf(@"\");
            CFGPath = s.Substring(0, i);
            CFGFile = s.Substring(i + 1);

            SaveConfig(CFGPath + @"\" + CFGFile);

            IsModified = false;
        }

        private void AddNewCommand(string s, string ret = "", bool match = false, bool trim = false,
            DTUCommand.CommandQueryDelegate beforeCmdSendHandler = null,
            DTUCommand.CommandQueryDelegate afterCmdSendHandler = null,
            string ret2 = "",
            bool wait4Resp = true)
        {
            if (s == null)
                return;
            _dtuCmdOc.Add(new DTUCommand()
            {
                Command = s,
                IsCmd = true,
                RCompare = ret,
                MustMatch = match,
                DoTrim = trim,
                BeforeCommandSendDelegateHandler = beforeCmdSendHandler,
                AfterCommandSendDelegateHandler = afterCmdSendHandler,
                RCompare2 = ret2
            });
        }

        private void AddNewQuery(string s, string comp = "", bool trim = false, bool needComp = true)
        {
            if (s == null)
                return;
            _dtuCmdOc.Add(new DTUCommand()
            {
                Command = s,
                IsCmd = false,
                Compare = comp,
                NeedCompareResponse = needComp,
                DoTrim = trim
            });
        }

        private void ComposeCommandList()
        {
            if (PowerCycleNeeded == true || (PowerCycleNeeded == false && PowerCycled == false))
            {
                AddNewCommand("AT^VERS\n", "GPRS DTU", true, true,
                    new DTUCommand.CommandQueryDelegate(
                    () =>
                    {
                        Dispatcher.Invoke((ThreadStart)delegate
                        {
                            MessageBox.Show(this, "请关闭DTU然后按确认.", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }, null);
                    }),
                    new DTUCommand.CommandQueryDelegate(
                    () =>
                    {
                        Dispatcher.Invoke((ThreadStart)delegate
                        {
                            MessageBox.Show(this, "按确认以后请再次打开DTU.", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        }, null);
                    }));

                PowerCycled = true;
            }
            AddNewCommand("AT^DEBUG=0\n", "AT^DEBUG=0\n\r\nOK\r\n", wait4Resp: false);
            AddNewCommand("+++", "+++", ret2: "OK", match: true, trim: true);
            //AddNewCommand("&F\n", "&F\n");
            AddNewCommand("AT^SERVER=" + ServerIP + ":" + ServerPortString + "\n", "AT^SERVER=" + ServerIP + ":" + ServerPortString + "\n\r\nOK\r\n");
            AddNewCommand("AT^BAUD=" + _localBundOc[cboxDtuBund.SelectedIndex] + "\n", "AT^BAUD=" + _localBundOc[cboxDtuBund.SelectedIndex] + "\n\r\nOK\r\n");
            AddNewCommand("AT^UTCF=" + _dtuDataOc[cboxDtuData.SelectedIndex] + (cboxDtuStop.SelectedIndex + 1).ToString() + cboxDtuParity.SelectedIndex.ToString() + "\n",
                "AT^UTCF=" + _dtuDataOc[cboxDtuData.SelectedIndex] + (cboxDtuStop.SelectedIndex + 1).ToString() + cboxDtuParity.SelectedIndex.ToString() + "\n\r\nOK\r\n");
            AddNewCommand("AT^PKMD=2\n", "AT^PKMD=2\n\r\nOK\r\n");
            AddNewCommand("AT^CRGDA=" + RegisterPackageHEX + "\n", "AT^CRGDA=" + RegisterPackageHEX + "\n\r\nOK\r\n");
            AddNewCommand("AT^SAVE\n", "AT^SAVE\n\r\nOK\r\n");
            //AddNewCommand("AT^GPRS\n", "AT^GPRS\n\r\nOK\r\n");
        }

        private void ComposeQueryList(bool setDebug = false, bool compare = true)
        {
            if (setDebug == true && (PowerCycleNeeded == true || (PowerCycleNeeded == false && PowerCycled == false)))
            {
                AddNewCommand("AT^VERS\n", "GPRS DTU", true, true,
                    new DTUCommand.CommandQueryDelegate(
                    () =>
                    {
                        Dispatcher.Invoke((ThreadStart)delegate
                        {
                            MessageBox.Show(this, "Please power off the DTU and then press OK.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        }, null);
                    }),
                    new DTUCommand.CommandQueryDelegate(
                    () =>
                    {
                        Dispatcher.Invoke((ThreadStart)delegate
                        {
                            MessageBox.Show(this, "Please press OK and then power on the DTU again.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        }, null);
                    }));
                AddNewCommand("AT^DEBUG=0\n", "AT^DEBUG=0\n\r\nOK\r\n", wait4Resp: false);
                AddNewCommand("+++", "+++", ret2: "OK", match: true, trim: true);
            }
            if (compare == true)
            {
                AddNewQuery("AT^SERVER=?\n", "AT^SERVER=?\n\r\n" + ServerIP + ":" + ServerPortString + "\r\n\r\nOK\r\n");
                AddNewQuery("AT^BAUD=?\n", "AT^BAUD=?\n\r\n" + _localBundOc[cboxDtuBund.SelectedIndex] + "\r\n\r\nOK\r\n");
                AddNewQuery("AT^UTCF=?\n", "AT^UTCF=?\n\r\n" + _dtuDataOc[cboxDtuData.SelectedIndex] + (cboxDtuStop.SelectedIndex + 1).ToString() + cboxDtuParity.SelectedIndex.ToString() + "\r\n\r\nOK\r\n");
                AddNewQuery("AT^PKMD=?\n", "AT^PKMD=?\n\r\n2\r\n\r\nOK\r\n");
                AddNewQuery("AT^CRGDA=?\n", "AT^CRGDA=?\n\r\n" + RegisterPackageHEX + "\r\n\r\nOK\r\n");
            }
            else
            {
                AddNewQuery("AT^SERVER=?\n", needComp: false);
                AddNewQuery("AT^BAUD=?\n", needComp: false);
                AddNewQuery("AT^UTCF=?\n", needComp: false);
                AddNewQuery("AT^PKMD=?\n", needComp: false);
                AddNewQuery("AT^CRGDA=?\n", needComp: false);
            }
        }

        private void Write_DTU_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_sPort == null || _sPort.IsOpen == false)
            {
                MessageBox.Show("串口没有被正常打开.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            _dtuCmdOc.Clear();
            ComposeCommandList();
            ComposeQueryList(false);
            DoWrite();
        }

        private void Read_DTU_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_sPort == null || _sPort.IsOpen == false)
            {
                MessageBox.Show("串口没有被正常打开.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            _dtuCmdOc.Clear();
            ComposeQueryList(true, false);
            DoWrite();
        }

        private void Stop_DTU_Button_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("该操作不安全.\n\n需要继续吗?", "确认", 
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            if (_ctsIO != null)
                _ctsIO.Cancel();
            ReadyString = "就绪";
            StatusPbarValue = 0;
            InRun = false;
        }

        private void DoWrite()
        {
            InRun = true;
            _ctsIO = new CancellationTokenSource();
            Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        //Thread th = Thread.CurrentThread;
                        //using (_ctsIO.Token.Register(th.Abort))
                        //{
                            DoWriteTask();
                        //}
                    }
                    catch (Exception ex)
                    {
                        LogMessage("任务异常终止 : " + ex.Message, DTUConfigLogMessage.MessageState.Error);
                    }
                }, _ctsIO.Token);
        }

        private void DoWriteTask()
        {
            int i = 0;
            foreach (DTUCommand dtuci in _dtuCmdOc)
            {
                if (_ctsIO.Token.IsCancellationRequested == true)
                    return;

                Dispatcher.BeginInvoke((ThreadStart)delegate
                {
                    dgCmd.SelectedItem = dgCmd.Items[i];
                    dgCmd.UpdateLayout();
                    dgCmd.ScrollIntoView(dgCmd.SelectedItem);
                }, null);
                try
                {
                    LogMessage(dtuci.CommandDisplay, DTUConfigLogMessage.MessageState.None, DTUConfigLogMessage.MessageFlow.ToDTU);

                    if (dtuci.BeforeCommandSendDelegateHandler != null)
                        dtuci.BeforeCommandSendDelegateHandler();

                    if (dtuci.IsCmd == true)
                    {
                        #region Command

                        ReadyString = "向DTU发送命令 : " + dtuci.CommandDisplay;
                        StatusPbarMax = _sPort.WriteTimeout + 1 * 1000;
                        StatusPbarValue = 0;
                        _timerPBar.Change(0, 1000);
                        _sPort.Write(dtuci.Command);
                        _timerPBar.Change(Timeout.Infinite, 1000);
                        StatusPbarValue = 0;

                        Thread.Sleep(Consts.TASK_DTU_THREAD_SLEEP_INTERVAL);

                        if (dtuci.AfterCommandSendDelegateHandler != null)
                            dtuci.AfterCommandSendDelegateHandler();

                        if (dtuci.MustMatch == false)
                        {
                            #region Not Must Match

                            ReadyString = "Reading return from DTU...";
                            StatusPbarMax = _sPort.ReadTimeout + 1 * 1000;
                            StatusPbarValue = 0;
                            _timerPBar.Change(0, 1000);
                            byte[] bytes = new byte[1024];
                            int len = _sPort.Read(bytes, 0, 1024);
                            dtuci.Return = Encoding.UTF8.GetString(bytes, 0, len);
                            _timerPBar.Change(Timeout.Infinite, 1000);
                            StatusPbarValue = 0;
                            ReadyString = "Read return from DTU : " + dtuci.ReturnDisplay;
                            string sr = dtuci.Return;
                            string src = dtuci.RCompare;
                            string src2 = dtuci.RCompare2;
                            if (dtuci.DoTrim == true)
                            {
                                sr = sr.Trim();
                                src = src.Trim();
                                src2 = src2.Trim();
                            }
                            if (string.Compare(sr, src, true) == 0)
                            {
                                Dispatcher.BeginInvoke((ThreadStart)delegate()
                                {
                                    LogMessage("DTU command is ok.", DTUConfigLogMessage.MessageState.OK, DTUConfigLogMessage.MessageFlow.FromDTU);
                                    dtuci.State = DTUCommand.DTUCommandState.OK;
                                }, null);
                            }
                            else
                            {
                                if (string.IsNullOrWhiteSpace(src2) == false)
                                {
                                    if (string.Compare(sr, src2, true) == 0)
                                    {
                                        Dispatcher.BeginInvoke((ThreadStart)delegate()
                                        {
                                            LogMessage("DTU command is ok.", DTUConfigLogMessage.MessageState.OK, DTUConfigLogMessage.MessageFlow.FromDTU);
                                            dtuci.State = DTUCommand.DTUCommandState.OK;
                                        }, null);
                                    }
                                    else
                                    {
                                        Dispatcher.BeginInvoke((ThreadStart)delegate()
                                        {
                                            if (dtuci.NeedCompareResponse == true)
                                            {
                                                LogMessage("DTU command fails : " + dtuci.ReturnDisplay, DTUConfigLogMessage.MessageState.Fail, DTUConfigLogMessage.MessageFlow.FromDTU);
                                                dtuci.State = DTUCommand.DTUCommandState.Fail;
                                            }
                                            else
                                            {
                                                LogMessage("DTU command fails : " + dtuci.ReturnDisplay, DTUConfigLogMessage.MessageState.Infomation, DTUConfigLogMessage.MessageFlow.FromDTU);
                                                dtuci.State = DTUCommand.DTUCommandState.Infomation;
                                            }
                                        }, null);
                                    }
                                }
                                else
                                {
                                    Dispatcher.BeginInvoke((ThreadStart)delegate()
                                    {
                                        if (dtuci.NeedCompareResponse == true)
                                        {
                                            LogMessage("DTU command fails : " + dtuci.ReturnDisplay, DTUConfigLogMessage.MessageState.Fail, DTUConfigLogMessage.MessageFlow.FromDTU);
                                            dtuci.State = DTUCommand.DTUCommandState.Fail;
                                        }
                                        else
                                        {
                                            LogMessage("DTU command fails : " + dtuci.ReturnDisplay, DTUConfigLogMessage.MessageState.Infomation, DTUConfigLogMessage.MessageFlow.FromDTU);
                                            dtuci.State = DTUCommand.DTUCommandState.Infomation;
                                        }
                                        //LogMessage("DTU command fails : " + dtuci.ReturnDisplay, DTUConfigLogMessage.MessageState.Fail, DTUConfigLogMessage.MessageFlow.FromDTU);
                                    }, null);
                                }
                            }

                            Thread.Sleep(Consts.TASK_DTU_THREAD_SLEEP_INTERVAL);

                            #endregion
                        }
                        else
                        {
                            #region Must Match

                            bool notMatched = true;
                            while (notMatched)
                            {
                                if (_ctsIO.Token.IsCancellationRequested == true)
                                    return;

                                ReadyString = "Reading return from DTU...";
                                StatusPbarMax = _sPort.ReadTimeout + 1 * 1000;
                                StatusPbarValue = 0;
                                _timerPBar.Change(0, 1000);
                                byte[] bytes = new byte[1024];
                                int len = _sPort.Read(bytes, 0, 1024);
                                dtuci.Return = Encoding.UTF8.GetString(bytes, 0, len);
                                _timerPBar.Change(Timeout.Infinite, 1000);
                                StatusPbarValue = 0;
                                ReadyString = "Read return from DTU : " + dtuci.ReturnDisplay;
                                string sr = dtuci.Return;
                                string src = dtuci.RCompare;
                                string src2 = dtuci.RCompare2;
                                if (dtuci.DoTrim == true)
                                {
                                    sr = sr.Trim();
                                    src = src.Trim();
                                    src2 = src2.Trim();
                                }
                                if (string.Compare(sr, src, true) == 0)
                                {
                                    Dispatcher.BeginInvoke((ThreadStart)delegate()
                                    {
                                        LogMessage("DTU command is ok.", DTUConfigLogMessage.MessageState.OK, DTUConfigLogMessage.MessageFlow.FromDTU);
                                        dtuci.State = DTUCommand.DTUCommandState.OK;
                                    }, null);
                                    notMatched = false;
                                }
                                else
                                {
                                    if (string.IsNullOrWhiteSpace(src2) == false)
                                    {
                                        if (string.Compare(sr, src2, true) == 0)
                                        {
                                            Dispatcher.BeginInvoke((ThreadStart)delegate()
                                            {
                                                LogMessage("DTU command is ok.", DTUConfigLogMessage.MessageState.OK, DTUConfigLogMessage.MessageFlow.FromDTU);
                                                dtuci.State = DTUCommand.DTUCommandState.OK;
                                            }, null);
                                            notMatched = false;
                                        }
                                        else
                                        {
                                            Dispatcher.BeginInvoke((ThreadStart)delegate()
                                            {
                                                LogMessage("DTU command waits \"" + dtuci.RCompareDisplay + "\" again : " + dtuci.ReturnDisplay, DTUConfigLogMessage.MessageState.Infomation, DTUConfigLogMessage.MessageFlow.FromDTU);
                                            }, null);
                                        }
                                    }
                                    else
                                    {
                                        Dispatcher.BeginInvoke((ThreadStart)delegate()
                                        {
                                            LogMessage("DTU command waits \"" + dtuci.RCompareDisplay + "\" again : " + dtuci.ReturnDisplay, DTUConfigLogMessage.MessageState.Infomation, DTUConfigLogMessage.MessageFlow.FromDTU);
                                        }, null);
                                    }
                                }

                                Thread.Sleep(Consts.TASK_DTU_THREAD_SLEEP_INTERVAL);
                            }

                            #endregion
                        }

                        Dispatcher.BeginInvoke((ThreadStart)delegate()
                        {
                            if (dtuci.NeedCompareResponse == false &&
                                (dtuci.State == DTUCommand.DTUCommandState.Error ||
                                dtuci.State == DTUCommand.DTUCommandState.Fail))
                                dtuci.State = DTUCommand.DTUCommandState.Infomation;
                        }, null);

                        #endregion
                    }
                    else
                    {
                        #region Query

                        ReadyString = "从DTU获取信息 : " + dtuci.CommandDisplay;
                        StatusPbarMax = _sPort.WriteTimeout + 1 * 1000;
                        StatusPbarValue = 0;
                        _timerPBar.Change(0, 1000);
                        _sPort.Write(dtuci.Command);
                        _timerPBar.Change(Timeout.Infinite, 1000);
                        StatusPbarValue = 0;

                        Thread.Sleep(Consts.TASK_DTU_THREAD_SLEEP_INTERVAL);

                        if (dtuci.AfterCommandSendDelegateHandler != null)
                            dtuci.AfterCommandSendDelegateHandler();

                        ReadyString = "正在等待DTU的响应...";
                        StatusPbarMax = _sPort.ReadTimeout + 1 * 1000;
                        StatusPbarValue = 0;
                        _timerPBar.Change(0, 1000);
                        byte[] bytes = new byte[1024];
                        int len = _sPort.Read(bytes, 0, 1024);
                        dtuci.Response = Encoding.UTF8.GetString(bytes, 0, len);
                        _timerPBar.Change(Timeout.Infinite, 1000);
                        StatusPbarValue = 0;
                        ReadyString = "从DTU得到响应 : " + dtuci.Response;

                        #region Update Config Panel

                        Dispatcher.Invoke((ThreadStart)delegate
                        {
                            string[] sa = dtuci.Response.Split(new string[] { "\n", "\r", "\0" }, StringSplitOptions.RemoveEmptyEntries);
                            if (sa.Length != 3)
                            {
                                LogMessage("查询响应失败 : " + dtuci.ResponseDisplay, DTUConfigLogMessage.MessageState.OK, DTUConfigLogMessage.MessageFlow.FromDTU);
                                dtuci.State = DTUCommand.DTUCommandState.Fail;
                            }
                            else
                            {
                                int index = -1;
                                switch (dtuci.Command)
                                {
                                    default:
                                        break;
                                    case "AT^SERVER=?\n":
                                        sa = sa[1].Trim().Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                                        if (sa.Length != 2)
                                        {
                                            LogMessage("查询服务器响应失败 : " + dtuci.ResponseDisplay, DTUConfigLogMessage.MessageState.OK, DTUConfigLogMessage.MessageFlow.FromDTU);
                                            dtuci.State = DTUCommand.DTUCommandState.Fail;
                                        }
                                        else
                                        {
                                            ServerIP = sa[0].Trim();
                                            CheckHostnameOrIP();
                                            ServerPortString = sa[1].Trim();
                                            if (ServerIPOK == false || ServerPortOK == false)
                                                dtuci.State = DTUCommand.DTUCommandState.Fail;
                                        }
                                        break;
                                    case "AT^BAUD=?\n":
                                        index = _localBundOc.IndexOf(sa[1].Trim());
                                        if (index < 0)
                                        {
                                            LogMessage("查询波特率响应失败 : " + dtuci.ResponseDisplay, DTUConfigLogMessage.MessageState.OK, DTUConfigLogMessage.MessageFlow.FromDTU);
                                            dtuci.State = DTUCommand.DTUCommandState.Fail;
                                        }
                                        else
                                            cboxDtuBund.SelectedIndex = index;
                                        break;
                                    case "AT^UTCF=?\n":
                                        if (sa[1].Trim().Length != 3)
                                        {
                                            LogMessage("查询格式响应失败 : " + dtuci.ResponseDisplay, DTUConfigLogMessage.MessageState.OK, DTUConfigLogMessage.MessageFlow.FromDTU);
                                            dtuci.State = DTUCommand.DTUCommandState.Fail;
                                        }
                                        else
                                        {
                                            string s1 = sa[1].Trim().Substring(0, 1);
                                            string s2 = sa[1].Trim().Substring(1, 1);
                                            string s3 = sa[1].Trim().Substring(2, 1);

                                            index = _dtuDataOc.IndexOf(s1);
                                            if (index < 0)
                                            {
                                                LogMessage("查询数据格式响应失败 : " + dtuci.ResponseDisplay, DTUConfigLogMessage.MessageState.OK, DTUConfigLogMessage.MessageFlow.FromDTU);
                                                dtuci.State = DTUCommand.DTUCommandState.Fail;
                                            }
                                            else
                                                cboxDtuData.SelectedIndex = index;
                                            if (s2 != "1" && s2 != "2" && s2 != "3")
                                            {
                                                LogMessage("查询停止格式响应失败 : " + dtuci.ResponseDisplay, DTUConfigLogMessage.MessageState.OK, DTUConfigLogMessage.MessageFlow.FromDTU);
                                                dtuci.State = DTUCommand.DTUCommandState.Fail;
                                            }
                                            else
                                            {
                                                int iv = int.Parse(s2);
                                                cboxDtuStop.SelectedIndex = iv - 1;
                                            }
                                            if (s3 != "0" && s3 != "1" && s3 != "2" && s3 != "3" && s3 != "4")
                                            {
                                                LogMessage("查询校验格式响应失败 : " + dtuci.ResponseDisplay, DTUConfigLogMessage.MessageState.OK, DTUConfigLogMessage.MessageFlow.FromDTU);
                                                dtuci.State = DTUCommand.DTUCommandState.Fail;
                                            }
                                            else
                                            {
                                                int iv = int.Parse(s3);
                                                cboxDtuStop.SelectedIndex = iv;
                                            }
                                        }
                                        break;
                                    case "AT^PKMD=?\n":
                                        if (sa[1].Trim() != "2")
                                        {
                                            LogMessage("查询响应失败 : " + dtuci.ResponseDisplay, DTUConfigLogMessage.MessageState.OK, DTUConfigLogMessage.MessageFlow.FromDTU);
                                            dtuci.State = DTUCommand.DTUCommandState.Fail;
                                        }
                                        break;
                                    case "AT^CRGDA=?\n":
                                        RegisterPackageHEX = sa[1].Trim();
                                        if (RegisterPackageOK == false)
                                            dtuci.State = DTUCommand.DTUCommandState.Fail;
                                        break;
                                }
                            }
                        }, null);

                        #endregion

                        if (dtuci.NeedCompareResponse == true)
                        {
                            #region Compare

                            string sr = dtuci.Response;
                            string sc = dtuci.Compare;
                            if (dtuci.DoTrim == true)
                            {
                                sr = sr.Trim();
                                sc = sc.Trim();
                            }
                            if (string.Compare(sr, sc, true) == 0)
                            {
                                Dispatcher.BeginInvoke((ThreadStart)delegate()
                                {
                                    LogMessage("DTU查询正常.", DTUConfigLogMessage.MessageState.OK, DTUConfigLogMessage.MessageFlow.FromDTU);
                                    dtuci.State = DTUCommand.DTUCommandState.OK;
                                }, null);
                            }
                            else
                            {
                                Dispatcher.BeginInvoke((ThreadStart)delegate()
                                {
                                    LogMessage("DTU查询失败 : " + dtuci.ResponseDisplay, DTUConfigLogMessage.MessageState.Fail, DTUConfigLogMessage.MessageFlow.FromDTU);
                                    dtuci.State = DTUCommand.DTUCommandState.Fail;
                                }, null);
                            }

                            #endregion
                        }
                        else
                        {
                            Dispatcher.BeginInvoke((ThreadStart)delegate()
                            {
                                LogMessage("DTU查询正常.", DTUConfigLogMessage.MessageState.OK, DTUConfigLogMessage.MessageFlow.FromDTU);
                                dtuci.State = DTUCommand.DTUCommandState.OK;
                            }, null);
                        }

                        Thread.Sleep(Consts.TASK_DTU_THREAD_SLEEP_INTERVAL);

                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke((ThreadStart)delegate()
                    {
                        if (dtuci.NeedCompareResponse == false)
                        {
                            LogMessage("DTU命令错误 : " + ex.Message, DTUConfigLogMessage.MessageState.Infomation);
                            dtuci.State = DTUCommand.DTUCommandState.Infomation;
                        }
                        else
                        {
                            LogMessage("DTU命令错误 : " + ex.Message, DTUConfigLogMessage.MessageState.Error);
                            dtuci.State = DTUCommand.DTUCommandState.Error;
                        }
                    }, null);
                }
                i++;

                Thread.Sleep(Consts.TASK_DTU_THREAD_SLEEP_INTERVAL);
            }
            _timerPBar.Change(Timeout.Infinite, 1000);
            StatusPbarValue = 0;
            ReadyString = "就绪";
            InRun = false;
        }

        private void LoadConfig(string cfg)
        {
            _inInit = true;

            CloseSerialPort();

            string strLine = null;
            int i = 0;
            StreamReader sr = null;
            try
            {
                sr = new StreamReader(cfg);
                while (true)
                {
                    strLine = sr.ReadLine();
                    if (string.IsNullOrWhiteSpace(strLine))
                        break;
                    strLine = strLine.Trim();
                    if (i == 0)
                    {
                        string[] sa = strLine.Split(new string[] { "\t" }, StringSplitOptions.None);
                        if (sa.Length != 3)
                            LogMessage("本地配置错误 : " + strLine, state: DTUConfigLogMessage.MessageState.Fail);
                        else
                        {
                            bool found = false;
                            for (int idx = 0; idx < _localPortOc.Count; idx++)
                            {
                                if (string.Compare(sa[0].Trim(), _localPortOc[idx].Trim(), true) == 0)
                                {
                                    found = true;
                                    cboxLocalPort.SelectedIndex = idx;
                                    break;
                                }
                            }
                            if (found == false)
                            {
                                LogMessage("本地端口错误 : " + sa[0].Trim(), state: DTUConfigLogMessage.MessageState.Fail);
                                if (_localPortOc.Count > 0)
                                    cboxLocalPort.SelectedIndex = 0;
                            }

                            found = false;
                            for (int idx = 0; idx < _localBundOc.Count; idx++)
                            {
                                if (string.Compare(sa[1].Trim(), _localBundOc[idx].Trim(), true) == 0)
                                {
                                    found = true;
                                    cboxLocalBund.SelectedIndex = idx;
                                    break;
                                }
                            }
                            if (found == false)
                            {
                                LogMessage("本地波特率错误 : " + sa[1].Trim(), state: DTUConfigLogMessage.MessageState.Fail);
                                if (_localBundOc.Count > 0)
                                    cboxLocalBund.SelectedIndex = 0;
                            }

                            found = false;
                            for (int idx = 0; idx < _localParityOc.Count; idx++)
                            {
                                if (string.Compare(sa[2].Trim(), _localParityOc[idx].Trim(), true) == 0)
                                {
                                    found = true;
                                    cboxLocalParity.SelectedIndex = idx;
                                    break;
                                }
                            }
                            if (found == false)
                            {
                                LogMessage("本地校验错误 : " + sa[2].Trim(), state: DTUConfigLogMessage.MessageState.Fail);
                                if (_localParityOc.Count > 0)
                                    cboxLocalParity.SelectedIndex = 0;
                            }
                        }
                    }
                    else if (i == 1)
                    {
                        string[] sa = strLine.Split(new string[] { "\t" }, StringSplitOptions.None);
                        if (sa.Length != 2)
                            LogMessage("连接设置错误 : " + strLine, state: DTUConfigLogMessage.MessageState.Fail);
                        else
                        {
                            ServerIP = sa[0].Trim();
                            CheckHostnameOrIP();
                            ServerPortString = sa[1].ToString();
                        }
                    }
                    else if (i == 2)
                    {
                        string[] sa = strLine.Split(new string[] { "\t" }, StringSplitOptions.None);
                        if (sa.Length != 6)
                            LogMessage("DTU设置错误 : " + strLine, state: DTUConfigLogMessage.MessageState.Fail);
                        else
                        {
                            bool found = false;
                            for (int idx = 0; idx < _localBundOc.Count; idx++)
                            {
                                if (string.Compare(sa[0].Trim(), _localBundOc[idx].Trim(), true) == 0)
                                {
                                    found = true;
                                    cboxDtuBund.SelectedIndex = idx;
                                    break;
                                }
                            }
                            if (found == false)
                            {
                                LogMessage("DTU波特率错误 : " + sa[0].Trim(), state: DTUConfigLogMessage.MessageState.Fail);
                                if (_localBundOc.Count > 0)
                                    cboxDtuBund.SelectedIndex = 0;
                            }

                            found = false;
                            for (int idx = 0; idx < _dtuDataOc.Count; idx++)
                            {
                                if (string.Compare(sa[1].Trim(), _dtuDataOc[idx].Trim(), true) == 0)
                                {
                                    found = true;
                                    cboxDtuData.SelectedIndex = idx;
                                    break;
                                }
                            }
                            if (found == false)
                            {
                                LogMessage("DTU数据错误 : " + sa[1].Trim(), state: DTUConfigLogMessage.MessageState.Fail);
                                if (_dtuDataOc.Count > 0)
                                    cboxDtuData.SelectedIndex = 0;
                            }

                            found = false;
                            for (int idx = 0; idx < _localParityOc.Count; idx++)
                            {
                                if (string.Compare(sa[2].Trim(), _localParityOc[idx].Trim(), true) == 0)
                                {
                                    found = true;
                                    cboxDtuParity.SelectedIndex = idx;
                                    break;
                                }
                            }
                            if (found == false)
                            {
                                LogMessage("DTU校验错误 : " + sa[2].Trim(), state: DTUConfigLogMessage.MessageState.Fail);
                                if (_dtuDataOc.Count > 0)
                                    cboxDtuParity.SelectedIndex = 0;
                            }

                            found = false;
                            for (int idx = 0; idx < _dtuStopOc.Count; idx++)
                            {
                                if (string.Compare(sa[3].Trim(), _dtuStopOc[idx].Trim(), true) == 0)
                                {
                                    found = true;
                                    cboxDtuStop.SelectedIndex = idx;
                                    break;
                                }
                            }
                            if (found == false)
                            {
                                LogMessage("DTU停止错误 : " + sa[3].Trim(), state: DTUConfigLogMessage.MessageState.Fail);
                                if (_dtuStopOc.Count > 0)
                                    cboxDtuStop.SelectedIndex = 0;
                            }

                            SIMID = sa[4].Trim();
                            RegisterPackage = sa[5].Trim();
                        }
                    }
                    else if (i == 3)
                    {
                        if (strLine.Trim() == "1")
                            ColumnIsVisible = Visibility.Visible;
                        else
                            ColumnIsVisible = Visibility.Collapsed;
                    }
                    else
                        break;

                    i++;
                }
                sr.Close();
                sr.Dispose();
            }
            catch (Exception ex)
            {
                LogMessage("装载配置错误 : " + ex.Message, state: DTUConfigLogMessage.MessageState.Error);

                if (sr != null)
                {
                    try
                    {
                        sr.Close();
                    }
                    catch (Exception) { }
                    try
                    {
                        sr.Dispose();
                    }
                    catch (Exception) { }
                }
            }

            OpenSerialPort();

            _inInit = false;
        }

        private void SaveConfig(string cfg)
        {
            lock (_saveCfgLock)
            {
                StreamWriter sw = null;
                try
                {
                    sw = new StreamWriter(cfg);
                    sw.WriteLine(_localPortOc[cboxLocalPort.SelectedIndex] + "\t" +
                        _localBundOc[cboxLocalBund.SelectedIndex] + "\t" +
                        _localParityOc[cboxLocalParity.SelectedIndex]);
                    sw.WriteLine(ServerIP + "\t" + ServerPortString);
                    sw.WriteLine(_localBundOc[cboxLocalBund.SelectedIndex] + "\t" +
                        _dtuDataOc[cboxDtuData.SelectedIndex] + "\t" +
                        _localParityOc[cboxDtuParity.SelectedIndex] + "\t" +
                        _dtuStopOc[cboxDtuStop.SelectedIndex] + "\t" +
                        SIMID + "\t" +
                        RegisterPackage);
                    sw.WriteLine((ColumnIsVisible == Visibility.Visible) ? "1" : "0");
                    sw.Close();
                    sw.Dispose();

                    MessageBox.Show("成功保存当前配置.", "保存配置", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    LogMessage("保存配置错误 : " + ex.Message, state: DTUConfigLogMessage.MessageState.Error);

                    if (sw != null)
                    {
                        try
                        {
                            sw.Close();
                        }
                        catch (Exception) { }
                        try
                        {
                            sw.Dispose();
                        }
                        catch (Exception) { }
                    }
                }
            }
        }

        private void DTUParameters_Combox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IsModified = true;
        }

        private void ConnectionMode_RadioButton_CheckedUnchecked(object sender, RoutedEventArgs e)
        {
            IsModified = true;
        }

        private void ClearLog_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_dtuLMOc.Count > 0 && MessageBox.Show("确认清除日志?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                _dtuLMOc.Clear();
        }

        private void PBarTimerCallBackHandler(object obj)
        {
            if ((StatusPbarValue + 1 * 1000) < StatusPbarMax)
                StatusPbarValue = StatusPbarValue + 1 * 1000;
        }

        private void Refresh_Button_Click(object sender, RoutedEventArgs e)
        {
            _inInit = true;

            _localPortOc.Clear();
            foreach (string s in SerialPort.GetPortNames())
            {
                _localPortOc.Add(s);
            }

            _inInit = false;

            if (_localPortOc.Count > 0)
                cboxLocalPort.SelectedIndex = 0;
            else
                LogMessage("无有效端口.", DTUConfigLogMessage.MessageState.Error);
        }

        private void ServerIP_TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CheckHostnameOrIP();
        }

        private void CheckHostnameOrIP()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ServerIP))
                {
                    ServerIPOK = false;
                    ServerIPFG = Brushes.Red;
                }
                else
                {
                    IPAddress[] ipads = Dns.GetHostAddresses(ServerIP);
                    if (ipads == null || ipads.Length < 1)
                    {
                        ServerIPOK = false;
                        ServerIPFG = Brushes.Red;
                    }
                    else
                    {
                        ServerIPOK = true;
                        ServerIPFG = Brushes.Black;
                    }
                }
            }
            catch (Exception)
            {
                ServerIPOK = false;
                ServerIPFG = Brushes.Red;
            }
        }

        private string CalcCharFromHEX(string sHex)
        {
            int iv = 0;
            try
            {
                iv = int.Parse(sHex, System.Globalization.NumberStyles.AllowHexSpecifier);
                return ((char)iv).ToString();
            }
            catch (Exception)
            {
                return "?";
            }
        }
    }

    public class DTUCommand : NotifyPropertyChangedClass
    {
        public enum DTUCommandState
        {
            None,
            Infomation,
            OK,
            Fail,
            Error
        }

        public int StateBackgroundIndex
        {
            get
            {
                if (State == DTUCommandState.Fail)
                    return (int)2;
                else if (State == DTUCommandState.Error)
                    return (int)3;
                else
                    return 0;
            }
        }

        private DTUCommandState _state = DTUCommandState.None;
        public DTUCommandState State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                _stateImage = new BitmapImage();
                _stateImage.BeginInit();
                switch (_state)
                {
                    default:
                    case DTUCommandState.None:
                        _stateImage.UriSource = new Uri("pack://application:,,,/terminalconfiguration;component/resources/status_none.png");
                        break;
                    case DTUCommandState.Infomation:
                        _stateImage.UriSource = new Uri("pack://application:,,,/terminalconfiguration;component/resources/status_info.png");
                        break;
                    case DTUCommandState.OK:
                        _stateImage.UriSource = new Uri("pack://application:,,,/terminalconfiguration;component/resources/status_ok.png");
                        break;
                    case DTUCommandState.Fail:
                        _stateImage.UriSource = new Uri("pack://application:,,,/terminalconfiguration;component/resources/status_error.png");
                        break;
                    case DTUCommandState.Error:
                        _stateImage.UriSource = new Uri("pack://application:,,,/terminalconfiguration;component/resources/status_ques.png");
                        break;
                }
                _stateImage.EndInit();
                NotifyPropertyChanged("State");
                NotifyPropertyChanged("StateImage");
                NotifyPropertyChanged("StateBackgroundIndex");                
            }
        }

        private BitmapImage _stateImage = null;
        public BitmapImage StateImage
        {
            get
            {
                return _stateImage;
            }
            set
            {
                _stateImage = value;
                NotifyPropertyChanged("StateImage");
            }
        }

        private Visibility _columnIsVisible = Visibility.Collapsed;
        public Visibility ColumnIsVisible
        {
            get
            {
                return _columnIsVisible;
            }
            set
            {
                _columnIsVisible = value;
                NotifyPropertyChanged("ColumnIsVisible");
            }
        }

        private bool _isCmd = true;
        public bool IsCmd
        {
            get
            {
                return _isCmd;
            }
            set
            {
                _isCmd = value;
                NotifyPropertyChanged("IsCmd");
            }
        }

        private string _cmd = "";
        public string Command
        {
            get
            {
                return _cmd;
            }
            set
            {
                _cmd = value;
                NotifyPropertyChanged("Command");
                NotifyPropertyChanged("CommandDisplay");
            }
        }

        public string CommandDisplay
        {
            get
            {
                string s = Command;
                while (s.IndexOf("\r") > -1)
                {
                    s = s.Replace("\r", "\\r");
                }
                while (s.IndexOf("\n") > -1)
                {
                    s = s.Replace("\n", "\\n");
                }
                while (s.IndexOf("\0") > -1)
                {
                    s = s.Replace("\0", "");
                }
                return s;
            }
        }

        private bool _mustMatch = false;
        public bool MustMatch
        {
            get
            {
                return _mustMatch;
            }
            set
            {
                _mustMatch = value;
                NotifyPropertyChanged("MustMatch");
            }
        }

        private bool _doTrim = false;
        public bool DoTrim
        {
            get
            {
                return _doTrim;
            }
            set
            {
                _doTrim = value;
                NotifyPropertyChanged("DoTrim");
            }
        }

        private string _return = "";
        public string Return
        {
            get
            {
                return _return;
            }
            set
            {
                _return = value;
                NotifyPropertyChanged("Return");
                NotifyPropertyChanged("ReturnDisplay");
                NotifyPropertyChanged("ReturnOfficial");
            }
        }

        public string ReturnOfficial
        {
            get
            {
                if (Return == null)
                    return "";
                string s = Return;
                string[] sa = s.Split(new string[] {"\r", "\n", "\0" }, StringSplitOptions.RemoveEmptyEntries);
                if (sa == null)
                    return "";
                string ret = "";
                foreach (string si in sa)
                {
                    if (ret == "")
                        ret = si;
                    else
                        ret = ret + " " + si;
                }
                return ret;
            }
        }

        public string ReturnDisplay
        {
            get
            {
                string s = Return;
                while (s.IndexOf("\r") > -1)
                {
                    s = s.Replace("\r", "\\r");
                }
                while (s.IndexOf("\n") > -1)
                {
                    s = s.Replace("\n", "\\n");
                }
                while (s.IndexOf("\0") > -1)
                {
                    s = s.Replace("\0", "");
                }
                return s;
            }
        }

        private string _rCompare = "";
        public string RCompare
        {
            get
            {
                return _rCompare;
            }
            set
            {
                _rCompare = value;
                NotifyPropertyChanged("RCompare");
                NotifyPropertyChanged("RCompareDisplay");
            }
        }

        private string _rCompare2 = "";
        public string RCompare2
        {
            get
            {
                return _rCompare2;
            }
            set
            {
                _rCompare2 = value;
                NotifyPropertyChanged("RCompare2");
                NotifyPropertyChanged("RCompareDisplay");
            }
        }

        public string RCompareDisplay
        {
            get
            {
                string s = RCompare;
                if (string.IsNullOrWhiteSpace(s))
                    s = "";
                while (s.IndexOf("\r") > -1)
                {
                    s = s.Replace("\r", "\\r");
                }
                while (s.IndexOf("\n") > -1)
                {
                    s = s.Replace("\n", "\\n");
                }
                while (s.IndexOf("\0") > -1)
                {
                    s = s.Replace("\0", "");
                }
                string s2 = RCompare2;
                if (string.IsNullOrWhiteSpace(s2))
                    s2 = "";
                while (s2.IndexOf("\r") > -1)
                {
                    s2 = s2.Replace("\r", "\\r");
                }
                while (s2.IndexOf("\n") > -1)
                {
                    s2 = s2.Replace("\n", "\\n");
                }
                while (s2.IndexOf("\0") > -1)
                {
                    s2 = s2.Replace("\0", "");
                }
                string sf = s;
                if (sf.Length > 0)
                {
                    if(s2.Length > 0)
                        sf = sf + " -OR- " + s2;
                }
                else
                    sf = s2;
                return sf;
            }
        }

        private string _response = "";
        public string Response
        {
            get
            {
                return _response;
            }
            set
            {
                _response = value;
                NotifyPropertyChanged("Response");
                NotifyPropertyChanged("ResponseDisplay");
                NotifyPropertyChanged("ResponseOfficial");
            }
        }


        public string ResponseOfficial
        {
            get
            {
                if (Response == null)
                    return "";
                string s = Response;
                string[] sa = s.Split(new string[] { "\r", "\n", "\0" }, StringSplitOptions.RemoveEmptyEntries);
                if (sa == null)
                    return "";
                string ret = "";
                foreach (string si in sa)
                {
                    if (ret == "")
                        ret = si;
                    else
                        ret = ret + " " + si;
                }
                return ret;
            }
        }

        public string ResponseDisplay
        {
            get
            {
                string s = Response;
                while (s.IndexOf("\r") > -1)
                {
                    s = s.Replace("\r", "\\r");
                }
                while (s.IndexOf("\n") > -1)
                {
                    s = s.Replace("\n", "\\n");
                }
                while (s.IndexOf("\0") > -1)
                {
                    s = s.Replace("\0", "");
                }
                return s;
            }
        }

        private bool _needCompareResponse = false;
        public bool NeedCompareResponse
        {
            get
            {
                return _needCompareResponse;
            }
            set
            {
                _needCompareResponse = value;
            }
        }

        private string _compare = "";
        public string Compare
        {
            get
            {
                return _compare;
            }
            set
            {
                _compare = value;
                NotifyPropertyChanged("Compare");
                NotifyPropertyChanged("CompareDisplay");
            }
        }

        public string CompareDisplay
        {
            get
            {
                string s = Compare;
                while (s.IndexOf("\r") > -1)
                {
                    s = s.Replace("\r", "\\r");
                }
                while (s.IndexOf("\n") > -1)
                {
                    s = s.Replace("\n", "\\n");
                }
                while (s.IndexOf("\0") > -1)
                {
                    s = s.Replace("\0", "");
                }
                return s;
            }
        }

        public delegate void CommandQueryDelegate();
        public CommandQueryDelegate BeforeCommandSendDelegateHandler;
        public CommandQueryDelegate AfterCommandSendDelegateHandler;

        private bool _waitForResponse = true;
        public bool WaitForReponse
        {
            get
            {
                return _waitForResponse;
            }
            set
            {
                _waitForResponse = value;
                NotifyPropertyChanged("WaitForReponse");
            }
        }
    }

    public class DTUConfigLogMessage : NotifyPropertyChangedClass
    {
        public enum MessageFlow
        {
            None,
            ToDTU,
            FromDTU
        }

        private MessageFlow _flow = MessageFlow.None;
        public MessageFlow Flow
        {
            get
            {
                return _flow;
            }
            set
            {
                _flow = value;
                _flowImage = new BitmapImage();
                _flowImage.BeginInit();
                switch (_flow)
                {
                    default:
                    case MessageFlow.None:
                        _flowImage.UriSource = new Uri("pack://application:,,,/terminalconfiguration;component/resources/flownone.png");
                        break;
                    case MessageFlow.ToDTU:
                        _flowImage.UriSource = new Uri("pack://application:,,,/terminalconfiguration;component/resources/flowrequest.png");
                        break;
                    case MessageFlow.FromDTU:
                        _flowImage.UriSource = new Uri("pack://application:,,,/terminalconfiguration;component/resources/flowresponse.png");
                        break;
                }
                _flowImage.EndInit();
                NotifyPropertyChanged("Flow");
                NotifyPropertyChanged("FlowImage");
            }
        }

        private BitmapImage _flowImage = null;
        public BitmapImage FlowImage
        {
            get
            {
                return _flowImage;
            }
            set
            {
                _flowImage = value;
                NotifyPropertyChanged("FlowImage");
            }
        }

        public enum MessageState
        {
            None,
            Infomation,
            OK,
            Fail,
            Error
        }

        private MessageState _state = MessageState.None;
        public MessageState State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                _stateImage = new BitmapImage();
                _stateImage.BeginInit();
                switch (_state)
                {
                    default:
                    case MessageState.None:
                        _stateImage.UriSource = new Uri("pack://application:,,,/terminalconfiguration;component/resources/status_none.png");
                        break;
                    case MessageState.Infomation:
                        _stateImage.UriSource = new Uri("pack://application:,,,/terminalconfiguration;component/resources/status_info.png");
                        break;
                    case MessageState.OK:
                        _stateImage.UriSource = new Uri("pack://application:,,,/terminalconfiguration;component/resources/status_ok.png");
                        break;
                    case MessageState.Fail:
                        _stateImage.UriSource = new Uri("pack://application:,,,/terminalconfiguration;component/resources/status_error.png");
                        break;
                    case MessageState.Error:
                        _stateImage.UriSource = new Uri("pack://application:,,,/terminalconfiguration;component/resources/status_ques.png");
                        break;
                }
                _stateImage.EndInit();
                NotifyPropertyChanged("State");
                NotifyPropertyChanged("StateImage");
            }
        }

        private BitmapImage _stateImage = null;
        public BitmapImage StateImage
        {
            get
            {
                return _stateImage;
            }
            set
            {
                _stateImage = value;
                NotifyPropertyChanged("StateImage");
            }
        }

        private string _message = "";
        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
                NotifyPropertyChanged("Message");
                NotifyPropertyChanged("MessageDisplay");
            }
        }

        public string MessageDisplay
        {
            get
            {
                string s = Message;
                while (s.IndexOf("\r") > -1)
                {
                    s = s.Replace("\r", "\\r");
                }
                while (s.IndexOf("\n") > -1)
                {
                    s = s.Replace("\n", "\\n");
                }
                while (s.IndexOf("\0") > -1)
                {
                    s = s.Replace("\0", "");
                }
                return s;
            }
        }
    }
}
