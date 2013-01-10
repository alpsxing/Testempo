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
    /// Interaction logic for ViewUserMsgLogDetail.xaml
    /// </summary>
    public partial class ViewUserMsgLogDetail : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _dtuId = "";
        public string DTUID
        {
            get
            {
                return _dtuId;
            }
            set
            {
                _dtuId = value;
                NotifyPropertyChanged("DTUID");
            }
        }

        private string _dtuFlow = "";
        public string DTUFlow
        {
            get
            {
                return _dtuFlow;
            }
            set
            {
                _dtuFlow = value;
                NotifyPropertyChanged("DTUFlow");
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

        public ViewUserMsgLogDetail(LogFileMessage lfm)
        {
            InitializeComponent();

            if (lfm != null)
            {
                DTUID = lfm.DTUID;
                DTUFlow = (lfm.FlowType == LogFileMessage.Flow.FromDTU) ? "来自DTU" : "发送到DTU";
                TimeStamp = lfm.TimeStamp;
                Message = lfm.Message;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string s = Message;
            while (s.IndexOf(@"\r") >= 0)
                s = s.Replace(@"\r", "\r");
            while (s.IndexOf(@"\n") >= 0)
                s = s.Replace(@"\n", "\n");
            while (s.IndexOf(@"\t") >= 0)
                s = s.Replace(@"\t", "\t");
            rtxtMessage.AppendText(s);
        }
    }
}
