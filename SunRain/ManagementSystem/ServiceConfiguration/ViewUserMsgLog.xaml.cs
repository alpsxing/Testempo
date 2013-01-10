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

using System.Windows.Forms;

using InformationTransferLibrary;

namespace ServiceConfiguration
{
    /// <summary>
    /// Interaction logic for ViewUserMsgLog.xaml
    /// </summary>
    public partial class ViewUserMsgLog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<Tuple<string,string,string>> _userDateOc;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private ObservableCollection<LogFile> _logFileOc = new ObservableCollection<LogFile>();
        private ObservableCollection<LogFileMessage> _emptyLogMessageOc = new ObservableCollection<LogFileMessage>();

        private bool _bInNormalClose = false;

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
                NotifyPropertyChanged("ServerIP");
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

        private string _readyString = "";
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

        public ViewUserMsgLog(string serverIp, int serverWebPort, ObservableCollection<Tuple<string, string, string>> userDateOc)
        {
            InitializeComponent();

            ServerIP = serverIp;
            ServerWebPort = serverWebPort;
            _userDateOc = userDateOc;

            DataContext = this;
        }

        public void DownloadFile(Tuple<string, string, string> ti)
        {
            Dispatcher.Invoke((ThreadStart)delegate()
            {
                TreeViewItem tvi = new TreeViewItem();

                string fileName = ti.Item1 + " " + ti.Item2 + " " + ti.Item3 + ".log";

                ReadyString = "获取日志文件 : " + fileName;

                StackPanel splf = new StackPanel();
                splf.Orientation = System.Windows.Controls.Orientation.Horizontal;
                splf.Margin = new Thickness(0, 0, 0, 0);
                System.Windows.Controls.Label lbllf = new System.Windows.Controls.Label();
                lbllf.Content = fileName;
                splf.Children.Add(lbllf);
                tvi.Header = splf;

                ObservableCollection<LogFileMessage> lfmOc = new ObservableCollection<LogFileMessage>();

                try
                {
                    System.Net.HttpWebRequest Myrq = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create("http://" + ServerIP + ":" + ServerWebPort.ToString() + "/service/message/" + fileName);
                    System.Net.HttpWebResponse myrp = (System.Net.HttpWebResponse)Myrq.GetResponse();
                    long totalBytes = myrp.ContentLength;
                    //Dispatcher.Invoke((ThreadStart)delegate()
                    //{
                        if (pbDownloadFile != null)
                            pbDownloadFile.Maximum = (int)totalBytes;
                    //}, null);
                    System.IO.Stream st = myrp.GetResponseStream();
                    long totalDownloadedByte = 0;
                    byte[] by = new byte[1024];
                    int osize = -1;
                    string sf = "";
                    while ((osize = st.Read(by, 0, (int)by.Length)) > 0 && _cts.Token.IsCancellationRequested == false)
                    {
                        sf = sf + System.Text.ASCIIEncoding.ASCII.GetString(by, 0, osize);
                        totalDownloadedByte = osize + totalDownloadedByte;
                        //Dispatcher.Invoke((ThreadStart)delegate()
                        //{
                            if (pbDownloadFile != null)
                                pbDownloadFile.Value = ((int)totalDownloadedByte > (int)totalBytes) ? (int)totalBytes : (int)totalDownloadedByte;
                        //}, null);
                    }
                    //Dispatcher.Invoke((ThreadStart)delegate()
                    //{
                        if (pbDownloadFile != null)
                            pbDownloadFile.Value = 0;
                    //}, null);
                    st.Close();

                    #region

                    string[] sa = sf.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string sai in sa)
                    {
                        string[] saia = sai.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                        if (saia != null && saia.Length == 5)
                        {
                            lfmOc.Add(new LogFileMessage()
                            {
                                UserName = saia[0],
                                DTUID = saia[1],
                                FlowType = (string.Compare(saia[2].Trim(), "true", true) == 0) ? LogFileMessage.Flow.ToDTU : LogFileMessage.Flow.FromDTU,
                                TimeStamp = saia[3],
                                Message = saia[4]
                            });
                        }
                    }

                    #endregion

                    Image imglf = new Image();
                    imglf.Source = new BitmapImage(new Uri("pack://application:,,,/serviceconfiguration;component/resources/logfileok.ico"));
                    imglf.Width = 16;
                    imglf.Height = 16;
                    splf.Children.Insert(0, imglf);
                }
                catch (System.Exception ex)
                {
                    lbllf.Content = fileName + " - " + ex.Message;

                    Image imglf = new Image();
                    imglf.Source = new BitmapImage(new Uri("pack://application:,,,/serviceconfiguration;component/resources/logfileerror.ico"));
                    imglf.Width = 16;
                    imglf.Height = 16;
                    splf.Children.Insert(0, imglf);
                }

                _logFileOc.Add(new LogFile()
                {
                    FileName = fileName,
                    TVI = tvi,
                    LogFileMessageOC = lfmOc
                });
            }, null);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title = "查看用户与DTU交互信息 - " + ServerIP;

            ReadyString = "获取日志文件中...";

            Task.Factory.StartNew(() =>
            {
                foreach (Tuple<string, string, string> ti in _userDateOc)
                {
                    if (_cts.Token.IsCancellationRequested == true)
                        break;
                    DownloadFile(ti);
                }
                Dispatcher.Invoke((ThreadStart)delegate()
                {
                    if (pbDownloadFile != null)
                        pbDownloadFile.Value = 0;
                    foreach (LogFile lfi in _logFileOc)
                    {
                        tvUserDate.Items.Add(lfi.TVI);
                    }
                }, null);
                ReadyString = "就绪";
            }, _cts.Token);
        }

        #region Window Exit

        private void Window_Exit(object sender, RoutedEventArgs e)
        {
            if (System.Windows.MessageBox.Show("确认退出\"查看用户与DTU交互信息\"?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _bInNormalClose = true;

            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_bInNormalClose == false)
            {
                if (System.Windows.MessageBox.Show("确认退出\"查看用户与DTU交互信息\"?", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    e.Cancel = true;
            }

            if (e.Cancel == false)
                _cts.Cancel();

            base.OnClosing(e);
        }

        #endregion

        private void TVUserDate_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (tvUserDate.Items.Count < 1 || tvUserDate.SelectedItem == null)
            {
                dgMessage.DataContext = _emptyLogMessageOc;
                return;
            }
            TreeViewItem tvi = tvUserDate.SelectedItem as TreeViewItem;
            if (tvi == null)
            {
                dgMessage.DataContext = _emptyLogMessageOc;
                return;
            }

            foreach (LogFile lfi in _logFileOc)
            {
                if (lfi.TVI == tvi)
                {
                    dgMessage.DataContext = lfi.LogFileMessageOC;
                    return;
                }
            }

            dgMessage.DataContext = _emptyLogMessageOc;
            return;
        }

        private void SaveLog_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDlg = new FolderBrowserDialog();
            folderDlg.RootFolder = Environment.SpecialFolder.MyComputer;
            if (folderDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            string s = folderDlg.SelectedPath;
            string msg = "";
            bool hasFile = false;
            foreach (LogFile lfi in _logFileOc)
            {
                if (File.Exists(s + lfi.FileName) == true)
                {
                    hasFile = true;
                    break;
                }
            }
            if (hasFile == true &&
                System.Windows.MessageBox.Show("请确认此目录下的同名文件将被覆盖 :\n" + s, "确认", 
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            foreach (LogFile lfi in _logFileOc)
            {
                try
                {
                    StreamWriter sw = new StreamWriter(s + lfi.FileName);
                    StringBuilder sb = new StringBuilder();
                    foreach (LogFileMessage lfmi in lfi.LogFileMessageOC)
                    {
                        sb.Append(lfmi.UserName + "\t" +
                            lfmi.DTUID + "\t" +
                            ((lfmi.FlowType == LogFileMessage.Flow.ToDTU) ? "True" : "False") + "\t" +
                            lfmi.TimeStamp + "\t" +
                            lfmi.Message);
                    }
                    sw.WriteLine(sb.ToString());
                    sw.Flush();
                    sw.Close();
                    sw.Dispose();
                }
                catch (Exception)
                {
                    if (msg == "")
                        msg = lfi.FileName;
                    else
                        msg = msg + "\n" + lfi.FileName;
                }
            }
            if (msg != "")
                System.Windows.MessageBox.Show("以下日志文件没有被正确保存 :\n" + msg, "保存日志错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public class LogFile : NotifyPropertyChangedClass
    {
        private string _fileName = "";
        public string FileName
        {
            get
            {
                return _fileName;
            }
            set
            {
                _fileName = value;
                NotifyPropertyChanged("FileName");
            }
        }

        private TreeViewItem _tvi = null;
        public TreeViewItem TVI
        {
            get
            {
                return _tvi;
            }
            set
            {
                _tvi = value;
            }
        }

        private ObservableCollection<LogFileMessage> _lfmOc = null;
        public ObservableCollection<LogFileMessage> LogFileMessageOC
        {
            get
            {
                return _lfmOc;
            }
            set
            {
                _lfmOc = value;
            }
        }
    }

    public class LogFileMessage : NotifyPropertyChangedClass
    {
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

        private string _dtuID = "";
        public string DTUID
        {
            get
            {
                return _dtuID;
            }
            set
            {
                _dtuID = value;
                NotifyPropertyChanged("DTUID");
            }
        }

        public enum Flow
        {
            ToDTU,
            FromDTU
        }

        private Flow _flowType = Flow.ToDTU;
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
                    case Flow.ToDTU:
                        _flowImage.UriSource = new Uri("pack://application:,,,/serviceconfiguration;component/resources/todtu.ico");
                        break;
                    case Flow.FromDTU:
                        _flowImage.UriSource = new Uri("pack://application:,,,/serviceconfiguration;component/resources/fromdtu.ico");
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

        private string _timeStamp = "";
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
            }
        }
    }
}
