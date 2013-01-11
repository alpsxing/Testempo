using System;
using System.Collections.Generic;
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
using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Specialized;
using System.Threading.Tasks;

using InformationTransferLibrary;

namespace RemoteTcpTerminal
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

		private ObservableCollection<SentReceivedItem> _sentReceivedOc = new ObservableCollection<SentReceivedItem>();
		public ObservableCollection<SentReceivedItem> SentReceivedOc
		{
			get
			{
				return _sentReceivedOc;
			}
		}

		private object _objLock = new object();
		public object ObjLock
		{
			get
			{
				return _objLock;
			}
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

        private string _serverIP = "127.0.0.1";
        public string ServerIP
        {
            get
            {
                return _serverIP;
            }
            set
            {
                _serverIP = value;
                NotifyPropertyChanged("ServerIP");
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

        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _mainTask = null;
        public ObservableCollection<Task> _taskOc = new ObservableCollection<Task>();
        public ObservableCollection<Task> TaskOc
        {
            get
            {
                return _taskOc;
            }
        }

		public MainWindow()
		{
			InitializeComponent();

			DataContext = this;
			dgSentReceived.DataContext = SentReceivedOc;
			SentReceivedOc.CollectionChanged += new NotifyCollectionChangedEventHandler(SentReceivedOc_CollectionChanged);
		}

		private void SentReceivedOc_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			//if (_dataAutoScrolling == false)
			//    return;

			//lock (ObjLock)
			{
				if (SentReceivedOc.Count < 1)
					return;
				var border = VisualTreeHelper.GetChild(dgSentReceived, 0) as Decorator;
				if (border != null)
				{
					var scroll = border.Child as ScrollViewer;
					if (scroll != null) scroll.ScrollToEnd();
				}
			}
		}

		public void PostLog(string msg)
		{
			PostLog(msg, Brushes.Black);
		}

		public void PostLog(string msg, SolidColorBrush scb)
		{
			Dispatcher.Invoke((ThreadStart)delegate()
			{
				if (fldocLog.Blocks.Count > 100)
					fldocLog.Blocks.Remove(fldocLog.Blocks.FirstBlock);
				Run rch = new Run(msg);
				Paragraph pch = new Paragraph(rch);
				pch.Foreground = scb;
				fldocLog.Blocks.Add(pch);
				rtxtLog.ScrollToEnd();
			}, null);
		}

		private void ClearQueue_ButtonClick(object sender, RoutedEventArgs e)
		{
			SentReceivedOc.Clear();
		}

		private void ClearLog_ButtonClick(object sender, RoutedEventArgs e)
		{
			fldocLog.Blocks.Clear();
		}

        private void StartClient()
        {
            InRun = true;
            _mainTask = new Task(
                () =>
                {
                    StartClientTask(_cts);
                }, _cts.Token);
            _mainTask.Start();
		}

        private void StartClientTask(CancellationTokenSource cts)
        {
            //IPAddress local = IPAddress.Any;
            Socket soc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipad = IPAddress.Parse(ServerIP);
            IPEndPoint iep = new IPEndPoint(ipad, Consts.DTU_PORT);
            PostLog("Trying connecting : " + ServerIP + ":" + Consts.DTU_PORT.ToString());
            soc.Connect(iep);
            if (soc.Connected == true)
            {
                PostLog("Connect : " + ServerIP + ":" + Consts.DTU_PORT.ToString());
                ClientThread newClient = new ClientThread(this, soc);
                Task t = new Task(
                    () =>
                    {
                        ClientService(_cts, soc);
                    }, _cts.Token);
                t.Start();
            }
            else
            {
                PostLog("Fail to connect : " + ServerIP + ":" + Consts.DTU_PORT.ToString());
                InRun = false;
            }
        }

        private void Connect_ButtonClick(object sender, RoutedEventArgs e)
        {
            StartClient();
        }

        public void ClientService(CancellationTokenSource cts, Socket soc)
        {
            string ip = ((IPEndPoint)soc.RemoteEndPoint).Address.ToString();
            byte[] bytes = new byte[Consts.SOCKET_RECEIVING_BUFFER_LENGTH];
            int len = 0;
            try
            {
                soc.Send(Encoding.UTF8.GetBytes(DTUID.Trim()));
                while (true)
                {
                    len = soc.Receive(bytes);
                    if (len < 1)
                    {
                        PostLog("Connection to DTU (" + ip + ") is broken.", Brushes.Red);
                        break;
                    }
                    else
                    {
                        SentReceivedItem sri = null;
                        Dispatcher.Invoke((ThreadStart)delegate()
                        {
                            sri = new SentReceivedItem();
                        }, null);

                        byte[] ba = new byte[len];
                        for (int i = 0; i < len; i++)
                        {
                            ba[i] = bytes[i];
                        }

                        string rbrs = "";
                        string rbs = "";
                        Dispatcher.Invoke((ThreadStart)delegate()
                        {
                            sri.Index = (SentReceivedOc.Count + 1).ToString();
                            sri.ReceivedBytes = ba;
                            rbrs = sri.ReceivedBytesRawString;
                            rbs = sri.ReceivedBytesString;
                        }, null);

                        PostLog("Received Raw : " + rbrs);
                        PostLog("Received : " + rbs);

                        bytes = Encoding.UTF8.GetBytes("Response : " + rbs);
                        PostLog("Trying sending response : Response : " + rbs);
                        soc.Send(bytes);
                        PostLog("Sent.");

                        Dispatcher.Invoke((ThreadStart)delegate()
                        {
                            sri.Sent = "Response : " + rbs;                            
                            SentReceivedOc.Add(sri);
                        }, null);
                    }
                }
            }
            catch (Exception ex)
            {
                PostLog(ex.Message);
            }
            PostLog("Start shutdowning...");
            try
            {
                soc.Shutdown(SocketShutdown.Both);
                PostLog("Shutdown.");
            }
            catch (Exception ex)
            {
                PostLog("Exception when shutdowning : " + ex.Message, Brushes.Red);
            }
            PostLog("Start disconnecting...");
            try
            {
                soc.Disconnect(false);
                PostLog("Disconnected.");
            }
            catch (Exception ex)
            {
                PostLog("Exception when disconnecting : " + ex.Message, Brushes.Red);
            }
            PostLog("Start closing...");
            try
            {
                soc.Close();
                PostLog("Closed.");
            }
            catch (Exception ex)
            {
                PostLog("Exception when closing : " + ex.Message, Brushes.Red);
            }
            PostLog("Start disposing...");
            try
            {
                soc.Dispose();
            }
            catch (Exception ex)
            {
                PostLog("Exception when disposing : " + ex.Message, Brushes.Red);
            }
            PostLog("Disposed.");
            int? taskId = Task.CurrentId;
            if (taskId != null)
            {
                Task tt = null;
                foreach (Task t in TaskOc)
                {
                    if (taskId == t.Id)
                    {
                        tt = t;
                        break;
                    }
                }
                if (tt != null)
                    TaskOc.Remove(tt);
            }
            InRun = false;
        }
    }

	class ClientThread
	{
		public Socket _client = null;
		private MainWindow _mw = null;

		public ClientThread(MainWindow mw, Socket s)
		{
			_mw = mw;
			_client = s;
		}

        private string ProcessRequest(string data)
        {
            return data;
            //if (string.IsNullOrWhiteSpace(data))
            //    return Consts.REMOTE_INVALID_REQUEST;
            //data = data.Trim();
            //if(data.Length <Consts.PROTOCOL_HEADER_LENGTH)
            //    return Consts.REMOTE_INVALID_REQUEST + data;

            //string header = data.Substring(0, Consts.PROTOCOL_HEADER_LENGTH);
            //string content = data.Substring(Consts.PROTOCOL_HEADER_LENGTH, len - Consts.PROTOCOL_HEADER_LENGTH);
            //if (string.IsNullOrWhiteSpace(content) == true)
            //    content = "";

            //switch (header)
            //{
            //    default:
            //        return Consts.REMOTE_INVALID_REQUEST + data;
            //    case Consts.REMOTE_PULSE_REQUEST:
            //        return Consts.REMOTE_PULSE_REQUEST_OK;
            //}
        }
	}
}
