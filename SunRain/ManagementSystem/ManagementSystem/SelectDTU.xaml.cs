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

namespace ManagementSystem
{
    /// <summary>
    /// Interaction logic for SelectDTU.xaml
    /// </summary>
    public partial class SelectDTU : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private Socket _currentSocket = null;

        //public delegate byte[] DoSocketSendReceiveDelegate(Socket soc, string cmd, bool doRec = true);
        //public DoSocketSendReceiveDelegate DoSocketSendReceive;

        private ObservableCollection<DTUInfo> _dtuInfoOc = null;
        public ObservableCollection<DTUInfo> DTUInfoOc
        {
            get
            {
                return _dtuInfoOc;
            }
            set
            {
                _dtuInfoOc = value;
            }
        }

        private bool _dtuSelectionOK = false;
        public bool DTUSelectionOK
        {
            get
            {
                return _dtuSelectionOK;
            }
            set
            {
                _dtuSelectionOK = value;
                NotifyPropertyChanged("DTUSelectionOK");
            }
        }

        private string _currentStatus = "";
        public string CurrentStatus
        {
            get
            {
                return _currentStatus;
            }
            set
            {
                _currentStatus = value;
                if (string.IsNullOrWhiteSpace(CurrentStatus))
                    Title = "Select DTU";
                else
                    Title = "Select DTU - " + CurrentStatus.Trim();
                NotifyPropertyChanged("CurrentStatus");
            }
        }

        private int _dtuSelectedIndex = -1;
        public int DTUSelectedIndex
        {
            get
            {
                return _dtuSelectedIndex;
            }
            set
            {
                _dtuSelectedIndex = value;
                NotifyPropertyChanged("DTUSelectedIndex");
            }
        }

        public SelectDTU(Socket soc, ObservableCollection<DTUInfo> dtuInfoOc)
        {
            _dtuInfoOc = dtuInfoOc;

            _currentSocket = soc;

            InitializeComponent();

            DataContext = this;

            dgDtu.DataContext = _dtuInfoOc;

            CurrentStatus = "获取DTU...";
        }

        private void DTU_DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DTUSelectionOK = false;

            if (_dtuInfoOc.Count < 1)
                return;
            if (dgDtu.SelectedIndex < 0)
                return;
            if (_dtuInfoOc[dgDtu.SelectedIndex].Online == false)
                return;
            if (_dtuInfoOc[dgDtu.SelectedIndex].Controller != null)
                return;

            DTUSelectionOK = true;
        }

        //private void Refresh_Button_Click(object sender, RoutedEventArgs e)
        //{
        //    if (DoSocketSendReceive != null)
        //    {
        //        try
        //        {
        //            CurrentStatus = "Retrieving DTU...";
        //            byte[] bytes = DoSocketSendReceive(_currentSocket, Consts.MAN_GET_ALL_DTU);
        //            string content = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        //            Helper.FillDTUInfoOC(DTUInfoOc, content);
        //            if (DTUInfoOc.Count < 1)
        //                MessageBox.Show("No DTU is available.", "DTU Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show("Cannot get the DTU information.\nError message : " + ex.Message, "DTU Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        }
        //        CurrentStatus = "";
        //    }
        //}

        private void Window_Load(object sender, RoutedEventArgs e)
        {
            if (DTUInfoOc.Count < 1)
                MessageBox.Show("无DTU.", "信息", MessageBoxButton.OK, MessageBoxImage.Information);
            CurrentStatus = "";
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            DTUSelectedIndex = dgDtu.SelectedIndex;
            DialogResult = true;
        }
    }
}
