using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
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
using System.Xml;
using System.Xml.Linq;
using Microsoft.Win32;

using iTextSharp;
using iTextSharp.text;
using iTextSharp.text.api;
using iTextSharp.text.error_messages;
using iTextSharp.text.exceptions;
using iTextSharp.text.factories;
using iTextSharp.text.html;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.io;
using iTextSharp.text.log;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.codec;
using iTextSharp.text.pdf.codec.wmf;
using iTextSharp.text.pdf.collection;
using iTextSharp.text.pdf.crypto;
using iTextSharp.text.pdf.draw;
using iTextSharp.text.pdf.events;
using iTextSharp.text.pdf.fonts;
using iTextSharp.text.pdf.fonts.cmaps;
using iTextSharp.text.pdf.hyphenation;
using iTextSharp.text.pdf.interfaces;
using iTextSharp.text.pdf.intern;
using iTextSharp.text.pdf.languages;
using iTextSharp.text.pdf.parser;
using iTextSharp.text.pdf.qrcode;
using iTextSharp.text.pdf.richmedia;
using iTextSharp.text.pdf.security;
using iTextSharp.text.pdf.spatial;
using iTextSharp.text.pdf.spatial.units;

using Bumblebee.SetCmd;
using Bumblebee.ExtSetCmd;

using WinParagraph = System.Windows.Documents.Paragraph;
using PdfParagraph = iTextSharp.text.Paragraph;

namespace Bumblebee
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

        public enum RunMode
        {
            User,
            Admin//,
            //Super
        }

        public enum LogType
        {
            Common,
            Information,
            Error
        }

        public enum SerialPortChkWriteReadType
        {
            E0H,
            E1H,
            E2H,
            E3H,
            E4H
        }

        public const int MAX_DISPLAY_LINE_COUNT = 2000;
        public const int CHK_CMD_INTERVAL = 500;

        #region Variables

        public static string[] _bauds = new string[]
        {
            "115200",
            "57600",
            "56000",
            "43000",
            "38400",
            "19200",
            "9600",
            "4800",
            "2400",
            "1200",
            "600",
            "300"
        };

        public static string[] _parities = new string[]
        {
            "Odd",
            "Even",
            "None"
        };

        public static string[] _dataBits = new string[]
        {
            "8",
            "7",
            "6",
            "5"
        };

        public static string[] _stopBits = new string[]
        {
            "1",
            "1.5",
            "2"
        };

        private RunMode _runMode = RunMode.User;
        private bool _normalClose = false;

        private CancellationTokenSource _cts = null;
        private object _cmdLock = new object();
        private Queue<CmdDefinition> _cdQueue = new Queue<CmdDefinition>();

        private SerialPort _sPort = null;

        private CancellationTokenSource _ctsDisplay = null;
        private Task _displayLogTask = null;
        private object _logLock = new object();
        private Queue<Tuple<string, LogType, bool, string>> _logQueue = new Queue<Tuple<string, LogType, bool, string>>();

        private XmlDocument _xd = new XmlDocument();

        private Task _serialPortTask = null;
        private Task _serialPortChkTask = null;
        private List<CmdDefinition> _cmdsList = new List<CmdDefinition>();
        private bool _displayedEnterChk = false;

        private Timer _timerPBar = null;

        private bool _started = false;

        private AutoResetEvent _createPdfEvent = new AutoResetEvent(false);

        private ObservableCollection<Cmd15HResponse> _cmd15HRespOc = new ObservableCollection<Cmd15HResponse>();
        private ObservableCollection<Cmd14HResponse> _cmd14HRespOc = new ObservableCollection<Cmd14HResponse>();
        private ObservableCollection<Cmd13HResponse> _cmd13HRespOc = new ObservableCollection<Cmd13HResponse>();
        private ObservableCollection<Cmd12HResponse> _cmd12HRespOc = new ObservableCollection<Cmd12HResponse>();
        private ObservableCollection<Cmd11HResponse> _cmd11HRespOc = new ObservableCollection<Cmd11HResponse>();
        private ObservableCollection<Cmd10HResponse> _cmd10HRespOc = new ObservableCollection<Cmd10HResponse>();
        private ObservableCollection<Cmd09HResponse> _cmd09HRespOc = new ObservableCollection<Cmd09HResponse>();
        private ObservableCollection<Cmd08HResponse> _cmd08HRespOc = new ObservableCollection<Cmd08HResponse>();

        private Document _pdfDocument = null;
        private string _docTitleDateTime = "";

        #endregion

        #region Properties

        private bool _needReport = false;
        public bool NeedReport
        {
            get
            {
                return _needReport;
            }
            set
            {
                _needReport = value;
                NotifyPropertyChanged("NeedReport");
            }
        }

        private string _curDir = "";
        public string CurrentDirectory
        {
            get
            {
                return _curDir;
            }
            set
            {
                _curDir = value;
                NotifyPropertyChanged("CurrentDirectory");
            }
        }

        public bool InChkCmd
        {
            get
            {
                return (CheckModeChecked == true) ? true : false;
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

        private bool _getCmdEnbaled = false;
        public bool GetCmdEnabled
        {
            get
            {
                return _getCmdEnbaled;
            }
            set
            {
                _getCmdEnbaled = value;
                NotifyPropertyChanged("GetCmdEnabled");
            }
        }

        private bool _setCmdEnbaled = false;
        public bool SetCmdEnabled
        {
            get
            {
                return _setCmdEnbaled;
            }
            set
            {
                _setCmdEnbaled = value;
                NotifyPropertyChanged("SetCmdEnabled");
            }
        }

        private bool _chkCmdEnbaled = false;
        public bool ChkCmdEnabled
        {
            get
            {
                return _chkCmdEnbaled;
            }
            set
            {
                _chkCmdEnbaled = value;
                NotifyPropertyChanged("ChkCmdEnabled");
            }
        }

        private bool _extGetCmdEnbaled = false;
        public bool ExtGetCmdEnabled
        {
            get
            {
                return _extGetCmdEnbaled;
            }
            set
            {
                _extGetCmdEnbaled = value;
                NotifyPropertyChanged("ExtGetCmdEnabled");
            }
        }

        private bool _extSetCmdEnbaled = false;
        public bool ExtSetCmdEnabled
        {
            get
            {
                return _extSetCmdEnbaled;
            }
            set
            {
                _extSetCmdEnbaled = value;
                NotifyPropertyChanged("ExtSetCmdEnabled");
            }
        }

        private bool? _checkModeChecked = false;
        public bool? CheckModeChecked
        {
            get
            {
                return _checkModeChecked;
            }
            set
            {
                _checkModeChecked = value;
                if (_checkModeChecked == true)
                {
                    foreach (CmdDefinition cd in _setCmdOc)
                    {
                        cd.CmdSelected = false;
                    }
                    foreach (CmdDefinition cd in _getCmdOc)
                    {
                        cd.CmdSelected = false;
                    }
                    foreach (CmdDefinition cd in _chkCmdOc)
                    {
                        cd.CmdSelected = false;
                    }
                    _chkCmdOc[0].CmdSelected = true;
                }
                else
                {
                    if(_cts !=null)
                        _cts.Cancel();
                    foreach (CmdDefinition cd in _chkCmdOc)
                    {
                        cd.CmdSelected = false;
                    }
                    CheckCmdState = "状态提示...";
                }
                NotifyPropertyChanged("InChkCmd");
                NotifyPropertyChanged("CheckModeChecked");
                NotifyPropertyChanged("CheckModeSelectEnabled");
            }
        }

        private bool _alreadyEnterCheck = false;
        public bool AlreadyEnterCheck
        {
            get
            {
                return _alreadyEnterCheck;
            }
            set
            {
                _alreadyEnterCheck = value;
                foreach (CmdDefinition cdi in _chkCmdOc)
                {
                    string header = cdi.CmdContent.Substring(0, 3).ToUpper();
                    if (_alreadyEnterCheck == false)
                    {
                        if (string.Compare(header, "E0H", true) == 0)
                        {
                            cdi.ChkCmdEnabled = true;
                        }
                        else
                        {
                            cdi.ChkCmdEnabled = false;
                        }
                    }
                    else
                    {
                        //if (string.Compare(header, "E0H", true) == 0)
                        //{
                        //    cdi.ChkCmdEnabled = false;
                        //}
                        //else
                        //{
                            cdi.ChkCmdEnabled = true;
                        //}
                    }
                }
                NotifyPropertyChanged("AlreadyEnterCheck");
            }
        }

        public bool CheckModeSelectEnabled
        {
            get
            {
                if (_checkModeChecked != true)
                    return false;
                else
                    return true;
            }
        }

        public int PBarMinimum
        {
            get
            {
                return 0;
            }
            set
            {
                NotifyPropertyChanged("PBarMinimum");
            }
        }

        public int PBarMaximum
        {
            get
            {
                return 10000;
            }
            set
            {
                NotifyPropertyChanged("PBarMaximum");
            }
        }

        private int _pBarValue = 0;
        public int PBarValue
        {
            get
            {
                return _pBarValue;
            }
            set
            {
                _pBarValue = value;
                NotifyPropertyChanged("PBarValue");
            }
        }

        private string _readyString = "就绪";
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

        private string _readyString2 = "";
        public string ReadyString2
        {
            get
            {
                return _readyString2;
            }
            set
            {
                _readyString2 = value;
                NotifyPropertyChanged("ReadyString2");
            }
        }

        private string _port = "COM1";
        public string Port
        {
            get
            {
                return _port;
            }
            set
            {
                _port = value;
                NotifyPropertyChanged("Port");
            }
        }

        private string _baud = "115200";
        public string Baud
        {
            get
            {
                return _baud;
            }
            set
            {
                _baud = value;
                NotifyPropertyChanged("Baud");
            }
        }

        private string _parity = "Odd";
        public string Parity
        {
            get
            {
                return _parity;
            }
            set
            {
                _parity = value;
                NotifyPropertyChanged("Parity");
            }
        }

        private string _dataBit = "8";
        public string DataBit
        {
            get
            {
                return _dataBit;
            }
            set
            {
                _dataBit = value;
                NotifyPropertyChanged("DataBit");
            }
        }

        private string _startBit = "1";
        public string StartBit
        {
            get
            {
                return _startBit;
            }
            set
            {
                _startBit = value;
                NotifyPropertyChanged("StartBit");
            }
        }

        private string _stopBit = "1";
        public string StopBit
        {
            get
            {
                return _stopBit;
            }
            set
            {
                _stopBit = value;
                NotifyPropertyChanged("StopBit");
            }
        }

        private string _timeout = "20000";
        public string TimeOut
        {
            get
            {
                return _timeout;
            }
            set
            {
                _timeout = value;
                double step = (double)PBarMaximum / ((double)(int.Parse(TimeOut)) / 100.0);
                PBarStep = (int)Math.Floor(step);
                NotifyPropertyChanged("TimeOut");
            }
        }

        private int _pBarStep = 10;
        public int PBarStep
        {
            get
            {
                return _pBarStep;
            }
            set
            {
                _pBarStep = value;
                NotifyPropertyChanged("PBarStep");
            }
        }

        private string _cmdInterval = "1000";
        public string CmdInterval
        {
            get
            {
                return _cmdInterval;
            }
            set
            {
                _cmdInterval = value;
                NotifyPropertyChanged("CmdInterval");
            }
        }

        private string _writeReadInterval = "1000";
        public string WriteReadInterval
        {
            get
            {
                return _writeReadInterval;
            }
            set
            {
                _writeReadInterval = value;
                NotifyPropertyChanged("WriteReadInterval");
            }
        }

        private string _serverip = "127.0.0.1";
        public string ServerIP
        {
            get
            {
                return _serverip;
            }
            set
            {
                _serverip = value;
                NotifyPropertyChanged("ServerIP");
            }
        }

        private string _serverport = "8768";
        public string ServerPort
        {
            get
            {
                return _serverport;
            }
            set
            {
                _serverport = value;
                NotifyPropertyChanged("ServerPort");
            }
        }

        private bool? _autoClearLog = true;
        public bool? AutoClearLog
        {
            get
            {
                return _autoClearLog;
            }
            set
            {
                _autoClearLog = value;
                NotifyPropertyChanged("AutoClearLog");
            }
        }

        private bool? _autoClearRecv = true;
        public bool? AutoClearRecv
        {
            get
            {
                return _autoClearRecv;
            }
            set
            {
                _autoClearRecv = value;
                NotifyPropertyChanged("AutoClearRecv");
            }
        }

        private bool? _autoClearSend = true;
        public bool? AutoClearSend
        {
            get
            {
                return _autoClearSend;
            }
            set
            {
                _autoClearSend = value;
                NotifyPropertyChanged("AutoClearSend");
            }
        }

        private string _d2 = "自定义";
        public string D2
        {
            get
            {
                return _d2;
            }
            set
            {
                _d2 = value;
                NotifyPropertyChanged("D2");
            }
        }

        private string _d1 = "自定义";
        public string D1
        {
            get
            {
                return _d1;
            }
            set
            {
                _d1 = value;
                NotifyPropertyChanged("D1");
            }
        }

        private string _d0 = "自定义";
        public string D0
        {
            get
            {
                return _d0;
            }
            set
            {
                _d0 = value;
                NotifyPropertyChanged("D0");
            }
        }

        #endregion

        #region Data Collection

        private ObservableCollection<CmdDefinition> _getCmdOc = new ObservableCollection<CmdDefinition>();
        private ObservableCollection<CmdDefinition> _setCmdOc = new ObservableCollection<CmdDefinition>();
        private ObservableCollection<CmdDefinition> _chkCmdOc = new ObservableCollection<CmdDefinition>();
        private ObservableCollection<CmdDefinition> _extGetCmdOc = new ObservableCollection<CmdDefinition>();
        private ObservableCollection<CmdDefinition> _extSetCmdOc = new ObservableCollection<CmdDefinition>();

        private string _checkCmdState = "状态提示...";
        public string CheckCmdState
        {
            get
            {
                return _checkCmdState;
            }
            set
            {
                _checkCmdState = value;
                if (_checkCmdState.IndexOf("出错") >= 0 ||
                    _checkCmdState.IndexOf("错误") >= 0 ||
                    _checkCmdState.IndexOf("异常") >= 0)
                    CheckCmdStateForeground = Brushes.Red;
                else
                    CheckCmdStateForeground = Brushes.Black;
                NotifyPropertyChanged("CheckCmdState");
            }
        }

        private Brush _checkCmdStateForeground = Brushes.Black;
        public Brush CheckCmdStateForeground
        {
            get
            {
                return _checkCmdStateForeground;
            }
            set
            {
                _checkCmdStateForeground = value;
                NotifyPropertyChanged("CheckCmdStateForeground");
            }
        }

        private bool _checkCmdEnabled = false;
        public bool CheckCmdEnabled
        {
            get
            {
                return _checkCmdEnabled;
            }
            set
            {
                _checkCmdEnabled = value;
                NotifyPropertyChanged("CheckCmdEnabled");
            }
        }

        private int _receivingByteNumber = 0;
        public int ReceivingByteNumber
        {
            get
            {
                return _receivingByteNumber;
            }
            set
            {
                _receivingByteNumber = value;
                NotifyPropertyChanged("ReceivingByteNumber");
                NotifyPropertyChanged("ReceivingByteNumberString");
            }
        }

        public string ReceivingByteNumberString
        {
            get
            {
                return "接收字节数 : " + ReceivingByteNumber.ToString();
            }
        }

        private int _sendingByteNumber = 0;
        public int SendingByteNumber
        {
            get
            {
                return _sendingByteNumber;
            }
            set
            {
                _sendingByteNumber = value;
                NotifyPropertyChanged("SendingByteNumber");
                NotifyPropertyChanged("SendingByteNumberString");
            }
        }

        public string SendingByteNumberString
        {
            get
            {
                return "发送字节数 : " + SendingByteNumber.ToString();
            }
        }

        #endregion

        public MainWindow(RunMode runMode = RunMode.User)
        {
            _runMode = runMode;
            switch (_runMode)
            {
                default:
                case RunMode.User:
                    GetCmdEnabled = true;
                    ExtGetCmdEnabled = true;
                    ExtSetCmdEnabled = true;
                    break;
                case RunMode.Admin:
                    GetCmdEnabled = true;
                    SetCmdEnabled = true;
                    ChkCmdEnabled = true;
                    ExtGetCmdEnabled = false;
                    ExtSetCmdEnabled = false;
                    break;
                //case RunMode.Super:
                //    GetCmdEnabled = true;
                //    SetCmdEnabled = true;
                //    ChkCmdEnabled = true;
                //    ExtGetCmdEnabled = true;
                //    ExtSetCmdEnabled = true;
                //    break;
            }

            InitializeComponent();

            DataContext = this;

            #region Data Init

            lvGetCmd.DataContext = _getCmdOc;
            lvSetCmd.DataContext = _setCmdOc;
            lvChkCmd.DataContext = _chkCmdOc;
            lvExtGetCmd.DataContext = _extGetCmdOc;
            lvExtSetCmd.DataContext = _extSetCmdOc;

            #region Get Cmd

            _getCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "00H : 记录仪执行标准版本",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Collapsed,
                    CmdState = "状态提示..."
                });
            _getCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "01H : 当前驾驶人信息",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Collapsed,
                    CmdState = "状态提示..."
                });
            _getCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "02H : 记录仪实时时间",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Collapsed,
                    CmdState = "状态提示..."
                });
            _getCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "03H : 累计行驶里程",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Collapsed,
                    CmdState = "状态提示..."
                });
            _getCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "04H : 记录仪脉冲系数",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Collapsed,
                    CmdState = "状态提示..."
                });
            _getCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "05H : 车辆信息",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Collapsed,
                    CmdState = "状态提示..."
                });
            _getCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "06H : 记录仪状态信号配置信息",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Collapsed,
                    CmdState = "状态提示..."
                });
            _getCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "07H : 记录仪唯一性编号",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Collapsed,
                    CmdState = "状态提示..."
                });
            _getCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "08H : 指定的行驶速度记录",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Visible,
                    CmdState = "状态提示...",
                    DataCountPerUnitMin = 1,
                    DataCountPerUnitMax = 7,
                    DataCountPerUnit = 7,
                    DataSingleCount = 126
                });
            _getCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "09H : 指定的位置信息记录",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Visible,
                    CmdState = "状态提示...",
                    DataCountPerUnitMin = 1,
                    DataCountPerUnitMax = 1,
                    DataCountPerUnit = 1,
                    DataSingleCount = 666
                });
            _getCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "10H : 指定的事故疑点记录",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Visible,
                    CmdState = "状态提示...",
                    DataCountPerUnitMin = 1,
                    DataCountPerUnitMax = 4,
                    DataCountPerUnit = 4,
                    DataSingleCount = 234
                });
            _getCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "11H : 指定的超时驾驶记录",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Visible,
                    CmdState = "状态提示...",
                    DataCountPerUnitMin = 1,
                    DataCountPerUnitMax = 19,
                    DataCountPerUnit = 19,
                    DataSingleCount = 50
                });
            _getCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "12H : 指定的驾驶人身份记录",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Visible,
                    CmdState = "状态提示...",
                    DataCountPerUnitMin = 1,
                    DataCountPerUnitMax = 39,
                    DataCountPerUnit = 39,
                    DataSingleCount = 25
                });
            _getCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "13H : 指定的外部供电记录",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Visible,
                    CmdState = "状态提示...",
                    DataCountPerUnitMin = 1,
                    DataCountPerUnitMax = 141,
                    DataCountPerUnit = 141,
                    DataSingleCount = 7
                });
            _getCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "14H : 指定的参数修改点记录",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Visible,
                    CmdState = "状态提示...",
                    DataCountPerUnitMin = 1,
                    DataCountPerUnitMax = 141,
                    DataCountPerUnit = 141,
                    DataSingleCount = 7
                });
            _getCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "15H : 指定的速度状态日志",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Visible,
                    CmdState = "状态提示...",
                    DataCountPerUnitMin = 1,
                    DataCountPerUnitMax = 7,
                    DataCountPerUnit = 7,
                    DataSingleCount = 133
                });

            #endregion

            #region Set Cmd

            _setCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "82H : 车辆信息",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Visible,
                    CmdState = "状态提示..."
                });
            _setCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "83H : 记录仪初次安装日期",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Visible,
                    CmdState = "状态提示..."
                });
            _setCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "84H : 状态量配置信息",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Visible,
                    CmdState = "状态提示..."
                });
            _setCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "C2H : 记录仪时间",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Visible,
                    CmdState = "状态提示..."
                });
            _setCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "C3H : 记录仪脉冲系数",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Visible,
                    CmdState = "状态提示..."
                });
            _setCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "C4H : 初始里程",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Visible,
                    CmdState = "状态提示..."
                });

            #endregion

            #region Chk Cmd

            _chkCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "E0H : 进入检定状态",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Collapsed
                });
            _chkCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "E1H : 进入里程误差测量",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Collapsed
                });
            _chkCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "E2H : 进入脉冲系数误差测量",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Collapsed
                });
            _chkCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "E3H : 进入实时时间程误差测量",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Collapsed
                });
            _chkCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "E4H : 返回正常工作状态",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Collapsed
                });

            #endregion

            #region Ext Get Cmd

            _extGetCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "20H : 采集传感器单圈脉冲个数",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Collapsed,
                    CmdState = "状态提示..."
                });
            _extGetCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "21H : 采集信号量状态配置",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Collapsed,
                    CmdState = "状态提示..."
                });

            #endregion

            #region Ext Set Cmd

            _extSetCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "D0H : 状态量配置",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Visible,
                    CmdState = "状态提示..."
                });
            _extSetCmdOc.Add(
                new CmdDefinition()
                {
                    CmdContent = "D1H : 传感器单圈脉冲数",
                    CmdSelected = false,
                    GetCmdSetParVisibility = Visibility.Visible,
                    CmdState = "状态提示..."
                });

            #endregion

            #endregion

            _ctsDisplay = new CancellationTokenSource();
        }

        #region Window Exit

        private void Window_Exit(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确认退出\"GB/T 19056-2013数据分析软件\"?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _normalClose = true;

            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_normalClose == false)
            {
                if (MessageBox.Show("确认退出\"GB/T 19056-2013数据分析软件\"?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    e.Cancel = true;
            }

            if (e.Cancel == false)
            {
                SaveConfig();

                if (_ctsDisplay != null)
                    _ctsDisplay.Cancel();
                if (_cts != null)
                    _cts.Cancel();
                try
                {
                    _serialPortTask.Wait(10000, _cts.Token);
                }
                catch (Exception) { }
                try
                {
                    _displayLogTask.Wait(1000, _ctsDisplay.Token);
                }
                catch (Exception) { }
                if (_sPort != null)
                {
                    try
                    {
                        _sPort.Close();
                        _sPort.Dispose();
                    }
                    catch (Exception) { }
                }
            }

            base.OnClosing(e);

            if (e.Cancel == false)
                System.Environment.Exit(0);
        }

        #endregion

        private void GetCmdSetPar_Button_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null)
                return;
            CmdDefinition cd = btn.DataContext as CmdDefinition;
            if (cd == null)
                return;
            GetCmdConfiguration gcc = new GetCmdConfiguration(cd.CmdContent,
                cd.StartDateTime, cd.StopDateTime,
                cd.DataCountPerUnitMin, cd.DataCountPerUnitMax, cd.DataCountPerUnit);
            bool? b = gcc.ShowDialog();
            if (b == true)
            {
                cd.CmdState = "设置完成.";
                cd.StartDateTime = gcc.StartDateTime;
                cd.StopDateTime = gcc.StopDateTime;
                foreach (CmdDefinition cdi in _getCmdOc)
                {
                    cdi.StartDateTime = gcc.StartDateTime;
                    cdi.StopDateTime = gcc.StopDateTime;
                }
                cd.DataCountPerUnit = gcc.UnitData;
            }
            else
            {
                cd.CmdState = "取消设置.";
            }
        }

        private void RefreshSerialPort_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            DisconnectSerialPort(true);

            OpenSerialPort();
        }

        private void ConfigSerialPort_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            SerialPortConfiguration spc = new SerialPortConfiguration(Port, Baud, Parity, DataBit, StartBit, StopBit);
            bool? b = spc.ShowDialog();
            if (b == true)
            {
                Port = spc.SelectedSerialPort;
                Baud = spc.SelectedBaud;
                Parity = spc.SelectedParity;
                DataBit = spc.SelectedDataBit;
                StartBit = spc.SelectedStartBit;
                StopBit = spc.SelectedStopBit;
                SaveConfig();

                DisconnectSerialPort(true);

                OpenSerialPort();
            }
        }

        private void DisconnectSerialPort_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            DisconnectSerialPort();

            LogMessageSeperator();
        }

        private void ConfigTimeout_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            TimeoutConfiguration tc = new TimeoutConfiguration(TimeOut, CmdInterval, WriteReadInterval);
            bool? b = tc.ShowDialog();
            if (b == true)
            {
                TimeOut = tc.TimeOut.ToString();
                CmdInterval = tc.CmdInterval.ToString();
                WriteReadInterval = tc.WriteReadInterval.ToString();
                SaveConfig();

                DisconnectSerialPort(true);

                OpenSerialPort();
            }
        }

        private void ConfigServer_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            ServerConfiguration sc = new ServerConfiguration(ServerIP, ServerPort);
            bool? b = sc.ShowDialog();
            if (b == true)
            {
                ServerIP = sc.ServerIP;
                ServerPort = sc.ServerPort.ToString();
                SaveConfig();
            }
        }

        private void GetSetCmdSetPar_CheckBox_CheckedUnchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cbox = sender as CheckBox;
            if (cbox == null)
                return;
            CmdDefinition cd = cbox.DataContext as CmdDefinition;
            if (cd == null)
                return;
            if (cbox.IsChecked != true)
                cd.CmdSelected = false;
            else
            {
                cd.CmdSelected = true;
                CheckModeChecked = false;
            }
        }

        private void SetCmdSelectAll_Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (CmdDefinition cdi in _setCmdOc)
            {
                cdi.CmdSelected = true;
            }
        }

        private void SetCmdReverseAll_Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (CmdDefinition cdi in _setCmdOc)
            {
                cdi.CmdSelected = !cdi.CmdSelected;
            }
        }

        private void SetCmdUnselectAll_Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (CmdDefinition cdi in _setCmdOc)
            {
                cdi.CmdSelected = false;
            }
        }

        private void GetCmdSelectAll_Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (CmdDefinition cdi in _getCmdOc)
            {
                cdi.CmdSelected = true;
            }
        }

        private void GetCmdReverseAll_Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (CmdDefinition cdi in _getCmdOc)
            {
                cdi.CmdSelected = !cdi.CmdSelected;
            }
        }

        private void GetCmdUnselectAll_Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (CmdDefinition cdi in _getCmdOc)
            {
                cdi.CmdSelected = false;
            }
        }

        private void SetCmdSetPar_Button_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null)
                return;
            CmdDefinition cd = btn.DataContext as CmdDefinition;
            if (cd == null)
                return;
            bool? b = false;
            switch (cd.CmdContent)
            {
                default:
                    break;
                case "82H : 车辆信息":
                    VehicleInformation vi = new VehicleInformation(cd.VehicleIDCode, cd.VehicleNumberCode, cd.VehicleNumberCategory);
                    b = vi.ShowDialog();
                    if (b == true)
                    {
                        cd.VehicleIDCode = vi.VehicleIDCode;
                        cd.VehicleNumberCode = vi.VehicleNumberCode;
                        cd.VehicleNumberCategory = vi.VehicleNumberCategory;
                    }
                    break;
                case "83H : 记录仪初次安装日期":
                    Recorder1stInstallDateTime r1idt = new Recorder1stInstallDateTime(cd.FirstInstallDateTime);
                    b = r1idt.ShowDialog();
                    if (b == true)
                        cd.FirstInstallDateTime = r1idt.FirstInstallDateTime;
                    break;
                case "84H : 状态量配置信息":
                    cd.D2 = D2;
                    cd.D1 = D1;
                    cd.D0 = D0;
                    StateConfigureInformation sci = new StateConfigureInformation(cd.D2, cd.D1, cd.D0);
                    b = sci.ShowDialog();
                    if (b == true)
                    {
                        cd.D2 = sci.D2;
                        cd.D1 = sci.D1;
                        cd.D0 = sci.D0;
                        D2 = cd.D2;
                        D1 = cd.D1;
                        D0 = cd.D0;
                    }
                    break;
                case "C2H : 记录仪时间":
                    RecorderDateTime rdt = new RecorderDateTime(cd.IsSystemModeDateTime, cd.UserModeDateTime);
                    b = rdt.ShowDialog();
                    if (b == true)
                    {
                        cd.IsSystemModeDateTime = rdt.IsSystemModeDateTime;
                        cd.SystemModeDateTime = rdt.SystemModeDateTime;
                        cd.UserModeDateTime = rdt.UserModeDateTime;
                    }
                    break;
                case "C3H : 记录仪脉冲系数":
                    RecorederPulseCoefficient rpc = new RecorederPulseCoefficient(cd.PulseCoefficient);
                    b = rpc.ShowDialog();
                    if (b == true)
                        cd.PulseCoefficient = rpc.PulseCoefficient;
                    break;
                case "C4H : 初始里程":
                    InitialDistance id = new InitialDistance(cd.InitialDistanceValue);
                    b = id.ShowDialog();
                    if (b == true)
                        cd.InitialDistanceValue = id.InitialDistanceValue;
                    break;
            }
            if (b == true)
            {
                cd.CmdState = "设置完成.";
            }
            else
            {
                cd.CmdState = "取消设置.";
            }
        }

        private void ChkCmd_RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb == null)
                return;
            CmdDefinition cd = rb.DataContext as CmdDefinition;
            if (cd == null)
                return;
            foreach (CmdDefinition cdi in _chkCmdOc)
            {
                if (cd == cdi)
                    continue;
                else
                    cdi.CmdSelected = false;
            }
            switch (cd.CmdContent)
            {
                default:
                    break;
                case "E0H : 进入检定状态":
                    CheckCmdState = "进入检定.";
                    break;
                case "E1H : 进入里程误差测量":
                    CheckCmdState = "检定里程.";
                    break;
                case "E2H : 进入脉冲系数误差测量":
                    CheckCmdState = "检定脉冲.";
                    break;
                case "E3H : 进入实时时间程误差测量":
                    CheckCmdState = "检定时钟.";
                    break;
                case "E4H : 返回正常工作状态":
                    CheckCmdState = "检定返回.";
                    break;
            }
        }

        private void ExtSetCmdSetPar_Button_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null)
                return;
            CmdDefinition cd = btn.DataContext as CmdDefinition;
            if (cd == null)
                return;
            bool? b = false;
            switch (cd.CmdContent)
            {
                default:
                    break;
                case "D0H : 状态量配置":
                    StateConfiguration sc = new StateConfiguration(D2, D1, D0, cd.D7State, cd.D6State,
                        cd.D5State, cd.D4State, cd.D3State, cd.D2State, cd.D1State, cd.D0State);
                    b = sc.ShowDialog();
                    if (b == true)
                    {
                        cd.D7State = sc.D7State;
                        cd.D6State = sc.D6State;
                        cd.D5State = sc.D5State;
                        cd.D4State = sc.D4State;
                        cd.D3State = sc.D3State;
                        cd.D2State = sc.D2State;
                        cd.D1State = sc.D1State;
                        cd.D0State = sc.D0State;
                    }
                    break;
                case "D1H : 传感器单圈脉冲数":
                    SinglePulseCount spc = new SinglePulseCount(cd.SinglePulseCnt);
                    b = spc.ShowDialog();
                    if (b == true)
                        cd.SinglePulseCnt = spc.PulseCount;
                    break;
            }
            if (b == true)
            {
                cd.CmdState = "设置完成.";
            }
            else
            {
                cd.CmdState = "取消设置.";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Process[] ps = Process.GetProcesses();
            //foreach (Process pi in ps)
            //{
            //    if (string.Compare(pi.ProcessName, Assembly.GetExecutingAssembly().GetName().Name, true) == 0)
            //    {
            //        MessageBox.Show("\"GB/T 19056-2013数据分析软件\"已经在运行中.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            //        System.Environment.Exit(0);
            //    }
            //}

            LoadConfig();

            OpenSerialPort();

            _displayLogTask = Task.Factory.StartNew(new Action(DisplayLogHandler), _ctsDisplay.Token);

            _timerPBar = new Timer(new TimerCallback(PBarTimerCallBackHandler), null, Timeout.Infinite, 100);

            AlreadyEnterCheck = false;

        }

        private void PBarTimerCallBackHandler(object obj)
        {
            if (PBarValue + PBarStep <= PBarMaximum)
                PBarValue = PBarValue + PBarStep;
        }

        private void OpenSerialPort()
        {
            if (string.IsNullOrWhiteSpace(Port) == false)
            {
                try
                {
                    _sPort = new SerialPort();
                    _sPort.PortName = Port;
                    _sPort.BaudRate = int.Parse(Baud);
                    if (string.Compare(Parity, "Odd", true) == 0)
                        _sPort.Parity = System.IO.Ports.Parity.Odd;
                    else if (string.Compare(Parity, "Even", true) == 0)
                        _sPort.Parity = System.IO.Ports.Parity.Even;
                    else
                        _sPort.Parity = System.IO.Ports.Parity.None;
                    _sPort.DataBits = int.Parse(DataBit);
                    if (string.Compare(StopBit, "1", true) == 0)
                        _sPort.StopBits = StopBits.One;
                    else if (string.Compare(StopBit, "1.5", true) == 0)
                        _sPort.StopBits = StopBits.OnePointFive;
                    else
                        _sPort.StopBits = StopBits.Two;
                    _sPort.WriteTimeout = int.Parse(TimeOut);
                    _sPort.ReadTimeout = int.Parse(TimeOut);
                    _sPort.Open();
                    if (_sPort.IsOpen)
                    {
                        LogMessageInformation("成功打开串口" + Port + ".");

                        ReadyString = "端口号:" + Port + " 波特率:" + Baud + " 校验位:" + Parity;
                    }
                    else
                    {
                        LogMessageError("无法打开串口" + Port + ".");

                        ReadyString = "串口已关闭.";
                    }
                }
                catch (Exception ex)
                {
                    _sPort = null;
                    LogMessageError("无法打开串口" + Port + ".\n" + ex.Message);

                    ReadyString = "串口已关闭.";
                }
            }
            else
            {
                LogMessageError("计算机无可用串口.");

                ReadyString = "无可用串口.";
            }

            LogMessageSeperator();
        }

        private void DisconnectSerialPort(bool isRefresh = false)
        {
            if (_sPort != null)
            {
                try
                {
                    _sPort.Close();
                    _sPort.Dispose();
                    _sPort = null;

                    LogMessageInformation("成功关闭串口" + Port + ".");
                }
                catch (Exception ex)
                {
                    _sPort = null;

                    LogMessageError("关闭串口" + Port + "出现错误.\n" + ex.Message);
                }
            }
            else
            {
                if(isRefresh == false)
                    LogMessageInformation("无打开的串口.");
            }

            ReadyString = "串口已关闭.";
        }

        #region Log

        private void LogMessageSeperator(bool startBlank = true)
        {
            if(startBlank == true)
                LogMessage("");
            LogMessage("".PadRight(100, '='), bold:true);
            LogMessage("");
        }

        private void LogMessageSubSeperator()
        {
            LogMessage("");
            LogMessage("".PadRight(100, '-'));
            LogMessage("");
        }

        private void LogMessageInformation(string msg)
        {
            LogMessage(msg, LogType.Information);
        }

        private void LogMessageTitle(string msg)
        {
            LogMessage(msg, LogType.Common, true);
        }

        private void LogMessageError(string msg)
        {
            LogMessage(msg, LogType.Error);
        }

        private void LogMessageLink(string link)
        {
            LogMessage(link, LogType.Information, false, link);
        }

        private void LogMessage(string msg, LogType lt = LogType.Common, bool bold = false, string link = null)
        {
            //lock (_logLock)
            {
                if (_ctsDisplay.IsCancellationRequested == true)
                {
                    _logQueue.Clear();
                    return;
                }

                _logQueue.Enqueue(new Tuple<string, LogType, bool, string>(msg, lt, bold, link));
            }
        }

        private void DisplayLogHandler()
        {
            while (_ctsDisplay.IsCancellationRequested == false)
            {
                //lock (_logLock)
                //{
                    //if (_ctsDisplay.IsCancellationRequested == true)
                    //{
                    //    _logQueue.Clear();
                    //    return;
                    //}

                    if (_logQueue.Count > 0)
                    {
                        Tuple<string, LogType, bool,string> di = _logQueue.Dequeue();

                        Dispatcher.BeginInvoke((ThreadStart)delegate
                        {
                            #region

                            while (fldocLog.Blocks.Count > MAX_DISPLAY_LINE_COUNT)
                            {
                                fldocLog.Blocks.Remove(fldocLog.Blocks.FirstBlock);
                            }
                            
                            Run rch = new Run(di.Item1);
                            WinParagraph pch = new WinParagraph(rch);
                            switch (di.Item2)
                            {
                                default:
                                case LogType.Common:
                                    pch.Foreground = Brushes.Black;
                                    break;
                                case LogType.Information:
                                    pch.Foreground = Brushes.Blue;
                                    break;
                                case LogType.Error:
                                    pch.Foreground = Brushes.Red;
                                    break;
                            }
                            if (string.IsNullOrWhiteSpace(di.Item4) == false)
                            {
                                pch.Foreground = Brushes.Green;
                                //pch.TextDecorations.Add(new TextDecoration());
                                pch.MouseEnter += new MouseEventHandler(ReportHyperLink_MouseEnter);
                                pch.MouseLeave += new MouseEventHandler(ReportHyperLink_MouseLeave);
                                pch.MouseDown += new MouseButtonEventHandler(ReportHyperLink_MouseDown);
                                rch.FontStyle = FontStyles.Italic;
                            }
                            if (di.Item3 == true)
                                pch.FontWeight = FontWeights.Bold;
                            else
                                pch.FontWeight = FontWeights.Regular;
                            fldocLog.Blocks.Add(pch);
                            rtxtLog.ScrollToEnd();

                            #endregion
                        }, null);
                    }
                //}

                Thread.Sleep(10);
            }
        }

        private void ReportHyperLink_MouseLeave(object sender, MouseEventArgs e)
        {
            WinParagraph par = sender as WinParagraph;
            par.Cursor = Cursors.UpArrow;
        }

        private void ReportHyperLink_MouseEnter(object sender, MouseEventArgs e)
        {
            WinParagraph par = sender as WinParagraph;
            par.Cursor = Cursors.Hand;
        }

        private void ReportHyperLink_MouseDown(object sender, MouseEventArgs e)
        {
            WinParagraph par = sender as WinParagraph;
            string str = ((Run)(par.Inlines.FirstInline)).Text;
            //int idx = str.LastIndexOf("\\");
            //System.Diagnostics.Process.Start(str.Substring(0, idx));
            System.Diagnostics.Process.Start(str);
        }

        #endregion

        #region Save & Log Config

        private void SaveConfig()
        {
            try
            {
                StreamWriter sw = new StreamWriter("config.xml", false);
                sw.WriteLine("<bumblebee>");
                sw.WriteLine("    <port>" + Port + "</port>");
                sw.WriteLine("    <baud>" + Baud + "</baud>");
                sw.WriteLine("    <parity>" + Parity + "</parity>");
                sw.WriteLine("    <data>" + DataBit + "</data>");
                sw.WriteLine("    <start>" + StartBit + "</start>");
                sw.WriteLine("    <stop>" + StopBit + "</stop>");
                sw.WriteLine("    <timeout>" + TimeOut + "</timeout>");
                sw.WriteLine("    <cmdintvl>" + CmdInterval + "</cmdintvl>");
                sw.WriteLine("    <wrintvl>" + WriteReadInterval + "</wrintvl>");
                sw.WriteLine("    <serverip>" + ServerIP + "</serverip>");
                sw.WriteLine("    <serverport>" + ServerPort + "</serverport>");
                sw.WriteLine("</bumblebee>");
                sw.Flush();
                sw.Close();
                sw.Dispose();

                LogMessage("成功保存配置文件.", LogType.Information);
            }
            catch (Exception ex)
            {
                LogMessage("保存配置文件出现错误.\n" + ex.Message, LogType.Error);
            }

            LogMessageInformation("当前串口号:" + Port + ".");
            LogMessageInformation("当前串口波特率:" + Baud + ".");
            LogMessageInformation("当前串口校验位:" + Parity + ".");
            LogMessageInformation("当前串口数据位:" + DataBit + ".");
            LogMessageInformation("当前串口开始位:" + StartBit + ".");
            LogMessageInformation("当前串口停止位:" + StopBit + ".");
            LogMessageInformation("当前串口超时时间:" + TimeOut + "ms.");
            LogMessageInformation("当前串口命令间隔时间:" + CmdInterval + "ms.");
            LogMessageInformation("当前串口读写间隔时间:" + WriteReadInterval + "ms.");
            LogMessageInformation("当前服务器IP:" + ServerIP + ".");
            LogMessageInformation("当前服务器端口:" + ServerPort + ".");

            LogMessageSeperator();
        }

        private void LoadConfig()
        {
            if (File.Exists("config.xml"))
            {
                try
                {
                    _xd.Load("config.xml");
                    XmlNodeList xnl = _xd.ChildNodes;
                    if (xnl.Count != 1)
                    {
                        LogMessage("加载配置文件出现错误,重置配置文件.", LogType.Error);
                        CreateNewConfig(false);
                        string[] ps = SerialPort.GetPortNames();
                        if (ps == null || ps.Length < 1)
                        {
                            Port = "";
                        }
                        else
                        {
                            Port = ps[0];
                        }
                        Baud = _bauds[0];
                        Parity = _parities[0];
                        DataBit = _dataBits[0];
                        StartBit = "1";
                        StopBit = _stopBits[0];
                        TimeOut = "1000";
                        ServerIP = "127.0.0.1";
                        ServerPort = "8678";
                    }
                    else
                    {
                        XmlNodeList xnls = xnl[0].ChildNodes;
                        foreach (XmlNode xni in xnls)
                        {
                            switch (xni.Name.ToUpper().Trim())
                            {
                                default:
                                    break;
                                case "PORT":
                                    #region

                                    if (xni.InnerText != null)
                                    {
                                        Port = xni.InnerText;
                                        if (string.IsNullOrWhiteSpace(Port))
                                        {
                                            LogMessageError("配置文件中串口端口号项为空.");

                                            string[] ps = SerialPort.GetPortNames();
                                            if (ps == null || ps.Length < 1)
                                            {
                                                LogMessageError("计算机无可用串口.");
                                            }
                                            else
                                            {
                                                LogMessageError("使用默认串口端口号:" + ps[0] + ".");
                                                Port = ps[0];
                                            }
                                        }
                                        else
                                        {
                                            string[] ps = SerialPort.GetPortNames();
                                            if (ps == null || ps.Length < 1)
                                            {
                                                LogMessageError("计算机无可用串口.");
                                            }
                                            else
                                            {
                                                bool found = false;
                                                foreach (string pi in ps)
                                                {
                                                    if (string.Compare(pi.ToUpper().Trim(), Port.ToUpper().Trim()) == 0)
                                                    {
                                                        found = true;
                                                        break;
                                                    }
                                                }
                                                if (found == false)
                                                {
                                                    LogMessageError("配置文件中串口端口号项(" + Port + ")不正确,使用默认串口端口号:" + ps[0] + ".");
                                                    Port = ps[0];
                                                }
                                                else
                                                {
                                                    LogMessageInformation("当前串口端口号:" + Port + ".");
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LogMessageError("配置文件缺少串口端口号项.");

                                        string[] ps = SerialPort.GetPortNames();
                                        if (ps == null || ps.Length < 1)
                                        {
                                            LogMessageError("计算机无可用串口.");
                                        }
                                        else
                                        {
                                            LogMessageError("使用默认串口端口号:" + ps[0] + ".");
                                            Port = ps[0];
                                        }
                                    }
                                    xni.InnerText = Port;

                                    #endregion
                                    break;
                                case "BAUD":
                                    #region

                                    if (xni.InnerText != null)
                                    {
                                        Baud = xni.InnerText;
                                        if (string.IsNullOrWhiteSpace(Baud))
                                        {
                                            LogMessageError("配置文件中串口波特率项为空.");

                                            LogMessageError("使用默认串口波特率:" + _bauds[0] + ".");
                                            Baud = _bauds[0];
                                        }
                                        else
                                        {
                                            bool found = false;
                                            foreach (string bi in _bauds)
                                            {
                                                if (string.Compare(bi.ToUpper().Trim(), Baud.ToUpper().Trim()) == 0)
                                                {
                                                    found = true;
                                                    break;
                                                }
                                            }
                                            if (found == false)
                                            {
                                                LogMessageError("配置文件中串口波特率项(" + Baud + ")不正确,使用默认串口波特率:" + _bauds[0] + ".");
                                                Baud = _bauds[0];
                                            }
                                            else
                                            {
                                                LogMessageInformation("当前串口波特率:" + Baud + ".");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LogMessageError("配置文件缺少串口波特率项.");

                                        LogMessageError("使用默认串口波特率:" + _bauds[0] + ".");
                                        Baud = _bauds[0];
                                    }
                                    xni.InnerText = Baud;

                                    #endregion
                                    break;
                                case "PARITY":
                                    #region

                                    if (xni.InnerText != null)
                                    {
                                        Parity = xni.InnerText;
                                        if (string.IsNullOrWhiteSpace(Parity))
                                        {
                                            LogMessageError("配置文件中串口校验位项为空.");
                                        }
                                        else
                                        {
                                            bool found = false;
                                            foreach (string bi in _parities)
                                            {
                                                if (string.Compare(bi.ToUpper().Trim(), Parity.ToUpper().Trim()) == 0)
                                                {
                                                    found = true;
                                                    break;
                                                }
                                            }
                                            if (found == false)
                                            {
                                                LogMessageError("配置文件中串口校验位项(" + Parity + ")不正确,使用默认串口校验位:" + _parities[0] + ".");
                                                Parity = _parities[0];
                                            }
                                            else
                                            {
                                                LogMessageInformation("当前串口校验位:" + Parity + ".");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LogMessageError("配置文件缺少串口校验位项.");

                                        LogMessageError("使用默认串口校验位:" + _parities[0] + ".");
                                        Parity = _parities[0];
                                    }
                                    xni.InnerText = Parity;

                                    #endregion
                                    break;
                                case "DATA":
                                    #region

                                    if (xni.InnerText != null)
                                    {
                                        DataBit = xni.InnerText;
                                        if (string.IsNullOrWhiteSpace(DataBit))
                                        {
                                            LogMessageError("配置文件中串口数据位项为空.");
                                        }
                                        else
                                        {
                                            bool found = false;
                                            foreach (string bi in _dataBits)
                                            {
                                                if (string.Compare(bi.ToUpper().Trim(), DataBit.ToUpper().Trim()) == 0)
                                                {
                                                    found = true;
                                                    break;
                                                }
                                            }
                                            if (found == false)
                                            {
                                                LogMessageError("配置文件中串口数据位项(" + DataBit + ")不正确,使用默认串口数据位:" + _dataBits[0] + ".");
                                                DataBit = _dataBits[0];
                                            }
                                            else
                                            {
                                                LogMessageInformation("当前串口数据位:" + DataBit + ".");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LogMessageError("配置文件缺少串口数据位项.");

                                        LogMessageError("使用默认串口数据位:" + _dataBits[0] + ".");
                                        DataBit = _dataBits[0];
                                    }
                                    xni.InnerText = DataBit;

                                    #endregion
                                    break;
                                case "START":
                                    #region

                                    if (xni.InnerText != null)
                                    {
                                        StartBit = xni.InnerText;
                                        if (string.IsNullOrWhiteSpace(StartBit))
                                        {
                                            LogMessageError("配置文件中串口起始位项为空.");
                                        }
                                        else
                                        {
                                            if (StartBit != "1")
                                            {
                                                LogMessageError("配置文件中串口起始位项(" + StartBit + ")不正确,使用默认串口起始位:1.");
                                                StartBit = "1";
                                            }
                                            else
                                            {
                                                LogMessageInformation("当前串口起始位:" + StartBit + ".");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LogMessageError("配置文件缺少串口起始位项.");

                                        LogMessageError("使用默认串口起始位:1.");
                                        StartBit = "1";
                                    }
                                    xni.InnerText = StartBit;

                                    #endregion
                                    break;
                                case "STOP":
                                    #region

                                    if (xni.InnerText != null)
                                    {
                                        StopBit = xni.InnerText;
                                        if (string.IsNullOrWhiteSpace(StopBit))
                                        {
                                            LogMessageError("配置文件中串口停止位项为空.");
                                        }
                                        else
                                        {
                                            bool found = false;
                                            foreach (string bi in _stopBits)
                                            {
                                                if (string.Compare(bi.ToUpper().Trim(), StopBit.ToUpper().Trim()) == 0)
                                                {
                                                    found = true;
                                                    break;
                                                }
                                            }
                                            if (found == false)
                                            {
                                                LogMessageError("配置文件中串口停止位项(" + StopBit + ")不正确,使用默认串口停止位:" + _stopBits[0] + ".");
                                                StopBit = _stopBits[0];
                                            }
                                            else
                                            {
                                                LogMessageInformation("当前串口停止位:" + StopBit + ".");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LogMessageError("配置文件缺少串口停止位项.");

                                        LogMessageError("使用默认串口停止位:" + _stopBits[0] + ".");
                                        StopBit = _stopBits[0];
                                    }
                                    xni.InnerText = StopBit;

                                    #endregion
                                    break;
                                case "TIMEOUT":
                                    #region

                                    if (xni.InnerText != null)
                                    {
                                        TimeOut = xni.InnerText;
                                        if (string.IsNullOrWhiteSpace(TimeOut))
                                        {
                                            LogMessageError("配置文件中串口超时时间项为空.");
                                        }
                                        else
                                        {
                                            int timeout = -1;
                                            if (int.TryParse(TimeOut, out timeout) == false)
                                            {
                                                LogMessageError("配置文件中串口超时时间(" + TimeOut + ")不正确,使用默认串口超时时间:1000ms.");
                                                TimeOut = "1000";
                                            }
                                            else
                                            {
                                                if (timeout < 1000)
                                                {
                                                    LogMessageError("配置文件中串口超时时间(" + TimeOut + ")不正确,使用默认串口超时时间:1000ms.");
                                                    TimeOut = "1000";
                                                }
                                                else if (timeout > 120000)
                                                {
                                                    LogMessageError("配置文件中串口超时时间(" + TimeOut + ")不正确,使用默认串口超时时间:120000ms.");
                                                    TimeOut = "120000";
                                                }
                                                else
                                                {
                                                    LogMessageInformation("当前串口超时时间:" + TimeOut + "ms.");
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LogMessageError("配置文件缺少串口超时时间项.");

                                        LogMessageError("使用默认串口超时时间:1000ms.");
                                        TimeOut = "1000";
                                    }
                                    xni.InnerText = TimeOut;

                                    #endregion
                                    break;
                                case "CMDINTVL":
                                    #region

                                    if (xni.InnerText != null)
                                    {
                                        CmdInterval = xni.InnerText;
                                        if (string.IsNullOrWhiteSpace(CmdInterval))
                                        {
                                            LogMessageError("配置文件中串口命令间隔时间项为空.");
                                        }
                                        else
                                        {
                                            int timeout = -1;
                                            if (int.TryParse(CmdInterval, out timeout) == false)
                                            {
                                                LogMessageError("配置文件中串口命令间隔时间(" + CmdInterval + ")不正确,使用默认串口命令间隔时间:1000ms.");
                                                CmdInterval = "1000";
                                            }
                                            else
                                            {
                                                if (timeout < 1000)
                                                {
                                                    LogMessageError("配置文件中串口命令间隔时间(" + CmdInterval + ")不正确,使用默认串口命令间隔时间:1000ms.");
                                                    CmdInterval = "1000";
                                                }
                                                else if (timeout > 10000)
                                                {
                                                    LogMessageError("配置文件中串口命令间隔时间(" + CmdInterval + ")不正确,使用默认串口命令间隔时间:10000ms.");
                                                    CmdInterval = "10000";
                                                }
                                                else
                                                {
                                                    LogMessageInformation("当前串口命令间隔时间:" + CmdInterval + "ms.");
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LogMessageError("配置文件缺少串口命令间隔时间项.");

                                        LogMessageError("使用默认串口命令间隔时间:1000ms.");
                                        CmdInterval = "1000";
                                    }
                                    xni.InnerText = CmdInterval;

                                    #endregion
                                    break;
                                case "WRINTVL":
                                    #region

                                    if (xni.InnerText != null)
                                    {
                                        WriteReadInterval = xni.InnerText;
                                        if (string.IsNullOrWhiteSpace(WriteReadInterval))
                                        {
                                            LogMessageError("配置文件中串口读写间隔时间项为空.");
                                        }
                                        else
                                        {
                                            int timeout = -1;
                                            if (int.TryParse(WriteReadInterval, out timeout) == false)
                                            {
                                                LogMessageError("配置文件中串口读写间隔时间(" + WriteReadInterval + ")不正确,使用默认串口读写间隔时间:1000ms.");
                                                TimeOut = "1000";
                                            }
                                            else
                                            {
                                                if (timeout < 1000)
                                                {
                                                    LogMessageError("配置文件中串口读写间隔时间(" + WriteReadInterval + ")不正确,使用默认串口读写间隔时间:1000ms.");
                                                    WriteReadInterval = "1000";
                                                }
                                                else if (timeout > 10000)
                                                {
                                                    LogMessageError("配置文件中串口读写间隔时间(" + WriteReadInterval + ")不正确,使用默认串口读写间隔时间:10000ms.");
                                                    WriteReadInterval = "10000";
                                                }
                                                else
                                                {
                                                    LogMessageInformation("当前串口读写间隔时间:" + WriteReadInterval + "ms.");
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LogMessageError("配置文件缺少串口读写间隔时间项.");

                                        LogMessageError("使用默认串口读写间隔时间:1000ms.");
                                        WriteReadInterval = "1000";
                                    }
                                    xni.InnerText = WriteReadInterval;

                                    #endregion
                                    break;
                                case "SERVERIP":
                                    #region

                                    if (xni.InnerText != null)
                                    {
                                        ServerIP = xni.InnerText;
                                        if (string.IsNullOrWhiteSpace(ServerIP))
                                        {
                                            LogMessageError("配置文件中服务器IP项为空.");
                                        }
                                        else
                                        {
                                            IPAddress timeout = null;
                                            if (IPAddress.TryParse(ServerIP, out timeout) == false)
                                            {
                                                LogMessageError("配置文件中服务器IP(" + ServerIP + ")不正确,使用默认服务器IP:127.0.0.1.");
                                                TimeOut = "127.0.0.1";
                                            }
                                            else
                                            {
                                                LogMessageInformation("当前服务器IP:" + ServerIP + ".");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LogMessageError("配置文件缺少服务器IP项.");

                                        LogMessageError("使用默认服务器IP:127.0.0.1.");
                                        ServerIP = "127.0.0.1";
                                    }
                                    xni.InnerText = ServerIP;

                                    #endregion
                                    break;
                                case "SERVERPORT":
                                    #region

                                    if (xni.InnerText != null)
                                    {
                                        ServerPort = xni.InnerText;
                                        if (string.IsNullOrWhiteSpace(ServerPort))
                                        {
                                            LogMessageError("配置文件中服务器端口项为空.");
                                        }
                                        else
                                        {
                                            int timeout = -1;
                                            if (int.TryParse(ServerPort, out timeout) == false)
                                            {
                                                LogMessageError("配置文件中服务器端口(" + ServerPort + ")不正确,使用默认服务器端口:8678.");
                                                TimeOut = "8678";
                                            }
                                            else
                                            {
                                                if (timeout < 1)
                                                {
                                                    LogMessageError("配置文件中服务器端口(" + ServerPort + ")不正确,使用默认服务器端口:8678.");
                                                    ServerPort = "8678";
                                                }
                                                else
                                                {
                                                    LogMessageInformation("当前服务器端口:" + ServerPort + ".");
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LogMessageError("配置文件缺少服务器端口项.");

                                        LogMessageError("使用默认服务器端口:8678.");
                                        ServerPort = "8678";
                                    }
                                    xni.InnerText = ServerPort;

                                    #endregion
                                    break;
                            }
                        }

                        LogMessage("成功加载配置文件.", LogType.Information);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage("加载配置文件出现错误,重置配置文件.\n" + ex.Message, LogType.Error);
                    CreateNewConfig(false);
                    string[] ps = SerialPort.GetPortNames();
                    if (ps == null || ps.Length < 1)
                    {
                        Port = "";
                    }
                    else
                    {
                        Port = ps[0];
                    }
                    Baud = _bauds[0];
                    Parity = _parities[0];
                    DataBit = _dataBits[0];
                    StartBit = "1";
                    StopBit = _stopBits[0];
                    TimeOut = "1000";
                    WriteReadInterval = "1000";
                    CmdInterval = "1000";
                    TimeOut = "1000";
                    ServerIP = "127.0.0.1";
                    ServerPort = "8678";

                    LogMessageInformation("当前串口端口号:" + Port + ".");
                    LogMessageInformation("当前串口波特率:" + Baud + ".");
                    LogMessageInformation("当前串口校验位:" + Parity + ".");
                    LogMessageInformation("当前串口数据位:" + DataBit + ".");
                    LogMessageInformation("当前串口开始位:" + StartBit + ".");
                    LogMessageInformation("当前串口停止位:" + StopBit + ".");
                    LogMessageInformation("当前串口超时时间:" + TimeOut + "ms.");
                    LogMessageInformation("当前串口命令间隔时间:" + CmdInterval + "ms.");
                    LogMessageInformation("当前串口读写间隔时间:" + WriteReadInterval + "ms.");
                    LogMessageInformation("当前服务器IP:" + ServerIP + ".");
                    LogMessageInformation("当前服务器端口:" + ServerPort + ".");
                }
            }
            else
            {
                LogMessage("无配置文件,创建新配置文件.", LogType.Information);
                CreateNewConfig();
                string[] ps = SerialPort.GetPortNames();
                if (ps == null || ps.Length < 1)
                {
                    Port = "";
                }
                else
                {
                    Port = ps[0];
                }
                Baud = _bauds[0];
                Parity = _parities[0];
                DataBit = _dataBits[0];
                StartBit = "1";
                StopBit = _stopBits[0];
                TimeOut = "1000";
                WriteReadInterval = "1000";
                CmdInterval = "1000";
                ServerIP = "127.0.0.1";
                ServerPort = "8678";

                LogMessageInformation("当前串口端口号:" + Port + ".");
                LogMessageInformation("当前串口波特率:" + Baud + ".");
                LogMessageInformation("当前串口校验位:" + Parity + ".");
                LogMessageInformation("当前串口数据位:" + DataBit + ".");
                LogMessageInformation("当前串口开始位:" + StartBit + ".");
                LogMessageInformation("当前串口停止位:" + StopBit + ".");
                LogMessageInformation("当前串口超时时间:" + TimeOut + "ms.");
                LogMessageInformation("当前串口命令间隔时间:" + CmdInterval + "ms.");
                LogMessageInformation("当前串口读写间隔时间:" + WriteReadInterval + "ms.");
                LogMessageInformation("当前服务器IP:" + ServerIP + ".");
                LogMessageInformation("当前服务器端口:" + ServerPort + ".");
            }

            LogMessageSeperator();
        }

        private void CreateNewConfig(bool isNew = true)
        {
            try
            {
                StreamWriter sw = new StreamWriter("config.xml", false);
                sw.WriteLine("<bumblebee>");
                string[] ps = SerialPort.GetPortNames();
                if (ps == null || ps.Length < 1)
                {
                    sw.WriteLine("    <port></port>");
                }
                else
                {
                    sw.WriteLine("    <port>" + ps[0] + "</port>");
                }
                sw.WriteLine("    <baud>" + _bauds[0] + "</baud>");
                sw.WriteLine("    <parity>" +_parities[0]+ "</parity>");
                sw.WriteLine("    <data>" + _dataBits[0] + "</data>");
                sw.WriteLine("    <start>1</start>");
                sw.WriteLine("    <stop>" + _stopBits[0] + "</stop>");
                sw.WriteLine("    <timeout>1000</timeout>");
                sw.WriteLine("    <serverip>127.0.0.1</serverip>");
                sw.WriteLine("    <serverport>8678</serverport>");
                sw.WriteLine("</bumblebee>");
                sw.Flush();
                sw.Close();
                sw.Dispose();
            }
            catch (Exception ex)
            {
                if(isNew == true)
                    LogMessage("创建新的配置文件出现错误.\n" + ex.Message, LogType.Error);
                else
                    LogMessage("重置配置文件出现错误.\n" + ex.Message, LogType.Error);
            }
        }

        #endregion

        private void ClearLog_Button_Click(object sender, RoutedEventArgs e)
        {
            if (fldocLog.Blocks.Count > 0 && MessageBox.Show("请确认清空日志.", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            fldocLog.Blocks.Clear();
        }

        private void ClearRecv_Button_Click(object sender, RoutedEventArgs e)
        {
            if (fldocRecv.Blocks.Count > 0 && MessageBox.Show("请确认清空接受区.", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            fldocRecv.Blocks.Clear();
        }

        private void ClearSend_Button_Click(object sender, RoutedEventArgs e)
        {
            if (fldocSend.Blocks.Count > 0 && MessageBox.Show("请确认清空发送区.", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            fldocSend.Blocks.Clear();
        }
        
        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_started == false)
            {
                MessageBox.Show("已经在停止操作中.请等待" + TimeOut + "毫秒.", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _started = false;

            if (MessageBox.Show("停止操作可能需要" + TimeOut + "毫秒.\n请确认停止操作.", "停止", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _cts.Cancel();

            try
            {
                if (InChkCmd == true)
                {
                    _serialPortChkTask.Wait(int.Parse(TimeOut));
                }
                else
                {
                    _serialPortTask.Wait(int.Parse(TimeOut));
                }
            }
            catch (AggregateException aex)
            {
                string msg = "";
                foreach(Exception ex in aex.InnerExceptions)
                {
                    msg = msg + "\n"+ ex.Message;
                }
                MessageBox.Show("停止操作出现错误.\n" + msg.Trim(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Start_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_sPort == null || _sPort.IsOpen == false)
            {
                LogMessageError("串口" + Port + "未打开.");
                LogMessageSeperator();
                return;
            }

            #region

            DateTime dt = DateTime.Now;
            CurrentDirectory = System.Environment.CurrentDirectory;
            if (NeedReport)
            {
                if (Directory.Exists(CurrentDirectory + @"\Reports") == false)
                {
                    try
                    {
                        Directory.CreateDirectory(CurrentDirectory + @"\Reports");
                        CurrentDirectory = CurrentDirectory + @"\Reports";
                    }
                    catch (Exception)
                    {
                        CurrentDirectory = System.Environment.CurrentDirectory;
                    }
                }
                _docTitleDateTime = string.Format("{0}_{1}_{2} {3}_{4}_{5}", dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
                CreateReport();
            }

            #endregion

            _cmd15HRespOc.Clear();
            _cmd14HRespOc.Clear();
            _cmd13HRespOc.Clear();
            _cmd12HRespOc.Clear();
            _cmd11HRespOc.Clear();
            _cmd10HRespOc.Clear();
            _cmd09HRespOc.Clear();
            _cmd08HRespOc.Clear();

            _started = true;

            InRun = true;

            //if (AutoClearLog == true)
            //    fldocLog.Blocks.Clear();
            //if (AutoClearRecv == true)
            //    fldocRecv.Blocks.Clear();
            //if (AutoClearSend == true)
            //    fldocSend.Blocks.Clear();

            _cmdsList.Clear();

            _cts = new CancellationTokenSource();

            if (InChkCmd == true)
            {
                _displayedEnterChk = false;

                _serialPortChkTask = Task.Factory.StartNew(new Action(SerialPortChkTaskHander), _cts.Token);
            }
            else
            {
                _serialPortTask = Task.Factory.StartNew(new Action(SerialPortTaskHander), _cts.Token);
            }
        }

        private void SerialPortChkTaskHander()
        {
            SerialPortChkWriteReadType wrt = SerialPortChkWriteReadType.E0H;
            SerialPortChkWriteReadType wrtPrev = SerialPortChkWriteReadType.E0H;

            try
            {
                while (_cts.IsCancellationRequested == false)
                {
                    foreach (CmdDefinition cdi in _chkCmdOc)
                    {
                        if (cdi.CmdSelected == false)
                            continue;

                        switch (cdi.CmdContent.Substring(0, 3).ToUpper())
                        {
                            default:
                            case "E0H":
                                wrt = SerialPortChkWriteReadType.E0H;
                                AlreadyEnterCheck = true;
                                break;
                            case "E1H":
                                wrt = SerialPortChkWriteReadType.E1H;
                                break;
                            case "E2H":
                                wrt = SerialPortChkWriteReadType.E2H;
                                break;
                            case "E3H":
                                wrt = SerialPortChkWriteReadType.E3H;
                                break;
                            case "E4H":
                                wrt = SerialPortChkWriteReadType.E4H;
                                CheckModeChecked = false;
                                AlreadyEnterCheck = false;
                                break;
                        }
                    }

                    if (wrt != wrtPrev)
                    {
                        switch (wrt)
                        {
                            default:
                                break;
                            //case SerialPortChkWriteReadType.E1H:
                                //SerialPortChkWriteReadHandler(_chkCmdOc[0], SerialPortChkWriteReadType.E0H);
                                //_sPort.DiscardInBuffer();
                                //_sPort.DiscardOutBuffer();
                                //Thread.Sleep(int.Parse(CmdInterval));
                                //SerialPortChkWriteReadHandler(_chkCmdOc[(int)wrt], wrt);
                                //break;
                            case SerialPortChkWriteReadType.E0H:
                                //SerialPortChkWriteReadHandler(_chkCmdOc[(int)wrt], wrt);
                                //break;
                            case SerialPortChkWriteReadType.E1H:
                            case SerialPortChkWriteReadType.E2H:
                            case SerialPortChkWriteReadType.E3H:
                                //SerialPortChkWriteReadHandler(_chkCmdOc[0], SerialPortChkWriteReadType.E0H);
                                //_sPort.DiscardInBuffer();
                                //_sPort.DiscardOutBuffer();
                                //Thread.Sleep(int.Parse(CmdInterval));
                                SerialPortChkWriteReadHandler(_chkCmdOc[(int)wrt], wrt);
                                break;
                        }

                        wrtPrev = wrt;
                    }
                    else
                    {
                        SerialPortChkWriteReadHandler(_chkCmdOc[0], SerialPortChkWriteReadType.E0H);

                        //Thread.Sleep(CHK_CMD_INTERVAL);
                    }

                    Thread.Sleep(CHK_CMD_INTERVAL);
                }

                //_sPort.DiscardInBuffer();
                //_sPort.DiscardOutBuffer();

                if (InChkCmd == false)
                {
                    SerialPortChkWriteReadHandler(_chkCmdOc[_chkCmdOc.Count - 1]);

                    _timerPBar.Change(Timeout.Infinite, 100);
                    PBarValue = 0;
                }
            }
            catch (Exception ex)
            {
                LogMessageError("串口读写错误或响应处理错误.\n" + ex.Message);

                _timerPBar.Change(Timeout.Infinite, 100);
                PBarValue = 0;

                //ReadyString2 = "";

                CheckCmdState = "执行异常.";
            }

            InRun = false;

            ReadyString2 = "";

            LogMessageSeperator(false);

            PBarValue = 0;
        }

        private void SerialPortChkWriteReadHandler(CmdDefinition cd, 
            SerialPortChkWriteReadType wrt = SerialPortChkWriteReadType.E4H)
        {
            _sPort.DiscardInBuffer();
            _sPort.DiscardOutBuffer();
            
            string finalCmd = "AA 75 " + wrt.ToString().Substring(0, 2).ToUpper() + " 00 00 00";
            finalCmd = finalCmd + " " + CmdDefinition.XORData(finalCmd);

            switch (wrt)
            {
                default:
                    LogMessageError("未知的检定命令.");
                    break;
                case SerialPortChkWriteReadType.E0H:
                    if(_displayedEnterChk == false)
                        LogMessageTitle("进入检定状态.");
                    else
                        LogMessageTitle("保持检定状态.");
                    break;
                case SerialPortChkWriteReadType.E1H:
                    LogMessageTitle("进入里程误差测量.");
                    break;
                case SerialPortChkWriteReadType.E2H:
                    LogMessageTitle("进入脉冲系数误差测量.");
                    break;
                case SerialPortChkWriteReadType.E3H:
                    LogMessageTitle("进入实时时间误差测量.");
                    break;
                case SerialPortChkWriteReadType.E4H:
                    LogMessageTitle("返回正常工作状态.");
                    break;
            }

            LogMessageTitle("");
 
            #region Send

            ReadyString2 = "发送(" + cd.CmdContent + "):" + finalCmd + "...";

            string[] sa = finalCmd.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            int lenSend = sa.Length;

            Dispatcher.Invoke((ThreadStart)delegate
            {
                #region

                while (fldocSend.Blocks.Count > MAX_DISPLAY_LINE_COUNT)
                {
                    fldocSend.Blocks.Remove(fldocSend.Blocks.FirstBlock);
                }

                Run rch = new Run(finalCmd);
                WinParagraph pch = new WinParagraph(rch);
                fldocSend.Blocks.Add(pch);
                Run rch1 = new Run("");
                WinParagraph pch1 = new WinParagraph(rch1);
                fldocSend.Blocks.Add(pch1);
                rtxtSend.ScrollToEnd();

                SendingByteNumber = SendingByteNumber + lenSend;

                #endregion
            }, null);

            PBarValue = 0;
            _timerPBar.Change(0, 100);

            byte[] ba = new byte[lenSend];
            for (int i = 0; i < lenSend; i++)
            {
                try
                {
                    ba[i] = byte.Parse(sa[i], NumberStyles.HexNumber);
                }
                catch (Exception)
                {
                    ba[i] = (byte)0;
                }
            }
            _sPort.Write(ba, 0, lenSend);

            #endregion

            Thread.Sleep(CHK_CMD_INTERVAL);

            //_sPort.DiscardInBuffer();
            //_sPort.DiscardOutBuffer();

            _timerPBar.Change(Timeout.Infinite, 100);
            PBarValue = 0;

            #region Receive

            ReadyString2 = "读取响应...";

            PBarValue = 0;
            _timerPBar.Change(0, 100);

            byte[] bytes = new byte[1024];
            int lenRecv = _sPort.Read(bytes, 0, 1024);
            byte[] baRecev = new byte[lenRecv];
            for (int i = 0; i < lenRecv; i++)
            {
                baRecev[i] = bytes[i];
            }
            string sRecv = BytesToHexString(baRecev);
            int lenSRecv = sRecv.Length;

            Dispatcher.Invoke((ThreadStart)delegate
            {
                #region

                while (fldocRecv.Blocks.Count > MAX_DISPLAY_LINE_COUNT)
                {
                    fldocRecv.Blocks.Remove(fldocRecv.Blocks.FirstBlock);
                }
                
                Run rch = new Run(sRecv);
                WinParagraph pch = new WinParagraph(rch);
                fldocRecv.Blocks.Add(pch);
                Run rch1 = new Run("");
                WinParagraph pch1 = new WinParagraph(rch1);
                fldocRecv.Blocks.Add(pch1);
                rtxtRecv.ScrollToEnd();

                ReceivingByteNumber = ReceivingByteNumber + lenRecv;

                #endregion
            }, null);

            #region Parse

            string[] saRecv = sRecv.Split(new string[] { "55 7A " }, StringSplitOptions.RemoveEmptyEntries);
            if (saRecv == null || saRecv.Length < 1)
            {
                LogMessageError("响应的格式错误.");

                CheckCmdState = "执行错误.";
            }
            else
            {
                foreach (string sRecvi in saRecv)
                {
                    byte[] baRecvi = CmdDefinition.HexStringToBytes("55 7A " + sRecvi.Trim());
                    string sRecviNew = "55 7A " + sRecvi.Trim();
                    int lenSRecviNew = sRecviNew.Length;

                    string sToXor = sRecviNew.Substring(0, lenSRecviNew - 3);
                    string sFromXor = CmdDefinition.XORData(sToXor);
                    string sXor = sRecviNew.Substring(lenSRecviNew - 2, 2);
                    if (string.Compare(sFromXor, sXor, true) != 0)
                    {
                        LogMessageError("响应的校验错误.");

                        CheckCmdState = "执行错误.";
                    }
                    else
                    {
                        #region

                        string header0 = string.Format("{0:X2}", baRecvi[0]);
                        string header1 = string.Format("{0:X2}", baRecvi[1]);
                        string header2 = string.Format("{0:X2}", baRecvi[2]);
                        if (string.Compare(header0, "55", true) != 0 || string.Compare(header1, "7A", true) != 0)
                        {
                            LogMessageError("响应的起始字头错误:" + header0 + " " + header1);

                            CheckCmdState = "执行错误.";
                        }
                        else
                        {
                            if (string.Compare(header2, "FA", true) == 0 || string.Compare(header2, "FB", true) == 0)
                            {
                                LogMessageError("命令帧接收错误.");

                                CheckCmdState = "执行错误.";
                            }
                            else
                            {
                                int iHigh = (int)baRecvi[3];
                                int iLow = (int)baRecvi[4];
                                int dataLen = iHigh * 256 + iLow;

                                if (baRecvi.Length != 6 + dataLen + 1)
                                {
                                    LogMessageError("响应的数据块长度不匹配.");

                                    CheckCmdState = "执行错误.";
                                }
                                else
                                {
                                    byte[] baData = new byte[dataLen];
                                    for (int i = 0; i < dataLen; i++)
                                        baData[i] = baRecvi[6 + i];

                                    switch (header2)
                                    {
                                        default:
                                            LogMessageError("未知的记录仪应答.");
                                            CheckCmdState = "错误应答.";
                                            break;
                                        case "E0":
                                            if (_displayedEnterChk == false)
                                            {
                                                LogMessage("进入检定状态:记录仪正确应答.");
                                                _displayedEnterChk = true;
                                                CheckCmdState = "进入检定.";
                                            }
                                            else
                                            {
                                                LogMessage("保持检定状态:记录仪正确应答.");
                                                CheckCmdState = "保持检定.";
                                            }
                                            break;
                                        case "E1":
                                            #region
                                            {
                                                string ccc = Encoding.ASCII.GetString(baData, 0, 7).Trim('\0').PadRight(27);
                                                string model = Encoding.ASCII.GetString(baData, 7, 16).Trim('\0').PadRight(27);
                                                string number = "20" + baData[23].ToString("X") + "-" + baData[24].ToString("X") + "-" + baData[25].ToString("X");
                                                number = number.PadRight(27);
                                                long flow = baData[26] * 256 * 256 * 256 + baData[27] * 256 * 256 + baData[28] * 256 + baData[29];
                                                string productflow = flow.ToString().PadRight(27);
                                                //LogMessage("+------------------------------------------------+");
                                                //LogMessage("|               记录仪唯一性编号                 |");
                                                LogMessage("+-------------------+----------------------------+");
                                                LogMessage("| 生产厂CCC认证代码 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", ccc));
                                                LogMessage("+-------------------+----------------------------+");
                                                LogMessage("|      认证产品型号 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", model));
                                                LogMessage("+-------------------+----------------------------+");
                                                LogMessage("|    记录仪生产时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                                LogMessage("+-------------------+----------------------------+");
                                                LogMessage("|    产品生产流水号 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", productflow));
                                                LogMessage("+-------------------+----------------------------+");

                                                int intHigh = (int)baData[35];
                                                int intLow = (int)baData[36];
                                                int intLen = intHigh * 256 + intLow;
                                                string sValue = intLen.ToString().PadRight(27);
                                                LogMessage("|          脉冲系数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                                LogMessage("+-------------------+----------------------------+");

                                                intHigh = (int)baData[37];
                                                intLow = (int)baData[38];
                                                intLen = intHigh * 256 + intLow;
                                                sValue = (intLen.ToString() + " (注:单位 0.1km/h)").PadRight(27 - 3);
                                                LogMessage("|          当前速度 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                                LogMessage("+-------------------+----------------------------+");


                                                int intHighHigh = (int)baData[39];
                                                intHigh = (int)baData[40];
                                                intLow = (int)baData[41];
                                                int intLowLow = (int)baData[42];
                                                long longLen = intHighHigh;
                                                longLen = longLen * 256 + intHigh;
                                                longLen = longLen * 256 + intLow;
                                                longLen = longLen * 256 + intLowLow;
                                                sValue = (longLen.ToString() + " (注:单位 米)").PadRight(27 - 4);
                                                LogMessage("|          累计里程 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                                LogMessage("+-------------------+----------------------------+");

                                                Encoding gb = Encoding.GetEncoding("GB2312");
                                                string d0 = D0;
                                                if ((baData[6] & 0x1) == 0x1)
                                                    d0 = "(有操作) " + d0;
                                                else
                                                    d0 = "(无操作) " + d0;
                                                d0 = d0.PadRight(27 - GetChineseNumber(d0) + 3);
                                                string d1 = D1;
                                                if ((baData[6] & 0x2) == 0x2)
                                                    d1 = "(有操作) " + d1;
                                                else
                                                    d1 = "(无操作) " + d1;
                                                d1 = d1.PadRight(27 - GetChineseNumber(d1) + 3);
                                                string d2 = D2;
                                                if ((baData[6] & 0x4) == 0x4)
                                                    d2 = "(有操作) " + d2;
                                                else
                                                    d2 = "(无操作) " + d2;
                                                d2 = d2.PadRight(27 - GetChineseNumber(d2) + 3);
                                                string d3 = "近光";
                                                if ((baData[6] & 0x8) == 0x8)
                                                    d3 = "(有操作) " + d3;
                                                else
                                                    d3 = "(无操作) " + d3;
                                                d3 = d3.PadRight(27 - GetChineseNumber(d3) + 3);
                                                string d4 = "远光";
                                                if ((baData[6] & 0x10) == 0x10)
                                                    d4 = "(有操作) " + d4;
                                                else
                                                    d4 = "(无操作) " + d4;
                                                d4 = d4.PadRight(27 - GetChineseNumber(d4) + 3);
                                                string d5 = "右转向";
                                                if ((baData[6] & 0x20) == 0x20)
                                                    d5 = "(有操作) " + d5;
                                                else
                                                    d5 = "(无操作) " + d5;
                                                d5 = d5.PadRight(27 - GetChineseNumber(d5) + 3);
                                                string d6 = "左转向";
                                                if ((baData[6] & 0x40) == 0x40)
                                                    d6 = "(有操作) " + d6;
                                                else
                                                    d6 = "(无操作) " + d6;
                                                d6 = d6.PadRight(27 - GetChineseNumber(d6) + 3);
                                                string d7 = "制动";
                                                if ((baData[6] & 0x80) == 0x80)
                                                    d7 = "(有操作) " + d7;
                                                else
                                                    d7 = "(无操作) " + d7;
                                                d7 = d7.PadRight(27 - GetChineseNumber(d7) + 3);
                                                //LogMessage("|                     状态信号                   |");
                                                //LogMessage("+-------------------+----------------------------+");
                                                LogMessage("|                D7 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d7));
                                                LogMessage("+-------------------+----------------------------+");
                                                LogMessage("|                D6 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d6));
                                                LogMessage("+-------------------+----------------------------+");
                                                LogMessage("|                D5 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d5));
                                                LogMessage("+-------------------+----------------------------+");
                                                LogMessage("|                D4 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d4));
                                                LogMessage("+-------------------+----------------------------+");
                                                LogMessage("|                D3 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d3));
                                                LogMessage("+-------------------+----------------------------+");
                                                LogMessage("|                D2 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d2));
                                                LogMessage("+-------------------+----------------------------+");
                                                LogMessage("|                D1 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d1));
                                                LogMessage("+-------------------+----------------------------+");
                                                LogMessage("|                D0 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d0));
                                                LogMessage("+-------------------+----------------------------+");
                                            }
                                            #endregion
                                            CheckCmdState = "里程误差.";
                                            break;
                                        case "E2":
                                            LogMessage("进入脉冲系数误差测量:记录仪正确应答.");
                                            CheckCmdState = "脉冲系数.";
                                            break;
                                        case "E3":
                                            LogMessage("进入实时时间误差测量:记录仪正确应答.");
                                            CheckCmdState = "实时时间.";
                                            break;
                                        case "E4":
                                            LogMessage("返回正常工作状态:记录仪正确应答.");
                                            CheckCmdState = "检定返回.";
                                            break;
                                    }
                                }
                            }
                        }

                        #endregion
                    }

                    LogMessage("");
                }
            }

            #endregion

            #endregion
        }

        private void SerialPortTaskHander()
        {
            #region Create Concrete Cmds

            foreach (CmdDefinition cdi in _getCmdOc)
            {
                if (cdi.CmdSelected == false)
                    continue;

                _cmdsList.Add(cdi);
            }

            foreach (CmdDefinition cdi in _setCmdOc)
            {
                if (cdi.CmdSelected == false)
                    continue;

                _cmdsList.Add(cdi);
            }

            //foreach (CmdDefinition cdi in _chkCmdOc)
            //{
            //}

            foreach (CmdDefinition cdi in _extGetCmdOc)
            {
                if (cdi.CmdSelected == false)
                    continue;

                _cmdsList.Add(cdi);
            }

            foreach (CmdDefinition cdi in _extSetCmdOc)
            {
                if (cdi.CmdSelected == false)
                    continue;

                _cmdsList.Add(cdi);
            }

            #endregion

            #region Send & Receive

            bool isContinued = false;
            string newCmdContinue = "";
            CmdDefinition cdPrev = null;

            for (int iCmd = 0; iCmd < _cmdsList.Count; iCmd++)//CmdDefinition cdi in _cmdsList)
            {
                CmdDefinition cdi = _cmdsList[iCmd];

                // If something wrong with the Cmd, make sure of the next Cmd being correct
                if (cdPrev != cdi)
                {
                    cdPrev = cdi;
                    isContinued = false;
                }

                #region

                if (_cts.IsCancellationRequested == true)
                    break;

                if(isContinued == false)
                    LogMessageTitle(cdi.CmdContent.Substring(6));

                try
                {
                    string cmd = "";

                    if (isContinued == false)
                    {
                        string[] cmda = cdi.GetConcreteCmds();
                        if (cmda == null || cmda.Length < 1 || cmda.Length > 2)
                        {
                            LogMessageError("命令(" + cdi.CmdContent + ")格式错误.");
                            LogMessage("");
                            cdi.CmdState = "参数错误.";
                            continue;
                        }
                        else if (cmda.Length == 2)
                        {
                            LogMessageError("命令(" + cdi.CmdContent + ")错误:" + cmda[1]);
                            cdi.CmdState = "参数错误.";
                            LogMessage("");
                            continue;
                        }

                        cmd = cmda[0];
                        newCmdContinue = cmd;
                    }
                    else
                    {
                        cmd = newCmdContinue;
                    }

                    _sPort.DiscardInBuffer();
                    _sPort.DiscardOutBuffer();

                    #region Send

                    ReadyString2 = "发送(" + cdi.CmdContent + "):" + cmd + "...";

                    string[] sa = cmd.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    int lenSend = sa.Length;

                    Dispatcher.Invoke((ThreadStart)delegate
                    {
                        #region

                        while (fldocSend.Blocks.Count > MAX_DISPLAY_LINE_COUNT)
                        {
                            fldocSend.Blocks.Remove(fldocSend.Blocks.FirstBlock);
                        }

                        Run rch = new Run(cmd);
                        WinParagraph pch = new WinParagraph(rch);
                        fldocSend.Blocks.Add(pch);
                        Run rch1 = new Run("");
                        WinParagraph pch1 = new WinParagraph(rch1);
                        fldocSend.Blocks.Add(pch1);
                        rtxtSend.ScrollToEnd();

                        SendingByteNumber = SendingByteNumber + lenSend;

                        #endregion
                    }, null);

                    PBarValue = 0;
                    _timerPBar.Change(0, 100);

                    byte[] ba = new byte[lenSend];
                    for (int i = 0; i < lenSend; i++)
                    {
                        try
                        {
                            ba[i] = byte.Parse(sa[i], NumberStyles.HexNumber);
                        }
                        catch (Exception)
                        {
                            ba[i] = (byte)0;
                        }
                    }
                    _sPort.Write(ba, 0, lenSend);

                    #endregion

                    Thread.Sleep(int.Parse(WriteReadInterval));

                    _timerPBar.Change(Timeout.Infinite, 100);
                    PBarValue = 0;

                    #region Receive

                    ReadyString2 = "读取响应...";

                    PBarValue = 0;
                    _timerPBar.Change(0, 100);

                    byte[] bytes = new byte[1024];
                    int lenRecv = _sPort.Read(bytes, 0, 1024);
                    byte[] baRecev = new byte[lenRecv];
                    for (int i = 0; i < lenRecv; i++)
                    {
                        baRecev[i] = bytes[i];
                    }
                    string sRecv = BytesToHexString(baRecev);
                    int lenSRecv = sRecv.Length;

                    #region Long Data

                    switch (sa[2].ToUpper())
                    {
                        default:
                            break;
                        case "08":
                        case "09":
                        case "10":
                        case "11":
                        case "12":
                        case "13":
                        case "14":
                        case "15":
                            {
                                int readTimeout = _sPort.ReadTimeout;
                                bool getException = false;
                                while (!getException)
                                {
                                    try
                                    {
                                        byte[] bytes2 = new byte[1024];
                                        _sPort.ReadTimeout = 1000;
                                        int lenRecv2 = _sPort.Read(bytes2, 0, 1024);
                                        byte[] baRecev2 = new byte[lenRecv2];
                                        for (int i = 0; i < lenRecv2; i++)
                                        {
                                            baRecev2[i] = bytes2[i];
                                        }
                                        string sRecv2 = BytesToHexString(baRecev2);
                                        int lenSRecv2 = sRecv2.Length;

                                        byte[] newBytes = new byte[lenRecv + lenRecv2];
                                        for (int i = 0; i < lenRecv; i++)
                                        {
                                            newBytes[i] = bytes[i];
                                        }
                                        for (int i = 0; i < lenRecv2; i++)
                                        {
                                            newBytes[lenRecv + i] = bytes2[i];
                                        }

                                        bytes = newBytes;
                                        lenRecv = bytes.Length;
                                        baRecev = new byte[lenRecv];
                                        for (int i = 0; i < lenRecv; i++)
                                        {
                                            baRecev[i] = bytes[i];
                                        }
                                        sRecv = BytesToHexString(baRecev);
                                        lenSRecv = sRecv.Length;
                                        _sPort.ReadTimeout = readTimeout;
                                    }
                                    catch (Exception)
                                    {
                                        getException = true;
                                    }
                                }
                                _sPort.ReadTimeout = readTimeout;
                            }
                            break;
                    }

                    #endregion

                    Dispatcher.Invoke((ThreadStart)delegate
                    {
                        #region

                        while (fldocRecv.Blocks.Count > MAX_DISPLAY_LINE_COUNT)
                        {
                            fldocRecv.Blocks.Remove(fldocRecv.Blocks.FirstBlock);
                        }

                        Run rch = new Run(sRecv);
                        WinParagraph pch = new WinParagraph(rch);
                        fldocRecv.Blocks.Add(pch);
                        Run rch1 = new Run("");
                        WinParagraph pch1 = new WinParagraph(rch1);
                        fldocRecv.Blocks.Add(pch1);
                        rtxtRecv.ScrollToEnd();

                        ReceivingByteNumber = ReceivingByteNumber + lenRecv;

                        #endregion
                    }, null);

                    if (lenRecv >= 5)
                    {
                        string sToXor = sRecv.Substring(0, lenSRecv - 3);
                        string sFromXor = CmdDefinition.XORData(sToXor);
                        string sXor = sRecv.Substring(lenSRecv - 2, 2);
                        if (string.Compare(sFromXor, sXor, true) != 0)
                        {
                            LogMessageError("命令(" + cdi.CmdContent + ")的响应的校验错误.");

                            cdi.CmdState = "执行错误.";

                            isContinued = false;
                        }
                        else
                        {
                            string header0 = string.Format("{0:X2}", baRecev[0]);
                            string header1 = string.Format("{0:X2}", baRecev[1]);
                            string header2 = string.Format("{0:X2}", baRecev[2]);
                            if (string.Compare(header0, "55", true) != 0 || string.Compare(header1, "7A", true) != 0)
                            {
                                LogMessageError("命令(" + cdi.CmdContent + ")的响应的起始字头错误:" + header0 + " " + header1);

                                cdi.CmdState = "执行错误.";

                                isContinued = false;
                            }
                            else
                            {
                                if (string.Compare(header2, "FA", true) == 0)
                                {
                                    LogMessageError("采集数据命令(" + cdi.CmdContent + ")的命令帧接收错误.");

                                    cdi.CmdState = "执行错误.";

                                    isContinued = false;
                                }
                                else if (string.Compare(header2, "FB", true) == 0)
                                {
                                    LogMessageError("设置参数命令(" + cdi.CmdContent + ")的命令帧接收错误.");

                                    cdi.CmdState = "执行错误.";

                                    isContinued = false;
                                }
                                else if (string.Compare(header2, sa[2], true) != 0)
                                {
                                    LogMessageError("响应与命令(" + cdi.CmdContent + ")的命令字不匹配:" + header2);

                                    cdi.CmdState = "执行错误.";

                                    isContinued = false;
                                }
                                else
                                {
                                    int iHigh = (int)baRecev[3];
                                    int iLow = (int)baRecev[4];
                                    int dataLen = iHigh * 256 + iLow;

                                    if (baRecev.Length != 6 + dataLen + 1)
                                    {
                                        LogMessageError("命令(" + cdi.CmdContent + ")的响应的数据块长度不匹配.");

                                        cdi.CmdState = "执行错误.";
                                    }
                                    else
                                    {
                                        byte[] baData = new byte[dataLen];
                                        for (int i = 0; i < dataLen; i++)
                                            baData[i] = baRecev[6 + i];

                                        #region Display Result

                                        switch (sa[2].ToUpper())
                                        {
                                            default:
                                                LogMessageError("命令(" + cdi.CmdContent + ")未知.");
                                                break;
                                            case "00":
                                                #region
                                                {
                                                    if (dataLen != 2)
                                                        LogMessageError("命令(" + cdi.CmdContent + ")的响应的数据块长度错误.");
                                                    else
                                                    {
                                                        string year = baData[0].ToString("X").PadRight(27);
                                                        string number = baData[1].ToString().PadRight(27);
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|              年号 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", year));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|          修改单号 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                                        LogMessage("+-------------------+----------------------------+");

                                                        if (NeedReport == true)
                                                        {
                                                            if (_pdfDocument == null)
                                                            {
                                                                LogMessageError("无法创建报表.");
                                                            }
                                                            else
                                                            {
                                                                #region

                                                                try
                                                                {
                                                                    string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                                    BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                                    PdfParagraph par = new PdfParagraph("--- 记录仪执行版本标准号 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                                    par.Alignment = Element.ALIGN_CENTER;
                                                                    _pdfDocument.Add(par);

                                                                    par.SpacingBefore = 25f;

                                                                    PdfPTable table = new PdfPTable(2);

                                                                    table.SpacingBefore = 25f;

                                                                    table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                                    float[] widths = { 50f, 150f };
                                                                    table.SetWidths(widths);
                                                                    table.LockedWidth = true;

                                                                    PdfPCell cell;
                                                                    cell = new PdfPCell(new Phrase("年号", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(year.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("修改单号", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(number.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);

                                                                    _pdfDocument.Add(table);
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    LogMessageError("创建报表出错:" + ex.Message);
                                                                    _pdfDocument = null;
                                                                }

                                                                #endregion
                                                            }
                                                        }
                                                    }
                                                }
                                                #endregion
                                                break;
                                            case "01":
                                                #region
                                                {
                                                    if (dataLen != 18)
                                                        LogMessageError("命令(" + cdi.CmdContent + ")的响应的数据块长度错误.");
                                                    else
                                                    {
                                                        string number = Encoding.UTF8.GetString(baData).PadRight(27);
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|            驾证号 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                                        LogMessage("+-------------------+----------------------------+");

                                                        if (NeedReport == true)
                                                        {
                                                            if (_pdfDocument == null)
                                                            {
                                                                LogMessageError("无法创建报表.");
                                                            }
                                                            else
                                                            {
                                                                #region

                                                                try
                                                                {
                                                                    string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                                    BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                                    PdfParagraph par = new PdfParagraph("--- 当前驾驶人信息 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                                    par.Alignment = Element.ALIGN_CENTER;
                                                                    _pdfDocument.Add(par);

                                                                    par.SpacingBefore = 25f;

                                                                    PdfPTable table = new PdfPTable(2);

                                                                    table.SpacingBefore = 25f;

                                                                    table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                                    float[] widths = { 50f, 150f };
                                                                    table.SetWidths(widths);
                                                                    table.LockedWidth = true;

                                                                    PdfPCell cell;
                                                                    cell = new PdfPCell(new Phrase("驾证号", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(number.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);

                                                                    _pdfDocument.Add(table);
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    LogMessageError("创建报表出错:" + ex.Message);
                                                                    _pdfDocument = null;
                                                                }

                                                                #endregion
                                                            }
                                                        }
                                                    }
                                                }
                                                #endregion
                                                break;
                                            case "02":
                                                #region
                                                {
                                                    if (dataLen != 6)
                                                        LogMessageError("命令(" + cdi.CmdContent + ")的响应的数据块长度错误.");
                                                    else
                                                    {
                                                        string number = "20" + baData[0].ToString("X") + "-" + baData[1].ToString("X") + "-" + baData[2].ToString("X") + " " +
                                                            baData[3].ToString("X") + ":" + baData[4].ToString("X") + ":" + baData[5].ToString("X");
                                                        number = number.PadRight(27);
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|          采集时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                                        LogMessage("+-------------------+----------------------------+");

                                                        if (NeedReport == true)
                                                        {
                                                            if (_pdfDocument == null)
                                                            {
                                                                LogMessageError("无法创建报表.");
                                                            }
                                                            else
                                                            {
                                                                #region

                                                                try
                                                                {
                                                                    string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                                    BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                                    PdfParagraph par = new PdfParagraph("--- 记录仪实时时间 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                                    par.Alignment = Element.ALIGN_CENTER;
                                                                    _pdfDocument.Add(par);

                                                                    par.SpacingBefore = 25f;

                                                                    PdfPTable table = new PdfPTable(2);

                                                                    table.SpacingBefore = 25f;

                                                                    table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                                    float[] widths = { 50f, 150f };
                                                                    table.SetWidths(widths);
                                                                    table.LockedWidth = true;

                                                                    PdfPCell cell;
                                                                    cell = new PdfPCell(new Phrase("采集时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(number.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);

                                                                    _pdfDocument.Add(table);
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    LogMessageError("创建报表出错:" + ex.Message);
                                                                    _pdfDocument = null;
                                                                }

                                                                #endregion
                                                            }
                                                        }
                                                    }
                                                }
                                                #endregion
                                                break;
                                            case "03":
                                                #region
                                                {
                                                    if (dataLen != 20)
                                                        LogMessageError("命令(" + cdi.CmdContent + ")的响应的数据块长度错误.");
                                                    else
                                                    {
                                                        string number1 = "20" + baData[0].ToString("X") + "-" + baData[1].ToString("X") + "-" + baData[2].ToString("X") + " " +
                                                            baData[3].ToString("X") + ":" + baData[4].ToString("X") + ":" + baData[5].ToString("X");
                                                        number1 = number1.PadRight(27);
                                                        string number2 = "20" + baData[6].ToString("X") + "-" + baData[7].ToString("X") + "-" + baData[8].ToString("X") + " " +
                                                            baData[9].ToString("X") + ":" + baData[10].ToString("X") + ":" + baData[11].ToString("X");
                                                        number2 = number2.PadRight(27);
                                                        string distance1 = baData[12].ToString("X") + baData[13].ToString("X") + baData[14].ToString("X") + baData[15].ToString("X");
                                                        while (distance1.StartsWith("0"))
                                                            distance1 = distance1.Substring(1);
                                                        if (string.IsNullOrWhiteSpace(distance1))
                                                            distance1 = "0";
                                                        distance1 = (distance1 + "0 (单位:0.1千米)").PadRight(27 - 4);
                                                        string distance2 = baData[16].ToString("X") + baData[17].ToString("X") + baData[18].ToString("X") + baData[19].ToString("X");
                                                        while (distance2.StartsWith("0"))
                                                            distance2 = distance2.Substring(1);
                                                        if (string.IsNullOrWhiteSpace(distance2))
                                                            distance2 = "0";
                                                        distance2 = (distance2 + " (单位:0.1千米)").PadRight(27 - 4); // Why not "0 (单位:0.1千米)"
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|          采集时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number1));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|          安装时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number2));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|          初始里程 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", distance1));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|          累计里程 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", distance2));
                                                        LogMessage("+-------------------+----------------------------+");

                                                        if (NeedReport == true)
                                                        {
                                                            if (_pdfDocument == null)
                                                            {
                                                                LogMessageError("无法创建报表.");
                                                            }
                                                            else
                                                            {
                                                                #region

                                                                try
                                                                {
                                                                    string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                                    BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                                    PdfParagraph par = new PdfParagraph("--- 累计行驶里程 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                                    par.Alignment = Element.ALIGN_CENTER;
                                                                    _pdfDocument.Add(par);

                                                                    par.SpacingBefore = 25f;

                                                                    PdfPTable table = new PdfPTable(2);

                                                                    table.SpacingBefore = 25f;

                                                                    table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                                    float[] widths = { 50f, 150f };
                                                                    table.SetWidths(widths);
                                                                    table.LockedWidth = true;

                                                                    PdfPCell cell;
                                                                    cell = new PdfPCell(new Phrase("采集时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(number1.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("安装时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(number2.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("初始里程", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(distance1.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("累计里程", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(distance2.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);

                                                                    _pdfDocument.Add(table);
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    LogMessageError("创建报表出错:" + ex.Message);
                                                                    _pdfDocument = null;
                                                                }

                                                                #endregion
                                                            }
                                                        }
                                                    }
                                                }
                                                #endregion
                                                break;
                                            case "04":
                                                #region
                                                {
                                                    if (dataLen != 8)
                                                        LogMessageError("命令(" + cdi.CmdContent + ")的响应的数据块长度错误.");
                                                    else
                                                    {
                                                        string number = "20" + baData[0].ToString("X") + "-" + baData[1].ToString("X") + "-" + baData[2].ToString("X") + " " +
                                                            baData[3].ToString("X") + ":" + baData[4].ToString("X") + ":" + baData[5].ToString("X");
                                                        number = number.PadRight(27);
                                                        int intHigh = (int)baData[6];
                                                        int intLow = (int)baData[7];
                                                        int intLen = intHigh * 256 + intLow;
                                                        string sValue = intLen.ToString().PadRight(27);
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|          采集时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|          脉冲系数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                                        LogMessage("+-------------------+----------------------------+");

                                                        if (NeedReport == true)
                                                        {
                                                            if (_pdfDocument == null)
                                                            {
                                                                LogMessageError("无法创建报表.");
                                                            }
                                                            else
                                                            {
                                                                #region

                                                                try
                                                                {
                                                                    string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                                    BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                                    PdfParagraph par = new PdfParagraph("--- 记录仪脉冲系数 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                                    par.Alignment = Element.ALIGN_CENTER;
                                                                    _pdfDocument.Add(par);

                                                                    par.SpacingBefore = 25f;

                                                                    PdfPTable table = new PdfPTable(2);

                                                                    table.SpacingBefore = 25f;

                                                                    table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                                    float[] widths = { 50f, 150f };
                                                                    table.SetWidths(widths);
                                                                    table.LockedWidth = true;

                                                                    PdfPCell cell;
                                                                    cell = new PdfPCell(new Phrase("采集时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(number.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("脉冲系数", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(sValue.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);

                                                                    _pdfDocument.Add(table);
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    LogMessageError("创建报表出错:" + ex.Message);
                                                                    _pdfDocument = null;
                                                                }

                                                                #endregion
                                                            }
                                                        }
                                                    }
                                                }
                                                #endregion
                                                break;
                                            case "05":
                                                #region
                                                {
                                                    if (dataLen != 41)
                                                        LogMessageError("命令(" + cdi.CmdContent + ")的响应的数据块长度错误.");
                                                    else
                                                    {
                                                        string id = Encoding.UTF8.GetString(baData, 0, 17).PadRight(27);
                                                        Encoding gb = Encoding.GetEncoding("GB2312");
                                                        string number = gb.GetString(baData, 17, 12).Trim('\0');
                                                        number = number.PadRight(27 - GetChineseNumber(number) + 1);
                                                        string category = gb.GetString(baData, 29, 12).Trim('\0');
                                                        category = category.PadRight(27 - GetChineseNumber(category));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|        车辆识别码 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", id));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|          车辆号牌 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|          号牌分类 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", category));
                                                        LogMessage("+-------------------+----------------------------+");

                                                        if (NeedReport == true)
                                                        {
                                                            if (_pdfDocument == null)
                                                            {
                                                                LogMessageError("无法创建报表.");
                                                            }
                                                            else
                                                            {
                                                                #region

                                                                try
                                                                {
                                                                    string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                                    BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                                    PdfParagraph par = new PdfParagraph("--- 车辆信息 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                                    par.Alignment = Element.ALIGN_CENTER;
                                                                    _pdfDocument.Add(par);

                                                                    par.SpacingBefore = 25f;

                                                                    PdfPTable table = new PdfPTable(2);

                                                                    table.SpacingBefore = 25f;

                                                                    table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                                    float[] widths = { 50f, 150f };
                                                                    table.SetWidths(widths);
                                                                    table.LockedWidth = true;

                                                                    PdfPCell cell;
                                                                    cell = new PdfPCell(new Phrase("车辆识别码", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(id.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("车辆号牌", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(number.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("号牌分类", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(category.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);

                                                                    _pdfDocument.Add(table);
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    LogMessageError("创建报表出错:" + ex.Message);
                                                                    _pdfDocument = null;
                                                                }

                                                                #endregion
                                                            }
                                                        }
                                                    }
                                                }
                                                #endregion
                                                break;
                                            case "06":
                                                #region
                                                {
                                                    if (dataLen != 87)
                                                        LogMessageError("命令(" + cdi.CmdContent + ")的响应的数据块长度错误.");
                                                    else
                                                    {
                                                        string number = "20" + baData[0].ToString("X") + "-" + baData[1].ToString("X") + "-" + baData[2].ToString("X") + " " +
                                                            baData[3].ToString("X") + ":" + baData[4].ToString("X") + ":" + baData[5].ToString("X");
                                                        number = number.PadRight(27);
                                                        Encoding gb = Encoding.GetEncoding("GB2312");
                                                        string d0 = gb.GetString(baData, 7, 10).Trim('\0');
                                                        if ((baData[6] & 0x1) == 0x1)
                                                            d0 = "(有操作) " + d0;
                                                        else
                                                            d0 = "(无操作) " + d0;
                                                        d0 = d0.PadRight(27 - GetChineseNumber(d0) + 3);
                                                        string d1 = gb.GetString(baData, 17, 10).Trim('\0');
                                                        if ((baData[6] & 0x2) == 0x2)
                                                            d1 = "(有操作) " + d1;
                                                        else
                                                            d1 = "(无操作) " + d1;
                                                        d1 = d1.PadRight(27 - GetChineseNumber(d1) + 3);
                                                        string d2 = gb.GetString(baData, 27, 10).Trim('\0');
                                                        if ((baData[6] & 0x4) == 0x4)
                                                            d2 = "(有操作) " + d2;
                                                        else
                                                            d2 = "(无操作) " + d2;
                                                        d2 = d2.PadRight(27 - GetChineseNumber(d2) + 3);
                                                        string d3 = gb.GetString(baData, 37, 10).Trim('\0');
                                                        if ((baData[6] & 0x8) == 0x8)
                                                            d3 = "(有操作) " + d3;
                                                        else
                                                            d3 = "(无操作) " + d3;
                                                        d3 = d3.PadRight(27 - GetChineseNumber(d3) + 3);
                                                        string d4 = gb.GetString(baData, 47, 10).Trim('\0');
                                                        if ((baData[6] & 0x10) == 0x10)
                                                            d4 = "(有操作) " + d4;
                                                        else
                                                            d4 = "(无操作) " + d4;
                                                        d4 = d4.PadRight(27 - GetChineseNumber(d4) + 3);
                                                        string d5 = gb.GetString(baData, 57, 10).Trim('\0');
                                                        if ((baData[6] & 0x20) == 0x20)
                                                            d5 = "(有操作) " + d5;
                                                        else
                                                            d5 = "(无操作) " + d5;
                                                        d5 = d5.PadRight(27 - GetChineseNumber(d5) + 3);
                                                        string d6 = gb.GetString(baData, 67, 10).Trim('\0');
                                                        if ((baData[6] & 0x40) == 0x40)
                                                            d6 = "(有操作) " + d6;
                                                        else
                                                            d6 = "(无操作) " + d6;
                                                        d6 = d6.PadRight(27 - GetChineseNumber(d6) + 3);
                                                        string d7 = gb.GetString(baData, 77, 10).Trim('\0');
                                                        if ((baData[6] & 0x80) == 0x80)
                                                            d7 = "(有操作) " + d7;
                                                        else
                                                            d7 = "(无操作) " + d7;
                                                        d7 = d7.PadRight(27 - GetChineseNumber(d7) + 3);
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|          采集时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|                D7 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d7));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|                D6 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d6));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|                D5 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d5));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|                D4 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d4));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|                D3 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d3));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|                D2 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d2));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|                D1 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d1));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|                D0 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d0));
                                                        LogMessage("+-------------------+----------------------------+");

                                                        if (NeedReport == true)
                                                        {
                                                            if (_pdfDocument == null)
                                                            {
                                                                LogMessageError("无法创建报表.");
                                                            }
                                                            else
                                                            {
                                                                #region

                                                                try
                                                                {
                                                                    string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                                    BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                                    PdfParagraph par = new PdfParagraph("--- 状态信号配置信息 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                                    par.Alignment = Element.ALIGN_CENTER;
                                                                    _pdfDocument.Add(par);

                                                                    par.SpacingBefore = 25f;

                                                                    PdfPTable table = new PdfPTable(2);

                                                                    table.SpacingBefore = 25f;

                                                                    table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                                    float[] widths = { 50f, 150f };
                                                                    table.SetWidths(widths);
                                                                    table.LockedWidth = true;

                                                                    PdfPCell cell;
                                                                    cell = new PdfPCell(new Phrase("采集时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(number.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("D7", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d7.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("D6", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d6.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("D5", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d4.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("D4", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d4.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("D3", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d3.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("D2", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d2.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("D1", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d1.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("D0", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d0.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);

                                                                    _pdfDocument.Add(table);
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    LogMessageError("创建报表出错:" + ex.Message);
                                                                    _pdfDocument = null;
                                                                }

                                                                #endregion
                                                            }
                                                        }
                                                    }
                                                }
                                                #endregion
                                                break;
                                            case "07":
                                                #region
                                                {
                                                    if (dataLen != 35)
                                                        LogMessageError("命令(" + cdi.CmdContent + ")的响应的数据块长度错误.");
                                                    else
                                                    {
                                                        string ccc = Encoding.ASCII.GetString(baData, 0, 7).Trim('\0').PadRight(27);
                                                        string model = Encoding.ASCII.GetString(baData, 7, 16).Trim('\0').PadRight(27);
                                                        string number = "20" + baData[23].ToString("X") + "-" + baData[24].ToString("X") + "-" + baData[25].ToString("X");
                                                        number = number.PadRight(27);
                                                        long flow = baData[26] * 256 * 256 * 256 + baData[27] * 256 * 256 + baData[28] * 256 + baData[29];
                                                        string productflow = flow.ToString().PadRight(27);
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("| 生产厂CCC认证代码 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", ccc));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|      认证产品型号 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", model));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|    记录仪生产时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|    产品生产流水号 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", productflow));
                                                        LogMessage("+-------------------+----------------------------+");

                                                        if (NeedReport == true)
                                                        {
                                                            if (_pdfDocument == null)
                                                            {
                                                                LogMessageError("无法创建报表.");
                                                            }
                                                            else
                                                            {
                                                                #region

                                                                try
                                                                {
                                                                    string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                                    BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                                    PdfParagraph par = new PdfParagraph("--- 记录仪唯一性编号 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                                    par.Alignment = Element.ALIGN_CENTER;
                                                                    _pdfDocument.Add(par);

                                                                    par.SpacingBefore = 25f;

                                                                    PdfPTable table = new PdfPTable(2);

                                                                    table.SpacingBefore = 25f;

                                                                    table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                                    float[] widths = { 50f, 150f };
                                                                    table.SetWidths(widths);
                                                                    table.LockedWidth = true;

                                                                    PdfPCell cell;
                                                                    cell = new PdfPCell(new Phrase("生产厂CCC认证代码", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(ccc.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("认证产品型号", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(model.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("记录仪生产时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(number.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("产品生产流水号", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(productflow.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);

                                                                    _pdfDocument.Add(table);
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    LogMessageError("创建报表出错:" + ex.Message);
                                                                    _pdfDocument = null;
                                                                }

                                                                #endregion
                                                            }
                                                        }
                                                    }
                                                }
                                                #endregion
                                                break;
                                            case "08":
                                                #region
                                                {
                                                    string number1 = cdi.StartDateTime.Year.ToString("") + "-" +
                                                        cdi.StartDateTime.Month.ToString("") + "-" +
                                                        cdi.StartDateTime.Day.ToString("") + " " +
                                                        cdi.StartDateTime.Hour.ToString("") + ":" +
                                                        cdi.StartDateTime.Minute.ToString("") + ":" +
                                                        cdi.StartDateTime.Second.ToString("");
                                                    number1 = number1.PadRight(27);
                                                    string number2 = cdi.StopDateTime.Year.ToString("") + "-" +
                                                        cdi.StopDateTime.Month.ToString("") + "-" +
                                                        cdi.StopDateTime.Day.ToString("") + " " +
                                                        cdi.StopDateTime.Hour.ToString("") + ":" +
                                                        cdi.StopDateTime.Minute.ToString("") + ":" +
                                                        cdi.StopDateTime.Second.ToString("");
                                                    number2 = number2.PadRight(27);

                                                    int blockCount = (int)(Math.Floor((double)dataLen / (double)126));
                                                    int dataRemain = dataLen % 126;
                                                    string sValue = (dataLen.ToString() + "/" + blockCount.ToString()).PadRight(27);
                                                    if (isContinued == false)
                                                    {
                                                        LogMessage("+-----------------------------------------------------------------------------+");
                                                        LogMessage("| (此处只显示捕获的数据信息,详细内容请见报表.)                                |");
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                        LogMessage("|      采集起始时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number1));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                        LogMessage("|      采集停止时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number2));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    DateTime lastDateTime = DateTime.Now;
                                                    if (blockCount > 0)
                                                    {
                                                        string numberblock = "";
                                                        for (int iblock = 0; iblock < blockCount; iblock++)
                                                        {
                                                            numberblock = "20" + baData[126 * iblock + 0].ToString("X") + "-" +
                                                                baData[126 * iblock + 1].ToString("X") + "-" +
                                                                baData[126 * iblock + 2].ToString("X") + " " +
                                                                baData[126 * iblock + 3].ToString("X") + ":" +
                                                                baData[126 * iblock + 4].ToString("X") + ":" +
                                                                baData[126 * iblock + 5].ToString("X");
                                                            numberblock = numberblock.PadRight(27);
                                                            lastDateTime = new DateTime(
                                                                ((int)Math.Floor((double)baData[126 * iblock + 0] / 16.0)) * 10 + baData[126 * iblock + 0] % 16 + 2000,
                                                                ((int)Math.Floor((double)baData[126 * iblock + 1] / 16.0)) * 10 + baData[126 * iblock + 1] % 16,
                                                                ((int)Math.Floor((double)baData[126 * iblock + 2] / 16.0)) * 10 + baData[126 * iblock + 2] % 16,
                                                                ((int)Math.Floor((double)baData[126 * iblock + 3] / 16.0)) * 10 + baData[126 * iblock + 3] % 16,
                                                                ((int)Math.Floor((double)baData[126 * iblock + 4] / 16.0)) * 10 + baData[126 * iblock + 4] % 16,
                                                                ((int)Math.Floor((double)baData[126 * iblock + 5] / 16.0)) * 10 + baData[126 * iblock + 5] % 16);
                                                            lastDateTime = lastDateTime.Subtract(new TimeSpan(0, 0, 1));//blockCount, 0));
                                                            ObservableCollection<Tuple<int, byte>> records = new ObservableCollection<Tuple<int, byte>>();
                                                            for (int iSec = 0; iSec < 60; iSec++)
                                                            {
                                                                int speed = baData[126 * iblock + iSec * 2 + 6 + 0];
                                                                if (speed == 0xFF)
                                                                    speed = 0;
                                                                byte state = baData[126 * iblock + iSec * 2 + 6 + 1];

                                                                records.Add(new Tuple<int, byte>(speed, state));
                                                            }
                                                            _cmd08HRespOc.Add(new Cmd08HResponse()
                                                            {
                                                                Index = (_cmd08HRespOc.Count + 1).ToString(),
                                                                StartDateTime = numberblock,
                                                                Records = records
                                                            });
                                                        }

                                                        LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$| @@@@@@@@@@@@@@@@@@@@@@@@@@@|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue).Replace("@@@@@@@@@@@@@@@@@@@@@@@@@@@", numberblock));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    else
                                                    {
                                                        LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    if (blockCount > 0)
                                                    {
                                                        //DateTime lastDateTime = new DateTime(2000 + int.Parse(sa[12]), int.Parse(sa[13]), int.Parse(sa[14]),
                                                        //    int.Parse(sa[15]), int.Parse(sa[16]), int.Parse(sa[17]));
                                                        //lastDateTime = lastDateTime.Subtract(new TimeSpan(0, blockCount, 0));
                                                        if (lastDateTime.Subtract(cdi.StartDateTime).TotalSeconds >= 0.0)
                                                        {
                                                            isContinued = true;
                                                            string[] saNew = newCmdContinue.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                                            saNew[12] = lastDateTime.Year.ToString();
                                                            saNew[12] = saNew[12].Substring(saNew[12].Length - 2, 2);
                                                            saNew[13] = lastDateTime.Month.ToString();
                                                            saNew[13] = saNew[13].PadLeft(2, '0');
                                                            saNew[14] = lastDateTime.Day.ToString();
                                                            saNew[14] = saNew[14].PadLeft(2, '0');
                                                            saNew[15] = lastDateTime.Hour.ToString();
                                                            saNew[15] = saNew[15].PadLeft(2, '0');
                                                            saNew[16] = lastDateTime.Minute.ToString();
                                                            saNew[16] = saNew[16].PadLeft(2, '0');
                                                            saNew[17] = lastDateTime.Second.ToString();
                                                            saNew[17] = saNew[17].PadLeft(2, '0');
                                                            newCmdContinue = "";
                                                            foreach (string saNewi in saNew)
                                                            {
                                                                newCmdContinue = newCmdContinue + " " + saNewi;
                                                            }
                                                            newCmdContinue = newCmdContinue.Trim();
                                                            newCmdContinue = newCmdContinue.Substring(0, newCmdContinue.Length - 3);
                                                            newCmdContinue = newCmdContinue + " " + CmdDefinition.XORData(newCmdContinue);
                                                        }
                                                        else
                                                        {
                                                            LogMessage("| 数据总数/数据块数 | 0/0                        |                            |");
                                                            LogMessage("+-------------------+----------------------------+----------------------------+");
                                                            isContinued = false;
                                                        }
                                                    }
                                                    else
                                                        isContinued = false;
                                                    if (isContinued == false && NeedReport == true && _pdfDocument != null)
                                                    {
                                                        _createPdfEvent.Reset();
                                                        Task.Factory.StartNew(() =>
                                                        {
                                                            Create08HReport();
                                                        });
                                                        _createPdfEvent.WaitOne();
                                                    }
                                                }
                                                #endregion
                                                break;
                                            case "09":
                                                #region
                                                {
                                                    string number1 = cdi.StartDateTime.Year.ToString("") + "-" +
                                                        cdi.StartDateTime.Month.ToString("") + "-" +
                                                        cdi.StartDateTime.Day.ToString("") + " " +
                                                        cdi.StartDateTime.Hour.ToString("") + ":" +
                                                        cdi.StartDateTime.Minute.ToString("") + ":" +
                                                        cdi.StartDateTime.Second.ToString("");
                                                    number1 = number1.PadRight(27);
                                                    string number2 = cdi.StopDateTime.Year.ToString("") + "-" +
                                                        cdi.StopDateTime.Month.ToString("") + "-" +
                                                        cdi.StopDateTime.Day.ToString("") + " " +
                                                        cdi.StopDateTime.Hour.ToString("") + ":" +
                                                        cdi.StopDateTime.Minute.ToString("") + ":" +
                                                        cdi.StopDateTime.Second.ToString("");
                                                    number2 = number2.PadRight(27);

                                                    int blockCount = (int)(Math.Floor((double)dataLen / (double)666));
                                                    int dataRemain = dataLen % 666;
                                                    string sValue = (dataLen.ToString() + "/" + blockCount.ToString()).PadRight(27);
                                                    if (isContinued == false)
                                                    {
                                                        LogMessage("+-----------------------------------------------------------------------------+");
                                                        LogMessage("| (此处只显示捕获的数据信息,详细内容请见报表.)                                |");
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                        LogMessage("|      采集起始时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number1));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                        LogMessage("|      采集停止时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number2));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    DateTime lastDateTime = DateTime.Now;
                                                    if (blockCount > 0)
                                                    {
                                                        string numberblock = "";
                                                        for (int iblock = 0; iblock < blockCount; iblock++)
                                                        {
                                                            numberblock = "20" + baData[666 * iblock + 0].ToString("X") + "-" +
                                                                baData[666 * iblock + 1].ToString("X") + "-" +
                                                                baData[666 * iblock + 2].ToString("X") + " " +
                                                                baData[666 * iblock + 3].ToString("X") + ":" +
                                                                baData[666 * iblock + 4].ToString("X") + ":" +
                                                                baData[666 * iblock + 5].ToString("X");
                                                            numberblock = numberblock.PadRight(27);
                                                            lastDateTime = new DateTime(
                                                                ((int)Math.Floor((double)baData[666 * iblock + 0] / 16.0)) * 10 + baData[666 * iblock + 0] % 16 + 2000,
                                                                ((int)Math.Floor((double)baData[666 * iblock + 1] / 16.0)) * 10 + baData[666 * iblock + 1] % 16,
                                                                ((int)Math.Floor((double)baData[666 * iblock + 2] / 16.0)) * 10 + baData[666 * iblock + 2] % 16,
                                                                ((int)Math.Floor((double)baData[666 * iblock + 3] / 16.0)) * 10 + baData[666 * iblock + 3] % 16,
                                                                ((int)Math.Floor((double)baData[666 * iblock + 4] / 16.0)) * 10 + baData[666 * iblock + 4] % 16,
                                                                ((int)Math.Floor((double)baData[666 * iblock + 5] / 16.0)) * 10 + baData[666 * iblock + 5] % 16);
                                                            int min = lastDateTime.Minute;
                                                            int sec = lastDateTime.Second;
                                                            lastDateTime = lastDateTime.Subtract(new TimeSpan(1, -59 + min, -59 + sec));
                                                            ObservableCollection<Tuple<string, string, string, int>> records = new ObservableCollection<Tuple<string, string, string, int>>();
                                                            for (int iMin = 0; iMin < 60; iMin++)
                                                            {
                                                                #region

                                                                byte[] baJingDu = new byte[4];
                                                                baJingDu[0] = baData[666 * iblock + iMin * 11 + 6 + 0];
                                                                baJingDu[1] = baData[666 * iblock + iMin * 11 + 6 + 1];
                                                                baJingDu[2] = baData[666 * iblock + iMin * 11 + 6 + 2];
                                                                baJingDu[3] = baData[666 * iblock + iMin * 11 + 6 + 3];
                                                                string sJingDu = "";
                                                                if (baData[666 * iblock + iMin * 11 + 6 + 0] == 0xFF ||
                                                                    baData[666 * iblock + iMin * 11 + 6 + 1] == 0xFF ||
                                                                    baData[666 * iblock + iMin * 11 + 6 + 2] == 0xFF ||
                                                                    baData[666 * iblock + iMin * 11 + 6 + 3] == 0xFF)
                                                                    sJingDu = "无效";
                                                                else
                                                                {
                                                                    float jingDu = System.BitConverter.ToSingle(baJingDu, 0);
                                                                    if (jingDu >= 0)
                                                                        sJingDu = "E" + ConvertJingWeiDuToString(jingDu);
                                                                    else
                                                                        sJingDu = "W" + ConvertJingWeiDuToString(jingDu);
                                                                }
                                                                byte[] baWeiDu = new byte[4];
                                                                baWeiDu[0] = baData[666 * iblock + iMin * 11 + 6 + 4];
                                                                baWeiDu[1] = baData[666 * iblock + iMin * 11 + 6 + 5];
                                                                baWeiDu[2] = baData[666 * iblock + iMin * 11 + 6 + 6];
                                                                baWeiDu[3] = baData[666 * iblock + iMin * 11 + 6 + 7];
                                                                string sWeiDu = "";
                                                                if (baData[666 * iblock + iMin * 11 + 6 + 4] == 0xFF ||
                                                                    baData[666 * iblock + iMin * 11 + 6 + 5] == 0xFF ||
                                                                    baData[666 * iblock + iMin * 11 + 6 + 6] == 0xFF ||
                                                                    baData[666 * iblock + iMin * 11 + 6 + 7] == 0xFF)
                                                                    sWeiDu = "无效";
                                                                else
                                                                {
                                                                    float weiDu = System.BitConverter.ToSingle(baWeiDu, 0);
                                                                    if (weiDu >= 0)
                                                                        sWeiDu = "N" + ConvertJingWeiDuToString(weiDu);
                                                                    else
                                                                        sWeiDu = "S" + ConvertJingWeiDuToString(weiDu);
                                                                }
                                                                byte[] baHeight = new byte[2];
                                                                baHeight[0] = baData[666 * iblock + iMin * 11 + 6 + 8];
                                                                baHeight[1] = baData[666 * iblock + iMin * 11 + 6 + 9];
                                                                string sHeight = "";
                                                                if (baData[666 * iblock + iMin * 11 + 6 + 8] == 0xFF ||
                                                                    baData[666 * iblock + iMin * 11 + 6 + 9] == 0xFF)
                                                                    sHeight = "无效";
                                                                else
                                                                {
                                                                    int iHeight = System.BitConverter.ToInt16(baHeight, 0);
                                                                    sHeight = iHeight.ToString();
                                                                }

                                                                #endregion

                                                                int speed = baData[666 * iblock + iMin * 11 + 6 + 10];
                                                                if (speed == 0xFF)
                                                                    speed = 0;

                                                                records.Add(new Tuple<string, string, string, int>(sJingDu, sWeiDu, sHeight, speed));
                                                            }
                                                            _cmd09HRespOc.Add(new Cmd09HResponse()
                                                            {
                                                                Index = (_cmd09HRespOc.Count + 1).ToString(),
                                                                StartDateTime = numberblock,
                                                                Records = records
                                                            });
                                                        }

                                                        LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$| @@@@@@@@@@@@@@@@@@@@@@@@@@@|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue).Replace("@@@@@@@@@@@@@@@@@@@@@@@@@@@", numberblock));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    else
                                                    {
                                                        LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    if (dataRemain != 0)
                                                    {
                                                        string sValue3 = dataRemain.ToString().PadRight(27);
                                                        LogMessage("|        错误数据数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue3));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    if (blockCount > 0)
                                                    {
                                                        //lastDateTime = new DateTime(2000 + int.Parse(sa[12]), int.Parse(sa[13]), int.Parse(sa[14]),
                                                        //    int.Parse(sa[15]), int.Parse(sa[16]), int.Parse(sa[17]));
                                                        //lastDateTime = lastDateTime.Subtract(new TimeSpan(blockCount, 0, 0));
                                                        if (lastDateTime.Subtract(cdi.StartDateTime).TotalSeconds >= 0.0)
                                                        {
                                                            isContinued = true;
                                                            string[] saNew = newCmdContinue.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                                            saNew[12] = lastDateTime.Year.ToString();
                                                            saNew[12] = saNew[12].Substring(saNew[12].Length - 2, 2);
                                                            saNew[13] = lastDateTime.Month.ToString();
                                                            saNew[13] = saNew[13].PadLeft(2, '0');
                                                            saNew[14] = lastDateTime.Day.ToString();
                                                            saNew[14] = saNew[14].PadLeft(2, '0');
                                                            saNew[15] = lastDateTime.Hour.ToString();
                                                            saNew[15] = saNew[15].PadLeft(2, '0');
                                                            saNew[16] = lastDateTime.Minute.ToString();
                                                            saNew[16] = saNew[16].PadLeft(2, '0');
                                                            saNew[17] = lastDateTime.Second.ToString();
                                                            saNew[17] = saNew[17].PadLeft(2, '0');
                                                            newCmdContinue = "";
                                                            foreach (string saNewi in saNew)
                                                            {
                                                                newCmdContinue = newCmdContinue + " " + saNewi;
                                                            }
                                                            newCmdContinue = newCmdContinue.Trim();
                                                            newCmdContinue = newCmdContinue.Substring(0, newCmdContinue.Length - 3);
                                                            newCmdContinue = newCmdContinue + " " + CmdDefinition.XORData(newCmdContinue);
                                                        }
                                                        else
                                                        {
                                                            LogMessage("| 数据总数/数据块数 | 0/0                        |                            |");
                                                            LogMessage("+-------------------+----------------------------+----------------------------+");
                                                            isContinued = false;
                                                        }
                                                    }
                                                    else
                                                        isContinued = false;
                                                    if (isContinued == false && NeedReport == true && _pdfDocument != null)
                                                    {
                                                        _createPdfEvent.Reset();
                                                        Task.Factory.StartNew(() =>
                                                        {
                                                            Create09HReport();
                                                        });
                                                        _createPdfEvent.WaitOne();
                                                    }
                                                }
                                                #endregion
                                                break;
                                            case "10":
                                                #region
                                                {
                                                    string number1 = cdi.StartDateTime.Year.ToString("") + "-" +
                                                        cdi.StartDateTime.Month.ToString("") + "-" +
                                                        cdi.StartDateTime.Day.ToString("") + " " +
                                                        cdi.StartDateTime.Hour.ToString("") + ":" +
                                                        cdi.StartDateTime.Minute.ToString("") + ":" +
                                                        cdi.StartDateTime.Second.ToString("");
                                                    number1 = number1.PadRight(27);
                                                    string number2 = cdi.StopDateTime.Year.ToString("") + "-" +
                                                        cdi.StopDateTime.Month.ToString("") + "-" +
                                                        cdi.StopDateTime.Day.ToString("") + " " +
                                                        cdi.StopDateTime.Hour.ToString("") + ":" +
                                                        cdi.StopDateTime.Minute.ToString("") + ":" +
                                                        cdi.StopDateTime.Second.ToString("");
                                                    number2 = number2.PadRight(27);

                                                    int blockCount = (int)(Math.Floor((double)dataLen / (double)234));
                                                    int dataRemain = dataLen % 234;
                                                    string sValue = (dataLen.ToString() + "/" + blockCount.ToString()).PadRight(27);
                                                    if (isContinued == false)
                                                    {
                                                        LogMessage("+-----------------------------------------------------------------------------+");
                                                        LogMessage("| (此处只显示捕获的数据信息,详细内容请见报表.)                                |");
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                        LogMessage("|      采集起始时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number1));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                        LogMessage("|      采集停止时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number2));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    DateTime lastDateTime = DateTime.Now;
                                                    if (blockCount > 0)
                                                    {
                                                        string numberblock = "";
                                                        for (int iblock = 0; iblock < blockCount; iblock++)
                                                        {
                                                            numberblock = "20" + baData[234 * iblock + 0].ToString("X") + "-" +
                                                                baData[234 * iblock + 1].ToString("X") + "-" +
                                                                baData[234 * iblock + 2].ToString("X") + " " +
                                                                baData[234 * iblock + 3].ToString("X") + ":" +
                                                                baData[234 * iblock + 4].ToString("X") + ":" +
                                                                baData[234 * iblock + 5].ToString("X");
                                                            numberblock = numberblock.PadRight(27);
                                                            lastDateTime = new DateTime(
                                                                ((int)Math.Floor((double)baData[234 * iblock + 0] / 16.0)) * 10 + baData[234 * iblock + 0] % 16 + 2000,
                                                                ((int)Math.Floor((double)baData[234 * iblock + 1] / 16.0)) * 10 + baData[234 * iblock + 1] % 16,
                                                                ((int)Math.Floor((double)baData[234 * iblock + 2] / 16.0)) * 10 + baData[234 * iblock + 2] % 16,
                                                                ((int)Math.Floor((double)baData[234 * iblock + 3] / 16.0)) * 10 + baData[234 * iblock + 3] % 16,
                                                                ((int)Math.Floor((double)baData[234 * iblock + 4] / 16.0)) * 10 + baData[234 * iblock + 4] % 16,
                                                                ((int)Math.Floor((double)baData[234 * iblock + 5] / 16.0)) * 10 + baData[234 * iblock + 5] % 16);
                                                            lastDateTime = lastDateTime.Subtract(new TimeSpan(0, 0, 20));
                                                            byte[] baNumber = new byte[18];
                                                            for (int idxBa = 0; idxBa < 18; idxBa++)
                                                            {
                                                                baNumber[idxBa] = baData[234 * iblock + 6 + idxBa];
                                                            }
                                                            string number = Encoding.UTF8.GetString(baNumber).PadRight(27);
                                                            ObservableCollection<Tuple<int, bool>> records = new ObservableCollection<Tuple<int, bool>>();
                                                            for (int iRec = 0; iRec < 100; iRec++)
                                                            {
                                                                int speed = (int)baData[234 * iblock + 24 + iRec * 2 + 0];
                                                                if (speed == 0xFF)
                                                                    speed = 0;
                                                                bool state = (((int)baData[234 * iblock + 24 + iRec * 2 + 1] & 1) == 1) ? true : false;
                                                                records.Add(new Tuple<int, bool>(speed, state));
                                                            }

                                                            #region

                                                            byte[] baJingDu = new byte[4];
                                                            baJingDu[0] = baData[234 * iblock + 224 + 0];
                                                            baJingDu[1] = baData[234 * iblock + 224 + 1];
                                                            baJingDu[2] = baData[234 * iblock + 224 + 2];
                                                            baJingDu[3] = baData[234 * iblock + 224 + 3];
                                                            float jingDu = System.BitConverter.ToSingle(baJingDu, 0);
                                                            string sJingDu = "";
                                                            if (jingDu >= 0)
                                                                sJingDu = "E" + ConvertJingWeiDuToString(jingDu);
                                                            else
                                                                sJingDu = "W" + ConvertJingWeiDuToString(jingDu);
                                                            byte[] baWeiDu = new byte[4];
                                                            baWeiDu[0] = baData[234 * iblock + 224 + 4];
                                                            baWeiDu[1] = baData[234 * iblock + 224 + 5];
                                                            baWeiDu[2] = baData[234 * iblock + 224 + 6];
                                                            baWeiDu[3] = baData[234 * iblock + 224 + 7];
                                                            float weiDu = System.BitConverter.ToSingle(baWeiDu, 0);
                                                            string sWeiDu = "";
                                                            if (weiDu >= 0)
                                                                sWeiDu = "N" + ConvertJingWeiDuToString(weiDu);
                                                            else
                                                                sWeiDu = "S" + ConvertJingWeiDuToString(weiDu);
                                                            byte[] baHeight = new byte[2];
                                                            baHeight[0] = baData[234 * iblock + 224 + 8];
                                                            baHeight[1] = baData[234 * iblock + 224 + 9];
                                                            int iHeight = System.BitConverter.ToInt16(baHeight, 0);
                                                            string sHeight = iHeight.ToString();

                                                            #endregion

                                                            _cmd10HRespOc.Add(new Cmd10HResponse()
                                                            {
                                                                Index = (_cmd10HRespOc.Count + 1).ToString(),
                                                                StopDateTime = numberblock,
                                                                Number = number,
                                                                Records = records,
                                                                Position = sJingDu + "/" + sWeiDu,
                                                                Height = sHeight
                                                            });
                                                        }
                                                        LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$| @@@@@@@@@@@@@@@@@@@@@@@@@@@|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue).Replace("@@@@@@@@@@@@@@@@@@@@@@@@@@@", numberblock));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    else
                                                    {
                                                        LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    if (dataRemain != 0)
                                                    {
                                                        string sValue3 = dataRemain.ToString().PadRight(27);
                                                        LogMessage("|        错误数据数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue3));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    if (blockCount > 0)
                                                    {
                                                        if (lastDateTime.Subtract(cdi.StartDateTime).TotalSeconds >= 0.0)
                                                        {
                                                            isContinued = true;
                                                            string[] saNew = newCmdContinue.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                                            saNew[12] = lastDateTime.Year.ToString();
                                                            saNew[12] = saNew[12].Substring(saNew[12].Length - 2, 2);
                                                            saNew[13] = lastDateTime.Month.ToString();
                                                            saNew[13] = saNew[13].PadLeft(2, '0');
                                                            saNew[14] = lastDateTime.Day.ToString();
                                                            saNew[14] = saNew[14].PadLeft(2, '0');
                                                            saNew[15] = lastDateTime.Hour.ToString();
                                                            saNew[15] = saNew[15].PadLeft(2, '0');
                                                            saNew[16] = lastDateTime.Minute.ToString();
                                                            saNew[16] = saNew[16].PadLeft(2, '0');
                                                            saNew[17] = lastDateTime.Second.ToString();
                                                            saNew[17] = saNew[17].PadLeft(2, '0');
                                                            newCmdContinue = "";
                                                            foreach (string saNewi in saNew)
                                                            {
                                                                newCmdContinue = newCmdContinue + " " + saNewi;
                                                            }
                                                            newCmdContinue = newCmdContinue.Trim();
                                                            newCmdContinue = newCmdContinue.Substring(0, newCmdContinue.Length - 3);
                                                            newCmdContinue = newCmdContinue + " " + CmdDefinition.XORData(newCmdContinue);
                                                        }
                                                        else
                                                        {
                                                            LogMessage("| 数据总数/数据块数 | 0/0                        |                            |");
                                                            LogMessage("+-------------------+----------------------------+----------------------------+");
                                                            isContinued = false;
                                                        }
                                                    }
                                                    else
                                                        isContinued = false;
                                                    if (isContinued == false && NeedReport == true && _pdfDocument != null)
                                                    {
                                                        _createPdfEvent.Reset();
                                                        Task.Factory.StartNew(() =>
                                                        {
                                                            Create10HReport();
                                                        });
                                                        _createPdfEvent.WaitOne();
                                                    }
                                                }
                                                #endregion
                                                break;
                                            case "11":
                                                #region
                                                {
                                                    string number1 = cdi.StartDateTime.Year.ToString("") + "-" +
                                                        cdi.StartDateTime.Month.ToString("") + "-" +
                                                        cdi.StartDateTime.Day.ToString("") + " " +
                                                        cdi.StartDateTime.Hour.ToString("") + ":" +
                                                        cdi.StartDateTime.Minute.ToString("") + ":" +
                                                        cdi.StartDateTime.Second.ToString("");
                                                    number1 = number1.PadRight(27);
                                                    string number2 = cdi.StopDateTime.Year.ToString("") + "-" +
                                                        cdi.StopDateTime.Month.ToString("") + "-" +
                                                        cdi.StopDateTime.Day.ToString("") + " " +
                                                        cdi.StopDateTime.Hour.ToString("") + ":" +
                                                        cdi.StopDateTime.Minute.ToString("") + ":" +
                                                        cdi.StopDateTime.Second.ToString("");
                                                    number2 = number2.PadRight(27);

                                                    int blockCount = (int)(Math.Floor((double)dataLen / (double)50));
                                                    int dataRemain = dataLen % 50;
                                                    string sValue = (dataLen.ToString() + "/" + blockCount.ToString()).PadRight(27);
                                                    if (isContinued == false)
                                                    {
                                                        LogMessage("+-----------------------------------------------------------------------------+");
                                                        LogMessage("| (此处只显示捕获的数据信息,详细内容请见报表.)                                |");
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                        LogMessage("|      采集起始时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number1));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                        LogMessage("|      采集停止时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number2));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    DateTime lastDateTime = DateTime.Now;
                                                    if (blockCount > 0)
                                                    {
                                                        string numberblock = "";
                                                        for (int iblock = 0; iblock < blockCount; iblock++)
                                                        {
                                                            numberblock = "20" + baData[50 * iblock + 18].ToString("X") + "-" +
                                                                baData[50 * iblock + 19].ToString("X") + "-" +
                                                                baData[50 * iblock + 20].ToString("X") + " " +
                                                                baData[50 * iblock + 21].ToString("X") + ":" +
                                                                baData[50 * iblock + 22].ToString("X") + ":" +
                                                                baData[50 * iblock + 23].ToString("X");
                                                            numberblock = numberblock.PadRight(27);
                                                            lastDateTime = new DateTime(
                                                                ((int)Math.Floor((double)baData[50 * iblock + 18] / 16.0)) * 10 + baData[50 * iblock + 18] % 16 + 2000,
                                                                ((int)Math.Floor((double)baData[50 * iblock + 19] / 16.0)) * 10 + baData[50 * iblock + 19] % 16,
                                                                ((int)Math.Floor((double)baData[50 * iblock + 20] / 16.0)) * 10 + baData[50 * iblock + 20] % 16,
                                                                ((int)Math.Floor((double)baData[50 * iblock + 21] / 16.0)) * 10 + baData[50 * iblock + 21] % 16,
                                                                ((int)Math.Floor((double)baData[50 * iblock + 22] / 16.0)) * 10 + baData[50 * iblock + 22] % 16,
                                                                ((int)Math.Floor((double)baData[50 * iblock + 23] / 16.0)) * 10 + baData[50 * iblock + 23] % 16);
                                                            lastDateTime = lastDateTime.Subtract(new TimeSpan(0, 0, 1));
                                                            string numberblockStop = "20" + baData[50 * iblock + 14].ToString("X") + "-" +
                                                                baData[50 * iblock + 25].ToString("X") + "-" +
                                                                baData[50 * iblock + 26].ToString("X") + " " +
                                                                baData[50 * iblock + 27].ToString("X") + ":" +
                                                                baData[50 * iblock + 28].ToString("X") + ":" +
                                                                baData[50 * iblock + 29].ToString("X");
                                                            numberblock = numberblock.PadRight(27);
                                                            byte[] baNumber = new byte[18];
                                                            for (int idxBa = 0; idxBa < 18; idxBa++)
                                                            {
                                                                baNumber[idxBa] = baData[50 * iblock + idxBa];
                                                            }
                                                            string number = Encoding.UTF8.GetString(baNumber).PadRight(27);

                                                            #region

                                                            byte[] baJingDu = new byte[4];
                                                            baJingDu[0] = baData[50 * iblock + 30];
                                                            baJingDu[1] = baData[50 * iblock + 31];
                                                            baJingDu[2] = baData[50 * iblock + 32];
                                                            baJingDu[3] = baData[50 * iblock + 33];
                                                            float jingDu = System.BitConverter.ToSingle(baJingDu, 0);
                                                            string sJingDu = "";
                                                            if (jingDu >= 0)
                                                                sJingDu = "E" + ConvertJingWeiDuToString(jingDu);
                                                            else
                                                                sJingDu = "W" + ConvertJingWeiDuToString(jingDu);
                                                            byte[] baWeiDu = new byte[4];
                                                            baWeiDu[0] = baData[50 * iblock + 34];
                                                            baWeiDu[1] = baData[50 * iblock + 35];
                                                            baWeiDu[2] = baData[50 * iblock + 36];
                                                            baWeiDu[3] = baData[50 * iblock + 37];
                                                            float weiDu = System.BitConverter.ToSingle(baWeiDu, 0);
                                                            string sWeiDu = "";
                                                            if (weiDu >= 0)
                                                                sWeiDu = "N" + ConvertJingWeiDuToString(weiDu);
                                                            else
                                                                sWeiDu = "S" + ConvertJingWeiDuToString(weiDu);
                                                            byte[] baHeight = new byte[2];
                                                            baHeight[0] = baData[50 * iblock + 38];
                                                            baHeight[1] = baData[50 * iblock + 39];
                                                            int iHeight = System.BitConverter.ToInt16(baHeight, 0);
                                                            string sHeight = iHeight.ToString();

                                                            #endregion

                                                            #region

                                                            byte[] baJingDu1 = new byte[4];
                                                            baJingDu1[0] = baData[50 * iblock + 40];
                                                            baJingDu1[1] = baData[50 * iblock + 41];
                                                            baJingDu1[2] = baData[50 * iblock + 42];
                                                            baJingDu1[3] = baData[50 * iblock + 43];
                                                            float jingDu1 = System.BitConverter.ToSingle(baJingDu1, 0);
                                                            string sJingDu1 = "";
                                                            if (jingDu1 >= 0)
                                                                sJingDu1 = "E" + ConvertJingWeiDuToString(jingDu1);
                                                            else
                                                                sJingDu1 = "W" + ConvertJingWeiDuToString(jingDu1);
                                                            byte[] baWeiDu1 = new byte[4];
                                                            baWeiDu1[0] = baData[50 * iblock + 44];
                                                            baWeiDu1[1] = baData[50 * iblock + 45];
                                                            baWeiDu1[2] = baData[50 * iblock + 46];
                                                            baWeiDu1[3] = baData[50 * iblock + 47];
                                                            float weiDu1 = System.BitConverter.ToSingle(baWeiDu1, 0);
                                                            string sWeiDu1 = "";
                                                            if (weiDu1 >= 0)
                                                                sWeiDu1 = "N" + ConvertJingWeiDuToString(weiDu1);
                                                            else
                                                                sWeiDu1 = "S" + ConvertJingWeiDuToString(weiDu1);
                                                            byte[] baHeight1 = new byte[2];
                                                            baHeight1[0] = baData[50 * iblock + 48];
                                                            baHeight1[1] = baData[50 * iblock + 49];
                                                            int iHeight1 = System.BitConverter.ToInt16(baHeight1, 0);
                                                            string sHeight1 = iHeight1.ToString();

                                                            #endregion

                                                            _cmd11HRespOc.Add(new Cmd11HResponse()
                                                            {
                                                                Index = (_cmd11HRespOc.Count + 1).ToString(),
                                                                Number = number,
                                                                RecordStartDateTime = numberblock,
                                                                RecordStopDateTime = numberblockStop,
                                                                StartPosition = sJingDu + "/" + sWeiDu,
                                                                StopPosition = sJingDu1 + "/" + sWeiDu1,
                                                                StartHeight = sHeight,
                                                                StopHeight = sHeight1
                                                            });
                                                        }
                                                        LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$| @@@@@@@@@@@@@@@@@@@@@@@@@@@|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue).Replace("@@@@@@@@@@@@@@@@@@@@@@@@@@@", numberblock));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    else
                                                    {
                                                        LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    if (dataRemain != 0)
                                                    {
                                                        string sValue3 = dataRemain.ToString().PadRight(27);
                                                        LogMessage("|        错误数据数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue3));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    if (blockCount > 0)
                                                    {
                                                        if (lastDateTime.Subtract(cdi.StartDateTime).TotalSeconds >= 0.0)
                                                        {
                                                            isContinued = true;
                                                            string[] saNew = newCmdContinue.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                                            saNew[12] = lastDateTime.Year.ToString();
                                                            saNew[12] = saNew[12].Substring(saNew[12].Length - 2, 2);
                                                            saNew[13] = lastDateTime.Month.ToString();
                                                            saNew[13] = saNew[13].PadLeft(2, '0');
                                                            saNew[14] = lastDateTime.Day.ToString();
                                                            saNew[14] = saNew[14].PadLeft(2, '0');
                                                            saNew[15] = lastDateTime.Hour.ToString();
                                                            saNew[15] = saNew[15].PadLeft(2, '0');
                                                            saNew[16] = lastDateTime.Minute.ToString();
                                                            saNew[16] = saNew[16].PadLeft(2, '0');
                                                            saNew[17] = lastDateTime.Second.ToString();
                                                            saNew[17] = saNew[17].PadLeft(2, '0');
                                                            newCmdContinue = "";
                                                            foreach (string saNewi in saNew)
                                                            {
                                                                newCmdContinue = newCmdContinue + " " + saNewi;
                                                            }
                                                            newCmdContinue = newCmdContinue.Trim();
                                                            newCmdContinue = newCmdContinue.Substring(0, newCmdContinue.Length - 3);
                                                            newCmdContinue = newCmdContinue + " " + CmdDefinition.XORData(newCmdContinue);
                                                        }
                                                        else
                                                        {
                                                            LogMessage("| 数据总数/数据块数 | 0/0                        |                            |");
                                                            LogMessage("+-------------------+----------------------------+----------------------------+");
                                                            isContinued = false;
                                                        }
                                                    }
                                                    else
                                                        isContinued = false;
                                                    if (isContinued == false && NeedReport == true && _pdfDocument != null)
                                                    {
                                                        _createPdfEvent.Reset();
                                                        Task.Factory.StartNew(() =>
                                                        {
                                                            Create11HReport();
                                                        });
                                                        _createPdfEvent.WaitOne();
                                                    }
                                                }
                                                #endregion
                                                break;
                                            case "12":
                                                #region
                                                {
                                                    string number1 = cdi.StartDateTime.Year.ToString("") + "-" +
                                                        cdi.StartDateTime.Month.ToString("") + "-" +
                                                        cdi.StartDateTime.Day.ToString("") + " " +
                                                        cdi.StartDateTime.Hour.ToString("") + ":" +
                                                        cdi.StartDateTime.Minute.ToString("") + ":" +
                                                        cdi.StartDateTime.Second.ToString("");
                                                    number1 = number1.PadRight(27);
                                                    string number2 = cdi.StopDateTime.Year.ToString("") + "-" +
                                                        cdi.StopDateTime.Month.ToString("") + "-" +
                                                        cdi.StopDateTime.Day.ToString("") + " " +
                                                        cdi.StopDateTime.Hour.ToString("") + ":" +
                                                        cdi.StopDateTime.Minute.ToString("") + ":" +
                                                        cdi.StopDateTime.Second.ToString("");
                                                    number2 = number2.PadRight(27);

                                                    int blockCount = (int)(Math.Floor((double)dataLen / (double)25));
                                                    int dataRemain = dataLen % 25;
                                                    string sValue = (dataLen.ToString() + "/" + blockCount.ToString()).PadRight(27);
                                                    if (isContinued == false)
                                                    {
                                                        LogMessage("+-----------------------------------------------------------------------------+");
                                                        LogMessage("| (此处只显示捕获的数据信息,详细内容请见报表.)                                |");
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                        LogMessage("|      采集起始时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number1));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                        LogMessage("|      采集停止时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number2));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    DateTime lastDateTime = DateTime.Now;
                                                    if (blockCount > 0)
                                                    {
                                                        string numberblock = "";
                                                        for (int iblock = 0; iblock < blockCount; iblock++)
                                                        {
                                                            numberblock = "20" + baData[25 * iblock + 0].ToString("X") + "-" +
                                                                 baData[25 * iblock + 1].ToString("X") + "-" +
                                                                 baData[25 * iblock + 2].ToString("X") + " " +
                                                                 baData[25 * iblock + 3].ToString("X") + ":" +
                                                                 baData[25 * iblock + 4].ToString("X") + ":" +
                                                                 baData[25 * iblock + 5].ToString("X");
                                                            numberblock = numberblock.PadRight(27);
                                                            lastDateTime = new DateTime(
                                                                ((int)Math.Floor((double)baData[25 * iblock + 0] / 16.0)) * 10 + baData[25 * iblock + 0] % 16 + 2000,
                                                                ((int)Math.Floor((double)baData[25 * iblock + 1] / 16.0)) * 10 + baData[25 * iblock + 1] % 16,
                                                                ((int)Math.Floor((double)baData[25 * iblock + 2] / 16.0)) * 10 + baData[25 * iblock + 2] % 16,
                                                                ((int)Math.Floor((double)baData[25 * iblock + 3] / 16.0)) * 10 + baData[25 * iblock + 3] % 16,
                                                                ((int)Math.Floor((double)baData[25 * iblock + 4] / 16.0)) * 10 + baData[25 * iblock + 4] % 16,
                                                                ((int)Math.Floor((double)baData[25 * iblock + 5] / 16.0)) * 10 + baData[25 * iblock + 5] % 16);
                                                            lastDateTime = lastDateTime.Subtract(new TimeSpan(0, 0, 1));
                                                            byte[] baNumber = new byte[18];
                                                            for (int idxBa = 0; idxBa < 18; idxBa++)
                                                            {
                                                                baNumber[idxBa] = baData[25 * iblock + 6 + idxBa];
                                                            }
                                                            string number = Encoding.UTF8.GetString(baNumber).PadRight(27);
                                                            string oper = "";
                                                            switch (baData[25 * iblock + 24].ToString("X").Trim().ToUpper())
                                                            {
                                                                default:
                                                                    oper = "未知操作";
                                                                    break;
                                                                case "1":
                                                                case "01":
                                                                    oper = "登录";
                                                                    break;
                                                                case "2":
                                                                case "02":
                                                                    oper = "退出";
                                                                    break;
                                                            }
                                                            _cmd12HRespOc.Add(new Cmd12HResponse()
                                                            {
                                                                Index = (_cmd12HRespOc.Count + 1).ToString(),
                                                                RecordDateTime = numberblock.Trim(),
                                                                Number = number,
                                                                Description = oper
                                                            });
                                                        }
                                                        LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$| @@@@@@@@@@@@@@@@@@@@@@@@@@@|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue).Replace("@@@@@@@@@@@@@@@@@@@@@@@@@@@", numberblock));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    else
                                                    {
                                                        LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    if (dataRemain != 0)
                                                    {
                                                        string sValue3 = dataRemain.ToString().PadRight(27);
                                                        LogMessage("|        错误数据数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue3));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    if (blockCount > 0)
                                                    {
                                                        if (lastDateTime.Subtract(cdi.StartDateTime).TotalSeconds >= 0.0)
                                                        {
                                                            isContinued = true;
                                                            string[] saNew = newCmdContinue.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                                            saNew[12] = lastDateTime.Year.ToString();
                                                            saNew[12] = saNew[12].Substring(saNew[12].Length - 2, 2);
                                                            saNew[13] = lastDateTime.Month.ToString();
                                                            saNew[13] = saNew[13].PadLeft(2, '0');
                                                            saNew[14] = lastDateTime.Day.ToString();
                                                            saNew[14] = saNew[14].PadLeft(2, '0');
                                                            saNew[15] = lastDateTime.Hour.ToString();
                                                            saNew[15] = saNew[15].PadLeft(2, '0');
                                                            saNew[16] = lastDateTime.Minute.ToString();
                                                            saNew[16] = saNew[16].PadLeft(2, '0');
                                                            saNew[17] = lastDateTime.Second.ToString();
                                                            saNew[17] = saNew[17].PadLeft(2, '0');
                                                            newCmdContinue = "";
                                                            foreach (string saNewi in saNew)
                                                            {
                                                                newCmdContinue = newCmdContinue + " " + saNewi;
                                                            }
                                                            newCmdContinue = newCmdContinue.Trim();
                                                            newCmdContinue = newCmdContinue.Substring(0, newCmdContinue.Length - 3);
                                                            newCmdContinue = newCmdContinue + " " + CmdDefinition.XORData(newCmdContinue);
                                                        }
                                                        else
                                                        {
                                                            LogMessage("| 数据总数/数据块数 | 0/0                        |                            |");
                                                            LogMessage("+-------------------+----------------------------+----------------------------+");
                                                            isContinued = false;
                                                        }
                                                    }
                                                    else
                                                        isContinued = false;
                                                    if (isContinued == false && NeedReport == true && _pdfDocument != null)
                                                    {
                                                        _createPdfEvent.Reset();
                                                        Task.Factory.StartNew(() =>
                                                        {
                                                            Create12HReport();
                                                        });
                                                        _createPdfEvent.WaitOne();
                                                    }
                                                }
                                                #endregion
                                                break;
                                            case "13":
                                                #region
                                                {
                                                    string number1 = cdi.StartDateTime.Year.ToString("") + "-" +
                                                        cdi.StartDateTime.Month.ToString("") + "-" +
                                                        cdi.StartDateTime.Day.ToString("") + " " +
                                                        cdi.StartDateTime.Hour.ToString("") + ":" +
                                                        cdi.StartDateTime.Minute.ToString("") + ":" +
                                                        cdi.StartDateTime.Second.ToString("");
                                                    number1 = number1.PadRight(27);
                                                    string number2 = cdi.StopDateTime.Year.ToString("") + "-" +
                                                        cdi.StopDateTime.Month.ToString("") + "-" +
                                                        cdi.StopDateTime.Day.ToString("") + " " +
                                                        cdi.StopDateTime.Hour.ToString("") + ":" +
                                                        cdi.StopDateTime.Minute.ToString("") + ":" +
                                                        cdi.StopDateTime.Second.ToString("");
                                                    number2 = number2.PadRight(27);

                                                    int blockCount = (int)(Math.Floor((double)dataLen / (double)7));
                                                    int dataRemain = dataLen % 7;
                                                    string sValue = (dataLen.ToString() + "/" + blockCount.ToString()).PadRight(27);
                                                    if (isContinued == false)
                                                    {
                                                        LogMessage("+-----------------------------------------------------------------------------+");
                                                        LogMessage("| (此处只显示捕获的数据信息,详细内容请见报表.)                                |");
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                        LogMessage("|      采集起始时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number1));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                        LogMessage("|      采集停止时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number2));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    DateTime lastDateTime = DateTime.Now;
                                                    if (blockCount > 0)
                                                    {
                                                        string numberblock = "";
                                                        for (int iblock = 0; iblock < blockCount; iblock++)
                                                        {
                                                            numberblock = "20" + baData[7 * iblock + 0].ToString("X") + "-" +
                                                                baData[7 * iblock + 1].ToString("X") + "-" +
                                                                baData[7 * iblock + 2].ToString("X") + " " +
                                                                baData[7 * iblock + 3].ToString("X") + ":" +
                                                                baData[7 * iblock + 4].ToString("X") + ":" +
                                                                baData[7 * iblock + 5].ToString("X");
                                                            numberblock = numberblock.PadRight(27);
                                                            lastDateTime = new DateTime(
                                                                ((int)Math.Floor((double)baData[7 * iblock + 0] / 16.0)) * 10 + baData[7 * iblock + 0] % 16 + 2000,
                                                                ((int)Math.Floor((double)baData[7 * iblock + 1] / 16.0)) * 10 + baData[7 * iblock + 1] % 16,
                                                                ((int)Math.Floor((double)baData[7 * iblock + 2] / 16.0)) * 10 + baData[7 * iblock + 2] % 16,
                                                                ((int)Math.Floor((double)baData[7 * iblock + 3] / 16.0)) * 10 + baData[7 * iblock + 3] % 16,
                                                                ((int)Math.Floor((double)baData[7 * iblock + 4] / 16.0)) * 10 + baData[7 * iblock + 4] % 16,
                                                                ((int)Math.Floor((double)baData[7 * iblock + 5] / 16.0)) * 10 + baData[7 * iblock + 5] % 16);
                                                            lastDateTime = lastDateTime.Subtract(new TimeSpan(0, 0, 1));
                                                            string oper = "";
                                                            switch (baData[7 * iblock + 6].ToString("X").Trim().ToUpper())
                                                            {
                                                                default:
                                                                    oper = "未知状态";
                                                                    break;
                                                                case "1":
                                                                case "01":
                                                                    oper = "通电";
                                                                    break;
                                                                case "2":
                                                                case "02":
                                                                    oper = "断电";
                                                                    break;
                                                            }
                                                            _cmd13HRespOc.Add(new Cmd13HResponse()
                                                            {
                                                                Index = (_cmd13HRespOc.Count + 1).ToString(),
                                                                RecordDateTime = numberblock.Trim(),
                                                                Description = oper
                                                            });
                                                        }
                                                        LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$| @@@@@@@@@@@@@@@@@@@@@@@@@@@|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue).Replace("@@@@@@@@@@@@@@@@@@@@@@@@@@@", numberblock));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    else
                                                    {
                                                        LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    if (dataRemain != 0)
                                                    {
                                                        string sValue3 = dataRemain.ToString().PadRight(27);
                                                        LogMessage("|        错误数据数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue3));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    if (blockCount > 0)
                                                    {
                                                        if (lastDateTime.Subtract(cdi.StartDateTime).TotalSeconds >= 0.0)
                                                        {
                                                            isContinued = true;
                                                            string[] saNew = newCmdContinue.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                                            saNew[12] = lastDateTime.Year.ToString();
                                                            saNew[12] = saNew[12].Substring(saNew[12].Length - 2, 2);
                                                            saNew[13] = lastDateTime.Month.ToString();
                                                            saNew[13] = saNew[13].PadLeft(2, '0');
                                                            saNew[14] = lastDateTime.Day.ToString();
                                                            saNew[14] = saNew[14].PadLeft(2, '0');
                                                            saNew[15] = lastDateTime.Hour.ToString();
                                                            saNew[15] = saNew[15].PadLeft(2, '0');
                                                            saNew[16] = lastDateTime.Minute.ToString();
                                                            saNew[16] = saNew[16].PadLeft(2, '0');
                                                            saNew[17] = lastDateTime.Second.ToString();
                                                            saNew[17] = saNew[17].PadLeft(2, '0');
                                                            newCmdContinue = "";
                                                            foreach (string saNewi in saNew)
                                                            {
                                                                newCmdContinue = newCmdContinue + " " + saNewi;
                                                            }
                                                            newCmdContinue = newCmdContinue.Trim();
                                                            newCmdContinue = newCmdContinue.Substring(0, newCmdContinue.Length - 3);
                                                            newCmdContinue = newCmdContinue + " " + CmdDefinition.XORData(newCmdContinue);
                                                        }
                                                        else
                                                        {
                                                            LogMessage("| 数据总数/数据块数 | 0/0                        |                            |");
                                                            LogMessage("+-------------------+----------------------------+----------------------------+");
                                                            isContinued = false;
                                                        }
                                                    }
                                                    else
                                                        isContinued = false;
                                                    if (isContinued == false && NeedReport == true && _pdfDocument != null)
                                                    {
                                                        _createPdfEvent.Reset();
                                                        Task.Factory.StartNew(() =>
                                                        {
                                                            Create13HReport();
                                                        });
                                                        _createPdfEvent.WaitOne();
                                                    }
                                                }
                                                #endregion
                                                break;
                                            case "14":
                                                #region
                                                {
                                                    string number1 = cdi.StartDateTime.Year.ToString("") + "-" +
                                                           cdi.StartDateTime.Month.ToString("") + "-" +
                                                           cdi.StartDateTime.Day.ToString("") + " " +
                                                           cdi.StartDateTime.Hour.ToString("") + ":" +
                                                           cdi.StartDateTime.Minute.ToString("") + ":" +
                                                           cdi.StartDateTime.Second.ToString("");
                                                    number1 = number1.PadRight(27);
                                                    string number2 = cdi.StopDateTime.Year.ToString("") + "-" +
                                                        cdi.StopDateTime.Month.ToString("") + "-" +
                                                        cdi.StopDateTime.Day.ToString("") + " " +
                                                        cdi.StopDateTime.Hour.ToString("") + ":" +
                                                        cdi.StopDateTime.Minute.ToString("") + ":" +
                                                        cdi.StopDateTime.Second.ToString("");
                                                    number2 = number2.PadRight(27);

                                                    int blockCount = (int)(Math.Floor((double)dataLen / (double)7));
                                                    int dataRemain = dataLen % 7;
                                                    string sValue = (dataLen.ToString() + "/" + blockCount.ToString()).PadRight(27);
                                                    if (isContinued == false)
                                                    {
                                                        LogMessage("+-----------------------------------------------------------------------------+");
                                                        LogMessage("| (此处只显示捕获的数据信息,详细内容请见报表.)                                |");
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                        LogMessage("|      采集起始时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number1));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                        LogMessage("|      采集停止时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number2));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    DateTime lastDateTime = DateTime.Now;
                                                    if (blockCount > 0)
                                                    {
                                                        string numberblock = "";
                                                        for (int iblock = 0; iblock < blockCount; iblock++)
                                                        {
                                                            numberblock = "20" + baData[7 * iblock + 0].ToString("X") + "-" +
                                                                baData[7 * iblock + 1].ToString("X") + "-" +
                                                                baData[7 * iblock + 2].ToString("X") + " " +
                                                                baData[7 * iblock + 3].ToString("X") + ":" +
                                                                baData[7 * iblock + 4].ToString("X") + ":" +
                                                                baData[7 * iblock + 5].ToString("X");
                                                            numberblock = numberblock.PadRight(27);
                                                            lastDateTime = new DateTime(
                                                                ((int)Math.Floor((double)baData[7 * iblock + 0] / 16.0)) * 10 + baData[7 * iblock + 0] % 16 + 2000,
                                                                ((int)Math.Floor((double)baData[7 * iblock + 1] / 16.0)) * 10 + baData[7 * iblock + 1] % 16,
                                                                ((int)Math.Floor((double)baData[7 * iblock + 2] / 16.0)) * 10 + baData[7 * iblock + 2] % 16,
                                                                ((int)Math.Floor((double)baData[7 * iblock + 3] / 16.0)) * 10 + baData[7 * iblock + 3] % 16,
                                                                ((int)Math.Floor((double)baData[7 * iblock + 4] / 16.0)) * 10 + baData[7 * iblock + 4] % 16,
                                                                ((int)Math.Floor((double)baData[7 * iblock + 5] / 16.0)) * 10 + baData[7 * iblock + 5] % 16);
                                                            lastDateTime = lastDateTime.Subtract(new TimeSpan(0, 0, 1));
                                                            string oper = "";
                                                            switch (baData[7 * iblock + 6].ToString("X").Trim().ToUpper())
                                                            {
                                                                default:
                                                                    oper = "未知操作";
                                                                    break;
                                                                case "82":
                                                                    oper = "修改车辆信息";
                                                                    break;
                                                                case "83":
                                                                    oper = "修改初次安装日期";
                                                                    break;
                                                                case "84":
                                                                    oper = "修改状态量配置信息";
                                                                    break;
                                                                case "C2":
                                                                    oper = "修改记录仪时间";
                                                                    break;
                                                                case "C3":
                                                                    oper = "修改脉冲系数";
                                                                    break;
                                                                case "C4":
                                                                    oper = "修改初始里程";
                                                                    break;
                                                            }
                                                            _cmd14HRespOc.Add(new Cmd14HResponse()
                                                            {
                                                                Index = (_cmd14HRespOc.Count + 1).ToString(),
                                                                RecordDateTime = numberblock.Trim(),
                                                                Description = oper
                                                            });
                                                        }
                                                        LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$| @@@@@@@@@@@@@@@@@@@@@@@@@@@|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue).Replace("@@@@@@@@@@@@@@@@@@@@@@@@@@@", numberblock));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    else
                                                    {
                                                        LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    if (dataRemain != 0)
                                                    {
                                                        string sValue3 = dataRemain.ToString().PadRight(27);
                                                        LogMessage("|        错误数据数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue3));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    if (blockCount > 0)
                                                    {
                                                        if (lastDateTime.Subtract(cdi.StartDateTime).TotalSeconds >= 0.0)
                                                        {
                                                            isContinued = true;
                                                            string[] saNew = newCmdContinue.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                                            saNew[12] = lastDateTime.Year.ToString();
                                                            saNew[12] = saNew[12].Substring(saNew[12].Length - 2, 2);
                                                            saNew[13] = lastDateTime.Month.ToString();
                                                            saNew[13] = saNew[13].PadLeft(2, '0');
                                                            saNew[14] = lastDateTime.Day.ToString();
                                                            saNew[14] = saNew[14].PadLeft(2, '0');
                                                            saNew[15] = lastDateTime.Hour.ToString();
                                                            saNew[15] = saNew[15].PadLeft(2, '0');
                                                            saNew[16] = lastDateTime.Minute.ToString();
                                                            saNew[16] = saNew[16].PadLeft(2, '0');
                                                            saNew[17] = lastDateTime.Second.ToString();
                                                            saNew[17] = saNew[17].PadLeft(2, '0');
                                                            newCmdContinue = "";
                                                            foreach (string saNewi in saNew)
                                                            {
                                                                newCmdContinue = newCmdContinue + " " + saNewi;
                                                            }
                                                            newCmdContinue = newCmdContinue.Trim();
                                                            newCmdContinue = newCmdContinue.Substring(0, newCmdContinue.Length - 3);
                                                            newCmdContinue = newCmdContinue + " " + CmdDefinition.XORData(newCmdContinue);
                                                        }
                                                        else
                                                        {
                                                            LogMessage("| 数据总数/数据块数 | 0/0                        |                            |");
                                                            LogMessage("+-------------------+----------------------------+----------------------------+");
                                                            isContinued = false;
                                                        }
                                                    }
                                                    else
                                                        isContinued = false;
                                                    if (isContinued == false && NeedReport == true && _pdfDocument != null)
                                                    {
                                                        _createPdfEvent.Reset();
                                                        Task.Factory.StartNew(() =>
                                                        {
                                                            Create14HReport();
                                                        });
                                                        _createPdfEvent.WaitOne();
                                                    }
                                                }
                                                #endregion
                                                break;
                                            case "15":
                                                #region
                                                {
                                                    string number1 = cdi.StartDateTime.Year.ToString("") + "-" +
                                                        cdi.StartDateTime.Month.ToString("") + "-" +
                                                        cdi.StartDateTime.Day.ToString("") + " " +
                                                        cdi.StartDateTime.Hour.ToString("") + ":" +
                                                        cdi.StartDateTime.Minute.ToString("") + ":" +
                                                        cdi.StartDateTime.Second.ToString("");
                                                    number1 = number1.PadRight(27);
                                                    string number2 = cdi.StopDateTime.Year.ToString("") + "-" +
                                                        cdi.StopDateTime.Month.ToString("") + "-" +
                                                        cdi.StopDateTime.Day.ToString("") + " " +
                                                        cdi.StopDateTime.Hour.ToString("") + ":" +
                                                        cdi.StopDateTime.Minute.ToString("") + ":" +
                                                        cdi.StopDateTime.Second.ToString("");
                                                    number2 = number2.PadRight(27);

                                                    int blockCount = (int)(Math.Floor((double)dataLen / (double)133));
                                                    int dataRemain = dataLen % 133;
                                                    string sValue = (dataLen.ToString() + "/" + blockCount.ToString()).PadRight(27);
                                                    if (isContinued == false)
                                                    {
                                                        LogMessage("+-----------------------------------------------------------------------------+");
                                                        LogMessage("| (此处只显示捕获的数据信息,详细内容请见报表.)                                |");
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                        LogMessage("|      采集起始时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number1));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                        LogMessage("|      采集停止时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number2));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    DateTime lastDateTime = DateTime.Now;
                                                    if (blockCount > 0)
                                                    {
                                                        string numberblock = "";
                                                        for (int iblock = 0; iblock < blockCount; iblock++)
                                                        {
                                                            byte state = baData[133 * iblock + 0];

                                                            numberblock = "20" + baData[133 * iblock + 1].ToString("X") + "-" +
                                                                baData[133 * iblock + 2].ToString("X") + "-" +
                                                                baData[133 * iblock + 3].ToString("X") + " " +
                                                                baData[133 * iblock + 4].ToString("X") + ":" +
                                                                baData[133 * iblock + 5].ToString("X") + ":" +
                                                                baData[133 * iblock + 6].ToString("X");
                                                            numberblock = numberblock.PadRight(27);
                                                            lastDateTime = new DateTime(
                                                                ((int)Math.Floor((double)baData[133 * iblock + 1] / 16.0)) * 10 + baData[133 * iblock + 1] % 16 + 2000,
                                                                ((int)Math.Floor((double)baData[133 * iblock + 2] / 16.0)) * 10 + baData[133 * iblock + 2] % 16,
                                                                ((int)Math.Floor((double)baData[133 * iblock + 3] / 16.0)) * 10 + baData[133 * iblock + 3] % 16,
                                                                ((int)Math.Floor((double)baData[133 * iblock + 4] / 16.0)) * 10 + baData[133 * iblock + 4] % 16,
                                                                ((int)Math.Floor((double)baData[133 * iblock + 5] / 16.0)) * 10 + baData[133 * iblock + 5] % 16,
                                                                ((int)Math.Floor((double)baData[133 * iblock + 6] / 16.0)) * 10 + baData[133 * iblock + 6] % 16);
                                                            lastDateTime = lastDateTime.Subtract(new TimeSpan(0, 0, 1));//blockCount, 0));
                                                            string numberblock2 = "20" + baData[133 * iblock + 7].ToString("X") + "-" +
                                                                baData[133 * iblock + 8].ToString("X") + "-" +
                                                                baData[133 * iblock + 9].ToString("X") + " " +
                                                                baData[133 * iblock + 10].ToString("X") + ":" +
                                                                baData[133 * iblock + 11].ToString("X") + ":" +
                                                                baData[133 * iblock + 12].ToString("X");
                                                            numberblock2 = numberblock.PadRight(27);
                                                            ObservableCollection<Tuple<int, int>> records = new ObservableCollection<Tuple<int, int>>();
                                                            for (int iSec = 0; iSec < 60; iSec++)
                                                            {
                                                                int speed = baData[133 * iblock + iSec * 2 + 13 + 0];
                                                                if (speed == 0xFF)
                                                                    speed = 0;
                                                                int refSpeed = baData[133 * iblock + iSec * 2 + 13 + 1];
                                                                if (refSpeed == 0xFF)
                                                                    refSpeed = 0;

                                                                records.Add(new Tuple<int, int>(speed, refSpeed));
                                                            }
                                                            _cmd15HRespOc.Add(new Cmd15HResponse()
                                                            {
                                                                Index = (_cmd15HRespOc.Count + 1).ToString(),
                                                                State = state,
                                                                StartDateTime = numberblock,
                                                                StopDateTime = numberblock2,
                                                                Records = records
                                                            });
                                                        }

                                                        LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$| @@@@@@@@@@@@@@@@@@@@@@@@@@@|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue).Replace("@@@@@@@@@@@@@@@@@@@@@@@@@@@", numberblock));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    else
                                                    {
                                                        LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    if (dataRemain != 0)
                                                    {
                                                        string sValue3 = dataRemain.ToString().PadRight(27);
                                                        LogMessage("|        错误数据数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue3));
                                                        LogMessage("+-------------------+----------------------------+----------------------------+");
                                                    }
                                                    if (blockCount > 0)
                                                    {
                                                        if (lastDateTime.Subtract(cdi.StartDateTime).TotalSeconds >= 0.0)
                                                        {
                                                            isContinued = true;
                                                            string[] saNew = newCmdContinue.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                                            saNew[12] = lastDateTime.Year.ToString();
                                                            saNew[12] = saNew[12].Substring(saNew[12].Length - 2, 2);
                                                            saNew[13] = lastDateTime.Month.ToString();
                                                            saNew[13] = saNew[13].PadLeft(2, '0');
                                                            saNew[14] = lastDateTime.Day.ToString();
                                                            saNew[14] = saNew[14].PadLeft(2, '0');
                                                            saNew[15] = lastDateTime.Hour.ToString();
                                                            saNew[15] = saNew[15].PadLeft(2, '0');
                                                            saNew[16] = lastDateTime.Minute.ToString();
                                                            saNew[16] = saNew[16].PadLeft(2, '0');
                                                            saNew[17] = lastDateTime.Second.ToString();
                                                            saNew[17] = saNew[17].PadLeft(2, '0');
                                                            newCmdContinue = "";
                                                            foreach (string saNewi in saNew)
                                                            {
                                                                newCmdContinue = newCmdContinue + " " + saNewi;
                                                            }
                                                            newCmdContinue = newCmdContinue.Trim();
                                                            newCmdContinue = newCmdContinue.Substring(0, newCmdContinue.Length - 3);
                                                            newCmdContinue = newCmdContinue + " " + CmdDefinition.XORData(newCmdContinue);
                                                        }
                                                        else
                                                        {
                                                            LogMessage("| 数据总数/数据块数 | 0/0                        |                            |");
                                                            LogMessage("+-------------------+----------------------------+----------------------------+");
                                                            isContinued = false;
                                                        }
                                                    }
                                                    else
                                                        isContinued = false;
                                                    if (isContinued == false && NeedReport == true && _pdfDocument != null)
                                                    {
                                                        _createPdfEvent.Reset();
                                                        Task.Factory.StartNew(() =>
                                                        {
                                                            Create15HReport();
                                                        });
                                                        _createPdfEvent.WaitOne();
                                                    }
                                                }
                                                #endregion
                                                break;
                                            case "82":
                                                LogMessageInformation("成功执行.");
                                                break;
                                            case "83":
                                                LogMessageInformation("成功执行.");
                                                break;
                                            case "84":
                                                LogMessageInformation("成功执行.");
                                                break;
                                            case "C2":
                                                LogMessageInformation("成功执行.");
                                                break;
                                            case "C3":
                                                LogMessageInformation("成功执行.");
                                                break;
                                            case "C4":
                                                LogMessageInformation("成功执行.");
                                                break;
                                            case "20":
                                                #region
                                                {
                                                    if (dataLen != 1)
                                                        LogMessageError("命令(" + cdi.CmdContent + ")的响应的数据块长度错误.");
                                                    else
                                                    {
                                                        string number = baData[0].ToString().PadRight(27);
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|  传感器单圈脉冲数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                                        LogMessage("+-------------------+----------------------------+");

                                                        if (NeedReport == true)
                                                        {
                                                            if (_pdfDocument == null)
                                                            {
                                                                LogMessageError("无法创建报表.");
                                                            }
                                                            else
                                                            {
                                                                #region

                                                                try
                                                                {
                                                                    string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                                    BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                                    PdfParagraph par = new PdfParagraph("--- 传感器单圈脉冲数 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                                    par.Alignment = Element.ALIGN_CENTER;
                                                                    _pdfDocument.Add(par);

                                                                    par.SpacingBefore = 25f;

                                                                    PdfPTable table = new PdfPTable(2);

                                                                    table.SpacingBefore = 25f;

                                                                    table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                                    float[] widths = { 50f, 150f };
                                                                    table.SetWidths(widths);
                                                                    table.LockedWidth = true;

                                                                    PdfPCell cell;
                                                                    cell = new PdfPCell(new Phrase("传感器单圈脉冲数", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(number.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);

                                                                    _pdfDocument.Add(table);
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    LogMessageError("创建报表出错:" + ex.Message);
                                                                    _pdfDocument = null;
                                                                }

                                                                #endregion
                                                            }
                                                        }
                                                    }
                                                }
                                                #endregion
                                                break;
                                            case "21":
                                                #region
                                                {
                                                    if (dataLen != 8 && dataLen != 14)
                                                        LogMessageError("命令(" + cdi.CmdContent + ")的响应的数据块长度错误.");
                                                    else
                                                    {
                                                        int intPad = 0;
                                                        if (dataLen == 14)
                                                            intPad = 6;
                                                        string number = "";
                                                        if (dataLen == 14)
                                                        {
                                                            number = "20" + baData[0].ToString("X") + "-" + baData[1].ToString("X") + "-" + baData[2].ToString("X") + " " +
                                                               baData[3].ToString("X") + ":" + baData[4].ToString("X") + ":" + baData[5].ToString("X");
                                                            number = number.PadRight(27);
                                                        }
                                                        string d2Disp = "D2(" + D2 + ")";
                                                        d2Disp.PadLeft(18 - D2.Length);
                                                        string d1Disp = "D2(" + D1 + ")";
                                                        d1Disp.PadLeft(18 - D1.Length);
                                                        string d0Disp = "D2(" + D0 + ")";
                                                        d0Disp.PadLeft(18 - D0.Length);
                                                        string d0 = "";
                                                        switch ((int)baData[0 + intPad])
                                                        {
                                                            default:
                                                                d0 = "未知状态";
                                                                break;
                                                            case 0:
                                                                d0 = "低有效";
                                                                break;
                                                            case 1:
                                                                d0 = "高有效";
                                                                break;
                                                            case 3:
                                                                d0 = "未启用";
                                                                break;
                                                        }
                                                        d0 = d0.PadLeft(27 - d0.Length);
                                                        string d1 = "";
                                                        switch ((int)baData[1 + intPad])
                                                        {
                                                            default:
                                                                d1 = "未知状态";
                                                                break;
                                                            case 0:
                                                                d1 = "低有效";
                                                                break;
                                                            case 1:
                                                                d1 = "高有效";
                                                                break;
                                                            case 3:
                                                                d1 = "未启用";
                                                                break;
                                                        }
                                                        d1 = d1.PadLeft(27 - d1.Length);
                                                        string d2 = "";
                                                        switch ((int)baData[2 + intPad])
                                                        {
                                                            default:
                                                                d2 = "未知状态";
                                                                break;
                                                            case 0:
                                                                d2 = "低有效";
                                                                break;
                                                            case 1:
                                                                d2 = "高有效";
                                                                break;
                                                            case 3:
                                                                d2 = "未启用";
                                                                break;
                                                        }
                                                        d2 = d2.PadLeft(27 - d2.Length);
                                                        string d3 = "";
                                                        switch ((int)baData[3 + intPad])
                                                        {
                                                            default:
                                                                d3 = "未知状态";
                                                                break;
                                                            case 0:
                                                                d3 = "低有效";
                                                                break;
                                                            case 1:
                                                                d3 = "高有效";
                                                                break;
                                                            case 3:
                                                                d3 = "未启用";
                                                                break;
                                                        }
                                                        d3 = d3.PadLeft(27 - d3.Length);
                                                        string d4 = "";
                                                        switch ((int)baData[4 + intPad])
                                                        {
                                                            default:
                                                                d4 = "未知状态";
                                                                break;
                                                            case 0:
                                                                d4 = "低有效";
                                                                break;
                                                            case 1:
                                                                d4 = "高有效";
                                                                break;
                                                            case 3:
                                                                d4 = "未启用";
                                                                break;
                                                        }
                                                        d4 = d4.PadLeft(27 - d4.Length);
                                                        string d5 = "";
                                                        switch ((int)baData[5 + intPad])
                                                        {
                                                            default:
                                                                d5 = "未知状态";
                                                                break;
                                                            case 0:
                                                                d5 = "低有效";
                                                                break;
                                                            case 1:
                                                                d5 = "高有效";
                                                                break;
                                                            case 3:
                                                                d5 = "未启用";
                                                                break;
                                                        }
                                                        d5 = d5.PadLeft(27 - d5.Length);
                                                        string d6 = "";
                                                        switch ((int)baData[6 + intPad])
                                                        {
                                                            default:
                                                                d6 = "未知状态";
                                                                break;
                                                            case 0:
                                                                d6 = "低有效";
                                                                break;
                                                            case 1:
                                                                d6 = "高有效";
                                                                break;
                                                            case 3:
                                                                d6 = "未启用";
                                                                break;
                                                        }
                                                        d6 = d6.PadLeft(27 - d6.Length);
                                                        string d7 = "";
                                                        switch ((int)baData[7 + intPad])
                                                        {
                                                            default:
                                                                d7 = "未知状态";
                                                                break;
                                                            case 0:
                                                                d7 = "低有效";
                                                                break;
                                                            case 1:
                                                                d7 = "高有效";
                                                                break;
                                                            case 3:
                                                                d7 = "未启用";
                                                                break;
                                                        }
                                                        d7 = d7.PadLeft(27 - d7.Length);
                                                        if (dataLen == 14)
                                                        {
                                                            LogMessage("+-------------------+----------------------------+");
                                                            LogMessage("|          采集时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                                        }
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|          D7(制动) | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d7));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|        D6(左转向) | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d6));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|        D5(右转向) | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d5));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|          D4(远光) | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d4));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|          D3(近光) | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d3));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|@@@@@@@@@@@@@@@@@@ | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("@@@@@@@@@@@@@@@@@@", d2Disp).Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d2));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|@@@@@@@@@@@@@@@@@@ | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("@@@@@@@@@@@@@@@@@@", d1Disp).Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d1));
                                                        LogMessage("+-------------------+----------------------------+");
                                                        LogMessage("|@@@@@@@@@@@@@@@@@@ | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("@@@@@@@@@@@@@@@@@@", d0Disp).Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d0));
                                                        LogMessage("+-------------------+----------------------------+");

                                                        if (NeedReport == true)
                                                        {
                                                            if (_pdfDocument == null)
                                                            {
                                                                LogMessageError("无法创建报表.");
                                                            }
                                                            else
                                                            {
                                                                #region

                                                                try
                                                                {
                                                                    string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                                    BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                                    PdfParagraph par = new PdfParagraph("--- 信号量状态配置 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                                    par.Alignment = Element.ALIGN_CENTER;
                                                                    _pdfDocument.Add(par);

                                                                    par.SpacingBefore = 25f;

                                                                    PdfPTable table = new PdfPTable(2);

                                                                    table.SpacingBefore = 25f;

                                                                    table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                                    float[] widths = { 50f, 150f };
                                                                    table.SetWidths(widths);
                                                                    table.LockedWidth = true;

                                                                    PdfPCell cell;
                                                                    if (dataLen == 14)
                                                                    {
                                                                        cell = new PdfPCell(new Phrase("采集时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                        table.AddCell(cell);
                                                                        cell = new PdfPCell(new Phrase(number.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                        table.AddCell(cell);
                                                                    }
                                                                    cell = new PdfPCell(new Phrase("D7", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d7.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("D6", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d6.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("D5", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d4.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("D4", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d4.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase("D3", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d3.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d2Disp.Trim(), new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d2.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d1Disp.Trim(), new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d1.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d0Disp.Trim(), new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);
                                                                    cell = new PdfPCell(new Phrase(d0.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                                    table.AddCell(cell);

                                                                    _pdfDocument.Add(table);
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    LogMessageError("创建报表出错:" + ex.Message);
                                                                    _pdfDocument = null;
                                                                }

                                                                #endregion
                                                            }
                                                        }
                                                    }
                                                }
                                                #endregion
                                                break;
                                            case "D0":
                                                LogMessageInformation("成功执行.");
                                                break;
                                            case "D1":
                                                LogMessageInformation("成功执行.");
                                                break;
                                        }

                                        #endregion

                                        cdi.CmdState = "成功执行.";
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        LogMessageError("命令(" + cdi.CmdContent + ")的响应的格式错误.");

                        cdi.CmdState = "执行错误.";

                        isContinued = false;
                    }

                    #endregion

                    if (_cmdsList.Count > 1 && cdi != _cmdsList[_cmdsList.Count - 1])
                    {
                        Thread.Sleep(int.Parse(CmdInterval));
                        if(isContinued == false)
                            LogMessage("");
                    }
                }
                catch (Exception ex)
                {
                    LogMessageError("串口读写错误或响应处理错误.\n" + ex.Message);

                    LogMessage("");

                    cdi.CmdState = "执行异常.";

                    isContinued = false;
                }

                _timerPBar.Change(Timeout.Infinite, 100);
                PBarValue = 0;

                ReadyString2 = "";

                #endregion

                if (isContinued == true)
                    iCmd = iCmd - 1;
            }

            #endregion

            if(_cmdsList.Count >0)
                LogMessageSeperator();

            InRun = false;

            PBarValue = 0;

            CloseReport();
        }

        private void OpenUSBVDR_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = false;
            dlg.Filter = "VDR file (*.VDR)|*.VDR";
            dlg.InitialDirectory = System.Environment.CurrentDirectory;
            dlg.Title = "Select a VDR file";
            bool? b = dlg.ShowDialog();
            if (b != true)
                return;

            InRun = true;

            Task.Factory.StartNew(() =>
                {
                    ParseVDRData(dlg.FileName);
                });
        }

        private void ParseVDRData(string fileName)
        {
            try
            {
                FileInfo fi = new FileInfo(fileName);
                int len = (int)fi.Length;
                if (len < 3)
                {
                    LogMessageError("VDR文件长度为0.");
                    return;
                }
                BinaryReader br = new BinaryReader(File.OpenRead(fileName));
                byte[] ba = null;
                ba = br.ReadBytes(len);

                int intXor = ba[0];
                for (int i = 1; i < len - 1; i++)
                    intXor = intXor ^ ba[i];
                if (intXor != ba[len - 1])
                {
                    LogMessageError("VDR文件校验结果不正确.");
                    return;
                }
                DateTime dt = DateTime.Now;
                CurrentDirectory = System.Environment.CurrentDirectory;
                if (Directory.Exists(CurrentDirectory + @"\Reports") == false)
                {
                    try
                    {
                        Directory.CreateDirectory(CurrentDirectory + @"\Reports");
                        CurrentDirectory = CurrentDirectory + @"\Reports";
                    }
                    catch (Exception)
                    {
                        CurrentDirectory = System.Environment.CurrentDirectory;
                    }
                }
                _docTitleDateTime = string.Format("{0}_{1}_{2} {3}_{4}_{5}", dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);

                if(NeedReport == true)
                    CreateReport();

                int position = 0;
                byte[] baCount = new byte[2];
                baCount[0] = ba[0];
                baCount[1] = ba[1];
                position = 2;
                int iCount = baCount[0] * 256 + baCount[1];// System.BitConverter.ToInt16(baCount, 0);
                for (int i = 0; i < iCount; i++)
                {
                    byte bType = ba[position];
                    byte[] baTitle = new byte[18];
                    for (int j = 0; j < 18; j++)
                    {
                        baTitle[j] = ba[position + 1 + j];
                    }
                    byte[] baBlockLen = new byte[4];
                    baBlockLen[0] = ba[position + 1 + 18 + 0];
                    baBlockLen[1] = ba[position + 1 + 18 + 1];
                    baBlockLen[2] = ba[position + 1 + 18 + 2];
                    baBlockLen[3] = ba[position + 1 + 18 + 3];
                    int dataLen = baBlockLen[0] * 256 * 256 * 256 +
                        baBlockLen[1] * 256 * 256 +
                        baBlockLen[2] * 256 +
                        baBlockLen[3];
                        //System.BitConverter.ToInt32(baBlockLen, 0);

                    byte[] baData = new byte[dataLen];
                    for (int j = 0; j < dataLen; j++)
                    {
                        baData[j] = ba[position + 1 + 18 + 4 + j];
                    }
                    string header = string.Format("{0:X2}", bType);
                    Encoding gb = Encoding.GetEncoding("GB2312");
                    string title = gb.GetString(baTitle);
                    if (title == null)
                        title = "";
                    title = title.Trim().Trim(new char[] { '\0'});
                    string cdiCmdContent = header + ":" + title;

                    LogMessageTitle(cdiCmdContent);

                    #region Display Result

                    switch (header)
                    {
                        default:
                            LogMessageError("命令类型(" + cdiCmdContent + ")未知.");
                            break;
                        case "00":
                            #region
                            {
                                if (dataLen != 2)
                                    LogMessageError("命令(" + header + ")的响应的数据块长度错误.");
                                else
                                {
                                    string year = baData[0].ToString("X").PadRight(27);
                                    string number = baData[1].ToString().PadRight(27);
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|              年号 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", year));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|          修改单号 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                    LogMessage("+-------------------+----------------------------+");

                                    if (NeedReport == true)
                                    {
                                        if (_pdfDocument == null)
                                        {
                                            LogMessageError("无法创建报表.");
                                        }
                                        else
                                        {
                                            #region

                                            try
                                            {
                                                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                PdfParagraph par = new PdfParagraph("--- 记录仪执行版本标准号 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                par.Alignment = Element.ALIGN_CENTER;
                                                _pdfDocument.Add(par);

                                                par.SpacingBefore = 25f;

                                                PdfPTable table = new PdfPTable(2);

                                                table.SpacingBefore = 25f;

                                                table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                float[] widths = { 50f, 150f };
                                                table.SetWidths(widths);
                                                table.LockedWidth = true;

                                                PdfPCell cell;
                                                cell = new PdfPCell(new Phrase("年号", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(year.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("修改单号", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(number.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);

                                                table.SpacingAfter = 60f;

                                                _pdfDocument.Add(table);
                                            }
                                            catch (Exception ex)
                                            {
                                                LogMessageError("创建报表出错:" + ex.Message);
                                                _pdfDocument = null;
                                            }

                                            #endregion
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "01":
                            #region
                            {
                                if (dataLen != 18)
                                    LogMessageError("命令(" + cdiCmdContent + ")的响应的数据块长度错误.");
                                else
                                {
                                    string number = Encoding.UTF8.GetString(baData).PadRight(27);
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|            驾证号 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                    LogMessage("+-------------------+----------------------------+");

                                    if (NeedReport == true)
                                    {
                                        if (_pdfDocument == null)
                                        {
                                            LogMessageError("无法创建报表.");
                                        }
                                        else
                                        {
                                            #region

                                            try
                                            {
                                                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                PdfParagraph par = new PdfParagraph("--- 当前驾驶人信息 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                par.Alignment = Element.ALIGN_CENTER;
                                                _pdfDocument.Add(par);

                                                par.SpacingBefore = 25f;

                                                PdfPTable table = new PdfPTable(2);

                                                table.SpacingBefore = 25f;

                                                table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                float[] widths = { 50f, 150f };
                                                table.SetWidths(widths);
                                                table.LockedWidth = true;

                                                PdfPCell cell;
                                                cell = new PdfPCell(new Phrase("驾证号", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(number.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);

                                                table.SpacingAfter = 60f;

                                                _pdfDocument.Add(table);
                                            }
                                            catch (Exception ex)
                                            {
                                                LogMessageError("创建报表出错:" + ex.Message);
                                                _pdfDocument = null;
                                            }

                                            #endregion
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "02":
                            #region
                            {
                                if (dataLen != 6)
                                    LogMessageError("命令(" + cdiCmdContent + ")的响应的数据块长度错误.");
                                else
                                {
                                    string number = "20" + baData[0].ToString("X") + "-" + baData[1].ToString("X") + "-" + baData[2].ToString("X") + " " +
                                        baData[3].ToString("X") + ":" + baData[4].ToString("X") + ":" + baData[5].ToString("X");
                                    number = number.PadRight(27);
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|          采集时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                    LogMessage("+-------------------+----------------------------+");

                                    if (NeedReport == true)
                                    {
                                        if (_pdfDocument == null)
                                        {
                                            LogMessageError("无法创建报表.");
                                        }
                                        else
                                        {
                                            #region

                                            try
                                            {
                                                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                PdfParagraph par = new PdfParagraph("--- 记录仪实时时间 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                par.Alignment = Element.ALIGN_CENTER;
                                                _pdfDocument.Add(par);

                                                par.SpacingBefore = 25f;

                                                PdfPTable table = new PdfPTable(2);

                                                table.SpacingBefore = 25f;

                                                table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                float[] widths = { 50f, 150f };
                                                table.SetWidths(widths);
                                                table.LockedWidth = true;

                                                PdfPCell cell;
                                                cell = new PdfPCell(new Phrase("采集时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(number.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);

                                                table.SpacingAfter = 60f;

                                                _pdfDocument.Add(table);
                                            }
                                            catch (Exception ex)
                                            {
                                                LogMessageError("创建报表出错:" + ex.Message);
                                                _pdfDocument = null;
                                            }

                                            #endregion
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "03":
                            #region
                            {
                                if (dataLen != 20)
                                    LogMessageError("命令(" + cdiCmdContent + ")的响应的数据块长度错误.");
                                else
                                {
                                    string number1 = "20" + baData[0].ToString("X") + "-" + baData[1].ToString("X") + "-" + baData[2].ToString("X") + " " +
                                        baData[3].ToString("X") + ":" + baData[4].ToString("X") + ":" + baData[5].ToString("X");
                                    number1 = number1.PadRight(27);
                                    string number2 = "20" + baData[6].ToString("X") + "-" + baData[7].ToString("X") + "-" + baData[8].ToString("X") + " " +
                                        baData[9].ToString("X") + ":" + baData[10].ToString("X") + ":" + baData[11].ToString("X");
                                    number2 = number2.PadRight(27);
                                    string distance1 = baData[12].ToString("X") + baData[13].ToString("X") + baData[14].ToString("X") + baData[15].ToString("X");
                                    while (distance1.StartsWith("0"))
                                        distance1 = distance1.Substring(1);
                                    if (string.IsNullOrWhiteSpace(distance1))
                                        distance1 = "0";
                                    distance1 = (distance1 + "0 (单位:0.1千米)").PadRight(27 - 4);
                                    string distance2 = baData[16].ToString("X") + baData[17].ToString("X") + baData[18].ToString("X") + baData[19].ToString("X");
                                    while (distance2.StartsWith("0"))
                                        distance2 = distance2.Substring(1);
                                    if (string.IsNullOrWhiteSpace(distance2))
                                        distance2 = "0";
                                    distance2 = (distance2 + " (单位:0.1千米)").PadRight(27 - 4); // Why not "0 (单位:0.1千米)"
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|          采集时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number1));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|          安装时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number2));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|          初始里程 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", distance1));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|          累计里程 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", distance2));
                                    LogMessage("+-------------------+----------------------------+");

                                    if (NeedReport == true)
                                    {
                                        if (_pdfDocument == null)
                                        {
                                            LogMessageError("无法创建报表.");
                                        }
                                        else
                                        {
                                            #region

                                            try
                                            {
                                                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                PdfParagraph par = new PdfParagraph("--- 累计行驶里程 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                par.Alignment = Element.ALIGN_CENTER;
                                                _pdfDocument.Add(par);

                                                par.SpacingBefore = 25f;

                                                PdfPTable table = new PdfPTable(2);

                                                table.SpacingBefore = 25f;

                                                table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                float[] widths = { 50f, 150f };
                                                table.SetWidths(widths);
                                                table.LockedWidth = true;

                                                PdfPCell cell;
                                                cell = new PdfPCell(new Phrase("采集时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(number1.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("安装时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(number2.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("初始里程", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(distance1.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("累计里程", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(distance2.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);

                                                table.SpacingAfter = 60f;

                                                _pdfDocument.Add(table);
                                            }
                                            catch (Exception ex)
                                            {
                                                LogMessageError("创建报表出错:" + ex.Message);
                                                _pdfDocument = null;
                                            }

                                            #endregion
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "04":
                            #region
                            {
                                if (dataLen != 8)
                                    LogMessageError("命令(" + cdiCmdContent + ")的响应的数据块长度错误.");
                                else
                                {
                                    string number = "20" + baData[0].ToString("X") + "-" + baData[1].ToString("X") + "-" + baData[2].ToString("X") + " " +
                                        baData[3].ToString("X") + ":" + baData[4].ToString("X") + ":" + baData[5].ToString("X");
                                    number = number.PadRight(27);
                                    int intHigh = (int)baData[6];
                                    int intLow = (int)baData[7];
                                    int intLen = intHigh * 256 + intLow;
                                    string sValue = intLen.ToString().PadRight(27);
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|          采集时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|          脉冲系数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                    LogMessage("+-------------------+----------------------------+");

                                    if (NeedReport == true)
                                    {
                                        if (_pdfDocument == null)
                                        {
                                            LogMessageError("无法创建报表.");
                                        }
                                        else
                                        {
                                            #region

                                            try
                                            {
                                                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                PdfParagraph par = new PdfParagraph("--- 记录仪脉冲系数 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                par.Alignment = Element.ALIGN_CENTER;
                                                _pdfDocument.Add(par);

                                                par.SpacingBefore = 25f;

                                                PdfPTable table = new PdfPTable(2);

                                                table.SpacingBefore = 25f;

                                                table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                float[] widths = { 50f, 150f };
                                                table.SetWidths(widths);
                                                table.LockedWidth = true;

                                                PdfPCell cell;
                                                cell = new PdfPCell(new Phrase("采集时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(number.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("脉冲系数", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(sValue.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);

                                                table.SpacingAfter = 60f;

                                                _pdfDocument.Add(table);
                                            }
                                            catch (Exception ex)
                                            {
                                                LogMessageError("创建报表出错:" + ex.Message);
                                                _pdfDocument = null;
                                            }

                                            #endregion
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "05":
                            #region
                            {
                                if (dataLen != 41)
                                    LogMessageError("命令(" + cdiCmdContent + ")的响应的数据块长度错误.");
                                else
                                {
                                    string id = Encoding.UTF8.GetString(baData, 0, 17).PadRight(27);
                                    //Encoding gb = Encoding.GetEncoding("GB2312");
                                    string number = gb.GetString(baData, 17, 12).Trim('\0');
                                    number = number.PadRight(27 - GetChineseNumber(number) + 1);
                                    string category = gb.GetString(baData, 29, 12).Trim('\0');
                                    category = category.PadRight(27 - GetChineseNumber(category));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|        车辆识别码 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", id));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|          车辆号牌 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|          号牌分类 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", category));
                                    LogMessage("+-------------------+----------------------------+");

                                    if (NeedReport == true)
                                    {
                                        if (_pdfDocument == null)
                                        {
                                            LogMessageError("无法创建报表.");
                                        }
                                        else
                                        {
                                            #region

                                            try
                                            {
                                                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                PdfParagraph par = new PdfParagraph("--- 车辆信息 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                par.Alignment = Element.ALIGN_CENTER;
                                                _pdfDocument.Add(par);

                                                par.SpacingBefore = 25f;

                                                PdfPTable table = new PdfPTable(2);

                                                table.SpacingBefore = 25f;

                                                table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                float[] widths = { 50f, 150f };
                                                table.SetWidths(widths);
                                                table.LockedWidth = true;

                                                PdfPCell cell;
                                                cell = new PdfPCell(new Phrase("车辆识别码", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(id.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("车辆号牌", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(number.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("号牌分类", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(category.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);

                                                table.SpacingAfter = 60f;

                                                _pdfDocument.Add(table);
                                            }
                                            catch (Exception ex)
                                            {
                                                LogMessageError("创建报表出错:" + ex.Message);
                                                _pdfDocument = null;
                                            }

                                            #endregion
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "06":
                            #region
                            {
                                if (dataLen != 87)
                                    LogMessageError("命令(" + cdiCmdContent + ")的响应的数据块长度错误.");
                                else
                                {
                                    string number = "20" + baData[0].ToString("X") + "-" + baData[1].ToString("X") + "-" + baData[2].ToString("X") + " " +
                                        baData[3].ToString("X") + ":" + baData[4].ToString("X") + ":" + baData[5].ToString("X");
                                    number = number.PadRight(27);
                                    //Encoding gb = Encoding.GetEncoding("GB2312");
                                    string d0 = gb.GetString(baData, 7, 10).Trim('\0');
                                    if ((baData[6] & 0x1) == 0x1)
                                        d0 = "(有操作) " + d0;
                                    else
                                        d0 = "(无操作) " + d0;
                                    d0 = d0.PadRight(27 - GetChineseNumber(d0) + 3);
                                    string d1 = gb.GetString(baData, 17, 10).Trim('\0');
                                    if ((baData[6] & 0x2) == 0x2)
                                        d1 = "(有操作) " + d1;
                                    else
                                        d1 = "(无操作) " + d1;
                                    d1 = d1.PadRight(27 - GetChineseNumber(d1) + 3);
                                    string d2 = gb.GetString(baData, 27, 10).Trim('\0');
                                    if ((baData[6] & 0x4) == 0x4)
                                        d2 = "(有操作) " + d2;
                                    else
                                        d2 = "(无操作) " + d2;
                                    d2 = d2.PadRight(27 - GetChineseNumber(d2) + 3);
                                    string d3 = gb.GetString(baData, 37, 10).Trim('\0');
                                    if ((baData[6] & 0x8) == 0x8)
                                        d3 = "(有操作) " + d3;
                                    else
                                        d3 = "(无操作) " + d3;
                                    d3 = d3.PadRight(27 - GetChineseNumber(d3) + 3);
                                    string d4 = gb.GetString(baData, 47, 10).Trim('\0');
                                    if ((baData[6] & 0x10) == 0x10)
                                        d4 = "(有操作) " + d4;
                                    else
                                        d4 = "(无操作) " + d4;
                                    d4 = d4.PadRight(27 - GetChineseNumber(d4) + 3);
                                    string d5 = gb.GetString(baData, 57, 10).Trim('\0');
                                    if ((baData[6] & 0x20) == 0x20)
                                        d5 = "(有操作) " + d5;
                                    else
                                        d5 = "(无操作) " + d5;
                                    d5 = d5.PadRight(27 - GetChineseNumber(d5) + 3);
                                    string d6 = gb.GetString(baData, 67, 10).Trim('\0');
                                    if ((baData[6] & 0x40) == 0x40)
                                        d6 = "(有操作) " + d6;
                                    else
                                        d6 = "(无操作) " + d6;
                                    d6 = d6.PadRight(27 - GetChineseNumber(d6) + 3);
                                    string d7 = gb.GetString(baData, 77, 10).Trim('\0');
                                    if ((baData[6] & 0x80) == 0x80)
                                        d7 = "(有操作) " + d7;
                                    else
                                        d7 = "(无操作) " + d7;
                                    d7 = d7.PadRight(27 - GetChineseNumber(d7) + 3);
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|          采集时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|                D7 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d7));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|                D6 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d6));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|                D5 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d5));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|                D4 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d4));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|                D3 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d3));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|                D2 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d2));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|                D1 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d1));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|                D0 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d0));
                                    LogMessage("+-------------------+----------------------------+");

                                    if (NeedReport == true)
                                    {
                                        if (_pdfDocument == null)
                                        {
                                            LogMessageError("无法创建报表.");
                                        }
                                        else
                                        {
                                            #region

                                            try
                                            {
                                                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                PdfParagraph par = new PdfParagraph("--- 状态信号配置信息 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                par.Alignment = Element.ALIGN_CENTER;
                                                _pdfDocument.Add(par);

                                                par.SpacingBefore = 25f;

                                                PdfPTable table = new PdfPTable(2);

                                                table.SpacingBefore = 25f;

                                                table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                float[] widths = { 50f, 150f };
                                                table.SetWidths(widths);
                                                table.LockedWidth = true;

                                                PdfPCell cell;
                                                cell = new PdfPCell(new Phrase("采集时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(number.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("D7", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d7.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("D6", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d6.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("D5", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d4.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("D4", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d4.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("D3", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d3.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("D2", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d2.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("D1", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d1.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("D0", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d0.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);

                                                table.SpacingAfter = 60f;

                                                _pdfDocument.Add(table);
                                            }
                                            catch (Exception ex)
                                            {
                                                LogMessageError("创建报表出错:" + ex.Message);
                                                _pdfDocument = null;
                                            }

                                            #endregion
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "07":
                            #region
                            {
                                if (dataLen != 35)
                                    LogMessageError("命令(" + cdiCmdContent + ")的响应的数据块长度错误.");
                                else
                                {
                                    string ccc = Encoding.ASCII.GetString(baData, 0, 7).Trim('\0').PadRight(27);
                                    string model = Encoding.ASCII.GetString(baData, 7, 16).Trim('\0').PadRight(27);
                                    string number = "20" + baData[23].ToString("X") + "-" + baData[24].ToString("X") + "-" + baData[25].ToString("X");
                                    number = number.PadRight(27);
                                    long flow = baData[26] * 256 * 256 * 256 + baData[27] * 256 * 256 + baData[28] * 256 + baData[29];
                                    string productflow = flow.ToString().PadRight(27);
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("| 生产厂CCC认证代码 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", ccc));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|      认证产品型号 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", model));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|    记录仪生产时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|    产品生产流水号 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", productflow));
                                    LogMessage("+-------------------+----------------------------+");

                                    if (NeedReport == true)
                                    {
                                        if (_pdfDocument == null)
                                        {
                                            LogMessageError("无法创建报表.");
                                        }
                                        else
                                        {
                                            #region

                                            try
                                            {
                                                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                PdfParagraph par = new PdfParagraph("--- 记录仪唯一性编号 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                par.Alignment = Element.ALIGN_CENTER;
                                                _pdfDocument.Add(par);

                                                par.SpacingBefore = 25f;

                                                PdfPTable table = new PdfPTable(2);

                                                table.SpacingBefore = 25f;

                                                table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                float[] widths = { 50f, 150f };
                                                table.SetWidths(widths);
                                                table.LockedWidth = true;

                                                PdfPCell cell;
                                                cell = new PdfPCell(new Phrase("生产厂CCC认证代码", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(ccc.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("认证产品型号", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(model.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("记录仪生产时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(number.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("产品生产流水号", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(productflow.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);

                                                table.SpacingAfter = 60f;

                                                _pdfDocument.Add(table);
                                            }
                                            catch (Exception ex)
                                            {
                                                LogMessageError("创建报表出错:" + ex.Message);
                                                _pdfDocument = null;
                                            }

                                            #endregion
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "08":
                            #region
                            {
                                int blockCount = (int)(Math.Floor((double)dataLen / (double)126));
                                int dataRemain = dataLen % 126;
                                string sValue = (dataLen.ToString() + "/" + blockCount.ToString()).PadRight(27);
                                //if (isContinued == false)
                                {
                                    LogMessage("+-----------------------------------------------------------------------------+");
                                    LogMessage("| (此处只显示捕获的数据信息,详细内容请见报表.)                                |");
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                    //LogMessage("|      采集起始时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number1));
                                    //LogMessage("+-------------------+----------------------------+----------------------------+");
                                    //LogMessage("|      采集停止时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number2));
                                    //LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                DateTime lastDateTime = DateTime.Now;
                                if (blockCount > 0)
                                {
                                    string numberblock = "";
                                    for (int iblock = 0; iblock < blockCount; iblock++)
                                    {
                                        numberblock = "20" + baData[126 * iblock + 0].ToString("X") + "-" +
                                            baData[126 * iblock + 1].ToString("X") + "-" +
                                            baData[126 * iblock + 2].ToString("X") + " " +
                                            baData[126 * iblock + 3].ToString("X") + ":" +
                                            baData[126 * iblock + 4].ToString("X") + ":" +
                                            baData[126 * iblock + 5].ToString("X");
                                        numberblock = numberblock.PadRight(27);
                                        lastDateTime = new DateTime(
                                            ((int)Math.Floor((double)baData[126 * iblock + 0] / 16.0)) * 10 + baData[126 * iblock + 0] % 16 + 2000,
                                            ((int)Math.Floor((double)baData[126 * iblock + 1] / 16.0)) * 10 + baData[126 * iblock + 1] % 16,
                                            ((int)Math.Floor((double)baData[126 * iblock + 2] / 16.0)) * 10 + baData[126 * iblock + 2] % 16,
                                            ((int)Math.Floor((double)baData[126 * iblock + 3] / 16.0)) * 10 + baData[126 * iblock + 3] % 16,
                                            ((int)Math.Floor((double)baData[126 * iblock + 4] / 16.0)) * 10 + baData[126 * iblock + 4] % 16,
                                            ((int)Math.Floor((double)baData[126 * iblock + 5] / 16.0)) * 10 + baData[126 * iblock + 5] % 16);
                                        lastDateTime = lastDateTime.Subtract(new TimeSpan(0, 0, 1));//blockCount, 0));
                                        ObservableCollection<Tuple<int, byte>> records = new ObservableCollection<Tuple<int, byte>>();
                                        for (int iSec = 0; iSec < 60; iSec++)
                                        {
                                            int speed = baData[126 * iblock + iSec * 2 + 6 + 0];
                                            if (speed == 0xFF)
                                                speed = 0;
                                            byte state = baData[126 * iblock + iSec * 2 + 6 + 1];

                                            records.Add(new Tuple<int, byte>(speed, state));
                                        }
                                        _cmd08HRespOc.Add(new Cmd08HResponse()
                                        {
                                            Index = (_cmd08HRespOc.Count + 1).ToString(),
                                            StartDateTime = numberblock,
                                            Records = records
                                        });
                                    }

                                    LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$| @@@@@@@@@@@@@@@@@@@@@@@@@@@|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue).Replace("@@@@@@@@@@@@@@@@@@@@@@@@@@@", numberblock));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                else
                                {
                                    LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                if (NeedReport == true)
                                {
                                    if (_pdfDocument == null)
                                    {
                                        LogMessageError("无法创建报表.");
                                    }
                                    else
                                    {
                                        _createPdfEvent.Reset();
                                        Task.Factory.StartNew(() =>
                                        {
                                            Create08HReport();
                                        });
                                        _createPdfEvent.WaitOne();
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "09":
                            #region
                            {
                                int blockCount = (int)(Math.Floor((double)dataLen / (double)666));
                                int dataRemain = dataLen % 666;
                                string sValue = (dataLen.ToString() + "/" + blockCount.ToString()).PadRight(27);
                                //if (isContinued == false)
                                {
                                    LogMessage("+-----------------------------------------------------------------------------+");
                                    LogMessage("| (此处只显示捕获的数据信息,详细内容请见报表.)                                |");
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                    //LogMessage("|      采集起始时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number1));
                                    //LogMessage("+-------------------+----------------------------+----------------------------+");
                                    //LogMessage("|      采集停止时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number2));
                                    //LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                DateTime lastDateTime = DateTime.Now;
                                if (blockCount > 0)
                                {
                                    string numberblock = "";
                                    for (int iblock = 0; iblock < blockCount; iblock++)
                                    {
                                        numberblock = "20" + baData[666 * iblock + 0].ToString("X") + "-" +
                                            baData[666 * iblock + 1].ToString("X") + "-" +
                                            baData[666 * iblock + 2].ToString("X") + " " +
                                            baData[666 * iblock + 3].ToString("X") + ":" +
                                            baData[666 * iblock + 4].ToString("X") + ":" +
                                            baData[666 * iblock + 5].ToString("X");
                                        numberblock = numberblock.PadRight(27);
                                        lastDateTime = new DateTime(
                                            ((int)Math.Floor((double)baData[666 * iblock + 0] / 16.0)) * 10 + baData[666 * iblock + 0] % 16 + 2000,
                                            ((int)Math.Floor((double)baData[666 * iblock + 1] / 16.0)) * 10 + baData[666 * iblock + 1] % 16,
                                            ((int)Math.Floor((double)baData[666 * iblock + 2] / 16.0)) * 10 + baData[666 * iblock + 2] % 16,
                                            ((int)Math.Floor((double)baData[666 * iblock + 3] / 16.0)) * 10 + baData[666 * iblock + 3] % 16,
                                            ((int)Math.Floor((double)baData[666 * iblock + 4] / 16.0)) * 10 + baData[666 * iblock + 4] % 16,
                                            ((int)Math.Floor((double)baData[666 * iblock + 5] / 16.0)) * 10 + baData[666 * iblock + 5] % 16);
                                        int min = lastDateTime.Minute;
                                        int sec = lastDateTime.Second;
                                        lastDateTime = lastDateTime.Subtract(new TimeSpan(1, -59 + min, -59 + sec));
                                        ObservableCollection<Tuple<string, string, string, int>> records = new ObservableCollection<Tuple<string, string, string, int>>();
                                        for (int iMin = 0; iMin < 60; iMin++)
                                        {
                                            #region

                                            byte[] baJingDu = new byte[4];
                                            baJingDu[0] = baData[666 * iblock + iMin * 11 + 6 + 0];
                                            baJingDu[1] = baData[666 * iblock + iMin * 11 + 6 + 1];
                                            baJingDu[2] = baData[666 * iblock + iMin * 11 + 6 + 2];
                                            baJingDu[3] = baData[666 * iblock + iMin * 11 + 6 + 3];
                                            string sJingDu = "";
                                            if (baData[666 * iblock + iMin * 11 + 6 + 0] == 0xFF ||
                                                baData[666 * iblock + iMin * 11 + 6 + 1] == 0xFF ||
                                                baData[666 * iblock + iMin * 11 + 6 + 2] == 0xFF ||
                                                baData[666 * iblock + iMin * 11 + 6 + 3] == 0xFF)
                                                sJingDu = "无效";
                                            else
                                            {
                                                float jingDu = System.BitConverter.ToSingle(baJingDu, 0);
                                                if (jingDu >= 0)
                                                    sJingDu = "E" + ConvertJingWeiDuToString(jingDu);
                                                else
                                                    sJingDu = "W" + ConvertJingWeiDuToString(jingDu);
                                            }
                                            byte[] baWeiDu = new byte[4];
                                            baWeiDu[0] = baData[666 * iblock + iMin * 11 + 6 + 4];
                                            baWeiDu[1] = baData[666 * iblock + iMin * 11 + 6 + 5];
                                            baWeiDu[2] = baData[666 * iblock + iMin * 11 + 6 + 6];
                                            baWeiDu[3] = baData[666 * iblock + iMin * 11 + 6 + 7];
                                            string sWeiDu = "";
                                            if (baData[666 * iblock + iMin * 11 + 6 + 4] == 0xFF ||
                                                baData[666 * iblock + iMin * 11 + 6 + 5] == 0xFF ||
                                                baData[666 * iblock + iMin * 11 + 6 + 6] == 0xFF ||
                                                baData[666 * iblock + iMin * 11 + 6 + 7] == 0xFF)
                                                sWeiDu = "无效";
                                            else
                                            {
                                                float weiDu = System.BitConverter.ToSingle(baWeiDu, 0);
                                                if (weiDu >= 0)
                                                    sWeiDu = "N" + ConvertJingWeiDuToString(weiDu);
                                                else
                                                    sWeiDu = "S" + ConvertJingWeiDuToString(weiDu);
                                            }
                                            byte[] baHeight = new byte[2];
                                            baHeight[0] = baData[666 * iblock + iMin * 11 + 6 + 8];
                                            baHeight[1] = baData[666 * iblock + iMin * 11 + 6 + 9];
                                            string sHeight = "";
                                            if (baData[666 * iblock + iMin * 11 + 6 + 8] == 0xFF ||
                                                baData[666 * iblock + iMin * 11 + 6 + 9] == 0xFF)
                                                sHeight = "无效";
                                            else
                                            {
                                                int iHeight = System.BitConverter.ToInt16(baHeight, 0);
                                                sHeight = iHeight.ToString();
                                            }

                                            #endregion

                                            int speed = baData[666 * iblock + iMin * 11 + 6 + 10];
                                            if (speed == 0xFF)
                                                speed = 0;

                                            records.Add(new Tuple<string, string, string, int>(sJingDu, sWeiDu, sHeight, speed));
                                        }
                                        _cmd09HRespOc.Add(new Cmd09HResponse()
                                        {
                                            Index = (_cmd09HRespOc.Count + 1).ToString(),
                                            StartDateTime = numberblock,
                                            Records = records
                                        });
                                    }

                                    LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$| @@@@@@@@@@@@@@@@@@@@@@@@@@@|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue).Replace("@@@@@@@@@@@@@@@@@@@@@@@@@@@", numberblock));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                else
                                {
                                    LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                if (dataRemain != 0)
                                {
                                    string sValue3 = dataRemain.ToString().PadRight(27);
                                    LogMessage("|        错误数据数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue3));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                if (NeedReport == true)
                                {
                                    if (_pdfDocument == null)
                                    {
                                        LogMessageError("无法创建报表.");
                                    }
                                    else
                                    {
                                        _createPdfEvent.Reset();
                                        Task.Factory.StartNew(() =>
                                        {
                                            Create09HReport();
                                        });
                                        _createPdfEvent.WaitOne();
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "10":
                            #region
                            {
                                int blockCount = (int)(Math.Floor((double)dataLen / (double)234));
                                int dataRemain = dataLen % 234;
                                string sValue = (dataLen.ToString() + "/" + blockCount.ToString()).PadRight(27);
                                //if (isContinued == false)
                                {
                                    LogMessage("+-----------------------------------------------------------------------------+");
                                    LogMessage("| (此处只显示捕获的数据信息,详细内容请见报表.)                                |");
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                    //LogMessage("|      采集起始时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number1));
                                    //LogMessage("+-------------------+----------------------------+----------------------------+");
                                    //LogMessage("|      采集停止时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number2));
                                    //LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                DateTime lastDateTime = DateTime.Now;
                                if (blockCount > 0)
                                {
                                    string numberblock = "";
                                    for (int iblock = 0; iblock < blockCount; iblock++)
                                    {
                                        numberblock = "20" + baData[234 * iblock + 0].ToString("X") + "-" +
                                            baData[234 * iblock + 1].ToString("X") + "-" +
                                            baData[234 * iblock + 2].ToString("X") + " " +
                                            baData[234 * iblock + 3].ToString("X") + ":" +
                                            baData[234 * iblock + 4].ToString("X") + ":" +
                                            baData[234 * iblock + 5].ToString("X");
                                        numberblock = numberblock.PadRight(27);
                                        lastDateTime = new DateTime(
                                            ((int)Math.Floor((double)baData[234 * iblock + 0] / 16.0)) * 10 + baData[234 * iblock + 0] % 16 + 2000,
                                            ((int)Math.Floor((double)baData[234 * iblock + 1] / 16.0)) * 10 + baData[234 * iblock + 1] % 16,
                                            ((int)Math.Floor((double)baData[234 * iblock + 2] / 16.0)) * 10 + baData[234 * iblock + 2] % 16,
                                            ((int)Math.Floor((double)baData[234 * iblock + 3] / 16.0)) * 10 + baData[234 * iblock + 3] % 16,
                                            ((int)Math.Floor((double)baData[234 * iblock + 4] / 16.0)) * 10 + baData[234 * iblock + 4] % 16,
                                            ((int)Math.Floor((double)baData[234 * iblock + 5] / 16.0)) * 10 + baData[234 * iblock + 5] % 16);
                                        lastDateTime = lastDateTime.Subtract(new TimeSpan(0, 0, 20));
                                        byte[] baNumber = new byte[18];
                                        for (int idxBa = 0; idxBa < 18; idxBa++)
                                        {
                                            baNumber[idxBa] = baData[234 * iblock + 6 + idxBa];
                                        }
                                        string number = Encoding.UTF8.GetString(baNumber).PadRight(27);
                                        ObservableCollection<Tuple<int, bool>> records = new ObservableCollection<Tuple<int, bool>>();
                                        for (int iRec = 0; iRec < 100; iRec++)
                                        {
                                            int speed = (int)baData[234 * iblock + 24 + iRec * 2 + 0];
                                            if (speed == 0xFF)
                                                speed = 0;
                                            bool state = (((int)baData[234 * iblock + 24 + iRec * 2 + 1] & 1) == 1) ? true : false;
                                            records.Add(new Tuple<int, bool>(speed, state));
                                        }

                                        #region

                                        byte[] baJingDu = new byte[4];
                                        baJingDu[0] = baData[234 * iblock + 224 + 0];
                                        baJingDu[1] = baData[234 * iblock + 224 + 1];
                                        baJingDu[2] = baData[234 * iblock + 224 + 2];
                                        baJingDu[3] = baData[234 * iblock + 224 + 3];
                                        float jingDu = System.BitConverter.ToSingle(baJingDu, 0);
                                        string sJingDu = "";
                                        if (jingDu >= 0)
                                            sJingDu = "E" + ConvertJingWeiDuToString(jingDu);
                                        else
                                            sJingDu = "W" + ConvertJingWeiDuToString(jingDu);
                                        byte[] baWeiDu = new byte[4];
                                        baWeiDu[0] = baData[234 * iblock + 224 + 4];
                                        baWeiDu[1] = baData[234 * iblock + 224 + 5];
                                        baWeiDu[2] = baData[234 * iblock + 224 + 6];
                                        baWeiDu[3] = baData[234 * iblock + 224 + 7];
                                        float weiDu = System.BitConverter.ToSingle(baWeiDu, 0);
                                        string sWeiDu = "";
                                        if (weiDu >= 0)
                                            sWeiDu = "N" + ConvertJingWeiDuToString(weiDu);
                                        else
                                            sWeiDu = "S" + ConvertJingWeiDuToString(weiDu);
                                        byte[] baHeight = new byte[2];
                                        baHeight[0] = baData[234 * iblock + 224 + 8];
                                        baHeight[1] = baData[234 * iblock + 224 + 9];
                                        int iHeight = System.BitConverter.ToInt16(baHeight, 0);
                                        string sHeight = iHeight.ToString();

                                        #endregion

                                        _cmd10HRespOc.Add(new Cmd10HResponse()
                                        {
                                            Index = (_cmd10HRespOc.Count + 1).ToString(),
                                            StopDateTime = numberblock,
                                            Number = number,
                                            Records = records,
                                            Position = sJingDu + "/" + sWeiDu,
                                            Height = sHeight
                                        });
                                    }
                                    LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$| @@@@@@@@@@@@@@@@@@@@@@@@@@@|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue).Replace("@@@@@@@@@@@@@@@@@@@@@@@@@@@", numberblock));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                else
                                {
                                    LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                if (dataRemain != 0)
                                {
                                    string sValue3 = dataRemain.ToString().PadRight(27);
                                    LogMessage("|        错误数据数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue3));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                if (NeedReport == true)
                                {
                                    if (_pdfDocument == null)
                                    {
                                        LogMessageError("无法创建报表.");
                                    }
                                    else
                                    {
                                        _createPdfEvent.Reset();
                                        Task.Factory.StartNew(() =>
                                        {
                                            Create10HReport();
                                        });
                                        _createPdfEvent.WaitOne();
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "11":
                            #region
                            {
                                int blockCount = (int)(Math.Floor((double)dataLen / (double)50));
                                int dataRemain = dataLen % 50;
                                string sValue = (dataLen.ToString() + "/" + blockCount.ToString()).PadRight(27);
                                //if (isContinued == false)
                                {
                                    LogMessage("+-----------------------------------------------------------------------------+");
                                    LogMessage("| (此处只显示捕获的数据信息,详细内容请见报表.)                                |");
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                    //LogMessage("|      采集起始时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number1));
                                    //LogMessage("+-------------------+----------------------------+----------------------------+");
                                    //LogMessage("|      采集停止时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number2));
                                    //LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                DateTime lastDateTime = DateTime.Now;
                                if (blockCount > 0)
                                {
                                    string numberblock = "";
                                    for (int iblock = 0; iblock < blockCount; iblock++)
                                    {
                                        numberblock = "20" + baData[50 * iblock + 18].ToString("X") + "-" +
                                            baData[50 * iblock + 19].ToString("X") + "-" +
                                            baData[50 * iblock + 20].ToString("X") + " " +
                                            baData[50 * iblock + 21].ToString("X") + ":" +
                                            baData[50 * iblock + 22].ToString("X") + ":" +
                                            baData[50 * iblock + 23].ToString("X");
                                        numberblock = numberblock.PadRight(27);
                                        lastDateTime = new DateTime(
                                            ((int)Math.Floor((double)baData[50 * iblock + 18] / 16.0)) * 10 + baData[50 * iblock + 18] % 16 + 2000,
                                            ((int)Math.Floor((double)baData[50 * iblock + 19] / 16.0)) * 10 + baData[50 * iblock + 19] % 16,
                                            ((int)Math.Floor((double)baData[50 * iblock + 20] / 16.0)) * 10 + baData[50 * iblock + 20] % 16,
                                            ((int)Math.Floor((double)baData[50 * iblock + 21] / 16.0)) * 10 + baData[50 * iblock + 21] % 16,
                                            ((int)Math.Floor((double)baData[50 * iblock + 22] / 16.0)) * 10 + baData[50 * iblock + 22] % 16,
                                            ((int)Math.Floor((double)baData[50 * iblock + 23] / 16.0)) * 10 + baData[50 * iblock + 23] % 16);
                                        lastDateTime = lastDateTime.Subtract(new TimeSpan(0, 0, 1));
                                        string numberblockStop = "20" + baData[50 * iblock + 14].ToString("X") + "-" +
                                            baData[50 * iblock + 25].ToString("X") + "-" +
                                            baData[50 * iblock + 26].ToString("X") + " " +
                                            baData[50 * iblock + 27].ToString("X") + ":" +
                                            baData[50 * iblock + 28].ToString("X") + ":" +
                                            baData[50 * iblock + 29].ToString("X");
                                        numberblock = numberblock.PadRight(27);
                                        byte[] baNumber = new byte[18];
                                        for (int idxBa = 0; idxBa < 18; idxBa++)
                                        {
                                            baNumber[idxBa] = baData[50 * iblock + idxBa];
                                        }
                                        string number = Encoding.UTF8.GetString(baNumber).PadRight(27);

                                        #region

                                        byte[] baJingDu = new byte[4];
                                        baJingDu[0] = baData[50 * iblock + 30];
                                        baJingDu[1] = baData[50 * iblock + 31];
                                        baJingDu[2] = baData[50 * iblock + 32];
                                        baJingDu[3] = baData[50 * iblock + 33];
                                        float jingDu = System.BitConverter.ToSingle(baJingDu, 0);
                                        string sJingDu = "";
                                        if (jingDu >= 0)
                                            sJingDu = "E" + ConvertJingWeiDuToString(jingDu);
                                        else
                                            sJingDu = "W" + ConvertJingWeiDuToString(jingDu);
                                        byte[] baWeiDu = new byte[4];
                                        baWeiDu[0] = baData[50 * iblock + 34];
                                        baWeiDu[1] = baData[50 * iblock + 35];
                                        baWeiDu[2] = baData[50 * iblock + 36];
                                        baWeiDu[3] = baData[50 * iblock + 37];
                                        float weiDu = System.BitConverter.ToSingle(baWeiDu, 0);
                                        string sWeiDu = "";
                                        if (weiDu >= 0)
                                            sWeiDu = "N" + ConvertJingWeiDuToString(weiDu);
                                        else
                                            sWeiDu = "S" + ConvertJingWeiDuToString(weiDu);
                                        byte[] baHeight = new byte[2];
                                        baHeight[0] = baData[50 * iblock + 38];
                                        baHeight[1] = baData[50 * iblock + 39];
                                        int iHeight = System.BitConverter.ToInt16(baHeight, 0);
                                        string sHeight = iHeight.ToString();

                                        #endregion

                                        #region

                                        byte[] baJingDu1 = new byte[4];
                                        baJingDu1[0] = baData[50 * iblock + 40];
                                        baJingDu1[1] = baData[50 * iblock + 41];
                                        baJingDu1[2] = baData[50 * iblock + 42];
                                        baJingDu1[3] = baData[50 * iblock + 43];
                                        float jingDu1 = System.BitConverter.ToSingle(baJingDu1, 0);
                                        string sJingDu1 = "";
                                        if (jingDu1 >= 0)
                                            sJingDu1 = "E" + ConvertJingWeiDuToString(jingDu1);
                                        else
                                            sJingDu1 = "W" + ConvertJingWeiDuToString(jingDu1);
                                        byte[] baWeiDu1 = new byte[4];
                                        baWeiDu1[0] = baData[50 * iblock + 44];
                                        baWeiDu1[1] = baData[50 * iblock + 45];
                                        baWeiDu1[2] = baData[50 * iblock + 46];
                                        baWeiDu1[3] = baData[50 * iblock + 47];
                                        float weiDu1 = System.BitConverter.ToSingle(baWeiDu1, 0);
                                        string sWeiDu1 = "";
                                        if (weiDu1 >= 0)
                                            sWeiDu1 = "N" + ConvertJingWeiDuToString(weiDu1);
                                        else
                                            sWeiDu1 = "S" + ConvertJingWeiDuToString(weiDu1);
                                        byte[] baHeight1 = new byte[2];
                                        baHeight1[0] = baData[50 * iblock + 48];
                                        baHeight1[1] = baData[50 * iblock + 49];
                                        int iHeight1 = System.BitConverter.ToInt16(baHeight1, 0);
                                        string sHeight1 = iHeight1.ToString();

                                        #endregion

                                        _cmd11HRespOc.Add(new Cmd11HResponse()
                                        {
                                            Index = (_cmd11HRespOc.Count + 1).ToString(),
                                            Number = number,
                                            RecordStartDateTime = numberblock,
                                            RecordStopDateTime = numberblockStop,
                                            StartPosition = sJingDu + "/" + sWeiDu,
                                            StopPosition = sJingDu1 + "/" + sWeiDu1,
                                            StartHeight = sHeight,
                                            StopHeight = sHeight1
                                        });
                                    }
                                    LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$| @@@@@@@@@@@@@@@@@@@@@@@@@@@|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue).Replace("@@@@@@@@@@@@@@@@@@@@@@@@@@@", numberblock));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                else
                                {
                                    LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                if (dataRemain != 0)
                                {
                                    string sValue3 = dataRemain.ToString().PadRight(27);
                                    LogMessage("|        错误数据数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue3));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                if (NeedReport == true)
                                {
                                    if (_pdfDocument == null)
                                    {
                                        LogMessageError("无法创建报表.");
                                    }
                                    else
                                    {
                                        _createPdfEvent.Reset();
                                        Task.Factory.StartNew(() =>
                                        {
                                            Create11HReport();
                                        });
                                        _createPdfEvent.WaitOne();
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "12":
                            #region
                            {
                                int blockCount = (int)(Math.Floor((double)dataLen / (double)25));
                                int dataRemain = dataLen % 25;
                                string sValue = (dataLen.ToString() + "/" + blockCount.ToString()).PadRight(27);
                                //if (isContinued == false)
                                {
                                    LogMessage("+-----------------------------------------------------------------------------+");
                                    LogMessage("| (此处只显示捕获的数据信息,详细内容请见报表.)                                |");
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                    //LogMessage("|      采集起始时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number1));
                                    //LogMessage("+-------------------+----------------------------+----------------------------+");
                                    //LogMessage("|      采集停止时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number2));
                                    //LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                DateTime lastDateTime = DateTime.Now;
                                if (blockCount > 0)
                                {
                                    string numberblock = "";
                                    for (int iblock = 0; iblock < blockCount; iblock++)
                                    {
                                        numberblock = "20" + baData[25 * iblock + 0].ToString("X") + "-" +
                                             baData[25 * iblock + 1].ToString("X") + "-" +
                                             baData[25 * iblock + 2].ToString("X") + " " +
                                             baData[25 * iblock + 3].ToString("X") + ":" +
                                             baData[25 * iblock + 4].ToString("X") + ":" +
                                             baData[25 * iblock + 5].ToString("X");
                                        numberblock = numberblock.PadRight(27);
                                        lastDateTime = new DateTime(
                                            ((int)Math.Floor((double)baData[25 * iblock + 0] / 16.0)) * 10 + baData[25 * iblock + 0] % 16 + 2000,
                                            ((int)Math.Floor((double)baData[25 * iblock + 1] / 16.0)) * 10 + baData[25 * iblock + 1] % 16,
                                            ((int)Math.Floor((double)baData[25 * iblock + 2] / 16.0)) * 10 + baData[25 * iblock + 2] % 16,
                                            ((int)Math.Floor((double)baData[25 * iblock + 3] / 16.0)) * 10 + baData[25 * iblock + 3] % 16,
                                            ((int)Math.Floor((double)baData[25 * iblock + 4] / 16.0)) * 10 + baData[25 * iblock + 4] % 16,
                                            ((int)Math.Floor((double)baData[25 * iblock + 5] / 16.0)) * 10 + baData[25 * iblock + 5] % 16);
                                        lastDateTime = lastDateTime.Subtract(new TimeSpan(0, 0, 1));
                                        byte[] baNumber = new byte[18];
                                        for (int idxBa = 0; idxBa < 18; idxBa++)
                                        {
                                            baNumber[idxBa] = baData[25 * iblock + 6 + idxBa];
                                        }
                                        string number = Encoding.UTF8.GetString(baNumber).PadRight(27);
                                        string oper = "";
                                        switch (baData[25 * iblock + 24].ToString("X").Trim().ToUpper())
                                        {
                                            default:
                                                oper = "未知操作";
                                                break;
                                            case "1":
                                            case "01":
                                                oper = "登录";
                                                break;
                                            case "2":
                                            case "02":
                                                oper = "退出";
                                                break;
                                        }
                                        _cmd12HRespOc.Add(new Cmd12HResponse()
                                        {
                                            Index = (_cmd12HRespOc.Count + 1).ToString(),
                                            RecordDateTime = numberblock.Trim(),
                                            Number = number,
                                            Description = oper
                                        });
                                    }
                                    LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$| @@@@@@@@@@@@@@@@@@@@@@@@@@@|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue).Replace("@@@@@@@@@@@@@@@@@@@@@@@@@@@", numberblock));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                else
                                {
                                    LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                if (dataRemain != 0)
                                {
                                    string sValue3 = dataRemain.ToString().PadRight(27);
                                    LogMessage("|        错误数据数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue3));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                if (NeedReport == true)
                                {
                                    if (_pdfDocument == null)
                                    {
                                        LogMessageError("无法创建报表.");
                                    }
                                    else
                                    {
                                        _createPdfEvent.Reset();
                                        Task.Factory.StartNew(() =>
                                        {
                                            Create12HReport();
                                        });
                                        _createPdfEvent.WaitOne();
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "13":
                            #region
                            {
                                int blockCount = (int)(Math.Floor((double)dataLen / (double)7));
                                int dataRemain = dataLen % 7;
                                string sValue = (dataLen.ToString() + "/" + blockCount.ToString()).PadRight(27);
                                //if (isContinued == false)
                                {
                                    LogMessage("+-----------------------------------------------------------------------------+");
                                    LogMessage("| (此处只显示捕获的数据信息,详细内容请见报表.)                                |");
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                    //LogMessage("|      采集起始时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number1));
                                    //LogMessage("+-------------------+----------------------------+----------------------------+");
                                    //LogMessage("|      采集停止时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number2));
                                    //LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                DateTime lastDateTime = DateTime.Now;
                                if (blockCount > 0)
                                {
                                    string numberblock = "";
                                    for (int iblock = 0; iblock < blockCount; iblock++)
                                    {
                                        numberblock = "20" + baData[7 * iblock + 0].ToString("X") + "-" +
                                            baData[7 * iblock + 1].ToString("X") + "-" +
                                            baData[7 * iblock + 2].ToString("X") + " " +
                                            baData[7 * iblock + 3].ToString("X") + ":" +
                                            baData[7 * iblock + 4].ToString("X") + ":" +
                                            baData[7 * iblock + 5].ToString("X");
                                        numberblock = numberblock.PadRight(27);
                                        lastDateTime = new DateTime(
                                            ((int)Math.Floor((double)baData[7 * iblock + 0] / 16.0)) * 10 + baData[7 * iblock + 0] % 16 + 2000,
                                            ((int)Math.Floor((double)baData[7 * iblock + 1] / 16.0)) * 10 + baData[7 * iblock + 1] % 16,
                                            ((int)Math.Floor((double)baData[7 * iblock + 2] / 16.0)) * 10 + baData[7 * iblock + 2] % 16,
                                            ((int)Math.Floor((double)baData[7 * iblock + 3] / 16.0)) * 10 + baData[7 * iblock + 3] % 16,
                                            ((int)Math.Floor((double)baData[7 * iblock + 4] / 16.0)) * 10 + baData[7 * iblock + 4] % 16,
                                            ((int)Math.Floor((double)baData[7 * iblock + 5] / 16.0)) * 10 + baData[7 * iblock + 5] % 16);
                                        lastDateTime = lastDateTime.Subtract(new TimeSpan(0, 0, 1));
                                        string oper = "";
                                        switch (baData[7 * iblock + 6].ToString("X").Trim().ToUpper())
                                        {
                                            default:
                                                oper = "未知状态";
                                                break;
                                            case "1":
                                            case "01":
                                                oper = "通电";
                                                break;
                                            case "2":
                                            case "02":
                                                oper = "断电";
                                                break;
                                        }
                                        _cmd13HRespOc.Add(new Cmd13HResponse()
                                        {
                                            Index = (_cmd13HRespOc.Count + 1).ToString(),
                                            RecordDateTime = numberblock.Trim(),
                                            Description = oper
                                        });
                                    }
                                    LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$| @@@@@@@@@@@@@@@@@@@@@@@@@@@|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue).Replace("@@@@@@@@@@@@@@@@@@@@@@@@@@@", numberblock));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                else
                                {
                                    LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                if (dataRemain != 0)
                                {
                                    string sValue3 = dataRemain.ToString().PadRight(27);
                                    LogMessage("|        错误数据数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue3));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                if (NeedReport == true)
                                {
                                    if (_pdfDocument == null)
                                    {
                                        LogMessageError("无法创建报表.");
                                    }
                                    else
                                    {
                                        _createPdfEvent.Reset();
                                        Task.Factory.StartNew(() =>
                                        {
                                            Create13HReport();
                                        });
                                        _createPdfEvent.WaitOne();
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "14":
                            #region
                            {
                                int blockCount = (int)(Math.Floor((double)dataLen / (double)7));
                                int dataRemain = dataLen % 7;
                                string sValue = (dataLen.ToString() + "/" + blockCount.ToString()).PadRight(27);
                                //if (isContinued == false)
                                {
                                    LogMessage("+-----------------------------------------------------------------------------+");
                                    LogMessage("| (此处只显示捕获的数据信息,详细内容请见报表.)                                |");
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                    //LogMessage("|      采集起始时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number1));
                                    //LogMessage("+-------------------+----------------------------+----------------------------+");
                                    //LogMessage("|      采集停止时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number2));
                                    //LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                DateTime lastDateTime = DateTime.Now;
                                if (blockCount > 0)
                                {
                                    string numberblock = "";
                                    for (int iblock = 0; iblock < blockCount; iblock++)
                                    {
                                        numberblock = "20" + baData[7 * iblock + 0].ToString("X") + "-" +
                                            baData[7 * iblock + 1].ToString("X") + "-" +
                                            baData[7 * iblock + 2].ToString("X") + " " +
                                            baData[7 * iblock + 3].ToString("X") + ":" +
                                            baData[7 * iblock + 4].ToString("X") + ":" +
                                            baData[7 * iblock + 5].ToString("X");
                                        numberblock = numberblock.PadRight(27);
                                        lastDateTime = new DateTime(
                                            ((int)Math.Floor((double)baData[7 * iblock + 0] / 16.0)) * 10 + baData[7 * iblock + 0] % 16 + 2000,
                                            ((int)Math.Floor((double)baData[7 * iblock + 1] / 16.0)) * 10 + baData[7 * iblock + 1] % 16,
                                            ((int)Math.Floor((double)baData[7 * iblock + 2] / 16.0)) * 10 + baData[7 * iblock + 2] % 16,
                                            ((int)Math.Floor((double)baData[7 * iblock + 3] / 16.0)) * 10 + baData[7 * iblock + 3] % 16,
                                            ((int)Math.Floor((double)baData[7 * iblock + 4] / 16.0)) * 10 + baData[7 * iblock + 4] % 16,
                                            ((int)Math.Floor((double)baData[7 * iblock + 5] / 16.0)) * 10 + baData[7 * iblock + 5] % 16);
                                        lastDateTime = lastDateTime.Subtract(new TimeSpan(0, 0, 1));
                                        string oper = "";
                                        switch (baData[7 * iblock + 6].ToString("X").Trim().ToUpper())
                                        {
                                            default:
                                                oper = "未知操作";
                                                break;
                                            case "82":
                                                oper = "修改车辆信息";
                                                break;
                                            case "83":
                                                oper = "修改初次安装日期";
                                                break;
                                            case "84":
                                                oper = "修改状态量配置信息";
                                                break;
                                            case "C2":
                                                oper = "修改记录仪时间";
                                                break;
                                            case "C3":
                                                oper = "修改脉冲系数";
                                                break;
                                            case "C4":
                                                oper = "修改初始里程";
                                                break;
                                        }
                                        _cmd14HRespOc.Add(new Cmd14HResponse()
                                        {
                                            Index = (_cmd14HRespOc.Count + 1).ToString(),
                                            RecordDateTime = numberblock.Trim(),
                                            Description = oper
                                        });
                                    }
                                    LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$| @@@@@@@@@@@@@@@@@@@@@@@@@@@|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue).Replace("@@@@@@@@@@@@@@@@@@@@@@@@@@@", numberblock));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                else
                                {
                                    LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                if (dataRemain != 0)
                                {
                                    string sValue3 = dataRemain.ToString().PadRight(27);
                                    LogMessage("|        错误数据数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue3));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                if (NeedReport == true)
                                {
                                    if (_pdfDocument == null)
                                    {
                                        LogMessageError("无法创建报表.");
                                    }
                                    else
                                    {
                                        _createPdfEvent.Reset();
                                        Task.Factory.StartNew(() =>
                                        {
                                            Create14HReport();
                                        });
                                        _createPdfEvent.WaitOne();
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "15":
                            #region
                            {
                                int blockCount = (int)(Math.Floor((double)dataLen / (double)133));
                                int dataRemain = dataLen % 133;
                                string sValue = (dataLen.ToString() + "/" + blockCount.ToString()).PadRight(27);
                                //if (isContinued == false)
                                {
                                    LogMessage("+-----------------------------------------------------------------------------+");
                                    LogMessage("| (此处只显示捕获的数据信息,详细内容请见报表.)                                |");
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                    //LogMessage("|      采集起始时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number1));
                                    //LogMessage("+-------------------+----------------------------+----------------------------+");
                                    //LogMessage("|      采集停止时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number2));
                                    //LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                DateTime lastDateTime = DateTime.Now;
                                if (blockCount > 0)
                                {
                                    string numberblock = "";
                                    for (int iblock = 0; iblock < blockCount; iblock++)
                                    {
                                        byte state = baData[133 * iblock + 0];

                                        numberblock = "20" + baData[133 * iblock + 1].ToString("X") + "-" +
                                            baData[133 * iblock + 2].ToString("X") + "-" +
                                            baData[133 * iblock + 3].ToString("X") + " " +
                                            baData[133 * iblock + 4].ToString("X") + ":" +
                                            baData[133 * iblock + 5].ToString("X") + ":" +
                                            baData[133 * iblock + 6].ToString("X");
                                        numberblock = numberblock.PadRight(27);
                                        lastDateTime = new DateTime(
                                            ((int)Math.Floor((double)baData[133 * iblock + 1] / 16.0)) * 10 + baData[133 * iblock + 1] % 16 + 2000,
                                            ((int)Math.Floor((double)baData[133 * iblock + 2] / 16.0)) * 10 + baData[133 * iblock + 2] % 16,
                                            ((int)Math.Floor((double)baData[133 * iblock + 3] / 16.0)) * 10 + baData[133 * iblock + 3] % 16,
                                            ((int)Math.Floor((double)baData[133 * iblock + 4] / 16.0)) * 10 + baData[133 * iblock + 4] % 16,
                                            ((int)Math.Floor((double)baData[133 * iblock + 5] / 16.0)) * 10 + baData[133 * iblock + 5] % 16,
                                            ((int)Math.Floor((double)baData[133 * iblock + 6] / 16.0)) * 10 + baData[133 * iblock + 6] % 16);
                                        lastDateTime = lastDateTime.Subtract(new TimeSpan(0, 0, 1));//blockCount, 0));
                                        string numberblock2 = "20" + baData[133 * iblock + 7].ToString("X") + "-" +
                                            baData[133 * iblock + 8].ToString("X") + "-" +
                                            baData[133 * iblock + 9].ToString("X") + " " +
                                            baData[133 * iblock + 10].ToString("X") + ":" +
                                            baData[133 * iblock + 11].ToString("X") + ":" +
                                            baData[133 * iblock + 12].ToString("X");
                                        numberblock2 = numberblock.PadRight(27);
                                        ObservableCollection<Tuple<int, int>> records = new ObservableCollection<Tuple<int, int>>();
                                        for (int iSec = 0; iSec < 60; iSec++)
                                        {
                                            int speed = baData[133 * iblock + iSec * 2 + 13 + 0];
                                            if (speed == 0xFF)
                                                speed = 0;
                                            int refSpeed = baData[133 * iblock + iSec * 2 + 13 + 1];
                                            if (refSpeed == 0xFF)
                                                refSpeed = 0;

                                            records.Add(new Tuple<int, int>(speed, refSpeed));
                                        }
                                        _cmd15HRespOc.Add(new Cmd15HResponse()
                                        {
                                            Index = (_cmd15HRespOc.Count + 1).ToString(),
                                            State = state,
                                            StartDateTime = numberblock,
                                            StopDateTime = numberblock2,
                                            Records = records
                                        });
                                    }

                                    LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$| @@@@@@@@@@@@@@@@@@@@@@@@@@@|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue).Replace("@@@@@@@@@@@@@@@@@@@@@@@@@@@", numberblock));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                else
                                {
                                    LogMessage("| 数据总数/数据块数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                if (dataRemain != 0)
                                {
                                    string sValue3 = dataRemain.ToString().PadRight(27);
                                    LogMessage("|        错误数据数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|                            |".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", sValue3));
                                    LogMessage("+-------------------+----------------------------+----------------------------+");
                                }
                                if (NeedReport == true)
                                {
                                    if (_pdfDocument == null)
                                    {
                                        LogMessageError("无法创建报表.");
                                    }
                                    else
                                    {
                                        _createPdfEvent.Reset();
                                        Task.Factory.StartNew(() =>
                                        {
                                            Create15HReport();
                                        });
                                        _createPdfEvent.WaitOne();
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "82":
                            LogMessageInformation("成功执行.");
                            break;
                        case "83":
                            LogMessageInformation("成功执行.");
                            break;
                        case "84":
                            LogMessageInformation("成功执行.");
                            break;
                        case "C2":
                            LogMessageInformation("成功执行.");
                            break;
                        case "C3":
                            LogMessageInformation("成功执行.");
                            break;
                        case "C4":
                            LogMessageInformation("成功执行.");
                            break;
                        case "20":
                            #region
                            {
                                if (dataLen != 1)
                                    LogMessageError("命令(" + cdiCmdContent + ")的响应的数据块长度错误.");
                                else
                                {
                                    string number = baData[0].ToString().PadRight(27);
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|  传感器单圈脉冲数 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                    LogMessage("+-------------------+----------------------------+");

                                    if (NeedReport == true)
                                    {
                                        if (_pdfDocument == null)
                                        {
                                            LogMessageError("无法创建报表.");
                                        }
                                        else
                                        {
                                            #region

                                            try
                                            {
                                                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                PdfParagraph par = new PdfParagraph("--- 传感器单圈脉冲数 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                par.Alignment = Element.ALIGN_CENTER;
                                                _pdfDocument.Add(par);

                                                par.SpacingBefore = 25f;

                                                PdfPTable table = new PdfPTable(2);

                                                table.SpacingBefore = 25f;

                                                table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                float[] widths = { 50f, 150f };
                                                table.SetWidths(widths);
                                                table.LockedWidth = true;

                                                PdfPCell cell;
                                                cell = new PdfPCell(new Phrase("传感器单圈脉冲数", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(number.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);

                                                table.SpacingAfter = 60f;

                                                _pdfDocument.Add(table);
                                            }
                                            catch (Exception ex)
                                            {
                                                LogMessageError("创建报表出错:" + ex.Message);
                                                _pdfDocument = null;
                                            }

                                            #endregion
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "21":
                            #region
                            {
                                if (dataLen != 8 && dataLen != 14)
                                    LogMessageError("命令(" + cdiCmdContent + ")的响应的数据块长度错误.");
                                else
                                {
                                    int intPad = 0;
                                    if (dataLen == 14)
                                        intPad = 6;
                                    string number = "";
                                    if (dataLen == 14)
                                    {
                                        number = "20" + baData[0].ToString("X") + "-" + baData[1].ToString("X") + "-" + baData[2].ToString("X") + " " +
                                           baData[3].ToString("X") + ":" + baData[4].ToString("X") + ":" + baData[5].ToString("X");
                                        number = number.PadRight(27);
                                    }
                                    string d2Disp = "D2(" + D2 + ")";
                                    d2Disp.PadLeft(18 - D2.Length);
                                    string d1Disp = "D2(" + D1 + ")";
                                    d1Disp.PadLeft(18 - D1.Length);
                                    string d0Disp = "D2(" + D0 + ")";
                                    d0Disp.PadLeft(18 - D0.Length);
                                    string d0 = "";
                                    switch ((int)baData[0 + intPad])
                                    {
                                        default:
                                            d0 = "未知状态";
                                            break;
                                        case 0:
                                            d0 = "低有效";
                                            break;
                                        case 1:
                                            d0 = "高有效";
                                            break;
                                        case 3:
                                            d0 = "未启用";
                                            break;
                                    }
                                    d0 = d0.PadLeft(27 - d0.Length);
                                    string d1 = "";
                                    switch ((int)baData[1 + intPad])
                                    {
                                        default:
                                            d1 = "未知状态";
                                            break;
                                        case 0:
                                            d1 = "低有效";
                                            break;
                                        case 1:
                                            d1 = "高有效";
                                            break;
                                        case 3:
                                            d1 = "未启用";
                                            break;
                                    }
                                    d1 = d1.PadLeft(27 - d1.Length);
                                    string d2 = "";
                                    switch ((int)baData[2 + intPad])
                                    {
                                        default:
                                            d2 = "未知状态";
                                            break;
                                        case 0:
                                            d2 = "低有效";
                                            break;
                                        case 1:
                                            d2 = "高有效";
                                            break;
                                        case 3:
                                            d2 = "未启用";
                                            break;
                                    }
                                    d2 = d2.PadLeft(27 - d2.Length);
                                    string d3 = "";
                                    switch ((int)baData[3 + intPad])
                                    {
                                        default:
                                            d3 = "未知状态";
                                            break;
                                        case 0:
                                            d3 = "低有效";
                                            break;
                                        case 1:
                                            d3 = "高有效";
                                            break;
                                        case 3:
                                            d3 = "未启用";
                                            break;
                                    }
                                    d3 = d3.PadLeft(27 - d3.Length);
                                    string d4 = "";
                                    switch ((int)baData[4 + intPad])
                                    {
                                        default:
                                            d4 = "未知状态";
                                            break;
                                        case 0:
                                            d4 = "低有效";
                                            break;
                                        case 1:
                                            d4 = "高有效";
                                            break;
                                        case 3:
                                            d4 = "未启用";
                                            break;
                                    }
                                    d4 = d4.PadLeft(27 - d4.Length);
                                    string d5 = "";
                                    switch ((int)baData[5 + intPad])
                                    {
                                        default:
                                            d5 = "未知状态";
                                            break;
                                        case 0:
                                            d5 = "低有效";
                                            break;
                                        case 1:
                                            d5 = "高有效";
                                            break;
                                        case 3:
                                            d5 = "未启用";
                                            break;
                                    }
                                    d5 = d5.PadLeft(27 - d5.Length);
                                    string d6 = "";
                                    switch ((int)baData[6 + intPad])
                                    {
                                        default:
                                            d6 = "未知状态";
                                            break;
                                        case 0:
                                            d6 = "低有效";
                                            break;
                                        case 1:
                                            d6 = "高有效";
                                            break;
                                        case 3:
                                            d6 = "未启用";
                                            break;
                                    }
                                    d6 = d6.PadLeft(27 - d6.Length);
                                    string d7 = "";
                                    switch ((int)baData[7 + intPad])
                                    {
                                        default:
                                            d7 = "未知状态";
                                            break;
                                        case 0:
                                            d7 = "低有效";
                                            break;
                                        case 1:
                                            d7 = "高有效";
                                            break;
                                        case 3:
                                            d7 = "未启用";
                                            break;
                                    }
                                    d7 = d7.PadLeft(27 - d7.Length);
                                    if (dataLen == 14)
                                    {
                                        LogMessage("+-------------------+----------------------------+");
                                        LogMessage("|          采集时间 | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", number));
                                    }
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|          D7(制动) | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d7));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|        D6(左转向) | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d6));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|        D5(右转向) | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d5));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|          D4(远光) | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d4));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|          D3(近光) | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d3));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|@@@@@@@@@@@@@@@@@@ | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("@@@@@@@@@@@@@@@@@@", d2Disp).Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d2));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|@@@@@@@@@@@@@@@@@@ | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("@@@@@@@@@@@@@@@@@@", d1Disp).Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d1));
                                    LogMessage("+-------------------+----------------------------+");
                                    LogMessage("|@@@@@@@@@@@@@@@@@@ | $$$$$$$$$$$$$$$$$$$$$$$$$$$|".Replace("@@@@@@@@@@@@@@@@@@", d0Disp).Replace("$$$$$$$$$$$$$$$$$$$$$$$$$$$", d0));
                                    LogMessage("+-------------------+----------------------------+");

                                    if (NeedReport == true)
                                    {
                                        if (_pdfDocument == null)
                                        {
                                            LogMessageError("无法创建报表.");
                                        }
                                        else
                                        {
                                            #region

                                            try
                                            {
                                                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                                                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                                                PdfParagraph par = new PdfParagraph("--- 信号量状态配置 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                                                par.Alignment = Element.ALIGN_CENTER;
                                                _pdfDocument.Add(par);

                                                par.SpacingBefore = 25f;

                                                PdfPTable table = new PdfPTable(2);

                                                table.SpacingBefore = 25f;

                                                table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                                                float[] widths = { 50f, 150f };
                                                table.SetWidths(widths);
                                                table.LockedWidth = true;

                                                PdfPCell cell;
                                                if (dataLen == 14)
                                                {
                                                    cell = new PdfPCell(new Phrase("采集时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                    table.AddCell(cell);
                                                    cell = new PdfPCell(new Phrase(number.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                    table.AddCell(cell);
                                                }
                                                cell = new PdfPCell(new Phrase("D7", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d7.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("D6", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d6.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("D5", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d4.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("D4", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d4.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase("D3", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d3.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d2Disp.Trim(), new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d2.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d1Disp.Trim(), new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d1.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d0Disp.Trim(), new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);
                                                cell = new PdfPCell(new Phrase(d0.Trim(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                                                cell.HorizontalAlignment = Element.ALIGN_CENTER;
                                                cell.VerticalAlignment = Element.ALIGN_CENTER;
                                                table.AddCell(cell);

                                                table.SpacingAfter = 60f;

                                                _pdfDocument.Add(table);
                                            }
                                            catch (Exception ex)
                                            {
                                                LogMessageError("创建报表出错:" + ex.Message);
                                                _pdfDocument = null;
                                            }

                                            #endregion
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "D0":
                            LogMessageInformation("成功执行.");
                            break;
                        case "D1":
                            LogMessageInformation("成功执行.");
                            break;
                    }

                    #endregion

                    position = position + dataLen + 23;

                    if(i < iCount)
                        LogMessage("");
                }

                if(NeedReport)
                    CloseReport();
            }
            catch (Exception ex)
            {
                LogMessageError("不能够解析VDR文件:" + ex.Message);
            }

            LogMessageSeperator();

            InRun = false;
        }

        private string ConvertJingWeiDuToString(float fVal)
        {
            float fValNew = fVal * 10000.0f;
            fValNew = Math.Abs(fValNew);

            int iValDu = (int)Math.Floor(Math.Floor(fValNew) / 60.0);

            float fValFen = (float)Math.Floor(fValNew - (iValDu * 60));
            int iValFen = (int)fValFen;

            float fValMiao = (float)(fValNew - Math.Floor(fValNew));
            fValMiao = (float)Math.Ceiling(fValMiao * 60.0f);
            int iValMiao = (int)fValMiao;

            return string.Format("{0:D}", iValDu) + "°"+ string.Format("{0:D}", iValFen) + "′" + string.Format("{0:D}", iValMiao) + "″"; 
        }

        private void Create08HReport()
        {
            Dispatcher.Invoke((ThreadStart)delegate
            {
                pbarMain.IsIndeterminate = true;
            }, null);
            ReadyString2 = "创建报表中...";
            try
            {
                //Document document = new Document(PageSize.A4);
                //PdfWriter.GetInstance(document, new FileStream(CurrentDirectory + @"\08H.pdf", FileMode.Create));
                //_pdfDocument.Open();

                #region

                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                PdfParagraph par = new PdfParagraph("--- 行驶速度记录 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                par.Alignment = Element.ALIGN_CENTER;
                _pdfDocument.Add(par);

                par.SpacingBefore = 25f;

                PdfParagraph par1 = new PdfParagraph("(" + _cmd08HRespOc.Count.ToString() + "个结果)", new Font(baseFont, 10, Font.NORMAL, BaseColor.BLUE));
                par1.Alignment = Element.ALIGN_CENTER;
                par1.SpacingBefore = 5f;

                if (_cmd08HRespOc.Count < 1)
                    par1.SpacingAfter = 35f;

                _pdfDocument.Add(par1);

                int index = 0;

                foreach (Cmd08HResponse cri in _cmd08HRespOc)
                {
                    PdfPTable table = new PdfPTable(11);

                    if (index == 0)
                        table.SpacingBefore = 25f;
                    else
                        table.SpacingBefore = 15f;
                    index++;

                    table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                    float[] widths = { 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f };
                    table.SetWidths(widths);
                    table.LockedWidth = true;

                    PdfPCell cell;
                    cell = new PdfPCell(new Phrase("开始时间:" + cri.StartDateTime, new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    cell.Colspan = 5;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("注:XX/YY(XX为速度,单位km/h;YY为状态信号,\"1\"为有效)", new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    cell.Colspan = 6;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("第1条", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    for (int i = 0; i < 10; i++)
                    {
                        cell = new PdfPCell(new Phrase(i.ToString(), new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);
                    }

                    ObservableCollection<Tuple<int, byte>> records = cri.Records;
                    for (int i = 0; i < 6; i++)
                    {
                        cell = new PdfPCell(new Phrase(i.ToString(), new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);

                        for (int j = 0; j < 10; j++)
                        {
                            int speed = records[i * 10 + j].Item1;
                            byte state = records[i * 10 + j].Item2;

                            cell = new PdfPCell(new Phrase(speed.ToString() + "/" + string.Format("{0:X2}", state), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                            cell.HorizontalAlignment = Element.ALIGN_CENTER;
                            cell.VerticalAlignment = Element.ALIGN_CENTER;
                            table.AddCell(cell);
                        }
                    }

                    if (index == _cmd15HRespOc.Count)
                        table.SpacingAfter = 60f;

                    _pdfDocument.Add(table);
                }

                #endregion

                //_pdfDocument.Close();

                //LogMessageInformation("成功创建报表."); 
                ////LogMessageInformation("成功创建报表.点击下面链接打开该报表:");
                ////LogMessageLink(CurrentDirectory + @"\08H.pdf");
                ////LogMessageInformation("或点击下面链接打开该报表所在文件夹:");
                ////LogMessageLink(CurrentDirectory);

                ////System.Diagnostics.Process.Start(CurrentDirectory + @"\08H.pdf");
            }
            catch (Exception ex)
            {
                LogMessageError("创建报表出错:" + ex.Message);
                _pdfDocument = null;
            }
            Dispatcher.Invoke((ThreadStart)delegate
            {
                pbarMain.IsIndeterminate = false;
            }, null);
            ReadyString2 = "";
            _createPdfEvent.Set();
        }

        private void Create09HReport()
        {
            Dispatcher.Invoke((ThreadStart)delegate
            {
                pbarMain.IsIndeterminate = true;
            }, null);
            ReadyString2 = "创建报表中...";
            try
            {
                //Document document = new Document(PageSize.A4);
                //PdfWriter.GetInstance(document, new FileStream(CurrentDirectory + @"\09H.pdf", FileMode.Create));
                //_pdfDocument.Open();

                #region

                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                PdfParagraph par = new PdfParagraph("--- 位置信息记录 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                par.Alignment = Element.ALIGN_CENTER;
                _pdfDocument.Add(par);

                par.SpacingBefore = 25f;
                
                PdfParagraph par1 = new PdfParagraph("(" + _cmd09HRespOc.Count.ToString() + "个结果)", new Font(baseFont, 10, Font.NORMAL, BaseColor.BLUE));
                par1.Alignment = Element.ALIGN_CENTER;
                par1.SpacingBefore = 5f;

                if (_cmd09HRespOc.Count < 1)
                    par1.SpacingAfter = 35f;

                _pdfDocument.Add(par1);

                int index = 0;

                foreach (Cmd09HResponse cri in _cmd09HRespOc)
                {
                    PdfPTable table = new PdfPTable(11);

                    if (index == 0)
                        table.SpacingBefore = 25f;
                    else
                        table.SpacingBefore = 15f;
                    index++;

                    table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                    float[] widths = { 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f };
                    table.SetWidths(widths);
                    table.LockedWidth = true;

                    PdfPCell cell;
                    cell = new PdfPCell(new Phrase("开始时间:" + cri.StartDateTime, new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    cell.Colspan = 5;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("注:XX/YY/ZZ/KK(XX经度;YY纬度;ZZ高度,单位m;KK速度,单位km/h)", new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    cell.Colspan = 6;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("第1条", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    for (int i = 0; i < 10; i++)
                    {
                        cell = new PdfPCell(new Phrase(i.ToString(), new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);
                    }

                    ObservableCollection<Tuple<string,string,string,int>> records = cri.Records;
                    for (int i = 0; i < 6; i++)
                    {
                        cell = new PdfPCell(new Phrase(i.ToString(), new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);

                        for (int j = 0; j < 10; j++)
                        {
                            int speed = records[i * 10 + j].Item4;

                            cell = new PdfPCell(new Phrase(records[i * 10 + j].Item1 + "/\n" + records[i * 10 + j].Item2 + "/\n" + 
                            records[i * 10 + j].Item3 + "/\n" + speed.ToString(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                            cell.HorizontalAlignment = Element.ALIGN_CENTER;
                            cell.VerticalAlignment = Element.ALIGN_CENTER;
                            table.AddCell(cell);
                        }
                    }

                    if (index == _cmd15HRespOc.Count)
                        table.SpacingAfter = 60f;

                    _pdfDocument.Add(table);
                }

                #endregion

                //_pdfDocument.Close();
                
                //LogMessageInformation("成功创建报表.");
                ////LogMessageInformation("成功创建报表.点击下面链接打开该报表:");
                ////LogMessageLink(CurrentDirectory + @"\09H.pdf");
                ////LogMessageInformation("或点击下面链接打开该报表所在文件夹:");
                ////LogMessageLink(CurrentDirectory);
            }
            catch (Exception ex)
            {
                LogMessageError("创建报表出错:" + ex.Message);
                _pdfDocument = null;
            }
            Dispatcher.Invoke((ThreadStart)delegate
            {
                pbarMain.IsIndeterminate = false;
            }, null);
            ReadyString2 = "";
            _createPdfEvent.Set();
        }

        private void Create10HReport()
        {
            Dispatcher.Invoke((ThreadStart)delegate
            {
                pbarMain.IsIndeterminate = true;
            }, null);
            ReadyString2 = "创建报表中...";
            try
            {
                //Document document = new Document(PageSize.A4);
                //PdfWriter.GetInstance(document, new FileStream(CurrentDirectory + @"\10H.pdf", FileMode.Create));
                //_pdfDocument.Open();

                #region

                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                PdfParagraph par = new PdfParagraph("--- 事故疑点记录 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                par.Alignment = Element.ALIGN_CENTER;
                _pdfDocument.Add(par);

                par.SpacingBefore = 25f;
                
                PdfParagraph par1 = new PdfParagraph("(" + _cmd10HRespOc.Count.ToString() + "个结果)", new Font(baseFont, 10, Font.NORMAL, BaseColor.BLUE));
                par1.Alignment = Element.ALIGN_CENTER;
                par1.SpacingBefore = 5f;

                if (_cmd10HRespOc.Count < 1)
                    par1.SpacingAfter = 35f;

                _pdfDocument.Add(par1);

                int index = 0;

                foreach (Cmd10HResponse cri in _cmd10HRespOc)
                {
                    PdfPTable table = new PdfPTable(11);

                    if (index == 0)
                        table.SpacingBefore = 25f;
                    else
                        table.SpacingBefore = 15f;
                    index++;

                    table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                    float[] widths = { 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f };
                    table.SetWidths(widths);
                    table.LockedWidth = true;

                    PdfPCell cell;
                    cell = new PdfPCell(new Phrase("停车时间:" + cri.StopDateTime, new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    cell.Colspan = 5;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("注:XX/ YY (XX为速度,单位km/h;YY为状态信号,\"1\"为有效)", new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    cell.Colspan = 6;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("驾驶证号码:" + cri.Number, new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    cell.Colspan = 5;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("位置信息:" + cri.Position + " " + cri.Height + "米", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    cell.Colspan = 6;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("第1条", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    for (int i = 0; i < 10; i++)
                    {
                        cell = new PdfPCell(new Phrase(i.ToString(), new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);
                    }

                    ObservableCollection<Tuple<int, bool>> records = cri.Records;
                    for (int i = 0; i < 10; i++)
                    {
                        cell = new PdfPCell(new Phrase(i.ToString(), new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);

                        for (int j = 0; j < 10; j++)
                        {
                            int speed = records[i * 10 + j].Item1;
                            bool state = records[i * 10 + j].Item2;

                            cell = new PdfPCell(new Phrase(speed.ToString() + "/" + ((state == true) ? "1" : "0"), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                            cell.HorizontalAlignment = Element.ALIGN_CENTER;
                            cell.VerticalAlignment = Element.ALIGN_CENTER;
                            table.AddCell(cell);
                        }
                    }

                    if (index == _cmd15HRespOc.Count)
                        table.SpacingAfter = 60f;

                    _pdfDocument.Add(table);
                }

                #endregion

                //_pdfDocument.Close();

                //LogMessageInformation("成功创建报表.");
                ////LogMessageInformation("成功创建报表.点击下面链接打开该报表:");
                ////LogMessageLink(CurrentDirectory + @"\10H.pdf");
                ////LogMessageInformation("或点击下面链接打开该报表所在文件夹:");
                ////LogMessageLink(CurrentDirectory);
            }
            catch (Exception ex)
            {
                LogMessageError("创建报表出错:" + ex.Message);
                _pdfDocument = null;
            }
            Dispatcher.Invoke((ThreadStart)delegate
            {
                pbarMain.IsIndeterminate = false;
            }, null);
            ReadyString2 = "";
            _createPdfEvent.Set();
        }

        private void Create11HReport()
        {
            Dispatcher.Invoke((ThreadStart)delegate
            {
                pbarMain.IsIndeterminate = true;
            }, null);
            ReadyString2 = "创建报表中...";
            try
            {
                //Document document = new Document(PageSize.A4);
                //PdfWriter.GetInstance(document, new FileStream(CurrentDirectory + @"\11H.pdf", FileMode.Create));
                //_pdfDocument.Open();

                #region

                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                PdfParagraph par = new PdfParagraph("--- 超时驾驶记录 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                par.Alignment = Element.ALIGN_CENTER;
                _pdfDocument.Add(par);

                par.SpacingBefore = 25f;
                
                PdfParagraph par1 = new PdfParagraph("(" + _cmd11HRespOc.Count.ToString() + "个结果)", new Font(baseFont, 10, Font.NORMAL, BaseColor.BLUE));
                par1.Alignment = Element.ALIGN_CENTER;
                par1.SpacingBefore = 5f;

                if (_cmd11HRespOc.Count < 1)
                    par1.SpacingAfter = 35f;

                _pdfDocument.Add(par1);

                int index = 0;

                foreach (Cmd11HResponse cri in _cmd11HRespOc)
                {
                    PdfPTable table = new PdfPTable(5);

                    if (index == 0)
                        table.SpacingBefore = 25f;
                    else
                        table.SpacingBefore = 15f;
                    index++;

                    table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                    float[] widths = { 50f, 100f, 100f, 150f, 150f };
                    table.SetWidths(widths);
                    table.LockedWidth = true;

                    PdfPCell cell;
                    cell = new PdfPCell(new Phrase("驾驶证号码:" + cri.Number, new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    cell.Colspan = 5;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("序号", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("超时开始时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("超时结束时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("超时开始位置", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("超时结束位置", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase(cri.Index, new Font(baseFont, 7)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase(cri.RecordStartDateTime.Replace(" ","\n").Trim(), new Font(baseFont, 7)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase(cri.RecordStopDateTime.Replace(" ", "\n").Trim(), new Font(baseFont, 7)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase(cri.StartPosition + "\n" + cri.StartHeight + "米", new Font(baseFont, 7)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase(cri.StopPosition + "\n" + cri.StartHeight + "米", new Font(baseFont, 7)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);

                    if (index == _cmd15HRespOc.Count)
                        table.SpacingAfter = 60f;

                    _pdfDocument.Add(table);
                }

                #endregion

                //_pdfDocument.Close();

                //LogMessageInformation("成功创建报表.");
                ////LogMessageInformation("成功创建报表.点击下面链接打开该报表:");
                ////LogMessageLink(CurrentDirectory + @"\11H.pdf");
                ////LogMessageInformation("或点击下面链接打开该报表所在文件夹:");
                ////LogMessageLink(CurrentDirectory);
            }
            catch (Exception ex)
            {
                LogMessageError("创建报表出错:" + ex.Message);
                _pdfDocument = null;
            }
            Dispatcher.Invoke((ThreadStart)delegate
            {
                pbarMain.IsIndeterminate = false;
            }, null);
            ReadyString2 = "";
            _createPdfEvent.Set();
        }

        private void Create12HReport()
        {
            Dispatcher.Invoke((ThreadStart)delegate
            {
                pbarMain.IsIndeterminate = true;
            }, null);
            ReadyString2 = "创建报表中...";
            try
            {
                //Document document = new Document(PageSize.A4);
                //PdfWriter.GetInstance(document, new FileStream(CurrentDirectory + @"\12H.pdf", FileMode.Create));
                //_pdfDocument.Open();

                #region

                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                PdfParagraph par = new PdfParagraph("--- 外部供电记录 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                par.Alignment = Element.ALIGN_CENTER;
                _pdfDocument.Add(par);

                par.SpacingBefore = 25f;
                
                PdfParagraph par1 = new PdfParagraph("(" + _cmd12HRespOc.Count.ToString() + "个结果)", new Font(baseFont, 10, Font.NORMAL, BaseColor.BLUE));
                par1.Alignment = Element.ALIGN_CENTER;
                par1.SpacingBefore = 5f;

                if (_cmd12HRespOc.Count < 1)
                    par1.SpacingAfter = 35f;

                _pdfDocument.Add(par1);

                if (_cmd12HRespOc.Count > 0)
                {
                    PdfPTable table = new PdfPTable(4);

                    table.SpacingBefore = 25f;

                    table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                    float[] widths = { 75f, 150f, 150f, 75f };
                    table.SetWidths(widths);
                    table.LockedWidth = true;

                    PdfPCell cell;
                    cell = new PdfPCell(new Phrase("序号", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("事件发生时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("机动车驾驶证号码", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("事件类型", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);

                    foreach (Cmd12HResponse cri in _cmd12HRespOc)
                    {
                        cell = new PdfPCell(new Phrase(cri.Index, new Font(baseFont, 7)));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);
                        cell = new PdfPCell(new Phrase(cri.RecordDateTime, new Font(baseFont, 7)));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);
                        cell = new PdfPCell(new Phrase(cri.Number, new Font(baseFont, 7)));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);
                        cell = new PdfPCell(new Phrase(cri.Description, new Font(baseFont, 7)));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);
                    }

                    table.SpacingAfter = 60f;

                    _pdfDocument.Add(table);
                }

                #endregion

                //_pdfDocument.Close();

                //LogMessageInformation("成功创建报表.");
                ////LogMessageInformation("成功创建报表.点击下面链接打开该报表:");
                ////LogMessageLink(CurrentDirectory + @"\12H.pdf");
                ////LogMessageInformation("或点击下面链接打开该报表所在文件夹:");
                ////LogMessageLink(CurrentDirectory);
            }
            catch (Exception ex)
            {
                LogMessageError("创建报表出错:" + ex.Message);
                _pdfDocument = null;
            }
            Dispatcher.Invoke((ThreadStart)delegate
            {
                pbarMain.IsIndeterminate = false;
            }, null);
            ReadyString2 = "";
            _createPdfEvent.Set();
        }

        private void Create13HReport()
        {
            Dispatcher.Invoke((ThreadStart)delegate
                {
                    pbarMain.IsIndeterminate = true;
                }, null);
            ReadyString2 = "创建报表中...";
            try
            {
                //Document document = new Document(PageSize.A4);
                //PdfWriter.GetInstance(document, new FileStream(CurrentDirectory + @"\13H.pdf", FileMode.Create));
                //_pdfDocument.Open();

                #region

                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                PdfParagraph par = new PdfParagraph("--- 外部供电记录 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                par.Alignment = Element.ALIGN_CENTER;
                _pdfDocument.Add(par);

                par.SpacingBefore = 25f;
                
                PdfParagraph par1 = new PdfParagraph("(" + _cmd13HRespOc.Count.ToString() + "个结果)", new Font(baseFont, 10, Font.NORMAL, BaseColor.BLUE));
                par1.Alignment = Element.ALIGN_CENTER;
                par1.SpacingBefore = 5f;

                if (_cmd13HRespOc.Count < 1)
                    par1.SpacingAfter = 35f;

                _pdfDocument.Add(par1);

                if (_cmd13HRespOc.Count > 0)
                {
                    PdfPTable table = new PdfPTable(3);

                    table.SpacingBefore = 25f;

                    table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                    float[] widths = { 75f, 150f, 150f };
                    table.SetWidths(widths);
                    table.LockedWidth = true;

                    PdfPCell cell;
                    cell = new PdfPCell(new Phrase("序号", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("事件发生时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("事件类型", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);

                    foreach (Cmd13HResponse cri in _cmd13HRespOc)
                    {
                        cell = new PdfPCell(new Phrase(cri.Index, new Font(baseFont, 7)));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);
                        cell = new PdfPCell(new Phrase(cri.RecordDateTime, new Font(baseFont, 7)));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);
                        cell = new PdfPCell(new Phrase(cri.Description, new Font(baseFont, 7)));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);
                    }

                    table.SpacingAfter = 60f;

                    _pdfDocument.Add(table);
                }

                #endregion

                //_pdfDocument.Close();

                //LogMessageInformation("成功创建报表.");
                ////LogMessageInformation("成功创建报表.点击下面链接打开该报表:");
                ////LogMessageLink(CurrentDirectory + @"\13H.pdf");
                ////LogMessageInformation("或点击下面链接打开该报表所在文件夹:");
                ////LogMessageLink(CurrentDirectory);
            }
            catch (Exception ex)
            {
                LogMessageError("创建报表出错:" + ex.Message);
                _pdfDocument = null;
            }
            Dispatcher.Invoke((ThreadStart)delegate
            {
                pbarMain.IsIndeterminate = false;
            }, null);
            ReadyString2 = "";
            _createPdfEvent.Set();
        }

        private void Create14HReport()
        {
            Dispatcher.Invoke((ThreadStart)delegate
            {
                pbarMain.IsIndeterminate = true;
            }, null);
            ReadyString2 = "创建报表中...";
            try
            {
                //Document document = new Document(PageSize.A4);
                //PdfWriter.GetInstance(document, new FileStream(CurrentDirectory + @"\14H.pdf", FileMode.Create));
                //_pdfDocument.Open();

                #region

                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                PdfParagraph par = new PdfParagraph("--- 参数修改记录 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                par.Alignment = Element.ALIGN_CENTER;
                _pdfDocument.Add(par);

                par.SpacingBefore = 25f;
                
                PdfParagraph par1 = new PdfParagraph("(" + _cmd14HRespOc.Count.ToString() + "个结果)", new Font(baseFont, 10, Font.NORMAL, BaseColor.BLUE));
                par1.Alignment = Element.ALIGN_CENTER;
                par1.SpacingBefore = 5f;

                if (_cmd14HRespOc.Count < 1)
                    par1.SpacingAfter = 35f;

                _pdfDocument.Add(par1);

                if (_cmd14HRespOc.Count > 0)
                {
                    PdfPTable table = new PdfPTable(3);

                    table.SpacingBefore = 25f;

                    table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                    float[] widths = { 75f, 150f, 150f };
                    table.SetWidths(widths);
                    table.LockedWidth = true;

                    PdfPCell cell;
                    cell = new PdfPCell(new Phrase("序号", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("事件发生时间", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("事件类型", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);

                    foreach (Cmd14HResponse cri in _cmd14HRespOc)
                    {
                        cell = new PdfPCell(new Phrase(cri.Index, new Font(baseFont, 7)));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);
                        cell = new PdfPCell(new Phrase(cri.RecordDateTime, new Font(baseFont, 7)));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);
                        cell = new PdfPCell(new Phrase(cri.Description, new Font(baseFont, 7)));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);
                    }

                    table.SpacingAfter = 60f;

                    _pdfDocument.Add(table);
                }

                #endregion

                //_pdfDocument.Close();

                //LogMessageInformation("成功创建报表.");
                ////LogMessageInformation("成功创建报表.点击下面链接打开该报表:");
                ////LogMessageLink(CurrentDirectory + @"\04H.pdf");
                ////LogMessageInformation("或点击下面链接打开该报表所在文件夹:");
                ////LogMessageLink(CurrentDirectory);
            }
            catch (Exception ex)
            {
                LogMessageError("创建报表出错:" + ex.Message);
                _pdfDocument = null;
            }
            Dispatcher.Invoke((ThreadStart)delegate
            {
                pbarMain.IsIndeterminate = false;
            }, null);
            ReadyString2 = "";
            _createPdfEvent.Set();
        }

        private void Create15HReport()
        {
            Dispatcher.Invoke((ThreadStart)delegate
            {
                pbarMain.IsIndeterminate = true;
            }, null);
            ReadyString2 = "创建报表中...";
            try
            {
                //Document document = new Document(PageSize.A4);
                //PdfWriter.GetInstance(document, new FileStream(CurrentDirectory + @"\15H.pdf", FileMode.Create));
                //_pdfDocument.Open();

                #region

                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                PdfParagraph par = new PdfParagraph("--- 速度状态日志 --- ", new Font(baseFont, 15, Font.BOLD, BaseColor.BLUE));
                par.Alignment = Element.ALIGN_CENTER;
                _pdfDocument.Add(par);

                par.SpacingBefore = 25f;

                PdfParagraph par1 = new PdfParagraph("(" + _cmd15HRespOc.Count.ToString() + "个结果)", new Font(baseFont, 10, Font.NORMAL, BaseColor.BLUE));
                par1.Alignment = Element.ALIGN_CENTER;
                par1.SpacingBefore = 5f;

                if(_cmd15HRespOc.Count < 1)
                    par1.SpacingAfter = 35f;

                _pdfDocument.Add(par1);

                int index = 0;

                foreach (Cmd15HResponse cri in _cmd15HRespOc)
                {
                    PdfPTable table = new PdfPTable(11);

                    if (index == 0)
                        table.SpacingBefore = 25f;
                    else
                        table.SpacingBefore = 15f;
                    index++;

                    table.TotalWidth = _pdfDocument.Right - _pdfDocument.Left;
                    float[] widths = { 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f, 50f };
                    table.SetWidths(widths);
                    table.LockedWidth = true;

                    PdfPCell cell;
                    string state = "";
                    switch (cri.State)
                    {
                        default:
                            state = "未知 - " + string.Format("{0:X2}",cri.State);
                            break;
                        case 1:
                            state = "正常 - " + string.Format("{0:X2}", cri.State);
                            break;
                        case 2:
                            state = "异常 - " + string.Format("{0:X2}", cri.State);
                            break;
                    }
                    cell = new PdfPCell(new Phrase("速度状态:" + state, new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    cell.Colspan = 5;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("注:XX/YY(XX为记录速度,单位km/h;YY为参考速度,单位km/h)", new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    cell.Colspan = 6;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("判定开始时间:" + cri.StartDateTime, new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    cell.Colspan = 5;
                    table.AddCell(cell);
                    cell = new PdfPCell(new Phrase("判定结束时间:" + cri.StopDateTime, new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    cell.Colspan = 6;
                    table.AddCell(cell);

                    cell = new PdfPCell(new Phrase("第1条", new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.VerticalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell);
                    for (int i = 0; i < 10; i++)
                    {
                        cell = new PdfPCell(new Phrase(i.ToString(), new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);
                    }

                    ObservableCollection<Tuple<int, int>> records = cri.Records;
                    for (int i = 0; i < 6; i++)
                    {
                        cell = new PdfPCell(new Phrase(i.ToString(), new Font(baseFont, 10, Font.BOLD)));//, BaseColor.BLUE)));
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.VerticalAlignment = Element.ALIGN_CENTER;
                        table.AddCell(cell);

                        for (int j = 0; j < 10; j++)
                        {
                            int speed = records[i * 10 + j].Item1;
                            int refSpeed = records[i * 10 + j].Item2;

                            cell = new PdfPCell(new Phrase(speed.ToString() + "/" + refSpeed.ToString(), new Font(baseFont, 7, Font.NORMAL)));//, BaseColor.BLUE)));
                            cell.HorizontalAlignment = Element.ALIGN_CENTER;
                            cell.VerticalAlignment = Element.ALIGN_CENTER;
                            table.AddCell(cell);
                        }
                    }

                    if (index == _cmd15HRespOc.Count)
                        table.SpacingAfter = 60f;

                    _pdfDocument.Add(table);
                }

                #endregion

                //_pdfDocument.Close();

                //LogMessageInformation("成功创建报表.");
                ////LogMessageInformation("成功创建报表.点击下面链接打开该报表:");
                ////LogMessageLink(CurrentDirectory + @"\15H.pdf");
                ////LogMessageInformation("或点击下面链接打开该报表所在文件夹:");
                ////LogMessageLink(CurrentDirectory);

                //System.Diagnostics.Process.Start(CurrentDirectory + @"\08H.pdf");
            }
            catch (Exception ex)
            {
                LogMessageError("创建报表出错:" + ex.Message);
                _pdfDocument = null;
            }
            Dispatcher.Invoke((ThreadStart)delegate
            {
                pbarMain.IsIndeterminate = false;
            }, null);
            ReadyString2 = "";
            _createPdfEvent.Set();
        }

        private void CreateReport()
        {
            try
            {
                _pdfDocument = new Document(PageSize.A4);
                PdfWriter.GetInstance(_pdfDocument, new FileStream(CurrentDirectory + @"\Report_" + _docTitleDateTime +".pdf", FileMode.Create));
                _pdfDocument.Open();

                string fontPath = Environment.GetEnvironmentVariable("WINDIR") + "\\FONTS\\STSONG.TTF";
                BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);

                PdfParagraph par = new PdfParagraph("+++++++++ 信息采集报表 +++++++++ ", new Font(baseFont, 18, Font.BOLD, BaseColor.BLACK));
                par.Alignment = Element.ALIGN_CENTER;
                par.SpacingAfter = 25f;
                _pdfDocument.Add(par);
            }
            catch (Exception ex)
            {
                LogMessageInformation("无法创建报表:" + ex.Message);
                _pdfDocument = null;
            }
        }

        private void CloseReport()
        {
            try
            {
                _pdfDocument.Close();
                _pdfDocument = null;
                bool needOpen = false;
                Dispatcher.Invoke((ThreadStart)delegate
                {
                    if (MessageBox.Show(this, "需要现在打开报表吗?", "打开报表", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        needOpen = true;
                }, null);
                if(needOpen== true)
                    System.Diagnostics.Process.Start(CurrentDirectory + @"\Report_" + _docTitleDateTime + ".pdf");
            }
            catch (Exception ex)
            {
                LogMessageInformation("无法创建报表:" + ex.Message);
                _pdfDocument = null;
            }
        }

        private int GetChineseNumber(string src)
        {
            if (string.IsNullOrWhiteSpace(src))
                return 0;

            string s = "abcdefghijklmnopqrstuvwxyz0123456789";
            int count = 0;

            for (int i = 0; i < src.Length; i++)
            {
                if (s.IndexOf(src.Substring(i, 1)) < 0)
                    count++;
            }

            return count;
        }

        private string BytesToHexString(byte[] bs)
        {
            if (bs == null || bs.Length < 1)
                return "";
            string s = "";
            foreach (byte bi in bs)
            {
                s = s + " " + string.Format("{0:X2}", bi);
            }
            return s.Trim();
        }

        private void ClearReceivingNumber_Button_Click(object sender, RoutedEventArgs e)
        {
            ReceivingByteNumber = 0;
        }

        private void ClearSendingNumber_Button_Click(object sender, RoutedEventArgs e)
        {
            SendingByteNumber = 0;
        }

        private void OpenPdfReportFolder_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            CurrentDirectory = System.Environment.CurrentDirectory;
            if (Directory.Exists(CurrentDirectory + @"\Reports") == false)
            {
                try
                {
                    Directory.CreateDirectory(CurrentDirectory + @"\Reports");
                    CurrentDirectory = CurrentDirectory + @"\Reports";
                }
                catch (Exception)
                {
                    CurrentDirectory = System.Environment.CurrentDirectory;
                }
            }
            else
                CurrentDirectory = CurrentDirectory + @"\Reports";
            System.Diagnostics.Process.Start(CurrentDirectory);
        }
    }

    public abstract class NotifiedClass : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CmdDefinition : NotifiedClass
    {
        private string _cmd = "";
        public string CMD
        {
            get
            {
                return _cmd;
            }
            set
            {
                _cmd = value;
                NotifyPropertyChanged("CMD");
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
            }
        }

        private string _cmdContent = "";
        public string CmdContent
        {
            get
            {
                return _cmdContent;
            }
            set
            {
                _cmdContent = value;
                NotifyPropertyChanged("CmdContent");
            }
        }

        private bool? _cmdSelected = false;
        public bool? CmdSelected 
        {
            get
            {
                return _cmdSelected;
            }
            set
            {
                _cmdSelected = value;
                NotifyPropertyChanged("CmdSelected");
                NotifyPropertyChanged("GetCmdSetParEnabled");
            }
        }

        private Visibility _getCmdSetParVisibility = Visibility.Collapsed;
        public Visibility GetCmdSetParVisibility
        {
            get
            {
                return _getCmdSetParVisibility;
            }
            set
            {
                _getCmdSetParVisibility = value;
                NotifyPropertyChanged("GetCmdSetParVisibility");
            }
        }

        public bool GetCmdSetParEnabled
        {
            get
            {
                if (CmdSelected != true)
                    return false;
                else
                    return true;
            }
        }

        private string _cmdState = "状态提示...";
        public string CmdState
        {
            get
            {
                return _cmdState;
            }
            set
            {
                _cmdState = value;
                if (_cmdState.IndexOf("错误") >= 0 ||
                    _cmdState.IndexOf("出错") >= 0 ||
                    _cmdState.IndexOf("异常") >= 0)
                    CmdStateForeground = Brushes.Red;
                else
                    CmdStateForeground = Brushes.Black;
                NotifyPropertyChanged("CmdState");
            }
        }

        private Brush _cmdStateForeground = Brushes.Black;
        public Brush CmdStateForeground
        {
            get
            {
                return _cmdStateForeground;
            }
            set
            {
                _cmdStateForeground = value;
                NotifyPropertyChanged("CmdStateForeground");
            }
        }

        #region Get Cmd

        private DateTime _startDateTime = DateTime.Now.Subtract(new TimeSpan(0, 0, 1));
        public DateTime StartDateTime
        {
            get
            {
                return _startDateTime;
            }
            set
            {
                _startDateTime = value;
                NotifyPropertyChanged("StartDateTime");
            }
        }

        private DateTime _stopDateTime = DateTime.Now;
        public DateTime StopDateTime
        {
            get
            {
                return _stopDateTime;
            }
            set
            {
                _stopDateTime = value;
                NotifyPropertyChanged("StopDateTime");
            }
        }

        private int _dataCountPerUnitMax = 1;
        public int DataCountPerUnitMax
        {
            get
            {
                return _dataCountPerUnitMax;
            }
            set
            {
                _dataCountPerUnitMax = value;
                NotifyPropertyChanged("DataCountPerUnitMax");
            }
        }

        private int _dataCountPerUnitMin = 1;
        public int DataCountPerUnitMin
        {
            get
            {
                return _dataCountPerUnitMin;
            }
            set
            {
                _dataCountPerUnitMin = value;
                NotifyPropertyChanged("DataCountPerUnitMin");
            }
        }

        private int _dataCountPerUnit = 1;
        public int DataCountPerUnit
        {
            get
            {
                return _dataCountPerUnit;
            }
            set
            {
                _dataCountPerUnit = value;
                NotifyPropertyChanged("DataCountPerUnit");
                NotifyPropertyChanged("DataTotalCount");
            }
        }

        private int _dataSingleCount = 1;
        public int DataSingleCount
        {
            get
            {
                return _dataSingleCount;
            }
            set
            {
                _dataSingleCount = value;
                NotifyPropertyChanged("DataSingleCount");
                NotifyPropertyChanged("DataTotalCount");
                NotifyPropertyChanged("DataTotalCountEnabled");
            }
        }

        public int DataTotalCount
        {
            get
            {
                return DataCountPerUnit * DataSingleCount;
            }
        }

        public bool DataTotalCountEnabled
        {
            get
            {
                return (DataTotalCount <= 1000);
            }
        }

        #endregion

        #region Set Cmd

        private string _vehicleIDCode = "";
        public string VehicleIDCode
        {
            get
            {
                return _vehicleIDCode;
            }
            set
            {
                _vehicleIDCode = value;
                NotifyPropertyChanged("VehicleNumberCode");
            }
        }

        private string _vehicleNumberCode = "";
        public string VehicleNumberCode
        {
            get
            {
                return _vehicleNumberCode;
            }
            set
            {
                _vehicleNumberCode = value;
                NotifyPropertyChanged("VehicleNumberCode");
            }
        }

        private string _vehicleNumberCategory = "";
        public string VehicleNumberCategory
        {
            get
            {
                return _vehicleNumberCategory;
            }
            set
            {
                _vehicleNumberCategory = value;
                NotifyPropertyChanged("VehicleNumberCategory");
            }
        }

        private DateTime _firstInstallDateTime = DateTime.Now;
        public DateTime FirstInstallDateTime
        {
            get
            {
                return _firstInstallDateTime;
            }
            set
            {
                _firstInstallDateTime = value;
                NotifyPropertyChanged("FirstInstallDateTime");
            }
        }

        private string _d2 = "自定义";
        public string D2
        {
            get
            {
                return _d2;
            }
            set
            {
                _d2 = value;
                NotifyPropertyChanged("D2");
            }
        }

        private string _d1 = "自定义";
        public string D1
        {
            get
            {
                return _d1;
            }
            set
            {
                _d1 = value;
                NotifyPropertyChanged("D1");
            }
        }

        private string _d0 = "自定义";
        public string D0
        {
            get
            {
                return _d0;
            }
            set
            {
                _d0 = value;
                NotifyPropertyChanged("D0");
            }
        }

        private bool _isSystemModeDateTime = true;
        public bool IsSystemModeDateTime
        {
            get
            {
                return _isSystemModeDateTime;
            }
            set
            {
                _isSystemModeDateTime = value;
                NotifyPropertyChanged("IsSystemModeDateTime");
            }
        }

        private DateTime _systemModeDateTime = DateTime.Now;
        public DateTime SystemModeDateTime
        {
            get
            {
                return _systemModeDateTime;
            }
            set
            {
                _systemModeDateTime = value;
                NotifyPropertyChanged("SystemModeDateTime");
            }
        }

        private DateTime _userModeDateTime = DateTime.Now;
        public DateTime UserModeDateTime
        {
            get
            {
                return _userModeDateTime;
            }
            set
            {
                _userModeDateTime = value;
                NotifyPropertyChanged("UserModeDateTime");
            }
        }

        private int _pulseCoefficient = 800;
        public int PulseCoefficient
        {
            get
            {
                return _pulseCoefficient;
            }
            set
            {
                _pulseCoefficient = value;
                NotifyPropertyChanged("PulseCoefficient");
            }
        }

        private int _initialDistanceValue = 100;
        public int InitialDistanceValue
        {
            get
            {
                return _initialDistanceValue;
            }
            set
            {
                _initialDistanceValue = value;
                NotifyPropertyChanged("InitialDistanceValue");
            }
        }

        #endregion

        #region Chk Cmd

        public enum ChkCmdMode
        {
            E0H,
            E1H,
            E2H,
            E3H,
            E4H
        }

        private bool _chkCmdEnabled = false;
        public bool ChkCmdEnabled
        {
            get
            {
                return _chkCmdEnabled;
            }
            set
            {
                _chkCmdEnabled = value;
                NotifyPropertyChanged("ChkCmdEnabled");
            }
        }

        private ChkCmdMode _checkMode = ChkCmdMode.E0H;
        public ChkCmdMode CheckMode
        {
            get
            {
                return _checkMode;
            }
            set
            {
                _checkMode = value;
                NotifyPropertyChanged("CheckMode");
            }
        }

        #endregion

        #region Ext Set Cmd

        private int _d7State = 1;
        public int D7State
        {
            get
            {
                return _d7State;
            }
            set
            {
                _d7State = value;
                if (_d7State != 1 && _d7State != 0 && _d7State != 3)
                    _d7State = 3;
                NotifyPropertyChanged("D7State");
            }
        }

        private int _d6State = 1;
        public int D6State
        {
            get
            {
                return _d6State;
            }
            set
            {
                _d6State = value;
                if (_d7State != 1 && _d6State != 0 && _d6State != 3)
                    _d6State = 3;
                NotifyPropertyChanged("D6State");
            }
        }

        private int _d5State = 1;
        public int D5State
        {
            get
            {
                return _d5State;
            }
            set
            {
                _d5State = value;
                if (_d5State != 1 && _d5State != 0 && _d5State != 3)
                    _d5State = 3;
                NotifyPropertyChanged("D5State");
            }
        }

        private int _d4State = 1;
        public int D4State
        {
            get
            {
                return _d4State;
            }
            set
            {
                _d4State = value;
                if (_d4State != 1 && _d4State != 0 && _d4State != 3)
                    _d4State = 3;
                NotifyPropertyChanged("D4State");
            }
        }

        private int _d3State = 1;
        public int D3State
        {
            get
            {
                return _d3State;
            }
            set
            {
                _d3State = value;
                if (_d3State != 1 && _d3State != 0 && _d3State != 3)
                    _d3State = 3;
                NotifyPropertyChanged("D3State");
            }
        }

        private int _d2State = 1;
        public int D2State
        {
            get
            {
                return _d2State;
            }
            set
            {
                _d2State = value;
                if (_d2State != 1 && _d2State != 0 && _d2State != 3)
                    _d2State = 3;
                NotifyPropertyChanged("D2State");
            }
        }

        private int _d1State = 1;
        public int D1State
        {
            get
            {
                return _d1State;
            }
            set
            {
                _d1State = value;
                if (_d1State != 1 && _d1State != 0 && _d1State != 3)
                    _d1State = 3;
                NotifyPropertyChanged("D1State");
            }
        }

        private int _d0State = 1;
        public int D0State
        {
            get
            {
                return _d0State;
            }
            set
            {
                _d0State = value;
                if (_d0State != 1 && _d0State != 0 && _d0State != 3)
                    _d0State = 3;
                NotifyPropertyChanged("D0State");
            }
        }

        private int _singlePulseCnt = 8;
        public int SinglePulseCnt
        {
            get
            {
                return _singlePulseCnt;
            }
            set
            {
                _singlePulseCnt = value;
                if (_singlePulseCnt < 1)
                    _singlePulseCnt = 1;
                if (_singlePulseCnt >255)
                    _singlePulseCnt =255;
                NotifyPropertyChanged("SinglePulseCnt");
            }
        }

        #endregion

        public static byte[] HexStringToBytes(string src)
        {
            if (src == null || src.Length < 1)
                return new byte[] { };

            string[] sa = src.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (sa == null || sa.Length < 1)
                return new byte[] { };

            byte[] ba = new byte[sa.Length];
            for (int i = 0; i < sa.Length; i++)
            {
                try
                {
                    ba[i] = byte.Parse(sa[i], NumberStyles.HexNumber);
                }
                catch (Exception)
                {
                    ba[i] = (byte)0;
                }
            }
            return ba;
        }

        /// <summary>
        /// Format : XX XX XX XX XX or XXXXXXXXXX
        /// Must be Hex and with " " when hasBlank is true
        /// Caller must make sure of each data is of "XX"
        /// </summary>
        /// <param name="src"></param>
        /// <param name="hasBlank">Default is true</param>
        /// <returns></returns>
        public static string XORData(string src, bool hasBlank = true)
        {
            if (src == null || src.Length < 1)
                return "";
            string[] srca = null;
            if (hasBlank == false)
            {
                int slen = src.Length - 2;
                while (slen > 0)
                {
                    src = src.Insert(slen, " ");
                    slen = slen - 2;
                }
            }
            srca = src.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            int len = srca.Length;
            int[] inta = new int[len];
            int value = 0;
            for (int i = 0; i < len; i++)
            {
                int intVal = 0;
                try
                {
                    intVal = int.Parse(srca[i], NumberStyles.HexNumber);
                }
                catch (Exception)
                {
                    intVal = 0;
                }
                if (i == 0)
                    value = intVal;
                else
                    value = value ^ intVal;
            }
            return String.Format("{0:X2}", value);
        }

        public static string CmdBytesToString(byte[] ba, bool addBlank = false)
        {
            if (ba == null || ba.Length < 1)
                return "";

            string s = "";
            foreach (byte bi in ba)
            {
                string si = bi.ToString("X");
                if (si.Length > 2)
                    si = si.Substring(si.Length - 2, 2);
                else if (si.Length == 1)
                    si = "0" + si;
                else if (si.Length < 1)
                    si = "00";
                s = s + ((addBlank == true) ? " " : "") + si;
            }

            return s.Trim();
        }

        public static byte[] PadBytes(byte[] src, int len)
        {
            if (src == null)
                src = new byte[] { };
            if (len <= src.Length)
                return src;
            byte[] dest = new byte[len];
            for (int i = 0; i < len; i++)
            {
                if (i < src.Length)
                    dest[i] = src[i];
                else
                    dest[i] = (byte)0;
            }
            return dest;
        }

        /// <summary>
        /// Caller must make sure of each data is of "XX"
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static string InsertBlank(string src)
        {
            if (src == null || src.Length < 1)
                return "";
            int len = src.Length - 2;
            while (len > 0)
            {
                src = src.Insert(len, " ");
                len = len - 2;
            }
            return src;
        }

        public string[] GetConcreteCmds()
        {
            #region GetCmd
            
            string startStopDT = StartDateTime.ToString("yy MM dd HH mm ss") + " " + StopDateTime.ToString("yy MM dd HH mm ss");
            string dataCntPerUnit = DataCountPerUnit.ToString("X");
            int len = dataCntPerUnit.Length;
            if (len > 8)
                dataCntPerUnit = dataCntPerUnit.Substring(len - 4, 4);
            else
                dataCntPerUnit = dataCntPerUnit.PadLeft(4, '0');
            dataCntPerUnit = dataCntPerUnit.Insert(2, " ");

            #endregion

            string cmd = "";
            string header = CmdContent.Substring(0, 3);
            switch (header.ToUpper())
            {
                default:
                    return new string[] { };
                case "00H":
                    return new string[] { "AA 75 00 00 00 00 DF" };
                case "01H":
                    return new string[] { "AA 75 01 00 00 00 DE" };
                case "02H":
                    return new string[] { "AA 75 02 00 00 00 DD" };
                case "03H":
                    return new string[] { "AA 75 03 00 00 00 DC" };
                case "04H":
                    return new string[] { "AA 75 04 00 00 00 DB" };
                case "05H":
                    return new string[] { "AA 75 05 00 00 00 DA" };
                case "06H":
                    return new string[] { "AA 75 06 00 00 00 D9" };
                case "07H":
                    return new string[] { "AA 75 07 00 00 00 D8" };
                case "08H":
                    cmd = "AA 75 08 00 0E 00 " + startStopDT + " " + dataCntPerUnit;
                    cmd = cmd + " " + XORData(cmd);
                    return new string[] { cmd };
                case "09H":
                    cmd = "AA 75 09 00 0E 00 " + startStopDT + " " + dataCntPerUnit;
                    cmd = cmd + " " + XORData(cmd);
                    return new string[] { cmd };
                case "10H":
                    cmd = "AA 75 10 00 0E 00 " + startStopDT + " " + dataCntPerUnit;
                    cmd = cmd + " " + XORData(cmd);
                    return new string[] { cmd };
                case "11H":
                    cmd = "AA 75 11 00 0E 00 " + startStopDT + " " + dataCntPerUnit;
                    cmd = cmd + " " + XORData(cmd);
                    return new string[] { cmd };
                case "12H":
                    cmd = "AA 75 12 00 0E 00 " + startStopDT + " " + dataCntPerUnit;
                    cmd = cmd + " " + XORData(cmd);
                    return new string[] { cmd };
                case "13H":
                    cmd = "AA 75 13 00 0E 00 " + startStopDT + " " + dataCntPerUnit;
                    cmd = cmd + " " + XORData(cmd);
                    return new string[] { cmd };
                case "14H":
                    cmd = "AA 75 14 00 0E 00 " + startStopDT + " " + dataCntPerUnit;
                    cmd = cmd + " " + XORData(cmd);
                    return new string[] { cmd };
                case "15H":
                    cmd = "AA 75 15 00 0E 00 " + startStopDT + " " + dataCntPerUnit;
                    cmd = cmd + " " + XORData(cmd);
                    return new string[] { cmd };
                case "82H":
                    #region
                    {
                        if (string.IsNullOrWhiteSpace(VehicleIDCode))
                            return new string[] { "", "车辆识别代码为空." };
                        else
                        {
                            Encoding gb = Encoding.GetEncoding("GB2312");
                            byte[] ba1 = gb.GetBytes(VehicleIDCode);
                            if (ba1 == null || ba1.Length != 17)
                                return new string[] { "", "车辆识别代码长度错误." };
                            else
                            {
                                if (string.IsNullOrWhiteSpace(VehicleNumberCode))
                                    return new string[] { "", "车辆号牌号码为空." };
                                else
                                {
                                    byte[] ba2 = gb.GetBytes(VehicleNumberCode);
                                    if (ba2 == null || ba2.Length < 6 || ba2.Length > 9)
                                        return new string[] { "", "车辆号牌号码长度错误." };
                                    else
                                    {
                                        if (string.IsNullOrWhiteSpace(VehicleNumberCategory))
                                            return new string[] { "", "车辆号牌分类为空." };
                                        else
                                        {
                                            byte[] ba3 = gb.GetBytes(VehicleNumberCategory);
                                            if (ba3 == null || ba3.Length < 1 || ba3.Length > 8)
                                                return new string[] { "", "车辆号牌分类长度错误." };
                                            else
                                            {
                                                string finalData = CmdBytesToString(PadBytes(ba1, 17)) + CmdBytesToString(PadBytes(ba2, 12)) + CmdBytesToString(PadBytes(ba2, 12));
                                                string finalDataLen = String.Format("{0:X4}", finalData.Length / 2);
                                                string finalCmd = "AA7582" + finalDataLen + "00" + finalData;
                                                finalCmd = finalCmd + XORData(finalCmd, false);
                                                return new string[] { InsertBlank(finalCmd) };
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                case "83H":
                    #region
                    {
                        string y = FirstInstallDateTime.Year.ToString();
                        y = y.Substring(y.Length - 2, 2);
                        string mo = FirstInstallDateTime.Month.ToString();
                        if (mo.Length == 1)
                            mo = "0" + mo;
                        string d = FirstInstallDateTime.Day.ToString();
                        if (d.Length == 1)
                            d = "0" + d;
                        string h = FirstInstallDateTime.Hour.ToString();
                        if (h.Length == 1)
                            h = "0" + h;
                        string mi = FirstInstallDateTime.Minute.ToString();
                        if (mi.Length == 1)
                            mi = "0" + mi;
                        string s = FirstInstallDateTime.Second.ToString();
                        if (s.Length == 1)
                            s = "0" + s;
                        string finalData = y + mo + d + h + mi + s;
                        string finalDataLen = String.Format("{0:X4}", finalData.Length / 2);
                        string finalCmd = "AA7583" + finalDataLen + "00" + finalData;
                        finalCmd = finalCmd + XORData(finalCmd, false);
                        return new string[] { InsertBlank(finalCmd) };
                    }
                    #endregion
                case "84H":
                    #region
                    {
                        Encoding gb = Encoding.GetEncoding("GB2312");
                        if (string.IsNullOrWhiteSpace(D2))
                            return new string[] { "", "D2为空." };
                        else
                        {
                            byte[] ba2 = gb.GetBytes(D2);
                            if (ba2 == null || ba2.Length < 1 || ba2.Length > 10)
                                return new string[] { "", "D2长度错误:" + D2 + "." };
                            else
                            {
                                if (string.IsNullOrWhiteSpace(D1))
                                    return new string[] { "", "D1为空." };
                                else
                                {
                                    byte[] ba1 = gb.GetBytes(D1);
                                    if (ba1 == null || ba1.Length < 1 || ba1.Length > 10)
                                        return new string[] { "", "D1长度错误:" + D1 + "." };
                                    else
                                    {
                                        if (string.IsNullOrWhiteSpace(D0))
                                            return new string[] { "", "D0为空." };
                                        else
                                        {
                                            byte[] ba0 = gb.GetBytes(D0);
                                            if (ba0 == null || ba0.Length < 1 || ba0.Length > 10)
                                                return new string[] { "", "D0长度错误:" + D0 + "." };
                                            else
                                            {
                                                byte[] da3 = PadBytes(gb.GetBytes("近光"), 10);
                                                byte[] da4 = PadBytes(gb.GetBytes("远光"), 10);
                                                byte[] da5 = PadBytes(gb.GetBytes("右转向"), 10);
                                                byte[] da6 = PadBytes(gb.GetBytes("左转向"), 10);
                                                byte[] da7 = PadBytes(gb.GetBytes("制动"), 10);
                                                string finalData = CmdBytesToString(PadBytes(ba0, 10)) + CmdBytesToString(PadBytes(ba1, 10)) + CmdBytesToString(PadBytes(ba2, 10)) +
                                                    CmdBytesToString(da3) + CmdBytesToString(da4) + CmdBytesToString(da5) + CmdBytesToString(da6) + CmdBytesToString(da7);
                                                string finalDataLen = String.Format("{0:X4}", finalData.Length / 2);
                                                string finalCmd = "AA7584" + finalDataLen + "00" + finalData;
                                                finalCmd = finalCmd + XORData(finalCmd, false);
                                                return new string[] { InsertBlank(finalCmd) };
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                case "C2H":
                    #region
                    {
                        DateTime dt = (IsSystemModeDateTime == true) ? SystemModeDateTime : UserModeDateTime;
                        string y = dt.Year.ToString();
                        y = y.Substring(y.Length - 2, 2);
                        string mo = dt.Month.ToString();
                        if (mo.Length == 1)
                            mo = "0" + mo;
                        string d = dt.Day.ToString();
                        if (d.Length == 1)
                            d = "0" + d;
                        string h = dt.Hour.ToString();
                        if (h.Length == 1)
                            h = "0" + h;
                        string mi = dt.Minute.ToString();
                        if (mi.Length == 1)
                            mi = "0" + mi;
                        string s = dt.Second.ToString();
                        if (s.Length == 1)
                            s = "0" + s;
                        string finalData = y + mo + d + h + mi + s;
                        string finalDataLen = String.Format("{0:X4}", finalData.Length / 2);
                        string finalCmd = "AA75C2" + finalDataLen + "00" + finalData;
                        finalCmd = finalCmd + XORData(finalCmd, false);
                        return new string[] { InsertBlank(finalCmd) };
                    }
                    #endregion
                case "C3H":
                    #region
                    {
                        if (PulseCoefficient < 0 || PulseCoefficient > 65535)
                        {
                            return new string[] { "", "脉冲系数数值大小错误:" + PulseCoefficient.ToString() + "." };
                        }
                        else
                        {
                            DateTime dt = DateTime.Now;
                            string y = dt.Year.ToString();
                            y = y.Substring(y.Length - 2, 2);
                            string mo = dt.Month.ToString();
                            if (mo.Length == 1)
                                mo = "0" + mo;
                            string d = dt.Day.ToString();
                            if (d.Length == 1)
                                d = "0" + d;
                            string h = dt.Hour.ToString();
                            if (h.Length == 1)
                                h = "0" + h;
                            string mi = dt.Minute.ToString();
                            if (mi.Length == 1)
                                mi = "0" + mi;
                            string s = dt.Second.ToString();
                            if (s.Length == 1)
                                s = "0" + s;
                            string finalData = y + mo + d + h + mi + s + PulseCoefficient.ToString("X").PadLeft(4, '0');
                            string finalDataLen = String.Format("{0:X4}", finalData.Length / 2);
                            string finalCmd = "AA75C3" + finalDataLen + "00" + finalData;
                            finalCmd = finalCmd + XORData(finalCmd, false);
                            return new string[] { InsertBlank(finalCmd) };
                        }
                    }
                    #endregion
                case "C4H":
                    #region
                    {
                        if (InitialDistanceValue < 0 || InitialDistanceValue > 99999999)
                        {
                            return new string[] { "", "初始里程数值大小错误:" + PulseCoefficient.ToString() + "." };
                        }
                        else
                        {
                            string finalData = InitialDistanceValue.ToString().PadLeft(8, '0');
                            string finalDataLen = String.Format("{0:X4}", finalData.Length / 2);
                            string finalCmd = "AA75C4" + finalDataLen + "00" + finalData;
                            finalCmd = finalCmd + XORData(finalCmd, false);
                            return new string[] { InsertBlank(finalCmd) };
                        }
                    }
                    #endregion
                case "20H":
                    return new string[] { "AA 75 20 00 00 00 FF" };
                case "21H":
                    return new string[] { "AA 75 21 00 00 00 FE" };
                case "D0H":
                    cmd = "AA 75 D0 00 08 00 " + String.Format("{0:X2}", D0State) + " " +
                        String.Format("{0:X2}", D1State) + " " + String.Format("{0:X2}", D2State) + " " +
                        String.Format("{0:X2}", D3State) + " " + String.Format("{0:X2}", D4State) + " " +
                        String.Format("{0:X2}", D5State) + " " + String.Format("{0:X2}", D6State) + " " +
                        String.Format("{0:X2}", D7State);
                    cmd = cmd + " " + XORData(cmd);
                    return new string[] { cmd };
                case "D1H":
                    cmd = "AA 75 D1 00 01 00 " + String.Format("{0:X2}", SinglePulseCnt);
                    cmd = cmd + " " + XORData(cmd);
                    return new string[] { cmd };
            }
        }
    }

    public class Cmd15HResponse
    {
        public string Index { get; set; }
        public int State { get; set; }
        public string StartDateTime { get; set; }
        public string StopDateTime { get; set; }
        public ObservableCollection<Tuple<int, int>> Records { get; set; }
    }

    public class Cmd14HResponse
    {
        public string Index { get; set; }
        public string RecordDateTime { get; set; }
        public string Description { get; set; }
    }

    public class Cmd13HResponse
    {
        public string Index { get; set; }
        public string RecordDateTime { get; set; }
        public string Description { get; set; }
    }

    public class Cmd12HResponse
    {
        public string Index { get; set; }
        public string RecordDateTime { get; set; }
        public string Number { get; set; }
        public string Description { get; set; }
    }

    public class Cmd11HResponse
    {
        public string Index { get; set; }
        public string Number { get; set; }
        public string RecordStartDateTime { get; set; }
        public string RecordStopDateTime { get; set; }
        public string StartPosition { get; set; }
        public string StopPosition { get; set; }
        public string StartHeight { get; set; }
        public string StopHeight { get; set; }
    }

    public class Cmd10HResponse
    {
        public string Index { get; set; }
        public string Number { get; set; }
        public string StopDateTime {get;set;}
        public ObservableCollection<Tuple<int, bool>> Records { get; set; }
        public string Position { get; set; }
        public string Height { get; set; }
    }

    public class Cmd09HResponse
    {
        public string Index { get; set; }
        public string StartDateTime { get; set; }
        public ObservableCollection<Tuple<string, string, string, int>> Records { get; set; }
    }

    public class Cmd08HResponse
    {
        public string Index { get; set; }
        public string StartDateTime { get; set; }
        public ObservableCollection<Tuple<int, byte>> Records { get; set; }
    }

    public class Bools2BoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter,
        CultureInfo culture)
        {
            bool bRetVal = true;
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] is bool)
                    {
                        bRetVal = bRetVal & (bool)(values[i]);
                    }
                    else
                        bRetVal = bRetVal & false;
                }
                return bRetVal;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
        CultureInfo culture)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }

    public class Bools2BoolOrConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter,
        CultureInfo culture)
        {
            bool bRetVal = false;
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] is bool)
                    {
                        bRetVal = bRetVal | (bool)(values[i]);
                    }
                    else
                        bRetVal = bRetVal | false;
                }
                return bRetVal;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
        CultureInfo culture)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }

    public class Helper
    {
        public static int FindIndex<T>(T[] ta, T ti)
        {
            for (int i = 0; i < ta.Length; i++)
            {
                if (ta[i].Equals(ti))
                    return i;
            }
            return -1;
        }
    }
}
