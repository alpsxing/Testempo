﻿using System;
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

using Bumblebee.SetCmd;
using Bumblebee.ExtSetCmd;

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

        private Task _serialPosrtTask = null;
        private CancellationTokenSource _cts = null;
        private object _cmdLock = new object();
        private Queue<CmdDefinition> _cdQueue = new Queue<CmdDefinition>();

        private SerialPort _sPort = null;

        private Task _displayLogTask = null;
        private object _logLock = new object();
        private Queue<Tuple<string, LogType>> _logQueue = new Queue<Tuple<string, LogType>>();

        private XmlDocument _xd = new XmlDocument();

        private Task _serialPortTask = null;

        #endregion

        #region Properties

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
                }
                else
                {
                    foreach (CmdDefinition cd in _chkCmdOc)
                    {
                        cd.CmdSelected = false;
                    }
                    CheckCmdState = "状态提示...";
                }
                NotifyPropertyChanged("CheckModeChecked");
                NotifyPropertyChanged("CheckModeSelectEnabled");
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

        private int _pBarMaximum = 100;
        public int PBarMaximum
        {
            get
            {
                return _pBarMaximum;
            }
            set
            {
                _pBarMaximum = value;
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

        private string _port = "";
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

        private string _baud = "";
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

        private string _parity = "";
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

        private string _dataBit = "";
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

        private string _startBit = "";
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

        private string _stopBit = "";
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

        private string _timeout = "";
        public string TimeOut
        {
            get
            {
                return _timeout;
            }
            set
            {
                _timeout = value;
                NotifyPropertyChanged("TimeOut");
            }
        }

        private string _serverip = "";
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

        private string _serverport = "";
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
                NotifyPropertyChanged("CheckCmdState");
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
                    DataCountPerUnit = 7
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
                    DataCountPerUnit = 1
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
                    DataCountPerUnit = 4
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
                    DataCountPerUnit = 19
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
                    DataCountPerUnit = 39
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
                    DataCountPerUnit = 141
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
                    DataCountPerUnit = 141
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
                    DataCountPerUnit = 7
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

            _cts = new CancellationTokenSource();
            _serialPosrtTask = Task.Factory.StartNew(new Action(SerialPortTaskHandler), _cts.Token);
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
                _cts.Cancel();
                try
                {
                    _serialPosrtTask.Wait(10000, _cts.Token);
                }
                catch (Exception) { }
                try
                {
                    _displayLogTask.Wait(1000, _cts.Token);
                }
                catch (Exception) { }
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
            TimeoutConfiguration tc = new TimeoutConfiguration(TimeOut);
            bool? b = tc.ShowDialog();
            if (b == true)
            {
                TimeOut = tc.TimeOut.ToString();
                SaveConfig();
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
                    StateConfigureInformation sci = new StateConfigureInformation(cd.D2, cd.D1, cd.D0);
                    b = sci.ShowDialog();
                    if (b == true)
                    {
                        cd.D2 = sci.D2;
                        cd.D1 = sci.D1;
                        cd.D0 = sci.D0;
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
                    CheckCmdState = "进入检定";
                    break;
                case "E1H : 进入里程误差测量":
                    CheckCmdState = "检定里程";
                    break;
                case "E2H : 进入脉冲系数误差测量":
                    CheckCmdState = "检定脉冲";
                    break;
                case "E3H : 进入实时时间程误差测量":
                    CheckCmdState = "检定时钟";
                    break;
                case "E4H : 返回正常工作状态":
                    CheckCmdState = "检定返回";
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
                    StateConfiguration sc = new StateConfiguration();
                    b = sc.ShowDialog();
                    break;
                case "D1H : 传感器单圈脉冲数":
                    SinglePulseCount spc = new SinglePulseCount();
                    b = spc.ShowDialog();
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
            Process[] ps = Process.GetProcesses();
            foreach (Process pi in ps)
            {
                if (string.Compare(pi.ProcessName, Assembly.GetExecutingAssembly().GetName().Name, true) == 0)
                {
                    MessageBox.Show("\"GB/T 19056-2013数据分析软件\"已经在运行中.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    System.Environment.Exit(0);
                }
            }

            LoadConfig();

            OpenSerialPort();

            _displayLogTask = Task.Factory.StartNew(new Action(DisplayLogHandler), _cts.Token);
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


        private void LogMessageSeperator()
        {
            LogMessage("---------------------------------------------------------------------------------");
            LogMessage("");
        }

        private void LogMessageInformation(string msg)
        {
            LogMessage(msg, LogType.Information);
        }

        private void LogMessageError(string msg)
        {
            LogMessage(msg, LogType.Error);
        }

        private void LogMessage(string msg, LogType lt = LogType.Common)
        {
            //lock (_logLock)
            {
                if (_cts.IsCancellationRequested == true)
                {
                    _logQueue.Clear();
                    return;
                }

                _logQueue.Enqueue(new Tuple<string, LogType>(msg, lt));
            }
        }

        private void DisplayLogHandler()
        {
            while (_cts.IsCancellationRequested == false)
            {
                lock (_logLock)
                {
                    if (_cts.IsCancellationRequested == true)
                    {
                        _logQueue.Clear();
                        return;
                    }

                    if (_logQueue.Count > 0)
                    {
                        Tuple<string, LogType> di = _logQueue.Dequeue();

                        Dispatcher.Invoke((ThreadStart)delegate
                        {
                            #region

                            Run rch = new Run(di.Item1);
                            Paragraph pch = new Paragraph(rch);
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
                            fldocLog.Blocks.Add(pch);
                            rtxtLog.ScrollToEnd();

                            #endregion
                        }, null);
                    }
                }

                Thread.Sleep(10);
            }
        }

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
                    ServerIP = "127.0.0.1";
                    ServerPort = "8678";

                    LogMessageInformation("当前串口端口号:" + Port + ".");
                    LogMessageInformation("当前串口波特率:" + Baud + ".");
                    LogMessageInformation("当前串口校验位:" + Parity + ".");
                    LogMessageInformation("当前串口数据位:" + DataBit + ".");
                    LogMessageInformation("当前串口开始位:" + StartBit + ".");
                    LogMessageInformation("当前串口停止位:" + StopBit + ".");
                    LogMessageInformation("当前串口超时时间:" + TimeOut + "ms.");
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
                ServerIP = "127.0.0.1";
                ServerPort = "8678";

                LogMessageInformation("当前串口端口号:" + Port + ".");
                LogMessageInformation("当前串口波特率:" + Baud + ".");
                LogMessageInformation("当前串口校验位:" + Parity + ".");
                LogMessageInformation("当前串口数据位:" + DataBit + ".");
                LogMessageInformation("当前串口开始位:" + StartBit + ".");
                LogMessageInformation("当前串口停止位:" + StopBit + ".");
                LogMessageInformation("当前串口超时时间:" + TimeOut + "ms.");
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

        #region Serial Port Task

        private void SerialPortTaskHandler()
        {
            try
            {
                while (_cts.IsCancellationRequested == false)
                {
                    lock (_cmdLock)
                    {
                        if (_cdQueue.Count > 0)
                        {
                            CmdDefinition cd = _cdQueue.Dequeue();

                            #region Process Cmd

                            #endregion
                        }
                    }

                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("串口通信错误，请重新启动软件。\n" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PutCmd(CmdDefinition cd)
        {
            while (_cts.IsCancellationRequested == false)
            {
                lock (_cmdLock)
                {
                    if (_cdQueue.Count < 1)
                    {
                        _cdQueue.Enqueue(cd);

                        break;
                    }
                }

                Thread.Sleep(100);
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

        private void Start_Button_Click(object sender, RoutedEventArgs e)
        {
            InRun = true;

            if (AutoClearLog == true)
                fldocLog.Blocks.Clear();
            if (AutoClearRecv == true)
                fldocRecv.Blocks.Clear();
            if (AutoClearSend == true)
                fldocSend.Blocks.Clear();

            _serialPortTask = Task.Factory.StartNew(new Action(SerialPortTaskHander), _cts.Token);
        }

        private void SerialPortTaskHander()
        {
            try
            {
                foreach (CmdDefinition cdi in _getCmdOc)
                {
                    if (cdi.CmdSelected == false)
                        continue;
                }

                foreach (CmdDefinition cdi in _setCmdOc)
                {
                }

                foreach (CmdDefinition cdi in _chkCmdOc)
                {
                }

                foreach (CmdDefinition cdi in _extGetCmdOc)
                {
                }

                foreach (CmdDefinition cdi in _extSetCmdOc)
                {
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                InRun = false;
            }
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
        //public static Dictionary<string, string> _definedCmds = new Dictionary<string, string>();

        //static CmdDefinition()
        //{
        //    #region Get Cmd

        //    _definedCmds.Add("00H", "AA 75 00 00 00 00 DF");
        //    _definedCmds.Add("01H", "AA 75 01 00 00 00 DE");
        //    _definedCmds.Add("02H", "AA 75 02 00 00 00 DD");
        //    _definedCmds.Add("03H", "AA 75 03 00 00 00 DC");
        //    _definedCmds.Add("04H", "AA 75 04 00 00 00 DB");
        //    _definedCmds.Add("05H", "AA 75 05 00 00 00 DA");
        //    _definedCmds.Add("06H", "AA 75 06 00 00 00 D9");
        //    _definedCmds.Add("07H", "AA 75 07 00 00 00 D8");
        //    _definedCmds.Add("08H", "AA 75 08");
        //    _definedCmds.Add("09H", "AA 75 09");
        //    _definedCmds.Add("10H", "AA 75 10");
        //    _definedCmds.Add("11H", "AA 75 11");
        //    _definedCmds.Add("12H", "AA 75 12");
        //    _definedCmds.Add("13H", "AA 75 13");
        //    _definedCmds.Add("14H", "AA 75 14");
        //    _definedCmds.Add("15H", "AA 75 15");

        //    #endregion

        //    #region Set Cmd

        //    _definedCmds.Add("82H", "");
        //    _definedCmds.Add("83H", "");
        //    _definedCmds.Add("84H", "");
        //    _definedCmds.Add("C2H", "");
        //    _definedCmds.Add("C3H", "");
        //    _definedCmds.Add("C4H", "");

        //    #endregion

        //    #region Chk Cmd

        //    _definedCmds.Add("E0H", "");
        //    _definedCmds.Add("E1H", "");
        //    _definedCmds.Add("E2H", "");
        //    _definedCmds.Add("E3H", "");
        //    _definedCmds.Add("E4H", "");

        //    #endregion

        //    #region Ext Get Cmd

        //    _definedCmds.Add("20H", "");
        //    _definedCmds.Add("21H", "");

        //    #endregion

        //    #region Ext Set Cmd

        //    _definedCmds.Add("D0H", "");
        //    _definedCmds.Add("D1H", "");

        //    #endregion
        //}

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
                NotifyPropertyChanged("CmdState");
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

        public string[] GetConcreteCmds()
        {
            string header = CmdContent.Substring(0, 3);
            switch (header.ToUpper())
            {
                default:
                    return null;
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
                    return new string[] { "AA 75 08 00 00 00 D8" };
                case "09H":
                    return new string[] { "AA 75 09 00 00 00 D8" };
                case "10H":
                    return new string[] { "AA 75 10 00 00 00 D8" };
                case "11H":
                    return new string[] { "AA 75 11 00 00 00 D8" };
                case "12H":
                    return new string[] { "AA 75 12 00 00 00 D8" };
                case "13H":
                    return new string[] { "AA 75 13 00 00 00 D8" };
                case "14H":
                    return new string[] { "AA 75 14 00 00 00 D8" };
                case "15H":
                    return new string[] { "AA 75 15 00 00 00 D8" };
            }
        }
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
            bool bRetVal = true;
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
