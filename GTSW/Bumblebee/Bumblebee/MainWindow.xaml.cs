using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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

        #region Variables

        private RunMode _runMode = RunMode.User;
        private bool _normalClose = false;

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
            }

            base.OnClosing(e);

            if(e.Cancel == false)
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
                cd.DataCountPerUnit = gcc.UnitData;
            }
            else
            {
                cd.CmdState = "取消设置.";
            }
        }

        private void ConfigSerialPort_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            SerialPortConfiguration spc = new SerialPortConfiguration();
            spc.ShowDialog();
        }

        private void ConfigTimeout_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            TimeoutConfiguration tc = new TimeoutConfiguration();
            tc.ShowDialog();
        }

        private void ConfigServer_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            ServerConfiguration sc = new ServerConfiguration();
            sc.ShowDialog();
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

        private DateTime _startDateTime = DateTime.Now;
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
}
