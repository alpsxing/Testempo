using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;

using InformationTransferLibrary;

namespace SystemService
{
    public partial class SystemService : ServiceBase
    {
        private ObservableCollection<UserInfo> _userInfoOc = new ObservableCollection<UserInfo>();
        private ObservableCollection<DTUInfo> _dtuInfoOc = new ObservableCollection<DTUInfo>();

        private Dictionary<UserInfo, List<CommunicationMessage>> _commMsgDict = new Dictionary<UserInfo, List<CommunicationMessage>>();

        private CancellationTokenSource _cts = new CancellationTokenSource();

        // Local management
        private Task _manageTask = null;
        private Dictionary<Socket, Task> _manageTaskDict = new Dictionary<Socket, Task>();
        //private object _managementLock = new object();
        private int _manPort = Consts.MAN_PORT;
        private int _manTimeout = Consts.MAN_TIMEOUT;

        // Control terminal

        private Task _termTask = null;
        private Dictionary<Socket, TerminalDTUTaskInformation> _termTaskList = new Dictionary<Socket, TerminalDTUTaskInformation>();
        //private object _termLock = new object();
        private int _termPort = Consts.TERM_PORT;
        private int _termTimeout = Consts.TERM_TIMEOUT;

        // DTU

        private Task _dtuTask = null;
        private Dictionary<Socket, TerminalDTUTaskInformation> _dtuTaskList = new Dictionary<Socket, TerminalDTUTaskInformation>();
        private int _dtuPort = Consts.DTU_PORT;
        private int _dtuTimeout = Consts.DTU_TIMEOUT;

        /// <summary>
        /// Key is to terminal, Value is to DTU
        /// </summary>
        private Dictionary<Socket, Socket> _terminalDTUMap = new Dictionary<Socket, Socket>();

        //private object _userLock = new object();
        //private object _dtuLock = new object();
        //private object _socketMapLock = new object();
        private object _taskLock = new object();
        private object _logLock = new object();

        private ObservableCollection<Tuple<string, string, string>> _logFileOc = new ObservableCollection<Tuple<string, string, string>>();

        public SystemService()
        {
            string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            if (Directory.Exists(folder + @"\COMWAY") == false)
                Directory.CreateDirectory(folder + @"\COMWAY");
            folder = folder + @"\COMWAY";
            if (Directory.Exists(folder + @"\Service") == false)
                Directory.CreateDirectory(folder + @"\Service");
            if (Directory.Exists(folder + @"\Service\config") == false)
                Directory.CreateDirectory(folder + @"\Service\config");
            if (Directory.Exists(folder + @"\Service\log") == false)
                Directory.CreateDirectory(folder + @"\Service\log");
            if (Directory.Exists(folder + @"\Service\message") == false)
                Directory.CreateDirectory(folder + @"\Service\message");

            InitializeComponent();

            InitService();

            InitLogFileOC();
        }

        private void InitLogFileOC()
        {
            _logFileOc.Clear();

            string msgFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments) + @"\COMWAY\Service\message";
            try
            {
                string[] fs = Directory.GetFiles(msgFolder);
                foreach (string f in fs)
                {
                    int index = f.LastIndexOf(@"\");
                    if (f.EndsWith(".log") == true)
                    {
                        string ftrim = f.Substring(index + 1, f.Length - index - 1 - 4);
                        if (Helper.FindStringCount(ftrim, " ") != 3)
                        {
                            try
                            {
                                File.Delete(f);
                                eventLogInformationTransfer.WriteEntry("删除" + f, EventLogEntryType.Warning);
                            }
                            catch (Exception ex)
                            {
                                eventLogInformationTransfer.WriteEntry("删除" + f + "出现错误 : " + ex.Message, EventLogEntryType.Error);
                            }
                        }
                        else
                        {
                            string[] sa = ftrim.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                            if (sa.Length != 4)
                            {
                                try
                                {
                                    File.Delete(f);
                                    eventLogInformationTransfer.WriteEntry("删除" + f, EventLogEntryType.Warning);
                                }
                                catch (Exception ex)
                                {
                                    eventLogInformationTransfer.WriteEntry("删除" + f + "出现错误 : " + ex.Message, EventLogEntryType.Error);
                                }
                            }
                            else
                            {
                                DateTime dt;
                                string dtstr = Helper.ConvertDateTime(sa[1], Helper.DateTimeType.Date) + " " + Helper.ConvertDateTime(sa[2], Helper.DateTimeType.Time);
                                if (DateTime.TryParse(dtstr, out dt) == true)
                                {
                                    DateTime dtNow = DateTime.Now;
                                    if (dtNow.Subtract(dt).Days > Consts.MAX_DTU_MESSAGE_LOG_DATE)
                                    {
                                        try
                                        {
                                            File.Delete(f);
                                            eventLogInformationTransfer.WriteEntry("删除" + f, EventLogEntryType.Warning);
                                        }
                                        catch (Exception ex)
                                        {
                                            eventLogInformationTransfer.WriteEntry("删除" + f + "出现错误 : " + ex.Message, EventLogEntryType.Error);
                                        }
                                    }
                                    else
                                        _logFileOc.Add(new Tuple<string, string, string>(sa[0], sa[1] + " " + sa[2], sa[3]));
                                }
                                else
                                {
                                    try
                                    {
                                        File.Delete(f);
                                        eventLogInformationTransfer.WriteEntry("删除" + f, EventLogEntryType.Warning);
                                    }
                                    catch (Exception ex)
                                    {
                                        eventLogInformationTransfer.WriteEntry("删除" + f + "出现错误 : " + ex.Message, EventLogEntryType.Error);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            File.Delete(f);
                        }
                        catch (Exception) { }
                    }
                }
            }
            catch (Exception) { }
        }

        private void InitService()
        {
            base.AutoLog = true;
            base.CanShutdown = true;
            base.CanStop = true;
            base.CanPauseAndContinue = false;
            base.ServiceName = "DTUManagement";

            if (!System.Diagnostics.EventLog.SourceExists("DTUManagement"))
                System.Diagnostics.EventLog.CreateEventSource("DTUManagement", "DTUManagementLog");
            eventLogInformationTransfer.Source = "DTUManagement";
            eventLogInformationTransfer.Log = "DTUManagementLog";
        }

        protected override void OnStart(string[] args)
        {
            LoadConfig();
            LoadUser();
            LoadDTU();

            try
            {
                StartSocketTask();

                eventLogInformationTransfer.WriteEntry("成功启动服务.");
            }
            catch (Exception ex)
            {
                eventLogInformationTransfer.WriteEntry("无法启动服务 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
            }
        }

        protected override void OnStop()
        {
            try
            {
                StopSocketTask();

                eventLogInformationTransfer.WriteEntry("成功停止服务.");
            }
            catch (Exception ex)
            {
                eventLogInformationTransfer.WriteEntry("停止服务出现错误 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
            }

            SaveConfig();
            SaveUser();
            SaveDTU();
        }

        #region Save & Load

        private void LoadConfig()
        {
            string line = null;
            int i = 0;
            StreamReader sr = null;
            try
            {
                string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
                sr = new StreamReader(folder + @"\COMWAY\Service\config\ssportto.cfg");
                while (true)
                {
                    line = sr.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        break;
                    if (i == 0)
                    {
                        int iv = Consts.TERM_PORT;
                        if (int.TryParse(EncryptDecrypt.Decrypt(line.Trim()), out iv) == false)
                            iv = Consts.TERM_PORT;
                        if (iv < Consts.MIN_PORT_NUMBER)
                            iv = Consts.TERM_PORT;
                        _termPort = iv;
                    }
                    else if (i == 1)
                    {
                        int iv = Consts.TERM_TIMEOUT;
                        if (int.TryParse(EncryptDecrypt.Decrypt(line.Trim()), out iv) == false)
                            iv = Consts.TERM_TIMEOUT;
                        if (iv < Consts.MIN_TIME_OUT)
                            iv = Consts.TERM_TIMEOUT;
                        _termTimeout = iv;
                    }
                    else if (i == 2)
                    {
                        int iv = Consts.DTU_PORT;
                        if (int.TryParse(EncryptDecrypt.Decrypt(line.Trim()), out iv) == false)
                            iv = Consts.DTU_PORT;
                        if (iv < Consts.MIN_PORT_NUMBER)
                            iv = Consts.DTU_PORT;
                        _dtuPort = iv;
                    }
                    else if (i == 3)
                    {
                        int iv = Consts.DTU_TIMEOUT;
                        if (int.TryParse(EncryptDecrypt.Decrypt(line.Trim()), out iv) == false)
                            iv = Consts.DTU_TIMEOUT;
                        if (iv < Consts.MIN_TIME_OUT)
                            iv = Consts.DTU_TIMEOUT;
                        _dtuTimeout = iv;
                    }
                    else if (i == 4)
                    {
                        int iv = Consts.MAN_PORT;
                        if (int.TryParse(EncryptDecrypt.Decrypt(line.Trim()), out iv) == false)
                            iv = Consts.MAN_PORT;
                        else if (iv < Consts.MIN_PORT_NUMBER)
                            iv = Consts.MAN_PORT;
                        _manPort = iv;
                    }
                    else if (i == 5)
                    {
                        int iv = Consts.MAN_TIMEOUT;
                        if (int.TryParse(EncryptDecrypt.Decrypt(line.Trim()), out iv) == false)
                            iv = Consts.MAN_TIMEOUT;
                        else if (iv < Consts.MIN_TIME_OUT)
                            iv = Consts.MAN_TIMEOUT;
                        _manTimeout = iv;
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
                if (i <= 0)
                {
                    _termPort = Consts.TERM_PORT;
                    if (i <= 1)
                    {
                        _termTimeout = Consts.TERM_TIMEOUT;
                        if (i <= 2)
                        {
                            _dtuPort = Consts.DTU_PORT;
                            if (i <= 3)
                            {
                                _dtuTimeout = Consts.DTU_TIMEOUT;
                                if (i <= 4)
                                {
                                    _manPort = Consts.MAN_PORT;
                                    if (i <= 5)
                                        _manTimeout = Consts.MAN_TIMEOUT;
                                }
                            }
                        }
                    }
                }

                Helper.SafeCloseIOStream(sr);

                eventLogInformationTransfer.WriteEntry("装载配置文件出现错误 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Warning);
            }
        }

        private void SaveConfig()
        {
            StreamWriter sw = null;
            try
            {
                string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
                sw = new StreamWriter(folder + @"\COMWAY\Service\config\ssportto.cfg");
                sw.WriteLine(EncryptDecrypt.Encrypt(_termPort.ToString()));
                sw.WriteLine(EncryptDecrypt.Encrypt(_termTimeout.ToString()));
                sw.WriteLine(EncryptDecrypt.Encrypt(_dtuPort.ToString()));
                sw.WriteLine(EncryptDecrypt.Encrypt(_dtuTimeout.ToString()));
                sw.WriteLine(EncryptDecrypt.Encrypt(_manPort.ToString()));
                sw.WriteLine(EncryptDecrypt.Encrypt(_manTimeout.ToString()));
                sw.Close();
                sw.Dispose();
            }
            catch (Exception ex)
            {
                Helper.SafeCloseIOStream(sw);

                eventLogInformationTransfer.WriteEntry("保存配置文件出现错误 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Warning);
            }
        }

        private void LoadUser()
        {
            _userInfoOc.Clear();
            StreamReader sr = null;
            try
            {
                string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
                sr = new StreamReader(folder + @"\COMWAY\Service\config\ituser.dat");
                string line = null;
                while (true)
                {
                    line = sr.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        break;
                    string[] sa = line.Split(new string[] { "\t" }, StringSplitOptions.None);
                    if (sa == null || sa.Length != Consts.USER_ACCOUNT_ITEM_COUNT)
                    {
                        eventLogInformationTransfer.WriteEntry("用户信息错误 : " + line, EventLogEntryType.Warning);
                        continue;
                    }
                    string user = sa[0].Trim();
                    string pw = sa[1].Trim();
                    string pm = sa[2].Trim();
                    string ruser = EncryptDecrypt.Decrypt(user);
                    string rpw = EncryptDecrypt.Decrypt(pw);
                    string rpm = EncryptDecrypt.Decrypt(pm);
                    if (rpm != "1" && rpm != "2")
                    {
                        eventLogInformationTransfer.WriteEntry("用户权限错误 : " + ruser, EventLogEntryType.Warning);
                        rpm = "2";
                    }
                    UserInfo ui = Helper.FindUserInfo(ruser, _userInfoOc);
                    if (ui != null)
                        eventLogInformationTransfer.WriteEntry("重复用户 : " + ruser, EventLogEntryType.Warning);
                    else
                    {
                        UserInfo uiNew = new UserInfo()
                        {
                            DisplayIcon = false,
                            UserName = ruser,
                            Password = rpw,
                            Permission = rpm
                        };
                        if (rpm == "2")
                            uiNew.OnlineOfflineEvent +=new UserInfo.OnlineOfflineEventHandler(UserInfo_OnlineOfflineEvent);
                        _userInfoOc.Add(uiNew);
                    }
                }
                sr.Close();
                sr.Dispose();
            }
            catch (Exception ex)
            {
                Helper.SafeCloseIOStream(sr);

                eventLogInformationTransfer.WriteEntry("装载用户信息出现错误 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Warning);
            }
            lock (_taskLock)
            {
                UserInfo ui = Helper.FindUserInfo(Consts.LOGIN_ADMIN_USERNAME, _userInfoOc);
                if (ui != null)
                    _userInfoOc.Remove(ui);
                _userInfoOc.Add(new UserInfo()
                {
                    DisplayIcon = false,
                    UserName = Consts.LOGIN_ADMIN_USERNAME,
                    Password = Consts.LOGIN_ADMIN_PASSWORD,
                    Permission = Consts.LOGIN_ADMIN_PERMISSION
                });
            }
        }

        /// <summary>
        /// Lock should be implemented in caller.
        /// </summary>
        private void SaveUser()
        {
            StreamWriter sw = null;
            try
            {
                string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
                sw = new StreamWriter(folder + @"\COMWAY\Service\config\ituser.dat");
                foreach (UserInfo uii in _userInfoOc)
                {
                    string user = uii.UserName;
                    if (string.Compare(user, Consts.LOGIN_ADMIN_USERNAME, true) == 0)
                        continue;
                    string pw = uii.Password;
                    string pm = uii.Permission;
                    if (pm != "1" && pm != "2")
                        pm = "2";
                    string ruser = EncryptDecrypt.Encrypt(user);
                    string rpw = EncryptDecrypt.Encrypt(pw);
                    string rpm = EncryptDecrypt.Encrypt(pm);
                    sw.WriteLine(ruser + "\t" + rpw + "\t" + rpm);
                }
                sw.Close();
                sw.Dispose();
            }
            catch (Exception ex)
            {
                Helper.SafeCloseIOStream(sw);

                eventLogInformationTransfer.WriteEntry("保存用户信息出现错误 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Warning);
            }
        }

        private void LoadDTU()
        {
            _dtuInfoOc.Clear();
            StreamReader sr = null;
            try
            {
                string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
                sr = new StreamReader(folder + @"\COMWAY\Service\config\itdtu.dat");
                string line = null;
                while (true)
                {
                    line = sr.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        break;
                    string[] sa = line.Split(new string[] { "\t" }, StringSplitOptions.None);
                    if (sa == null || sa.Length != Consts.DTU_ACCOUNT_ITEM_COUNT)
                    {
                        eventLogInformationTransfer.WriteEntry("DTU信息错误 : " + line, EventLogEntryType.Warning);
                        continue;
                    }
                    string dtuId = sa[0].Trim();
                    string simId = sa[1].Trim();
                    string userName = sa[2].Trim();
                    string userTel = sa[3].Trim();
                    string rdtuId = EncryptDecrypt.Decrypt(dtuId);
                    string rsimId = EncryptDecrypt.Decrypt(simId);
                    string ruserName = EncryptDecrypt.Decrypt(userName);
                    string ruserTel = EncryptDecrypt.Decrypt(userTel);
                    DTUInfo di = Helper.FindDTUInfo(rdtuId, _dtuInfoOc);
                    if (di != null)
                        eventLogInformationTransfer.WriteEntry("重复DTU : " + rdtuId, EventLogEntryType.Warning);
                    else
                    {
                        _dtuInfoOc.Add(new DTUInfo()
                        {
                            DisplayIcon = false,
                            DtuId = rdtuId,
                            SimId = rsimId,
                            UserName = ruserName,
                            UserTel = ruserTel
                        });
                    }
                }
                sr.Close();
                sr.Dispose();
            }
            catch (Exception ex)
            {
                Helper.SafeCloseIOStream(sr);

                eventLogInformationTransfer.WriteEntry("装载DTU出现错误 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Warning);
            }
        }
        
        /// <summary>
        /// Lock should be implemented in caller.
        /// </summary>
        private void SaveDTU()
        {
            StreamWriter sw = null;
            try
            {
                string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
                sw = new StreamWriter(folder + @"\COMWAY\Service\config\itdtu.dat");
                foreach (DTUInfo dii in _dtuInfoOc)
                {
                    string dtuId = dii.DtuId;
                    string simId = dii.SimId;
                    string userName = dii.UserName;
                    string userTel = dii.UserTel;
                    string rdtuId = EncryptDecrypt.Encrypt(dtuId);
                    string rsimId = EncryptDecrypt.Encrypt(simId);
                    string ruserName = EncryptDecrypt.Encrypt(userName);
                    string ruserTel = EncryptDecrypt.Encrypt(userTel);
                    sw.WriteLine(rdtuId + "\t" + rsimId + "\t" + ruserName + "\t" + ruserTel);
                }
                sw.Close();
                sw.Dispose();
            }
            catch (Exception ex)
            {
                Helper.SafeCloseIOStream(sw);

                eventLogInformationTransfer.WriteEntry("保存DTU信息出现错误 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Warning);
            }
        }

        #endregion

        #region Start

        private void StartSocketTask()
        {
            #region Management Task

            _manageTask = Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        StartManageSocket();
                    }
                    catch (Exception ex)
                    {
                        eventLogInformationTransfer.WriteEntry("开始管理服务出现错误 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                        _cts.Cancel();
                        System.Environment.Exit(1);
                    }
                }, _cts.Token
            );

            #endregion

            #region Term Task

            _termTask = Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        StartTermSocket();
                    }
                    catch (Exception ex)
                    {
                        eventLogInformationTransfer.WriteEntry("开始终端服务出现错误 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                        _cts.Cancel();
                        System.Environment.Exit(1);
                    }
                }, _cts.Token
            );

            #endregion

            #region DTU Task

            _dtuTask = Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        StartDTUService();
                    }
                    catch (Exception ex)
                    {
                        eventLogInformationTransfer.WriteEntry("开始DTU服务出现错误 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                        _cts.Cancel();
                        System.Environment.Exit(1);
                    }
                }, _cts.Token);

            #endregion
        }

        #region Management

        private void StartManageSocket()
        {
            IPAddress local = IPAddress.Any;
            IPEndPoint iep = new IPEndPoint(local, _manPort);
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(iep);
            server.Listen(Consts.SOCKET_LISTEN_BACKLOG_COUNT);
            eventLogInformationTransfer.WriteEntry("管理服务开始侦听...");
            while (_cts.Token.IsCancellationRequested == false)
            {
                Socket soc = server.Accept();
                soc.ReceiveTimeout = _manTimeout;
                soc.SendTimeout = _manTimeout;
                string ip = ((IPEndPoint)soc.RemoteEndPoint).Address.ToString();
                eventLogInformationTransfer.WriteEntry("获得一个管理终端连接" + ip);

                Task t = new Task(
                    () =>
                    {
                        ManagementProcessService(soc);
                    }, _cts.Token
                );
                lock (_taskLock)
                {
                    _manageTaskDict.Add(soc, t);
                }
                t.Start();
                eventLogInformationTransfer.WriteEntry("开始管理任务" + t.Id.ToString());
            }
            eventLogInformationTransfer.WriteEntry("管理服务停止侦听.");
        }

        /// <summary>
        /// If the response is null, service won't send any response to the management terminal.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="ip"></param>
        public void ManagementProcessService(Socket soc)
        {
            string ip = ((IPEndPoint)soc.RemoteEndPoint).Address.ToString();
            string data = null;
            byte[] bytes = new byte[Consts.SOCKET_RECEIVING_BUFFER_LENGTH];
            int len = 0;
            try
            {
                while (_cts.Token.IsCancellationRequested == false && 
                    soc != null && soc.Connected == true)
                {
                    len = soc.Receive(bytes);
                    if (len < 1)
                    {
                        eventLogInformationTransfer.WriteEntry("失去与管理终端" + ip + "的连接.", EventLogEntryType.Warning);
                        break;
                    }
                    else
                    {
                        data = Encoding.UTF8.GetString(bytes, 0, len);
                        // Management response MUST NOT have unnecessary blank characters at the begin or the end.
                        string resp = ProcessManagementData(soc, bytes, len);//.Trim();
                        soc.Send(Encoding.UTF8.GetBytes(resp));
                    }
                }
            }
            catch (Exception ex)
            {
                eventLogInformationTransfer.WriteEntry("与管理终端" + ip + "的连接出现错误 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
            }
            lock (_taskLock)
            {
                if (_manageTaskDict.ContainsKey(soc))
                    _manageTaskDict.Remove(soc);

                UserInfo ui = Helper.FindUserInfo(soc, _userInfoOc);
                if (ui != null && ui.Online == true)
                    ui.Online = false;
            
                Helper.SafeCloseSocket(soc);
            }
            eventLogInformationTransfer.WriteEntry("管理任务" + Task.CurrentId.ToString() + "停止.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private string ProcessManagementData(Socket soc, byte[] bytes, int length)
        {
            Tuple<string, byte[], string, string> data = Helper.ExtractSocketResponse(bytes, length);
            if (data == null)
                return Consts.MAN_INVALID_REQUEST;

            switch (data.Item1)
            {
                default:
                    return Consts.MAN_INVALID_REQUEST + data;
                case Consts.MAN_TEST_CONN:
                    return Consts.MAN_TEST_CONN_OK;
                case Consts.MAN_GET_ALL_DTU:
                    return GetAllDTU();
                case Consts.MAN_ADD_DTU:
                    return AddDTU(soc, data.Item3);
                case Consts.MAN_DELETE_DTU:
                    return DeleteDTU(soc, data.Item3);
                case Consts.MAN_MODIFY_DTU:
                    return ModifyDTU(data.Item3);
                case Consts.MAN_GET_ALL_USER:
                    return GetAllUser();
                case Consts.MAN_ADD_USER:
                    return AddUser(data.Item3);
                case Consts.MAN_MODIFY_USER:
                    return ModifyUser(data.Item3);
                case Consts.MAN_DELETE_USER:
                    return DeleteUser(data.Item3);
                case Consts.MAN_LOGIN:
                    return CheckLogin(soc, data.Item3);
                case Consts.MAN_LOGOUT:
                    return CheckLogout(soc, data.Item3);
                case Consts.MAN_KICK_USER:
                    return KickOffUser(data.Item3);
                case Consts.MAN_KICK_DTU:
                    return KickOffDTU(data.Item3);
                case Consts.MAN_UNCTRL_DTU:
                    return ReleaseDTU(data.Item3);
                case Consts.MAN_DEL_LOG_USER_DATE:
                    return DeleteLogUserDate(UserDateType.UserDate, data.Item3);
                case Consts.MAN_DEL_LOG_USER:
                    return DeleteLogUserDate(UserDateType.User, data.Item3);
                case Consts.MAN_DEL_LOG_DATE:
                    return DeleteLogUserDate(UserDateType.Date, data.Item3);
                case Consts.MAN_GET_LOG_INFO:
                    return GetLogUserDateInfo();
            }
        }

        #endregion

        #region Process Management Data Callbacks

        private string GetLogUserDateInfo()
        {
            string s = "";
            lock (_taskLock)
            {
                List<string> userList = new List<string>();
                foreach (Tuple<string, string, string> ti in _logFileOc)
                {
                    bool find = false;
                    foreach (string si in userList)
                    {
                        if (string.Compare(si.Trim(), ti.Item1.Trim(), true) == 0)
                        {
                            find = true;
                            break;
                        }
                    }
                    if (find == false)
                        userList.Add(ti.Item1.Trim() + " " + ti.Item2.Trim() + " " + ti.Item3.Trim());
                }
                foreach (string si in userList)
                {
                    if (s == "")
                        s = si;
                    else
                        s = s + "\t" + si;
                }
            }
            return Consts.MAN_GET_LOG_INFO_OK + s;
        }

        public enum UserDateType
        {
            UserDate,
            User,
            Date
        }

        private string DeleteLogUserDate(UserDateType udt = UserDateType.UserDate, string info = "")
        {
            if (string.IsNullOrWhiteSpace(info) == true)
            {
                switch (udt)
                {
                    default:
                    case UserDateType.UserDate:
                        return Consts.MAN_DEL_LOG_USER_DATE_ERR + "空的用户和日期";
                    case UserDateType.User:
                        return Consts.MAN_DEL_LOG_USER_DATE_ERR + "空的用户";
                    case UserDateType.Date:
                        return Consts.MAN_DEL_LOG_USER_DATE_ERR + "空的日期";
                }
            }

            string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments) + @"\COMWAY\Service\message";
            ObservableCollection<Tuple<string, string, string>> toc = new ObservableCollection<Tuple<string, string, string>>();

            lock (_taskLock)
            {
                switch (udt)
                {
                    default:
                    case UserDateType.UserDate:
                        {
                            string[] sa = info.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                            if (sa.Length != 2)
                                return Consts.MAN_DEL_LOG_USER_DATE_ERR + "空的用户或日期";
                            DateTime dt;
                            if (DateTime.TryParse(Helper.ConvertDateTime(sa[1]), out dt) == false)
                                return Consts.MAN_DEL_LOG_USER_DATE_ERR + "无效的日期";
                            dt = dt.AddDays(1.0);
                            foreach (Tuple<string, string, string> ti in _logFileOc)
                            {
                                DateTime dti;
                                if (DateTime.TryParse(Helper.ConvertDateTime(ti.Item2), out dti) == false)
                                    continue;
                                if (DateTime.Compare(dti, dt) <= 0 &&
                                    (string.Compare(sa[0].Trim(), ti.Item1.Trim(), true) == 0 || 
                                    string.Compare(sa[0].Trim(), "all", true) == 0))
                                    toc.Add(ti);
                            }
                            foreach (Tuple<string, string, string> ti in toc)
                            {
                                _logFileOc.Remove(ti);
                                try
                                {
                                    File.Delete(folder + @"\" + ti.Item1 + " " + ti.Item2 + " " + ti.Item3 + ".log");
                                }
                                catch (Exception) { }
                            }
                            InitLogFileOC();
                            return Consts.MAN_DEL_LOG_USER_DATE_OK;
                        }
                    case UserDateType.User:
                        {
                            if (string.Compare(info, "all", true) == 0)
                            {
                                foreach (Tuple<string, string, string> ti in _logFileOc)
                                {
                                    try
                                    {
                                        File.Delete(folder + @"\" + ti.Item1 + " " + ti.Item2 + " " + ti.Item3 + ".log");
                                    }
                                    catch (Exception) { }
                                }

                                _logFileOc.Clear();
                            }
                            else
                            {
                                foreach (Tuple<string, string, string> ti in _logFileOc)
                                {
                                    if (string.Compare(info.Trim(), ti.Item1.Trim(), true) == 0)
                                        toc.Add(ti);
                                }
                                foreach (Tuple<string, string, string> ti in toc)
                                {
                                    _logFileOc.Remove(ti);
                                    try
                                    {
                                        File.Delete(folder + @"\" + ti.Item1 + " " + ti.Item2 + " " + ti.Item3 + ".log");
                                    }
                                    catch (Exception) { }
                                }
                            }
                            InitLogFileOC();
                            return Consts.MAN_DEL_LOG_USER_OK;
                        }
                    case UserDateType.Date:
                        {
                            DateTime dt;
                            if (DateTime.TryParse(Helper.ConvertDateTime(info), out dt) == false)
                                return Consts.MAN_DEL_LOG_USER_DATE_ERR + "无效的日期";
                            foreach (Tuple<string, string, string> ti in _logFileOc)
                            {
                                DateTime dti;
                                if (DateTime.TryParse(Helper.ConvertDateTime(ti.Item2), out dti) == false)
                                    continue;
                                if (DateTime.Compare(dti, dt) <= 0)
                                    toc.Add(ti);
                            }
                            foreach (Tuple<string, string, string> ti in toc)
                            {
                                _logFileOc.Remove(ti);
                                try
                                {
                                    File.Delete(folder + @"\" + ti.Item1 + " " + ti.Item2 + " " + ti.Item3 + ".log");
                                }
                                catch (Exception) { }
                            } 
                            InitLogFileOC();
                            return Consts.MAN_DEL_LOG_DATE_OK;
                        }
                }
            }
        }

        private string AddUser(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Consts.MAN_ADD_USER_ERR + "无用户信息.";
            content = content.Trim();

            string[] sa = content.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
            if (sa == null || sa.Length != Consts.USER_ACCOUNT_ITEM_COUNT)
                return Consts.MAN_ADD_USER_ERR + "非法用户信息 :" + content;
            string userName = sa[0].Trim();
            string password = sa[1].Trim();
            string permission = sa[2].Trim();
            if (Helper.CheckValidChar(userName, Helper.CheckMethod.CharNum) == false)
                return Consts.MAN_ADD_USER_ERR + "无效用户名 : " + userName;
            if (Helper.CheckValidChar(password, Helper.CheckMethod.CharNum) == false)
                return Consts.MAN_ADD_USER_ERR + "无效密码 :" + password;
            if (permission != "1" && permission != "2")
                return Consts.MAN_ADD_USER_ERR + "无效权限 : " + permission;
            lock (_taskLock)
            {
                UserInfo ui = Helper.FindUserInfo(userName, _userInfoOc);
                if (ui != null)
                    return Consts.MAN_ADD_USER_ERR + "重复用户 : " + userName;
                UserInfo uiNew = new UserInfo()
                {
                    DisplayIcon = false,
                    UserName = userName,
                    Password = password,
                    Permission = permission
                };
                if(permission == "2")
                    uiNew.OnlineOfflineEvent += new UserInfo.OnlineOfflineEventHandler(UserInfo_OnlineOfflineEvent);
                _userInfoOc.Add(uiNew);
                SaveUser();
            }
            return Consts.MAN_ADD_USER_OK;
        }

        private string ModifyUser(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Consts.MAN_MODIFY_USER_ERR + "无用户信息.";
            content = content.Trim();

            string[] sa = content.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
            if (sa == null || sa.Length != Consts.USER_ACCOUNT_ITEM_COUNT - 1)
                return Consts.MAN_MODIFY_USER_ERR + "无效用户信息 :" + content;
            string userName = sa[0].Trim();
            string password = sa[1].Trim();
            if (Helper.CheckValidChar(password, Helper.CheckMethod.CharNum) == false)
                return Consts.MAN_MODIFY_USER_ERR + "无效密码 :" + password;
            lock (_taskLock)
            {
                UserInfo ui = Helper.FindUserInfo(userName, _userInfoOc);
                if (ui == null)
                    return Consts.MAN_MODIFY_USER_ERR + "无此用户 : " + userName;
                ui.Password = password;
                SaveUser();
            }
            return Consts.MAN_MODIFY_USER_OK;
        }

        private string DeleteUser(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return Consts.MAN_DELETE_USER_ERR + "无需要删除的用户名.";
            userName = userName.Trim();

            lock (_taskLock)
            {
                UserInfo ui = Helper.FindUserInfo(userName, _userInfoOc);
                if (ui == null)
                    return Consts.MAN_DELETE_USER_ERR + "用户(" + userName + ")不存在.";
                if (ui.Online == true)
                    return Consts.MAN_DELETE_USER_ERR + "用户(" + userName + ")在线中.";
                if (ui.Permission == "2")
                    ui.OnlineOfflineEvent -= new UserInfo.OnlineOfflineEventHandler(UserInfo_OnlineOfflineEvent);
                UserInfo_OnlineOfflineEvent(ui, new CommonEventArgs() { CommonObject = ui });
                _userInfoOc.Remove(ui);
                SaveUser();

                foreach (TerminalDTUTaskInformation tdti in ui.UserTermSockets)
                {
                    if (_terminalDTUMap.ContainsKey(tdti.TaskSocket) == true)
                    {
                        if (_dtuTaskList.ContainsKey(_terminalDTUMap[tdti.TaskSocket]))
                            _dtuTaskList[_terminalDTUMap[tdti.TaskSocket]].Controller = null;
                        _terminalDTUMap.Remove(tdti.TaskSocket);
                    }
                    tdti.CTS.Cancel();
                    tdti.RequestQueue.Clear();
                    if (_termTaskList.ContainsKey(tdti.TaskSocket) == true)
                        _termTaskList.Remove(tdti.TaskSocket);
                    Helper.SafeCloseSocket(tdti.TaskSocket);
                }
                ui.UserTermSockets.Clear();

                foreach (DTUInfo di in ui.UserDTUs)
                {
                    di.Controller = null;
                    if (_dtuTaskList.ContainsKey(di.DTUSocket))
                        _dtuTaskList[di.DTUSocket].Controller = null;
                }
                ui.UserDTUs.Clear();

                Helper.SafeCloseSocket(ui.UserSocket);
            }

            return Consts.MAN_DELETE_USER_OK;
        }

        private string GetAllUser()
        {
            string s = "";

            lock (_taskLock)
            {
                foreach (UserInfo uii in _userInfoOc)
                {
                    string si = uii.UserName + "\t" +
                        uii.Permission + "\t" +
                        ((uii.Online) ? "Y" : "N") + "\t" +
                        uii.DtLogString + "\t" +
                        uii.Information;
                    if (s == "")
                        s = si;
                    else
                        s = s + "\n" + si;
                }
            }

            return Consts.MAN_GET_ALL_USER_OK + s;
        }

        private string AddDTU(Socket soc, string content)
        {
            // Must not do Trim() because there are empty(optional) fields.
            if (string.IsNullOrEmpty(content))
                return Consts.MAN_ADD_DTU_ERR + "无DTU信息.";
            //content = content.Trim();
            string[] sa = content.Split(new string[] { "\t" }, StringSplitOptions.None);
            if (sa == null || sa.Length != Consts.DTU_ACCOUNT_ITEM_COUNT)
                return Consts.MAN_ADD_DTU_ERR + "非完整的DTU信息 :" + content;
            string dtuId = sa[0].Trim();
            string simId = sa[1].Trim();
            string un = sa[2].Trim();
            string utel = sa[3].Trim();
            lock (_taskLock)
            {
                DTUInfo di = Helper.FindDTUInfo(sa[0].Trim(), _dtuInfoOc);
                if (di != null)
                    return Consts.MAN_ADD_DTU_ERR + "重复的DTU : " + dtuId;
                _dtuInfoOc.Add(new DTUInfo()
                {
                    DisplayIcon = false,
                    DtuId = dtuId,
                    SimId = simId,
                    UserName = un,
                    UserTel = utel
                });
                SaveDTU();
            }
            return Consts.MAN_ADD_DTU_OK;
        }

        private string DeleteDTU(Socket soc, string dtuId)
        {
            if (string.IsNullOrEmpty(dtuId))
                return Consts.MAN_ADD_DTU_ERR + "无DTU ID.";
            dtuId = dtuId.Trim();

            lock (_taskLock)
            {
                DTUInfo dtu = Helper.FindDTUInfo(dtuId, _dtuInfoOc);
                if (dtu == null)
                    return Consts.MAN_DELETE_DTU_ERR + "DTU(" + dtuId + ")不存在.";
                _dtuInfoOc.Remove(dtu);
                SaveDTU();

                if (dtu.DTUSocket != null && _dtuTaskList.ContainsKey(dtu.DTUSocket))
                {
                    _dtuTaskList[dtu.DTUSocket].CTS.Cancel();
                    _dtuTaskList[dtu.DTUSocket].RequestQueue.Clear();
                    _dtuTaskList.Remove(dtu.DTUSocket);
                }

                UserInfo ui = dtu.Controller;
                if (ui != null)
                {
                    if (ui.UserDTUs.Contains(dtu) == true)
                        ui.UserDTUs.Remove(dtu);
                }

                Socket socTerm = null;
                foreach (KeyValuePair<Socket, Socket> kvp in _terminalDTUMap)
                {
                    if (kvp.Value == dtu.DTUSocket)
                    {
                        socTerm = kvp.Key;
                        break;
                    }
                }
                if (socTerm != null)
                    _terminalDTUMap.Remove(socTerm);

                Helper.SafeCloseSocket(dtu.DTUSocket);
            }

            return Consts.MAN_DELETE_DTU_OK;
        }

        private string ModifyDTU(string content)
        {
            // Must not do Trim() because there are empty(optional) fields.
            if (string.IsNullOrEmpty(content))
                return Consts.MAN_MODIFY_DTU_ERR + "无DTU信息.";
            //content = content.Trim();
            string[] sa = content.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
            if (sa == null || sa.Length != Consts.DTU_ACCOUNT_ITEM_COUNT)
                return Consts.MAN_MODIFY_DTU_ERR + "非完整的DTU信息 :" + content;
            string dtuId = sa[0].Trim();
            string simId = sa[1].Trim();
            string un = sa[2].Trim();
            string utel = sa[3].Trim();
            lock (_taskLock)
            {
                DTUInfo di = Helper.FindDTUInfo(dtuId, _dtuInfoOc);
                if (di == null)
                    return Consts.MAN_MODIFY_DTU_ERR + "DTU ID(" + dtuId + ")不存在.";
                di.SimId = simId;
                di.UserName = un;
                di.UserTel = utel;
                SaveDTU();
            }
            return Consts.MAN_MODIFY_DTU_OK;
        }

        private string GetAllDTU()
        {
            return Consts.MAN_GET_ALL_DTU_OK + GetAllDTUInfo();
        }

        private string GetAllDTUInfo()
        {
            string s = "";

            lock (_taskLock)
            {
                foreach (DTUInfo dii in _dtuInfoOc)
                {
                    string si = dii.DtuId + "\t" +
                        dii.SimId + "\t" +
                        dii.UserName + "\t" +
                        dii.UserTel + "\t" +
                        ((dii.Online) ? "Y" : "N") + "\t" +
                        dii.DtLogString + "\t" +
                        dii.ControllerName;
                    if (s == "")
                        s = si;
                    else
                        s = s + "\n" + si;
                }
            }

            return s;
        }

        private string CheckLogin(Socket soc, string s, bool isMan = true)
        {
            string headerOk = (isMan == true) ? Consts.MAN_LOGIN_OK : Consts.TERM_LOGIN_OK;
            string headerErr = (isMan == true) ? Consts.MAN_LOGIN_ERR : Consts.TERM_LOGIN_ERR;
            string ip = ((IPEndPoint)soc.RemoteEndPoint).Address.ToString();
            if (string.IsNullOrWhiteSpace(s))
                return headerErr + "无用户名和密码.";
            string[] sa = s.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
            if (sa == null || sa.Length != Consts.USER_ACCOUNT_ITEM_COUNT - 1)
                return headerErr + "无用户名或密码.";

            lock (_taskLock)
            {
                UserInfo ui = Helper.FindUserInfo(sa[0].Trim(), _userInfoOc);
                if (ui == null)
                    return headerErr + "无效用户名.";
                if (string.Compare(sa[1].Trim(), ui.Password, true) != 0)
                    return headerErr + "无效密码.";

                if (isMan == true)
                {
                    if (ui.Permission != "0" && ui.Permission != "1")
                        return headerErr + "普通用户不允许登录服务管理系统.";
                }
                else
                {
                    if (ui.Permission != "2")
                        return headerErr + "只有普通用户才可以登录DTU管理系统.";
                }

                if (ui.Online == true)
                    return headerErr + "此用户已经在" + ui.DtLogString + "时刻从" + ui.Information + "在线";

                ui.Online = true;
                ui.DtLog = DateTime.Now;
                ui.Information = ip;
                ui.UserSocket = soc;

                return headerOk + ui.Permission;
            }
        }

        private string CheckLogout(Socket soc, string userName, bool isMan = true)
        {
            string headerOk = (isMan == true) ? Consts.MAN_LOGOUT_OK : Consts.TERM_LOGOUT_OK;
            string headerErr = (isMan == true) ? Consts.MAN_LOGOUT_ERR : Consts.TERM_LOGOUT_ERR;

            lock (_taskLock)
            {
                UserInfo ui = Helper.FindUserInfo(userName, _userInfoOc);
                if (ui == null)
                    return headerErr + "无效用户名.";

                if (isMan == false)
                {
                    foreach (TerminalDTUTaskInformation tdti in ui.UserTermSockets)
                    {
                        if (_terminalDTUMap.ContainsKey(tdti.TaskSocket) == true)
                        {
                            if (_dtuTaskList.ContainsKey(_terminalDTUMap[tdti.TaskSocket]))
                                _dtuTaskList[_terminalDTUMap[tdti.TaskSocket]].Controller = null;
                            _terminalDTUMap.Remove(tdti.TaskSocket);
                        }
                        tdti.CTS.Cancel();
                        tdti.RequestQueue.Clear();
                        if (_termTaskList.ContainsKey(tdti.TaskSocket) == true)
                            _termTaskList.Remove(tdti.TaskSocket);
                        Helper.SafeCloseSocket(tdti.TaskSocket);
                    }
                    ui.UserTermSockets.Clear();
                }

                foreach (DTUInfo di in ui.UserDTUs)
                {
                    di.Controller = null;
                }
                ui.UserDTUs.Clear();

                //Debug.Assert(ui.UserSocket == soc);

                Helper.SafeCloseSocket(soc);
                ui.UserSocket = null;

                ui.Online = false;
            }

            return headerOk;
        }

        private string KickOffUser(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return Consts.MAN_KICK_USER_ERR + "空的用户名.";
            userName = userName.Trim();

            lock (_taskLock)
            {
                UserInfo ui = Helper.FindUserInfo(userName, _userInfoOc);
                if (ui == null)
                    return Consts.MAN_KICK_USER_ERR + "无效用户名.";

                ui.Online = false;

                foreach (TerminalDTUTaskInformation tdti in ui.UserTermSockets)
                {
                    if (_terminalDTUMap.ContainsKey(tdti.TaskSocket) == true)
                    {
                        if (_dtuTaskList.ContainsKey(_terminalDTUMap[tdti.TaskSocket]))
                            _dtuTaskList[_terminalDTUMap[tdti.TaskSocket]].Controller = null;
                        _terminalDTUMap.Remove(tdti.TaskSocket);
                    }
                    tdti.CTS.Cancel();
                    tdti.RequestQueue.Clear();
                    if (_termTaskList.ContainsKey(tdti.TaskSocket) == true)
                        _termTaskList.Remove(tdti.TaskSocket);
                    Helper.SafeCloseSocket(tdti.TaskSocket);
                }
                ui.UserTermSockets.Clear();

                foreach (DTUInfo dii in ui.UserDTUs)
                {
                    dii.Controller = null;
                }
                ui.UserDTUs.Clear();
                
                Helper.SafeCloseSocket(ui.UserSocket);
                ui.UserSocket = null;
            }

            return Consts.MAN_KICK_USER_OK;
        }

        private string KickOffDTU(string dtuId)
        {
            if (string.IsNullOrEmpty(dtuId))
                return Consts.MAN_KICK_DTU_ERR + "无DTU.";
            dtuId = dtuId.Trim();

            lock (_taskLock)
            {
                DTUInfo dtu = Helper.FindDTUInfo(dtuId, _dtuInfoOc);
                if (dtu == null)
                    return Consts.MAN_KICK_DTU_ERR + "DTU(" + dtuId + ")不存在.";
                if (dtu.Online == false)
                    return Consts.MAN_KICK_DTU_ERR + "DTU(" + dtuId + ")已经不在线.";

                if (dtu.DTUSocket != null && _dtuTaskList.ContainsKey(dtu.DTUSocket))
                {
                    _dtuTaskList[dtu.DTUSocket].CTS.Cancel();
                    _dtuTaskList[dtu.DTUSocket].RequestQueue.Clear();
                    _dtuTaskList.Remove(dtu.DTUSocket);
                }

                UserInfo ui = dtu.Controller;
                if (ui != null)
                {
                    if (ui.UserDTUs.Contains(dtu) == true)
                        ui.UserDTUs.Remove(dtu);
                }

                Socket socTerm = null;
                foreach (KeyValuePair<Socket, Socket> kvp in _terminalDTUMap)
                {
                    if (kvp.Value == dtu.DTUSocket)
                    {
                        socTerm = kvp.Key;
                        break;
                    }
                }
                if (socTerm != null)
                    _terminalDTUMap.Remove(socTerm);

                dtu.Controller = null;
                dtu.Online = false;
  
                Helper.SafeCloseSocket(dtu.DTUSocket);
            }

            return Consts.MAN_KICK_DTU_OK;
        }

        private string ReleaseDTU(string dtuId)
        {
            if (string.IsNullOrEmpty(dtuId))
                return Consts.MAN_UNCTRL_DTU_ERR + "无DTU名.";
            //content = content.Trim();
            string[] sa = dtuId.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
            if (sa == null || sa.Length != 2)
                return Consts.MAN_UNCTRL_DTU_ERR + "无用户名或DTU ID.";

            lock (_taskLock)
            {
                DTUInfo di = null;
                di = Helper.FindDTUInfo(sa[1].Trim(), _dtuInfoOc);
                if (di == null)
                    return Consts.MAN_UNCTRL_DTU_ERR + "DTU(" + dtuId + ")不存在.";
                if (di.Online == false)
                    return Consts.MAN_UNCTRL_DTU_ERR + "DTU(" + dtuId + ")已经不在线.";
                if (di.Controller == null)
                    return Consts.MAN_UNCTRL_DTU_ERR + "DTU(" + dtuId + ")已经不再被控制.";

                UserInfo ui = di.Controller;
                if (ui != null)
                {
                    if (string.Compare("admin", sa[0].Trim(), true) != 0)
                        return Consts.MAN_UNCTRL_DTU_ERR + "只有admin可以进行释放DTU的操作";// Debug.Assert(string.Compare(ui.UserName, sa[0].Trim(), true) == 0);
                    if (ui.UserDTUs.Contains(di) == true)
                        ui.UserDTUs.Remove(di);
                }

                Socket socTerm = null;
                foreach (KeyValuePair<Socket, Socket> kvp in _terminalDTUMap)
                {
                    if (kvp.Value == di.DTUSocket)
                    {
                        socTerm = kvp.Key;
                        break;
                    }
                }
                if (socTerm != null)
                    _terminalDTUMap.Remove(socTerm);

                di.Controller = null;
                if (di.DTUSocket != null && _dtuTaskList.ContainsKey(di.DTUSocket) == true)
                    _dtuTaskList[di.DTUSocket].Controller = null;
            }

            return Consts.MAN_UNCTRL_DTU_OK;
        }

        #endregion

        #region Terminal

        private void StartTermSocket()
        {
            IPAddress local = IPAddress.Any;
            IPEndPoint iep = new IPEndPoint(local, _termPort);
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(iep);
            server.Listen(Consts.SOCKET_LISTEN_BACKLOG_COUNT);
            eventLogInformationTransfer.WriteEntry("终端服务开始侦听...");
            while (_cts.Token.IsCancellationRequested == false)
            {
                Socket soc = server.Accept();
                soc.ReceiveTimeout =  _termTimeout;
                soc.SendTimeout = _termTimeout;
                string ip = ((IPEndPoint)soc.RemoteEndPoint).Address.ToString();
                eventLogInformationTransfer.WriteEntry("获得一个终端请求" + ip);

                CancellationTokenSource cts = new CancellationTokenSource();
                Task ts = new Task(
                    () =>
                    {
                        TermSendService(soc);
                    }, cts.Token
                );
                Task tr = new Task(
                    () =>
                    {
                        TermReceiveService(soc);
                    }, cts.Token
                );
                lock (_taskLock)
                {
                    _termTaskList.Add(soc, new TerminalDTUTaskInformation ()
                    {
                        TaskSocket = soc,
                        CTS = cts,
                        SendTask = ts,
                        ReceiveTask = tr
                    });
                }
                ts.Start();
                tr.Start();
                eventLogInformationTransfer.WriteEntry("开始终端发送任务" + ts.Id.ToString() + "和接收任务" + tr.Id.ToString());
            }
            eventLogInformationTransfer.WriteEntry("终端服务停止侦听.");
        }

        public void TermSendService(Socket soc)
        {
            string ip = ((IPEndPoint)soc.RemoteEndPoint).Address.ToString();
            Queue<byte[]> q = _termTaskList[soc].RequestQueue;
            CancellationTokenSource cts = _termTaskList[soc].CTS;

            while (_cts.Token.IsCancellationRequested == false &&
                cts.Token.IsCancellationRequested == false &&
                soc != null && soc.Connected == true)
            {
                try
                {
                    if (q.Count > 0)
                    {
                        byte[] ba = q.Dequeue();
                        soc.Send(ba);
                    }
                    else
                        Thread.Sleep(Consts.TASK_THREAD_SLEEP_INTERVAL);
                }
                catch (Exception ex)
                {
                    eventLogInformationTransfer.WriteEntry("终端" + ip + "发送任务" + Task.CurrentId.ToString() + "出现错误 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error, Consts.EVENT_ID_TO_TERM);
                    break;
                }
            }

            TerminateTermSocket(soc);
        }

        public void TermReceiveService(Socket soc)
        {
            string ip = ((IPEndPoint)soc.RemoteEndPoint).Address.ToString();
            byte[] ba = new byte[Consts.SOCKET_RECEIVING_BUFFER_LENGTH];
            int len = 0;
            CancellationTokenSource cts = _termTaskList[soc].CTS;

            while (_cts.Token.IsCancellationRequested == false &&
                cts.Token.IsCancellationRequested == false && 
                soc != null && soc.Connected == true)
            {
                try
                {
                    len = soc.Receive(ba);
                    if (len < 1)
                    {
                        eventLogInformationTransfer.WriteEntry("失去到终端" + ip + "的连接.", EventLogEntryType.Warning, Consts.EVENT_ID_FROM_DTU);
                        break;
                    }
                    else
                    {
                        bool dtuMsg = false;
                        lock(_taskLock)
                        {
                            foreach (KeyValuePair<Socket, Socket> kvp in _terminalDTUMap)
                            {
                                if (kvp.Key == soc)
                                {
                                    if (kvp.Value == null)
                                    {
                                        eventLogInformationTransfer.WriteEntry("终端" + ip + "没有分配任何DTU", EventLogEntryType.Error, Consts.EVENT_ID_FROM_TERM);
                                        break;
                                    }
                                    byte[] badtu = new byte[len];
                                    for (int i = 0; i < len; i++)
                                    {
                                        badtu[i] = ba[i];
                                    }
                                    TerminalDTUTaskInformation tdti = _dtuTaskList[kvp.Value];
                                    tdti.RequestQueue.Enqueue(badtu);
                                    dtuMsg = true;

                                    #region Log Term Message

                                    UserInfo ui = tdti.Controller;
                                    DTUInfo di = tdti.CurrentDTU;
                                    if (ui != null && di != null)
                                    {
                                        if (_commMsgDict.ContainsKey(ui))
                                        {
                                            List<CommunicationMessage> cmList = _commMsgDict[ui];
                                            string sar = Encoding.UTF8.GetString(badtu);
                                            sar = sar.Replace("\r", @"\r");
                                            sar = sar.Replace("\n", @"\n");
                                            sar = sar.Replace("\t", @"\t");
                                            sar = sar.Replace("\0", @"\0");
                                            cmList.Add(new CommunicationMessage()
                                            {
                                                UserName = ui.UserName,
                                                DTUID = di.DtuId,
                                                IsToDTU = true,
                                                TimeStamp = DateTime.Now,
                                                Message = sar
                                            });
                                        }
                                    }

                                    #endregion
                                    
                                    break;
                                }
                            }
                        }
                        if(dtuMsg == false)
                            ProcessTermData(soc, ba, len);
                    }
                }
                catch (Exception ex)
                {
                    eventLogInformationTransfer.WriteEntry("终端(" + ip + ")的接收任务(" + Task.CurrentId.ToString() + ")出错 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error, Consts.EVENT_ID_FROM_DTU);
                    break;
                }
            }

            TerminateTermSocket(soc);
        }

        private void TerminateTermSocket(Socket soc)
        {
            TerminalDTUTaskInformation tdti = null;
            UserInfo ui = null;

            lock (_taskLock)
            {
                ui = Helper.FindUserInfo(soc, _userInfoOc);
                if (ui == null)
                {
                    // Terminal socket for a user

                    if (_termTaskList.ContainsKey(soc) == true)
                    {
                        tdti = _termTaskList[soc];
                        ui = _termTaskList[soc].Controller;

                        _termTaskList[soc].CTS.Cancel();
                        _termTaskList[soc].RequestQueue.Clear();
                        _termTaskList.Remove(soc);
                    }

                    if (ui != null && tdti != null && ui.UserTermSockets.Contains(tdti) == true)
                        ui.UserTermSockets.Remove(tdti);

                    if(tdti != null && _terminalDTUMap.ContainsKey(tdti.TaskSocket) == true)
                        _terminalDTUMap.Remove(tdti.TaskSocket);

                    if (tdti != null)
                        tdti.Controller = null;
                }
                else
                {
                    // Pulse socket for a user

                    if (_termTaskList.ContainsKey(soc) == true)
                    {
                        tdti = _termTaskList[soc];

                        tdti.CTS.Cancel();
                        tdti.RequestQueue.Clear();
                        _termTaskList.Remove(soc);
                    }

                    foreach (TerminalDTUTaskInformation tdtii in ui.UserTermSockets)
                    {
                        if (_terminalDTUMap.ContainsKey(tdti.TaskSocket) == true)
                            _terminalDTUMap.Remove(tdti.TaskSocket);
                        tdtii.CTS.Cancel();
                        tdtii.RequestQueue.Clear();
                        Helper.SafeCloseSocket(tdtii.TaskSocket);
                    }
                    ui.UserTermSockets.Clear();

                    foreach (DTUInfo dtui in ui.UserDTUs)
                    {
                        dtui.Controller = null;
                        if(_dtuTaskList.ContainsKey(dtui.DTUSocket) == true)
                            _dtuTaskList[dtui.DTUSocket].Controller = null;
                    }
                    ui.UserDTUs.Clear();

                    ui.Online = false;
                }

                //Debug.Assert(tdti.TaskSocket == soc);

                Helper.SafeCloseSocket(soc);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="ba"></param>
        /// <param name="len"></param>
        private void ProcessTermData(Socket soc, byte[] ba, int len)
        {
            Tuple<string, byte[], string, string> data = Helper.ExtractSocketResponse(ba, len);
            if (data == null)
            {
                byte[] br = Helper.ComposeResponseBytes(Consts.TERM_INVALID_REQUEST, ba);
                PutTermDTUResponse(soc, br, br.Length);
                return;
            }

            byte[] bar = null;
            switch (data.Item1)
            {
                default:
                    bar = Helper.ComposeResponseBytes(Consts.TERM_INVALID_REQUEST, ba);
                    break;
                case Consts.MAN_UNCTRL_DTU:
                    bar = Encoding.UTF8.GetBytes(ReleaseDTU(data.Item3));
                    break;
                case Consts.TERM_TEST_CONN:
                    bar = Helper.ComposeResponseBytes(Consts.TERM_TEST_CONN_OK, "");
                    break;
                case Consts.TERM_PULSE_REQ:
                    bar = Helper.ComposeResponseBytes(Consts.TERM_PULSE_REQ_OK, "");
                    break;
                case Consts.TERM_GET_ALL_DTU:
                    bar = Helper.ComposeResponseBytes(Consts.TERM_GET_ALL_DTU_OK, GetAllDTUInfo());
                    break;
                case Consts.TERM_ADD_DTU:
                    bar = Encoding.UTF8.GetBytes(TermAddDtu(soc, data.Item3));
                    break;
                case Consts.TERM_LOGIN:
                    bar = Encoding.UTF8.GetBytes(CheckLogin(soc, data.Item3, false));
                    break;
                case Consts.TERM_LOGOUT:
                    bar = Encoding.UTF8.GetBytes(CheckLogout(soc, data.Item3, false));
                    break;
                case Consts.TERM_INIT_USER:
                    bar = Encoding.UTF8.GetBytes(TermInitUser(soc, data.Item3));
                    break;
                case Consts.TERM_CHECK_DTU:
                    bar = Encoding.UTF8.GetBytes(TermCheckDtu(soc, data.Item3));
                    break;
            }
            PutTermDTUResponse(soc, bar, bar.Length);
        }

        private void PutTermDTUResponse(Socket s, string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                PutTermDTUResponse(s, new byte[] { }, 0);
            else
            {
                byte[] ba = Encoding.UTF8.GetBytes(str);
                PutTermDTUResponse(s, ba, ba.Length);
            }
        }

        #endregion

        #region Process Terminal Data Callbacks

        private string TermAddDtu(Socket soc, string userAndDtuId)
        {
            if (string.IsNullOrWhiteSpace(userAndDtuId))
                return Consts.TERM_ADD_DTU_ERR + "无用户名和DTU标识.";
            string[] sa = userAndDtuId.Split(new string[] { "\t" }, StringSplitOptions.None);
            if (sa == null || sa.Length != 2)
                return Consts.TERM_ADD_DTU_ERR + "错误的用户名和DTU标识.";
            string un = sa[0].Trim();
            string dtuId = sa[1].Trim();
            lock (_taskLock)
            {
                UserInfo ui = Helper.FindUserInfo(un, _userInfoOc);
                if (ui == null)
                    return Consts.TERM_ADD_DTU_ERR + "无此用户 : " + un;
                if (ui.Online == false)
                    return Consts.TERM_ADD_DTU_ERR + "用户(" + un + ")不在线.";
                DTUInfo di = Helper.FindDTUInfo(sa[1].Trim(), _dtuInfoOc);
                if (di == null)
                    return Consts.TERM_ADD_DTU_ERR + "无此DTU : " + dtuId;
                if (di.Online == false)
                    return Consts.TERM_ADD_DTU_ERR + "DTU(" + dtuId + ")不在线.";
                if (ui.UserDTUs.Contains(di) == true || di.Controller != null)
                    return Consts.TERM_ADD_DTU_ERR + "DTU(" + dtuId + ")已经被用户(" + un + ")控制.";

                di.Controller = ui;
                if (ui.UserDTUs.Contains(di) == false)
                    ui.UserDTUs.Add(di);
                if (_termTaskList.ContainsKey(soc) == true)
                {
                    _termTaskList[soc].CurrentDTU = di;
                    _termTaskList[soc].Controller = ui;
                }
                if (_dtuTaskList.ContainsKey(di.DTUSocket) == true)
                    _dtuTaskList[di.DTUSocket].Controller = ui;
                if (_terminalDTUMap.ContainsKey(soc) == false)
                    _terminalDTUMap.Add(soc, di.DTUSocket);

                //soc.SendTimeout = -1;
                soc.ReceiveTimeout = -1;

                eventLogInformationTransfer.WriteEntry("DTU(" + dtuId + ")现在被用户(" + un + ")控制.");
            }

            return Consts.TERM_ADD_DTU_OK;
        }

        private string TermInitUser(Socket soc, string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return Consts.TERM_INIT_USER_ERR + "无用户名.";
            userName = userName.Trim();

            lock (_taskLock)
            {
                UserInfo ui = Helper.FindUserInfo(userName, _userInfoOc);
                if (ui == null)
                    return Consts.TERM_INIT_USER_ERR + "无此用户:" + userName;

                if (_termTaskList.ContainsKey(soc) == true)
                {
                    _termTaskList[soc].Controller = ui;
                    _termTaskList[soc].IsMainSocket = true;
                    if (ui.UserTermSockets.Contains(_termTaskList[soc]) == false)
                        ui.UserTermSockets.Add(_termTaskList[soc]);
                    if (_commMsgDict.ContainsKey(ui) == false)
                        _commMsgDict[ui] = new List<CommunicationMessage>();
                }
                else
                    return Consts.TERM_INIT_USER_ERR + "用户(" + userName + ")的终端发送和接收任务还没有被创建.";
            }
            return Consts.TERM_INIT_USER_OK;
        }

        private string TermCheckDtu(Socket soc, string dtuIds)
        {
            if (string.IsNullOrWhiteSpace(dtuIds) == true)
                return Consts.TERM_CHECK_DTU_ERR + "无查询的DTU ID";
            string s = dtuIds.Trim();
            string[] sa = s.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
            List<DTUInfo> tiList = new List<DTUInfo>();
            string sret = "";
            lock (_taskLock)
            {
                UserInfo ui = Helper.FindUserInfo(soc, _userInfoOc);
                if (ui == null)
                    return Consts.TERM_CHECK_DTU_ERR + "无法查询到当前用户";

                foreach (string sai in sa)
                {
                    DTUInfo di = Helper.FindDTUInfo(sai.Trim(), _dtuInfoOc);
                    if (di == null || di.Online == false)
                        continue;
                    bool diffSocket = false;
                    foreach(DTUInfo dii in ui.UserDTUs)
                    {
                        if (string.Compare(dii.DtuId.Trim(), di.DtuId.Trim(), true) == 0)
                        {
                            if (dii.DTUSocket != di.DTUSocket)
                            {
                                diffSocket = true;
                                break;
                            }
                        }
                    }
                    if (diffSocket == true)
                        continue;
                    if (string.IsNullOrWhiteSpace(sret) == true)
                        sret = sai;
                    else
                        sret = sret + "\t" + sai;
                }
            }
            return Consts.TERM_CHECK_DTU_OK + sret;
        }

        #endregion

        #region DTU

        private void StartDTUService()
        {
            IPAddress local = IPAddress.Any;
            IPEndPoint iep = new IPEndPoint(local, _dtuPort);
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(iep);
            server.Listen(Consts.SOCKET_LISTEN_BACKLOG_COUNT);
            eventLogInformationTransfer.WriteEntry("DTU服务开始侦听...");
            while (_cts.Token.IsCancellationRequested == false)
            {
                Socket soc = server.Accept();
                soc.ReceiveTimeout = _dtuTimeout;
                soc.SendTimeout = _dtuTimeout;
                string ip = ((IPEndPoint)soc.RemoteEndPoint).Address.ToString();
                eventLogInformationTransfer.WriteEntry("接受来自" + ip+ "的DTU.");

                CancellationTokenSource cts = new CancellationTokenSource();
                Task ts = new Task(
                    () =>
                    {
                        DTUSendService(soc);
                    }, cts.Token
                );
                Task tr = new Task(
                    () =>
                    {
                        DTUReceiveService(soc);
                    }, cts.Token
                );
                lock (_taskLock)
                {
                    _dtuTaskList.Add(soc, new TerminalDTUTaskInformation()
                    {
                        TaskSocket = soc,
                        CTS = cts,
                        SendTask = ts,
                        ReceiveTask = tr
                    });
                }
                ts.Start();
                tr.Start();
                eventLogInformationTransfer.WriteEntry("开始DTU发送任务" + ts.Id.ToString() + "和接收任务" + tr.Id.ToString());
            }
            eventLogInformationTransfer.WriteEntry("DTU服务停止侦听.");
        }

        public void DTUSendService(Socket soc)
        {
            string ip = ((IPEndPoint)soc.RemoteEndPoint).Address.ToString();
            Queue<byte[]> q = _dtuTaskList[soc].RequestQueue;
            CancellationTokenSource cts = _dtuTaskList[soc].CTS;

            while (_cts.Token.IsCancellationRequested == false &&
                cts.Token.IsCancellationRequested == false && 
                soc != null && soc.Connected == true)
            {
                try
                {
                    if (q.Count > 0)
                    {
                        byte[] ba = q.Dequeue();
                        soc.Send(ba);
                    }
                    else
                        Thread.Sleep(Consts.TASK_THREAD_SLEEP_INTERVAL);
                }
                catch (Exception ex)
                {
                    eventLogInformationTransfer.WriteEntry("DTU(" + ip + ")的发送任务(" + Task.CurrentId.ToString() + ")出错 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error, Consts.EVENT_ID_TO_DTU);
                    break;
                }
            }

            TerminateDTUSocket(soc);
        }

        public void DTUReceiveService(Socket soc)
        {
            bool getDTUId = true;
            string ip = ((IPEndPoint)soc.RemoteEndPoint).Address.ToString();
            byte[] ba = new byte[Consts.SOCKET_RECEIVING_BUFFER_LENGTH];
            int len = 0;
            DTUInfo dtu = null;
            CancellationTokenSource cts = _dtuTaskList[soc].CTS;
            int waitDTUIDCount = 0;

            while (_cts.Token.IsCancellationRequested == false &&
                cts.Token.IsCancellationRequested == false &&
                soc != null && soc.Connected == true)
            {
                try
                {
                    len = soc.Receive(ba);
                    if (len < 1)
                    {
                        #region Disconnected

                        lock (_taskLock)
                        {
                            dtu = Helper.FindDTUInfo(soc, _dtuInfoOc);
                            if (dtu == null)
                                eventLogInformationTransfer.WriteEntry("失去和DTU(" + ip + ")的连接.", EventLogEntryType.Warning, Consts.EVENT_ID_FROM_DTU);
                            else
                            {
                                dtu.Online = false;
                                dtu.DTUSocket = null;
                                dtu.Controller = null;
                                eventLogInformationTransfer.WriteEntry("失去和来自" + ip + "的DTU(" + dtu.DtuId + ")的连接.", EventLogEntryType.Warning, Consts.EVENT_ID_FROM_DTU);
                            }
                            _dtuTaskList[soc].CurrentDTU = null;
                            break;
                        }

                        #endregion
                    }
                    else
                    {
                        if (getDTUId == true)
                        {
                            #region Get DTU ID

                            string dtuId = Encoding.UTF8.GetString(ba);
                            dtuId = dtuId.Trim(new char[] { '\r', '\n', '\0' }).Trim();
                            if (dtuId.Length < Consts.DTU_INFO_ITEM_COUNT)
                            {
                                if (dtuId == null)
                                    dtuId = "";
                                if (waitDTUIDCount < Consts.WAIT_DTU_ID_COUNT_MAX)
                                {
                                    eventLogInformationTransfer.WriteEntry("再次等待DTU ID: " + dtuId, EventLogEntryType.Error, Consts.EVENT_ID_FROM_DTU);
                                    waitDTUIDCount++;
                                }
                                else
                                {
                                    eventLogInformationTransfer.WriteEntry("没有获得DTU ID : " + dtuId, EventLogEntryType.Error, Consts.EVENT_ID_FROM_DTU);
                                    break;
                                }
                            }
                            else
                            {
                                lock (_taskLock)
                                {
                                    dtuId = dtuId.Substring(0, Consts.DTU_INFO_ITEM_COUNT);
                                    dtu = Helper.FindDTUInfo(dtuId, _dtuInfoOc);
                                    if (dtu == null)
                                    {
                                        if (waitDTUIDCount < Consts.WAIT_DTU_ID_COUNT_MAX)
                                        {
                                            eventLogInformationTransfer.WriteEntry("再次等待DTU ID: " + dtuId, EventLogEntryType.Error, Consts.EVENT_ID_FROM_DTU);
                                            waitDTUIDCount++;
                                        }
                                        else
                                        {
                                            eventLogInformationTransfer.WriteEntry("未注册的DTU ID : " + dtuId, EventLogEntryType.Error, Consts.EVENT_ID_FROM_DTU);
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        if (dtu.DTUSocket != null)
                                            TerminateDTUSocket(dtu.DTUSocket);

                                        dtu.Online = true;
                                        dtu.DTUSocket = soc;
                                        getDTUId = false;
                                        _dtuTaskList[soc].CurrentDTU = dtu;

                                        //soc.SendTimeout = -1;
                                        soc.ReceiveTimeout = -1;
                                    }
                                }
                            }

                            #endregion
                        }
                        else
                        {
                            #region Transparent Transfer

                            lock (_taskLock)
                            {
                                Socket socTerm = null;
                                foreach (KeyValuePair<Socket, Socket> kvp in _terminalDTUMap)
                                {
                                    if (kvp.Value == soc)
                                    {
                                        socTerm = kvp.Key;
                                        break;
                                    }
                                }

                                if (socTerm == null)
                                    eventLogInformationTransfer.WriteEntry("忽略来自" + ip + "的DTU(" + dtu.DtuId + ")的消息因为它不再被控制.", EventLogEntryType.Warning, Consts.EVENT_ID_FROM_DTU);
                                else
                                {
                                    TerminalDTUTaskInformation tdti = null;
                                    if (_termTaskList.ContainsKey(socTerm) == true)
                                        tdti = _termTaskList[socTerm];

                                    if (tdti == null)
                                    {
                                        eventLogInformationTransfer.WriteEntry("忽略来自" + ip + "的DTU(" + dtu.DtuId + ")的消息因为控制它的终端的任务服务没有正常运作.", EventLogEntryType.Warning, Consts.EVENT_ID_FROM_DTU);
                                        if (_terminalDTUMap.ContainsKey(socTerm))
                                            _terminalDTUMap.Remove(socTerm);
                                    }
                                    else
                                    {
                                        Queue<byte[]> qb = tdti.RequestQueue;
                                        byte[] bar = new byte[len];
                                        for (int i = 0; i < len; i++)
                                        {
                                            bar[i] = ba[i];
                                        }
                                        qb.Enqueue(bar);

                                        #region Log DTU Message

                                        UserInfo ui = tdti.Controller;
                                        if (ui != null && dtu != null)
                                        {
                                            if (_commMsgDict.ContainsKey(ui))
                                            {
                                                List<CommunicationMessage> cmList = _commMsgDict[ui];
                                                string sar = Encoding.UTF8.GetString(bar);
                                                sar = sar.Replace("\r", @"\r");
                                                sar = sar.Replace("\n", @"\n");
                                                sar = sar.Replace("\t", @"\t");
                                                sar = sar.Replace("\0", @"\0");
                                                cmList.Add(new CommunicationMessage()
                                                {
                                                    UserName = ui.UserName,
                                                    DTUID = dtu.DtuId,
                                                    IsToDTU = false,
                                                    TimeStamp = DateTime.Now,
                                                    Message = sar
                                                });
                                            }
                                        }

                                        #endregion
                                    }
                                }
                            }

                            #endregion
                        }
                    }
                }
                catch (Exception ex)
                {
                    eventLogInformationTransfer.WriteEntry("来自" + ip + "的DTU的接收任务(" + Task.CurrentId.ToString() + ")出现错误 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Warning, Consts.EVENT_ID_FROM_DTU);
                    break;
                }
            }

            TerminateDTUSocket(soc);
        }

        private void TerminateDTUSocket(Socket soc)
        {
            TerminalDTUTaskInformation tdti = null;
            DTUInfo dtu = null;
            UserInfo ui = null;
            lock (_taskLock)
            {
                if (_dtuTaskList.ContainsKey(soc) == true)
                {
                    tdti = _dtuTaskList[soc];
                    ui = tdti.Controller;

                    tdti.CTS.Cancel();
                    tdti.RequestQueue.Clear();
                    _dtuTaskList.Remove(soc);
                }
                dtu = Helper.FindDTUInfo(soc, _dtuInfoOc);
                if (dtu != null)
                {
                    dtu.Online = false;
                    dtu.Controller = null;
                }

                if (ui != null && dtu != null && ui.UserDTUs.Contains(dtu))
                    ui.UserDTUs.Remove(dtu);

                Socket socTerm = null;
                foreach (KeyValuePair<Socket, Socket> kvp in _terminalDTUMap)
                {
                    if (kvp.Value == soc)
                    {
                        socTerm = kvp.Key;
                        break;
                    }
                }
                if (socTerm != null)
                    _terminalDTUMap.Remove(socTerm);
            }

            //Debug.Assert(tdti.TaskSocket == soc);
            //Debug.Assert(dtu.DTUSocket == soc);
            try
            {
                tdti.TaskSocket = null;
            }
            catch (Exception) { }
            try
            {
                dtu.DTUSocket = null;
            }
            catch (Exception) { }
            Helper.SafeCloseSocket(soc);
        }

        #endregion

        #endregion

        #region Stop

        private void StopSocketTask()
        {
            _cts.Cancel();
            foreach (KeyValuePair<Socket, Task> kvp in _manageTaskDict)
            {
                Socket soc = kvp.Key;
                Task t = kvp.Value;
                Helper.SafeCloseSocket(soc);
                try
                {
                    t.Wait(Consts.TASK_STOP_WAIT_TIME);//, _manageCts.Token);

                    eventLogInformationTransfer.WriteEntry("管理任务 " + t.Id.ToString() + "停止.");
                }
                catch (AggregateException ex)
                {
                    eventLogInformationTransfer.WriteEntry("管理任务" + t.Id.ToString() + "不能够正常停止 : (AggregateException) " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    foreach (Exception exi in ex.InnerExceptions)
                    {
                        eventLogInformationTransfer.WriteEntry("管理任务inner exception : " + exi.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    }
                }
                catch (Exception ex)
                {
                    eventLogInformationTransfer.WriteEntry("管理任务" + t.Id.ToString() + "无法正确停止 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                }
            }
            foreach (KeyValuePair<Socket, TerminalDTUTaskInformation> kvp in _termTaskList)
            {
                Helper.SafeCloseSocket(kvp.Key);
                Task ts = kvp.Value.SendTask;
                Task tr = kvp.Value.ReceiveTask;
                CancellationTokenSource cts = kvp.Value.CTS;
                cts.Cancel();
                try
                {
                    ts.Wait(Consts.TASK_STOP_WAIT_TIME);//, _termCts.Token);

                    eventLogInformationTransfer.WriteEntry("终端发送任务" + ts.Id.ToString() + "停止.");
                }
                catch (AggregateException ex)
                {
                    eventLogInformationTransfer.WriteEntry("终端发送任务" + ts.Id.ToString() + "不能正确停止 : (AggregateException) " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    foreach (Exception exi in ex.InnerExceptions)
                    {
                        eventLogInformationTransfer.WriteEntry("终端发送任务 inner exception : " + exi.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    }
                }
                catch (Exception ex)
                {
                    eventLogInformationTransfer.WriteEntry("终端发送任务" + ts.Id.ToString() + "不能正确停止 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                }
                try
                {
                    tr.Wait(Consts.TASK_STOP_WAIT_TIME);//, _termCts.Token);

                    eventLogInformationTransfer.WriteEntry("终端接收任务" + tr.Id.ToString() + "停止.");
                }
                catch (AggregateException ex)
                {
                    eventLogInformationTransfer.WriteEntry("终端接收任务" + tr.Id.ToString() + "不能正确停止 : (AggregateException) " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    foreach (Exception exi in ex.InnerExceptions)
                    {
                        eventLogInformationTransfer.WriteEntry("终端接收任务inner exception : " + exi.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    }
                }
                catch (Exception ex)
                {
                    eventLogInformationTransfer.WriteEntry("终端接收任务" + tr.Id.ToString() + "不能正确停止 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                }
            }
            foreach (KeyValuePair<Socket, TerminalDTUTaskInformation> kvp in _dtuTaskList)
            {
                Helper.SafeCloseSocket(kvp.Key);
                Task ts = kvp.Value.SendTask;
                Task tr = kvp.Value.ReceiveTask;
                CancellationTokenSource cts = kvp.Value.CTS;
                cts.Cancel();
                try
                {
                    ts.Wait(Consts.TASK_STOP_WAIT_TIME);//, _termCts.Token);

                    eventLogInformationTransfer.WriteEntry("DTU发送任务" + ts.Id.ToString() + "停止.");
                }
                catch (AggregateException ex)
                {
                    eventLogInformationTransfer.WriteEntry("DTU发送任务" + ts.Id.ToString() + "不能正确停止 : (AggregateException) " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    foreach (Exception exi in ex.InnerExceptions)
                    {
                        eventLogInformationTransfer.WriteEntry("DTU发送任务inner exception : " + exi.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    }
                }
                catch (Exception ex)
                {
                    eventLogInformationTransfer.WriteEntry("DTU发送任务" + ts.Id.ToString() + "不能正确停止 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                }
                try
                {
                    tr.Wait(Consts.TASK_STOP_WAIT_TIME);//, _termCts.Token);

                    eventLogInformationTransfer.WriteEntry("DTU接收任务" + tr.Id.ToString() + "停止.");
                }
                catch (AggregateException ex)
                {
                    eventLogInformationTransfer.WriteEntry("DTU接收任务" + tr.Id.ToString() + "不能正确停止 : (AggregateException) " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    foreach (Exception exi in ex.InnerExceptions)
                    {
                        eventLogInformationTransfer.WriteEntry("DTU接收任务inner exception : " + exi.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    }
                }
                catch (Exception ex)
                {
                    eventLogInformationTransfer.WriteEntry("DTU接收任务" + tr.Id.ToString() + "不能正确停止 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                }
            }
        }

        #endregion

        #region Methods

        private void PutTermDTUResponse(Socket s, byte[] ba, int len, bool isTerm = true)
        {
            lock (_taskLock)
            {
                Queue<byte[]> q = null;
                if (isTerm == true)
                {
                    if (_termTaskList.ContainsKey(s))
                        q = _termTaskList[s].RequestQueue;
                }
                else
                {
                    if (_dtuTaskList.ContainsKey(s))
                        q = _dtuTaskList[s].RequestQueue;
                }

                if (q != null)
                {
                    if (ba == null || ba.Length < 1)
                        q.Enqueue(new byte[] { });
                    else
                    {
                        if (len < 0)
                            q.Enqueue(ba);
                        else
                        {
                            if (len == 0)
                                q.Enqueue(new byte[] { });
                            else
                            {
                                int balen = ba.Length;
                                if (len >= balen)
                                    q.Enqueue(ba);
                                else
                                {
                                    byte[] ban = new byte[len];
                                    for (int i = 0; i < len; i++)
                                    {
                                        ban[i] = ba[i];
                                    }
                                    q.Enqueue(ban);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void UserInfo_OnlineOfflineEvent(object sender, CommonEventArgs args)
        {
            lock (_taskLock)
            {
                UserInfo ui = sender as UserInfo;
                if (ui == null)
                    return;
                if (_commMsgDict.ContainsKey(ui) == true)
                {
                    List<CommunicationMessage> cmList = _commMsgDict[ui];
                    StringBuilder sb = new StringBuilder();
                    foreach (CommunicationMessage cm in cmList)
                    {
                        sb.Append(cm.UserName + "\t" + cm.DTUID + "\t" + cm.IsToDTU.ToString() + "\t" +
                            cm.TimeStamp.ToLongDateString() + " " + cm.TimeStamp.ToLongTimeString() + "\t" +
                            cm.Message + "\n");
                    }
                    cmList.Clear();
                    string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
                    DateTime dt = DateTime.Now;
                    string sdt1 = dt.Year.ToString() + "-" + dt.Month.ToString() + "-" + dt.Day.ToString()
                        + " " + dt.Hour.ToString() + "-" + dt.Minute.ToString() + "-" + dt.Second.ToString();
                    string sdt2 = dt.Millisecond.ToString();
                    string sdt = sdt1 + " " + sdt2;
                    try
                    {
                        StreamWriter sw = new StreamWriter(folder + @"\COMWAY\Service\message\" + ui.UserName + " " + sdt + ".log");
                        sw.WriteLine(sb.ToString());
                        sw.Flush();
                        sw.Close();
                        sw.Dispose();

                        _logFileOc.Add(new Tuple<string, string, string>(ui.UserName, sdt1, sdt2));
                    }
                    catch (Exception ex)
                    {
                        eventLogInformationTransfer.WriteEntry("保存消息日志的时候出现错误 : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Warning);
                    }
                    _commMsgDict.Remove(ui);
                }
            }
        }

        #endregion
    }
}
