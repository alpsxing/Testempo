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

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Data.Odbc;
using System.IO;

using MySql.Data;
using MySql.Data.MySqlClient;
using MySql.Data.Common;
using MySql.Data.Entity;
using MySql.Data.Types;
//using MySql.Data.VisualStudio;
using System.Globalization;
using System.Data;
using System.Collections.Specialized;

using iTester;

namespace iTesterDBSetup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class iTesterDBSetupMain : Window, INotifyPropertyChanged
    {
        private bool _bInNormalClose = false;
        private const string DB_NAME = "itester";
        private const string DB_USERNAME = "root";

		private object _objLock = new object();

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
                NotifyPropertyChanged("InDatabaseTalking");
                NotifyPropertyChanged("NotInDatabaseTalking");
            }
        }

        public bool NotInDatabaseTalking
        {
            get
            {
                return !_inDatabaseTalking;
            }
        }

		private bool _databaseInitialized = false;
		public bool DatabaseInitialized
		{
			get
			{
				return _databaseInitialized;
			}
			set
			{
				_databaseInitialized = value;
				NotifyPropertyChanged("DatabaseInitialized");
				NotifyPropertyChanged("DatabaseNotInitialized");
			}
		}

		public bool DatabaseNotInitialized
		{
			get
			{
				return !_databaseInitialized;
			}
		}

		private bool _inProjectInit = false;

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

        private ObservableCollection<DatabaseMessage> _dbMsgOc = new ObservableCollection<DatabaseMessage>();
        public ObservableCollection<DatabaseMessage> DBMsgOc
        {
            get
            {
                return _dbMsgOc;
            }
            set
            {
                _dbMsgOc = value;
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

		private string _dbName = DB_NAME;
        public string DBName
        {
            get
            {
				return DB_NAME;
            }
            set
            {
				_dbName = DB_NAME;
                NotifyPropertyChanged("DBName");
                NotifyPropertyChanged("DBNameOK");
            }
        }

        public bool DBNameOK
        {
            get
            {
                return !string.IsNullOrWhiteSpace(_dbName);
            }
        }

        private string _dbUsername = DB_USERNAME;
		public string DBUsername
		{
			get
			{
                return DB_USERNAME;
			}
			set
			{
                _dbUsername = DB_USERNAME;
				NotifyPropertyChanged("DBUsername");
				NotifyPropertyChanged("DBUsernameOK");
			}
		}

		public bool DBUsernameOK
		{
			get
			{
				return !string.IsNullOrWhiteSpace(_dbUsername);
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


        public iTesterDBSetupMain()
        {
            InitializeComponent();

            DataContext = this;
            dgLog.ItemsSource = DBMsgOc;
			DBMsgOc.CollectionChanged += new NotifyCollectionChangedEventHandler(DBMsgOc_CollectionChanged);
			//cboxProject.DataContext = ProjectOc;
			//cboxSubProject.DataContext = SubProjectOc;

			LoadDBConfig();
        }

		private void DBMsgOc_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			lock (_objLock)
			{
				if (DBMsgOc.Count < 1)
					return;
				try
				{
					var border = VisualTreeHelper.GetChild(dgLog, 0) as Decorator;
					if (border != null)
					{
						var scroll = border.Child as ScrollViewer;
						if (scroll != null) scroll.ScrollToEnd();
					}
				}
				catch (Exception) { }
			}
		}

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Window Exit

        private void Window_Exit(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure to quit \"iTester Database Setup\"?", "Comfirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            _bInNormalClose = true;

            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_bInNormalClose == false)
            {
                if (MessageBox.Show("Are you sure to quit \"iTester Database Setup\"?", "Comfirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    e.Cancel = true;
            }

            base.OnClosing(e);
        }

        #endregion

		private void LoadDBConfig()
		{
			try
			{
				StreamReader sr = new StreamReader("dbconfig.txt");
				string strLine = null;
				int i=0;
				while (true)
				{
					strLine = sr.ReadLine();
					if (string.IsNullOrWhiteSpace(strLine))
						break;
					if (i == 0)
						DBIPAddress = strLine.Trim();
					else if (i == 1)
						DBName = DB_NAME;
					else if (i == 2)
						DBUsername = strLine.Trim();
					else if (i == 3)
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
				AddDatabaseMessage(DatabaseMessage.DBState.Error, "Cannot load the database config file.");
				AddDatabaseMessage(DatabaseMessage.DBState.Error, "Error message :");
				AddDatabaseMessage(DatabaseMessage.DBState.Error, ex.Message);
			}
		}

		private void SaveDBConfig()
		{
			try
			{
				StreamWriter sw = new StreamWriter("dbconfig.txt");
				sw.WriteLine(DBIPAddress);
				sw.WriteLine(DB_NAME);
				sw.WriteLine(DBUsername);
				sw.WriteLine(DBPassword);
				sw.Close();
				sw.Dispose();
			}
			catch (Exception ex)
			{
				AddDatabaseMessage(DatabaseMessage.DBState.Error, "Cannot save the database config file.");
				AddDatabaseMessage(DatabaseMessage.DBState.Error, "Error message :");
				AddDatabaseMessage(DatabaseMessage.DBState.Error, ex.Message);
			}
		}

        private void SetupDatabase_Button_Click(object sender, RoutedEventArgs e)
        {
            InDatabaseTalking = true;
            spbTimer.IsIndeterminate = true;
            DBMsgOc.Clear();
			ReadyString = "Start creating or verifying the database.";

            Thread th = new Thread(new ThreadStart(ConnectDisconnectDBThread));
            th.Start();
        }

        private void ConnectDisconnectDBThread()
        {
			try
			{
				AddDatabaseMessage(DatabaseMessage.DBState.None, "Begin configging the database.");

				DoSetupDatabase();

				AddDatabaseMessage(DatabaseMessage.DBState.None, "End configging the database.");

				SaveDBConfig();
			}
			catch (MySqlException ex)
			{
				switch (ex.Number)
				{
					case 0:
						AddDatabaseMessage(DatabaseMessage.DBState.Error, "Cannot connect to server.  Contact administrator");
						break;
					case 1045:
						AddDatabaseMessage(DatabaseMessage.DBState.Error, "Invalid username/password, please try again");
						break;
					default:
						break;
				}
				AddDatabaseMessage(DatabaseMessage.DBState.Error, "Error message :");
				AddDatabaseMessage(DatabaseMessage.DBState.Error, ex.Message);

				ReadyString = "Warning : Database for iTester is NOT ready.";
			}
			catch (Exception ex)
			{
				AddDatabaseMessage(DatabaseMessage.DBState.Error, "Cannot config the database.");
				AddDatabaseMessage(DatabaseMessage.DBState.Error, "Error message :");
				AddDatabaseMessage(DatabaseMessage.DBState.Error, ex.Message);

				ReadyString = "Warning : Database for iTester is NOT ready.";
			}
            finally
            {
                InDatabaseTalking = false;
                Dispatcher.Invoke((ThreadStart)delegate()
                {
                    spbTimer.IsIndeterminate = false;
                }, null);
				DatabaseInitialized = true;
            }
        }

        private void AddDatabaseMessage(DatabaseMessage.DBState dbs, string msg)
        {
            Dispatcher.Invoke((ThreadStart)delegate()
            {
                DateTime dt = DateTime.Now;
                DBMsgOc.Add(
                    new DatabaseMessage()
                    {
                        MsgDateTime = dt.ToLongDateString() + " : " + dt.ToLongTimeString(),
                        DBStateType = dbs,
                        Message = msg
                    });
            }, null);
        }

		private void DoSetupDatabase()
		{
			MySqlConnection conn = new MySqlConnection();
			string s = "server=" + DBIPAddress + ";" + "uid=" + DBUsername + ";" + "pwd=" + DBPassword + ";";
			conn.ConnectionString = s;
			conn.Open();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is opened.");

			#region Database

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Query all databases.");

			DataTable dt = conn.GetSchema("Databases");
			bool found = false;
			foreach (System.Data.DataRow row in dt.Rows)
			{
				int index = 0;
				foreach (System.Data.DataColumn col in dt.Columns)
				{
					if (index == 1)
					{
						if (string.Compare(row[col].ToString(), DB_NAME) == 0)
							found = true;
						AddDatabaseMessage(DatabaseMessage.DBState.None, String.Format("Database : {0}", row[col]));
					}
					index++;
				}
			}
			if (found == true)
				AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Find the itester database.");
			else
			{
				AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "No itester database.");
				AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Start creating the itester database.");

				MySqlCommand cmd = new MySqlCommand();
				cmd.Connection = conn;
				cmd.CommandText = "CREATE DATABASE " + DB_NAME;
				cmd.ExecuteNonQuery();

				AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "End creating the itester database.");
			}

			#endregion

			#region Table

			conn.Close();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is closed.");

			conn.Dispose();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is disposed.");

			conn = new MySqlConnection();
			s = "server=" + DBIPAddress + ";" + "uid=" + DBUsername + ";" + "pwd=" + DBPassword + ";database=" + DB_NAME + ";";
			conn.ConnectionString = s;
			conn.Open();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is opened.");

			#region Project & Sub Project

			// Project
			//
            // Project Name : pname : varchar(32)
            // Project Display Name : pdname : varchar(32)

			string sql = "CREATE TABLE project("
				+ "pname VARCHAR(32) NOT NULL PRIMARY KEY,"
				+ "pdname VARCHAR(32) NOT NULL)";
			if (CreateTable(conn, "project", sql) == false)
				return;

			// Sub Project
			//
			// Project & Sub Project : pspname : varchar(64) // pname+spname
			// Project Name : pname : varchar(32)
            // Sub Project Name : spname : varchar(32)
            // Sub Project Display Name : spdname : varchar(32)

			sql = "CREATE TABLE subproject("
				+ "pspname VARCHAR(64) NOT NULL PRIMARY KEY,"
				+ "pname VARCHAR(32) NOT NULL,"
                + "spname VARCHAR(32) NOT NULL,"
                + "spdname VARCHAR(32) NOT NULL,"
                + "CONSTRAINT FOREIGN KEY (pname) REFERENCES project(pname))";
			if (CreateTable(conn, "subproject", sql) == false)
				return;

			Dispatcher.Invoke((ThreadStart)delegate()
			{
				ProjectOc.Clear();
				SubProjectOc.Clear();
			}, null);

			sql = "SELECT * FROM itester.project";
			MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
			DataSet ds = new DataSet();
			da.Fill(ds, "project");
			foreach (DataRow dr in ds.Tables["project"].Rows)
			{
				Dispatcher.Invoke((ThreadStart)delegate()
				{
					ProjectOc.Add(new Tuple<string,string>((string)dr["pname"], (string)dr["pdname"]));
				}, null);
			}
			if (ProjectOc.Count > 0)
			{
				_inProjectInit = true;
				Dispatcher.Invoke((ThreadStart)delegate()
				{
                    cboxProject.Items.Clear();
                    foreach (Tuple<string, string> t in ProjectOc)
                    {
                        cboxProject.Items.Add(t.Item2);
                    }
                    cboxProject.SelectedIndex = 0;
				}, null);
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
							ti.Item2.Add(new Tuple<string,string>(sp, spd));
						else
						{
                            ObservableCollection<Tuple<string, string>> spoc = new ObservableCollection<Tuple<string, string>>();
                            spoc.Add(new Tuple<string, string>(sp, spd));
                            ti = new Tuple<string, ObservableCollection<Tuple<string, string>>>(p, spoc);
							SubProjectOc.Add(ti);
						}
					}
					ti = null;
					foreach (Tuple<string, ObservableCollection<Tuple<string,string>>> t in SubProjectOc)
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
                        foreach (Tuple<string, string> tit in ti.Item2)
                        {
                            cboxSubProject.Items.Add(tit.Item2);
                        }
						if (ti.Item2.Count > 0)
							cboxSubProject.SelectedIndex = 0;
					}
				}, null);
				_inProjectInit = false;
			}

			#endregion

			#region Test View

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

			sql = "CREATE TABLE testview("
				+ "id VARCHAR(96) NOT NULL PRIMARY KEY,"
				+ "pspname VARCHAR(64) NOT NULL,"
				+ "prate VARCHAR(8) NOT NULL DEFAULT '0.0%',"
				+ "tcount INT NOT NULL DEFAULT 0,"
                + "fecount INT NOT NULL DEFAULT 0,"
				+ "stime DATETIME NOT NULL DEFAULT '1000-01-01 00:00:00',"
				+ "etime DATETIME NOT NULL DEFAULT '1000-01-01 00:00:00',"
				+ "duration VARCHAR(16) NOT NULL DEFAULT '00.00:00:00.0',"
				+ "testpc VARCHAR(32) NOT NULL DEFAULT 'NA',"
				+ "tester VARCHAR(32) NOT NULL DEFAULT 'NA',"
				+ "CONSTRAINT FOREIGN KEY (pspname) REFERENCES subproject(pspname))";
			if (CreateTable(conn, "testview", sql) == false)
				return;

			#endregion

			#region Test Collection

			// TestCol
			//
            // #ID : id : varchar(128) // testpc + tester + stime + tname
            // Test View ID : tvid : varchar(96)
			// Test Name : tname : varchar(32)
			// Start Time : stime : datetime
			// Duration : duration : varchar(16)
			// Error Count : ecount : int
			// Fail Count : fcount : int
			// Pass Count : pcount : int
			// Information Count : icount: int

			sql = "CREATE TABLE testcol("
                + "id VARCHAR(128) NOT NULL PRIMARY KEY,"
                + "tvid VARCHAR(96) NOT NULL,"
				+ "tname VARCHAR(32) NOT NULL DEFAULT 'NA',"
				+ "stime DATETIME NOT NULL DEFAULT '1000-01-01 00:00:00',"
				+ "duration VARCHAR(16) NOT NULL DEFAULT '00.00:00:00.0',"
                + "ecount INT NOT NULL DEFAULT 0,"
                + "fcount INT NOT NULL DEFAULT 0,"
                + "pcount INT NOT NULL DEFAULT 0,"
                + "icount INT NOT NULL DEFAULT 0,"
				+ "CONSTRAINT FOREIGN KEY (tvid) REFERENCES testview(id))";
			if (CreateTable(conn, "testcol", sql) == false)
				return;

			#endregion

			#region Test Result (Dynamically in Run-Time)

			//// TestResult
			////
			//// #ID : id : varchar(255)
			//// Project ID : pid : int
			//// Message Index : msgidx : int(255)
			//// Test Collection ID : tcid : varchar(255)
			//// State : state : tinyint
			//// Start Time : stime : datetime
			//// Category : category : varchar(255)
			//// Message : message : varchar(255)
			//// Data Value : value : varchar(255)
			//// Constraint : const : varchar(255)

			//sql = "CREATE TABLE testresult("
			//    + "id VARCHAR(255) NOT NULL PRIMARY KEY,"
			//    + "pid INT NOT NULL,"
			//    + "msgidx INT NOT NULL,"
			//    + "tcid VARCHAR(255) NOT NULL,"
			//    + "state INT NOT NULL DEFAULT 0,"
			//    + "stime DATETIME NOT NULL DEFAULT '1000-01-01 00:00:00',"
			//    + "msec INT NOT NULL,"
			//    + "category VARCHAR(255) NOT NULL DEFAULT '',"
			//    + "message VARCHAR(255) NOT NULL DEFAULT '',"
			//    + "value VARCHAR(255) NOT NULL DEFAULT '',"
			//    + "const VARCHAR(255) NOT NULL DEFAULT '',"
			//    + "CONSTRAINT FOREIGN KEY (tcid) REFERENCES testcol(id))";
			//if (CreateTable(conn, "testresult", sql) == false)
			//    return;

			#endregion

			#endregion

			#region Create sep user (Empty)

			//sql = "SELECT * FROM mysql.user WHERE User='sep' AND Host='localhost'";
			//MySqlCommand sqlCmd = new MySqlCommand();
			//sqlCmd.Connection = conn;
			//sqlCmd.CommandText = sql;
			//sqlCmd.CommandType = CommandType.Text;
			//MySqlDataReader dr = sqlCmd.ExecuteReader();
			//found = false;
			//while (dr.Read())
			//{
			//    if (string.Compare("sep", (string)dr[1], true) == 0)
			//    {
			//        found = true;
			//        break;
			//    }
			//}
			//dr.Close();
			//dr.Dispose();
			//if (found == true)
			//    AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Find the sep user.");
			//else
			//{
			//    AddDatabaseMessage(DatabaseMessage.DBState.Fail, "No sep user.");
			//    AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Start creating the sep user.");

			//    sql = "INSERT INTO mysql.user(Host,User,Password,ssl_cipher,x509_issuer,x509_subject) VALUES('localhost','sep','sep','','','')";
			//    sqlCmd.CommandText = sql;
			//    sqlCmd.ExecuteNonQuery();

			//    found = false;
			//    int count = 0;
			//    while (true)
			//    {
			//        AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Query the user : Loop Index " + (count + 1).ToString());

			//        sql = "SELECT * FROM mysql.user WHERE User='sep' AND Host='localhost'";
			//        sqlCmd.CommandText = sql;
			//        dr = sqlCmd.ExecuteReader();
			//        found = false;
			//        while (dr.Read())
			//        {
			//            if (string.Compare("sep", (string)dr[1], true) == 0)
			//            {
			//                found = true;
			//                break;
			//            }
			//        }
			//        dr.Close();
			//        dr.Dispose();
			//        if (found)
			//            break;
			//        if (count >= 30)
			//            break;
			//        count++;
			//        Thread.Sleep(1000);
			//    }
			//    if (found == true)
			//        AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Find the sep user.");
			//    else
			//    {
			//        AddDatabaseMessage(DatabaseMessage.DBState.Error, "Cannot find the sep user.");

			//        conn.Close();

			//        AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is closed.");

			//        conn.Dispose();

			//        AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is disposed.");

			//        ReadyString = "Warning : Database for iTester is NOT ready.";

			//        return;
			//    }
			//}

			//AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Set SELECT/INSERT privileges to the sep user.");

			//sql = "UPDATE mysql.user SET Select_priv='Y',Update_priv='Y' WHERE Host='localhost' AND User='sep';";
			//sqlCmd.CommandText = sql;
			//sqlCmd.ExecuteNonQuery();

			//AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Flush privileges.");

			//sql = "FLUSH PRIVILEGES";
			//sqlCmd.CommandText = sql;
			//sqlCmd.ExecuteNonQuery();

			//AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Delete null user.");

			//sql = "DELETE FROM mysql.user WHERE Host='localhost' AND User='';";
			//sqlCmd.CommandText = sql;
			//sqlCmd.ExecuteNonQuery();

			#endregion

			conn.Close();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is closed.");

			conn.Dispose();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is disposed.");

			ReadyString = "Infromation : Database for iTester is ready.";
		}

		private bool CreateTable(MySqlConnection conn, string tablename, string sql, bool setReady = true)
		{
			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Query all tables.");

			DataTable dt = conn.GetSchema("Tables");
			bool found = false;
			foreach (System.Data.DataRow row in dt.Rows)
			{
				int index = 0;
				foreach (System.Data.DataColumn col in dt.Columns)
				{
					if (index == 2)
					{
						if (string.Compare(row[col].ToString(), tablename, false) == 0)
							found = true;
						AddDatabaseMessage(DatabaseMessage.DBState.None, String.Format("Table : {0}", row[col]));
					}
					index++;
				}
			}
			if (found)
				AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Find the : " + tablename + " table.");
			else
			{
				#region

				AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "No " + tablename + " table.");
				AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Start creating the " + tablename + " table.");

				MySqlCommand cmd = new MySqlCommand();
				cmd.Connection = conn;
				cmd.CommandText = sql;
				cmd.ExecuteNonQuery();

				AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "End creating the " + tablename + " table.");

				#endregion
			}

			return true;
		}

        private void DBIPAddress_TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
			DBIPAddress = DBIPAddress.Trim();
        }

        private void DBName_TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
			DBName = DBName.Trim();
        }

		private void DBPassword_PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
		{
			DBPassword = pbDBPW.Password.Trim();
		}

		private void DBUsername_TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			DBUsername = DBUsername.Trim();
		}

		private void SaveConfig_Button_Click(object sender, RoutedEventArgs e)
		{
			SaveDBConfig();
		}

		private void DestroyDatabase_Button_Click(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show("Are you sure to destroy the whole database?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
				return;

			OneLineEditor ole = new OneLineEditor("Input Password", "Password");
			if (ole.ShowDialog() != true)
				return;

			if (ole.UserContent != "itester")
			{
				MessageBox.Show("Wrong password!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			InDatabaseTalking = true;
			spbTimer.IsIndeterminate = true;
			DBMsgOc.Clear();
			ReadyString = "Start destroying the database.";
			
			Thread th = new Thread(new ThreadStart(DestroyDatabaseThread));
			th.Start();
		}

		private void DestroyDatabaseThread()
		{
			try
			{
				AddDatabaseMessage(DatabaseMessage.DBState.None, "Begin destroying the database.");

				DoDestroyDatabase();

				AddDatabaseMessage(DatabaseMessage.DBState.None, "End destroying the database.");
			}
			catch (MySqlException ex)
			{
				switch (ex.Number)
				{
					case 0:
						AddDatabaseMessage(DatabaseMessage.DBState.Error, "Cannot connect to server.  Contact administrator");
						break;
					case 1045:
						AddDatabaseMessage(DatabaseMessage.DBState.Error, "Invalid username/password, please try again");
						break;
					default:
						break;
				}
				AddDatabaseMessage(DatabaseMessage.DBState.Error, "Error message :");
				AddDatabaseMessage(DatabaseMessage.DBState.Error, ex.Message);
			}
			catch (Exception ex)
			{
				AddDatabaseMessage(DatabaseMessage.DBState.Error, "Cannot destroy the database.");
				AddDatabaseMessage(DatabaseMessage.DBState.Error, "Error message :");
				AddDatabaseMessage(DatabaseMessage.DBState.Error, ex.Message);
			}
			finally
			{
				InDatabaseTalking = false;
				Dispatcher.Invoke((ThreadStart)delegate()
				{
					spbTimer.IsIndeterminate = false;
				}, null);
				ReadyString = "Ready";
				DatabaseInitialized = false;
			}
		}

		private void DoDestroyDatabase()
		{
			MySqlConnection conn = new MySqlConnection();
			string s = "server=" + DBIPAddress + ";" + "uid=" + DBUsername + ";" + "pwd=" + DBPassword + ";";
			conn.ConnectionString = s;
			conn.Open();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is opened.");

			#region Delete User (Empty)

			//AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Start deleting the sep user.");
			
			//string sql = "DELETE FROM mysql.user WHERE User='sep' AND Host='localhost'";
			//MySqlCommand sqlCmd = new MySqlCommand();
			//sqlCmd.Connection = conn;
			//sqlCmd.CommandText = sql;
			//sqlCmd.CommandType = CommandType.Text;
			//sqlCmd.ExecuteNonQuery();

			//AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Finish deleting the sep user.");

			//AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Query the sep user.");
	
			//sql = "SELECT * FROM mysql.user WHERE User='sep' AND Host='localhost'";
			//sqlCmd = new MySqlCommand();
			//sqlCmd.Connection = conn;
			//sqlCmd.CommandText = sql;
			//sqlCmd.CommandType = CommandType.Text;
			//MySqlDataReader dr = sqlCmd.ExecuteReader();
			//bool found = false;
			//while (dr.Read())
			//{
			//    if (string.Compare("sep", (string)dr[1], true) == 0)
			//    {
			//        found = true;
			//        break;
			//    }
			//}
			//dr.Close();
			//dr.Dispose();
			//if (found == false)
			//    AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "The sep user is deleted.");
			//else
			//{
			//    AddDatabaseMessage(DatabaseMessage.DBState.Fail, "Find the sep user.");
			//    AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Start deleting the sep user.");

			//    sql = "DELETE FROM mysql.user WHERE User='sep' AND Host='localhost'";
			//    sqlCmd.CommandText = sql;
			//    sqlCmd.ExecuteNonQuery();

			//    found = false;
			//    int count = 0;
			//    while (true)
			//    {
			//        AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Query the sep user : Loop Index " + (count + 1).ToString());

			//        sql = "DELETE FROM mysql.user WHERE User='sep' AND Host='localhost'";
			//        sqlCmd.CommandText = sql;
			//        dr = sqlCmd.ExecuteReader();
			//        found = false;
			//        while (dr.Read())
			//        {
			//            if (string.Compare("sep", (string)dr[1], true) == 0)
			//            {
			//                found = true;
			//                break;
			//            }
			//        }
			//        dr.Close();
			//        dr.Dispose();
			//        if (found)
			//            break;
			//        if (count >= 30)
			//            break;
			//        count++;
			//        Thread.Sleep(1000);
			//    }
			//    if (found == false)
			//        AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "The sep user is deleted.");
			//    else
			//    {
			//        AddDatabaseMessage(DatabaseMessage.DBState.Error, "Cannot delete the sep user.");

			//        conn.Close();

			//        AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is closed.");

			//        conn.Dispose();

			//        AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is disposed.");

			//        ReadyString = "Warning : Database has error in deleting the sep user.";

			//        return;
			//    }
			//}

			#endregion

			#region Drop Database

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Query the database.");

			DataTable dt = conn.GetSchema("Databases");
			bool found = false;
			foreach (System.Data.DataRow row in dt.Rows)
			{
				int index = 0;
				foreach (System.Data.DataColumn col in dt.Columns)
				{
					if (index == 1)
					{
						if (string.Compare(row[col].ToString(), DB_NAME) == 0)
							found = true;
						AddDatabaseMessage(DatabaseMessage.DBState.None, String.Format("Database : {0}", row[col]));
					}
					index++;
				}
			}
			if (found == false)
			{
				AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "No itester database.");

				return;
			}

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Start dropping the itester database.");

			string sql = "DROP DATABASE itester";
			MySqlCommand sqlCmd = new MySqlCommand();
			sqlCmd.Connection = conn;
			sqlCmd.CommandText = sql;
			sqlCmd.CommandType = CommandType.Text;
			sqlCmd.ExecuteNonQuery();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Finish dropping the database.");

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Query the database.");

			dt = conn.GetSchema("Databases");
			found = false;
			foreach (System.Data.DataRow row in dt.Rows)
			{
				int index = 0;
				foreach (System.Data.DataColumn col in dt.Columns)
				{
					if (index == 1)
					{
						if (string.Compare(row[col].ToString(), DB_NAME) == 0)
							found = true;
						AddDatabaseMessage(DatabaseMessage.DBState.None, String.Format("Database : {0}", row[col]));
					}
					index++;
				}
			}
			if (found == false)
				AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "The itester database is dropped.");
			else
			{
				AddDatabaseMessage(DatabaseMessage.DBState.Fail, "Find the itester database.");

				AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Start deleting the sep user.");

				sql = "DELETE FROM mysql.user WHERE User='sep' AND Host='localhost'";
				sqlCmd.CommandText = sql;
				sqlCmd.ExecuteNonQuery();

				AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Start dropping the itester database.");

				sql = "DROP DATABASE itester";
				sqlCmd = new MySqlCommand();
				sqlCmd.Connection = conn;
				sqlCmd.CommandText = sql;
				sqlCmd.CommandType = CommandType.Text;
				sqlCmd.ExecuteNonQuery();

				AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Finish dropping the database.");

				AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Query the database.");

				dt = conn.GetSchema("Databases");
				found = false;
				int count = 0;
				foreach (System.Data.DataRow row in dt.Rows)
				{
					int index = 0;
					foreach (System.Data.DataColumn col in dt.Columns)
					{
						if (index == 1)
						{
							if (string.Compare(row[col].ToString(), DB_NAME) == 0)
								found = true;
							AddDatabaseMessage(DatabaseMessage.DBState.None, String.Format("Database : {0}", row[col]));
						}
						index++;
					}
					if (found)
						break;
					if (count >= 30)
						break;
					count++;
					Thread.Sleep(1000);
				}
				if (found == false)
					AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "The itester database is deleted.");
				else
				{
					AddDatabaseMessage(DatabaseMessage.DBState.Error, "Cannot drop the itester database.");

					conn.Close();

					AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is closed.");

					conn.Dispose();

					AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is disposed.");

					ReadyString = "Warning : Database has error in dropping the itester database.";

					return;
				}
			}

			#endregion

			Dispatcher.Invoke((ThreadStart)delegate()
			{
				_inProjectInit = true;
				cboxProject.Items.Clear();
				cboxSubProject.Items.Clear();
				ProjectOc.Clear();
				SubProjectOc.Clear();
				_inProjectInit = false;
			}, null);
		}

        private void ProjectNew_Button_Click(object sender, RoutedEventArgs e)
        {
            OneLineEditor ole = new OneLineEditor("New Project", "Project Name :", userCompareListTuple: ProjectOc, maxLength: 32);
            if (ole.ShowDialog() != true)
                return;

            InDatabaseTalking = true;
            spbTimer.IsIndeterminate = true;
            ReadyString = "Start creating the new project : " + ole.UserContent;

            Thread th = new Thread(new ParameterizedThreadStart(ProjectNewThread));
            th.Start(new Tuple<string, string>(ole.UserContentDB, ole.UserContent));
        }

		private void ProjectNewThread(object obj)
		{
            Tuple<string, string> t = obj as Tuple<string, string>;

			try
			{
				AddDatabaseMessage(DatabaseMessage.DBState.None, "Begin creating the new project : " + t.Item2);

				DoProjectNew(t);

				AddDatabaseMessage(DatabaseMessage.DBState.None, "End creating the new project : " + t.Item2);
			}
			catch (Exception ex)
			{
				AddDatabaseMessage(DatabaseMessage.DBState.Error, "Cannot create the new project : " + t.Item2);
				AddDatabaseMessage(DatabaseMessage.DBState.Error, "Error message :");
				AddDatabaseMessage(DatabaseMessage.DBState.Error, ex.Message);
			}
			finally
			{
				InDatabaseTalking = false;
				Dispatcher.Invoke((ThreadStart)delegate()
				{
					spbTimer.IsIndeterminate = false;
				}, null);
				ReadyString = "Ready";
			}
		}

		private void DoProjectNew(Tuple<string, string> tp)
		{
			MySqlConnection conn = new MySqlConnection();
			string s = "server=" + DBIPAddress + ";" + "uid=" + DBUsername + ";" + "pwd=" + DBPassword + ";";
			conn.ConnectionString = s;
			conn.Open();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is opened.");

			string sql = "INSERT INTO itester.project(pname,pdname) VALUES('" + tp.Item1 + "','" + tp.Item2 + "')";
			MySqlCommand cmd = new MySqlCommand();
			cmd.Connection = conn;
			cmd.CommandText = sql;
			cmd.ExecuteNonQuery();

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

			sql = "CREATE TABLE itester." + tp.Item1 + "testresult("
				+ "msgidx INT NOT NULL,"
				+ "tcid VARCHAR(128) NOT NULL,"
				+ "state INT NOT NULL DEFAULT 0,"
				+ "stime DATETIME NOT NULL DEFAULT '1000-01-01 00:00:00',"
				+ "msec INT NOT NULL,"
				+ "category VARCHAR(255) NOT NULL DEFAULT '',"
				+ "message VARCHAR(255) NOT NULL DEFAULT '',"
				+ "value VARCHAR(255) NOT NULL DEFAULT '',"
				+ "const VARCHAR(255) NOT NULL DEFAULT '',"
				+ "CONSTRAINT FOREIGN KEY (tcid) REFERENCES testcol(id))";
			//MySqlCommand cmd = new MySqlCommand();
			//cmd.Connection = conn;
			cmd.CommandText = sql;
			cmd.ExecuteNonQuery();

			conn.Close();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is closed.");

			conn.Dispose();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is disposed.");

			Dispatcher.Invoke((ThreadStart)delegate()
			{
				_inProjectInit = true;
				ProjectOc.Add(tp);
                cboxProject.Items.Add(tp.Item2);
				cboxProject.SelectedIndex = ProjectOc.Count - 1;
                cboxSubProject.Items.Clear();
                ObservableCollection<Tuple<string, string>> spoc = new ObservableCollection<Tuple<string, string>>();
                SubProjectOc.Add(new Tuple<string, ObservableCollection<Tuple<string, string>>>(tp.Item1, spoc));
				_inProjectInit = false;
			}, null);
		}

		private void ProjectDelete_Button_Click(object sender, RoutedEventArgs e)
		{
			if (cboxProject.Items.Count < 1)
				return;
			if (cboxProject.SelectedIndex < 0)
				return;
            if (MessageBox.Show("Are you sure to delete the Project : " + ProjectOc[cboxProject.SelectedIndex].Item1 + " ?", "Delete Project", MessageBoxButton.YesNo, MessageBoxImage.Question)
				!= MessageBoxResult.Yes)
				return;

			InDatabaseTalking = true;
			spbTimer.IsIndeterminate = true;
			ReadyString = "Start deleting the project : " + ProjectOc[cboxProject.SelectedIndex].Item2;

			Thread th = new Thread(new ParameterizedThreadStart(DeleteProjectThread));
			th.Start(ProjectOc[cboxProject.SelectedIndex]);
		}

		private void DeleteProjectThread(object obj)
		{
            Tuple<string, string> t = obj as Tuple<string, string>;

			try
			{
				AddDatabaseMessage(DatabaseMessage.DBState.None, "Begin deleting the project : " + t.Item2);

                DoDeleteProject(t);

                AddDatabaseMessage(DatabaseMessage.DBState.None, "End deleting the project : " + t.Item2);
			}
			catch (Exception ex)
			{
                AddDatabaseMessage(DatabaseMessage.DBState.Error, "Cannot delete the project : " + t.Item2);
				AddDatabaseMessage(DatabaseMessage.DBState.Error, "Error message :");
				AddDatabaseMessage(DatabaseMessage.DBState.Error, ex.Message);
			}
			finally
			{
				InDatabaseTalking = false;
				Dispatcher.Invoke((ThreadStart)delegate()
				{
					spbTimer.IsIndeterminate = false;
				}, null);
				ReadyString = "Ready";
			}
		}

		private void DoDeleteProject(Tuple<string, string> tp)
		{
			MySqlConnection conn = new MySqlConnection();
			string s = "server=" + DBIPAddress + ";" + "uid=" + DBUsername + ";" + "pwd=" + DBPassword + ";";
			conn.ConnectionString = s;
			conn.Open();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is opened.");

			AddDatabaseMessage(DatabaseMessage.DBState.None, "Deleting project : " + tp.Item2);

			string sql = "DROP TABLE itester." + tp.Item1 + "testresult";
			MySqlCommand cmd = new MySqlCommand();
			cmd.Connection = conn;
			cmd.CommandText = sql;
			cmd.ExecuteNonQuery();

            sql = "SELECT * FROM itester.subproject WHERE pname='" + tp.Item1 + "'";
			MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
			DataSet ds = new DataSet();
			da.Fill(ds, "subprojct");
			List<string> pspList = new List<string>();
			foreach (DataRow dr in ds.Tables["subproject"].Rows)
			{
				pspList.Add((string)dr["pspname"]);
			}

			List<string> tvidList = new List<string>();
			foreach (string psp in pspList)
			{
                sql = "SELECT * FROM itester.testview WHERE pspname='" + psp + "'";
				da = new MySqlDataAdapter(sql, conn);
				ds = new DataSet();
                da.Fill(ds, "testview");
				foreach (DataRow dr in ds.Tables["testview"].Rows)
				{
					tvidList.Add((string)dr["id"]);
				}
			}

			sql = "DELETE FROM itester.project WHERE pname='" + tp.Item1 + "'";
			cmd.CommandText = sql;
			cmd.ExecuteNonQuery();

            sql = "DELETE FROM itester.subproject WHERE pname='" + tp.Item1 + "'";
			cmd.CommandText = sql;
			cmd.ExecuteNonQuery();

			// testview

			foreach (string item in pspList)
			{
				sql = "DELETE FROM itester.testview WHERE pspname='" + item + "'";
				cmd.CommandText = sql;
				cmd.ExecuteNonQuery();
			}

			// testcol

			foreach (string item in tvidList)
			{
				sql = "DELETE FROM itester.testcol WHERE tvid='" + item + "'";
				cmd.CommandText = sql;
				cmd.ExecuteNonQuery();
			}

            AddDatabaseMessage(DatabaseMessage.DBState.None, "Deleted project : " + tp.Item2);

			conn.Close();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is closed.");

			conn.Dispose();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is disposed.");

			Dispatcher.Invoke((ThreadStart)delegate()
			{
				_inProjectInit = true;
                Tuple<string, string> ti = null;
                foreach (Tuple<string, string> t in ProjectOc)
                {
                    if (t.Item1 == tp.Item1)
                    {
                        ti = t;
                        break;
                    }
                }
				ProjectOc.Remove(ti);
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
                foreach (Tuple<string, string> t in ProjectOc)
                {
                    if (t.Item1 == cboxProject.Items[cboxProject.SelectedIndex].ToString())
                    {
                        ti = t;
                        break;
                    }
                }
                tii = null;
                foreach (Tuple<string, ObservableCollection<Tuple<string, string>>> t in SubProjectOc)
                {
                    if (t.Item1 == ti.Item1)
                    {
                        tii = t;
                        break;
                    }
                }
                cboxSubProject.Items.Clear();
                foreach (Tuple<string, string> t in tii.Item2)
                {
                    cboxSubProject.Items.Add(t.Item2);
                }
                if (cboxSubProject.Items.Count > 0)
					cboxSubProject.SelectedIndex = 0;
				_inProjectInit = false;
			}, null);
		}

		private void ProjectClear_Button_Click(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show("Are you sure to delete all Projects?", "Delete Projects", MessageBoxButton.YesNo, MessageBoxImage.Question)
				!= MessageBoxResult.Yes)
				return;

			InDatabaseTalking = true;
			spbTimer.IsIndeterminate = true;
			ReadyString = "Start deleting all projects.";

			Thread th = new Thread(new ThreadStart(DeleteProjectsThread));
			th.Start();
		}

		private void DeleteProjectsThread()
		{
			try
			{
				AddDatabaseMessage(DatabaseMessage.DBState.None, "Begin deleting all projects.");

				DoDeleteProjects();

				AddDatabaseMessage(DatabaseMessage.DBState.None, "End deleting all projects.");
			}
			catch (Exception ex)
			{
				AddDatabaseMessage(DatabaseMessage.DBState.Error, "Cannot delete all projects.");
				AddDatabaseMessage(DatabaseMessage.DBState.Error, "Error message :");
				AddDatabaseMessage(DatabaseMessage.DBState.Error, ex.Message);
			}
			finally
			{
				InDatabaseTalking = false;
				Dispatcher.Invoke((ThreadStart)delegate()
				{
					spbTimer.IsIndeterminate = false;
				}, null);
				ReadyString = "Ready";
			}
		}

		private void DoDeleteProjects()
		{
			MySqlConnection conn = new MySqlConnection();
			string s = "server=" + DBIPAddress + ";" + "uid=" + DBUsername + ";" + "pwd=" + DBPassword + ";";
			conn.ConnectionString = s;
			conn.Open();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is opened.");

			foreach (Tuple<string, string> t in ProjectOc)
			{
                AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Deleting " + t.Item2 + "...");

				string sql = "DROP TABLE itester." + t.Item1 + "testresult";
				MySqlCommand cmd = new MySqlCommand();
				cmd.Connection = conn;
				cmd.CommandText = sql;
				cmd.ExecuteNonQuery();

                sql = "DELETE FROM itester.project WHERE pname='" + t.Item1 + "'";
				cmd.CommandText = sql;
				cmd.ExecuteNonQuery();

                sql = "DELETE FROM itester.subproject WHERE pname='" + t.Item1 + "'";
				cmd.CommandText = sql;
				cmd.ExecuteNonQuery();

                AddDatabaseMessage(DatabaseMessage.DBState.Infomation, t.Item2 + " is deleted.");
			}

			conn.Close();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is closed.");

			conn.Dispose();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is disposed.");

			Dispatcher.Invoke((ThreadStart)delegate()
			{
				_inProjectInit = true;
                cboxProject.Items.Clear();
                cboxSubProject.Items.Clear();
				ProjectOc.Clear();
				SubProjectOc.Clear();
				_inProjectInit = false;
			}, null);
		}

		private void SubProjectNew_Button_Click(object sender, RoutedEventArgs e)
		{
            if (cboxProject.Items.Count < 1 || cboxProject.SelectedIndex < 0)
            {
                MessageBox.Show("No selected project.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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
            
            OneLineEditor ole = null;
            if(tii != null)
                ole = new OneLineEditor("New Sub Project", "Sub Project Name :", userCompareListTuple: tii.Item2, maxLength: 32);
            else
                ole = new OneLineEditor("New Sub Project", "Sub Project Name :", maxLength: 32);
            if (ole.ShowDialog() != true)
                return;

            InDatabaseTalking = true;
            spbTimer.IsIndeterminate = true;
            ReadyString = "Start creating the sub project : " + ole.UserContent;

            Thread th = new Thread(new ParameterizedThreadStart(SubProjectNewThread));
            th.Start(new Tuple<string, string, string, string>(ti.Item1, ti.Item2, ole.UserContentDB, ole.UserContent));
		}

		private void SubProjectNewThread(object obj)
		{
            Tuple<string, string, string, string> t = obj as Tuple<string, string, string, string>;

			try
			{
				AddDatabaseMessage(DatabaseMessage.DBState.None, "Begin creating the new sub project : " + t.Item4);

				DoSubProjectNew(t);

                AddDatabaseMessage(DatabaseMessage.DBState.None, "End creating the new sub project : " + t.Item4);
			}
			catch (Exception ex)
			{
                AddDatabaseMessage(DatabaseMessage.DBState.Error, "Cannot create the new sub project : " + t.Item4);
				AddDatabaseMessage(DatabaseMessage.DBState.Error, "Error message :");
				AddDatabaseMessage(DatabaseMessage.DBState.Error, ex.Message);
			}
			finally
			{
				InDatabaseTalking = false;
				Dispatcher.Invoke((ThreadStart)delegate()
				{
					spbTimer.IsIndeterminate = false;
				}, null);
				ReadyString = "Ready";
			}
		}

        private void DoSubProjectNew(Tuple<string, string, string, string> tsp)
		{
			MySqlConnection conn = new MySqlConnection();
			string s = "server=" + DBIPAddress + ";" + "uid=" + DBUsername + ";" + "pwd=" + DBPassword + ";";
			conn.ConnectionString = s;
			conn.Open();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is opened.");

			// Sub Project
			//
			// Project & Sub Project : pspname : varchar(64) // pname+spname
			// Project Name : pname : varchar(32)
			// Sub Project Name : spname : varchar(32)

            string sql = "INSERT INTO itester.subproject(pspname,pname,spname,spdname) VALUES('" + tsp.Item1 + tsp.Item3 + "','" + tsp.Item1 + "','" + tsp.Item3 + "','" + tsp.Item4 + "')";
			MySqlCommand cmd = new MySqlCommand();
			cmd.Connection = conn;
			cmd.CommandText = sql;
			cmd.ExecuteNonQuery();

			conn.Close();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is closed.");

			conn.Dispose();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is disposed.");

			Dispatcher.Invoke((ThreadStart)delegate()
			{
                _inProjectInit = true;
                cboxSubProject.Items.Add(tsp.Item4);
                cboxSubProject.SelectedIndex = cboxSubProject.Items.Count - 1;
				Tuple<string, string> ti = null;
				foreach (Tuple<string, string> t in ProjectOc)
				{
					if (t.Item2 == tsp.Item2)
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
				tii.Item2.Add(new Tuple<string, string>(tsp.Item3, tsp.Item4));
				_inProjectInit = false;
			}, null);
		}

		private void SubProjectDelete_Button_Click(object sender, RoutedEventArgs e)
		{
			if (cboxSubProject.Items.Count < 1 || cboxSubProject.SelectedIndex < 0)
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

            if (MessageBox.Show("Are you sure to delete the Sub Project : " + tii.Item2[cboxSubProject.SelectedIndex].Item2 + " ?", "Delete Project", MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes)
                return;

            InDatabaseTalking = true;
            spbTimer.IsIndeterminate = true;
            ReadyString = "Start deleting the Sub Project : " + tii.Item2[cboxSubProject.SelectedIndex].Item2;

            Thread th = new Thread(new ParameterizedThreadStart(DeletSubProjectThread));
            th.Start(new Tuple<string, string, string, string>(ti.Item1, ti.Item2, tii.Item2[cboxSubProject.SelectedIndex].Item1, tii.Item2[cboxSubProject.SelectedIndex].Item2));
        }

		private void DeletSubProjectThread(object obj)
		{
            Tuple<string, string, string, string> t = obj as Tuple<string, string, string, string>;
			try
			{
				AddDatabaseMessage(DatabaseMessage.DBState.None, "Begin deleting the sub project : " + t.Item4);

				DoSubProjectDelete(t);

                AddDatabaseMessage(DatabaseMessage.DBState.None, "End deleting the sub project : " + t.Item4);
			}
			catch (Exception ex)
			{
                AddDatabaseMessage(DatabaseMessage.DBState.Error, "Cannot delete the sub project : " + t.Item4);
				AddDatabaseMessage(DatabaseMessage.DBState.Error, "Error message :");
				AddDatabaseMessage(DatabaseMessage.DBState.Error, ex.Message);
			}
			finally
			{
				InDatabaseTalking = false;
				Dispatcher.Invoke((ThreadStart)delegate()
				{
					spbTimer.IsIndeterminate = false;
				}, null);
				ReadyString = "Ready";
			}
		}

        private void DoSubProjectDelete(Tuple<string, string, string, string> pspt)
		{
			MySqlConnection conn = new MySqlConnection();
			string s = "server=" + DBIPAddress + ";" + "uid=" + DBUsername + ";" + "pwd=" + DBPassword + ";";
			conn.ConnectionString = s;
			conn.Open();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is opened.");

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Deleting sub project : " + pspt.Item4);

            string sql = "";

            List<string> tvidList = new List<string>();
            sql = "SELECT * FROM itester.testview WHERE pspname='" + pspt.Item1 + pspt.Item3 + "'";
            MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
            DataSet ds = new DataSet();
            da.Fill(ds, "testview");
            foreach (DataRow dr in ds.Tables["testview"].Rows)
            {
                tvidList.Add((string)dr["id"]);
            }

			sql = "DELETE FROM itester.subproject WHERE pname='" + pspt.Item1 + "' AND spname='" + pspt.Item3 + "'";
			MySqlCommand cmd = new MySqlCommand();
			cmd.Connection = conn;
			cmd.CommandText = sql;
			cmd.ExecuteNonQuery();

			sql = "DELETE FROM itester." + pspt.Item1 + "testresult WHERE pname='" + pspt.Item1 + "' AND spname='" + pspt.Item3 + "'";
			cmd.CommandText = sql;
			cmd.ExecuteNonQuery();

            // testview

            sql = "DELETE FROM itester.testview WHERE pspname='" + pspt.Item1 + pspt.Item3 + "'";
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();

            // testcol

            foreach (string item in tvidList)
            {
                sql = "DELETE FROM itester.testcol WHERE tvid='" + item + "'";
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Deleted sub project : " + pspt.Item4);
	
			conn.Close();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is closed.");

			conn.Dispose();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is disposed.");

			Dispatcher.Invoke((ThreadStart)delegate()
			{
				Tuple<string, ObservableCollection<Tuple<string, string>>> ti = null;
                foreach (Tuple<string, ObservableCollection<Tuple<string, string>>> t in SubProjectOc)
				{
					if (t.Item1 == pspt.Item1)
					{
						ti = t;
						break;
					}
				}
                Tuple<string, string> tii = null;
                foreach (Tuple<string, string> t in ti.Item2)
                {
                    if (t.Item1 == pspt.Item3)
                    {
                        tii = t;
                        break;
                    }
                }
                ti.Item2.Remove(tii);
                _inProjectInit = true;
                cboxSubProject.Items.Remove(tii.Item2);
                _inProjectInit = false;
			}, null);
		}

		private void SubProjectClear_Button_Click(object sender, RoutedEventArgs e)
		{
			if (cboxProject.Items.Count < 1 || cboxProject.SelectedIndex < 0)
				return;
			if (MessageBox.Show("Are you sure to delete all Sub Projects in Project \""+ProjectOc[cboxProject.SelectedIndex] + "\"?",
				"Delete Sub Projects", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
				return;

			InDatabaseTalking = true;
			spbTimer.IsIndeterminate = true;
			ReadyString = "Start deleting all Sub Project in Project \"" + ProjectOc[cboxProject.SelectedIndex].Item2;

			Thread th = new Thread(new ParameterizedThreadStart(SubProjectsDeleteThread));
			th.Start(ProjectOc[cboxProject.SelectedIndex]);
		}

		private void SubProjectsDeleteThread(object obj)
		{
            Tuple<string, string> t = obj as Tuple<string, string>;
			try
			{
				AddDatabaseMessage(DatabaseMessage.DBState.None, "Begin deleting all Sub Project in Project \"" + t.Item2 + "\"");

				DoSubProjectsDelete(t);

                AddDatabaseMessage(DatabaseMessage.DBState.None, "End deleting all Sub Project \"" + t.Item2 + "\"");
			}
			catch (Exception ex)
			{
                AddDatabaseMessage(DatabaseMessage.DBState.Error, "Cannot delete all Sub Project \"" + t.Item2 + "\"");
				AddDatabaseMessage(DatabaseMessage.DBState.Error, "Error message :");
				AddDatabaseMessage(DatabaseMessage.DBState.Error, ex.Message);
			}
			finally
			{
				InDatabaseTalking = false;
				Dispatcher.Invoke((ThreadStart)delegate()
				{
					spbTimer.IsIndeterminate = false;
				}, null);
				ReadyString = "Ready";
			}
		}

        private void DoSubProjectsDelete(Tuple<string, string> tp)
		{
			MySqlConnection conn = new MySqlConnection();
			string s = "server=" + DBIPAddress + ";" + "uid=" + DBUsername + ";" + "pwd=" + DBPassword + ";";
			conn.ConnectionString = s;
			conn.Open();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is opened.");

            AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Deleting all Sub Project in Project \"" + tp.Item2 + "\"");

            string sql = "SELECT * FROM itester.subproject WHERE pname='" + tp.Item1 + "'";
            MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
            DataSet ds = new DataSet();
            da.Fill(ds, "subprojct");
            List<string> pspList = new List<string>();
            foreach (DataRow dr in ds.Tables["subproject"].Rows)
            {
                pspList.Add((string)dr["pspname"]);
            }

            List<string> tvidList = new List<string>();
            foreach (string psp in pspList)
            {
                sql = "SELECT * FROM itester.testview WHERE pspname='" + psp + "'";
                da = new MySqlDataAdapter(sql, conn);
                ds = new DataSet();
                da.Fill(ds, "testview");
                foreach (DataRow dr in ds.Tables["testview"].Rows)
                {
                    tvidList.Add((string)dr["id"]);
                }
            }

            sql = "DELETE FROM itester.subproject WHERE pname='" + tp.Item1 + "'";
			MySqlCommand cmd = new MySqlCommand();
			cmd.Connection = conn;
			cmd.CommandText = sql;
			cmd.ExecuteNonQuery();

            // testview

            foreach (string item in pspList)
            {
                sql = "DELETE FROM itester.testview WHERE pspname='" + item + "'";
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }

            // testcol

            foreach (string item in tvidList)
            {
                sql = "DELETE FROM itester.testcol WHERE tvid='" + item + "'";
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }

            AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Deleted all Sub Project in Project " + tp.Item2);

			conn.Close();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is closed.");

			conn.Dispose();

			AddDatabaseMessage(DatabaseMessage.DBState.Infomation, "Database is disposed.");

			Dispatcher.Invoke((ThreadStart)delegate()
			{
                cboxSubProject.Items.Clear(); 
                Tuple<string, ObservableCollection<Tuple<string, string>>> ti = null;
                foreach (Tuple<string, ObservableCollection<Tuple<string, string>>> t in SubProjectOc)
				{
                    if (t.Item1 == tp.Item1)
					{
						ti = t;
						break;
					}
				}
                ti.Item2.Clear();
			}, null);
		}

		private void Project_ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_inProjectInit)
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

            cboxSubProject.Items.Clear();
            if (tii != null && tii.Item2.Count > 0)
            {
                foreach (Tuple<string, string> t in tii.Item2)
                {
                    cboxSubProject.Items.Add(t.Item2);
                }
                cboxSubProject.SelectedIndex = 0;
            }
        }
    }

    public class DatabaseMessage : iTestBase.INotifyPropertyChangedClass
    {
        public enum DBState
        {
            None,
            Infomation,
            OK,
            Fail,
            Error
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
                        case DBState.Infomation:
							_dbStateImage.UriSource = new Uri("pack://application:,,,/itesterdbsetup;component/resources/status_info.png");
                            break;
                        case DBState.OK:
							_dbStateImage.UriSource = new Uri("pack://application:,,,/itesterdbsetup;component/resources/status_ok.png");
                            break;
                        case DBState.Fail:
							_dbStateImage.UriSource = new Uri("pack://application:,,,/itesterdbsetup;component/resources/status_error.png");
                            break;
                        case DBState.Error:
							_dbStateImage.UriSource = new Uri("pack://application:,,,/itesterdbsetup;component/resources/status_ques.ico");
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
					case DBState.Infomation:
						_dbStateBackgound = new SolidColorBrush(Colors.LightBlue);
						break;
					case DBState.OK:
						_dbStateBackgound = new SolidColorBrush(Colors.LightGreen);
						break;
					case DBState.Fail:
						_dbStateBackgound = new SolidColorBrush(Colors.Red);
						break;
					case DBState.Error:
                        _dbStateBackgound = new SolidColorBrush(Colors.Yellow);
						break;
				}
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
					case DBState.Infomation:
						return "resources/status_info.png";
					case DBState.OK:
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

        private string _msgDateTime = "";
        public string MsgDateTime
       {
            get
            {
                return _msgDateTime;
            }
            set
            {
                _msgDateTime = value;
                NotifyPropertyChanged("MsgDateTime");
            }
        }

        private string _msg = "";
        public string Message
        {
            get
            {
                return _msg;
            }
            set
            {
                _msg = value;
                NotifyPropertyChanged("Message");
            }
        }
    }

	[ValueConversion(typeof(DatabaseMessage.DBState), typeof(BitmapImage))]
	public class DBStateImageConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter,
		CultureInfo culture)
		{
			if (value == null)
				return null;

			DatabaseMessage.DBState dbs = DatabaseMessage.DBState.None;
			try
			{
				dbs = (DatabaseMessage.DBState)value;
			}
			catch (Exception)
			{
				dbs = DatabaseMessage.DBState.None;
			}

			BitmapImage dbi = new BitmapImage();
			dbi.BeginInit();
			switch (dbs)
			{
				default:
				case DatabaseMessage.DBState.None:
					break;
				case DatabaseMessage.DBState.Infomation:
					dbi.UriSource = new Uri("pack://application:,,,/itesterdbsetup;component/resources/status_info.png");
					break;
				case DatabaseMessage.DBState.OK:
					dbi.UriSource = new Uri("pack://application:,,,/itesterdbsetup;component/resources/status_ok.png");
					break;
				case DatabaseMessage.DBState.Fail:
					dbi.UriSource = new Uri("pack://application:,,,/itesterdbsetup;component/resources/status_error.png");
					break;
				case DatabaseMessage.DBState.Error:
					dbi.UriSource = new Uri("pack://application:,,,/itesterdbsetup;component/resources/status_ques.ico");
					break;
			}
			dbi.EndInit();

			return dbi;
		}

		public object ConvertBack(object value, Type targetTypes, object parameter,
		CultureInfo culture)
		{
			throw new Exception("The method or operation is not implemented.");
		}
	}
}
