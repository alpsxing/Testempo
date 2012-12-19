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
using System.Windows.Shapes;

using System.ComponentModel;
using System.Collections;
using System.Collections.ObjectModel;

namespace ManagementSystem
{
	/// <summary>
	/// Interaction logic for OneLineEditor.xaml
	/// </summary>
	public partial class OneLineEditor : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public delegate bool UserConectentCheckDelegate(string s);
        public UserConectentCheckDelegate UserConectentCheck;

        public enum WindowIcon
        {
            OneLineEditor,
            DTU
        }

        private WindowIcon _iconType = WindowIcon.OneLineEditor;
        public WindowIcon IconType
        {
            get
            {
                return _iconType;
            }
            set
            {
                _iconType = value;
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                switch (IconType)
                {
                    default:
                    case WindowIcon.OneLineEditor:
                        image.UriSource = new Uri("pack://application:,,,/managementsystem;component/resources/onelineeditor.ico");
                        break;
                    case WindowIcon.DTU:
                        image.UriSource = new Uri("pack://application:,,,/managementsystem;component/resources/dtuok.ico");
                        break;
                }
                image.EndInit();
                Icon = image;
                NotifyPropertyChanged("IsDTU");
            }
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="title"></param>
		/// <param name="name"></param>
		/// <param name="userContent"></param>
		/// <param name="userCompareList">Each item should already be Trim()</param>
		/// <param name="maxLength"></param>
        public OneLineEditor(string title, string name, string userContent = null, bool listCompare = true, ObservableCollection<string> userCompareList = null, ObservableCollection<Tuple<string, string>> userCompareListTuple = null, int maxLength = 0)
        {
            if (listCompare == true)
            {
                _compareList = true;
                _userCompareList = userCompareList;
            }
            else
            {
                _compareList = false;
                _userCompareListTuple = userCompareListTuple;
            }

            InitializeComponent();

            DataContext = this;

            _maxLength = maxLength;
            Title = title;
            UserLabel = name;
            UserContent = userContent;
        }

        private bool _compareList = true;

        private ObservableCollection<string> _userCompareList = null;
        private ObservableCollection<Tuple<string, string>> _userCompareListTuple = null;
        private int _maxLength = -1;

        private string _userLabel = "";
        public string UserLabel
        {
            get
            {
                return _userLabel;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    _userLabel = "";
                else
                    _userLabel = value.Trim();
                _userLabel = value;
                NotifyPropertyChanged("UserLabel");
            }
        }

        private string _userContent = "";
        public string UserContent
        {
            get
            {
                return _userContent;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    _userContent = "";
                else
                    _userContent = value.Trim();

                if (_compareList == true)
                {
                    #region List

                    if (_userCompareList == null || _userCompareList.Count < 1)
                    {
                        if (_maxLength > 0 && _userContent.Length > _maxLength)
                        {
                            UserContentOK = false;
                            UserContentFG = Brushes.Red;
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(_userContent))
                            {
                                UserContentOK = false;
                                UserContentFG = Brushes.Red;
                            }
                            else
                            {
                                UserContentOK = true;
                                UserContentFG = Brushes.Black;
                            }
                        }
                    }
                    else
                    {
                        bool found = false;
                        foreach (string s in _userCompareList)
                        {
                            if (string.Compare(_userContent, s) == 0)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found == true)
                        {
                            UserContentOK = false;
                            UserContentFG = Brushes.Red;
                        }
                        else
                        {
                            if (_maxLength > 0 && _userContent.Length > _maxLength)
                            {
                                UserContentOK = false;
                                UserContentFG = Brushes.Red;
                            }
                            else
                            {
                                if (string.IsNullOrWhiteSpace(_userContent))
                                {
                                    UserContentOK = false;
                                    UserContentFG = Brushes.Red;
                                }
                                else
                                {
                                    UserContentOK = true;
                                    UserContentFG = Brushes.Black;
                                }
                            }
                        }
                    }

                    #endregion
                }
                else
                {
                    #region List Tuple

                    if (_userCompareListTuple == null || _userCompareListTuple.Count < 1)
                    {
                        if (_maxLength > 0 && _userContent.Length > _maxLength)
                        {
                            UserContentOK = false;
                            UserContentFG = Brushes.Red;
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(_userContent))
                            {
                                UserContentOK = false;
                                UserContentFG = Brushes.Red;
                            }
                            else
                            {
                                UserContentOK = true;
                                UserContentFG = Brushes.Black;
                            }
                        }
                    }
                    else
                    {
                        bool found = false;
                        foreach (Tuple<string, string> s in _userCompareListTuple)
                        {
                            if (string.Compare(_userContent, s.Item2) == 0)
                            //|| string.Compare(iTestBase.CommonOperations.GetValidDatabaseName(txtContent.Text), s.Item2) == 0)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (found == true)
                        {
                            UserContentOK = false;
                            UserContentFG = Brushes.Red;
                        }
                        else
                        {
                            if (_maxLength > 0 && _userContent.Length > _maxLength)
                            {
                                UserContentOK = false;
                                UserContentFG = Brushes.Red;
                            }
                            else
                            {
                                if (string.IsNullOrWhiteSpace(_userContent))
                                {
                                    UserContentOK = false;
                                    UserContentFG = Brushes.Red;
                                }
                                else
                                {
                                    UserContentOK = true;
                                    UserContentFG = Brushes.Black;
                                }
                            }
                        }
                    }

                    #endregion
                }

                if (UserConectentCheck != null)
                {
                    if (UserConectentCheck(_userContent) == true)
                    {
                        UserContentOK = true;
                        UserContentFG = Brushes.Black;
                    }
                    else
                    {
                        UserContentOK = false;
                        UserContentFG = Brushes.Red;
                    }
                }

                NotifyPropertyChanged("UserContent");
            }
        }

        private bool _userContentOK = false;
        public bool UserContentOK
        {
            get
            {
                return _userContentOK;
            }
            set
            {
                _userContentOK = value;
                if (_userContentOK == true)
                {
                    btnOK.IsDefault = true;
                    btnCancel.IsDefault = false;
                }
                else
                {
                    btnOK.IsDefault = false;
                    btnCancel.IsDefault = true;
                }
                NotifyPropertyChanged("UserContentOK");
            }
        }

        private Brush _userContentFG = Brushes.Red;
        public Brush UserContentFG
        {
            get
            {
                return _userContentFG;
            }
            set
            {
                _userContentFG = value;
                NotifyPropertyChanged("UserContentFG");
            }
        }

        //public string UserContentDB
        //{
        //    get
        //    {
        //        return iTestBase.CommonOperations.GetValidDatabaseName(_userContent);
        //    }
        //}

		private void OK_Button_Click(object sender, RoutedEventArgs e)
		{
			 DialogResult = true;
		}

		private void Cancel_Button_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}

        private void Window_Load(object sender, RoutedEventArgs e)
        {
            txtUserContent.Focus();
        }
	}
}
