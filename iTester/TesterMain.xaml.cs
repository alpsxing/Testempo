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
using System.Globalization;
using System.ComponentModel;

using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.Reflection;
using System.Threading;
using System.Windows.Markup;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Windows.Threading;
using System.Collections.Specialized;
using System.Data;

using MySql.Data;
using MySql.Data.MySqlClient;
using MySql.Data.Common;
using MySql.Data.Entity;
using MySql.Data.Types;

using iTestBase;

namespace iTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class TesterMain : Window, INotifyPropertyChanged
    {
		public class NameStrings
		{
			public static readonly string ApplicationFolder = "ApplicationFolder";
		}

        #region Const

        //public const int LISTVIEW_IMAGE_SIZE = 18;
        public const int LISTVIEWITEM_MARGIN = -3;

        public const int MAX_TESTRESULTOC_COUNT = 1000;

        public const string STRING_DB_PW = "Database Password";

        #endregion

        #region Variables

        private bool _bInNormalClose = false;
        //private TreeView tvTestGroup = new TreeView();
		private object _objLock = new object();

		private Thread _thWorker = null;
        private TestGroupCase _curTgcUnderTest = null;

        private const int CFG_INDEX_DBIP = 0;
        //private const int CFG_INDEX_DBPW = 1;
        private const int CFG_INDEX_PRJNAME = 1;
        private const int CFG_INDEX_SUBPRJNAME = 2;
        //private const int CFG_INDEX_FUNCNAME = 4;
        private string[] _globalCfgEntries = new string[]
		{
			"Database IP",
            //"Database Password",
			"Project Name",
			"Sub Project Name"//,
			//"Function Name"
		};

		//private const int CFG_P_INDEX_PRJNAME = 0;
		//private const int CFG_P_INDEX_SUBPRJNAME = 1;
		//private const int CFG_P_INDEX_FUNCNAME = 2;
		//private const int CFG_P_INDEX_EMADDR = 3;
		//private string[] _privateCfgEntries = new string[]
		//{
		//    "Project Name",
		//    "Sub Project Name",
		//    "Function Name",
		//    "EMail Address"
		//};

		//public enum OSVersionEnum
		//{
		//    XP,
		//    WIN7
		//}
		//private OSVersionEnum _osVersion = OSVersionEnum.WIN7;

        private MySqlConnection _mysqlConn = null;
        private string _testViewID = "";
        private string _testColID = "";
		private int _testResultIndex = 0;

		private DispatcherTimer _timer = new DispatcherTimer();

		private int _intMSec = 0;
		private int _intSec = 0;
		private int _intMin = 0;
		private int _intHour = 0;
		private int _intDay = 0;

        private string _curProject = "";
        private string _curPorjectDB = "";
        private string _curSubProject = "";
        private string _curSubProjectDB = "";

        #endregion

		#region Events & Handlers

		#endregion

		#region Properties

        private ObservableCollection<Tuple<string, string>> _projectOc = new ObservableCollection<Tuple<string, string>>();
        public ObservableCollection<Tuple<string, string>> ProjectOc
		{
			get
			{
				return _projectOc;
			}
		}

        private ObservableCollection<Tuple<string, ObservableCollection<Tuple<string, string>>>> _subProjectOc = new ObservableCollection<Tuple<string, ObservableCollection<Tuple<string, string>>>>();
        public ObservableCollection<Tuple<string, ObservableCollection<Tuple<string, string>>>> SubProjectOc
		{
			get
			{
				return _subProjectOc;
			}
		}

		private Dictionary<string, object> _dicGlobalSettings = new Dictionary<string, object>();
		public Dictionary<string, object> GlobalSettings
		{
			get
			{
				return _dicGlobalSettings;
			}
		}

		private bool _databaseConnected = false;
		public bool DatabaseConnected
		{
			get
			{
				return _databaseConnected;
			}
			set
			{
                InDatabaseTalking = true;

                if (_databaseConnected != value)
                {
                    if (_databaseConnected == false)
                    {
                        if (string.IsNullOrWhiteSpace(DBIPAddress))
                        {
                            MessageBox.Show("Database IP address is empty.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        if (_mysqlConn == null)
                        {
                            try
                            {
                                _mysqlConn = new MySqlConnection();
                                string s = "server=" + DBIPAddress + ";" + "uid=root;" + "pwd=qwewq;database=itester;";
                                _mysqlConn.ConnectionString = s;
                                _mysqlConn.Open();
                                MessageBox.Show("Database is connected.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Cannot connect the database.\nError message:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                _mysqlConn = null;
                                return;
                            }
                        }
                    }
                    else
                    {
                        if (_mysqlConn != null)
                        {
                            try
                            {
                                _mysqlConn.Close();
                                _mysqlConn.Dispose();
                                _mysqlConn = null;
                                MessageBox.Show("Database is disconnected.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Cannot disconnect the database.\nError message:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                _mysqlConn = null;
                                return;
                            }
                        }
                    }
                }
				_databaseConnected = value;

                InDatabaseTalking = false;

				NotifyPropertyChanged("DatabaseConnected");
			}
		}

        public string DBIPAddress
        {
            get
            {
                return GetGlobalConfigEntry(_globalCfgEntries[CFG_INDEX_DBIP]);
            }
        }

		//private TestGroupCase _copiedTestGroupCase = null;
        //public TestGroupCase CopiedTestGroupCase
        //{
        //    get
        //    {
        //        return _copiedTestGroupCase;
        //    }
        //    set
        //    {
        //        _copiedTestGroupCase = value;
        //        if (_copiedTestGroupCase != null)
        //        {
        //            tbtnCut.IsEnabled = false;

        //            if (_copiedTestGroupCase.IsCase == false)
        //            {
        //                if (tvTestGroup.Items.Count < 1)
        //                {
        //                    tbtnPaste.ToolTip = "Paste Group";
        //                    tbtnPaste.IsEnabled = true;
        //                    return;
        //                }

        //                TreeViewItem tvi = (TreeViewItem)tvTestGroup.SelectedItem;
        //                if (tvi == null)
        //                {
        //                    tbtnPaste.ToolTip = "Paste Group";
        //                    tbtnPaste.IsEnabled = true;
        //                    return;
        //                }

        //                TestGroupCase tgc = TestColInstance.FindTestGroupCase(tvi);
        //                if (CopiedTestGroupCase.IsCase == false)
        //                {
        //                    tbtnPaste.ToolTip = "Paste Sub Group";
        //                    tbtnPaste.IsEnabled = true;
        //                }
        //            }
        //            else
        //            {
        //                if (tvTestGroup.Items.Count < 1)
        //                    return;

        //                TreeViewItem tvi = (TreeViewItem)tvTestGroup.SelectedItem;
        //                if (tvi == null)
        //                    return;

        //                TestGroupCase tgc = TestColInstance.FindTestGroupCase(tvi);
        //                if (CopiedTestGroupCase.IsCase == false)
        //                {
        //                    tbtnPaste.ToolTip = "Paste Sub Case";
        //                    tbtnPaste.IsEnabled = true;
        //                }
        //            }
        //        }
        //    }
        //}

        private TestCollection _testCol = null;
        public TestCollection TestColInstance
        {
            get
            {
                return _testCol;
            }
            set
            {
                _testCol = value;
            }
        }

        private bool _inDatabaseTalking = false;
        public bool InDatabaseTalking
        {
            get
            {
                return _inDatabaseTalking;
            }
            set
            {
                _inDatabaseTalking = value;
                NotifyPropertyChanged("InRun");
                NotifyPropertyChanged("NotInRun");
                NotifyPropertyChanged("InDatabaseTalking");
            }
        }

        private bool _inRun = false;
        public bool InRun
        {
            get
            {
                return _inRun || _inDatabaseTalking;
            }
            set
            {
                _inRun = value;
                NotifyPropertyChanged("InRun");
                NotifyPropertyChanged("NotInRun");
                NotifyPropertyChanged("InDatabaseTalking");
            }
        }

        public bool NotInRun
        {
            get
            {
                return !_inRun && !_inDatabaseTalking;
            }
        }

		private bool _inPause = false;
		public bool InPause
		{
			get
			{
				return _inPause;
			}
			set
			{
				_inPause = value;
				NotifyPropertyChanged("InPause");
				NotifyPropertyChanged("NotInPause");
			}
		}

		public bool NotInPause
		{
			get
			{
				return !_inPause;
			}
		}

        private bool _addGroupEnabled = true;
        public bool AddGroupEnabled
        {
            get
            {
                return _addGroupEnabled;
            }
            set
            {
                _addGroupEnabled = value;
                NotifyPropertyChanged("AddGrouEnabled");
            }
        }

        private bool _addSubGroupEnabled = false;
        public bool AddSubGroupEnabled
        {
            get
            {
                return _addSubGroupEnabled;
            }
            set
            {
                _addSubGroupEnabled = value;
                NotifyPropertyChanged("AddSubGroupEnabled");
            }
        }

        private bool _addSubCaseEnabled = false;
        public bool AddSubCaseEnabled
        {
            get
            {
                return _addSubCaseEnabled;
            }
            set
            {
                _addSubCaseEnabled = value;
                NotifyPropertyChanged("AddSubCaseEnabled");
            }
        }

        private bool _editNameEnabled = false;
        public bool EditNameEnabled
        {
            get
            {
                return _editNameEnabled;
            }
            set
            {
                _editNameEnabled = value;
                NotifyPropertyChanged("EditNameEnabled");
            }
        }

        private bool _deleteEnabled = false;
        public bool DeleteEnabled
        {
            get
            {
                return _deleteEnabled;
            }
            set
            {
                _deleteEnabled = value;
                NotifyPropertyChanged("DeleteEnabled");
            }
        }

        private bool _moveUpEnabled = false;
        public bool MoveUpEnabled
        {
            get
            {
                return _moveUpEnabled;
            }
            set
            {
                _moveUpEnabled = value;
                NotifyPropertyChanged("MoveUpEnabled");
            }
        }

        private bool _moveDownEnabled = false;
        public bool MoveDownEnabled
        {
            get
            {
                return _moveDownEnabled;
            }
            set
            {
                _moveDownEnabled = value;
                NotifyPropertyChanged("MoveDownEnabled");
            }
        }

        //private bool _testGroupFocused = false;
        //public bool TestGroupFocused
        //{
        //    get
        //    {
        //        return _testGroupFocused;
        //    }
        //    set
        //    {
        //        _testGroupFocused = value;
        //        NotifyPropertyChanged("TestGroupFocused");
        //        NotifyPropertyChanged("TestGroupOrCaseFocused");
        //    }
        //}

        //private bool _testGroupCaseFocused = false;
        //public bool TestGroupCaseFocused
        //{
        //    get
        //    {
        //        return _testGroupCaseFocused;
        //    }
        //    set
        //    {
        //        _testGroupCaseFocused = value;
        //        NotifyPropertyChanged("TestGroupCaseFocused");
        //        NotifyPropertyChanged("TestGroupOrCaseFocused");
        //    }
        //}

        //public bool TestGroupOrCaseFocused
        //{
        //    get
        //    {
        //        return _testGroupFocused | _testGroupCaseFocused;
        //    }
        //}

		private ObservableCollection<TestGroupCase> _testGroupCaseOc = new ObservableCollection<TestGroupCase>();
		public ObservableCollection<TestGroupCase> TestGroupCaseOc
		{
			get
			{
				return _testGroupCaseOc;
			}
		}

		private ObservableCollection<TestConfiguration> _testConfigGlobalOc = new ObservableCollection<TestConfiguration>();
		public ObservableCollection<TestConfiguration> TestConfiguraionGlobalOc
		{
			get
			{
				return _testConfigGlobalOc;
			}
		}

        private ObservableCollection<TestResult> _testResultOc = new ObservableCollection<TestResult>();
        public ObservableCollection<TestResult> TestResultOc
        {
            get
            {
                return _testResultOc;
            }
        }

        private string _curFilePath = "(None)";
		public string CurrentFilePath
		{
			get
			{
				return _curFilePath;
			}
			set
			{
				_curFilePath = value;
                if (string.IsNullOrWhiteSpace(_curFilePath))
                    _curFilePath = "(None)";
                if (CurrentDirtyFlag == true)
                    Title = "Testempo - " + _curFilePath + " *";
                else
                    Title = "Testempo - " + _curFilePath;
			}
		}

		private bool _curDirtyFlag = false;
		private bool CurrentDirtyFlag
		{
			get
			{
				return _curDirtyFlag;
			}
			set
			{
				_curDirtyFlag = value;
                if (_curDirtyFlag == true)
                    Title = "Testempo - " + _curFilePath + " *";
				else
                    Title = "Testempo - " + _curFilePath;
			}
		}

		#region Global Settings

		private string _globalCfgPath = "";
		public string GlobalCfgPath
		{
			get
			{
				return _globalCfgPath;
			}
			set
			{
				_globalCfgPath = value;
			}
		}

		private bool _globalCfgAddEnabled = true;
		public bool GlobalCfgAddEnabled
		{
			get
			{
				return _globalCfgAddEnabled;
			}
			set
			{
				_globalCfgAddEnabled = value;
				NotifyPropertyChanged("GlobalCfgAddEnabled");
			}
		}

		private bool _globalCfgDeleteEnabled = false;
		public bool GlobalCfgDeleteEnabled
		{
			get
			{
				return _globalCfgDeleteEnabled;
			}
			set
			{
				_globalCfgDeleteEnabled = value;
				NotifyPropertyChanged("GlobalCfgDeleteEnabled");
			}
		}

		private bool _globalCfgClearEnabled = false;
		public bool GlobalCfgClearEnabled
		{
			get
			{
				return _globalCfgClearEnabled;
			}
			set
			{
				_globalCfgClearEnabled = value;
				NotifyPropertyChanged("GlobalCfgClearEnabled");
			}
		}

		private bool _globalCfgDeleteAllEnabled = true;
		public bool GlobalCfgDeleteAllEnabled
		{
			get
			{
				return _globalCfgDeleteAllEnabled;
			}
			set
			{
				_globalCfgDeleteAllEnabled = value;
				NotifyPropertyChanged("GlobalCfgDeleteAllEnabled");
			}
		}

		//private bool _privateCfgAddEnabled = true;
		//public bool PrivateCfgAddEnabled
		//{
		//    get
		//    {
		//        return _privateCfgAddEnabled;
		//    }
		//    set
		//    {
		//        _privateCfgAddEnabled = value;
		//        NotifyPropertyChanged("PrivateCfgAddEnabled");
		//    }
		//}

		//private bool _privateCfgDeleteEnabled = false;
		//public bool PrivateCfgDeleteEnabled
		//{
		//    get
		//    {
		//        return _privateCfgDeleteEnabled;
		//    }
		//    set
		//    {
		//        _privateCfgDeleteEnabled = value;
		//        NotifyPropertyChanged("PrivateCfgDeleteEnabled");
		//    }
		//}

		//private bool _privateCfgClearEnabled = false;
		//public bool PrivateCfgClearEnabled
		//{
		//    get
		//    {
		//        return _privateCfgClearEnabled;
		//    }
		//    set
		//    {
		//        _privateCfgClearEnabled = value;
		//        NotifyPropertyChanged("PrivateCfgClearEnabled");
		//    }
		//}

		//private bool _privateCfgDeleteAllEnabled = true;
		//public bool PrivateCfgDeleteAllEnabled
		//{
		//    get
		//    {
		//        return _privateCfgDeleteAllEnabled;
		//    }
		//    set
		//    {
		//        _privateCfgDeleteAllEnabled = value;
		//        NotifyPropertyChanged("PrivateCfgDeleteAllEnabled");
		//    }
		//}

		#endregion

		#endregion

		#region Constructor & Init

		public TesterMain()
        {
            InitializeComponent();

            DataContext = this;
            lvGroupCase.DataContext = TestGroupCaseOc;
			lvcmGroupCase.DataContext = this;
			dgGlobalCfg.DataContext = TestConfiguraionGlobalOc;
			//lvPrivateCfg.DataContext = TestConfiguraionLocalOc;
            dgTestResults.DataContext = TestResultOc;
            TestResultOc.CollectionChanged += new NotifyCollectionChangedEventHandler(TestResultOc_CollectionChanged);

            TestColInstance = new TestCollection(TestGroupCaseOc);

			InitApplicationFolders();

			OpenConfig();

			_timer.Tick += new EventHandler(Timer_Tick);
			_timer.Interval = new TimeSpan(1000 * 1000);

			GlobalCfg_ListView_SelectionChanged(null, null);
			//PrivateCfg_ListView_SelectionChanged(null, null);
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			//m_intTimerValue = m_intTimerValue + 1;
			//double dbCur = spbTimer.Maximum * 100.0 * (double)m_intTimerValue / (double)m_intTimerCount;
			//spbTimer.Value = (dbCur > spbTimer.Maximum) ? spbTimer.Maximum : dbCur;
			IncreaseUpdateTimerlabel();
		}

		public void IncreaseUpdateTimerlabel()
		{
			IncreaseeTimerLabel();
			UpdateTimerLabel();
		}

		public void IncreaseeTimerLabel()
		{
			_intMSec = _intMSec + 1;
			if (_intMSec >= 9)
			{
				_intMSec = 0;
				_intSec = _intSec + 1;
				if (_intSec >= 59)
				{
					_intSec = 0;
					_intMin = _intMin + 1;
					if (_intMin >= 9)
					{
						_intMin = 0;
						_intHour = _intHour + 1;
						if (_intHour >= 23)
						{
							_intHour = 0;
							_intDay = _intDay + 1;
						}
					}
				}
			}
		}

		public void UpdateTimerLabel()
		{
			string strTimer = _intMSec.ToString();
			strTimer = "." + strTimer;
			strTimer = _intSec.ToString() + strTimer;
			if (strTimer.Length < 4)
				strTimer = "0" + strTimer;
			strTimer = ":" + strTimer;
			strTimer = _intMin.ToString() + strTimer;
			if (strTimer.Length < 7)
				strTimer = "0" + strTimer;
			strTimer = ":" + strTimer;
			strTimer = _intHour.ToString() + strTimer;
			if (strTimer.Length < 10)
				strTimer = "0" + strTimer;
			strTimer = "." + strTimer;
			strTimer = _intDay.ToString() + strTimer;

			//Dispatcher.Invoke((ThreadStart)delegate()
			//{
				TimerString = strTimer;
			//}, null);
		}

		private void TestResultOc_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			lock (_objLock)
			{
				if (TestResultOc.Count < 1)
					return;
				//dgTestResults.Tag = e.NewItems[0];
				//object selItem = dgTestResults.SelectedItem;
				//dgTestResults.SelectedItem = dgTestResults.Tag;
				//dgTestResults.UpdateLayout();
				//dgTestResults.ScrollIntoView(TestResultOc[TestResultOc.Count - 1]);
				//if (selItem != null)
				//    dgTestResults.SelectedItem = selItem;
				//if (dgTestResults.Items.Count > 0)
				//{
					var border = VisualTreeHelper.GetChild(dgTestResults, 0) as Decorator;
					if (border != null)
					{
						var scroll = border.Child as ScrollViewer;
						if (scroll != null) scroll.ScrollToEnd();
					}
				//} 
			}
		}

		private void InitApplicationFolders()
		{
			string appDir = System.Environment.CurrentDirectory;
			GlobalSettings.Add(NameStrings.ApplicationFolder, appDir);

            if (Directory.Exists(appDir + @"\system") == false)
                Directory.CreateDirectory(appDir + @"\system");
            if (Directory.Exists(appDir + @"\config") == false)
                Directory.CreateDirectory(appDir + @"\config");
            if (Directory.Exists(appDir + @"\files") == false)
				Directory.CreateDirectory(appDir + @"\files");
			if (Directory.Exists(appDir + @"\data") == false)
				Directory.CreateDirectory(appDir + @"\data");
			if (Directory.Exists(appDir + @"\results") == false)
				Directory.CreateDirectory(appDir + @"\results");
		}

		#endregion

		public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Status Bar & Message Bar

        private int _allCaseCount = 0;
        public int AllCaseCount
        {
            get
            {
                return _allCaseCount;
            }
            set
            {
                _allCaseCount = value;
                if (_allCaseCount < 0)
                    _allCaseCount = 0;
                if (_allCaseCount < _executedCaseCount)
                    _executedCaseCount = _allCaseCount;
                if (_allCaseCount < _passCaseCount)
                    _passCaseCount = _allCaseCount;
                if (_allCaseCount < _failCaseCount)
                    _failCaseCount = _allCaseCount;
                if (_allCaseCount < _errorCaseCount)
                    _errorCaseCount = _allCaseCount;
                NotifyPropertyChanged("AllCaseCount");
				NotifyPropertyChanged("AllCaseCountString");
				NotifyPropertyChanged("AllCaseCountWholeString");
				NotifyPropertyChanged("ExecutedCaseCount");
                NotifyPropertyChanged("ExecutedCaseCountString");
                NotifyPropertyChanged("PassCaseCount");
                NotifyPropertyChanged("PassCaseCountString");
                NotifyPropertyChanged("PassCaseCount");
                NotifyPropertyChanged("PassCaseCountString");
                NotifyPropertyChanged("ErrorCaseCount");
                NotifyPropertyChanged("ErrorCaseCountString");
				NotifyPropertyChanged("PassRateString");
				NotifyPropertyChanged("PassRateWholeString");
			}
        }

		public string AllCaseCountWholeString
		{
			get
			{
				return "Total Cases : " + _allCaseCount.ToString();
			}
		}

		public string AllCaseCountString
		{
			get
			{
				return _allCaseCount.ToString();
			}
		}

        private int _executedCaseCount = 0;
        public int ExecutedCaseCount
        {
            get
            {
                return _executedCaseCount;
            }
            set
            {
                _executedCaseCount = value;
                if (_executedCaseCount < 0)
                    _executedCaseCount = 0;
                if (_allCaseCount < _executedCaseCount)
                    _executedCaseCount = _allCaseCount;
                NotifyPropertyChanged("ExecutedCaseCount");
                NotifyPropertyChanged("ExecutedCaseCountString");
            }
        }

        public string ExecutedCaseCountString
        {
            get
            {
                return _executedCaseCount.ToString();
            }
        }

        private int _passCaseCount = 0;
        public int PassCaseCount
        {
            get
            {
                return _passCaseCount;
            }
            set
            {
                _passCaseCount = value;
                if (_passCaseCount < 0)
                    _passCaseCount = 0;
                if (_allCaseCount < _passCaseCount)
                    _passCaseCount = _allCaseCount;
                NotifyPropertyChanged("PassCaseCount");
                NotifyPropertyChanged("PassCaseCountString");
				NotifyPropertyChanged("PassRateString");
				NotifyPropertyChanged("PassRateWholeString");
			}
        }

        public string PassCaseCountString
        {
            get
            {
                return _passCaseCount.ToString();
            }
        }

        private int _failCaseCount = 0;
        public int FailCaseCount
        {
            get
            {
                return _failCaseCount;
            }
            set
            {
                _failCaseCount = value;
                if (_failCaseCount < 0)
                    _failCaseCount = 0;
                if (_allCaseCount < _failCaseCount)
                    _failCaseCount = _allCaseCount;
                NotifyPropertyChanged("FailCaseCount");
                NotifyPropertyChanged("FailCaseCountString");
            }
        }

        public string FailCaseCountString
        {
            get
            {
                return _failCaseCount.ToString();
            }
        }

        private int _errorCaseCount = 0;
        public int ErrorCaseCount
        {
            get
            {
                return _errorCaseCount;
            }
            set
            {
                _errorCaseCount = value;
                if (_errorCaseCount < 0)
                    _errorCaseCount = 0;
                if (_allCaseCount < _errorCaseCount)
                    _errorCaseCount = _allCaseCount;
                NotifyPropertyChanged("ErrorCaseCount");
                NotifyPropertyChanged("ErrorCaseCountString");
            }
        }

        public string ErrorCaseCountString
        {
            get
            {
                return _errorCaseCount.ToString();
            }
        }

		public string PassRateWholeString
		{
			get
			{
				if (_allCaseCount == 0)
					return "Pass Rate : 0.0%";
				else
				{
					double dv = 100.0 * _passCaseCount / _allCaseCount;
					return "Pass Rate : " + dv.ToString("F2") + "%";
				}
			}
		}

		public string PassRateString
		{
			get
			{
				if (_allCaseCount == 0)
					return "0.0%";
				else
				{
					double dv = 100.0 * _passCaseCount / _allCaseCount;
					return dv.ToString("F2") + "%";
				}
			}
		}

        private string _strReady = "Ready";
        public string ReadyString
        {
            get
            {
                return _strReady;
            }
            set
            {
                _strReady = value.Trim();
                NotifyPropertyChanged("ReadyString");
            }
        }

        private string _strTimer = "0.00:00:00.0";
        public string TimerString
        {
            get
            {
                return _strTimer;
            }
            set
            {
                _strTimer = value.Trim();
                NotifyPropertyChanged("TimerString");
            }
        }

        private string _strFind = "None";
        public string FindString
        {
            get
            {
                return _strFind;
            }
            set
            {
                _strFind = value.Trim();
                NotifyPropertyChanged("FindString");
                if (string.Compare(_strFind, "None") == 0)
                    FindForeground = Colors.Gray;
                else
                    FindForeground = Colors.Black;
            }
        }

        private Color _crFindFore = Colors.Black;
        public Color FindForeground
        {
            get
            {
                return _crFindFore;
            }
            set
            {
                _crFindFore = value;
                NotifyPropertyChanged("FindForeground");
            }
        }

        private int _intInfoCountSingle = 0;
        public int InformationCountSingle
        {
            get
            {
                return _intInfoCountSingle;
            }
            set
            {
                _intInfoCountSingle = value;
                if (_intInfoCountSingle < 0) _intInfoCountSingle = 0;
                NotifyPropertyChanged("InformationCount");
                NotifyPropertyChanged("InformationCountSingle");
                NotifyPropertyChanged("InformationCountString");
                NotifyPropertyChanged("InformationCountSingleString");
            }
        }

        private int _intInfoCount = 0;
        public int InformationCount
        {
            get
            {
                return _intInfoCount;
            }
            set
            {
                _intInfoCount = value;
                if (_intInfoCount < 0) _intInfoCount = 0;
                NotifyPropertyChanged("InformationCount");
                NotifyPropertyChanged("InformationCountSingle");
                NotifyPropertyChanged("InformationCountString");
                NotifyPropertyChanged("InformationCountSingleString");
            }
        }

        public string InformationCountString
        {
            get
            {
                return _intInfoCount.ToString();
            }
        }

        public string InformationCountSingleString
        {
            get
            {
                return _intInfoCountSingle.ToString();
            }
        }

        private int _intPassCountSingle = 0;
        public int PassCountSingle
        {
            get
            {
                return _intPassCountSingle;
            }
            set
            {
                _intPassCountSingle = value;
                if (_intPassCountSingle < 0) _intPassCountSingle = 0;
                NotifyPropertyChanged("PassCount");
                NotifyPropertyChanged("PassCountSingle");
                NotifyPropertyChanged("PassCountString");
                NotifyPropertyChanged("PassCountSingleString");
            }
        }

        private int _intPassCount = 0;
        public int PassCount
        {
            get
            {
                return _intPassCount;
            }
            set
            {
                _intPassCount = value;
                if (_intPassCount < 0) _intPassCount = 0;
                NotifyPropertyChanged("PassCount");
                NotifyPropertyChanged("PassCountSingle");
                NotifyPropertyChanged("PassCountString");
                NotifyPropertyChanged("PassCountSingleString");
            }
        }

        public string PassCountString
        {
            get
            {
                return _intPassCount.ToString();
            }
        }

        public string PassCountSingleString
        {
            get
            {
                return _intPassCountSingle.ToString();
            }
        }

        private int _intFailCountSingle = 0;
        public int FailCountSingle
        {
            get
            {
                return _intFailCountSingle;
            }
            set
            {
                _intFailCountSingle = value;
                if (_intFailCountSingle < 0) _intFailCountSingle = 0;
                NotifyPropertyChanged("FailCount");
                NotifyPropertyChanged("FailCountSingle");
                NotifyPropertyChanged("FailCountString");
                NotifyPropertyChanged("FailCountSingleString");
            }
        }

        private int _intFailCount = 0;
        public int FailCount
        {
            get
            {
                return _intFailCount;
            }
            set
            {
                _intFailCount = value;
                if (_intFailCount < 0) _intFailCount = 0;
                NotifyPropertyChanged("FailCount");
                NotifyPropertyChanged("FailCountSingle");
                NotifyPropertyChanged("FailCountString");
                NotifyPropertyChanged("FailCountSingleString");
            }
        }

        public string FailCountString
        {
            get
            {
                return _intFailCount.ToString();
            }
        }

        public string FailCountSingleString
        {
            get
            {
                return _intFailCountSingle.ToString();
            }
        }

        private int _intErrorCountSingle = 0;
        public int ErrorCountSingle
        {
            get
            {
                return _intErrorCountSingle;
            }
            set
            {
                _intErrorCountSingle = value;
                if (_intErrorCountSingle < 0) _intErrorCountSingle = 0;
                NotifyPropertyChanged("ErrorCount");
                NotifyPropertyChanged("ErrorCountSingle");
                NotifyPropertyChanged("ErrorCountString");
                NotifyPropertyChanged("ErrorCountSingleString");
            }
        }

        private int _intErrorCount = 0;
        public int ErrorCount
        {
            get
            {
                return _intErrorCount;
            }
            set
            {
                _intErrorCount = value;
                if (_intErrorCount < 0) _intErrorCount = 0;
                NotifyPropertyChanged("ErrorCount");
                NotifyPropertyChanged("ErrorCountSingle");
                NotifyPropertyChanged("ErrorCountString");
                NotifyPropertyChanged("ErrorCountSingleString");
            }
        }

        public string ErrorCountString
        {
            get
            {
                return _intErrorCount.ToString();
            }
        }

        public string ErrorCountSingleString
        {
            get
            {
                return _intErrorCountSingle.ToString();
            }
        }

        private int _intTotalCountSingle = 0;
        public int TotalCountSingle
        {
            get
            {
                return _intTotalCountSingle;
            }
            set
            {
                _intTotalCountSingle = value;
                if (_intTotalCountSingle < 0) _intTotalCountSingle = 0;
                NotifyPropertyChanged("TotalCount");
                NotifyPropertyChanged("TotalCountSingle");
                NotifyPropertyChanged("TotalCountString");
                NotifyPropertyChanged("TotalCountSingleString");
            }
        }

        private int _intTotalCount = 0;
        public int TotalCount
        {
            get
            {
                return _intTotalCount;
            }
            set
            {
                _intTotalCount = value;
                if (_intTotalCount < 0) _intTotalCount = 0;
                NotifyPropertyChanged("TotalCount");
                NotifyPropertyChanged("TotalCountSingle");
                NotifyPropertyChanged("TotalCountString");
                NotifyPropertyChanged("TotalCountSingleString");
            }
        }

        public string TotalCountString
        {
            get
            {
                return _intTotalCount.ToString();
            }
        }

        public string TotalCountSingleString
        {
            get
            {
                return _intTotalCountSingle.ToString();
            }
        }

        #endregion

        private void ResultViewer_Click(object sender, RoutedEventArgs e)
        {
            ResultViewer rv = new ResultViewer();
            rv.Show();
        }

        #region Window Exit

        private void Window_Exit(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure to quit \"Testempo\"?", "Comfirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _bInNormalClose = true;

            DatabaseConnected = false;

            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_bInNormalClose == false)
            {
                if (MessageBox.Show("Are you sure to quit \"Testempo\"?", "Comfirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    e.Cancel = true;
            }

            base.OnClosing(e);
        }

        #endregion

        #region Edit Group/Case releted methods

        #region Add, Add Sub Group, Add Sub Case, Edit Name, Delete, Move Up, Move Down

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            OneLineEditor ole = new OneLineEditor("New Group", "Group Name");
            if (ole.ShowDialog() != true)
                return;

            int index = 0;
            string sc = "";
            while (true)
            {
                bool bFound = false;
                foreach (TestGroupCase tgct in TestColInstance.TestGroupCaseOc)
                {
                    sc = ole.UserContent + ((index == 0) ? "" : " (" + index.ToString() + ")");
                    if (string.Compare(sc, tgct.GroupCaseName) == 0)
                    {
                        bFound = true;
                        break;
                    }
                }
                if (bFound == true)
                    index++;
                else
                    break;
            }
            if (string.IsNullOrWhiteSpace(sc))
                sc = ole.UserContent;

            TestGroupCase tgc = new TestGroupCase(TestColInstance, null, sc, false, null, null);

			CurrentDirtyFlag = true;

            UpdateTotalTestCases();
        }

        private void AddSubGroup_Click(object sender, RoutedEventArgs e)
        {
            OneLineEditor ole = new OneLineEditor("New Sub Group", "Sub Group Name");
            if (ole.ShowDialog() != true)
                return;

            TestGroupCase tgc = (TestGroupCase)lvGroupCase.SelectedItem;

            int index = 0;
            string sc = "";
            while (true)
            {
                bool bFound = false;
                foreach (TestGroupCase tgci in tgc.TestGroupCaseOc)
                {
                    sc = ole.UserContent + ((index == 0) ? "" : " (" + index.ToString() + ")");
                    if (string.Compare(sc, tgci.GroupCaseName) == 0)
                    {
                        bFound = true;
                        break;
                    }
                }
                if (bFound == true)
                    index++;
                else
                    break;
            }
            if (string.IsNullOrWhiteSpace(sc))
                sc = ole.UserContent;

            TestGroupCase tgct = new TestGroupCase(TestColInstance, tgc, sc, false, null, null);

			CurrentDirtyFlag = true;

            UpdateTotalTestCases();
        }

        private void AddSubCase_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = false;
            dlg.Filter = "Test Data (*.dll)|*.dll";
            dlg.InitialDirectory = GlobalSettings[NameStrings.ApplicationFolder] + @"\data";
            dlg.Title = "Select a test binary";
            if (dlg.ShowDialog() != true)
                return;

            #region

			iTestBase.iTestBase tb = LoadTestBaseDllFromFile(dlg.FileName, false);
			if (tb == null)
				return;

            #endregion

			TestGroupCase tgc = (TestGroupCase)lvGroupCase.SelectedItem;

			int index = 0;
			string sc = "";
			while (true)
			{
				bool bFound = false;
				foreach (TestGroupCase tgci in tgc.TestGroupCaseOc)
				{
					sc = tb.TestName + ((index == 0) ? "" : " (" + index.ToString() + ")");
					if (string.Compare(sc, tgci.GroupCaseName) == 0)
					{
						bFound = true;
						break;
					}
				}
				if (bFound == true)
					index++;
				else
					break;
			}
			if (string.IsNullOrWhiteSpace(sc))
				sc = tb.TestName;

			#region Create Relative Path

            //int idx = dlg.FileName.LastIndexOf(@"\");
            //string strName = dlg.FileName.Substring(idx + 1);
            //string strPath = dlg.FileName.Substring(0, idx);
            //string strApp = (string)GlobalSettings[NameStrings.ApplicationFolder];
            string strFinal = "";
            //if (string.Compare(strPath.Substring(0, 1), strApp.Substring(0, 1), true) != 0)
                strFinal = dlg.FileName;
            //else if (strPath.StartsWith(@"\") || strPath.StartsWith(@"\"))
            //    strFinal = dlg.FileName;
            //else
            //{
            //    string[] saPath = strPath.Split(new string[] { @"\" }, StringSplitOptions.None);
            //    string[] saApp = strApp.Split(new string[] { @"\" }, StringSplitOptions.None);

            //    int lsaP = saPath.Length;
            //    int lsaA = saApp.Length;
            //    int minLen = Math.Min(lsaP, lsaA);

            //    int i = 0;
            //    for (; i < minLen; i++)
            //    {
            //        if (string.Compare(saPath[i], saApp[i], true) != 0)
            //            break;
            //    }
            //    lsaA = lsaA - i;
            //    for (; i < lsaP; i++)
            //    {
            //        strFinal = strFinal + saPath[i] + @"\";
            //    }
            //    for (i = 0; i < lsaA; i++)
            //    {
            //        strFinal = @"..\" + strFinal;
            //    }

            //    strFinal = strFinal + strName;
            //}

			#endregion

			TestGroupCase tgct = new TestGroupCase(TestColInstance, tgc, sc, true, tb, strFinal);
            if(tgct.IsCase == true)
                tgct.TestBaseInstance.TestName = sc;

			CurrentDirtyFlag = true;

            UpdateTotalTestCases();
        }

        private void EditName_Click(object sender, RoutedEventArgs e)
        {
			TestGroupCase tgc = (TestGroupCase)lvGroupCase.SelectedItem;

			OneLineEditor ole = null;
			if (tgc.IsCase == true)
				ole = new OneLineEditor("Edit Group Name", "Group Name", tgc.GroupCaseName);
			else
				ole = new OneLineEditor("Edit Case Name", "Case Name", tgc.GroupCaseName);
			if (ole.ShowDialog() != true)
				return;

			tgc.GroupCaseName = ole.UserContent;
            if (tgc.IsCase == true)
                tgc.TestBaseInstance.TestName = ole.UserContent;

			CurrentDirtyFlag = true;
		}

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
			TestGroupCase tgc = (TestGroupCase)lvGroupCase.SelectedItem;

			if (MessageBox.Show("Are you sure to delete the test " + (tgc.IsCase ? "group" : "case") + " : " + tgc.GroupCaseName, "Confirmation",
				 MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
				return;

			TestColInstance.DeleteTestGroupCase(tgc);

			CurrentDirtyFlag = true;

            UpdateTotalTestCases();
        }

		private void MoveUp_Click(object sender, RoutedEventArgs e)
		{
			TestGroupCase tgc = (TestGroupCase)lvGroupCase.SelectedItem;

			int index = TestColInstance.FindTestGroupCaseIndex(tgc);
			if (index < 1)
				return;

			if (tgc.TestGroupCaseParent == null)
			{
				TestGroupCase tgcb = TestColInstance.TestGroupCaseOc[index - 1];

				TestColInstance.TestGroupCaseOc.Remove(tgcb);
				TestColInstance.TestGroupCaseOc.Insert(index, tgcb);

				int indexDispb = TestColInstance.FindTestGroupCaseDisplayIndex(tgcb);
				int indexDisp = TestColInstance.FindTestGroupCaseDisplayIndex(tgc);
				int indexDispl = TestColInstance.FindLastSubGroupCaseDisplayIndex(tgc);
				for (int i = indexDispb; i < indexDisp; i++)
				{
					TestGroupCase tgct = TestColInstance.TestGroupCaseOcDisp[indexDispb];
					TestColInstance.TestGroupCaseOcDisp.Remove(tgct);
					TestColInstance.TestGroupCaseOcDisp.Insert(indexDispl, tgct);
				}
			}
			else
			{
				TestGroupCase tgcp = tgc.TestGroupCaseParent;
				TestGroupCase tgcb = tgcp.TestGroupCaseOc[index - 1];

				tgcp.TestGroupCaseOc.Remove(tgcb);
				tgcp.TestGroupCaseOc.Insert(index, tgcb);

				int indexDispb = TestColInstance.FindTestGroupCaseDisplayIndex(tgcb);
				int indexDisp = TestColInstance.FindTestGroupCaseDisplayIndex(tgc);
				int indexDispl = TestColInstance.FindLastSubGroupCaseDisplayIndex(tgc);
				for (int i = indexDispb; i < indexDisp; i++)
				{
					TestGroupCase tgct = TestColInstance.TestGroupCaseOcDisp[indexDispb];
					TestColInstance.TestGroupCaseOcDisp.Remove(tgct);
					TestColInstance.TestGroupCaseOcDisp.Insert(indexDispl, tgct);
				}
			}

			GroupCase_ListView_SelectionChanged(null, null);

			CurrentDirtyFlag = true;
		}

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
			TestGroupCase tgc = (TestGroupCase)lvGroupCase.SelectedItem;
			
            int index = TestColInstance.FindTestGroupCaseIndex(tgc);
			if (tgc.TestGroupCaseParent == null)
			{
				if (index >= TestColInstance.TestGroupCaseOc.Count - 1)
					return;

				TestGroupCase tgca = TestColInstance.TestGroupCaseOc[index + 1];

				TestColInstance.TestGroupCaseOc.Remove(tgca);
				TestColInstance.TestGroupCaseOc.Insert(index, tgca);

				int indexDisp = TestColInstance.FindTestGroupCaseDisplayIndex(tgc);
				int indexDispa = TestColInstance.FindTestGroupCaseDisplayIndex(tgca);
				int indexDispal = TestColInstance.FindLastSubGroupCaseDisplayIndex(tgca);
				for (int i = indexDispal; i >= indexDispa; i--)
				{
					TestGroupCase tgct = TestColInstance.TestGroupCaseOcDisp[indexDispal];
					TestColInstance.TestGroupCaseOcDisp.Remove(tgct);
					TestColInstance.TestGroupCaseOcDisp.Insert(indexDisp, tgct);
				}
			}
			else
			{
				if (index >= tgc.TestGroupCaseParent.TestGroupCaseOc.Count - 1)
					return;

				TestGroupCase tgcp = tgc.TestGroupCaseParent;
				TestGroupCase tgca = tgcp.TestGroupCaseOc[index + 1];

				tgcp.TestGroupCaseOc.Remove(tgca);
				tgcp.TestGroupCaseOc.Insert(index, tgca);

				int indexDisp = TestColInstance.FindTestGroupCaseDisplayIndex(tgc);
				int indexDispa = TestColInstance.FindTestGroupCaseDisplayIndex(tgca);
				int indexDispal = TestColInstance.FindLastSubGroupCaseDisplayIndex(tgca);
				for (int i = indexDispal; i >= indexDispa; i--)
				{
					TestGroupCase tgct = TestColInstance.TestGroupCaseOcDisp[indexDispal];
					TestColInstance.TestGroupCaseOcDisp.Remove(tgct);
					TestColInstance.TestGroupCaseOcDisp.Insert(indexDisp, tgct);
				}
			}

			GroupCase_ListView_SelectionChanged(null, null);

			CurrentDirtyFlag = true;
		}

        #endregion

        //#region Copy

        //private void Copy_ContextMenuItem_Click(object sender, RoutedEventArgs e)
        //{
        //    CopyGroupCase();
        //}

        //private void Copy_MenuItem_Click(object sender, RoutedEventArgs e)
        //{
        //    CopyGroupCase();
        //}

        //private void Copy_ToobarButton_Click(object sender, RoutedEventArgs e)
        //{
        //    CopyGroupCase();
        //}

        //private void CopyGroupCase()
        //{
        //    TreeViewItem tvi = (TreeViewItem)tvTestGroup.SelectedItem;
        //    TestGroupCase tgc = TestColInstance.FindTestGroupCase(tvi);

        //}

        //#endregion

        //#region Cut

        //private void Cut_ContextMenuItem_Click(object sender, RoutedEventArgs e)
        //{
        //    CutGroupCase();
        //}

        //private void Cut_MenuItem_Click(object sender, RoutedEventArgs e)
        //{
        //    CutGroupCase();
        //}

        //private void Cut_ToobarButton_Click(object sender, RoutedEventArgs e)
        //{
        //    CutGroupCase();
        //}

        //private void CutGroupCase()
        //{
        //    TreeViewItem tvi = (TreeViewItem)tvTestGroup.SelectedItem;
        //    CopiedTestGroupCase = TestColInstance.FindTestGroupCase(tvi);
        //    if (CopiedTestGroupCase.TestGroupCaseParent != null)
        //        CopiedTestGroupCase.TestGroupCaseParent.TestGroupCaseOc.Remove(CopiedTestGroupCase);
        //    else
        //        TestColInstance.TestGroupCaseOc.Remove(CopiedTestGroupCase);
        //    tvTestGroup.Items.Remove(CopiedTestGroupCase.TviGroupCase);
        //}

        //#endregion

        //#region Paste

        //private void Paste_ContextMenuItem_Click(object sender, RoutedEventArgs e)
        //{
        //    PasteGroupCase();
        //}

        //private void Paste_MenuItem_Click(object sender, RoutedEventArgs e)
        //{
        //    PasteGroupCase();
        //}

        //private void Paste_ToobarButton_Click(object sender, RoutedEventArgs e)
        //{
        //    PasteGroupCase();
        //}

        //private void PasteGroupCase()
        //{
        //    if (CopiedTestGroupCase == null)
        //        return;

        //    if (tvTestGroup.Items.Count < 1 || tvTestGroup.SelectedItem == null)
        //    {
        //        if (CopiedTestGroupCase.IsCase == true)
        //            return;

        //        TestColInstance.TestGroupCaseOc.Add(CopiedTestGroupCase);
        //        tvTestGroup.Items.Add(CopiedTestGroupCase.TviGroupCase);

        //        return;
        //    }

        //    TreeViewItem tvi = (TreeViewItem)tvTestGroup.SelectedItem;
        //    TestGroupCase tgc = TestColInstance.FindTestGroupCase(tvi);
        //    if (tgc.IsCase)
        //        return;

        //    tgc.TestGroupCaseOc.Add(CopiedTestGroupCase);
        //    tgc.TviGroupCase.Items.Add(CopiedTestGroupCase.TviGroupCase);
        //}

        //#endregion

        #endregion

        #region Group/Case TreeeView/ListView

        //private void TestGroup_TreeView_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    TestGroupFocused = false;
        //}

        //private void TestGroupCase_ListView_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    TestGroupCaseFocused = false;
        //}

        //private void TestGroup_TreeView_GotFocus(object sender, RoutedEventArgs e)
        //{
        //    TestGroupFocused = true;
        //}

        //private void TestGroupCase_ListView_GotFocus(object sender, RoutedEventArgs e)
        //{
        //    TestGroupCaseFocused = true;
        //}

        //private void TestGroup_TreeView_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    tvTestGroup.Focus();
        //    if (tvTestGroup.Items.Count >0 && tvTestGroup.SelectedItem == null)
        //        ((TreeViewItem)tvTestGroup.Items[0]).Focus();
        //}

        //private void TestGroupCase_ListView_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    lvTestGroupCase.Focus();
        //    if (lvTestGroupCase.Items.Count > 0 && lvTestGroupCase.SelectedIndex < 0)
        //        lvTestGroupCase.SelectedIndex = 0;
        //}

        private void TestGroup_TreeView_SelectionItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //lvTestCase.DataContext = null;

            //tbtnAddGroup.IsEnabled = false;
            //tbtnAddSubGroup.IsEnabled = false;
            //tbtnAddSubCase.IsEnabled = false;
            //tbtnEditName.IsEnabled = false;
            ////tbtnCopy.IsEnabled = false;
            ////tbtnCut.IsEnabled = false;
            ////tbtnPaste.IsEnabled = false;
            //tbtnDelete.IsEnabled = false;
            //tbtnMoveUp.IsEnabled = false;
            //tbtnMoveDown.IsEnabled = false;

            //if (tvTestGroup.Items.Count < 1)
            //{
            //    tbtnAddGroup.IsEnabled = true;
            //    return;
            //}

            //TreeViewItem tvi = (TreeViewItem)tvTestGroup.SelectedItem;
            //if (tvi == null)
            //{
            //    tbtnAddGroup.IsEnabled = true;
            //    return;
            //}

            //TestGroupCase tgc = TestColInstance.FindTestGroupCase(tvi);
            //int index = TestColInstance.FindTestGroupCaseIndex(tgc);
            //if (tgc.IsCase == true)
            //{
            //    tbtnAddGroup.IsEnabled = true;
            //    tbtnEditName.ToolTip = "Edit Case Name";
            //    tbtnEditName.IsEnabled = true;
            //    //tbtnCopy.ToolTip = "Copy Case";
            //    //tbtnCopy.IsEnabled = true;
            //    //tbtnCut.ToolTip = "Cut Case";
            //    //tbtnCut.IsEnabled = true;
            //    tbtnDelete.ToolTip = "Delete Case";
            //    tbtnDelete.IsEnabled = true;
            //    if (index > 0)
            //    {
            //        tbtnMoveUp.ToolTip = "Move Up Case";
            //        tbtnMoveUp.IsEnabled = true;
            //    }
            //    if (tgc.TestGroupCaseParent == null)
            //    {
            //        if (index < TestColInstance.TestGroupCaseOc.Count - 1)
            //        {
            //            tbtnMoveDown.ToolTip = "Move Down Case";
            //            tbtnMoveDown.IsEnabled = true;
            //        }
            //    }
            //    else
            //    {
            //        if (index < tgc.TestGroupCaseParent.TestGroupCaseOc.Count - 1)
            //        {
            //            tbtnMoveDown.ToolTip = "Move Down Case";
            //            tbtnMoveDown.IsEnabled = true;
            //        }
            //    }
            //}
            //else
            //{
            //    tbtnAddGroup.IsEnabled = true;
            //    tbtnAddSubGroup.IsEnabled = true;
            //    tbtnAddSubCase.IsEnabled = true;
            //    tbtnEditName.ToolTip = "Edit Group Name";
            //    tbtnEditName.IsEnabled = true;
            //    //tbtnCopy.ToolTip = "Copy Group";
            //    //tbtnCopy.IsEnabled = true;
            //    //tbtnCut.ToolTip = "Cut Group";
            //    //tbtnCut.IsEnabled = true;
            //    //if (CopiedTestGroupCase != null)
            //    //{
            //    //    if (CopiedTestGroupCase.IsCase == true)
            //    //        tbtnPaste.ToolTip = "Paste Sub Case";
            //    //    else
            //    //        tbtnPaste.ToolTip = "Paste Sub Group";
            //    //    tbtnCut.IsEnabled = true;
            //    //}
            //    tbtnDelete.ToolTip = "Delete Group";
            //    tbtnDelete.IsEnabled = true;
            //    if (index > 0)
            //    {
            //        tbtnMoveUp.ToolTip = "Move Up Case";
            //        tbtnMoveUp.IsEnabled = true;
            //    }
            //    if (tgc.TestGroupCaseParent == null)
            //    {
            //        if (index < TestColInstance.TestGroupCaseOc.Count - 1)
            //        {
            //            tbtnMoveDown.ToolTip = "Move Down Group";
            //            tbtnMoveDown.IsEnabled = true;
            //        }
            //    }
            //    else
            //    {
            //        if (index < tgc.TestGroupCaseParent.TestGroupCaseOc.Count - 1)
            //        {
            //            tbtnMoveDown.ToolTip = "Move Down Group";
            //            tbtnMoveDown.IsEnabled = true;
            //        }
            //    }
            //}

            //if (tgc.IsCase == true)
            //    lvTestCase.DataContext = tgc.TestGroupCaseParent.TestGroupCaseOc;
            //else
            //    lvTestCase.DataContext = tgc.TestGroupCaseOc;
        }

        #endregion

        private void iTester_Loaded(object sender, RoutedEventArgs e)
        {
			GlobalCfg_ListView_SelectionChanged(null, null);
			//PrivateCfg_ListView_SelectionChanged(null, null);
        }

        /// <summary>
        /// This call will before GroupCase_Image_ListViewItem_PreviewLeftMosueDown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GroupCase_ListViewItem_PreviewMosueDown(object sender, MouseButtonEventArgs e)
        {
            ListViewItem lvi = (ListViewItem)sender;
            lvi.IsSelected = true;
        }

        /// <summary>
        /// This call will after GroupCase_ListViewItem_PreviewMosueDown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GroupCase_Image_ListViewItem_PreviewLeftMosueDown(object sender, MouseButtonEventArgs e)
        {
            TestGroupCase tgc = (TestGroupCase)lvGroupCase.SelectedItem;
            if (tgc.IsCase == false)
                tgc.IsExpanded = !tgc.IsExpanded;
        }

        private FrameworkElement FindByName(string name, FrameworkElement root)
        {
            Stack<FrameworkElement> tree = new Stack<FrameworkElement>();
            tree.Push(root);

            while (tree.Count > 0)
            {
                FrameworkElement current = tree.Pop();
                if (current.Name == name)
                    return current;

                int count = VisualTreeHelper.GetChildrenCount(current);
                for (int i = 0; i < count; ++i)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(current, i);
                    if (child is FrameworkElement)
                        tree.Push((FrameworkElement)child);
                }
            }

            return null;
        }

        private void GroupCase_TreeViewItem_CheckBox_CheckedUnchecked(object sender, RoutedEventArgs e)
        {
            TestGroupCase tgc = (TestGroupCase)lvGroupCase.SelectedItem;
            int index = TestColInstance.GetGroupCaseIndex(tgc);
            ListViewItem lvi = (ListViewItem)lvGroupCase.ItemContainerGenerator.ContainerFromIndex(index);
			if (lvi == null)
				return;
            CheckBox cbSel = (CheckBox)FindByName("cbGroupCase", lvi);
            CheckBox cb = (CheckBox)sender;
            if (cb == cbSel)
            {
                tgc.SelectedState = (bool)cb.IsChecked;
                tgc.SelectedStateBackColor = new SolidColorBrush(Colors.Transparent);
                TestColInstance.UpdateParentChildSelectedState(tgc);
            }

            UpdateTotalTestCases();
        }

        private void GroupCase_ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AddGroupEnabled = false;
            AddSubGroupEnabled = false;
            AddSubCaseEnabled = false;
            EditNameEnabled = false;
            DeleteEnabled = false;
            MoveUpEnabled = false;
            MoveDownEnabled = false;

            if (lvGroupCase.Items.Count < 1)
            {
                AddGroupEnabled = true;
				wpgInstance.SelectedObject = null;
                return;
            }

            if(lvGroupCase.SelectedItem == null)
            {
                AddGroupEnabled = true;
				wpgInstance.SelectedObject = null;
                return;
            }

            TestGroupCase tgc = (TestGroupCase)lvGroupCase.SelectedItem;
            int index = TestColInstance.FindTestGroupCaseIndex(tgc);
            if (tgc.IsCase == true)
            {
                AddGroupEnabled = true;
                EditNameEnabled = true;
                DeleteEnabled = true;
                if (index > 0)
                    MoveUpEnabled = true;
                if (tgc.TestGroupCaseParent == null)
                {
                    if (index < TestColInstance.TestGroupCaseOc.Count - 1)
                        MoveDownEnabled = true;
                }
                else
                {
                    if (index < tgc.TestGroupCaseParent.TestGroupCaseOc.Count - 1)
                        MoveDownEnabled = true;
                }
				wpgInstance.SelectedObject = tgc.TestBaseInstance;
				tcConfigProperty.SelectedIndex = 2;
            }
            else
            {
                AddGroupEnabled = true;
                AddSubGroupEnabled = true;
                AddSubCaseEnabled = true;
                EditNameEnabled = true;
                DeleteEnabled = true;
                if (index > 0)
                    MoveUpEnabled = true;
                if (tgc.TestGroupCaseParent == null)
                {
                    if (index < TestColInstance.TestGroupCaseOc.Count - 1)
                        MoveDownEnabled = true;
                }
                else
                {
                    if (index < tgc.TestGroupCaseParent.TestGroupCaseOc.Count - 1)
                        MoveDownEnabled = true;
                }
				wpgInstance.SelectedObject = null;
            }
        }

		private void OpenTestCollection_Button_Click(object sender, RoutedEventArgs e)
		{
            if (CurrentDirtyFlag == true)
            {
                if (MessageBox.Show(CurrentFilePath + " hasn't been saved.\nDo you want to save it first?", "Save Test Collection",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (string.Compare(CurrentFilePath, "(None)") != 0)
                    {
                        if (GetSaveFilePath() != false)
                            DoSave();
                    }
                    else
                        DoSave();
                }
            }

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = false;
            dlg.Filter = "Test Collection (*.itc)|*.itc";
            dlg.InitialDirectory = GlobalSettings[NameStrings.ApplicationFolder] + @"\files";
            dlg.Title = "Open Test Collection";
            if (dlg.ShowDialog() != true)
                return;

            TestColInstance.TestGroupCaseOc.Clear();
            TestColInstance.TestGroupCaseOcDisp.Clear();

            CurrentFilePath = dlg.FileName;
            CurrentDirtyFlag = false;

            DoOpen();

            UpdateTotalTestCases();
        }

		private void NewTestCollection_Button_Click(object sender, RoutedEventArgs e)
		{
            if (CurrentDirtyFlag == true)
            {
                if (MessageBox.Show(CurrentFilePath + " hasn't been saved.\nDo you want to save it first?", "Save Test Collection",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (string.Compare(CurrentFilePath, "(None)") != 0)
                    {
                        if (GetSaveFilePath() != false)
                            DoSave();
                    }
                    else
                        DoSave();
                }
            }

            CurrentFilePath = "(None)";
            CurrentDirtyFlag = false;

            TestColInstance.TestGroupCaseOc.Clear();
            TestColInstance.TestGroupCaseOcDisp.Clear();
		}

		private void SaveTestCollection_Button_Click(object sender, RoutedEventArgs e)
		{
            if (string.Compare(CurrentFilePath, "(None)") == 0 && 
				CurrentDirtyFlag == false)
				return;

			if (string.Compare(CurrentFilePath, "(None)") == 0)
			{
				if (GetSaveFilePath() == false)
					return;
			}

            DoSave();

			MessageBox.Show(CurrentFilePath + " is saved.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
		}

        private void SaveAsTestCollection_Button_Click(object sender, RoutedEventArgs e)
        {
			if (string.Compare(CurrentFilePath, "(None)") == 0 &&
				CurrentDirtyFlag == false)
				return;

            if (GetSaveFilePath() == false)
                return;

            DoSave();
		
			MessageBox.Show(CurrentFilePath + " is saved.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
		}

        private bool GetSaveFilePath()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "TestCollection";
            sfd.DefaultExt = ".itc";
            sfd.Filter = "Test Collection (*.itc)|*.itc";
            sfd.AddExtension = true;
            sfd.OverwritePrompt = true;
            sfd.CheckPathExists = true;
            sfd.Title = "Save Test Collection As...";
            sfd.InitialDirectory = GlobalSettings[NameStrings.ApplicationFolder] + @"\files";

            bool? b = sfd.ShowDialog();
            if (b != true)
                return false;

            CurrentFilePath = sfd.FileName;

            return true;
        }

        private bool GetSaveCfgPath()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "TestConfiguration";
            sfd.DefaultExt = ".cfg";
            sfd.Filter = "Test Configuration (*.cfg)|*.cfg";
            sfd.AddExtension = true;
            sfd.OverwritePrompt = true;
            sfd.CheckPathExists = true;
            sfd.Title = "Save Test Configuration As...";
            sfd.InitialDirectory = GlobalSettings[NameStrings.ApplicationFolder] + @"\config";

            bool? b = sfd.ShowDialog();
            if (b != true)
                return false;

            GlobalCfgPath = sfd.FileName;

            return true;
        }

		private void DoSave()
		{
			#region Save XML

            try
            {
                //FileStream fs = new FileStream(CurrentFilePath, FileMode.Create);
                //XmlSerializer formatter = new XmlSerializer(typeof(TestCollection));
                //formatter.Serialize(fs, TestColInstance);

                StreamWriter sw = new StreamWriter(CurrentFilePath);

                sw.WriteLine("<itester>");
                sw.WriteLine("    <config>");
                sw.WriteLine("    </config>");
                sw.WriteLine("    <suites>");
                foreach (TestGroupCase tgc in TestColInstance.TestGroupCaseOc)
                {
                    DoSaveTestGroupCase(sw, tgc, 2);
                }
                sw.WriteLine("    </suites>");
                sw.WriteLine("</itester>");
                sw.Flush();
                sw.Close();
                sw.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot save " + CurrentFilePath + ".\nError message :\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

			#endregion

			CurrentDirtyFlag = false;
		}

        private string GetLevelPadding(int level)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < level * 4; i++)
            {
                sb.Append(" ");
            }
            return sb.ToString();
        }

        private void DoSaveTestGroupCase(StreamWriter sw, TestGroupCase tgc, int level)
        {
            if (tgc.IsCase)
            {
				//XmlSerializer formatter = new XmlSerializer(typeof(TestGroupCase));

				#region Calculate Relative Path

                int idx = tgc.TestFilePath.LastIndexOf(@"\");
                string strName = tgc.TestFilePath.Substring(idx + 1);
                string strPath = tgc.TestFilePath.Substring(0, idx);
                idx =  CurrentFilePath.LastIndexOf(@"\");
                string strApp = CurrentFilePath.Substring(0, idx);
                string strFinal = "";
                if (string.Compare(strPath.Substring(0, 1), strApp.Substring(0, 1)) != 0)
                    strFinal = tgc.TestFilePath;
                else if (strPath.StartsWith(@"\") || strPath.StartsWith(@"\"))
                    strFinal = tgc.TestFilePath;
                else
                {
                    string[] saPath = strPath.Split(new string[] { @"\" }, StringSplitOptions.None);
                    string[] saApp = strApp.Split(new string[] { @"\" }, StringSplitOptions.None);

                    int lsaP = saPath.Length;
                    int lsaA = saApp.Length;
                    int minLen = Math.Min(lsaP, lsaA);

                    int i = 0;
                    for (; i < minLen; i++)
                    {
                        if (string.Compare(saPath[i], saApp[i]) != 0)
                            break;
                    }
                    lsaA = lsaA - i;
                    for (; i < lsaP; i++)
                    {
                        strFinal = strFinal + saPath[i] + @"\";
                    }
                    for (i = 0; i < lsaA; i++)
                    {
                        strFinal = @"..\" + strFinal;
                    }

                    strFinal = strFinal + strName;
                }

				#endregion

				sw.WriteLine(GetLevelPadding(level) + "<groupcase type=\"case\"" +
                    " selected=\"" + ((tgc.SelectedState== true)? "true" : "false")+ 
                    "\" name=\"" + CommonOperations.ConvertFileString2XmlString(tgc.GroupCaseName) +
                    "\" path=\"" + CommonOperations.ConvertFileString2XmlString(strFinal) + "\">");//tgc.TestFilePath) + "\">");
                Type t = tgc.TestBaseInstance.GetType();
                PropertyInfo[] pia = t.GetProperties();
                foreach (PropertyInfo pi in pia)
                {
                    object obj = pi.GetValue(tgc.TestBaseInstance, null);
                    Type to = obj.GetType();
                    if (to.IsValueType || string.Compare(to.Name, "string") == 0)
                    {
                        sw.WriteLine(GetLevelPadding(level + 1) + "<item name=\"" +
                            CommonOperations.ConvertFileString2XmlString(pi.Name) +
                            "\" value=\"" +
                            CommonOperations.ConvertFileString2XmlString(obj.ToString()) +
                            "\"/>");
                    }
                }
                sw.WriteLine(GetLevelPadding(level) + "</groupcase>");
            }
            else
            {
                sw.WriteLine(GetLevelPadding(level) + "<groupcase type=\"group\"" +
                    " selected=\"" + ((tgc.SelectedState == true) ? "true" : "false") +
                    "\" name=\"" + CommonOperations.ConvertFileString2XmlString(tgc.GroupCaseName) + "\">");
                foreach (TestGroupCase tgci in tgc.TestGroupCaseOc)
                {
                    DoSaveTestGroupCase(sw, tgci, level + 1);
                }
                sw.WriteLine(GetLevelPadding(level) + "</groupcase>");
            }
        }

        private bool DoOpen()
        {
            try
            {
                //FileStream fs = new FileStream(CurrentFilePath, FileMode.Open);
                //XmlSerializer formatter = new XmlSerializer(typeof(TestCollection));
                //TestColInstance = (TestCollection)formatter.Deserialize(fs);

                XDocument xd = XDocument.Load(CurrentFilePath);
                XElement xeItester = xd.Element("itester");
                if (xeItester == null)
                {
                    MessageBox.Show("Cannot parse " + CurrentFilePath + ".", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                #region

                XElement xeConfig = xeItester.Element("config");
                if (xeConfig != null)
                {
                    // TODO
                }

                XElement xeSuites = xeItester.Element("suites");
                if (xeSuites != null)
                {
                    foreach (XElement xe in xeSuites.Elements("groupcase"))
                    {
                        ParseXMlToTestGroupCase(null, xe);
                    }

                    for (int i = 0; i < TestColInstance.TestGroupCaseOcDisp.Count; i++)
                    {
                        TestGroupCase tgc = TestColInstance.TestGroupCaseOcDisp[i];
                        if (tgc.IsCase == true)
                            TestColInstance.UpdateParentSelectedState(tgc);
                    }
                    for (int i = TestColInstance.TestGroupCaseOcDisp.Count -1; i >=0; i--)
                    {
                        TestGroupCase tgc = TestColInstance.TestGroupCaseOcDisp[i];
                        if (tgc.IsCase == true)
                            TestColInstance.UpdateParentSelectedState(tgc);
                    }
                    //foreach (TestGroupCase tgc in TestColInstance.TestGroupCaseOcDisp)
                    //{
                    //    if (tgc.IsCase == true)
                    //        TestColInstance.UpdateParentSelectedState(tgc);
                    //}
                }

                #endregion

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot open " + CurrentFilePath + ".\nError message :\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

		private void ParseXMlToTestGroupCase(TestGroupCase tgc, XElement xeSuite)
		{
			string groupOrCase = CommonOperations.ConvertXmlString2FileString(xeSuite.Attribute("type").Value);
            string groupCaseSelected = "true";
            if(xeSuite.Attribute("selected") != null)
                groupCaseSelected = xeSuite.Attribute("selected").Value;
            string groupCaseName = CommonOperations.ConvertXmlString2FileString(xeSuite.Attribute("name").Value);
            if (string.Compare(groupOrCase, "case") == 0)
			{
				string filePath = CommonOperations.ConvertXmlString2FileString(xeSuite.Attribute("path").Value);
				iTestBase.iTestBase tb = LoadTestBaseDllFromFile(filePath);
				if (tb == null)
					return;

				TestGroupCase tgct = null;
				if (tgc == null)
                    tgct = new TestGroupCase(TestColInstance, null, groupCaseName, true, tb, filePath);
				else
                    tgct = new TestGroupCase(TestColInstance, tgc, groupCaseName, true, tb, filePath);
                if (string.Compare(groupCaseSelected, "false") == 0)
                    tgct.SelectedState = false;
                else
                    tgct.SelectedState = true;
				TestColInstance.ConfigTestGroupCaseFromXML(tgct, xeSuite);
			}
			else
			{
				TestGroupCase tgct = null;
                if (tgc == null)
                    tgct = new TestGroupCase(TestColInstance, null, groupCaseName, false, null, null);
				else
                    tgct = new TestGroupCase(TestColInstance, tgc, groupCaseName, false, null, null);

                foreach (XElement xe in xeSuite.Elements("groupcase"))
                {
                    ParseXMlToTestGroupCase(tgct, xe);
                }
			}
		}

		private void OpenConfig()
		{
			if (string.IsNullOrWhiteSpace(GlobalCfgPath))
				GlobalCfgPath = (string)GlobalSettings[NameStrings.ApplicationFolder] + @"\system\globalCfg.cfg";
			try
			{
				XDocument xd = XDocument.Load(GlobalCfgPath);
				XElement xeItester = xd.Element("itester");
				if (xeItester == null)
				{
					MessageBox.Show("Cannot parse the global configuration.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				TestConfiguraionGlobalOc.Clear();
				XElement xeLocal = xeItester.Element("config");
				if (xeLocal != null)
				{
					foreach (XElement xe in xeLocal.Elements("item"))
					{
						string skey = CommonOperations.ConvertXmlString2FileString(xe.Attribute("key").Value);
						string sValue = CommonOperations.ConvertXmlString2FileString(xe.Attribute("value").Value);
						foreach (string s in _globalCfgEntries)
						{
							if (string.Compare(s, skey.Trim()) == 0)
								skey = s;
						}
						TestConfiguraionGlobalOc.Add(
							new TestConfiguration()
							{
								ConfigKey = skey,
								ConfigValue = sValue
							});
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Cannot open the global configuration file.\nError message :\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				MessageBox.Show("Use the default global configuration.", "Infromation", MessageBoxButton.OK, MessageBoxImage.Information);

				TestConfiguraionGlobalOc.Clear();
				foreach (string s in _globalCfgEntries)
				{
					TestConfiguraionGlobalOc.Add(
						new TestConfiguration()
						{
							ConfigKey = s,
							ConfigValue = ""
						});
				}

				SaveConfig();
			}

			bool modified = false;
			foreach (string s in _globalCfgEntries)
			{
				bool found = false;
				foreach (TestConfiguration tc in TestConfiguraionGlobalOc)
				{
					if (string.Compare(tc.ConfigKey.Trim(), s.Trim()) == 0)
					{
						found = true;
						break;
					}
				}
				if (found == false)
				{
					modified = true;
					TestConfiguraionGlobalOc.Add(
						new TestConfiguration()
						{
							ConfigKey = s,
							ConfigValue = ""
						});
				}
			}

			for (int i = 0; i < TestConfiguraionGlobalOc.Count; i++)
			{
				bool found = false;
				foreach (string s in _globalCfgEntries)
				{
					if (string.Compare(TestConfiguraionGlobalOc[i].ConfigKey.Trim(), s.Trim()) == 0)
					{
						found = true;
						break;
					}
				}
				if (found == true)
				{
					TestConfiguration tc = TestConfiguraionGlobalOc[i];
					TestConfiguraionGlobalOc.Remove(tc);
					TestConfiguraionGlobalOc.Insert(0, tc);
				}
            }

            #region Adjust

            TestConfiguration tcip = null;
            TestConfiguration tcp = null;
            TestConfiguration tcsp = null;

            foreach (TestConfiguration tci in TestConfiguraionGlobalOc)
            {
                if (tci.ConfigKey == _globalCfgEntries[CFG_INDEX_DBIP])
                    tcip = tci;
                else if (tci.ConfigKey == _globalCfgEntries[CFG_INDEX_PRJNAME])
                    tcp = tci;
                else if (tci.ConfigKey == _globalCfgEntries[CFG_INDEX_SUBPRJNAME])
                    tcsp = tci;
            }
            TestConfiguraionGlobalOc.Remove(tcip);
            TestConfiguraionGlobalOc.Remove(tcp);
            TestConfiguraionGlobalOc.Remove(tcsp);
            TestConfiguraionGlobalOc.Insert(0, tcsp);
            TestConfiguraionGlobalOc.Insert(0, tcp);
            TestConfiguraionGlobalOc.Insert(0, tcip);

            #endregion

            if (modified == true)
				SaveConfig();
		}

		private void SaveConfig()
		{
			try
			{
				StreamWriter sw = new StreamWriter(GlobalCfgPath);

				sw.WriteLine("<itester>");
				sw.WriteLine("    <config>");
				foreach (TestConfiguration tc in TestConfiguraionGlobalOc)
				{
					sw.WriteLine("        <item key=\"" +
						CommonOperations.ConvertFileString2XmlString(tc.ConfigKey) +
						"\" value=\"" +
						CommonOperations.ConvertFileString2XmlString(tc.ConfigValue) +
						"\"/>");
				}
				sw.WriteLine("    </config>");
				sw.WriteLine("</itester>");

				sw.Flush();
				sw.Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Cannot save " + GlobalCfgPath + ".\nError message :\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void Jumpout_Button_Click(object sender, RoutedEventArgs e)
		{

		}

		private void Stop_Button_Click(object sender, RoutedEventArgs e)
		{
			if (_thWorker.IsAlive == true)
			{
				_thWorker.Abort();
				//_thWorker.Join();
			}
			_thWorker = null;
		}

		private void Pause_Button_Click(object sender, RoutedEventArgs e)
		{
			InPause = true;
			if (_thWorker != null && _thWorker.IsAlive == true)
			{
				//_thWorker.p
			}
		}

		private void Start_Button_Click(object sender, RoutedEventArgs e)
		{
			if (TestColInstance.TestGroupCaseOc.Count < 1)
				return;

            if (DatabaseConnected == true &&
                (string.IsNullOrWhiteSpace(_curProject) || string.IsNullOrWhiteSpace(_curSubProject)))
            {
                MessageBox.Show("Project Name/Sub Project Name error.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

			if (InRun == true)
				InPause = false;
			else
			{
				InRun = true;
				InPause = false;
                TestResultOc.Clear();
                PassCountSingle = 0;
                PassCount = 0;
                FailCountSingle = 0;
                FailCount = 0;
                ErrorCountSingle = 0;
                ErrorCount = 0;
                InformationCountSingle = 0;
                InformationCount = 0;
                TotalCountSingle = 0;
                TotalCount = 0;

                PassCaseCount = 0;
                FailCaseCount = 0;
                ErrorCaseCount = 0;

				//dgTestResults.Focus();

                //_canSendMessage = true;
                _curTgcUnderTest = null;

                foreach (TestGroupCase tgc in TestColInstance.TestGroupCaseOcDisp)
                {
                    tgc.PassCount = 0;
                    tgc.PassImage = null;
                    //tgc.PassBackground = new SolidColorBrush(Colors.Transparent);
                    tgc.FailCount = 0;
                    tgc.FailImage = null;
                    //tgc.FailBackground = new SolidColorBrush(Colors.Transparent);
                    tgc.ErrorCount = 0;
                    tgc.ErrorImage = null;
                    //tgc.ErrorBackground = new SolidColorBrush(Colors.Transparent);
                    //tgc.PassRate = 0;
                    tgc.Duration = new TimeSpan();
                    tgc.AlreadyRun = false;
					tgc.InRun = true;
                }

				_intMSec = 0;
				_intSec = 0;
				_intMin = 0;
				_intHour = 0;
				_intDay = 0;
				UpdateTimerLabel();
				_timer.Start();

				_thWorker = new Thread(new ThreadStart(DoTestJobs));
				_thWorker.Start();
			}
		}

        private string GetGlobalConfigEntry(string skey)
        {
            foreach (TestConfiguration tc in TestConfiguraionGlobalOc)
            {
                if (string.Compare(tc.ConfigKey.Trim(), skey.Trim()) == 0)
                    return tc.ConfigValue.Trim();
            }

            return "";
        }

        private void DoTestJobs()
        {
            DateTime sdt = DateTime.Now;
            DateTime edt = DateTime.Now;
            try
            {
                if (DatabaseConnected)
                {
                    string pname = _curPorjectDB;
                    string spname = _curSubProjectDB;
					sdt = DateTime.Now;
					string strdt = sdt.Year.ToString() + "-" + sdt.Month.ToString() + "-" + sdt.Day.ToString()
						+ " " + sdt.Hour.ToString() + ":" + sdt.Minute.ToString() + ":" + sdt.Second.ToString();
					_testViewID = Environment.MachineName + Environment.UserName + strdt;
                    string sql = "INSERT INTO itester.testview(id,pspname,prate,tcount,fecount,stime,etime,duration,testpc,tester) "
                        + "VALUES('" + _testViewID + "','" + pname + spname + "','0%','0','0','"
						+ strdt + "','" + strdt + "','0.0hours','" + System.Environment.MachineName + "','" + System.Environment.UserName + "')";
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = _mysqlConn;
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }

                SendMessageCategory("Begin Test Collection", false);
                SendMessageData("Hostname", System.Environment.MachineName, false);
				SendMessageData("Username", System.Environment.UserName, false);
				SendMessageData("Test ID", GetTestID(), false);
				SendMessageData("Start time", DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString(), false);

                foreach (TestGroupCase tgc in TestColInstance.TestGroupCaseOc)
                {
                    Dispatcher.Invoke((ThreadStart)delegate()
                    {
                        if (tgc.IsCase == false && tgc.IsExpanded == false)
                            tgc.IsExpanded = true;
                    }, null);

                    DoConcreteTestJobsWrapper(tgc);
                    tgc.AlreadyRun = true;
                }
            }
            catch (Exception ex)
            {
				MessageBox.Show("Test is terminated.\nError message :\n" + ex.Message, "Jobs Error", MessageBoxButton.OK, MessageBoxImage.Error);

				SendMessage(iTestBase.iTestBase.TestStateEnum.Error, "", "Exception", "Test is terminated.", "", "", false);
                SendMessage(iTestBase.iTestBase.TestStateEnum.Error, "", "Exception", "Error message", ex.Message, "", false);
				string[] sa = Get255Lentring(ex.Message);
				foreach (string s in sa)
				{
                    SendMessage(iTestBase.iTestBase.TestStateEnum.Error, "", "", s, "", "");
				}
                SendMessage(iTestBase.iTestBase.TestStateEnum.Error, "", "Exception", "Stack Trace", "", "", false);
				sa = Get255Lentring(ex.StackTrace);
				foreach (string s in sa)
				{
                    SendMessage(iTestBase.iTestBase.TestStateEnum.Error, "", "", s, "", "", false);
				}
			}
            finally
            {
                InRun = false;
                InPause = false;
                ReadyString = "Ready";

                _curTgcUnderTest = null;

                SendMessageData("Stop time", DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString(), false);
                SendMessageCategory("Test Collection Ends.", false);

                if (DatabaseConnected)
                {
                    edt = DateTime.Now;
                    TimeSpan ts = edt.Subtract(sdt);
                    string sth = ts.TotalHours.ToString("F2") + "hours";
                    string strdt = edt.Year.ToString() + "-" + edt.Month.ToString() + "-" + edt.Day.ToString()
						+ " " + edt.Hour.ToString() + ":" + edt.Minute.ToString() + ":" + edt.Second.ToString();
                    string sql = "UPDATE itester.testview SET prate='" + PassRateString + "',tcount='" + AllCaseCountString
						+ "',fecount='" + (ErrorCaseCount + FailCaseCount).ToString() + "',etime='" + strdt + "',duration='" + sth + "' "
                        + "WHERE id='" + _testViewID + "'";
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = _mysqlConn;
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }

				foreach (TestGroupCase tgc in TestColInstance.TestGroupCaseOcDisp)
				{
					tgc.InRun = false;
				}

                _timer.Stop();
            }
        }

        private string GetTestID()
        {
            string udn = System.Environment.UserDomainName;
            string hn = System.Environment.MachineName;
            string un = System.Environment.UserName;
            DateTime dt = DateTime.Now;
            return udn + "-" + hn + "-" + un + "-" + 
                dt.Year.ToString() + "-" + 
                dt.Month.ToString() + "-" + 
                dt.Day.ToString() + "-" + 
                dt.Hour.ToString() + "-" + 
                dt.Minute.ToString() + "-" + 
                dt.Second.ToString() + "-" + 
                dt.Millisecond.ToString();
        }

		private void DoConcreteTestJobsWrapper(TestGroupCase tgc)
		{
			try
			{
				DoConcreteTestJobs(tgc);
			}
            catch (Exception ex)
            {
                SendMessage(iTestBase.iTestBase.TestStateEnum.Error, tgc.GroupCaseName, "Exception", "Test Case is terminated.", "", "", false);
                SendMessage(iTestBase.iTestBase.TestStateEnum.Error, tgc.GroupCaseName, "Exception", "Error message", ex.Message, "", false);
				string[] sa = Get255Lentring(ex.Message);
				foreach (string s in sa)
				{
                    SendMessage(iTestBase.iTestBase.TestStateEnum.Error, tgc.GroupCaseName, "", s, "", "");
				}
                SendMessage(iTestBase.iTestBase.TestStateEnum.Error, tgc.GroupCaseName, "Exception", "Stack Trace", "", "", false);
				sa = Get255Lentring(ex.StackTrace);
				foreach (string s in sa)
				{
                    SendMessage(iTestBase.iTestBase.TestStateEnum.Error, tgc.GroupCaseName, "", s, "", "", false);
				}
			}
		}

		private void DoConcreteTestJobs(TestGroupCase tgc)
		{
			if (tgc.IsCase == true)
			{
				#region Case

				if (tgc.SelectedState == false)
					return;

				DateTime sdt = DateTime.Now;

				if (DatabaseConnected)
				{
					_testColID = System.Environment.MachineName + "_" + System.Environment.UserName + "_"
						+ sdt.ToLongDateString() + "_" + sdt.ToLongTimeString() + "_" + sdt.Millisecond.ToString()
						+ "_" + tgc.GroupCaseName;
					string strdt = sdt.Year.ToString() + "-" + sdt.Month.ToString() + "-" + sdt.Day.ToString() + " " + 
						sdt.Hour.ToString() + ":" + sdt.Minute.ToString() + ":" + sdt.Second.ToString();
					string sql = "INSERT INTO itester.testcol(id,tvid,tname,stime,duration,ecount,fcount,pcount,icount) "
						+ "VALUES('" + _testColID + "','" + _testViewID + "','" + tgc.GroupCaseName + "','" + strdt + "','0.0hours','0','0','0','0')";
					MySqlCommand cmd = new MySqlCommand();
					cmd.Connection = _mysqlConn;
					cmd.CommandText = sql;
					cmd.ExecuteNonQuery();
				}

                SendMessage(iTestBase.iTestBase.TestStateEnum.None, tgc.GroupCaseName, "Enter case", "", "", "", false);

				_testResultIndex = 0;

				_curTgcUnderTest = tgc;

				Dispatcher.Invoke((ThreadStart)delegate()
				{
					int index = TestColInstance.GetGroupCaseIndex(tgc);
					ListViewItem lvi = (ListViewItem)lvGroupCase.ItemContainerGenerator.ContainerFromIndex(index);
                    if (lvi != null)
                    {
                        lvi.IsSelected = true;
                        lvGroupCase.ScrollIntoView(lvi);
                    }
				}, null);

				InformationCountSingle = 0;
				PassCountSingle = 0;
				FailCountSingle = 0;
				ErrorCountSingle = 0;
				TotalCountSingle = 0;

				ReadyString = tgc.GroupCaseName;

				try
				{
                    SendMessage(iTestBase.iTestBase.TestStateEnum.None, tgc.GroupCaseName, "Case Setup", "");

					tgc.TestBaseInstance.Setup();

					for (int i = 0; i < tgc.TestBaseInstance.RunCount; i++)
					{
                        SendMessage(iTestBase.iTestBase.TestStateEnum.None, tgc.GroupCaseName, "Case Run", "");

						if (tgc.TestBaseInstance.RunCount > 1)
						{
                            SendMessage(iTestBase.iTestBase.TestStateEnum.None, tgc.GroupCaseName, "Loop " + (i + 1).ToString() + " / " + tgc.TestBaseInstance.RunCount.ToString(), "");

							Dispatcher.Invoke((ThreadStart)delegate()
							{
								ReadyString = tgc.GroupCaseName + " ( " + (i + 1).ToString() + " / " + tgc.TestBaseInstance.RunCount.ToString() + " )";
							}, null);
						}

						tgc.TestBaseInstance.Run();
					}

                    SendMessage(iTestBase.iTestBase.TestStateEnum.None, tgc.GroupCaseName, "Case Clean", "");

					tgc.TestBaseInstance.Clean();
				}
				catch (Exception ex)
				{
                    SendMessage(iTestBase.iTestBase.TestStateEnum.Error, tgc.GroupCaseName, "Exception", "Test Case is terminated.", "");
                    SendMessage(iTestBase.iTestBase.TestStateEnum.Error, tgc.GroupCaseName, "Exception", "Error message", "");
					string[] sa = Get255Lentring(ex.Message);
					foreach (string s in sa)
					{
                        SendMessage(iTestBase.iTestBase.TestStateEnum.Error, tgc.GroupCaseName, "", s, "", "");
					}
                    SendMessage(iTestBase.iTestBase.TestStateEnum.Error, tgc.GroupCaseName, "Exception", "Stack Trace", "", "");
					sa = Get255Lentring(ex.StackTrace);
					foreach (string s in sa)
					{
                        SendMessage(iTestBase.iTestBase.TestStateEnum.Error, tgc.GroupCaseName, "", s, "", "");
					} 
				}

                SendMessage(iTestBase.iTestBase.TestStateEnum.None, tgc.GroupCaseName, "Exit case", "", "", "", false);

				Dispatcher.Invoke((ThreadStart)delegate()
				{
					if (tgc.FailCount < 1 && tgc.ErrorCount < 1)
					{
						tgc.PassImage = new BitmapImage();
						tgc.PassImage.BeginInit();
						tgc.PassImage.UriSource = new Uri("pack://application:,,,/itester;component/resources/status_ok.png");
						tgc.PassImage.EndInit();

						TestGroupCase tgct = tgc.TestGroupCaseParent;
						while (tgct != null)
						{
							//tgct.PassBackground = new SolidColorBrush(Colors.Green);
							tgct.PassCount++;

							tgct = tgct.TestGroupCaseParent;
						}
					}

					if (tgc.FailCount > 0)
					{
						tgc.FailImage = new BitmapImage();
						tgc.FailImage.BeginInit();
						tgc.FailImage.UriSource = new Uri("pack://application:,,,/itester;component/resources/status_error.png");
						tgc.FailImage.EndInit();

						TestGroupCase tgct = tgc.TestGroupCaseParent;
						while (tgct != null)
						{
							//tgct.FailBackground = new SolidColorBrush(Colors.Red);
							tgct.FailCount++;

							tgct = tgct.TestGroupCaseParent;
						}
					}

					if (tgc.ErrorCount > 0)
					{
						tgc.ErrorImage = new BitmapImage();
						tgc.ErrorImage.BeginInit();
						tgc.ErrorImage.UriSource = new Uri("pack://application:,,,/itester;component/resources/status_ques.ico");
						tgc.ErrorImage.EndInit();

						TestGroupCase tgct = tgc.TestGroupCaseParent;
						while (tgct != null)
						{
                            //tgct.ErrorBackground = new SolidColorBrush(Colors.Yellow);
							tgct.ErrorCount++;

							tgct = tgct.TestGroupCaseParent;
						}
					}

                    PassCaseCount = 0;
                    FailCaseCount = 0;
                    ErrorCaseCount = 0;
                    foreach (TestGroupCase tgcit in TestColInstance.TestGroupCaseOc)
                    {
                        PassCaseCount = PassCaseCount + tgcit.PassCount;
                        FailCaseCount = FailCaseCount + tgcit.FailCount;
                        ErrorCaseCount = ErrorCaseCount + tgcit.ErrorCount;
                    }

				}, null);

				_curTgcUnderTest = null;

				tgc.Duration = DateTime.Now.Subtract(sdt);

				if (DatabaseConnected)
				{
					string sth = tgc.Duration.TotalHours.ToString("F2") + "hours";
					string sql = "UPDATE itester.testcol SET duration='" + sth + "',ecount='" + ErrorCountSingleString
						+ "',fcount='" + FailCountSingleString + "',pcount='" + PassCountSingleString
						+ "',icount='" + InformationCountSingleString + "' "
						+ "WHERE id='" + _testColID + "'";
					MySqlCommand cmd = new MySqlCommand();
					cmd.Connection = _mysqlConn;
					cmd.CommandText = sql;
					cmd.ExecuteNonQuery();
				}

				#endregion
			}
			else
			{
				DateTime dt = DateTime.Now;

				foreach (TestGroupCase tgci in tgc.TestGroupCaseOc)
				{
					Dispatcher.Invoke((ThreadStart)delegate()
					{
						if (tgci.IsCase == false && tgci.IsExpanded == false)
							tgci.IsExpanded = true;
					}, null);
					
					DoConcreteTestJobs(tgci);

					tgci.AlreadyRun = true;
				}

				tgc.Duration = DateTime.Now.Subtract(dt);
			}
		}

		private string[] Get255Lentring(string src)
		{
			if (string.IsNullOrWhiteSpace(src))
				return new string[] { };
			src = src.Trim();
			int count = (int)Math.Ceiling((double)src.Length / 255.0);
			string[] sa = new string[count];
			for (int i = 0; i < count - 1; i++)
			{
				string s = src.Substring(0, 255);
				sa[i] = s;
				src = src.Substring(255);
			}
			sa[count - 1] = src;
			return sa;
		}

		#region Method

		private iTestBase.iTestBase LoadTestBaseDllFromFile(string fileName, bool fromFile = true)
		{
			iTestBase.iTestBase tb = null;
			try
			{
                Assembly asm = null;
                if (fromFile == true)
                {
                    int index = CurrentFilePath.LastIndexOf("\\");
                    string sf = CurrentFilePath.Substring(0, index);
                    string[] sfa = sf.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
                    string[] sda = fileName.Split(new string[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
                    int i = 0;
                    for (; i < sda.Length; )
                    {
                        if (sda[i] != "..")
                            break;
                        i++;
                    }
                    string sFinale = "";
                    if (i == 0)
                        asm = Assembly.LoadFrom(fileName);
                    else
                    {
                        for (int idx = 0; idx < sfa.Length - i; idx++)
                        {
                            if (sFinale == "")
                                sFinale = sfa[idx];
                            else
                                sFinale = sFinale + "\\" + sfa[idx];
                        }
                        for (int idx = i; idx < sda.Length; idx++)
                        {
                            if (sFinale == "")
                                sFinale = sda[idx];
                            else
                                sFinale = sFinale + "\\" + sda[idx];
                        }
                        asm = Assembly.LoadFrom(sFinale);
                    }
                }
                else
                {
                    asm = Assembly.LoadFrom(fileName);
                }
				Type[] ts = asm.GetTypes();
				ConstructorInfo ci = ts[0].GetConstructor(new Type[] { });
				tb = (iTestBase.iTestBase)ci.Invoke(new object[] { });
                tb.SendMessage += SendMessage;

				return tb;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Cannot load " + fileName +
					".\nPlease check whether it is a right dll file.\nError message :\n" + ex.Message,
					"Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return null;
			}
		}

        private void UpdateTotalTestCases()
        {
            List<TestGroupCase> listTgc = new List<TestGroupCase>();
            Stack<TestGroupCase> stackTgc = new Stack<TestGroupCase>();
            foreach (TestGroupCase tgc in TestColInstance.TestGroupCaseOc)
            {
                stackTgc.Push(tgc);
            }

            while (true)
            {
                if (stackTgc.Count < 1)
                    break;

                TestGroupCase tgc = stackTgc.Pop();
                if (listTgc.IndexOf(tgc) > -1)
                {
                    int i = 0;
                    foreach (TestGroupCase tgci in tgc.TestGroupCaseOc)
                    {
                        if (tgci.IsCase)
                        {
                            if (tgci.SelectedState == true)
                                i++;
                        }
                        else
                            i = i + tgci.TotalTests;
                    }
                    tgc.TotalTests = i;
                }
                else
                {
                    bool hasSubGroup = false;
                    foreach (TestGroupCase tgci in tgc.TestGroupCaseOc)
                    {
                        if (tgci.IsCase == false)
                        {
                            hasSubGroup = true;
                            break;
                        }
                    }

                    if (hasSubGroup == true)
                    {
                        listTgc.Add(tgc);
                        stackTgc.Push(tgc);
                        foreach (TestGroupCase tgci in tgc.TestGroupCaseOc)
                        {
                            if (tgci.IsCase == false)
                            {
                                stackTgc.Push(tgci);
                            }
                        }
                    }
                    else
                    {
                        int i = 0;
                        foreach (TestGroupCase tgci in tgc.TestGroupCaseOc)
                        {
                            if (tgci.SelectedState == true)
                                i++;
                        }
                        tgc.TotalTests = i;
                    }
                }
            }

            AllCaseCount = 0;
            foreach (TestGroupCase tgc in TestColInstance.TestGroupCaseOc)
            {
                AllCaseCount = AllCaseCount + tgc.TotalTests; 
            }
        }

        #region Message Related

		private void SendMessageOnly(string message, bool logDb = true)
		{
			SendMessage(iTestBase.iTestBase.TestStateEnum.None, "", "", message, "", "", logDb);
		}

		private void SendMessageCategory(string category, bool logDb = true)
		{
            SendMessage(iTestBase.iTestBase.TestStateEnum.None, "", category, "", "", "", logDb);
		}

		private void SendMessageData(string message, string dataValue, bool logDb = true)
		{
            SendMessage(iTestBase.iTestBase.TestStateEnum.None, "", "", message, dataValue, "", logDb);
		}

		private void SendMessage(string category, string message = "", bool logDb = true)
		{
            SendMessage(iTestBase.iTestBase.TestStateEnum.None, "", category, message, "", "", logDb);
		}

        private void SendMessage(iTestBase.iTestBase.TestStateEnum tse, string testName, string category, string message, string dataValue = "", string constraint = "", bool logDb = true)
        {
            Dispatcher.Invoke((ThreadStart)delegate()
            {
                if (TestResultOc.Count > MAX_TESTRESULTOC_COUNT)
                    TestResultOc.RemoveAt(0);
				TestResultOc.Add(new TestResult()
				{
					TestState = tse,
					TestName = testName,
					Category = category,
					Message = message,
					DataValue = dataValue,
					Constraint = constraint
				});
                switch (tse)
                {
                    default:
                    case iTestBase.iTestBase.TestStateEnum.None:
                        break;
                    case iTestBase.iTestBase.TestStateEnum.Infromation:
                        InformationCountSingle++;
                        InformationCount++;
                        break;
                    case iTestBase.iTestBase.TestStateEnum.Pass:
                        PassCountSingle++;
                        PassCount++;
                        break;
                    case iTestBase.iTestBase.TestStateEnum.Fail:
                        FailCountSingle++;
                        FailCount++;
                        Dispatcher.Invoke((ThreadStart)delegate()
                        {
                            if (_curTgcUnderTest != null)
                            {
                                _curTgcUnderTest.FailImage = new BitmapImage();
                                _curTgcUnderTest.FailImage.BeginInit();
                                _curTgcUnderTest.FailImage.UriSource = new Uri("pack://application:,,,/itester;component/resources/status_error.png");
                                _curTgcUnderTest.FailImage.EndInit();
                            }
                        }, null);
                        break;
                    case iTestBase.iTestBase.TestStateEnum.Error:
                        ErrorCountSingle++;
                        ErrorCount++;
                        Dispatcher.Invoke((ThreadStart)delegate()
                        {
                            if (_curTgcUnderTest != null)
                            {
                                _curTgcUnderTest.ErrorImage = new BitmapImage();
                                _curTgcUnderTest.ErrorImage.BeginInit();
                                _curTgcUnderTest.ErrorImage.UriSource = new Uri("pack://application:,,,/itester;component/resources/status_ques.ico");
                                _curTgcUnderTest.ErrorImage.EndInit();
                            }
                        }, null);
                        break;
                }
                TotalCountSingle++;
                TotalCount++;

                //ListViewItem lvi = (ListViewItem)lvTestResults.ItemContainerGenerator.ContainerFromIndex(TestResultOc.Count - 1);
                //lvi.IsSelected = true;
                //lvTestResults.ScrollIntoView(lvi);
            }, null);

            if (DatabaseConnected == true && logDb == true)
            {
				DateTime dt = DateTime.Now;
				string strdt = dt.Year.ToString() + "-" + dt.Month.ToString() + "-" + dt.Day.ToString() + " " +
					dt.Hour.ToString() + ":" + dt.Minute.ToString() + ":" + dt.Second.ToString();
				string sql = "INSERT INTO itester." + CommonOperations.GetValidDatabaseName(_curProject) + "testresult(msgidx,tcid,state,stime,msec,category,message,value,const) "
					+ "VALUES('" + _testResultIndex + "','" + _testColID + "','"
					+ ((int)tse).ToString() + "','" + strdt + "','" + dt.Millisecond + "','" + category + "','"
					+ message + "','" + dataValue + "','" + constraint + "')";
				MySqlCommand cmd = new MySqlCommand();
				cmd.Connection = _mysqlConn;
				cmd.CommandText = sql;
				cmd.ExecuteNonQuery();

                //Thread.Sleep(100);
			}
        }

        #endregion

		private void GlobalCfg_ListView_SelectionChanged(object sender, SelectedCellsChangedEventArgs e)
		{
			GlobalCfgAddEnabled = false;
			GlobalCfgDeleteEnabled = false;
			GlobalCfgClearEnabled = false;
			GlobalCfgDeleteAllEnabled = false;

			if (dgGlobalCfg.Items.Count < 1)
			{
				GlobalCfgAddEnabled = true;

				return;
			}

			int intIndex = dgGlobalCfg.SelectedIndex;
			if (intIndex < 0)
			{
				GlobalCfgAddEnabled = true;
				if (dgGlobalCfg.Items.Count > _globalCfgEntries.Length)
					GlobalCfgDeleteAllEnabled = true;

				return;
			}

			if (intIndex < _globalCfgEntries.Length)
			{
				GlobalCfgAddEnabled = true;
				GlobalCfgClearEnabled = true;
				if (dgGlobalCfg.Items.Count > _globalCfgEntries.Length)
					GlobalCfgDeleteAllEnabled = true;

				return;
			}

			GlobalCfgAddEnabled = true;
			GlobalCfgDeleteEnabled = true;
			GlobalCfgClearEnabled = true;
			GlobalCfgDeleteAllEnabled = true;
		}

		//private void PrivateCfg_ListView_SelectionChanged(object sender, SelectedCellsChangedEventArgs e)
		//{
		//    PrivateCfgAddEnabled = false;
		//    PrivateCfgDeleteEnabled = false;
		//    PrivateCfgClearEnabled = false;
		//    PrivateCfgDeleteAllEnabled = false;

		//    if (lvPrivateCfg.Items.Count < 1)
		//    {
		//        PrivateCfgAddEnabled = true;

		//        return;
		//    }

		//    int intIndex = lvPrivateCfg.SelectedIndex;
		//    if (intIndex < 0)
		//    {
		//        PrivateCfgAddEnabled = true;
		//        PrivateCfgDeleteAllEnabled = true;

		//        return;
		//    }

		//    PrivateCfgAddEnabled = true;
		//    PrivateCfgDeleteEnabled = true;
		//    PrivateCfgClearEnabled = true;
		//    PrivateCfgDeleteAllEnabled = true;
		//}

		private void EnabledDisableDatebase_Button_Click(object sender, RoutedEventArgs e)
		{
			Thread th = new Thread(new ThreadStart(EnabledDisableDatebaseThread));
			th.Start();
		}

		private void EnabledDisableDatebaseThread()
		{
			try
			{
				DoEnabledDisableDatebase(!DatabaseConnected);
                DatabaseConnected = !DatabaseConnected;
            }
			catch (Exception ex)
			{
				if (DatabaseConnected == true)
				{
					MessageBox.Show("Cannot disconnect the database.\nError message :\n"+ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				else
				{
					MessageBox.Show("Cannot connect the database.\nError message :\n"+ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void DoEnabledDisableDatebase(bool doConn)
		{
			if (doConn == false)
			{
				Dispatcher.Invoke((ThreadStart)delegate()
				{
					ProjectOc.Clear();
					SubProjectOc.Clear();
				}, null);
                _curProject = "";
                _curPorjectDB = "";
                _curSubProject = "";
                _curSubProjectDB = "";
            }
			else
			{
				Dispatcher.Invoke((ThreadStart)delegate()
				{
					ProjectOc.Clear();
					SubProjectOc.Clear();
				}, null);

				MySqlConnection conn = new MySqlConnection();
				string s = "server=" + DBIPAddress + ";" + "uid=root;pwd=qwewq;";
				conn.ConnectionString = s;
				conn.Open();

				string sql = "SELECT * FROM itester.project";
				MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
				DataSet ds = new DataSet();
				da.Fill(ds, "project");
				foreach (DataRow dr in ds.Tables["project"].Rows)
				{
					Dispatcher.Invoke((ThreadStart)delegate()
					{
                        ProjectOc.Add(new Tuple<string, string>((string)dr["pname"], (string)dr["pdname"]));
                    }, null);
				}
				if (ProjectOc.Count > 0)
				{
					sql = "SELECT * FROM itester.subproject";
					da = new MySqlDataAdapter(sql, conn);
					ds = new DataSet();
					da.Fill(ds, "subproject");
					Dispatcher.Invoke((ThreadStart)delegate()
					{
                        Tuple<string, ObservableCollection<Tuple<string, string>>> ti = null;
						foreach (DataRow dr in ds.Tables["subproject"].Rows)
						{
							string p = (string)dr["pname"];
                            string sp = (string)dr["spname"];
                            string spd = (string)dr["spdname"];
                            ti = null;
                            foreach (Tuple<string, ObservableCollection<Tuple<string, string>>> t in SubProjectOc)
							{
								if (t.Item1 == p)
								{
									ti = t;
									break;
								}
							}
							if (ti != null)
								ti.Item2.Add(new Tuple<string, string>(sp, spd));
							else
							{
                                ObservableCollection<Tuple<string, string>> spoc = new ObservableCollection<Tuple<string, string>>();
                                spoc.Add(new Tuple<string, string>(sp, spd));
                                ti = new Tuple<string, ObservableCollection<Tuple<string, string>>>(p, spoc);
								SubProjectOc.Add(ti);
							}
						}
					}, null);
				}
				TestConfiguration tcpn = null;
				TestConfiguration tcspn = null;
				foreach (TestConfiguration tc in TestConfiguraionGlobalOc)
				{
					if (tc.ConfigKey == "Project Name")
						tcpn = tc;
					else if (tc.ConfigKey == "Sub Project Name")
						tcspn = tc;
				}
				bool find = false;
                Tuple<string, string> tp = null;
                foreach (Tuple<string, string> ti in ProjectOc)
				{
					if (ti.Item2 == tcpn.ConfigValue)
					{
                        tp = ti;
						find = true;
						break;
					}
				}
				if (find == false)
				{
                    tcpn.ConfigValue = "";
                    tcspn.ConfigValue = "";
                    MessageBox.Show("Project is not selected correctly.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}
                _curProject = tp.Item2;
                _curPorjectDB = tp.Item1;
                Tuple<string, ObservableCollection<Tuple<string, string>>> tii = null;
                foreach (Tuple<string, ObservableCollection<Tuple<string, string>>> t in SubProjectOc)
				{
					if (t.Item1 == tp.Item1)
					{
						tii = t;
						break;
					}
				}
				if (tii == null)
				{
					MessageBox.Show("Database has error in the Sub Project table.\nProject Name : " + tcpn.ConfigValue, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}
				find = false;
                Tuple<string, string> tsp = null;
                foreach (Tuple<string, string> ti in tii.Item2)
				{
                    if (ti.Item2 == tcspn.ConfigValue)
					{
                        tsp = ti;
						find = true;
						break;
					}
				}
				if (find == false)
				{
					tcspn.ConfigValue = "";
					MessageBox.Show("Sub Project is not selected correctly.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}
                _curSubProject = tsp.Item2;
                _curSubProjectDB = tsp.Item1;
            }
        }

		private void GlobalConfig_Add_Button_Click(object sender, RoutedEventArgs e)
		{
			ObservableCollection<string> ls = new ObservableCollection<string>();
			foreach (TestConfiguration tc in TestConfiguraionGlobalOc)
			{
				ls.Add(tc.ConfigKey);
			}

			OneLineEditor ole = new OneLineEditor("New Global Entry", "Key", userCompareList: ls);
			bool? bv = ole.ShowDialog();
			if (bv != true)
				return;
			string s = ole.UserContent.Trim();
			TestConfiguraionGlobalOc.Add(new TestConfiguration()
			{
				ConfigKey = s,
				ConfigValue = ""
			});

			SaveConfig();
		}

        private void GlobalConfig_Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            if (dgGlobalCfg.Items.Count < 1)
                return;
            if (dgGlobalCfg.SelectedIndex < _globalCfgEntries.Length)
                return;

            TestConfiguration tc = TestConfiguraionGlobalOc[dgGlobalCfg.SelectedIndex];
            if (MessageBox.Show("Are you sure to delete \"" + tc.ConfigKey + "\"?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            TestConfiguraionGlobalOc.Remove(tc);

            SaveConfig();
        }

        private void GlobalConfig_Modify_Button_Click(object sender, RoutedEventArgs e)
        {
            if (dgGlobalCfg.Items.Count < 1)
                return;
            if (dgGlobalCfg.SelectedIndex < 0)
                return;

            TestConfiguration tc = TestConfiguraionGlobalOc[dgGlobalCfg.SelectedIndex];

            if (tc.ConfigKey == "Project Name" || tc.ConfigKey == "Sub Project Name")
            {
                if (DatabaseConnected == false)
                {
                    MessageBox.Show("Database is disconnected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else
                {
                    if (tc.ConfigKey == "Project Name")
                    {
                        if (ProjectOc == null || ProjectOc.Count < 1)
                        {
                            MessageBox.Show("Please create the project in the database first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        Tuple<string, string> ti = null;
                        foreach (Tuple<string, string> t in ProjectOc)
                        {
                            if (t.Item2 == tc.ConfigValue)
                            {
                                ti = t;
                                break;
                            }
                        }

                        OneLineSelector ols = new OneLineSelector("Select Project", "Project Name", ProjectOc, (string.IsNullOrWhiteSpace(tc.ConfigValue)) ? null : tc.ConfigValue);
                        bool? b = ols.ShowDialog();
                        if (b != true)
                            return;
                        tc.ConfigValue = ols.UserSelectContent;
                        _curProject = tc.ConfigValue;
                        ti = null;
                        foreach (Tuple<string, string> t in ProjectOc)
                        {
                            if (t.Item2 == tc.ConfigValue)
                            {
                                ti = t;
                                break;
                            }
                        }
                        _curPorjectDB = ti.Item1;

                        foreach (TestConfiguration tci in TestConfiguraionGlobalOc)
                        {
                            if (tci.ConfigKey == "Sub Project Name")
                            {
                                tci.ConfigValue = "";
                            }
                        }
                        _curSubProject = "";
                        _curSubProjectDB = "";
                    }
                    else
                    {
                        if (ProjectOc == null || ProjectOc.Count < 1)
                        {
                            MessageBox.Show("Please create the project in the database first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        if (string.IsNullOrWhiteSpace(_curProject))
                        {
                            MessageBox.Show("Please select the project first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        Tuple<string, string> ti = null;
                        foreach (Tuple<string, string> t in ProjectOc)
                        {
                            if (t.Item2 == _curProject)
                            {
                                ti = t;
                                break;
                            }
                        }

                        Tuple<string, ObservableCollection<Tuple<string, string>>> tii = null;
                        foreach (Tuple<string, ObservableCollection<Tuple<string, string>>> t in SubProjectOc)
                        {
                            if (t.Item1 == ti.Item1)
                            {
                                tii = t;
                                break;
                            }
                        }

                        if (tii == null || tii.Item2.Count < 1)
                        {
                            MessageBox.Show("Please create the sub project for the Project \"" + _curProject + "\"in the database first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        OneLineSelector ols = new OneLineSelector("Select Sub Project", "Sub Project Name", tii.Item2, (string.IsNullOrWhiteSpace(tc.ConfigValue)) ? null : tc.ConfigValue);
                        bool? b = ols.ShowDialog();
                        if (b != true)
                            return;
                        tc.ConfigValue = ols.UserSelectContent;
                        _curSubProject = tc.ConfigValue;
                        ti = null;
                        foreach (Tuple<string, string> t in tii.Item2)
                        {
                            if (t.Item2 == _curSubProject)
                            {
                                ti = t;
                                break;
                            }
                        }
                        _curSubProjectDB = ti.Item1;
                    }
                }
            }
            else
            {
                List<string> ls = new List<string>();
                foreach (TestConfiguration tci in TestConfiguraionGlobalOc)
                {
                    ls.Add(tci.ConfigKey);
                }

                OneLineEditor ole = new OneLineEditor("Modify Global Entry", "New Value", tc.ConfigValue);
                bool? bv = ole.ShowDialog();
                if (bv != true)
                    return;
                string s = ole.UserContent.Trim();
                tc.ConfigValue = s;
            }

            SaveConfig();
        }

        private void GlobalConfig_DelAll_Button_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure to delete all entries?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            for (int i = TestConfiguraionGlobalOc.Count - 1; i >= _globalCfgEntries.Length; i--)
            {
                TestConfiguraionGlobalOc.RemoveAt(i);
            }

            SaveConfig();
        }

		private void LoadGlobalConfig_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = false;
            dlg.Filter = "Test Configuration (*.cfg)|*.cfg";
            dlg.InitialDirectory = GlobalSettings[NameStrings.ApplicationFolder] + @"\config";
            dlg.Title = "Open Test Configuration";
            if (dlg.ShowDialog() != true)
                return;

            GlobalCfgPath = dlg.FileName;

            OpenConfig();
        }

        private void GlobalConfiguration_DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender == null)
                return;
            DataGrid grid = sender as DataGrid;
            if (grid == null)
                return;
            if (grid.Items.Count < 1)
                return;
            if (grid.SelectedIndex < 0)
                return;

            GlobalConfig_Modify_Button_Click(null, null);
        }

        //private void MsgFilter_Button_Click(object sender, RoutedEventArgs e)
        //{
        //    cmMsgFilter.IsOpen = true;
        //}

        //private void InitializeTestGroupAndTestGroupCase()
        //{
        //    InitializeTestGroup();
        //    //InitializeTestGroupCase();
        //}

        //private void InitializeTestGroup()
        //{
        //    tvTestGroup.Items.Add(CreateGroupTreeViewItem("Default Group"));
        //}

        //private void UpdateGroupTreeViewItemCount(TreeViewItem tvi, int folder, int all, int self)
        //{
        //    ((Label)((StackPanel)tvi.Header).Children[2]).Content = string.Format("( {0} : {1} : {2} )", folder, all, self);
        //    tvi.ToolTip = string.Format("{0} subgroup" + ((folder > 1) ? "s" : "") +
        //        ", {1} total case" + ((all > 1) ? "s" : "") +
        //        ", {2} direct case" + ((self > 1) ? "s" : ""), folder, all, self);
        //}

        //private void InitializeTestGroupCase()
        //{
        //    TestGroupCaseListOc.Add(new TestGroupCase());
        //}

        #endregion
    }

    #region Converter

    [ValueConversion(typeof(string), typeof(int))]
    public class StringIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
        CultureInfo culture)
        {
            int intValue = (int)value;
            return intValue.ToString();
        }
        public object ConvertBack(object value, Type targetType, object parameter,
        CultureInfo culture)
        {
            string strValue = (string)value;
            return int.Parse(strValue);
        }
    }

    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
        CultureInfo culture)
        {
            bool bValue = (bool)value;
            if (bValue == true)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter,
        CultureInfo culture)
        {
            string strValue = (string)value;
            return int.Parse(strValue);
        }
    }

    public class BoolsBoolConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter,
        CultureInfo culture)
        {
            bool bRetVal = true;
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] is bool)
                    {
                        bRetVal = bRetVal & (bool)(values[i]);
                    }
                    else
                        bRetVal = bRetVal & false;
                }
                return bRetVal;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
        CultureInfo culture)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }

	public sealed class ListViewTestResultConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter,
		CultureInfo culture)
		{
			ListViewItem item = (ListViewItem)value;
			ListView listView = ItemsControl.ItemsControlFromItemContainer(item) as ListView;
			if (listView == null)
				return Brushes.Transparent;
			// Get the index of a ListViewItem 
			int index = listView.ItemContainerGenerator.IndexFromContainer(item);

			TestResult tr = (TestResult)listView.Items[index];
			if (tr == null)
				return Brushes.Transparent;

			switch (tr.TestState)
			{
				default:
					return Brushes.Transparent;
				case iTestBase.iTestBase.TestStateEnum.Error:
                    return Brushes.Yellow;
				case iTestBase.iTestBase.TestStateEnum.Fail:
					return Brushes.Red;
			}

			//if (index % 2 == 0)
			//{
			//    return Brushes.LightBlue;
			//}
			//else
			//{
			//    return Brushes.Beige;
			//}
		}

		public object ConvertBack(object value, Type targetTypes, object parameter,
		CultureInfo culture)
		{
			throw new Exception("The method or operation is not implemented.");
		}
	}

    public class BoolsBoolOrConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter,
        CultureInfo culture)
        {
            bool bRetVal = false;
            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] is bool)
                    {
                        bRetVal = bRetVal | (bool)(values[i]);
                    }
                    //else
                    //    bRetVal = bRetVal | false;
                }
                return bRetVal;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
        CultureInfo culture)
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }

    #endregion

    //public class INotifyPropertyChangedClass : INotifyPropertyChanged
    //{
    //    public event PropertyChangedEventHandler PropertyChanged;
    //    public void NotifyPropertyChanged(string propertyName)
    //    {
    //        if (PropertyChanged != null)
    //            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    //    }
    //}

    #region Test Data Structure

    //public class TestCollection : INotifyPropertyChangedClass
    //{
    //    ObservableCollection<TestGroupCase> _testGroupCaseOcDisp;
    //    public ObservableCollection<TestGroupCase> TestGroupCaseOcDisp
    //    {
    //        get
    //        {
    //            return _testGroupCaseOcDisp;
    //        }
    //        set
    //        {
    //            _testGroupCaseOcDisp = value;
    //        }
    //    }

    //    ObservableCollection<TestGroupCase> _testGroupCaseOc = new ObservableCollection<TestGroupCase>();
    //    public ObservableCollection<TestGroupCase> TestGroupCaseOc
    //    {
    //        get
    //        {
    //            return _testGroupCaseOc;
    //        }
    //        set
    //        {
    //            _testGroupCaseOc = value;
    //        }
    //    }

    //    public TestCollection(ObservableCollection<TestGroupCase> testGroupCaseOcDisp)
    //    {
    //        _testGroupCaseOcDisp = testGroupCaseOcDisp;
    //    }

    //    public void DeleteTestGroupCase(TestGroupCase tgc)
    //    {
    //        if (tgc == null)
    //            return;
    //        if (tgc.TestGroupCaseParent != null)
    //            tgc.TestGroupCaseParent.TestGroupCaseOc.Remove(tgc);
    //        else
    //            TestGroupCaseOc.Remove(tgc);

    //        TestGroupCaseOcDisp.Remove(tgc);

    //        Stack<TestGroupCase> tcStack = new Stack<TestGroupCase>();

    //        foreach (TestGroupCase tgci in tgc.TestGroupCaseOc)
    //        {
    //            tcStack.Push(tgci);
    //        }

    //        for (; ; )
    //        {
    //            if (tcStack.Count < 1)
    //                return;

    //            TestGroupCase tgci = tcStack.Pop();
    //            TestGroupCaseOcDisp.Remove(tgci);
    //            if (tgci.IsCase == false)
    //            {
    //                foreach (TestGroupCase tgct in tgci.TestGroupCaseOc)
    //                {
    //                    tcStack.Push(tgct);
    //                }
    //            }
    //        }
    //    }

    //    public void UpdateParentChildSelectedState(TestGroupCase tgc)
    //    {
    //        UpdateChildSelectedState(tgc);
    //        UpdateParentSelectedState(tgc);
    //    }

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    public void UpdateChildSelectedState(TestGroupCase tgc)
    //    {
    //        if (tgc.IsCase == true)
    //            return;

    //        Stack<TestGroupCase> tcStack = new Stack<TestGroupCase>();

    //        foreach (TestGroupCase tgci in tgc.TestGroupCaseOc)
    //        {
    //            tcStack.Push(tgci);
    //        }

    //        for (; ; )
    //        {
    //            if (tcStack.Count < 1)
    //                return;

    //            TestGroupCase tgci = tcStack.Pop();
    //            tgci.SelectedState = tgc.SelectedState;
    //            tgci.SelectedStateBackColor = new SolidColorBrush(Colors.Transparent);
    //            if (tgci.IsCase == false)
    //            {
    //                foreach (TestGroupCase tgct in tgci.TestGroupCaseOc)
    //                {
    //                    tcStack.Push(tgct);
    //                }
    //            }
    //        }
    //    }

    //    public void UpdateParentSelectedState(TestGroupCase tgc)
    //    {
    //        bool hasChecked = false;
    //        bool hasUnchecked = false;

    //        TestGroupCase tgcp = tgc.TestGroupCaseParent;
    //        while (tgcp != null)
    //        {
    //            foreach (TestGroupCase tgci in tgcp.TestGroupCaseOc)
    //            {
    //                if (tgci.SelectedState == true)
    //                    hasChecked = true;
    //                else if (tgci.SelectedState == false)
    //                    hasUnchecked = true;
    //            }

    //            if (hasChecked && hasUnchecked)
    //                tgcp.SelectedStateBackColor = new SolidColorBrush(Colors.DarkGray);
    //            else
    //                tgcp.SelectedStateBackColor = new SolidColorBrush(Colors.Transparent);

    //            if (hasChecked)
    //                tgcp.SelectedState = true;
    //            else
    //                tgcp.SelectedState = false;

    //            tgcp = tgcp.TestGroupCaseParent;
    //        }
    //    }

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    /// <param name="tgc"></param>
    //    /// <returns></returns>
    //    public int GetGroupCaseIndex(TestGroupCase tgc)
    //    {
    //        for (int i = 0; i < TestGroupCaseOcDisp.Count; i++)
    //        {
    //            if (TestGroupCaseOcDisp[i] == tgc)
    //                return i;
    //        }

    //        return -1;
    //    }

    //    public static long _globalIndex = 0;

    //    public static long GetGlobalIndex()
    //    {
    //        return _globalIndex++;
    //    }

    //    public int FindLastSubGroupCaseDisplayIndex(TestGroupCase tgc)
    //    {
    //        if (tgc == null)
    //            return -1;

    //        if (tgc.IsCase == true)
    //            return FindTestGroupCaseDisplayIndex(tgc);

    //        if(tgc.TestGroupCaseOc.Count < 1)
    //            return FindTestGroupCaseDisplayIndex(tgc);

    //        TestGroupCase tgcp = tgc.TestGroupCaseOc[tgc.TestGroupCaseOc.Count - 1];
    //        while (tgcp.IsCase == false && tgcp.TestGroupCaseOc.Count >0)
    //        {
    //            tgcp = tgcp.TestGroupCaseOc[tgcp.TestGroupCaseOc.Count - 1];
    //        }
    //        return FindTestGroupCaseDisplayIndex(tgcp);
    //    }

    //    public int FindTestGroupCaseDisplayIndex(TestGroupCase tgc)
    //    {
    //        if (tgc == null)
    //            return -1;

    //        for (int i = 0; i < TestGroupCaseOcDisp.Count; i++)
    //        {
    //            if (TestGroupCaseOcDisp[i] == tgc)
    //                return i;
    //        }

    //        return -1;
    //    }

    //    public int FindTestGroupCaseIndex(TestGroupCase tgc)
    //    {
    //        if (tgc == null)
    //            return -1;

    //        if (tgc.TestGroupCaseParent == null)
    //        {
    //            for (int i = 0; i < TestGroupCaseOc.Count; i++)
    //            {
    //                if (TestGroupCaseOc[i] == tgc)
    //                    return i;
    //            }
    //        }
    //        else
    //        {
    //            for (int i = 0; i < tgc.TestGroupCaseParent.TestGroupCaseOc.Count; i++)
    //            {
    //                if (tgc.TestGroupCaseParent.TestGroupCaseOc[i] == tgc)
    //                    return i;
    //            }
    //        }

    //        return -1;
    //    }

    //    public void ConfigTestGroupCaseFromXML(TestGroupCase tgc, XElement xeCofig)
    //    {
    //        TestBase tb = tgc.TestBaseInstance;
    //        Type t = tb.GetType();
    //        PropertyInfo[] pia = t.GetProperties();
    //        //FieldInfo[] fia = t.GetFields();
    //        foreach (XElement xe in xeCofig.Elements("item"))
    //        {
    //            string sn = CommonOperations.ConvertXmlString2FileString(xe.Attribute("name").Value);
    //            string sv = CommonOperations.ConvertXmlString2FileString(xe.Attribute("value").Value);
    //            foreach (PropertyInfo pi in pia)
    //            {
    //                string pt = pi.PropertyType.Name;
    //                string ptb = pi.PropertyType.BaseType.Name;
    //                if (string.Compare(ptb, "Enum") != 0
    //                    && !pi.PropertyType.IsValueType 
    //                    && string.Compare(pt, "string") != 0)
    //                    break;

    //                if (string.Compare(pi.Name, sn, true) == 0)
    //                {
    //                    try
    //                    {
    //                        if (string.Compare(pt, "SByte") == 0)
    //                        {
    //                            pi.SetValue(tb, sbyte.Parse(sv), null);
    //                        }
    //                        else if (string.Compare(pt, "Byte") == 0)
    //                        {
    //                            pi.SetValue(tb, byte.Parse(sv), null);
    //                        }
    //                        else if (string.Compare(pt, "Char") == 0)
    //                        {
    //                            pi.SetValue(tb, char.Parse(sv), null);
    //                        }
    //                        else if (string.Compare(pt, "Int16") == 0)
    //                        {
    //                            pi.SetValue(tb, Int16.Parse(sv), null);
    //                        }
    //                        else if (string.Compare(pt, "UInt16") == 0)
    //                        {
    //                            pi.SetValue(tb, UInt16.Parse(sv), null);
    //                        }
    //                        else if (string.Compare(pt, "Int32") == 0)
    //                        {
    //                            pi.SetValue(tb, Int32.Parse(sv), null);
    //                        }
    //                        else if (string.Compare(pt, "UInt32") == 0)
    //                        {
    //                            pi.SetValue(tb, UInt32.Parse(sv), null);
    //                        }
    //                        else if (string.Compare(pt, "Int64") == 0)
    //                        {
    //                            pi.SetValue(tb, Int64.Parse(sv), null);
    //                        }
    //                        else if (string.Compare(pt, "UInt64") == 0)
    //                        {
    //                            pi.SetValue(tb, UInt64.Parse(sv), null);
    //                        }
    //                        else if (string.Compare(pt, "Decimal") == 0)
    //                        {
    //                            pi.SetValue(tb, decimal.Parse(sv), null);
    //                        }
    //                        else if (string.Compare(pt, "float") == 0)
    //                        {
    //                            pi.SetValue(tb, float.Parse(sv), null);
    //                        }
    //                        else if (string.Compare(pt, "double") == 0)
    //                        {
    //                            pi.SetValue(tb, double.Parse(sv), null);
    //                        }
    //                        else if (string.Compare(pt, "Boolean") == 0)
    //                        {
    //                            pi.SetValue(tb, bool.Parse(sv), null);
    //                        }
    //                        else if (string.Compare(ptb, "Enum") == 0)
    //                        {
    //                            pi.SetValue(tb, Enum.Parse(pi.PropertyType, sv), null);
    //                        }
    //                        else if (string.Compare(pt, "String") == 0)
    //                        {
    //                            pi.SetValue(tb, sv, null);
    //                        }
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        MessageBox.Show("Cannot parse \"" + sv + "\" for \" " + sn + "\".\nError message :\n" + ex.Message, "Parse Error", MessageBoxButton.OK, MessageBoxImage.Error);
    //                    }
    //                }
    //            }
    //            //foreach (FieldInfo fi in fia)
    //            //{
    //            //    string pt = fi.FieldType.ToString();
    //            //    if (!fi.FieldType.IsValueType && string.Compare(pt, "string", true) != 0)
    //            //        break;

    //            //    if (string.Compare(fi.Name, sn, true) == 0)
    //            //    {
    //            //    }
    //            //}
    //        }
    //    }
    //}

    //public class TestGroupCase : INotifyPropertyChangedClass
    //{
    //    #region Properties

    //    #region Test Related

    //    private bool _alreadyRun = false;
    //    public bool AlreadyRun
    //    {
    //        get
    //        {
    //            return _alreadyRun;
    //        }
    //        set
    //        {
    //            _alreadyRun = value;
    //        }
    //    }

    //    private TestGroupCase _testGroupCaseParent = null;
    //    public TestGroupCase TestGroupCaseParent
    //    {
    //        get
    //        {
    //            return _testGroupCaseParent;
    //        }
    //        set
    //        {
    //            _testGroupCaseParent = value;
    //        }
    //    }

    //    private TestBase _testBaseInstace = null;
    //    public TestBase TestBaseInstance
    //    {
    //        get
    //        {
    //            return _testBaseInstace;
    //        }
    //        set
    //        {
    //            _testBaseInstace = value;
    //        }
    //    }

    //    private TestCollection _testColInstance = null;
    //    public TestCollection TestColInstance
    //    {
    //        get
    //        {
    //            return _testColInstance;
    //        }
    //        set
    //        {
    //            _testColInstance = value;
    //        }
    //    }

    //    private XElement _testGroupCaseXE = null;
    //    public XElement TestGroupCaseXE
    //    {
    //        get
    //        {
    //            return _testGroupCaseXE;
    //        }
    //        set
    //        {
    //            _testGroupCaseXE = value;
    //        }
    //    }

    //    #region Case Necessary

    //    private string _groupCaseName = "";
    //    public string GroupCaseName
    //    {
    //        get
    //        {
    //            return _groupCaseName;
    //        }
    //        set
    //        {
    //            if (string.IsNullOrWhiteSpace(value))
    //                _groupCaseName = "No Name";
    //            else
    //                _groupCaseName = value;
    //            NotifyPropertyChanged("GroupCaseName");
    //        }
    //    }

    //    private SolidColorBrush _passBackground = new SolidColorBrush(Colors.Transparent);
    //    public SolidColorBrush PassBackground
    //    {
    //        get
    //        {
    //            return _passBackground;
    //        }
    //        set
    //        {
    //            _passBackground = value;
    //            NotifyPropertyChanged("PassBackground");
    //        }
    //    }

    //    private BitmapImage _passImage = null;
    //    public BitmapImage PassImage
    //    {
    //        get
    //        {
    //            return _passImage;
    //        }
    //        set
    //        {
    //            _passImage = value;
    //            NotifyPropertyChanged("PassImage");
    //        }
    //    }

    //    private int _passCount = 0;
    //    public int PassCount
    //    {
    //        get
    //        {
    //            return _passCount;
    //        }
    //        set
    //        {
    //            if (value < 0)
    //                _passCount = 0;
    //            else
    //                _passCount = value;
    //            int sum = _passCount + _failCount + _errorCount;
    //            if (sum != 0.0)
    //                PassRate = 100.0 * _passCount / sum;
    //            else
    //                PassRate = 0.0;
    //            NotifyPropertyChanged("PassCount");
    //            NotifyPropertyChanged("PassCountString");
    //            if (IsCase == false)
    //            {
    //                if (_passCount > 0)
    //                    _passBackground = new SolidColorBrush(Colors.Green);
    //                else
    //                    _passBackground = new SolidColorBrush(Colors.Transparent);
    //                NotifyPropertyChanged("PassBackground");
    //            }
    //        }
    //    }

    //    public string PassCountString
    //    {
    //        get
    //        {
    //            if (IsCase)
    //                return "";
    //            else
    //                return _passCount.ToString();
    //        }
    //    }

    //    private SolidColorBrush _failBackground = new SolidColorBrush(Colors.Transparent);
    //    public SolidColorBrush FailBackground
    //    {
    //        get
    //        {
    //            return _failBackground;
    //        }
    //        set
    //        {
    //            _failBackground = value;
    //            NotifyPropertyChanged("FailBackground");
    //        }
    //    }

    //    private BitmapImage _failImage = null;
    //    public BitmapImage FailImage
    //    {
    //        get
    //        {
    //            //if (IsCase == true && FailCount > 0)
    //                return _failImage;
    //            //else
    //            //    return null;
    //        }
    //        set
    //        {
    //            _failImage = value;
    //            NotifyPropertyChanged("FailImage");
    //        }
    //    }

    //    private int _failCount = 0;
    //    public int FailCount
    //    {
    //        get
    //        {
    //            return _failCount;
    //        }
    //        set
    //        {
    //            if (value < 0)
    //                _failCount = 0;
    //            else
    //                _failCount = value;
    //            int sum = _passCount + _failCount + _errorCount;
    //            if (sum != 0.0)
    //                PassRate = 100.0 * _passCount / sum;
    //            else
    //                PassRate = 0.0;
    //            NotifyPropertyChanged("FailCount");
    //            NotifyPropertyChanged("FailCountString");
    //            //NotifyPropertyChanged("PassImage");
    //            if (IsCase == false)
    //            {
    //                if (_failCount > 0)
    //                    _failBackground = new SolidColorBrush(Colors.Red);
    //                else
    //                    _failBackground = new SolidColorBrush(Colors.Transparent);
    //                NotifyPropertyChanged("FailBackground");
    //            }
    //        }
    //    }

    //    public string FailCountString
    //    {
    //        get
    //        {
    //            if (IsCase)
    //                return "";
    //            else
    //                return _failCount.ToString();
    //        }
    //    }

    //    private SolidColorBrush _errorBackground = new SolidColorBrush(Colors.Transparent);
    //    public SolidColorBrush ErrorBackground
    //    {
    //        get
    //        {
    //            return _errorBackground;
    //        }
    //        set
    //        {
    //            _errorBackground = value;
    //            NotifyPropertyChanged("ErrorBackground");
    //        }
    //    }

    //    private BitmapImage _errorImage = null;
    //    public BitmapImage ErrorImage
    //    {
    //        get
    //        {
    //            //if (IsCase == true && ErrorCount > 0)
    //            return _errorImage;
    //            //else
    //            //    return null;
    //        }
    //        set
    //        {
    //            _errorImage = value;
    //            NotifyPropertyChanged("ErrorImage");
    //        }
    //    }

    //    private int _errorCount = 0;
    //    public int ErrorCount
    //    {
    //        get
    //        {
    //            return _errorCount;
    //        }
    //        set
    //        {
    //            if (value < 0)
    //                _errorCount = 0;
    //            else
    //                _errorCount = value;
    //            int sum = _passCount + _failCount + _errorCount;
    //            if (sum != 0.0)
    //                PassRate = 100.0 * _passCount / sum;
    //            else
    //                PassRate = 0.0;
    //            NotifyPropertyChanged("ErrorCount");
    //            NotifyPropertyChanged("ErrorCountString");
    //            if (IsCase == false)
    //            {
    //                if (_errorCount > 0)
    //                    _errorBackground = new SolidColorBrush(Colors.Yellow);
    //                else
    //                    _errorBackground = new SolidColorBrush(Colors.Transparent);
    //                NotifyPropertyChanged("ErrorBackground");
    //            }
    //        }
    //    }

    //    public string ErrorCountString
    //    {
    //        get
    //        {
    //            if (IsCase)
    //                return "";
    //            else
    //                return _errorCount.ToString();
    //        }
    //    }

    //    private int _totalTests = 0;
    //    public int TotalTests
    //    {
    //        get
    //        {
    //            return _totalTests;
    //        }
    //        set
    //        {
    //            if (value < 0)
    //                _totalTests = 0;
    //            else
    //                _totalTests = value;
    //            NotifyPropertyChanged("TotalTests");
    //            NotifyPropertyChanged("TotalTestsString");
    //        }
    //    }

    //    public string TotalTestsString
    //    {
    //        get
    //        {
    //            if (IsCase)
    //                return "";
    //            else
    //                return _totalTests.ToString();
    //        }
    //    }

    //    private double _passRate = 0.0;
    //    public double PassRate
    //    {
    //        get
    //        {
    //            if (IsCase)
    //                return 0.0;
    //            else
    //                return _passRate;
    //        }
    //        set
    //        {
    //            if (value < 0.0)
    //                _passRate = 0.0;
    //            else if (value > 100.0)
    //                _passRate = 100.0;
    //            else
    //                _passRate = value;
    //            NotifyPropertyChanged("PassRate");
    //            NotifyPropertyChanged("PassRateString");
    //        }
    //    }

    //    public string PassRateString
    //    {
    //        get
    //        {
    //            if (IsCase)
    //                return "";
    //            else
    //                return _passRate.ToString("F2") + "%";
    //        }
    //    }

    //    private TimeSpan _duration = new TimeSpan(0, 0, 0, 0, 0);
    //    public TimeSpan Duration
    //    {
    //        get
    //        {
    //            return _duration;
    //        }
    //        set
    //        {
    //            _duration = value;
    //            NotifyPropertyChanged("Duration");
    //            NotifyPropertyChanged("DurationString");
    //        }
    //    }

    //    public string DurationString
    //    {
    //        get
    //        {
    //            int intMs = (int)(_duration.Milliseconds / 100.0);
    //            return _duration.Days.ToString() + "." + _duration.Hours.ToString("D2") + ":"
    //                + _duration.Minutes.ToString("D2") + ":" + _duration.Seconds.ToString("D2") + "."
    //                + intMs.ToString("D1");
    //        }
    //    }

    //    private int _runCount = 1;
    //    public int RunCount
    //    {
    //        get
    //        {
    //            return _runCount;
    //        }
    //        set
    //        {
    //            if (value < 1)
    //                _runCount = 1;
    //            else
    //                _runCount = value;
    //            NotifyPropertyChanged("RunCount");
    //            NotifyPropertyChanged("RunCountString");
    //        }
    //    }

    //    public string RunCountString
    //    {
    //        get
    //        {
    //            if (IsCase)
    //                return _runCount.ToString();
    //            else
    //                return "";
    //        }
    //    }

    //    private string _testClass = "";
    //    public string TestClass
    //    {
    //        get
    //        {
    //            if (IsCase)
    //                return _testClass;
    //            else
    //                return "";
    //        }
    //        set
    //        {
    //            _testClass = value;
    //            NotifyPropertyChanged("TestClass");
    //        }
    //    }

    //    private string _testFilePath = "";
    //    public string TestFilePath
    //    {
    //        get
    //        {
    //            return _testFilePath;
    //        }
    //        set
    //        {
    //            _testFilePath = value;
    //            NotifyPropertyChanged("TestFilePath");
    //        }
    //    }

    //    public enum TestPriorityType
    //    {
    //        High,
    //        Medium,
    //        Low
    //    }

    //    private TestPriorityType _testPriority = TestPriorityType.High;
    //    public TestPriorityType TestPriority
    //    {
    //        get
    //        {
    //            return _testPriority;
    //        }
    //        set
    //        {
    //            _testPriority = value;
    //            NotifyPropertyChanged("TestPriority");
    //            NotifyPropertyChanged("TestPriorityString");
    //        }
    //    }

    //    public string TestPriorityString
    //    {
    //        get
    //        {
    //            return _testPriority.ToString();
    //        }
    //    }

    //    #endregion

    //    #region Group Necessary

    //    private ObservableCollection<TestGroupCase> _testGroupCaseOc = new ObservableCollection<TestGroupCase>();
    //    public ObservableCollection<TestGroupCase> TestGroupCaseOc
    //    {
    //        get
    //        {
    //            return _testGroupCaseOc;
    //        }
    //        set
    //        {
    //            _testGroupCaseOc = value;
    //        }
    //    }

    //    //public int GroupCount
    //    //{
    //    //    get
    //    //    {
    //    //        int count = 0;
    //    //        foreach (TestGroupCase tgc in TestGroupCaseOc)
    //    //        {
    //    //            if (tgc.IsCase == false)
    //    //                count++;
    //    //        }
    //    //        return count;
    //    //    }
    //    //}

    //    //public int AllCount
    //    //{
    //    //    get
    //    //    {
    //    //        int count = 0;
    //    //        foreach (TestGroupCase tgc in TestGroupCaseOc)
    //    //        {
    //    //            if (tgc.IsCase == false)
    //    //                count = count + tgc.AllCount;
    //    //        }
    //    //        return count + DirectCount;
    //    //    }
    //    //}

    //    //public int DirectCount
    //    //{
    //    //    get
    //    //    {
    //    //        int count = 0;
    //    //        foreach (TestGroupCase tgc in TestGroupCaseOc)
    //    //        {
    //    //            if (tgc.IsCase == true)
    //    //                count++;
    //    //        }
    //    //        return count;
    //    //    }
    //    //}

    //    #endregion

    //    #endregion

    //    #region Display Related

    //    private bool _testGroupCaseIsVisible = true;
    //    public bool TestGroupCaseIsVisible
    //    {
    //        get
    //        {
    //            return _testGroupCaseIsVisible;
    //        }
    //        set
    //        {
    //            _testGroupCaseIsVisible = value;
    //            NotifyPropertyChanged("TestGroupCaseIsVisible");
    //        }
    //    }

    //    private bool _isCase = true;
    //    public bool IsCase
    //    {
    //        get
    //        {
    //            return _isCase;
    //        }
    //        set
    //        {
    //            _isCase = value;
    //            NotifyPropertyChanged("IsCase");
    //        }
    //    }

    //    private bool _notRun = true;
    //    public bool NotRun
    //    {
    //        get
    //        {
    //            return _notRun;
    //        }
    //        set
    //        {
    //            _notRun = value;
    //            NotifyPropertyChanged("NotRun");
    //            NotifyPropertyChanged("PassCountString");
    //            NotifyPropertyChanged("FailCountString");
    //            NotifyPropertyChanged("ErrorCountString");
    //            NotifyPropertyChanged("TotalTestsString");
    //            NotifyPropertyChanged("PassRateString");
    //        }
    //    }

    //    private int _displayLevel = 0;
    //    public int DisplayLevel
    //    {
    //        get
    //        {
    //            return _displayLevel;
    //        }
    //        set
    //        {
    //            if (value < 0)
    //                _displayLevel = 0;
    //            else
    //                _displayLevel = value;
    //            NotifyPropertyChanged("DisplayLevel");
    //            NotifyPropertyChanged("DisplayLevelLength");
    //        }
    //    }

    //    public int DisplayLevelLength
    //    {
    //        get
    //        {
    //            return TesterMain.LISTVIEW_IMAGE_SIZE * (DisplayLevel + 1);
    //        }
    //    }

    //    private bool _selState = true;
    //    public bool SelectedState
    //    {
    //        get
    //        {
    //            return _selState;
    //        }
    //        set
    //        {
    //            _selState = value;
    //            NotifyPropertyChanged("SelectedState");
    //            //UpdateChildSelectedState();
    //        }
    //    }

    //    private SolidColorBrush _selectedStateBackColor = new SolidColorBrush(Colors.Transparent);
    //    public SolidColorBrush SelectedStateBackColor
    //    {
    //        get
    //        {
    //            return _selectedStateBackColor;
    //        }
    //        set
    //        {
    //            _selectedStateBackColor = value;
    //            NotifyPropertyChanged("SelectedStateBackColor");
    //        }
    //    }

    //    private bool _isExpanded = true;
    //    public bool IsExpanded
    //    {
    //        get
    //        {
    //            return _isExpanded;
    //        }
    //        set
    //        {
    //            _isExpanded = value;
    //            NotifyPropertyChanged("IsExpanded");
    //            GroupCaseImage = new BitmapImage();
    //            GroupCaseImage.BeginInit();
    //            if (IsExpanded == true)
    //                GroupCaseImage.UriSource = new Uri("pack://application:,,,/itester;component/resources/group_expand.png");
    //            else
    //                GroupCaseImage.UriSource = new Uri("pack://application:,,,/itester;component/resources/group_collaps.png");
    //            GroupCaseImage.EndInit();
    //            NotifyPropertyChanged("GroupCaseImage");
    //            UpdateAllChildrenVisible(IsExpanded);
    //        }
    //    }

    //    private BitmapImage _groupCaseImage = null;
    //    public BitmapImage GroupCaseImage
    //    {
    //        get
    //        {
    //            return _groupCaseImage;
    //        }
    //        set
    //        {
    //            _groupCaseImage = value;
    //            NotifyPropertyChanged("GroupCaseImage");
    //        }
    //    }

    //    private long _globalIndex = 0;
    //    public long GlobalIndex
    //    {
    //        get
    //        {
    //            return _globalIndex;
    //        }
    //        set
    //        {
    //            _globalIndex = value;
    //            NotifyPropertyChanged("GlobalIndex");
    //            NotifyPropertyChanged("GlobalIndexString");
    //        }
    //    }

    //    public string GlobalIndexString
    //    {
    //        get
    //        {
    //            return _globalIndex.ToString();
    //        }
    //    }

    //    private bool _isGlobalIndexVisible = false;
    //    public bool IsGlobalIndexVisible
    //    {
    //        get
    //        {
    //            return _isGlobalIndexVisible;
    //        }
    //        set
    //        {
    //            _isGlobalIndexVisible = false;
    //            NotifyPropertyChanged("IsGlobalIndexVisible");
    //        }
    //    }

    //    #endregion

    //    #endregion

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    /// <param name="testColInst"></param>
    //    /// <param name="groupCaseName"></param>
    //    /// <param name="level"></param>
    //    /// <param name="isCase"></param>
    //    /// <param name="testBaseInst"></param>
    //    /// <param name="testFilePath"></param>
    //    public TestGroupCase(TestCollection testColInst, TestGroupCase parent, string groupCaseName, bool isCase, TestBase testBaseInst, string testFilePath)
    //    {
    //        if (testColInst == null)
    //            throw new ArgumentNullException("Cannot initialize TestGroupCase() because of the null parameter.");
    //        GlobalIndex = TestCollection.GetGlobalIndex();
    //        TestColInstance = testColInst;
    //        GroupCaseName = groupCaseName;
    //        int level = 0;
    //        TestGroupCase tgc = parent;
    //        while (tgc != null)
    //        {
    //            level++;
    //            tgc = tgc.TestGroupCaseParent;
    //        }
    //        DisplayLevel = level;
    //        IsCase = isCase;
    //        TestBaseInstance = testBaseInst;
    //        if (IsCase == true) 
    //            TestBaseInstance.TestGroupCaseLocal = this;
    //        TestFilePath = testFilePath;

    //        GroupCaseImage = new BitmapImage();
    //        GroupCaseImage.BeginInit();
    //        if (IsCase == true)
    //            GroupCaseImage.UriSource = new Uri("pack://application:,,,/itester;component/resources/group_case.png");
    //        else
    //        {
    //            if (TestGroupCaseOc.Count > 1)
    //            {
    //                GroupCaseImage.UriSource = new Uri("pack://application:,,,/itester;component/resources/group_expand.png");
    //                _isExpanded = true;
    //            }
    //            else
    //            {
    //                GroupCaseImage.UriSource = new Uri("pack://application:,,,/itester;component/resources/group_collaps.png");
    //                _isExpanded = false;
    //            }
    //        }
    //        GroupCaseImage.EndInit();

    //        //if (IsCase == true)
    //        //{
    //        //    PassImage = new BitmapImage();
    //        //    PassImage.BeginInit();
    //        //    PassImage.UriSource = new Uri("pack://application:,,,/itester;component/resources/status_ok.png");
    //        //    PassImage.EndInit();

    //        //    FailImage = new BitmapImage();
    //        //    FailImage.BeginInit();
    //        //    FailImage.UriSource = new Uri("pack://application:,,,/itester;component/resources/status_error.png");
    //        //    FailImage.EndInit();
    //        //}

    //        if (parent != null)
    //        {
    //            TestGroupCaseParent = parent;
    //            int index = TestColInstance.FindLastSubGroupCaseDisplayIndex(parent);
    //            parent.TestGroupCaseOc.Add(this);
    //            TestGroupCaseParent.IsExpanded = true;
    //            TestColInstance.TestGroupCaseOcDisp.Insert(index + 1, this);
    //        }
    //        else
    //        {
    //            TestColInstance.TestGroupCaseOc.Add(this);
    //            TestColInstance.TestGroupCaseOcDisp.Add(this);
    //        }

    //        if(IsCase == true)
    //            TestBaseInstance.RunCountChangedEvent += new EventHandler<RunCountChangedEventArgs>(TestBaseInstance_RunCountChangedEvent_EventHandler);
    //    }

    //    void TestBaseInstance_RunCountChangedEvent_EventHandler(object sender, RunCountChangedEventArgs e)
    //    {
    //        RunCount = e.RunCount;
    //    }

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    /// <param name="isVis"></param>
    //    private void UpdateAllChildrenVisible(bool isVis)
    //    {
    //        Stack<TestGroupCase> tcStack = new Stack<TestGroupCase>();

    //        foreach (TestGroupCase tgc in TestGroupCaseOc)
    //        {
    //            tcStack.Push(tgc);
    //        }

    //        for (; ; )
    //        {
    //            if (tcStack.Count < 1)
    //                return;

    //            TestGroupCase tgc = tcStack.Pop();
    //            tgc.TestGroupCaseIsVisible = isVis;
    //            if (tgc.IsCase == false && tgc.IsExpanded == true)
    //            {
    //                foreach (TestGroupCase tgci in tgc.TestGroupCaseOc)
    //                {
    //                    tcStack.Push(tgci);
    //                }
    //            }
    //        }
    //    }
    //}

    public class TestResult : INotifyPropertyChangedClass
    {
        private iTestBase.iTestBase.TestStateEnum _testState = iTestBase.iTestBase.TestStateEnum.None;
        public iTestBase.iTestBase.TestStateEnum TestState
        {
            get
            {
                return _testState;
            }
            set
            {
                _testState = value;
                switch (_testState)
                {
                    default:
                    case iTestBase.iTestBase.TestStateEnum.None:
                        _testStateImage = null;
                        break;
                    case iTestBase.iTestBase.TestStateEnum.Infromation:
                        _testStateImage = new BitmapImage();
                        _testStateImage.BeginInit();
                        _testStateImage.UriSource = new Uri("pack://application:,,,/itester;component/resources/status_info.png");
                        _testStateImage.EndInit();
                        break;
                    case iTestBase.iTestBase.TestStateEnum.Pass:
                        _testStateImage = new BitmapImage();
                        _testStateImage.BeginInit();
                        _testStateImage.UriSource = new Uri("pack://application:,,,/itester;component/resources/status_ok.png");
                        _testStateImage.EndInit();
                        break;
                    case iTestBase.iTestBase.TestStateEnum.Fail:
                        _testStateImage = new BitmapImage();
                        _testStateImage.BeginInit();
                        _testStateImage.UriSource = new Uri("pack://application:,,,/itester;component/resources/status_error.png");
                        _testStateImage.EndInit();
                        break;
                    case iTestBase.iTestBase.TestStateEnum.Error:
                        _testStateImage = new BitmapImage();
                        _testStateImage.BeginInit();
                        _testStateImage.UriSource = new Uri("pack://application:,,,/itester;component/resources/status_ques.ico");
                        _testStateImage.EndInit();
                        break;
                }
                NotifyPropertyChanged("TestState");
                NotifyPropertyChanged("TestStateImage");
            }
        }
		
		public int DBStateBackgroundIndex
		{
			get
			{
				return (int)TestState;
			}
		}

        private BitmapImage _testStateImage = null;
        public BitmapImage TestStateImage
        {
            get
            {
                return _testStateImage;
            }
            set
            {
                _testStateImage = value;
                NotifyPropertyChanged("TestState");
                NotifyPropertyChanged("TestStateImage");
            }
        }

        private string _testName = "";
        public string TestName
        {
            get
            {
                return _testName;
            }
            set
            {
                _testName = value;
                NotifyPropertyChanged("TestName");
            }
        }

        private DateTime _testTime = DateTime.Now;
        public DateTime TestTime
        {
            get
            {
                return _testTime;
            }
            set
            {
                _testTime = value;
                NotifyPropertyChanged("TestTime");
                NotifyPropertyChanged("TestTimeString");
            }
        }

        public string TestTimeString
        {
            get
            {
                return _testTime.Hour.ToString() + ":" + _testTime.Minute.ToString() + ":" + _testTime.Second.ToString() + "." + _testTime.Millisecond.ToString();
            }
        }

		private string _category = "";
		public string Category
		{
			get
			{
				return _category;
			}
			set
			{
				_category = value;
				NotifyPropertyChanged("Category");
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

        private string _dateValue = "";
        public string DataValue
        {
            get
            {
                return _dateValue;
            }
            set
            {
                _dateValue = value;
                NotifyPropertyChanged("DataValue");
            }
        }

        private string _constraint = "";
        public string Constraint
        {
            get
            {
                return _constraint;
            }
            set
            {
                _constraint = value;
                NotifyPropertyChanged("Constraint");
            }
        }

        //private string _comment = "";
        //public string Comment
        //{
        //    get
        //    {
        //        return _comment;
        //    }
        //    set
        //    {
        //        _comment = value;
        //        NotifyPropertyChanged("Comment");
        //    }
        //}
    }

	public class TestConfiguration : INotifyPropertyChangedClass
	{
		private int _index = 99999;
		public int Index
		{
			get
			{
				return _index;
			}
			set
			{
				_index = value;
			}
		}

		private string _cfgKey = "";
		public string ConfigKey
		{
			get
			{
				return _cfgKey;
			}
			set
			{
				_cfgKey = value;
                NotifyPropertyChanged("ConfigKey");
			}
		}

		private string _cfgValue = "";
		public string ConfigValue
		{
			get
			{
				return _cfgValue;
			}
			set
			{
				_cfgValue = value;
                NotifyPropertyChanged("ConfigValue");
                NotifyPropertyChanged("ConfigValueDisplay");
            }
		}

        public string ConfigValueDisplay
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_cfgValue))
                    return "";
                else
                {
                    if (_cfgKey == "Password")
                    {
                        int len = _cfgValue.Length;
                        string s = "";
                        for (int i = 0; i < len; i++)
                            s = s + "*";
                        return s;
                    }
                    else
                        return _cfgValue;
                }
            }
        }
    }

    #endregion

    //public class RunCountChangedEventArgs : EventArgs
    //{
    //    private int _runCount = 1;
    //    public int RunCount
    //    {
    //        get
    //        {
    //            return _runCount;
    //        }
    //        set
    //        {
    //            _runCount = value;
    //            if (_runCount < 1)
    //                _runCount = 1;
    //        }
    //    }
    //}

    //[DefaultProperty("RunCount")]
    //public abstract class TestBase
    //{
    //    public event EventHandler<RunCountChangedEventArgs> RunCountChangedEvent;

    //    public delegate void SendMessageDelegate(TestResult.TestStateEnum tse, string testName, string category, string dataKey, string dataValue, string constraint, bool logDb = true);

    //    //public EventHandler PauseStateChangedEventHandler;

    //    /// <summary>
    //    /// Do NOT call it directly, call DoSendMessage instead
    //    /// </summary>
    //    public SendMessageDelegate SendMessage;

    //    public TestBase()
    //    {
    //    }

    //    private bool _doPause = false;
    //    [Browsable(false)]
    //    public bool DoPause
    //    {
    //        get
    //        {
    //            return _doPause;
    //        }
    //        set
    //        {
    //            _doPause = value;
    //        }
    //    }

    //    private string _testName = "No Name";
    //    [ReadOnly(true), Browsable(true)]
    //    public string TestName
    //    {
    //        get
    //        {
    //            return _testName;
    //        }
    //        set
    //        {
    //            _testName = value;
    //        }
    //    }

    //    private string _contactEngineer = "";
    //    public string ContactEngineer
    //    {
    //        get
    //        {
    //            return _contactEngineer;
    //        }
    //        set
    //        {
    //            _contactEngineer = value;
    //        }
    //    }

    //    private int _runCount = 1;
    //    public int RunCount
    //    {
    //        get
    //        {
    //            return _runCount;
    //        }
    //        set
    //        {
    //            if (value < 1)
    //                _runCount = 1;
    //            else
    //                _runCount = value;
    //            RunCountChangedEvent(this, new RunCountChangedEventArgs() { RunCount = _runCount });
    //        }
    //    }

    //    private TestGroupCase _tgcLocal = null;
    //    [Browsable(false)]
    //    public TestGroupCase TestGroupCaseLocal
    //    {
    //        get
    //        {
    //            return _tgcLocal;
    //        }
    //        set
    //        {
    //            _tgcLocal = value;
    //        }
    //    }

    //    public virtual void Setup()
    //    {
    //        throw new NotImplementedException("TestBase : Setup()");
    //    }

    //    public virtual void Clean()
    //    {
    //        throw new NotImplementedException("TestBase : Clean()");
    //    }

    //    public virtual void Run()
    //    {
    //         throw new NotImplementedException("TestBase : Run()");
    //    }

    //    protected void DoSendMessageOnly(TestResult.TestStateEnum tse = TestResult.TestStateEnum.None, string message = "")
    //    {
    //        DoSendMessage(tse, "", message, "", "");
    //    }

    //    protected void DoSendMessageData(TestResult.TestStateEnum tse = TestResult.TestStateEnum.None, string message = "", string dataValue="", string constraint="")
    //    {
    //        DoSendMessage(tse, "", message, dataValue, constraint);;
    //    }

    //    protected void DoSendMessage(string category, string message = "", string dataValue = "")
    //    {
    //        DoSendMessage(TestResult.TestStateEnum.None, category, message, dataValue, "");
    //    }

    //    protected void DoSendMessage(TestResult.TestStateEnum tse, string category, string message, string dataValue = "", string constraint = "")
    //    {
    //        switch (tse)
    //        {
    //            default:
    //            case TestResult.TestStateEnum.None:
    //                break;
    //            case TestResult.TestStateEnum.Infromation:
    //                break;
    //            case TestResult.TestStateEnum.Pass:
    //                TestGroupCaseLocal.PassCount++;
    //                break;
    //            case TestResult.TestStateEnum.Fail:
    //                TestGroupCaseLocal.FailCount++;
    //                break;
    //            case TestResult.TestStateEnum.Error:
    //                TestGroupCaseLocal.ErrorCount++;
    //                break;
    //        }

    //        if (SendMessage != null)
    //            SendMessage(tse, TestName, category, message, dataValue, constraint);
    //    }
    //}
}
