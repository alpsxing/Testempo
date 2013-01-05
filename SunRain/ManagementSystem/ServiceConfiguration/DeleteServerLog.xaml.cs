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
    /// Interaction logic for DeleteServerLog.xaml
    /// </summary>
    public partial class DeleteServerLog : Window, INotifyPropertyChanged
    {
        public enum ServerLogType
        {
            Delete,
            Select
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<string> _serverLogOc = null;

        public string DeleteUser { get; set; }
        public string DeleteDate { get; set; }

        private ServerLogType _windowType = ServerLogType.Delete;

        private bool _inputIsOK = false;
        public bool InputIsOK
        {
            get
            {
                return _inputIsOK;
            }
            set
            {
                _inputIsOK = value;
                NotifyPropertyChanged("InputIsOK");
            }
        }

        private bool? _isDeleteUser = false;
        public bool? IsDeleteUser
        {
            get
            {
                return _isDeleteUser;
            }
            set
            {
                _isDeleteUser = value;
                if ((_isDeleteDate == true || _isDeleteUser == true) && _serverLogOc.Count > 0)
                    InputIsOK = true;
                else
                    InputIsOK = false;
                NotifyPropertyChanged("IsDeleteUser");
            }
        }

        private bool? _isDeleteDate = false;
        public bool? IsDeleteDate
        {
            get
            {
                return _isDeleteDate;
            }
            set
            {
                _isDeleteDate = value;
                if ((_isDeleteDate == true || _isDeleteUser == true) && _serverLogOc.Count > 0)
                    InputIsOK = true;
                else
                    InputIsOK = false;
                NotifyPropertyChanged("IsDeleteDate");
            }
        }

        public DeleteServerLog(ObservableCollection<string> sloc, ServerLogType windowType = ServerLogType.Delete)
        {
            InitializeComponent();

            _serverLogOc = sloc;

            DataContext = this;

            _windowType = windowType;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_Load(object sender, RoutedEventArgs e)
        {
            cboxUser.Items.Add("全部");
            foreach (string si in _serverLogOc)
            {
                cboxUser.Items.Add(si);
            }
            cboxUser.SelectedIndex = 0;

            if (_windowType == ServerLogType.Delete)
            {
                Title = "删除服务器消息日志";
                lblDate.Content = "保留日志开始日期";
            }
            else
            {
                Title = "查看服务器消息日志";
                lblDate.Content = "查看日志开始日期";
            }

            dpDate.DisplayDate = DateTime.Now;
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            DeleteUser = (IsDeleteUser == true) ? ((cboxUser.SelectedIndex == 0) ? "all" : _serverLogOc[cboxUser.SelectedIndex]) : "";
            DeleteDate = (IsDeleteDate == true) ? (dpDate.DisplayDate.Year.ToString() + "-" + dpDate.DisplayDate.Month.ToString() + "-" + dpDate.DisplayDate.Day.ToString()) : "";

            DialogResult = true;
        }
    }
}
