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

        public SystemService()
        {
            if (Directory.Exists(Consts.DEFAULT_DIRECTORY) == false)
                Directory.CreateDirectory(Consts.DEFAULT_DIRECTORY);
            if (Directory.Exists(Consts.DEFAULT_DIRECTORY + @"\ServiceConfiguration") == false)
                Directory.CreateDirectory(Consts.DEFAULT_DIRECTORY + @"\ServiceConfiguration");
            if (Directory.Exists(Consts.DEFAULT_DIRECTORY + @"\ServiceConfiguration\config") == false)
                Directory.CreateDirectory(Consts.DEFAULT_DIRECTORY + @"\ServiceConfiguration\config");
            if (Directory.Exists(Consts.DEFAULT_DIRECTORY + @"\ServiceConfiguration\log") == false)
                Directory.CreateDirectory(Consts.DEFAULT_DIRECTORY + @"\ServiceConfiguration\log");

            InitializeComponent();

            InitService();
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
            //if (EncryptDecryptLibrary.EncryptDecryptLibrary.CheckRunOrNot() == false)
            //{
            //    eventLogInformationTransfer.WriteEntry("IT service cannot be started because of no valid license.", EventLogEntryType.Error);
            //    System.Environment.Exit(0);
            //}

            LoadConfig();
            LoadUser();
            LoadDTU();

            try
            {
                StartSocketTask();

                eventLogInformationTransfer.WriteEntry("IT service is started.");
            }
            catch (Exception ex)
            {
                eventLogInformationTransfer.WriteEntry("IT service cannot be started : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
            }
        }

        protected override void OnStop()
        {
            try
            {
                StopSocketTask();

                eventLogInformationTransfer.WriteEntry("IT service is stopped.");
            }
            catch (Exception ex)
            {
                eventLogInformationTransfer.WriteEntry("IT service cannot be stopped : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
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
                sr = new StreamReader(Consts.DEFAULT_DIRECTORY + @"\ServiceConfiguration\config\ssportto.cfg");
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

                eventLogInformationTransfer.WriteEntry("Exception when loading config : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Warning);
            }
        }

        private void SaveConfig()
        {
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(Consts.DEFAULT_DIRECTORY + @"\ServiceConfiguration\config\ssportto.cfg");
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

                eventLogInformationTransfer.WriteEntry("Exception when saving config : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Warning);
            }
        }

        private void LoadUser()
        {
            _userInfoOc.Clear();
            StreamReader sr = null;
            try
            {
                sr = new StreamReader(Consts.DEFAULT_DIRECTORY + @"\ServiceConfiguration\config\ituser.dat");
                string line = null;
                while (true)
                {
                    line = sr.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        break;
                    string[] sa = line.Split(new string[] { "\t" }, StringSplitOptions.None);
                    if (sa == null || sa.Length != Consts.USER_ACCOUNT_ITEM_COUNT)
                    {
                        eventLogInformationTransfer.WriteEntry("User information error : " + line, EventLogEntryType.Warning);
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
                        eventLogInformationTransfer.WriteEntry("User permission error : " + ruser, EventLogEntryType.Warning);
                        rpm = "2";
                    }
                    UserInfo ui = Helper.FindUserInfo(ruser, _userInfoOc);
                    if (ui != null)
                        eventLogInformationTransfer.WriteEntry("Duplicated user : " + ruser, EventLogEntryType.Warning);
                    else
                    {
                        _userInfoOc.Add(new UserInfo()
                        {
                            DisplayIcon = false,
                            UserName = ruser,
                            Password = rpw,
                            Permission = rpm
                        });
                    }
                }
                sr.Close();
                sr.Dispose();
            }
            catch (Exception ex)
            {
                Helper.SafeCloseIOStream(sr);

                eventLogInformationTransfer.WriteEntry("Exception when loading user : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Warning);
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
                sw = new StreamWriter(Consts.DEFAULT_DIRECTORY + @"\ServiceConfiguration\config\ituser.dat");
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

                eventLogInformationTransfer.WriteEntry("Exception when saving user : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Warning);
            }
        }

        private void LoadDTU()
        {
            _dtuInfoOc.Clear();
            StreamReader sr = null;
            try
            {
                sr = new StreamReader(Consts.DEFAULT_DIRECTORY + @"\ServiceConfiguration\config\itdtu.dat");
                string line = null;
                while (true)
                {
                    line = sr.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        break;
                    string[] sa = line.Split(new string[] { "\t" }, StringSplitOptions.None);
                    if (sa == null || sa.Length != Consts.DTU_ACCOUNT_ITEM_COUNT)
                    {
                        eventLogInformationTransfer.WriteEntry("DTU information error : " + line, EventLogEntryType.Warning);
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
                        eventLogInformationTransfer.WriteEntry("Dupliacted DTU : " + rdtuId, EventLogEntryType.Warning);
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

                eventLogInformationTransfer.WriteEntry("Exception when loading DTU : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Warning);
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
                sw = new StreamWriter(Consts.DEFAULT_DIRECTORY + @"\ServiceConfiguration\config\itdtu.dat");
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

                eventLogInformationTransfer.WriteEntry("Exception when saving DTU : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Warning);
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
                        eventLogInformationTransfer.WriteEntry("Starting management socket exception : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
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
                        eventLogInformationTransfer.WriteEntry("Starting terminal socket exception : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
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
                        eventLogInformationTransfer.WriteEntry("Start DTU service exception : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
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
            eventLogInformationTransfer.WriteEntry("Management starts listening...");
            while (_cts.Token.IsCancellationRequested == false)
            {
                Socket soc = server.Accept();
                soc.ReceiveTimeout = _manTimeout;
                soc.SendTimeout = _manTimeout;
                string ip = ((IPEndPoint)soc.RemoteEndPoint).Address.ToString();
                eventLogInformationTransfer.WriteEntry("Accepted one management from " + ip);

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
                eventLogInformationTransfer.WriteEntry("Start management task " + t.Id.ToString());
            }
            eventLogInformationTransfer.WriteEntry("Management stops listening.");
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
            byte[] bytes = new byte[Consts.SOCKET_RECEIVING_BUFFER_LENGTH_MAN];
            int len = 0;
            try
            {
                while (_cts.Token.IsCancellationRequested == false && 
                    soc != null && soc.Connected == true)
                {
                    len = soc.Receive(bytes);
                    if (len < 1)
                    {
                        eventLogInformationTransfer.WriteEntry("Connection to management (" + ip + ") is broken.", EventLogEntryType.Warning);
                        break;
                    }
                    else
                    {
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, len);
                        // Management response MUST NOT have unnecessary blank characters at the begin or the end.
                        string resp = ProcessManagementData(soc, bytes, len);//.Trim();
                        soc.Send(System.Text.Encoding.ASCII.GetBytes(resp));
                    }
                }
            }
            catch (Exception ex)
            {
                eventLogInformationTransfer.WriteEntry("Communication with management (" + ip + ") exception : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
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
            eventLogInformationTransfer.WriteEntry("Management task " + Task.CurrentId.ToString() + " ends.");
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
            }
        }

        #endregion

        #region Process Management Data Callbacks

        private string AddUser(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Consts.MAN_ADD_USER_ERR + "No user information.";
            content = content.Trim();

            string[] sa = content.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
            if (sa == null || sa.Length != Consts.USER_ACCOUNT_ITEM_COUNT)
                return Consts.MAN_ADD_USER_ERR + "Invalid user information :" + content;
            string userName = sa[0].Trim();
            string password = sa[1].Trim();
            string permission = sa[2].Trim();
            if (Helper.CheckValidChar(userName, Helper.CheckMethod.CharNum) == false)
                return Consts.MAN_ADD_USER_ERR + "Invalid user name : " + userName;
            if (Helper.CheckValidChar(password, Helper.CheckMethod.CharNum) == false)
                return Consts.MAN_ADD_USER_ERR + "Invalid password :" + password;
            if (permission != "1" && permission != "2")
                return Consts.MAN_ADD_USER_ERR + "Invalid permission : " + permission;
            lock (_taskLock)
            {
                UserInfo ui = Helper.FindUserInfo(userName, _userInfoOc);
                if (ui != null)
                    return Consts.MAN_ADD_USER_ERR + "Duplicated user : " + userName;
                _userInfoOc.Add(new UserInfo()
                {
                    DisplayIcon = false,
                    UserName = userName,
                    Password = password,
                    Permission = permission
                });
                SaveUser();
            }
            return Consts.MAN_ADD_USER_OK;
        }

        private string DeleteUser(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return Consts.MAN_DELETE_USER_ERR + "No user to be deleted.";
            userName = userName.Trim();

            lock (_taskLock)
            {
                UserInfo ui = Helper.FindUserInfo(userName, _userInfoOc);
                if (ui == null)
                    return Consts.MAN_DELETE_USER_ERR + "User (" + userName + ") doesn't exist.";
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
                return Consts.MAN_ADD_DTU_ERR + "No DTU information.";
            //content = content.Trim();
            string[] sa = content.Split(new string[] { "\t" }, StringSplitOptions.None);
            if (sa == null || sa.Length != Consts.DTU_ACCOUNT_ITEM_COUNT)
                return Consts.MAN_ADD_DTU_ERR + "Uncompelte DTU information :" + content;
            string dtuId = sa[0].Trim();
            string simId = sa[1].Trim();
            string un = sa[2].Trim();
            string utel = sa[3].Trim();
            lock (_taskLock)
            {
                DTUInfo di = Helper.FindDTUInfo(sa[0].Trim(), _dtuInfoOc);
                if (di != null)
                    return Consts.MAN_ADD_DTU_ERR + "Duplicated DTU : " + dtuId;
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
                return Consts.MAN_ADD_DTU_ERR + "No DTU ID.";
            dtuId = dtuId.Trim();

            lock (_taskLock)
            {
                DTUInfo dtu = Helper.FindDTUInfo(dtuId, _dtuInfoOc);
                if (dtu == null)
                    return Consts.MAN_DELETE_DTU_ERR + "DTU (" + dtuId + ") doesn't exist.";
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
                return Consts.MAN_MODIFY_DTU_ERR + "No DTU information.";
            //content = content.Trim();
            string[] sa = content.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
            if (sa == null || sa.Length != Consts.DTU_ACCOUNT_ITEM_COUNT)
                return Consts.MAN_MODIFY_DTU_ERR + "Uncompelte DTU information :" + content;
            string dtuId = sa[0].Trim();
            string simId = sa[1].Trim();
            string un = sa[2].Trim();
            string utel = sa[3].Trim();
            lock (_taskLock)
            {
                DTUInfo di = Helper.FindDTUInfo(dtuId, _dtuInfoOc);
                if (di == null)
                    return Consts.MAN_MODIFY_DTU_ERR + "DTU ID (" + dtuId + ") doesn't exists.";
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
                return headerErr + "No user name and password.";
            string[] sa = s.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
            if (sa == null || sa.Length != Consts.USER_ACCOUNT_ITEM_COUNT - 1)
                return headerErr + "No user name or password.";

            lock (_taskLock)
            {
                UserInfo ui = Helper.FindUserInfo(sa[0].Trim(), _userInfoOc);
                if (ui == null)
                    return headerErr + "Invalid user name.";
                if (string.Compare(sa[1].Trim(), ui.Password, true) != 0)
                    return headerErr + "Invalid password.";

                if (isMan == true)
                {
                    if (ui.Permission != "0" && ui.Permission != "1")
                        return headerErr + "Common user is not allowed for service configuration purpose.";
                }
                else
                {
                    if (ui.Permission != "2")
                        return headerErr + "Super/Management user is not allowed for DTU management purpose.";
                }

                if (ui.Online == true)
                    return headerErr + "Already online from " + ui.Information + " when " + ui.DtLogString;

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
                    return headerErr + "Invalid user name.";

                ui.Online = false;

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

                Debug.Assert(ui.UserSocket == soc);

                Helper.SafeCloseSocket(soc);
                ui.UserSocket = null;
            }

            return headerOk;
        }

        private string KickOffUser(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return Consts.MAN_KICK_USER_ERR + "No user to be kicked off.";
            userName = userName.Trim();

            lock (_taskLock)
            {
                UserInfo ui = Helper.FindUserInfo(userName, _userInfoOc);
                if (ui == null)
                    return Consts.MAN_KICK_USER_ERR + "Invalid user name.";

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
                return Consts.MAN_KICK_DTU_ERR + "No DTU to be kick off.";
            dtuId = dtuId.Trim();

            lock (_taskLock)
            {
                DTUInfo dtu = Helper.FindDTUInfo(dtuId, _dtuInfoOc);
                if (dtu == null)
                    return Consts.MAN_KICK_DTU_ERR + "DTU (" + dtuId + ") doesn't exist.";
                if (dtu.Online == false)
                    return Consts.MAN_KICK_DTU_ERR + "DTU (" + dtuId + ") is already offline.";

                dtu.Online = false;

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
                
                Helper.SafeCloseSocket(dtu.DTUSocket);
            }

            return Consts.MAN_KICK_DTU_OK;
        }

        private string ReleaseDTU(string dtuId)
        {
            if (string.IsNullOrEmpty(dtuId))
                return Consts.MAN_UNCTRL_DTU_ERR + "No DTU to be released.";
            //content = content.Trim();
            string[] sa = dtuId.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
            if (sa == null || sa.Length != 2)
                return Consts.MAN_UNCTRL_DTU_ERR + "No user name or DTU ID.";

            lock (_taskLock)
            {
                DTUInfo di = null;
                di = Helper.FindDTUInfo(sa[1].Trim(), _dtuInfoOc);
                if (di == null)
                    return Consts.MAN_UNCTRL_DTU_ERR + "DTU (" + dtuId + ") doesn't exist.";
                if (di.Online == false)
                    return Consts.MAN_UNCTRL_DTU_ERR + "DTU (" + dtuId + ") is already offline.";
                if (di.Controller == null)
                    return Consts.MAN_UNCTRL_DTU_ERR + "DTU (" + dtuId + ") is already released.";

                UserInfo ui = di.Controller;
                if (ui != null)
                {
                    if (string.Compare("admin", sa[0].Trim(), true) != 0)
                        Debug.Assert(string.Compare(ui.UserName, sa[0].Trim(), true) == 0);
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
            eventLogInformationTransfer.WriteEntry("Terminal starts listening...");
            while (_cts.Token.IsCancellationRequested == false)
            {
                Socket soc = server.Accept();
                soc.ReceiveTimeout =  _termTimeout;
                soc.SendTimeout = _termTimeout;
                string ip = ((IPEndPoint)soc.RemoteEndPoint).Address.ToString();
                eventLogInformationTransfer.WriteEntry("Accepted one terminal from " + ip);

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
                eventLogInformationTransfer.WriteEntry("Start terminal sending task " + ts.Id.ToString() + " and receive task " + tr.Id.ToString());
            }
            eventLogInformationTransfer.WriteEntry("Terminal stops listening.");
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
                    eventLogInformationTransfer.WriteEntry("Exception in sending task (" + Task.CurrentId.ToString() + ") for terminal (" + ip + ") : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error, Consts.EVENT_ID_TO_TERM);
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
                        eventLogInformationTransfer.WriteEntry("Connection to terminal (" + ip + ") is broken.", EventLogEntryType.Warning, Consts.EVENT_ID_FROM_DTU);
                        break;
                    }
                    else
                    {
                        bool dtuMsg = false;
                        foreach (KeyValuePair<Socket, Socket> kvp in _terminalDTUMap)
                        {
                            if (kvp.Key == soc)
                            {
                                if (kvp.Value == null)
                                {
                                    eventLogInformationTransfer.WriteEntry("No DTU is assigned to terminal (" + ip + ")", EventLogEntryType.Error, Consts.EVENT_ID_FROM_TERM);
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
                                break;
                            }
                        }
                        if(dtuMsg == false)
                            ProcessTermData(soc, ba, len);
                    }
                }
                catch (Exception ex)
                {
                    eventLogInformationTransfer.WriteEntry("Exception in receiving task (" + Task.CurrentId.ToString() + ") for terminal (" + ip + ") : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error, Consts.EVENT_ID_FROM_DTU);
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

                Debug.Assert(tdti.TaskSocket == soc);

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
                    bar = System.Text.Encoding.ASCII.GetBytes(ReleaseDTU(data.Item3));
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
                    bar = System.Text.Encoding.ASCII.GetBytes(TermAddDtu(soc, data.Item3));
                    break;
                //case Consts.TERM_DELETE_DTU:
                //    bar = System.Text.Encoding.ASCII.GetBytes(TermDeleteDtu(soc, data.Item3));
                //    break;
                case Consts.TERM_LOGIN:
                    bar = System.Text.Encoding.ASCII.GetBytes(CheckLogin(soc, data.Item3, false));
                    break;
                case Consts.TERM_LOGOUT:
                    bar = System.Text.Encoding.ASCII.GetBytes(CheckLogout(soc, data.Item3, false));
                    break;
                case Consts.TERM_INIT_USER:
                    bar = System.Text.Encoding.ASCII.GetBytes(TermInitUser(soc, data.Item3));
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
                byte[] ba = System.Text.Encoding.ASCII.GetBytes(str);
                PutTermDTUResponse(s, ba, ba.Length);
            }
        }

        #endregion

        #region Process Terminal Data Callbacks

        private string TermAddDtu(Socket soc, string userAndDtuId)
        {
            if (string.IsNullOrWhiteSpace(userAndDtuId))
                return Consts.TERM_ADD_DTU_ERR + "Wrong user name and DTU id.";
            string[] sa = userAndDtuId.Split(new string[] { "\t" }, StringSplitOptions.None);
            if (sa == null || sa.Length != 2)
                return Consts.TERM_ADD_DTU_ERR + "Wrong user name or DTU id.";
            string un = sa[0].Trim();
            string dtuId = sa[1].Trim();
            lock (_taskLock)
            {
                UserInfo ui = Helper.FindUserInfo(un, _userInfoOc);
                if (ui == null)
                    return Consts.TERM_ADD_DTU_ERR + "User (" + un + ") not exists.";
                DTUInfo di = Helper.FindDTUInfo(sa[1].Trim(), _dtuInfoOc);
                if (di == null)
                    return Consts.TERM_ADD_DTU_ERR + "DTU (" + dtuId + ") doesn't exists.";
                if (di.Online == false)
                    return Consts.TERM_ADD_DTU_ERR + "DTU (" + dtuId + ") is offline.";
                if (ui.UserDTUs.Contains(di) == true)
                    return Consts.TERM_ADD_DTU_ERR + "DTU (" + dtuId + ") is already under control.";
                if (di.Controller != null)
                    return Consts.TERM_ADD_DTU_ERR + "DTU (" + dtuId + ") is under control by " + di.Controller.UserName;

                di.Online = true;
                di.Controller = ui;

                if (ui.UserDTUs.Contains(di) == false)
                    ui.UserDTUs.Add(di);
                if (_dtuTaskList.ContainsKey(di.DTUSocket) == true)
                    _dtuTaskList[di.DTUSocket].Controller = ui;
                if (_terminalDTUMap.ContainsKey(soc) == false)
                    _terminalDTUMap.Add(soc, di.DTUSocket);

                soc.SendTimeout = -1;
                soc.ReceiveTimeout = -1;

                eventLogInformationTransfer.WriteEntry("DTU (" + dtuId + ") is now under control by " + ui.UserName);
            }

            return Consts.TERM_ADD_DTU_OK;
        }

        private string TermInitUser(Socket soc, string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return Consts.TERM_INIT_USER_ERR + "Wrong user name.";
            userName = userName.Trim();

            lock (_taskLock)
            {
                UserInfo ui = Helper.FindUserInfo(userName, _userInfoOc);
                if (ui == null)
                    return Consts.TERM_INIT_USER_ERR + "User (" + userName + ") doesn't exsit.";

                if (_termTaskList.ContainsKey(soc) == true)
                {
                    _termTaskList[soc].Controller = ui;
                    if (ui.UserTermSockets.Contains(_termTaskList[soc]) == false)
                        ui.UserTermSockets.Add(_termTaskList[soc]);
                }
                else
                    return Consts.TERM_INIT_USER_ERR + "Terminal tasks for user (" + userName + ") is not ready.";
            }
            return Consts.TERM_INIT_USER_OK;
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
            eventLogInformationTransfer.WriteEntry("DTU starts listening...");
            while (_cts.Token.IsCancellationRequested == false)
            {
                Socket soc = server.Accept();
                soc.ReceiveTimeout = _dtuTimeout;
                soc.SendTimeout = _dtuTimeout;
                string ip = ((IPEndPoint)soc.RemoteEndPoint).Address.ToString();
                eventLogInformationTransfer.WriteEntry("Accepted one DTU from " + ip);

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
                eventLogInformationTransfer.WriteEntry("Start DTU send task " + ts.Id.ToString() + " and receive task " + tr.Id.ToString());
            }
            eventLogInformationTransfer.WriteEntry("DTU stops listening.");
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
                    eventLogInformationTransfer.WriteEntry("Exception in sending task (" + Task.CurrentId.ToString() + ") for DTU (" + ip + ") : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error, Consts.EVENT_ID_TO_DTU);
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
                                eventLogInformationTransfer.WriteEntry("Connection to DTU from " + ip + " is broken.", EventLogEntryType.Warning, Consts.EVENT_ID_FROM_DTU);
                            else
                            {
                                dtu.Online = false;
                                dtu.DTUSocket = null;
                                dtu.Controller = null;
                                eventLogInformationTransfer.WriteEntry("Connection to DTU (" + dtu.DtuId + ") from " + ip + " is broken.", EventLogEntryType.Warning, Consts.EVENT_ID_FROM_DTU);
                            }
                            break;
                        }

                        #endregion
                    }
                    else
                    {
                        if (getDTUId == true)
                        {
                            #region Get DTU ID

                            string dtuId = System.Text.ASCIIEncoding.ASCII.GetString(ba);
                            dtuId = dtuId.Trim(new char[] { '\r', '\n', '\0' }).Trim();
                            if (dtuId.Length != Consts.DTU_INFO_ITEM_COUNT)
                                eventLogInformationTransfer.WriteEntry("Wait for DTU ID again : " + dtuId, EventLogEntryType.Error, Consts.EVENT_ID_FROM_DTU);
                            else
                            {
                                lock (_taskLock)
                                {
                                    dtu = Helper.FindDTUInfo(dtuId, _dtuInfoOc);
                                    if (dtu == null)
                                        eventLogInformationTransfer.WriteEntry("Unregistered DTU ID : " + dtuId, EventLogEntryType.Error, Consts.EVENT_ID_FROM_DTU);
                                    else
                                    {
                                        dtu.Online = true;
                                        dtu.DTUSocket = soc;
                                        getDTUId = false;

                                        soc.SendTimeout = -1;
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
                                    eventLogInformationTransfer.WriteEntry("Ignore message from DTU (" + dtu.DtuId + ") at " + ip + " because it is not under control.", EventLogEntryType.Warning, Consts.EVENT_ID_FROM_DTU);
                                else
                                {
                                    TerminalDTUTaskInformation tdti = null;
                                    if (_termTaskList.ContainsKey(socTerm) == true)
                                        tdti = _termTaskList[socTerm];

                                    if (tdti == null)
                                    {
                                        eventLogInformationTransfer.WriteEntry("Ignore message from DTU (" + dtu.DtuId + ") at " + ip + " because the terminal task serviceis not ready.", EventLogEntryType.Warning, Consts.EVENT_ID_FROM_DTU);
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
                                    }
                                }
                            }

                            #endregion
                        }
                    }
                }
                catch (Exception ex)
                {
                    eventLogInformationTransfer.WriteEntry("Exception in receiving task (" + Task.CurrentId.ToString() + ") for DTU (" + ip + ") : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Warning, Consts.EVENT_ID_FROM_DTU);
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

            Debug.Assert(tdti.TaskSocket == soc);
            Debug.Assert(dtu.DTUSocket == soc);
            tdti.TaskSocket = null;
            dtu.DTUSocket = null;
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

                    eventLogInformationTransfer.WriteEntry("Management task " + t.Id.ToString() + " stopped.");
                }
                catch (AggregateException ex)
                {
                    eventLogInformationTransfer.WriteEntry("Management task " + t.Id.ToString() + " cannot stop successfully : (AggregateException) " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    foreach (Exception exi in ex.InnerExceptions)
                    {
                        eventLogInformationTransfer.WriteEntry("Management task inner exception : " + exi.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    }
                }
                catch (Exception ex)
                {
                    eventLogInformationTransfer.WriteEntry("Management task " + t.Id.ToString() + " cannot stop successfully : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
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

                    eventLogInformationTransfer.WriteEntry("Terminal send task " + ts.Id.ToString() + " stopped.");
                }
                catch (AggregateException ex)
                {
                    eventLogInformationTransfer.WriteEntry("Terminal send task " + ts.Id.ToString() + " cannot stop successfully : (AggregateException) " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    foreach (Exception exi in ex.InnerExceptions)
                    {
                        eventLogInformationTransfer.WriteEntry("Terminal send task inner exception : " + exi.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    }
                }
                catch (Exception ex)
                {
                    eventLogInformationTransfer.WriteEntry("Terminal send task " + ts.Id.ToString() + " cannot stop successfully : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                }
                try
                {
                    tr.Wait(Consts.TASK_STOP_WAIT_TIME);//, _termCts.Token);

                    eventLogInformationTransfer.WriteEntry("Terminal receive task " + tr.Id.ToString() + " stopped.");
                }
                catch (AggregateException ex)
                {
                    eventLogInformationTransfer.WriteEntry("Terminal receive task " + tr.Id.ToString() + " cannot stop successfully : (AggregateException) " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    foreach (Exception exi in ex.InnerExceptions)
                    {
                        eventLogInformationTransfer.WriteEntry("Terminal receive task inner exception : " + exi.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    }
                }
                catch (Exception ex)
                {
                    eventLogInformationTransfer.WriteEntry("Terminal receive task " + tr.Id.ToString() + " cannot stop successfully : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
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

                    eventLogInformationTransfer.WriteEntry("DTU send task " + ts.Id.ToString() + " stopped.");
                }
                catch (AggregateException ex)
                {
                    eventLogInformationTransfer.WriteEntry("DTU send task " + ts.Id.ToString() + " cannot stop successfully : (AggregateException) " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    foreach (Exception exi in ex.InnerExceptions)
                    {
                        eventLogInformationTransfer.WriteEntry("DTU send task inner exception : " + exi.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    }
                }
                catch (Exception ex)
                {
                    eventLogInformationTransfer.WriteEntry("DTU send task " + ts.Id.ToString() + " cannot stop successfully : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                }
                try
                {
                    tr.Wait(Consts.TASK_STOP_WAIT_TIME);//, _termCts.Token);

                    eventLogInformationTransfer.WriteEntry("DTU receive task " + tr.Id.ToString() + " stopped.");
                }
                catch (AggregateException ex)
                {
                    eventLogInformationTransfer.WriteEntry("DTU receive task " + tr.Id.ToString() + " cannot stop successfully : (AggregateException) " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    foreach (Exception exi in ex.InnerExceptions)
                    {
                        eventLogInformationTransfer.WriteEntry("DTU receive task inner exception : " + exi.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
                    }
                }
                catch (Exception ex)
                {
                    eventLogInformationTransfer.WriteEntry("DTU receive task " + tr.Id.ToString() + " cannot stop successfully : " + ex.Message + "\n\n" + ex.StackTrace, EventLogEntryType.Error);
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

        #endregion
    }
}
