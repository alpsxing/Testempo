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
        private ObservableCollection<Tuple<Tuple<string, string, string>, bool, TreeViewItem>> _userDateTVIOc = new ObservableCollection<Tuple<Tuple<string, string, string>, bool, TreeViewItem>>();

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

        public ViewUserMsgLog(string serverIp, ObservableCollection<Tuple<string,string,string>> userDateOc)
        {
            InitializeComponent();

            ServerIP = serverIp;
            _userDateOc = userDateOc;

            DataContext = this;
        }

        public void DownloadFile(Tuple<string,string,string> ti)
        {
            TreeViewItem tvi = new TreeViewItem();
            try
            {
                string fileName = ti.Item1 + " " + ti.Item2 + " " + ti.Item3 + ".log";
                
                ReadyString = "获取日志文件 : " + fileName;

                //StackPanel spSimId = new StackPanel();
                //spSimId.Orientation = Orientation.Horizontal;
                //spSimId.Margin = new Thickness(0, 0, 0, 0);
                //Label lblSimId = new Label();
                //lblSimId.Content = "SIM ID :" + CurrentDTU.SimId;
                //spSimId.Children.Add(lblSimId);
                //tviSimId.Header = spSimId;
                //Image imgSimId = new Image();
                //imgSimId.Source = new BitmapImage(new Uri("pack://application:,,,/InformationTransferLibrary;component/resources/simid.ico"));
                //imgSimId.Width = 16;
                //imgSimId.Height = 16;
                //spSimId.Children.Insert(0, imgSimId);
                //_curTvItem.Items.Add(tviSimId);


                System.Net.HttpWebRequest Myrq = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create("http://" + ServerIP + "/comway/service/message/" + fileName);
                System.Net.HttpWebResponse myrp = (System.Net.HttpWebResponse)Myrq.GetResponse();
                long totalBytes = myrp.ContentLength;
                if (pbDownloadFile != null)
                    pbDownloadFile.Maximum = (int)totalBytes;
                System.IO.Stream st = myrp.GetResponseStream();
                long totalDownloadedByte = 0;
                byte[] by = new byte[1024];
                int osize = -1;
                while ((osize = st.Read(by, 0, (int)by.Length)) > 0 && _cts.Token.IsCancellationRequested == false)
                {
                    totalDownloadedByte = osize + totalDownloadedByte;
                    if (pbDownloadFile != null)
                        pbDownloadFile.Value = ((int)totalDownloadedByte > (int)totalBytes) ? (int)totalBytes : (int)totalDownloadedByte;
                }
                if (pbDownloadFile != null)
                    pbDownloadFile.Value = 0;
                st.Close();
            }
            catch (System.Exception ex)
            {
            }
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
                    if (pbDownloadFile != null)
                        pbDownloadFile.Value = 0;
                    ReadyString = "就绪";
                }, _cts.Token);
        }
    }
}
