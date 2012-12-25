using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
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

namespace InformationTransferLibrary
{
    public class Consts
    {
        #region Communication codes between server and management system

        public const string TERM_TEST_CONN = "#010001";
        public const string TERM_PULSE_REQ = "#010002";
        public const string TERM_GET_ALL_DTU = "#010003";
        public const string TERM_ADD_DTU = "#010004";
        //public const string TERM_TRANS_REQ = "#010005";
        public const string TERM_DELETE_DTU = "#010006";
        public const string TERM_LOGIN = "#010007";
        public const string TERM_LOGOUT = "#010008";
        public const string TERM_INIT_USER = "#010009";

        public const string TERM_TEST_CONN_OK = "#020001";
        public const string TERM_PULSE_REQ_OK = "#020002";
        public const string TERM_GET_ALL_DTU_OK = "#020003";
        public const string TERM_ADD_DTU_OK = "#020004";
        //public const string TERM_TRANS_REQ_OK = "#020005";
        public const string TERM_DELETE_DTU_OK = "#020006";
        public const string TERM_LOGIN_OK = "#020007";
        public const string TERM_LOGOUT_OK = "#020008";
        public const string TERM_INIT_USER_OK = "#020009";

        public const string TERM_INVALID_REQUEST = "#030000";
        public const string TERM_GET_ALL_DTU_ERR = "#030003";
        public const string TERM_ADD_DTU_ERR = "#030004";
        //public const string TERM_TRANS_REQ_ERR = "#030005";
        public const string TERM_DELETE_DTU_ERR = "#030006";
        public const string TERM_LOGIN_ERR = "#030007";
        public const string TERM_LOGOUT_ERR = "#030008";
        public const string TERM_INIT_USER_ERR = "#030009";

        public const string MAN_TEST_CONN = "#040001";
        //public const string MAN_PULSE_REQ = "#040002";
        public const string MAN_GET_ALL_DTU = "#040003";
        public const string MAN_ADD_DTU = "#040004";
        public const string MAN_DELETE_DTU = "#040005";
        public const string MAN_MODIFY_DTU = "#040006";
        public const string MAN_GET_ALL_USER = "#040007";
        public const string MAN_ADD_USER = "#040008";
        public const string MAN_DELETE_USER = "#040009";
        //public const string MAN_MODIFY_USER = "#040010";
        //public const string MAN_LOOP_STATE = "#040011";
        public const string MAN_LOGIN = "#040012";
        public const string MAN_LOGOUT = "#040013";
        //public const string MAN_GET_DTU = "#040014";
        //public const string MAN_KICK_MAN = "#040015";
        public const string MAN_KICK_USER = "#040016";
        public const string MAN_KICK_DTU = "#040017";
        public const string MAN_UNCTRL_DTU = "#040018";

        public const string MAN_TEST_CONN_OK = "#050001";
        //public const string MAN_PULSE_REQ_OK = "#050002";
        public const string MAN_GET_ALL_DTU_OK = "#050003";
        public const string MAN_ADD_DTU_OK = "#050004";
        public const string MAN_DELETE_DTU_OK = "#050005";
        public const string MAN_MODIFY_DTU_OK = "#050006";
        public const string MAN_GET_ALL_USER_OK = "#050007";
        public const string MAN_ADD_USER_OK = "#050008";
        public const string MAN_DELETE_USER_OK = "#050009";
        //public const string MAN_MODIFY_USER_OK = "#0500010";
        //public const string MAN_LOOP_STATE_OK = "#050011";
        public const string MAN_LOGIN_OK = "#050012";
        public const string MAN_LOGOUT_OK = "#050013";
        //public const string MAN_KICK_MAN_OK = "#050015";
        public const string MAN_KICK_USER_OK = "#050016";
        public const string MAN_KICK_DTU_OK = "#050017";
        //public const string MAN_GET_DTU_OK = "#050014";
        public const string MAN_UNCTRL_DTU_OK = "#050018";

        public const string MAN_INVALID_REQUEST = "#060000";
        public const string MAN_GET_ALL_DTU_ERR = "#060003";
        public const string MAN_ADD_DTU_ERR = "#060004";
        public const string MAN_DELETE_DTU_ERR = "#060005";
        public const string MAN_MODIFY_DTU_ERR = "#060006";
        public const string MAN_GET_ALL_USER_ERR = "#060007";
        public const string MAN_ADD_USER_ERR = "#060008";
        public const string MAN_DELETE_USER_ERR = "#060009";
        //public const string MAN_MODIFY_USER_ERR = "#060010";
        //public const string MAN_LOOP_STATE_ERR = "#060011";
        public const string MAN_LOGIN_ERR = "#060012";
        public const string MAN_LOGOUT_ERR = "#060013";
        //public const string MAN_KICK_MAN_ERR = "#060015";
        public const string MAN_KICK_USER_ERR = "#060016";
        public const string MAN_KICK_DTU_ERR = "#060017";
        //public const string MAN_GET_DTU_ERR = "#060014";
        public const string MAN_UNCTRL_DTU_ERR = "#060018";

        #endregion

        public const int EVENT_ID_COMMON = 0;
        public const int EVENT_ID_FROM_TERM = 1;
        public const int EVENT_ID_TO_TERM = 2;
        public const int EVENT_ID_FROM_DTU = 3;
        public const int EVENT_ID_TO_DTU = 4;
        public const int EVENT_ID_FROM_MAN = 5;
        public const int EVENT_ID_TO_MAN = 5;

        public const int SERVER_MESSAGE_PROCESSING_TASK = 0;
        public const int SERVER_MESSAGE_SENDING_TASK = 1;
        public const int SERVER_MESSAGE_RECEIVING_TASK = 2;

        /// <summary>
        /// For system service management
        /// </summary>
        public const int MAN_PORT = 5081;
        /// <summary>
        /// For local terminal
        /// </summary>
        public const int TERM_PORT = 5082;
        /// <summary>
        /// For remote terminal
        /// </summary>
        public const int DTU_PORT = 5083;

        public const int MIN_PORT_NUMBER = 5000;
        public const int MAX_PORT_NUMBER = 65534;

        public const int SOCKET_RECEIVING_BUFFER_LENGTH = 1024;
        public const int SOCKET_RECEIVING_BUFFER_LENGTH_MAN = 1024 * 128;
        public const int SOCKET_LISTEN_BACKLOG_COUNT = 100;

        public const int DTU_CFG_TIMEOUT = 15000; // ms
        public const int MAN_TIMEOUT = 30000; // ms
        public const int TERM_TIMEOUT = 30000; // ms
        public const int DTU_TIMEOUT = 90000; // ms

        public const int MIN_TIME_OUT = 10000;

        public const int TERM_TASK_REQUEST_SLEEP_TIME = 500; // ms
        public const int TERM_TASK_TIMER_START_DELAY_TIME = 0; //ms
        //public const int TERM_TASK_TIMER_INTERVAL = 10000; //ms
        public const int TERM_TASK_TIMER_PULSE = 5000; //ms

        public const int MAN_TASK_TIMER_START_DELAY_TIME = 0; //ms
        public const int MAN_TASK_TIMER_INTERVAL = 3000; //ms

        public const int TASK_STOP_WAIT_TIME = 0; //ms

        public const int DTU_TO_SERVER_PULSE_INTERVAL_S = 30; //s
        public const int SERVER_TO_DTU_RESPONSE_INTERVAL_S = 30; //s
        public const int DTU_PULSE_CONTENT_LENGTH_MAX = 16;
        public const int DTU_REGISTER_CONTENT_LENGTH = 7;

        public const int MAX_LOG_DISPLAY_COUNT = 1000;
        public const int MIN_MAX_LOG_DISPLAY_COUNT = 100;
        public const int MAX_LOG_COUNT = 10000;
        public const int MIN_MAX_LOG_COUNT = 1000;

        public const int PROTOCOL_HEADER_LENGTH = 7;

        public const int TASK_THREAD_SLEEP_INTERVAL = 100;
        public const int TASK_DTU_THREAD_SLEEP_INTERVAL = 500;

        public const string LOGIN_ADMIN_USERNAME = "admin";
        public const string LOGIN_ADMIN_PASSWORD = "service";
        public const string LOGIN_ADMIN_PERMISSION = "0";

        public const int USER_NAME_MIN_LENGTH = 3;
        public const int USER_PASSWORD_MIN_LENGTH = 6;
        public const int USER_NAME_PASSWORD_MAX_LENGTH = 12;

        public const int DTU_CONFIG_MIN_LENGTH = 1;
        public const int DTU_CONFIG_LENGTH = 7;
        public const int DTU_CONFIG_MAX_LENGTH = 50;

        public const int USER_ACCOUNT_ITEM_COUNT = 3;
        public const int USER_INFO_ITEM_COUNT = 5;
        public const int DTU_ACCOUNT_ITEM_COUNT = 4;
        public const int DTU_INFO_ITEM_COUNT = 7;

        public const string DEFAULT_DIRECTORY = @"C:\COMWAY";
    }

    [ValueConversion(typeof(string), typeof(int))]
    public class StringIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
        CultureInfo culture)
        {
            int intValue = (int)value;
            return intValue.ToString();
        }
        public object ConvertBack(object value, Type targetType, object parameter,
        CultureInfo culture)
        {
            string strValue = (string)value;
            return int.Parse(strValue);
        }
    }

    [ValueConversion(typeof(int), typeof(string))]
    public class IntStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
        CultureInfo culture)
        {
            string strValue = (string)value;
            return int.Parse(strValue);
        }
        public object ConvertBack(object value, Type targetType, object parameter,
        CultureInfo culture)
        {
            int intValue = (int)value;
            return intValue.ToString();
        }
    }

    public class EncryptDecrypt
    {
        public const string KEY = "comwayit";

        /// <summary>
        /// DES Encrypt
        /// </summary>
        /// <param name="pToEncrypt">string to be encrypted</param>
        /// <param name="sKey">key, length must be 8</param>
        /// <returns>Encrypted string in the format of Base64</returns>
        public static string Encrypt(string pToEncrypt, string sKey = EncryptDecrypt.KEY)
        {
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                byte[] inputByteArray = Encoding.UTF8.GetBytes(pToEncrypt);
                des.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
                des.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                    cs.Close();
                }
                string str = Convert.ToBase64String(ms.ToArray());
                ms.Close();
                return str;
            }
        }

        /// <summary>
        /// DES Decrypt
        /// </summary>
        /// <param name="pToDecrypt">string in the format of Base64 to be decrypt</param>
        /// <param name="sKey">key, length must be 8</param>
        /// <returns>decrypted string</returns>
        public static string Decrypt(string pToDecrypt, string sKey = EncryptDecrypt.KEY)
        {
            byte[] inputByteArray = Convert.FromBase64String(pToDecrypt);
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                des.Key = ASCIIEncoding.ASCII.GetBytes(sKey);
                des.IV = ASCIIEncoding.ASCII.GetBytes(sKey);
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                    cs.Close();
                }
                string str = Encoding.UTF8.GetString(ms.ToArray());
                ms.Close();
                return str;
            }
        }
    }

    public class Helper
    {
        public enum CheckMethod
        {
            None,
            Char,
            Num,
            Tel,
            CharNum,
            CharNumBlank
        }

        public static bool CheckValidChar(string s, CheckMethod cm = CheckMethod.None)
        {
            if (string.IsNullOrWhiteSpace(s))
                return false;

            string sm = "";
            switch (cm)
            {
                default:
                case CheckMethod.None:
                    return true;
                case CheckMethod.CharNum:
                    sm = "[0-9a-zA-Z]";
                    break;
                case CheckMethod.CharNumBlank:
                    sm = "[0-9a-zA-Z ]";
                    break;
                case CheckMethod.Char:
                    sm = "[a-zA-Z]";
                    break;
                case CheckMethod.Num:
                    sm = "[0-9.]";
                    break;
                case CheckMethod.Tel:
                    sm = "[0-9-()]";
                    break;
            }
            Regex reg = new Regex(sm);
            int len = s.Length;
            for (int i = 0; i < len; i++)
            {
                string c = s.Substring(i, 1);
                if (reg.Match(c).Success == false)
                    return false;
            }
            return true;
        }

        public static byte[] ComposeResponseBytes(string header, string str)
        {
            if (string.IsNullOrWhiteSpace(header))
                header = "";
            header = header.Trim(new char[] { '\0' });
            if (string.IsNullOrWhiteSpace(header))
                str = "";
            str = str.Trim(new char[] { '\0' });
            return ComposeResponseBytes(System.Text.Encoding.ASCII.GetBytes(header), System.Text.Encoding.ASCII.GetBytes(str));
        }

        public static byte[] ComposeResponseBytes(string header, byte[] ba)
        {
            if (string.IsNullOrWhiteSpace(header))
                header = "";
            header = header.Trim();
            return ComposeResponseBytes(System.Text.Encoding.ASCII.GetBytes(header), ba);
        }

        public static int GetValidByteLength(byte[] ba)
        {
            if (ba == null)
                return 0;
            for (int i = 0; i < ba.Length; i++)
            {
                if (ba[i] == '\0')
                    return i;
            }
            return ba.Length;
        }

        public static byte[] ComposeResponseBytes(byte[] bah, byte[] ba)
        {
            if (bah == null)
                bah = new byte[] { };
            if (ba == null)
                ba = new byte[] { };
            int len = GetValidByteLength(bah) + GetValidByteLength(ba);
            byte[] br = new byte[len];
            for (int i = 0; i < bah.Length; i++)
            {
                br[i] = bah[i];
            }
            for (int i = 0; i < len - bah.Length; i++)
            {
                br[i + bah.Length] = ba[i];
            }
            return br;
        }

        public static UserInfo FindUserInfo(string username, ObservableCollection<UserInfo> uiOC)
        {
            if (string.IsNullOrWhiteSpace(username) || uiOC == null)
                return null;
            foreach (UserInfo uii in uiOC)
            {
                if (string.Compare(username.Trim(), uii.UserName, true) == 0)
                    return uii;
            }
            return null;
        }

        public static UserInfo FindUserInfo(Socket soc, ObservableCollection<UserInfo> uiOC)
        {
            if (soc == null || uiOC == null)
                return null;
            foreach (UserInfo uii in uiOC)
            {
                if (soc == uii.UserSocket)
                    return uii;
            }
            return null;
        }

        public static UserInfo FindUserInfoFromTermSocket(Socket soc, ObservableCollection<UserInfo> uiOC)
        {
            if (soc == null || uiOC == null)
                return null;
            foreach (UserInfo uii in uiOC)
            {
                foreach (TerminalDTUTaskInformation tdti in uii.UserTermSockets)
                {
                    if (tdti.TaskSocket == soc)
                        return uii;
                }
            }
            return null;
        }

        public static DTUInfo FindDTUInfo(string dtuId, ObservableCollection<DTUInfo> uiOC)
        {
            if (string.IsNullOrWhiteSpace(dtuId) || uiOC == null)
                return null;
            foreach (DTUInfo dii in uiOC)
            {
                if (string.Compare(dtuId.Trim(), dii.DtuId.Trim(), true) == 0)
                    return dii;
            }
            return null;
        }

        public static DTUInfo FindDTUInfo(Socket soc, ObservableCollection<DTUInfo> uiOC, object _lock = null)
        {
            if (soc == null || uiOC == null)
                return null;
            foreach (DTUInfo dii in uiOC)
            {
                if (soc == dii.DTUSocket)
                    return dii;
            }
            return null;
        }

        public static void FillDTUInfoOC(ObservableCollection<DTUInfo> dtuInfoOc, string content, bool clear = true)
        {
            if (clear == true)
                dtuInfoOc.Clear();
            if (string.IsNullOrWhiteSpace(content))
                return;
            content = content.Trim(new char[] { '\0' });
            string[] sa = content.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (sa == null || sa.Length < 1)
                return;
            foreach (string sai in sa)
            {
                string[] saia = sai.Split(new string[] { "\t" }, StringSplitOptions.None);
                if (saia == null || (saia.Length != Consts.DTU_INFO_ITEM_COUNT))
                    continue;
                bool online = false;
                if (string.Compare(saia[4].Trim(), "y", true) == 0)
                    online = true;
                DTUInfo dtui = new DTUInfo()
                {
                    DtuId = saia[0].Trim(),
                    SimId = saia[1].Trim(),
                    UserName = saia[2].Trim(),
                    UserTel = saia[3].Trim(),
                    Online = online,
                    DtLogString = saia[5].Trim(),
                    Controller = (string.IsNullOrWhiteSpace(saia[6].Trim()) ?
                    null :
                    new UserInfo()
                    {
                        UserName = saia[6].Trim()
                    })
                };
                if (FindDTUInfo(saia[0].Trim(), dtuInfoOc) == null)
                    dtuInfoOc.Add(dtui);
            }
        }

        public static TerminalInformation FindTermInfo(TreeViewItem tvi, ObservableCollection<TerminalInformation> tiOc)
        {
            if (tvi == null || tiOc == null)
                return null;
            foreach (TerminalInformation tii in tiOc)
            {
                if (tvi == tii.CurrentTvItem)
                    return tii;
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="soc"></param>
        /// <param name="cmd"></param>
        /// <returns>data with exactly received data length</returns>
        public static byte[] DoSendReceive(Socket soc, string cmd, bool doRec = true)
        {
            soc.Send(Encoding.ASCII.GetBytes(cmd));
            if (doRec == false)
                return null;
            byte[] bytes = new byte[Consts.SOCKET_RECEIVING_BUFFER_LENGTH];
            int length = soc.Receive(bytes);
            if (length >= bytes.Length)
                return bytes;
            byte[] bytesret = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytesret[i] = bytes[i];
            }
            return bytesret;
        }

        public static void SafeCloseSocket(Socket soc)
        {
            if (soc != null)
            {
                try
                {
                    soc.Shutdown(SocketShutdown.Both);
                }
                catch (Exception) { }
                try
                {
                    soc.Disconnect(false);
                }
                catch (Exception) { }
                try
                {
                    soc.Close();
                }
                catch (Exception) { }
                try
                {
                    soc.Dispose();
                }
                catch (Exception) { }
            }
        }

        public static void SafeCloseIOStream(StreamReader sr)
        {
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

        public static void SafeCloseIOStream(StreamWriter sw)
        {
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

        /// <summary>
        /// About the return : First string is header, second string is content, last string is content without '\0'.
        /// "\r", "\n", " " and "\t" won't be trimmed.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="len"></param>
        /// <returns> First string is header, second string is content, last string is conetnt without '\0'</returns>
        public static Tuple<string, byte[], string, string> ExtractSocketResponse(byte[] bytes, int len)
        {
            if (bytes == null || bytes.Length < 1 || len < Consts.PROTOCOL_HEADER_LENGTH)
                return null;

            byte[] bh = new byte[Consts.PROTOCOL_HEADER_LENGTH];
            byte[] bc = new byte[len - Consts.PROTOCOL_HEADER_LENGTH];
            for (int i = 0; i < Consts.PROTOCOL_HEADER_LENGTH; i++)
            {
                bh[i] = bytes[i];
            }
            string header = System.Text.Encoding.ASCII.GetString(bh, 0, Consts.PROTOCOL_HEADER_LENGTH);
            string content = "";
            string contentTrim = "";
            if (len > Consts.PROTOCOL_HEADER_LENGTH)
            {
                for (int i = 0; i < len - Consts.PROTOCOL_HEADER_LENGTH; i++)
                {
                    bc[i] = bytes[i + Consts.PROTOCOL_HEADER_LENGTH];
                }
                content = System.Text.Encoding.ASCII.GetString(bc, 0, len - Consts.PROTOCOL_HEADER_LENGTH);
                contentTrim = content.Trim(new char[] { '\0' });
            }
            return new Tuple<string, byte[], string, string>(header, bc, content, contentTrim);
        }
    }

    public class CommonEventArgs : EventArgs
    {
        public object CommonObject { get; set; }
    }

    public class NotifyPropertyChangedClass: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class UserInfo : NotifyPropertyChangedClass
    {
        private Socket _userSocket = null;
        /// <summary>
        /// Only coupled with Online
        /// </summary>
        public Socket UserSocket
        {
            get
            {
                return _userSocket;
            }
            set
            {
                if (Online == true)
                    _userSocket = value;
                else
                    _userSocket = null;
                NotifyPropertyChanged("UserSocket");
            }
        }

        private string _username = "";
        public string UserName
        {
            get
            {
                return _username;
            }
            set
            {
                _username = value;
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

        private string _permission = "2";
        public string Permission
        {
            get
            {
                return _permission;
            }
            set
            {
                _permission = value;
                if (_permission != "0" && _permission != "1" && _permission != "2")
                    _permission = "2";
                NotifyPropertyChanged("Permission");
                NotifyPropertyChanged("PermissionString");
            }
        }

        public string PermissionString
        {
            get
            {
                if (_permission == "0")
                    return "Super";
                else if (_permission == "1")
                    return "Manage";
                else
                    return "Common";
            }
        }

        private bool _displayIcon = true;
        public bool DisplayIcon
        {
            get
            {
                return _displayIcon;
            }
            set
            {
                _displayIcon = value;
            }
        }

        private bool _online = false;
        /// <summary>
        /// Only coupled with UserSocket
        /// </summary>
        public bool Online
        {
            get
            {
                return _online;
            }
            set
            {
                _online = value;
                if (DisplayIcon == true)
                {
                    _onlineImage = new BitmapImage();
                    _onlineImage.BeginInit();
                    if (_online == true)
                        _onlineImage.UriSource = new Uri("pack://application:,,,/informationtransferlibrary;component/resources/useronline.ico");
                    else
                        _onlineImage.UriSource = new Uri("pack://application:,,,/informationtransferlibrary;component/resources/useroffline.ico");
                    _onlineImage.EndInit();
                    NotifyPropertyChanged("OnlineImage");
                }
                if(_online == false)
                    UserSocket = null;
                NotifyPropertyChanged("Online");
                NotifyPropertyChanged("DtLog");
                NotifyPropertyChanged("DtLogString");
                NotifyPropertyChanged("Information");
            }
        }

        private BitmapImage _onlineImage = null;
        public BitmapImage OnlineImage
        {
            get
            {
                return _onlineImage;
            }
            set
            {
                _onlineImage = value;
                NotifyPropertyChanged("OnlineImage");
            }
        }

        private DateTime _dtLog = DateTime.Now;
        public DateTime DtLog
        {
            get
            {
                return _dtLog;
            }
            set
            {
                _dtLog = value;
                NotifyPropertyChanged("DtLog");
                NotifyPropertyChanged("DtLogString");
            }
        }

        public string DtLogString
        {
            get
            {
                if (Online == true)
                    return _dtLog.ToLongDateString() + " " + _dtLog.ToLongTimeString();
                else
                    return "";
            }
            set
            {
                DateTime dt;
                if (DateTime.TryParse(value, out dt) == false)
                    DtLog = DateTime.Now;
                else
                    DtLog = dt;
                NotifyPropertyChanged("DtLog");
                NotifyPropertyChanged("DtLogString");
            }
        }

        private string _information = "";
        public string Information
        {
            get
            {
                if (Online == true)
                    return _information;
                else
                    return "";
            }
            set
            {
                _information = value;
                NotifyPropertyChanged("Information");
            }
        }

        private List<TerminalDTUTaskInformation> _userTermSockets = new List<TerminalDTUTaskInformation>();
        /// <summary>
        /// All sockets related to the user
        /// </summary>
        public List<TerminalDTUTaskInformation> UserTermSockets
        {
            get
            {
                return _userTermSockets;
            }
        }

        private List<DTUInfo> _userDTUs = new List<DTUInfo>();
        /// <summary>
        /// All DTU sockets related to the user
        /// </summary>
        public List<DTUInfo> UserDTUs
        {
            get
            {
                return _userDTUs;
            }
        }
    }

    public class LogMessage : NotifyPropertyChangedClass
    {
        private int _index = 1;
        public int Index
        {
            get
            {
                return _index;
            }
            set
            {
                _index = value;
                NotifyPropertyChanged("Index");
                NotifyPropertyChanged("IndexString");
            }
        }

        public string IndexString
        {
            get
            {
                return _index.ToString();
            }
        }

        public enum State
        {
            None,
            Infomation,
            OK,
            Fail,
            Error
        }

        private State _stateType = State.None;
        public State StateType
        {
            get
            {
                return _stateType;
            }
            set
            {
                _stateType = value;
                _stateImage = new BitmapImage();
                _stateImage.BeginInit();
                switch (_stateType)
                {
                    default:
                    case State.None:
                        _stateImage.UriSource = new Uri("pack://application:,,,/informationtransferlibrary;component/resources/status_none.png");
                        _stateBackgound = new SolidColorBrush(Colors.Transparent);
                        break;
                    case State.Infomation:
                        _stateBackgound = new SolidColorBrush(Colors.LightBlue);
                        _stateImage.UriSource = new Uri("pack://application:,,,/informationtransferlibrary;component/resources/status_info.png");
                        break;
                    case State.OK:
                        _stateBackgound = new SolidColorBrush(Colors.LightGreen);
                        _stateImage.UriSource = new Uri("pack://application:,,,/informationtransferlibrary;component/resources/status_ok.png");
                        break;
                    case State.Fail:
                        _stateBackgound = new SolidColorBrush(Colors.Red);
                        _stateImage.UriSource = new Uri("pack://application:,,,/informationtransferlibrary;component/resources/status_error.png");
                        break;
                    case State.Error:
                        _stateBackgound = new SolidColorBrush(Colors.Yellow);
                        _stateImage.UriSource = new Uri("pack://application:,,,/informationtransferlibrary;component/resources/status_ques.png");
                        break;
                }
                _stateImage.EndInit();
                NotifyPropertyChanged("StateType");
                NotifyPropertyChanged("StateImage");
                NotifyPropertyChanged("StateBackground");
                NotifyPropertyChanged("StateBackgroundIndex");
                NotifyPropertyChanged("StateImagePath");
            }
        }

        public int StateBackgroundIndex
        {
            get
            {
                return (int)StateType;
            }
        }

        private SolidColorBrush _stateBackgound = new SolidColorBrush(Colors.Transparent);
        public SolidColorBrush StateBackground
        {
            get
            {
                return _stateBackgound;
            }
            set
            {
                _stateBackgound = value;
                NotifyPropertyChanged("StateBackground");
            }
        }

        private string StateImagePath
        {
            get
            {
                switch (_stateType)
                {
                    default:
                    case State.None:
                        return "";
                    case State.Infomation:
                        return "resources/status_info.png";
                    case State.OK:
                        return "resources/status_ok.png";
                    case State.Fail:
                        return "resources/status_error.png";
                    case State.Error:
                        return "resources/status_ques.png";
                }
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

        public enum Flow
        {
            None,
            Request,
            Response
        }

        private Flow _flowType = Flow.None;
        public Flow FlowType
        {
            get
            {
                return _flowType;
            }
            set
            {
                _flowType = value;
                _flowImage = new BitmapImage();
                _flowImage.BeginInit();
                switch (_flowType)
                {
                    default:
                    case Flow.None:
                        _flowImage.UriSource = new Uri("pack://application:,,,/informationtransferlibrary;component/resources/flownone.png");
                        break;
                    case Flow.Request:
                        _flowImage.UriSource = new Uri("pack://application:,,,/informationtransferlibrary;component/resources/flowrequest.png");
                        break;
                    case Flow.Response:
                        _flowImage.UriSource = new Uri("pack://application:,,,/informationtransferlibrary;component/resources/flowresponse.png");
                        break;
                }
                _flowImage.EndInit();
                NotifyPropertyChanged("FlowType");
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

        private string _ipAddr = "";
        public string IPAddr
        {
            get
            {
                return _ipAddr;
            }
            set
            {
                _ipAddr = value;
                NotifyPropertyChanged("IPAddr");
            }
        }

        private DateTime _timeStamp = DateTime.Now;
        public DateTime TimeStamp
        {
            get
            {
                return _timeStamp;
            }
            set
            {
                _timeStamp = value;
                NotifyPropertyChanged("TimeStamp");
                NotifyPropertyChanged("MsgDateTime");
            }
        }

        public string MsgDateTime
        {
            get
            {
                return TimeStamp.ToLongDateString() + " " + TimeStamp.ToLongTimeString();
            }
        }

        private string _msg = "";
        public string Message
        {
            get
            {
                return _msg;
            }
            set
            {
                _msg = value;
                NotifyPropertyChanged("Message");
            }
        }
    }

    public class DTUInfo : NotifyPropertyChangedClass
    {
        private string _dtuId = "";
        public string DtuId
        {
            get
            {
                return _dtuId;
            }
            set
            {
                _dtuId = value;
                NotifyPropertyChanged("DtuId");
            }
        }

        private string _simId = "";
        public string SimId
        {
            get
            {
                return _simId;
            }
            set
            {
                _simId = value;
                NotifyPropertyChanged("SimId");
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

        private string _userTel = "";
        public string UserTel
        {
            get
            {
                return _userTel;
            }
            set
            {
                _userTel = value;
                NotifyPropertyChanged("UserTel");
            }
        }

        private bool _displayIcon = true;
        public bool DisplayIcon
        {
            get
            {
                return _displayIcon;
            }
            set
            {
                _displayIcon = value;
            }
        }

        private bool _online = false;
        public bool Online
        {
            get
            {
                return _online;
            }
            set
            {
                _online = value;
                if (DisplayIcon == true)
                {
                    _onlineImage = new BitmapImage();
                    _onlineImage.BeginInit();
                    if (_online == true)
                        _onlineImage.UriSource = new Uri("pack://application:,,,/informationtransferlibrary;component/resources/dtuon.ico");
                    else
                        _onlineImage.UriSource = new Uri("pack://application:,,,/informationtransferlibrary;component/resources/dtuoff.ico");
                    _onlineImage.EndInit();
                    NotifyPropertyChanged("OnlineImage");
                }
                if (_online == false && Controller != null)
                    Controller = null;
                NotifyPropertyChanged("Online");
                NotifyPropertyChanged("DtLog");
                NotifyPropertyChanged("DtLogString");
                NotifyPropertyChanged("Information");
            }
        }

        private BitmapImage _onlineImage = null;
        public BitmapImage OnlineImage
        {
            get
            {
                return _onlineImage;
            }
            set
            {
                _onlineImage = value;
                NotifyPropertyChanged("OnlineImage");
            }
        }

        private DateTime _dtLog = DateTime.Now;
        public DateTime DtLog
        {
            get
            {
                return _dtLog;
            }
            set
            {
                _dtLog = value;
                NotifyPropertyChanged("DtLog");
                NotifyPropertyChanged("DtLogString");
            }
        }

        public string DtLogString
        {
            get
            {
                if (Online == true)
                    return _dtLog.ToLongDateString() + " " + _dtLog.ToLongTimeString();
                else
                    return "";
            }
            set
            {
                DateTime dt;
                if (DateTime.TryParse(value, out dt) == false)
                    DtLog = DateTime.Now;
                else
                    DtLog = dt;
                NotifyPropertyChanged("DtLog");
                NotifyPropertyChanged("DtLogString");
            }
        }

        /// <summary>
        /// Readonly
        /// </summary>
        public bool UnderControl
        {
            get
            {
                return (Controller == null) ? true : false;
            }
        }

        private BitmapImage _underControlImage = null;
        public BitmapImage UnderControlImage
        {
            get
            {
                return _underControlImage;
            }
            set
            {
                _underControlImage = value;
                NotifyPropertyChanged("UnderControlImage");
            }
        }

        private UserInfo _controller = null;
        public UserInfo Controller
        {
            get
            {
                return _controller;
            }
            set
            {
                if (Online == true)
                    _controller = value;
                else
                    _controller = null;
                if (DisplayIcon == true)
                {
                    //_underControlImage = new BitmapImage();
                    //_underControlImage.BeginInit();
                    //if (_controller == null)
                    //    _underControlImage.UriSource = new Uri("pack://application:,,,/informationtransferlibrary;component/resources/dtuoffline.ico");
                    //else
                    //    _underControlImage.UriSource = new Uri("pack://application:,,,/informationtransferlibrary;component/resources/dtuonline.ico");
                    //_underControlImage.EndInit();
                    //NotifyPropertyChanged("UnderControlImage");
                }
                NotifyPropertyChanged("Controller");
                NotifyPropertyChanged("UnderControl");
                NotifyPropertyChanged("ControllerName");
            }
        }

        /// <summary>
        /// Readonly
        /// </summary>
        public string ControllerName
        {
            get
            {
                if (Controller == null)
                    return "";
                else
                    return Controller.UserName;
            }
        }

        private Socket _dtuSocket = null;
        public Socket DTUSocket
        {
            get
            {
                return _dtuSocket;
            }
            set
            {
                _dtuSocket = value;
                NotifyPropertyChanged("DTUSocket");
            }
        }
    }

    public class TerminalInformationEventArgs : EventArgs
    {
        public TerminalInformation Instance { get; set; }
        public string Message { get; set; }
        public byte[] MessageByte { get; set; }
        public int MessageByteLength { get; set; }
    }

    public class TerminalInformation
    {
        public delegate void SocketStateChangeEventHandler(object sender, TerminalInformationEventArgs args);
        public SocketStateChangeEventHandler SocketStateChangeEvent;

        public delegate void MessageReceivedEventHandler(object sender, TerminalInformationEventArgs args);
        public MessageReceivedEventHandler MessageReceivedEvent;

        public void InitUI()
        {
            InitTVItem();
            InitTabItem();
            InitSocketTask();
            MessageReceivedEvent += new MessageReceivedEventHandler(TIUC.MessageReceivedEventHandler);
            SocketStateChangeEvent += new SocketStateChangeEventHandler(TIUC.SocketStateChangeEventHandler);
        }

        private void InitTVItem()
        {
            if (_curTvItem == null)
                _curTvItem = new TreeViewItem();
            //else
            //    return;

            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            sp.Margin = new Thickness(0, 0, 0, 0);//-4, -4, -4, -4);

            Label lbl = new Label();
            lbl.Content = CurrentDTU.DtuId;
            //lbl.FontWeight = FontWeights.Bold;
            sp.Children.Add(lbl);

            _curTvItem.Header = sp;

            Image img = new Image();
            switch (_state)
            {
                default:
                case TiState.Unknown:
                    img.Source = new BitmapImage(new Uri("pack://application:,,,/InformationTransferLibrary;component/resources/dtuunknown.ico"));
                    lbl.Foreground = Brushes.Red;
                    _curTvItem.ToolTip = CurrentDTU.DtuId + " : Unknown";
                    break;
                case TiState.Connected:
                    img.Source = new BitmapImage(new Uri("pack://application:,,,/InformationTransferLibrary;component/resources/dtuon.ico"));
                    lbl.Foreground = Brushes.Black;
                    _curTvItem.ToolTip = CurrentDTU.DtuId + " : Connected";
                    break;
                case TiState.Disconnected:
                    img.Source = new BitmapImage(new Uri("pack://application:,,,/InformationTransferLibrary;component/resources/dtuoff.ico"));
                    lbl.Foreground = Brushes.Red;
                    _curTvItem.ToolTip = CurrentDTU.DtuId + " : Disconnected";
                    break;
            }
            img.Width = 16;
            img.Height = 16;
            //img.Opacity = 0.75;
            sp.Children.Insert(0, img);
        }

        private void InitTabItem()
        {
            if (_curTabItem == null)
            {
                _curTabItem = new TabItem();

                _tiUc = new TerminalInformationUC(this);
                _curTabItem.Content = _tiUc;
            }
            //else
            //    return;

            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            sp.Margin = new Thickness(0, 0, 0, 0);//-4, -4, -4, -4);

            Label lbl = new Label();
            lbl.Content = CurrentDTU.DtuId;
            //lbl.FontWeight = FontWeights.Bold;
            sp.Children.Add(lbl);

            _curTabItem.Header = sp;

            Image img = new Image();
            switch (_state)
            {
                default:
                case TiState.Unknown:
                    img.Source = new BitmapImage(new Uri("pack://application:,,,/InformationTransferLibrary;component/resources/dtuunknown.ico"));
                    lbl.Foreground = Brushes.Red;
                    _curTvItem.ToolTip = CurrentDTU.DtuId + " : Unknown";
                    break;
                case TiState.Connected:
                    img.Source = new BitmapImage(new Uri("pack://application:,,,/InformationTransferLibrary;component/resources/dtuon.ico"));
                    lbl.Foreground = Brushes.Black;
                    _curTvItem.ToolTip = CurrentDTU.DtuId + " : Connected";
                    break;
                case TiState.Disconnected:
                    img.Source = new BitmapImage(new Uri("pack://application:,,,/InformationTransferLibrary;component/resources/dtuoff.ico"));
                    lbl.Foreground = Brushes.Red;
                    _curTvItem.ToolTip = CurrentDTU.DtuId + " : Disconnected";
                    break;
            }
            img.Width = 16;
            img.Height = 16;
            //img.Opacity = 0.75;
            sp.Children.Insert(0, img);
        }

        private void InitSocketTask()
        {
            Task ts = new Task(
                () =>
                {
                    DTUSendService();
                }, _cts.Token
            );
            Task tr = new Task(
                () =>
                {
                    DTUReceiveService();
                }, _cts.Token
            );
            ts.Start();
            tr.Start();
        }

        private TabItem _curTabItem = null;
        public TabItem CurrentTabItem
        {
            get
            {
                return _curTabItem;
            }
            set
            {
                _curTabItem = value;
            }
        }

        private IPAddress _serverIp = null;
        public IPAddress ServerIP
        {
            get
            {
                return _serverIp;
            }
            set
            {
                _serverIp = value;
            }
        }

        public string ServerIPString
        {
            get
            {
                if (ServerIP == null)
                    return "";
                else
                    return _serverIp.ToString();
            }
            set
            {
                IPAddress ipad = null;
                if (IPAddress.TryParse(value, out ipad) == false)
                    ServerIP = null;
                else
                    ServerIP = ipad;
            }
        }

        private IPAddress _terminalIp = null;
        public IPAddress TerminalIP
        {
            get
            {
                return _terminalIp;
            }
            set
            {
                _terminalIp = value;
            }
        }

        public string TerminalIPString
        {
            get
            {
                if (TerminalIP == null)
                    return "";
                else
                    return _terminalIp.ToString();
            }
            set
            {
                IPAddress ipad = null;
                if (IPAddress.TryParse(value, out ipad) == false)
                    TerminalIP = null;
                else
                    TerminalIP = ipad;
            }
        }

        private DTUInfo _curDTU = null;
        public DTUInfo CurrentDTU
        {
            get
            {
                return _curDTU;
            }
            set
            {
                _curDTU = value;
            }
        }

        private Socket _termSocket = null;
        public Socket TerminalSocket
        {
            get
            {
                return _termSocket;
            }
            set
            {
                _termSocket = value;
            }
        }

        private TreeViewItem _curTvItem = null;
        public TreeViewItem CurrentTvItem
        {
            get
            {
                return _curTvItem;
            }
            set
            {
                _curTvItem = value;
            }
        }

        private TerminalInformationUC _tiUc = null;
        public TerminalInformationUC TIUC
        {
            get
            {
                return _tiUc;
            }
            set
            {
                _tiUc = value;
            }
        }

        public enum TiState
        {
            Connected,
            Disconnected,
            Unknown
        }

        private TiState _state = TiState.Unknown;
        public TiState State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                InitTVItem();
                InitTabItem();
            }
        }

        private CancellationTokenSource _cts = new CancellationTokenSource();
        public CancellationTokenSource CTS
        {
            get
            {
                return _cts;
            }
        }

        private Task _taskSend = null;
        public Task TaskSend
        {
            get
            {
                return _taskSend;
            }
            set
            {
                _taskSend = value;
            }
        }

        private Task _taskReceive = null;
        public Task TaskReceive
        {
            get
            {
                return _taskReceive;
            }
            set
            {
                _taskReceive = value;
            }
        }

        private Queue<byte[]> _reqQueue = new Queue<byte[]>();
        public Queue<byte[]> ReqQueue
        {
            get
            {
                return _reqQueue;
            }
        }

        public void PutReq(string s)
        {
            if (s == null || s.Length <= 0)
                return;
            ReqQueue.Enqueue(System.Text.Encoding.ASCII.GetBytes(s));
        }

        private void DTUSendService()
        {
            string ip = ((IPEndPoint)_termSocket.RemoteEndPoint).Address.ToString();
            string message = "";

            while (_cts.Token.IsCancellationRequested == false &&
                _termSocket != null && _termSocket.Connected == true)
            {
                try
                {
                    if (ReqQueue.Count > 0)
                    {
                        byte[] ba = ReqQueue.Dequeue();
                        _termSocket.Send(ba);
                    }
                    else
                        Thread.Sleep(Consts.TASK_THREAD_SLEEP_INTERVAL);
                }
                catch (Exception ex)
                {
                    message = "Sending service exception : " + ex.Message;
                    break;
                }
            }

            State = TiState.Disconnected;

            SocketStateChangeEvent(this, new TerminalInformationEventArgs() { Instance = this, Message = message });
        }

        private void DTUReceiveService()
        {
            string ip = ((IPEndPoint)_termSocket.RemoteEndPoint).Address.ToString();
            byte[] ba = new byte[Consts.SOCKET_RECEIVING_BUFFER_LENGTH];
            int len = 0;
            string message = "";

            while (_cts.Token.IsCancellationRequested == false &&
                _termSocket != null && _termSocket.Connected == true)
            {
                try
                {
                    len = _termSocket.Receive(ba);
                    if (len < 1)
                    {
                        message = "Receiving service exception : empty message";
                        break;
                    }
                    else
                        MessageReceivedEvent(this, new TerminalInformationEventArgs() { MessageByte = ba, MessageByteLength = len });
                }
                catch (Exception ex)
                {
                    message = "Receiving service exception : " + ex.Message;
                    break;
                }
            }

            State = TiState.Disconnected;

            SocketStateChangeEvent(this, new TerminalInformationEventArgs() { Instance = this, Message = message });
        }
    }

    public class TerminalDTUTaskInformation
    {
        public Socket TaskSocket { get; set; }

        public CancellationTokenSource CTS { get; set; }

        public Task ReceiveTask { get; set; }

        public Task SendTask { get; set; }

        private Queue<byte[]> _requestQueue = new Queue<byte[]>();
        public Queue<byte[]> RequestQueue
        {
            get
            {
                return _requestQueue;
            }
        }

        /// <summary>
        /// For terminal & dtu
        /// </summary>
        public UserInfo Controller { get; set; }

        /// <summary>
        /// For dtu only
        /// </summary>
        public DTUInfo CurrentDTU { get; set; }
    }

    public class SentReceivedItem :NotifyPropertyChangedClass
    {
        private string _index = "";
        public string Index
        {
            get
            {
                return _index;
            }
            set
            {
                _index = value;
                NotifyPropertyChanged("Index");
            }
        }

        private DateTime _curDateTime = DateTime.Now;
        public DateTime CurrentDateTime
        {
            get
            {
                return _curDateTime;
            }
            set
            {
                _curDateTime = value;
                NotifyPropertyChanged("CurrentDateTime");
            }
        }

        private string _timeStamp = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();
        public string TimeStamp
        {
            get
            {
                return _timeStamp;
            }
            set
            {
                _timeStamp = value;
                NotifyPropertyChanged("TimeStamp");
            }
        }

        private string _sent = "";
        public string Sent
        {
            get
            {
                return _sent;
            }
            set
            {
                _sent = value;
                NotifyPropertyChanged("Sent");
            }
        }

        private byte[] _receivedBytes = null;
        public byte[] ReceivedBytes
        {
            get
            {
                return _receivedBytes;
            }
            set
            {
                _receivedBytes = value;
                if (_receivedBytes != null)
                {
                    ReceivedBytesString = System.Text.ASCIIEncoding.ASCII.GetString(_receivedBytes);
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in _receivedBytes)
                    {
                        if (sb.Length > 0)
                            sb.Append(" ");
                        sb.Append(b.ToString());
                    }
                    ReceivedBytesRawString = sb.ToString();
                }
                else
                {
                    ReceivedBytesString = "";
                    ReceivedBytesRawString = "";
                }
                NotifyPropertyChanged("ReceivedBytes");
                NotifyPropertyChanged("ReceivedBytesRawString");
                NotifyPropertyChanged("ReceivedBytesString");
            }
        }

        private string _receivedBytesRawString = null;
        public string ReceivedBytesRawString
        {
            get
            {
                return _receivedBytesRawString;
            }
            set
            {
                _receivedBytesRawString = value;
                NotifyPropertyChanged("ReceivedBytesRawString");
            }
        }

        private string _receivedBytesString = null;
        public string ReceivedBytesString
        {
            get
            {
                return _receivedBytesString;
            }
            set
            {
                _receivedBytesString = value;
                NotifyPropertyChanged("ReceivedBytesString");
            }
        }

    }
}
