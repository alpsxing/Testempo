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
using System.IO;

using MySql.Data;
using MySql.Data.MySqlClient;
using MySql.Data.Common;
//using MySql.Data.Entity;
using MySql.Data.Types;
using System.Data;

using System.Threading;

namespace iTesterViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class iTesterViewerMain : Window, INotifyPropertyChanged
    {
        private const int DISPLAY_TESTVIEW = 0;
        private const int DISPLAY_TESTCOL = 1;
        private const int DISPLAY_TESTRESULT = 2;
		
		//private bool _inPrjSubProuncUpdate = false;
		//private bool _inProjectUpdate = false;
		//private bool _inSubProjectUpdate = false;

		private int _currentTestViewIndex = -1;
		private int _currentTestColIndex = -1;
		private int _currentTestResultIndex = -1;
		private int _displayTestViewIndex = -1;
		private int _displayTestColIndex = -1;

        private bool _bInNormalClose = false;

        private int _currentDisplay = DISPLAY_TESTVIEW;
        public int CurrentDisplay
        {
            get
            {
                return _currentDisplay;
            }
            set
            {
                bool changed = (_currentDisplay == value) ? false : true;
                bool isInto = (_currentDisplay < value) ? true : false;
                _currentDisplay = value;
                if (changed)
                {
                    if (_currentDisplay > DISPLAY_TESTRESULT)
                        _currentDisplay = DISPLAY_TESTRESULT;
                    if (_currentDisplay < DISPLAY_TESTVIEW)
                        _currentDisplay = DISPLAY_TESTVIEW;
                    switch (_currentDisplay)
                    {
                        default:
                        case DISPLAY_TESTVIEW:
                            dgTestView.Visibility = System.Windows.Visibility.Visible;
                            dgTestCol.Visibility = System.Windows.Visibility.Collapsed;
                            dgTestResult.Visibility = System.Windows.Visibility.Collapsed;
                            break;
                        case DISPLAY_TESTCOL:
                            dgTestView.Visibility = System.Windows.Visibility.Collapsed;
                            dgTestCol.Visibility = System.Windows.Visibility.Visible;
                            dgTestResult.Visibility = System.Windows.Visibility.Collapsed;
							if (isInto && _displayTestViewIndex != _currentTestViewIndex)
							{
								UpdatetestCol();
								_displayTestViewIndex = _currentTestViewIndex;
							}
                            break;
                        case DISPLAY_TESTRESULT:
                            dgTestView.Visibility = System.Windows.Visibility.Collapsed;
                            dgTestCol.Visibility = System.Windows.Visibility.Collapsed;
                            dgTestResult.Visibility = System.Windows.Visibility.Visible;
							if (isInto && _displayTestColIndex != _currentTestColIndex)
							{
								UpdateTestResult();
								_displayTestColIndex = _currentTestColIndex;
							}
                            break;
                    }
                }
                NotifyPropertyChanged("CurrentDisplay");
                NotifyPropertyChanged("IsBrowseIntoOK");
                NotifyPropertyChanged("IsBrowseBackOK");
            }
        }

        public bool IsBrowseIntoOK
        {
            get
            {
                return (_currentDisplay < DISPLAY_TESTRESULT) ? true : false;
            }
        }

        public bool IsBrowseBackOK
        {
            get
            {
                return (_currentDisplay > DISPLAY_TESTVIEW) ? true : false;
            }
        }

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

        private ObservableCollection<TestViewItem> _testViewOc = new ObservableCollection<TestViewItem>();
        public ObservableCollection<TestViewItem> TestViewOc
        {
            get
            {
                return _testViewOc;
            }
        }

        private ObservableCollection<TestColItem> _testColOc = new ObservableCollection<TestColItem>();
        public ObservableCollection<TestColItem> TestColOc
        {
            get
            {
                return _testColOc;
            }
        }

        private ObservableCollection<TestResultItem> _testResultOc = new ObservableCollection<TestResultItem>();
        public ObservableCollection<TestResultItem> TestResultOc
        {
            get
            {
                return _testResultOc;
            }
        }

        private string _dbIpAddress = "";
        public string DBIPAddress
        {
            get
            {
                return _dbIpAddress.Trim();
            }
            set
            {
                _dbIpAddress = value;
                if (string.IsNullOrWhiteSpace(_dbIpAddress))
                    _dbIpAddress = "";
                NotifyPropertyChanged("DBIPAddress");
                NotifyPropertyChanged("DBIPAddressOK");
            }
        }

        public bool DBIPAddressOK
        {
            get
            {
                return !string.IsNullOrWhiteSpace(_dbIpAddress);
            }
        }

        private string _dbPassword = "";
        public string DBPassword
        {
            get
            {
                return _dbPassword.Trim();
            }
            set
            {
                if (_dbPassword != value)
                {
                    _dbPassword = value;
                    if (string.IsNullOrWhiteSpace(_dbPassword))
                        _dbPassword = "";
                    NotifyPropertyChanged("DBPassword");
                    NotifyPropertyChanged("DBPasswordOK");
                }
            }
        }

        public bool DBPasswordOK
        {
            get
            {
                return !string.IsNullOrWhiteSpace(_dbPassword);
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

        private bool _initOk = false;
        public bool InitOK
        {
            get
            {
                return _initOk;
            }
            set
            {
                _initOk = value;
                NotifyPropertyChanged("InitOK");
                NotifyPropertyChanged("IsBrowseIntoOK");
                NotifyPropertyChanged("IsBrowseBackOK");
            }
        }

        private bool? _useProjectSubProjectFilter = false;
        public bool? UseProjectSubProjectFilter
        {
            get
            {
                return _useProjectSubProjectFilter;
            }
            set
            {
                _useProjectSubProjectFilter = value;
                NotifyPropertyChanged("UseProjectSubProjectFilter");
                NotifyPropertyChanged("UseProjectSubProjectFilterBool");
            }
        }

        public bool? UseProjectSubProjectFilterBool
        {
            get
            {
                return (_useProjectSubProjectFilter == true) ? true : false;
            }
        }

        public iTesterViewerMain()
        {
            InitializeComponent();

            DataContext = this;
            dgTestView.DataContext = _testViewOc;
            dgTestCol.DataContext = _testColOc;
            dgTestResult.DataContext = _testResultOc;

            LoadDBConfig();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadDBConfig()
        {
            try
            {
                StreamReader sr = new StreamReader("dbconfig.txt");
                string strLine = null;
                int i = 0;
                while (true)
                {
                    strLine = sr.ReadLine();
                    if (string.IsNullOrWhiteSpace(strLine))
                        break;
                    if (i == 0)
                        DBIPAddress = strLine.Trim();
                    else if (i == 1)
                        pbDBPW.Password = strLine.Trim();
                    else
                        break;

                    i++;
                }
                sr.Close();
                sr.Dispose();
            }
            catch (Exception ex)
            {
                //AddDatabaseMessage(DatabaseMessage.DBState.Error, "Cannot load the database config file.");
                //AddDatabaseMessage(DatabaseMessage.DBState.Error, "Error message :");
                //AddDatabaseMessage(DatabaseMessage.DBState.Error, ex.Message);
            }
        }

        private void SaveDBConfig()
        {
            try
            {
                StreamWriter sw = new StreamWriter("dbconfig.txt");
                sw.WriteLine(DBIPAddress);
                sw.WriteLine(DBPassword);
                sw.Close();
                sw.Dispose();
            }
            catch (Exception ex)
            {
                //AddDatabaseMessage(DatabaseMessage.DBState.Error, "Cannot save the database config file.");
                //AddDatabaseMessage(DatabaseMessage.DBState.Error, "Error message :");
                //AddDatabaseMessage(DatabaseMessage.DBState.Error, ex.Message);
            }
        }

        private void DBPassword_PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            DBPassword = pbDBPW.Password.Trim();
        }

        private void SaveConfig_Button_Click(object sender, RoutedEventArgs e)
        {
            SaveDBConfig();
        }

        private void InitializeFromDatabase_Button_Click(object sender, RoutedEventArgs e)
        {
			InRun = true;
			spbTimer.IsIndeterminate = true;
            UseProjectSubProjectFilter = false;

			Thread th = new Thread(new ThreadStart(InitializeFromDatabaseThread));
			th.Start();
		}

		private void InitializeFromDatabaseThread()
		{
			try
			{
				DoInitializeFromDatabase();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Cannot do initialization from the database.\nError message:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				Dispatcher.Invoke((ThreadStart)delegate()
				{
					CurrentDisplay = DISPLAY_TESTVIEW;
					spbTimer.IsIndeterminate = false;
				}, null);

				InRun = false;
			}
		}

		private void DoInitializeFromDatabase()
		{
			//_inPrjSubProuncUpdate = true;

			Dispatcher.Invoke((ThreadStart)delegate()
			{
				CurrentDisplay = DISPLAY_TESTVIEW;
			}, null);

            InitializeFromDatabaseJob();

			//_inPrjSubProuncUpdate = false;
        }

        private void InitializeFromDatabaseJob(string condition = null)
        {
            if (condition == null)
                InitOK = false;
            //InDatabaseTalking = true;

			Dispatcher.Invoke((ThreadStart)delegate()
			{
				if (condition == null)
				{
					ProjectOc.Clear();
					SubProjectOc.Clear();
				}

				TestViewOc.Clear();
			}, null);

            try
            {
                MySqlConnection mysqlConn = new MySqlConnection();
                string s = "server=" + DBIPAddress + ";" + "uid=root;" + "pwd=" + DBPassword + ";database=itester;";
                mysqlConn.ConnectionString = s;
                mysqlConn.Open();

                #region

                string sql = "";
                MySqlDataAdapter da = null;
                DataSet ds = null;

                if (string.IsNullOrWhiteSpace(condition))
                    sql = "SELECT * FROM itester.testview";
                else
                    sql = "SELECT * FROM itester.testview WHERE " + condition;
                da = new MySqlDataAdapter(sql, mysqlConn);
                ds = new DataSet();
                da.Fill(ds, "testview");
                foreach (DataRow dr in ds.Tables["testview"].Rows)
                {
					Dispatcher.Invoke((ThreadStart)delegate()
					{
						sql = "SELECT * FROM itester.subproject WHERE pspname='" + (string)dr["pspname"] +"'";
						MySqlDataAdapter dai = new MySqlDataAdapter(sql, mysqlConn);
						DataSet dsi = new DataSet();
						dai.Fill(dsi, "subproject");
						string spdname = (string)dsi.Tables["subproject"].Rows[0]["spdname"];

                        sql = "SELECT * FROM itester.project WHERE pname='" + (string)dsi.Tables["subproject"].Rows[0]["pname"] + "'";
						dai = new MySqlDataAdapter(sql, mysqlConn);
						dsi = new DataSet();
						dai.Fill(dsi, "project");
						string pdname = (string)dsi.Tables["project"].Rows[0]["pdname"];

						TestViewOc.Add(new TestViewItem()
						{
							ID = (string)dr["id"],
							ProjectName = pdname,
							SubProjectName = spdname,
							PassRate = (string)dr["prate"],
							TotalCount = (int)dr["tcount"],
							FailErrorCount = (int)dr["fecount"],
							StartTime = (DateTime)dr["stime"],
							EndTime = (DateTime)dr["etime"],
							Duration = (string)dr["duration"],
							TestPC = (string)dr["testpc"],
							Tester = (string)dr["tester"]
						});
					}, null);
                }

				Dispatcher.Invoke((ThreadStart)delegate()
				{
					if (condition == null)
					{
						sql = "SELECT * FROM itester.project";
						da = new MySqlDataAdapter(sql, mysqlConn);
						ds = new DataSet();
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
							InRun = true;
							Dispatcher.Invoke((ThreadStart)delegate()
							{
								cboxProject.Items.Clear();
								foreach (Tuple<string, string> t in ProjectOc)
								{
									cboxProject.Items.Add(t.Item2);
								}
                                if(cboxProject.Items.Count > 0)
                                    cboxProject.SelectedIndex = 0;
							}, null);
							sql = "SELECT * FROM itester.subproject";
							da = new MySqlDataAdapter(sql, mysqlConn);
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
								ti = null;
								foreach (Tuple<string, ObservableCollection<Tuple<string, string>>> t in SubProjectOc)
								{
									if (t.Item1 == ProjectOc[cboxProject.SelectedIndex].Item1)
									{
										ti = t;
										break;
									}
								}
								if (ti != null)
								{
									cboxSubProject.Items.Clear();
									//cboxSubProject.Items.Add("All");
									foreach (Tuple<string, string> tit in ti.Item2)
									{
										cboxSubProject.Items.Add(tit.Item2);
									}
									if (ti.Item2.Count > 0)
										cboxSubProject.SelectedIndex = 0;
								}
							}, null);
							InRun = false;
						}
					}
				}, null);

                #endregion

                mysqlConn.Close();
                mysqlConn.Dispose();

                if (condition == null)
                {
                    SaveDBConfig();
                    InitOK = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot do initialization from the database.\nError message:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

				Dispatcher.Invoke((ThreadStart)delegate()
				{
					if (condition == null)
					{
						ProjectOc.Clear();
						SubProjectOc.Clear();
					}

					TestViewOc.Clear();
				}, null);
            }
            finally
            {
                //InDatabaseTalking = false;
            }
        }

        private void Project_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
			if (InRun == true)//_inPrjSubProuncUpdate == true)
				return;
			InRun = true;
            //_inProjectUpdate = true;

			cboxSubProject.Items.Clear();
			//cboxSubProject.Items.Add("All");

            if (cboxProject.Items.Count < 1 || cboxProject.SelectedIndex < 0)
                return;

            Tuple<string, string> ti = null;
            foreach (Tuple<string, string> t in ProjectOc)
            {
                if (t.Item2 == cboxProject.Items[cboxProject.SelectedIndex].ToString())
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
            foreach (Tuple<string, string> t in tii.Item2)
            {
                cboxSubProject.Items.Add(t.Item2);
            }
            if (cboxSubProject.Items.Count > 0)
                cboxSubProject.SelectedIndex = 0;

            //_inProjectUpdate = false;
			InRun = false;
        }

        private void SubProject_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InRun == true)//_inProjectUpdate == true || _inPrjSubProuncUpdate == true)
                return;
            //_inSubProjectUpdate = true;
			InRun = true;

			//FunctionOc.Clear();

			//FunctionOc.Add("All");

			//if (string.Compare(ProjectOc[cboxProject.SelectedIndex], "All", true) == 0)
			//{
			//    if (string.Compare(SubProjectOc[cboxSubProject.SelectedIndex], "All") == 0)
			//    {
			//        foreach (Tuple<string, string, string> tp in PrjSprjFuncOc)
			//        {
			//            if (FunctionOc.IndexOf(tp.Item3) < 0)
			//                FunctionOc.Add(tp.Item3);
			//        }
			//    }
			//    else
			//    {
			//        foreach (Tuple<string, string, string> tp in PrjSprjFuncOc)
			//        {
			//            if (string.Compare(SubProjectOc[cboxSubProject.SelectedIndex], tp.Item2) == 0)
			//            {
			//                if (FunctionOc.IndexOf(tp.Item3) < 0)
			//                    FunctionOc.Add(tp.Item3);
			//            }
			//        }
			//    }
			//}
			//else
			//{
			//    if (string.Compare(SubProjectOc[cboxSubProject.SelectedIndex], "All") == 0)
			//    {
			//        foreach (Tuple<string, string, string> tp in PrjSprjFuncOc)
			//        {
			//            if (string.Compare(ProjectOc[cboxProject.SelectedIndex], tp.Item1) == 0)
			//            {
			//                if (FunctionOc.IndexOf(tp.Item3) < 0)
			//                    FunctionOc.Add(tp.Item3);
			//            }
			//        }
			//    }
			//    else
			//    {
			//        foreach (Tuple<string, string, string> tp in PrjSprjFuncOc)
			//        {
			//            if (string.Compare(ProjectOc[cboxProject.SelectedIndex], tp.Item1) == 0
			//                && string.Compare(SubProjectOc[cboxSubProject.SelectedIndex], tp.Item2) == 0)
			//            {
			//                if (FunctionOc.IndexOf(tp.Item3) < 0)
			//                    FunctionOc.Add(tp.Item3);
			//            }
			//        }
			//    }
			//}

			//cboxFunction.SelectedIndex = 0;

			////_inSubProjectUpdate = false;
			InRun = false;
        }

        #region Window Exit

        private void Window_Exit(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure to quit \"iTester Viewer\"?", "Comfirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _bInNormalClose = true;

            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_bInNormalClose == false)
            {
                if (MessageBox.Show("Are you sure to quit \"iTester Viewer\"?", "Comfirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    e.Cancel = true;
            }

            base.OnClosing(e);
        }

        #endregion

        private void BrowseBack_Button_Click(object sender, RoutedEventArgs e)
        {
			Thread th = new Thread(new ThreadStart(BrowsBackThread));
			th.Start();
		}

		private void BrowsBackThread()
		{
			InRun = true;
			//InDatabaseTalking = true;

			try
			{
				DoBrowseBack();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Cannot browes back.\nError message:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				InRun = false;
				//InDatabaseTalking = false;
			}
		}

		private void DoBrowseBack()
		{
			Dispatcher.Invoke((ThreadStart)delegate()
			{
				CurrentDisplay--;
			}, null);
        }

        private void BrowseInto_Button_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentDisplay == DISPLAY_TESTVIEW)
            {
                if (dgTestView.Items.Count < 1)
                {
                    MessageBox.Show("No test collection exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (dgTestView.SelectedIndex < 0)
                {
                    MessageBox.Show("No test collection is selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else if (CurrentDisplay == DISPLAY_TESTCOL)
            {
                if (dgTestCol.Items.Count < 1)
                {
                    MessageBox.Show("No test item exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (dgTestCol.SelectedIndex < 0)
                {
                    MessageBox.Show("No test item is selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
                return;

			Thread th = new Thread(new ThreadStart(BrowsIntoThread));
			th.Start();
		}

		private void BrowsIntoThread()
		{
			InRun = true;
			//InDatabaseTalking = true;

			try
			{
				DoBrowseInto();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Cannot browes into.\nError message:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				InRun = false;
				//InDatabaseTalking = false;
			}
		}

		private void DoBrowseInto()
		{
			Dispatcher.Invoke((ThreadStart)delegate()
			{
				CurrentDisplay++;
			}, null);
		}

        private void BrowseUpdate_Button_Click(object sender, RoutedEventArgs e)
        {
			//CurrentDisplay = DISPLAY_TESTVIEW;

			Thread th = new Thread(new ThreadStart(BrowseUpdateThread));
			th.Start();
		}

		private void BrowseUpdateThread()
		{
			InRun = true;
			//InDatabaseTalking = true;

			try
			{
				DoBrowseUpdate();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Cannot update the results.\nError message:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				InRun = false;
				//InDatabaseTalking = false;
			}
		}

		private void DoBrowseUpdate()
		{
			Dispatcher.Invoke((ThreadStart)delegate()
			{
				BrowseUpdateJob();
			}, null);
		}

		private void BrowseUpdateJob()
		{
            switch (CurrentDisplay)
            {
                default:
                case DISPLAY_TESTVIEW:
                    Tuple<string, string> ti = null;
                    foreach (Tuple<string, string> t in ProjectOc)
                    {
                        if (t.Item2 == cboxProject.Items[cboxProject.SelectedIndex].ToString())
                        {
                            ti = t;
                            break;
                        }
                    }
                    string sp = ti.Item1;
                    Tuple<string, ObservableCollection<Tuple<string, string>>> tii = null;
                    foreach (Tuple<string, ObservableCollection<Tuple<string, string>>> t in SubProjectOc)
                    {
                        if (t.Item1 == ti.Item1)
                        {
                            tii = t;
                            break;
                        }
                    }
                    ti = null;
                    foreach (Tuple<string, string> t in tii.Item2)
                    {
                        if (t.Item2 == cboxSubProject.Items[cboxSubProject.SelectedIndex].ToString())
                        {
                            ti = t;
                            break;
                        }
                    }
                    string ssp = ti.Item1;
                    if (UseProjectSubProjectFilter != true)
                    {
                        sp = "";
                        ssp = "";
                    }
                    List<string> lstS = new List<string>();
                    if(!string.IsNullOrWhiteSpace(sp))
                        lstS.Add(sp);
                    if (!string.IsNullOrWhiteSpace(ssp))
                    {
                        if(lstS.Count > 0)
                            lstS.Add("AND");
                        lstS.Add(ssp);
                    }
                    string sfinal = "";
                    foreach (string s in lstS)
                    {
                        sfinal = sfinal + " " + s;
                    }
                    sfinal = sfinal.Trim();
					InitializeFromDatabaseJob(sfinal);
                    break;
                case DISPLAY_TESTCOL:
                    UpdatetestCol();
                    break;
                case DISPLAY_TESTRESULT:
                    UpdateTestResult();
                    break;
            }
        }

        private void UpdatetestCol()
        {
            #region

            //InDatabaseTalking = true;
			InRun = true;
            TestColOc.Clear();

            try
            {
                MySqlConnection mysqlConn = new MySqlConnection();
                string s = "server=" + DBIPAddress + ";" + "uid=root;" + "pwd=" + DBPassword + ";database=itester;";
                mysqlConn.ConnectionString = s;
                mysqlConn.Open();

                string tvid = TestViewOc[dgTestView.SelectedIndex].ID;

                string sql = "SELECT * FROM itester.testcol WHERE tvid='" + tvid + "'";
                MySqlDataAdapter da = new MySqlDataAdapter(sql, mysqlConn);
                DataSet ds = new DataSet();
                da.Fill(ds, "testcol");
                foreach (DataRow dr in ds.Tables["testcol"].Rows)
                {
                    TestColOc.Add(new TestColItem()
                    {
                        ID = (string)dr["id"],
                        TestViewID = (string)dr["tvid"],
                        TestName = (string)dr["tname"],
                        StartTime = (DateTime)dr["stime"],
                        Duration = (string)dr["duration"],
                        ErrorCount = (int)dr["ecount"],
                        FailCount = (int)dr["fcount"],
                        PassCount = (int)dr["pcount"],
                        InfoCount = (int)dr["icount"]
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot do initialization from the database.\nError message:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                TestColOc.Clear();
            }
            finally
            {
                //InDatabaseTalking = false;
				InRun = false;
            }

            #endregion
        }

        private void UpdateTestResult()
        {
            #region

            //InDatabaseTalking = true;
			InRun = true;
            TestResultOc.Clear();
			dgTestResult.DataContext = null;

            try
            {
                MySqlConnection mysqlConn = new MySqlConnection();
                string s = "server=" + DBIPAddress + ";" + "uid=root;" + "pwd=" + DBPassword + ";database=itester;";
                mysqlConn.ConnectionString = s;
                mysqlConn.Open();

                string tcid = TestColOc[dgTestCol.SelectedIndex].ID;

                if (UseProjectSubProjectFilter == true)
                {
                    Tuple<string, string> ti = null;
                    foreach (Tuple<string, string> t in ProjectOc)
                    {
                        if (t.Item2 == cboxProject.Items[cboxProject.SelectedIndex].ToString())
                        {
                            ti = t;
                            break;
                        }
                    }
                    string sql = "SELECT * FROM itester." + ti.Item1 + "testresult WHERE tcid='" + tcid + "' ORDER BY msgidx ASC";
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, mysqlConn);
                    DataSet ds = new DataSet();
                    da.Fill(ds, ti.Item1 + "testresult");
                    foreach (DataRow dr in ds.Tables[ti.Item1 + "testresult"].Rows)
                    {
                        TestResultOc.Add(new TestResultItem()
                        {
                            TestColID = (string)dr["tcid"],
                            MessageIndex = ((int)dr["msgidx"]).ToString(),
                            DBStateIndex = (int)dr["state"],
                            StartTime = (DateTime)dr["stime"],
                            Millisecond = (int)dr["msec"],
                            Category = (string)dr["category"],
                            Message = (string)dr["message"],
                            DataValue = (string)dr["value"],
                            Constraint = (string)dr["const"]
                        });
                    }
                }
                else
                {
                    foreach (Tuple<string, string> t in ProjectOc)
                    {
                        string sql = "SELECT * FROM itester." + t.Item1 + "testresult WHERE tcid='" + tcid + "' ORDER BY msgidx ASC";
                        MySqlDataAdapter da = new MySqlDataAdapter(sql, mysqlConn);
                        DataSet ds = new DataSet();
                        da.Fill(ds, t.Item1 + "testresult");
                        foreach (DataRow dr in ds.Tables[t.Item1 + "testresult"].Rows)
                        {
                            TestResultOc.Add(new TestResultItem()
                            {
                                TestColID = (string)dr["tcid"],
                                MessageIndex = ((int)dr["msgidx"]).ToString(),
                                DBStateIndex = (int)dr["state"],
                                StartTime = (DateTime)dr["stime"],
                                Millisecond = (int)dr["msec"],
                                Category = (string)dr["category"],
                                Message = (string)dr["message"],
                                DataValue = (string)dr["value"],
                                Constraint = (string)dr["const"]
                            });
                        }
                    }
                }

				dgTestResult.DataContext = TestResultOc;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot do initialization from the database.\nError message:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                TestResultOc.Clear();
            }
            finally
            {
                //InDatabaseTalking = false;
				InRun = false;
            }

            #endregion
        }

		private void TestView_DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (dgTestView.Items.Count < 1 || dgTestView.SelectedIndex < 0)
			{
				_currentTestViewIndex = -1;
				return;
			}
			_currentTestViewIndex = dgTestView.SelectedIndex;
		}

		private void TestCol_DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (dgTestCol.Items.Count < 1 || dgTestCol.SelectedIndex < 0)
			{
				_currentTestColIndex = -1;
				return;
			}
			_currentTestColIndex = dgTestCol.SelectedIndex;
		}

		private void TestResult_DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (dgTestResult.Items.Count < 1 || dgTestResult.SelectedIndex < 0)
			{
				_currentTestResultIndex = -1;
				return;
			}
			_currentTestResultIndex = dgTestResult.SelectedIndex;
		}

		private void TestResult_DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			//if (sender == null)
			//    return;
			//DataGrid grid = sender as DataGrid;
			//if (grid == null)
			//    return;
			//if (grid.Items.Count < 1)
			//    return;
			//if (grid.SelectedIndex < 0)
			//    return;

			//CurrentDisplay++;
		}

		private void TestCol_DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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

			CurrentDisplay++;
		}

		private void TestView_DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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

			CurrentDisplay++;
		}

		private void Statistics_Button_Click(object sender, RoutedEventArgs e)
		{

		}
    }

    public class TestViewItem : iTestBase.INotifyPropertyChangedClass
    {
		// TestView
		//
		// #ID : id : varchar(96) // testpc + tester + stime
		// Project Sub Project Name : pspname : varchar(64)
		// PassRate : prate : varchar(8)
		// TotalTestCount : tcount : int
		// FailErrorCount : fecount: int
		// StartTime : stime : datetime
		// EndTime : etime : datetime
		// Duration : duration : varchar(16)
		// TestPC : testpc : char(32)
		// Tester : tester : char(32)

        private string _id = "";
        public string ID
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                NotifyPropertyChanged("ID");
            }
        }

        private string _pName = "";
        public string ProjectName
        {
            get
            {
                return _pName;
            }
            set
            {
                _pName = value;
                NotifyPropertyChanged("ProjectName");
            }
        }

        private string _spName = "";
        public string SubProjectName
        {
            get
            {
                return _spName;
            }
            set
            {
                _spName = value;
                NotifyPropertyChanged("SubProjectName");
            }
        }

        private string _pr = "";
        public string PassRate
        {
            get
            {
                return _pr;
            }
            set
            {
                _pr = value;
                NotifyPropertyChanged("PassRate");
            }
        }

        private int _totalCount = 0;
        public int TotalCount
        {
            get
            {
                return _totalCount;
            }
            set
            {
                _totalCount = value;
                NotifyPropertyChanged("TotalCount");
                NotifyPropertyChanged("TotalCountString");
            }
        }

        public string TotalCountString
        {
            get
            {
                return _totalCount.ToString();
            }
        }

        private int _feCount = 0;
        public int FailErrorCount
        {
            get
            {
                return _feCount;
            }
            set
            {
                _feCount = value;
                NotifyPropertyChanged("FailErrorCount");
                NotifyPropertyChanged("FailErrorCountString");
            }
        }

        public string FailErrorCountString
        {
            get
            {
                return _feCount.ToString();
            }
        }

        private DateTime _sTime;
        public DateTime StartTime
        {
            get
            {
                return _sTime;
            }
            set
            {
                _sTime = value;
                NotifyPropertyChanged("StartTime");
                NotifyPropertyChanged("StartTimeString");
            }
        }

        public string StartTimeString
        {
            get
            {
                return StartTime.Year.ToString() + "-" + StartTime.Month.ToString() + "-" + StartTime.Day.ToString() + " " +
                    StartTime.Hour.ToString() + ":" + StartTime.Minute.ToString() + ":" + StartTime.Second.ToString() + "." + StartTime.Millisecond.ToString();
            }
         }

        private DateTime _eTime;
        public DateTime EndTime
        {
            get
            {
                return _eTime;
            }
            set
            {
                _eTime = value;
                NotifyPropertyChanged("EndTime");
                NotifyPropertyChanged("EndTimeString");
            }
        }

        public string EndTimeString
        {
            get
            {
                return EndTime.Year.ToString() + "-" + EndTime.Month.ToString() + "-" + EndTime.Day.ToString() + " " +
                    EndTime.Hour.ToString() + ":" + EndTime.Minute.ToString() + ":" + EndTime.Second.ToString() + "." + EndTime.Millisecond.ToString();
            }
        }

        private string _duration = "";
        public string Duration
        {
            get
            {
                return _duration;
            }
            set
            {
                _duration = value;
                NotifyPropertyChanged("Duration");
            }
        }

        private string _testPC = "";
        public string TestPC
        {
            get
            {
                return _testPC;
            }
            set
            {
                _testPC = value;
                NotifyPropertyChanged("TestPC");
            }
        }

        private string _tester = "";
        public string Tester
        {
            get
            {
                return _tester;
            }
            set
            {
                _tester = value;
                NotifyPropertyChanged("Tester");
            }
        }
    }

    public class TestColItem : iTestBase.INotifyPropertyChangedClass
    {
        // TestCol
        //
        // #ID : id : varchar(255)
        // Test View ID : tvid : varchar(255)
        // Test Name : tname : varchar(255)
        // Start Time : stime : datetime
        // Duration : duration : varchar(255)
        // Error Count : ecount : int
        // Fail Count : fcount : int
        // Pass Count : pcount : int
        // Information Count : icount: int

        private string _id = "";
        public string ID
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                NotifyPropertyChanged("ID");
            }
        }

        private string _tvId = "";
        public string TestViewID
        {
            get
            {
                return _tvId;
            }
            set
            {
                _tvId = value;
                NotifyPropertyChanged("TestViewID");
            }
        }

        private string _tname = "";
        public string TestName
        {
            get
            {
                return _tname;
            }
            set
            {
                _tname = value;
                NotifyPropertyChanged("TestName");
            }
        }

        private DateTime _sTime;
        public DateTime StartTime
        {
            get
            {
                return _sTime;
            }
            set
            {
                _sTime = value;
                NotifyPropertyChanged("StartTime");
                NotifyPropertyChanged("StartTimeString");
            }
        }

        public string StartTimeString
        {
            get
            {
                return StartTime.Year.ToString() + "-" + StartTime.Month.ToString() + "-" + StartTime.Day.ToString() + " " +
                    StartTime.Hour.ToString() + ":" + StartTime.Minute.ToString() + ":" + StartTime.Second.ToString() + "." + StartTime.Millisecond.ToString();
            }
        }

        private string _duration = "";
        public string Duration
        {
            get
            {
                return _duration;
            }
            set
            {
                _duration = value;
                NotifyPropertyChanged("Duration");
            }
        }

        private int _fCount = 0;
        public int FailCount
        {
            get
            {
                return _fCount;
            }
            set
            {
                _fCount = value;
                NotifyPropertyChanged("FailCount");
                NotifyPropertyChanged("FailCountString");
                NotifyPropertyChanged("DBStateBackgroundIndex");
            }
        }

        public string FailCountString
        {
            get
            {
                return _fCount.ToString();
            }
        }

        private int _eCount = 0;
        public int ErrorCount
        {
            get
            {
                return _eCount;
            }
            set
            {
                _eCount = value;
                NotifyPropertyChanged("ErrorCount");
                NotifyPropertyChanged("ErrorCountString");
                NotifyPropertyChanged("DBStateBackgroundIndex");
            }
        }

        public string ErrorCountString
        {
            get
            {
                return _eCount.ToString();
            }
        }

        public enum DBState
        {
            None,
            Pass,
            Fail,
            Error,
            Information
        }

        public int DBStateBackgroundIndex
        {
            get
            {
                if (ErrorCount > 0)
                    return (int)DBState.Error;
                if (FailCount > 0)
                    return (int)DBState.Fail;
                return (int)DBState.None;
            }
        }


        private int _pCount = 0;
        public int PassCount
        {
            get
            {
                return _pCount;
            }
            set
            {
                _pCount = value;
                NotifyPropertyChanged("PassCount");
                NotifyPropertyChanged("PassCountString");
                NotifyPropertyChanged("DBStateBackgroundIndex");
            }
        }

        public string PassCountString
        {
            get
            {
                return _pCount.ToString();
            }
        }

        private int _iCount = 0;
        public int InfoCount
        {
            get
            {
                return _iCount;
            }
            set
            {
                _iCount = value;
                NotifyPropertyChanged("InfoCount");
                NotifyPropertyChanged("InfoCountString");
                NotifyPropertyChanged("DBStateBackgroundIndex");
            }
        }

        public string InfoCountString
        {
            get
            {
                return _iCount.ToString();
            }
        }
    }

    public class TestResultItem : iTestBase.INotifyPropertyChangedClass
    {
        // TestResult
        //
        // Message Index : msgidx : int
        // Test Collection ID : tcid : varchar(128)
        // State : state : tinyint
        // Start Time : stime : datetime
        // Millisecond : msec : int
        // Category : category : varchar(255)
        // Message : message : varchar(255)
        // Data Value : value : varchar(255)
        // Constraint : const : varchar(255)

        private string _msgIdx = "";
        public string MessageIndex
        {
            get
            {
                return _msgIdx;
            }
            set
            {
                _msgIdx = value;
                NotifyPropertyChanged("MessageIndex");
            }
        }

        private string _tcId = "";
        public string TestColID
        {
            get
            {
                return _tcId;
            }
            set
            {
                _tcId = value;
                NotifyPropertyChanged("TestColID");
            }
        }

        public enum DBState
        {
            None,
            Pass,
            Fail,
            Error,
            Information
        }

        private int _dbStateIndex = 0;
        public int DBStateIndex
        {
            get
            {
                return _dbStateIndex;
            }
            set
            {
                _dbStateIndex = value;
                if (_dbStateIndex < 0)
                    _dbStateIndex = 0;
                if (_dbStateIndex > (int)DBState.Error)
                    _dbStateIndex = (int)DBState.Error;
                DBStateType = (DBState)value;
                NotifyPropertyChanged("DBStateIndex");
                NotifyPropertyChanged("DBStateIndexString");
            }
        }

        public string DBStateIndexString
        {
            get
            {
                return DBStateIndex.ToString();
            }
        }

        private DBState _dbStateType = DBState.None;
        public DBState DBStateType
        {
            get
            {
                return _dbStateType;
            }
            set
            {
                _dbStateType = value;
                if (_dbStateType != DBState.None)
                {
                    _dbStateImage = new BitmapImage();
                    _dbStateImage.BeginInit();
                    switch (_dbStateType)
                    {
                        default:
                        case DBState.None:
                            break;
                        case DBState.Information:
							_dbStateImage.UriSource = new Uri("pack://application:,,,/TestempoResultViewer;component/resources/status_info.png");
                            break;
                        case DBState.Pass:
							_dbStateImage.UriSource = new Uri("pack://application:,,,/TestempoResultViewer;component/resources/status_ok.png");
                            break;
                        case DBState.Fail:
							_dbStateImage.UriSource = new Uri("pack://application:,,,/TestempoResultViewer;component/resources/status_error.png");
                            break;
                        case DBState.Error:
							_dbStateImage.UriSource = new Uri("pack://application:,,,/TestempoResultViewer;component/resources/status_ques.ico");
                            break;
                    }
                    _dbStateImage.EndInit();
                }
                switch (_dbStateType)
                {
                    default:
                    case DBState.None:
                        _dbStateBackgound = new SolidColorBrush(Colors.Transparent);
                        break;
                    case DBState.Information:
                        _dbStateBackgound = new SolidColorBrush(Colors.LightBlue);
                        break;
                    case DBState.Pass:
                        _dbStateBackgound = new SolidColorBrush(Colors.LightGreen);
                        break;
                    case DBState.Fail:
                        _dbStateBackgound = new SolidColorBrush(Colors.Red);
                        break;
                    case DBState.Error:
                        _dbStateBackgound = new SolidColorBrush(Colors.Yellow);
                        break;
                }
                _dbStateIndex = (int)_dbStateType;
                NotifyPropertyChanged("DBStateIndex");
                NotifyPropertyChanged("DBStateIndexString");
                NotifyPropertyChanged("DBStateType");
                NotifyPropertyChanged("DBStateImage");
                NotifyPropertyChanged("DBStateBackground");
                NotifyPropertyChanged("DBStateBackgroundIndex");
                NotifyPropertyChanged("DBStateImagePath");
            }
        }

        public int DBStateBackgroundIndex
        {
            get
            {
                return (int)DBStateType;
            }
        }

        private SolidColorBrush _dbStateBackgound = new SolidColorBrush(Colors.Transparent);
        public SolidColorBrush DBStateBackground
        {
            get
            {
                return _dbStateBackgound;
            }
            set
            {
                _dbStateBackgound = value;
                NotifyPropertyChanged("DBStateBackground");
            }
        }

        private string DBStateImagePath
        {
            get
            {
                switch (_dbStateType)
                {
                    default:
                    case DBState.None:
                        return "";
                    case DBState.Information:
                        return "resources/status_info.png";
                    case DBState.Pass:
                        return "resources/status_ok.png";
                    case DBState.Fail:
                        return "resources/status_error.png";
                    case DBState.Error:
                        return "resources/status_ques.ico";
                }
            }
        }

        private BitmapImage _dbStateImage = null;
        public BitmapImage DBStateImage
        {
            get
            {
                return _dbStateImage;
            }
            set
            {
                _dbStateImage = value;
                NotifyPropertyChanged("DBStateImage");
            }
        }

        private DateTime _sTime;
        public DateTime StartTime
        {
            get
            {
                return _sTime;
            }
            set
            {
                _sTime = value;
                NotifyPropertyChanged("StartTime");
                NotifyPropertyChanged("StartTimeString");
            }
        }

		private int _millisecond = 0;
		public int Millisecond
		{
			get
			{
				return _millisecond;
			}
			set
			{
				_millisecond = value;
				NotifyPropertyChanged("Millisecond");
			}
		}

        public string StartTimeString
        {
            get
            {
                return StartTime.Year.ToString() + "-" + StartTime.Month.ToString() + "-" + StartTime.Day.ToString() + " " +
                    StartTime.Hour.ToString() + ":" + StartTime.Minute.ToString() + ":" + StartTime.Second.ToString() + "." + Millisecond.ToString();
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

        private string _dataValue = "";
        public string DataValue
        {
            get
            {
                return _dataValue;
            }
            set
            {
                _dataValue = value;
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
    }
}
