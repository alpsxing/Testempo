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

namespace InformationTransferLibrary
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _content1 = "";
        public string Label1Content
        {
            get
            {
                return _content1;
            }
            set
            {
                _content1 = value;
                NotifyPropertyChanged("Label1Content");
            }
        }

        private string _content2 = "";
        public string Label2Content
        {
            get
            {
                return _content2;
            }
            set
            {
                _content2 = value;
                NotifyPropertyChanged("Label2Content");
            }
        }

        public About(string content1 = "", string content2 = "")
        {
            InitializeComponent();

            DataContext = this;

            Label1Content = content1;
            Label2Content = content2;
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }


    }
}
