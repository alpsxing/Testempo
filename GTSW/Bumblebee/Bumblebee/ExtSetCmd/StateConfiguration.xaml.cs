using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Bumblebee.ExtSetCmd
{
    /// <summary>
    /// Interaction logic for StateConfiguration.xaml
    /// </summary>
    public partial class StateConfiguration : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Properties

        private string _d2Label = "自定义";
        public string D2Label
        {
            get
            {
                return _d2Label;
            }
            set
            {
                _d2Label = value;
                NotifyPropertyChanged("D2Label");
            }
        }

        private string _d1Label = "自定义";
        public string D1Label
        {
            get
            {
                return _d1Label;
            }
            set
            {
                _d1Label = value;
                NotifyPropertyChanged("D1Label");
            }
        }

        private string _d0Label = "自定义";
        public string D0Label
        {
            get
            {
                return _d0Label;
            }
            set
            {
                _d0Label = value;
                NotifyPropertyChanged("D0Label");
            }
        }

        private int _d7State = 1;
        public int D7State
        {
            get
            {
                return _d7State;
            }
            set
            {
                _d7State = value;
                if (_d7State != 1 && _d7State != 0 && _d7State != 3)
                    _d7State = 3;
                NotifyPropertyChanged("D7State");
            }
        }

        private int _d6State = 1;
        public int D6State
        {
            get
            {
                return _d6State;
            }
            set
            {
                _d6State = value;
                if (_d7State != 1 && _d6State != 0 && _d6State != 3)
                    _d6State = 3;
                NotifyPropertyChanged("D6State");
            }
        }

        private int _d5State = 1;
        public int D5State
        {
            get
            {
                return _d5State;
            }
            set
            {
                _d5State = value;
                if (_d5State != 1 && _d5State != 0 && _d5State != 3)
                    _d5State = 3;
                NotifyPropertyChanged("D5State");
            }
        }

        private int _d4State = 1;
        public int D4State
        {
            get
            {
                return _d4State;
            }
            set
            {
                _d4State = value;
                if (_d4State != 1 && _d4State != 0 && _d4State != 3)
                    _d4State = 3;
                NotifyPropertyChanged("D4State");
            }
        }

        private int _d3State = 1;
        public int D3State
        {
            get
            {
                return _d3State;
            }
            set
            {
                _d3State = value;
                if (_d3State != 1 && _d3State != 0 && _d3State != 3)
                    _d3State = 3;
                NotifyPropertyChanged("D3State");
            }
        }

        private int _d2State = 1;
        public int D2State
        {
            get
            {
                return _d2State;
            }
            set
            {
                _d2State = value;
                if (_d2State != 1 && _d2State != 0 && _d2State != 3)
                    _d2State = 3;
                NotifyPropertyChanged("D2State");
            }
        }

        private int _d1State = 1;
        public int D1State
        {
            get
            {
                return _d1State;
            }
            set
            {
                _d1State = value;
                if (_d1State != 1 && _d1State != 0 && _d1State != 3)
                    _d1State = 3;
                NotifyPropertyChanged("D1State");
            }
        }

        private int _d0State = 1;
        public int D0State
        {
            get
            {
                return _d0State;
            }
            set
            {
                _d0State = value;
                if (_d0State != 1 && _d0State != 0 && _d0State != 3)
                    _d0State = 3;
                NotifyPropertyChanged("D0State");
            }
        }

        #endregion

        public StateConfiguration(string d2Label = "自定义", string d1Label = "自定义", string d0Label = "自定义",
            int d7State = 1, int d6State = 1, int d5State = 1, int d4State = 1,
            int d3State = 1, int d2State = 1, int d1State = 1, int d0State = 1)
        {
            InitializeComponent();

            DataContext = this;

            D2Label = d2Label;
            D1Label = d1Label;
            D0Label = d0Label;

            D7State = d7State;
            D6State = d6State;
            D5State = d5State;
            D4State = d4State;
            D3State = d3State;
            D2State = d2State;
            D1State = d1State;
            D0State = d0State;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            switch (D7State)
            {
                default:
                case 3:
                    cboxD7.SelectedIndex = 2;
                    break;
                case 1:
                    cboxD7.SelectedIndex = 0;
                    break;
                case 0:
                    cboxD7.SelectedIndex = 1;
                    break;
            }
            switch (D6State)
            {
                default:
                case 3:
                    cboxD6.SelectedIndex = 2;
                    break;
                case 1:
                    cboxD6.SelectedIndex = 0;
                    break;
                case 0:
                    cboxD6.SelectedIndex = 1;
                    break;
            }
            switch (D5State)
            {
                default:
                case 3:
                    cboxD5.SelectedIndex = 2;
                    break;
                case 1:
                    cboxD5.SelectedIndex = 0;
                    break;
                case 0:
                    cboxD5.SelectedIndex = 1;
                    break;
            }
            switch (D4State)
            {
                default:
                case 3:
                    cboxD4.SelectedIndex = 2;
                    break;
                case 1:
                    cboxD4.SelectedIndex = 0;
                    break;
                case 0:
                    cboxD4.SelectedIndex = 1;
                    break;
            }
            switch (D3State)
            {
                default:
                case 3:
                    cboxD3.SelectedIndex = 2;
                    break;
                case 1:
                    cboxD3.SelectedIndex = 0;
                    break;
                case 0:
                    cboxD3.SelectedIndex = 1;
                    break;
            }
            switch (D2State)
            {
                default:
                case 3:
                    cboxD2.SelectedIndex = 2;
                    break;
                case 1:
                    cboxD2.SelectedIndex = 0;
                    break;
                case 0:
                    cboxD2.SelectedIndex = 1;
                    break;
            }
            switch (D1State)
            {
                default:
                case 3:
                    cboxD1.SelectedIndex = 2;
                    break;
                case 1:
                    cboxD1.SelectedIndex = 0;
                    break;
                case 0:
                    cboxD1.SelectedIndex = 1;
                    break;
            }
            switch (D0State)
            {
                default:
                case 3:
                    cboxD0.SelectedIndex = 2;
                    break;
                case 1:
                    cboxD0.SelectedIndex = 0;
                    break;
                case 0:
                    cboxD0.SelectedIndex = 1;
                    break;
            }
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            switch (cboxD7.SelectedIndex)
            {
                default:
                case 2:
                    D7State = 3;
                    break;
                case 1:
                    D7State = 0;
                    break;
                case 0:
                    D7State = 1;
                    break;
            }
            switch (cboxD6.SelectedIndex)
            {
                default:
                case 2:
                    D6State = 3;
                    break;
                case 1:
                    D6State = 0;
                    break;
                case 0:
                    D6State = 1;
                    break;
            }
            switch (cboxD5.SelectedIndex)
            {
                default:
                case 2:
                    D5State = 3;
                    break;
                case 1:
                    D5State = 0;
                    break;
                case 0:
                    D5State = 1;
                    break;
            }
            switch (cboxD4.SelectedIndex)
            {
                default:
                case 2:
                    D4State = 3;
                    break;
                case 1:
                    D4State = 0;
                    break;
                case 0:
                    D4State = 1;
                    break;
            }
            switch (cboxD3.SelectedIndex)
            {
                default:
                case 2:
                    D3State = 3;
                    break;
                case 1:
                    D3State = 0;
                    break;
                case 0:
                    D3State = 1;
                    break;
            }
            switch (cboxD2.SelectedIndex)
            {
                default:
                case 2:
                    D2State = 3;
                    break;
                case 1:
                    D2State = 0;
                    break;
                case 0:
                    D2State = 1;
                    break;
            }
            switch (cboxD1.SelectedIndex)
            {
                default:
                case 2:
                    D1State = 3;
                    break;
                case 1:
                    D1State = 0;
                    break;
                case 0:
                    D1State = 1;
                    break;
            }
            switch (cboxD0.SelectedIndex)
            {
                default:
                case 2:
                    D0State = 3;
                    break;
                case 1:
                    D0State = 0;
                    break;
                case 0:
                    D0State = 1;
                    break;
            }

            DialogResult = true;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
