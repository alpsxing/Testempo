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

namespace Bumblebee
{
    /// <summary>
    /// Interaction logic for UploadVDR.xaml
    /// </summary>
    public partial class UploadVDR : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private Task _uploadTask = null;
        private CancellationTokenSource _cts = null;
        
        #region Properties

        private string _server = "";
        public string Server
        {
            get
            {
                return (_server == null) ? "" : _server.Trim();
            }
            set
            {
                _server = value;
                if (string.IsNullOrWhiteSpace(_server))
                    ServerForeground = Brushes.Red;
                else
                    ServerForeground = Brushes.Black;
                NotifyPropertyChanged("Server");
                NotifyPropertyChanged("ServerForeground");
                NotifyPropertyChanged("OKEnabled");
                NotifyPropertyChanged("OKDefault");
                NotifyPropertyChanged("CancelDefault");
            }
        }

        private SolidColorBrush _serverForeground = Brushes.Red;
        public SolidColorBrush ServerForeground
        {
            get
            {
                return _serverForeground;
            }
            set
            {
                _serverForeground = value;
                NotifyPropertyChanged("ServerForeground");
            }
        }

        private string _local = "";
        public string Local
        {
            get
            {
                return (_local == null) ? "" : _local.Trim();
            }
            set
            {
                _local = value;
                if (string.IsNullOrWhiteSpace(_local))
                    LocalForeground = Brushes.Red;
                else
                    LocalForeground = Brushes.Black;
                NotifyPropertyChanged("Local");
                NotifyPropertyChanged("LocalForeground");
                NotifyPropertyChanged("OKEnabled");
                NotifyPropertyChanged("OKDefault");
                NotifyPropertyChanged("CancelDefault");
            }
        }

        private SolidColorBrush _localForeground = Brushes.Red;
        public SolidColorBrush LocalForeground
        {
            get
            {
                return _localForeground;
            }
            set
            {
                _localForeground = value;
                NotifyPropertyChanged("LocalForeground");
            }
        }

        public bool OKEnabled
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Server) ||
                    string.IsNullOrWhiteSpace(Local))
                    return false;
                else
                    return true;
            }
        }

        public bool OKDefault
        {
            get
            {
                return OKEnabled;
            }
        }

        public bool CancelDefault
        {
            get
            {
                return !OKEnabled;
            }
        }

        private string _state = "无操作.";
        public string State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                NotifyPropertyChanged("State");
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

		private long _maxValue = 1;
		public long MaxValue
		{
			get
			{
				return _maxValue;
			}
			set
			{
				_maxValue = value;
				NotifyPropertyChanged("MaxValue");
			}
		}

		private long _minValue = 0;
		public long MinValue
		{
			get
			{
				return _minValue;
			}
			set
			{
				_minValue = value;
				NotifyPropertyChanged("MinValue");
			}
		}

		private long _curValue = 0;
		public long CurValue
		{
			get
			{
				return _curValue;
			}
			set
			{
				_curValue = value;
				NotifyPropertyChanged("CurValue");
			}
		}

        #endregion

        public UploadVDR(string server)
        {
            InitializeComponent();

            DataContext = this;

			Server = server;
        }

        private void DoUpload()
        {
            InRun = true;

            _cts = new CancellationTokenSource();

            _uploadTask = Task.Factory.StartNew(new Action(UploadVDRData), _cts.Token);
        }

        private void UploadVDRData()
        {
            try
            {
                State = "开始上传文件...";
                string onlyFn = Local.Substring(Local.LastIndexOf(@"\") + 1);

                ftp myFtp = new ftp("ftp://" + Server + "/", "innov", "123456");
				myFtp.UploadEventHandler += new EventHandler(UploadEventHandler);
                string ret = myFtp.upload(onlyFn, Local);
				myFtp.UploadEventHandler -= new EventHandler(UploadEventHandler);
				if (string.IsNullOrWhiteSpace(ret))
                    State = "上传文件成功.";
                else
                    State = "上传文件失败:" + ret.Substring("{error}".Length);
            }
            catch (Exception ex)
            {
                State = "上传文件失败:" + ex.Message;
            }

			MinValue = 0;
			CurValue = 0;
			MaxValue = 1;

			Dispatcher.Invoke((ThreadStart)delegate
			{
				if (State == "上传文件成功.")
					MessageBox.Show(this, State, "上传文件", MessageBoxButton.OK, MessageBoxImage.Information);
				else
					MessageBox.Show(this, State, "上传文件", MessageBoxButton.OK, MessageBoxImage.Error);
			}, null);

            InRun = false;
        }

		private void UploadEventHandler(object sender, EventArgs args)
		{
			ftp.UploadEventArgs ulargs = args as ftp.UploadEventArgs;
			if (ulargs == null)
				return;

			int min = 0;
			long max = ulargs.Max;
			long cur = ulargs.Cur;

			if (max < min)
				max = min;
			if (cur < min)
				cur = min;
			if (cur > max)
				cur = max;

			MinValue = min;
			MaxValue = max;
			CurValue = cur;
		}

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            DoUpload();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Local_Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = false;
            dlg.Filter = "VDR file (*.VDR)|*.VDR";
            dlg.InitialDirectory = System.Environment.CurrentDirectory;
            dlg.Title = "Select a VDR file";
            bool? b = dlg.ShowDialog();
            if (b != true)
                return;

            Local = dlg.FileName;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Server))
                txtServer.Focus();
            else if (string.IsNullOrWhiteSpace(Local))
                txtLocal.Focus();
            else
                txtServer.Focus();
        }
    }

    public class ftp
    {
        private string host = null;
        private string user = null;
        private string pass = null;
        private FtpWebRequest ftpRequest = null;
        private FtpWebResponse ftpResponse = null;
        private Stream ftpStream = null;
        private int bufferSize = 1024;

		public event EventHandler UploadEventHandler;

		public class UploadEventArgs : EventArgs
		{
			public long Max { get; set; }
			public long Cur { get; set; }
		}

        /* Construct Object */
        public ftp(string hostIP, string userName, string password) { host = hostIP; user = userName; pass = password; }

		///* Download File */
		//public string download(string remoteFile, string localFile)
		//{
		//    string serr = "";
		//    try
		//    {
		//        /* Create an FTP Request */
		//        ftpRequest = (FtpWebRequest)FtpWebRequest.Create(host + "/" + remoteFile);
		//        /* Log in to the FTP Server with the User Name and Password Provided */
		//        ftpRequest.Credentials = new NetworkCredential(user, pass);
		//        /* When in doubt, use these options */
		//        ftpRequest.UseBinary = true;
		//        ftpRequest.UsePassive = true;
		//        ftpRequest.KeepAlive = true;
		//        /* Specify the Type of FTP Request */
		//        ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;
		//        /* Establish Return Communication with the FTP Server */
		//        ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
		//        /* Get the FTP Server's Response Stream */
		//        ftpStream = ftpResponse.GetResponseStream();
		//        /* Open a File Stream to Write the Downloaded File */
		//        FileStream localFileStream = new FileStream(localFile, FileMode.Create);
		//        /* Buffer for the Downloaded Data */
		//        byte[] byteBuffer = new byte[bufferSize];
		//        int bytesRead = ftpStream.Read(byteBuffer, 0, bufferSize);
		//        /* Download the File by Writing the Buffered Data Until the Transfer is Complete */
		//        try
		//        {
		//            while (bytesRead > 0)
		//            {
		//                localFileStream.Write(byteBuffer, 0, bytesRead);
		//                bytesRead = ftpStream.Read(byteBuffer, 0, bufferSize);
		//            }
		//        }
		//        catch (Exception ex)
		//        {
		//            serr = "{error}" + ex.Message;
		//        }
		//        /* Resource Cleanup */
		//        localFileStream.Close();
		//        ftpStream.Close();
		//        ftpResponse.Close();
		//        ftpRequest = null;
		//    }
		//    catch (Exception ex)
		//    {
		//        if (string.IsNullOrWhiteSpace(serr))
		//            serr = "{error}" + ex.Message;
		//        else
		//            serr = serr + "\n" + ex.Message;
		//    }
		//    return serr;
		//}

        /* Upload File */
        public string upload(string remoteFile, string localFile)
        {
            string serr = "";
            try
            {
                /* Create an FTP Request */
                ftpRequest = (FtpWebRequest)FtpWebRequest.Create(host + "/" + remoteFile);
                /* Log in to the FTP Server with the User Name and Password Provided */
                ftpRequest.Credentials = new NetworkCredential(user, pass);
                /* When in doubt, use these options */
                ftpRequest.UseBinary = true;
                ftpRequest.UsePassive = true;
                ftpRequest.KeepAlive = true;
                /* Specify the Type of FTP Request */
                ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
                /* Establish Return Communication with the FTP Server */
                ftpStream = ftpRequest.GetRequestStream();
                /* Open a File Stream to Read the File for Upload */
                FileStream localFileStream = new FileStream(localFile, FileMode.Open);
				FileInfo fi = new FileInfo(localFile);
				long size = fi.Length;
				if (UploadEventHandler != null)
					UploadEventHandler(this, new UploadEventArgs() { Max = size, Cur = 0 });
				/* Buffer for the Downloaded Data */
                byte[] byteBuffer = new byte[bufferSize];
                int bytesSent = localFileStream.Read(byteBuffer, 0, bufferSize);
                /* Upload the File by Sending the Buffered Data Until the Transfer is Complete */
				int totalRead = bytesSent;
				try
                {
                    while (bytesSent != 0)
                    {
                        ftpStream.Write(byteBuffer, 0, bytesSent);

						if (UploadEventHandler != null)
							UploadEventHandler(this, new UploadEventArgs() { Max = size, Cur = totalRead });
						
						bytesSent = localFileStream.Read(byteBuffer, 0, bufferSize);

						totalRead = totalRead + bytesSent;
                    }
                }
                catch (Exception ex)
                {
                    serr = "{error}" + ex.Message;
                }
                /* Resource Cleanup */
                localFileStream.Close();
                ftpStream.Close();
                ftpRequest = null;
            }
            catch (Exception ex)
            {
                if (string.IsNullOrWhiteSpace(serr))
                    serr = "{error}" + ex.Message;
                else
                    serr = serr + "\n" + ex.Message;
            }
            return serr;
        }

		///* Delete File */
		//public string delete(string deleteFile)
		//{
		//    try
		//    {
		//        /* Create an FTP Request */
		//        ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + deleteFile);
		//        /* Log in to the FTP Server with the User Name and Password Provided */
		//        ftpRequest.Credentials = new NetworkCredential(user, pass);
		//        /* When in doubt, use these options */
		//        ftpRequest.UseBinary = true;
		//        ftpRequest.UsePassive = true;
		//        ftpRequest.KeepAlive = true;
		//        /* Specify the Type of FTP Request */
		//        ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;
		//        /* Establish Return Communication with the FTP Server */
		//        ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
		//        /* Resource Cleanup */
		//        ftpResponse.Close();
		//        ftpRequest = null;
		//    }
		//    catch (Exception ex)
		//    {
		//        return "{error}" + ex.Message;
		//    }
		//    return "";
		//}

		///* Rename File */
		//public string rename(string currentFileNameAndPath, string newFileName)
		//{
		//    try
		//    {
		//        /* Create an FTP Request */
		//        ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + currentFileNameAndPath);
		//        /* Log in to the FTP Server with the User Name and Password Provided */
		//        ftpRequest.Credentials = new NetworkCredential(user, pass);
		//        /* When in doubt, use these options */
		//        ftpRequest.UseBinary = true;
		//        ftpRequest.UsePassive = true;
		//        ftpRequest.KeepAlive = true;
		//        /* Specify the Type of FTP Request */
		//        ftpRequest.Method = WebRequestMethods.Ftp.Rename;
		//        /* Rename the File */
		//        ftpRequest.RenameTo = newFileName;
		//        /* Establish Return Communication with the FTP Server */
		//        ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
		//        /* Resource Cleanup */
		//        ftpResponse.Close();
		//        ftpRequest = null;
		//    }
		//    catch (Exception ex)
		//    {
		//        return "{error}" + ex.Message;
		//    }
		//    return "";
		//}

		///* Create a New Directory on the FTP Server */
		//public string createDirectory(string newDirectory)
		//{
		//    try
		//    {
		//        /* Create an FTP Request */
		//        ftpRequest = (FtpWebRequest)WebRequest.Create(host + "/" + newDirectory);
		//        /* Log in to the FTP Server with the User Name and Password Provided */
		//        ftpRequest.Credentials = new NetworkCredential(user, pass);
		//        /* When in doubt, use these options */
		//        ftpRequest.UseBinary = true;
		//        ftpRequest.UsePassive = true;
		//        ftpRequest.KeepAlive = true;
		//        /* Specify the Type of FTP Request */
		//        ftpRequest.Method = WebRequestMethods.Ftp.MakeDirectory;
		//        /* Establish Return Communication with the FTP Server */
		//        ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
		//        /* Resource Cleanup */
		//        ftpResponse.Close();
		//        ftpRequest = null;
		//    }
		//    catch (Exception ex)
		//    {
		//        return "{error}" + ex.Message;
		//    }
		//    return "";
		//}

		///* Get the Date/Time a File was Created */
		//public string getFileCreatedDateTime(string fileName)
		//{
		//    try
		//    {
		//        /* Create an FTP Request */
		//        ftpRequest = (FtpWebRequest)FtpWebRequest.Create(host + "/" + fileName);
		//        /* Log in to the FTP Server with the User Name and Password Provided */
		//        ftpRequest.Credentials = new NetworkCredential(user, pass);
		//        /* When in doubt, use these options */
		//        ftpRequest.UseBinary = true;
		//        ftpRequest.UsePassive = true;
		//        ftpRequest.KeepAlive = true;
		//        /* Specify the Type of FTP Request */
		//        ftpRequest.Method = WebRequestMethods.Ftp.GetDateTimestamp;
		//        /* Establish Return Communication with the FTP Server */
		//        ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
		//        /* Establish Return Communication with the FTP Server */
		//        ftpStream = ftpResponse.GetResponseStream();
		//        /* Get the FTP Server's Response Stream */
		//        StreamReader ftpReader = new StreamReader(ftpStream);
		//        /* Store the Raw Response */
		//        string fileInfo = null;
		//        /* Read the Full Response Stream */
		//        try { fileInfo = ftpReader.ReadToEnd(); }
		//        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
		//        /* Resource Cleanup */
		//        ftpReader.Close();
		//        ftpStream.Close();
		//        ftpResponse.Close();
		//        ftpRequest = null;
		//        /* Return File Created Date Time */
		//        return fileInfo;
		//    }
		//    catch (Exception ex)
		//    {
		//        return "{error}" + ex.Message;
		//    }
		//}

		///* Get the Size of a File */
		//public string getFileSize(string fileName)
		//{
		//    try
		//    {
		//        /* Create an FTP Request */
		//        ftpRequest = (FtpWebRequest)FtpWebRequest.Create(host + "/" + fileName);
		//        /* Log in to the FTP Server with the User Name and Password Provided */
		//        ftpRequest.Credentials = new NetworkCredential(user, pass);
		//        /* When in doubt, use these options */
		//        ftpRequest.UseBinary = true;
		//        ftpRequest.UsePassive = true;
		//        ftpRequest.KeepAlive = true;
		//        /* Specify the Type of FTP Request */
		//        ftpRequest.Method = WebRequestMethods.Ftp.GetFileSize;
		//        /* Establish Return Communication with the FTP Server */
		//        ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
		//        /* Establish Return Communication with the FTP Server */
		//        ftpStream = ftpResponse.GetResponseStream();
		//        /* Get the FTP Server's Response Stream */
		//        StreamReader ftpReader = new StreamReader(ftpStream);
		//        /* Store the Raw Response */
		//        string fileInfo = null;
		//        /* Read the Full Response Stream */
		//        try { while (ftpReader.Peek() != -1) { fileInfo = ftpReader.ReadToEnd(); } }
		//        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
		//        /* Resource Cleanup */
		//        ftpReader.Close();
		//        ftpStream.Close();
		//        ftpResponse.Close();
		//        ftpRequest = null;
		//        /* Return File Size */
		//        return fileInfo;
		//    }
		//    catch (Exception ex)
		//    {
		//        return "{error}" + ex.Message;
		//    }
		//}

		///* List Directory Contents File/Folder Name Only */
		//public string[] directoryListSimple(string directory)
		//{
		//    try
		//    {
		//        /* Create an FTP Request */
		//        ftpRequest = (FtpWebRequest)FtpWebRequest.Create(host + "/" + directory);
		//        /* Log in to the FTP Server with the User Name and Password Provided */
		//        ftpRequest.Credentials = new NetworkCredential(user, pass);
		//        /* When in doubt, use these options */
		//        ftpRequest.UseBinary = true;
		//        ftpRequest.UsePassive = true;
		//        ftpRequest.KeepAlive = true;
		//        /* Specify the Type of FTP Request */
		//        ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;
		//        /* Establish Return Communication with the FTP Server */
		//        ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
		//        /* Establish Return Communication with the FTP Server */
		//        ftpStream = ftpResponse.GetResponseStream();
		//        /* Get the FTP Server's Response Stream */
		//        StreamReader ftpReader = new StreamReader(ftpStream);
		//        /* Store the Raw Response */
		//        string directoryRaw = null;
		//        /* Read Each Line of the Response and Append a Pipe to Each Line for Easy Parsing */
		//        try { while (ftpReader.Peek() != -1) { directoryRaw += ftpReader.ReadLine() + "|"; } }
		//        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
		//        /* Resource Cleanup */
		//        ftpReader.Close();
		//        ftpStream.Close();
		//        ftpResponse.Close();
		//        ftpRequest = null;
		//        /* Return the Directory Listing as a string Array by Parsing 'directoryRaw' with the Delimiter you Append (I use | in This Example) */
		//        try { string[] directoryList = directoryRaw.Split("|".ToCharArray()); return directoryList; }
		//        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
		//    }
		//    catch (Exception ex) { Console.WriteLine(ex.ToString()); }
		//    /* Return an Empty string Array if an Exception Occurs */
		//    return new string[] { "" };
		//}

		///* List Directory Contents in Detail (Name, Size, Created, etc.) */
		//public string[] directoryListDetailed(string directory)
		//{
		//    try
		//    {
		//        /* Create an FTP Request */
		//        ftpRequest = (FtpWebRequest)FtpWebRequest.Create(host + "/" + directory);
		//        /* Log in to the FTP Server with the User Name and Password Provided */
		//        ftpRequest.Credentials = new NetworkCredential(user, pass);
		//        /* When in doubt, use these options */
		//        ftpRequest.UseBinary = true;
		//        ftpRequest.UsePassive = true;
		//        ftpRequest.KeepAlive = true;
		//        /* Specify the Type of FTP Request */
		//        ftpRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
		//        /* Establish Return Communication with the FTP Server */
		//        ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
		//        /* Establish Return Communication with the FTP Server */
		//        ftpStream = ftpResponse.GetResponseStream();
		//        /* Get the FTP Server's Response Stream */
		//        StreamReader ftpReader = new StreamReader(ftpStream);
		//        /* Store the Raw Response */
		//        string directoryRaw = null;
		//        /* Read Each Line of the Response and Append a Pipe to Each Line for Easy Parsing */
		//        try { while (ftpReader.Peek() != -1) { directoryRaw += ftpReader.ReadLine() + "|"; } }
		//        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
		//        /* Resource Cleanup */
		//        ftpReader.Close();
		//        ftpStream.Close();
		//        ftpResponse.Close();
		//        ftpRequest = null;
		//        /* Return the Directory Listing as a string Array by Parsing 'directoryRaw' with the Delimiter you Append (I use | in This Example) */
		//        try { string[] directoryList = directoryRaw.Split("|".ToCharArray()); return directoryList; }
		//        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
		//    }
		//    catch (Exception ex) { Console.WriteLine(ex.ToString()); }
		//    /* Return an Empty string Array if an Exception Occurs */
		//    return new string[] { "" };
		//}
    }
}
