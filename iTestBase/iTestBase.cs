using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Xml.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;

namespace iTestBase
{
    public class INotifyPropertyChangedClass : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TestCollection : INotifyPropertyChangedClass
    {
        private ObservableCollection<TestGroupCase> _testGroupCaseOcDisp;
        public ObservableCollection<TestGroupCase> TestGroupCaseOcDisp
        {
            get
            {
                return _testGroupCaseOcDisp;
            }
            set
            {
                _testGroupCaseOcDisp = value;
            }
        }

        private ObservableCollection<TestGroupCase> _testGroupCaseOc = new ObservableCollection<TestGroupCase>();
        public ObservableCollection<TestGroupCase> TestGroupCaseOc
        {
            get
            {
                return _testGroupCaseOc;
            }
            set
            {
                _testGroupCaseOc = value;
            }
        }

        public TestCollection(ObservableCollection<TestGroupCase> testGroupCaseOcDisp)
        {
            _testGroupCaseOcDisp = testGroupCaseOcDisp;
        }

        public void DeleteTestGroupCase(TestGroupCase tgc)
        {
            if (tgc == null)
                return;
            if (tgc.TestGroupCaseParent != null)
                tgc.TestGroupCaseParent.TestGroupCaseOc.Remove(tgc);
            else
                TestGroupCaseOc.Remove(tgc);

            TestGroupCaseOcDisp.Remove(tgc);

            Stack<TestGroupCase> tcStack = new Stack<TestGroupCase>();

            foreach (TestGroupCase tgci in tgc.TestGroupCaseOc)
            {
                tcStack.Push(tgci);
            }

            for (; ; )
            {
                if (tcStack.Count < 1)
                    return;

                TestGroupCase tgci = tcStack.Pop();
                TestGroupCaseOcDisp.Remove(tgci);
                if (tgci.IsCase == false)
                {
                    foreach (TestGroupCase tgct in tgci.TestGroupCaseOc)
                    {
                        tcStack.Push(tgct);
                    }
                }
            }
        }

        public void UpdateParentChildSelectedState(TestGroupCase tgc)
        {
            UpdateChildSelectedState(tgc);
            UpdateParentSelectedState(tgc);
        }

        /// <summary>
        /// 
        /// </summary>
        public void UpdateChildSelectedState(TestGroupCase tgc)
        {
            if (tgc.IsCase == true)
                return;

            Stack<TestGroupCase> tcStack = new Stack<TestGroupCase>();

            foreach (TestGroupCase tgci in tgc.TestGroupCaseOc)
            {
                tcStack.Push(tgci);
            }

            for (; ; )
            {
                if (tcStack.Count < 1)
                    return;

                TestGroupCase tgci = tcStack.Pop();
                tgci.SelectedState = tgc.SelectedState;
                tgci.SelectedStateBackColor = new SolidColorBrush(Colors.Transparent);
                if (tgci.IsCase == false)
                {
                    foreach (TestGroupCase tgct in tgci.TestGroupCaseOc)
                    {
                        tcStack.Push(tgct);
                    }
                }
            }
        }

        public void UpdateParentSelectedState(TestGroupCase tgc)
        {
            bool hasChecked = false;
            bool hasUnchecked = false;

            TestGroupCase tgcp = tgc.TestGroupCaseParent;
            while (tgcp != null)
            {
                foreach (TestGroupCase tgci in tgcp.TestGroupCaseOc)
                {
                    if (tgci.SelectedState == true)
                        hasChecked = true;
                    else if (tgci.SelectedState == false)
                        hasUnchecked = true;
                }

                if (hasChecked && hasUnchecked)
                    tgcp.SelectedStateBackColor = new SolidColorBrush(Colors.DarkGray);
                else
                    tgcp.SelectedStateBackColor = new SolidColorBrush(Colors.Transparent);

                if (hasChecked)
                    tgcp.SelectedState = true;
                else
                    tgcp.SelectedState = false;

                tgcp = tgcp.TestGroupCaseParent;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tgc"></param>
        /// <returns></returns>
        public int GetGroupCaseIndex(TestGroupCase tgc)
        {
            for (int i = 0; i < TestGroupCaseOcDisp.Count; i++)
            {
                if (TestGroupCaseOcDisp[i] == tgc)
                    return i;
            }

            return -1;
        }

        public static long _globalIndex = 0;

        public static long GetGlobalIndex()
        {
            return _globalIndex++;
        }

        public int FindLastSubGroupCaseDisplayIndex(TestGroupCase tgc)
        {
            if (tgc == null)
                return -1;

            if (tgc.IsCase == true)
                return FindTestGroupCaseDisplayIndex(tgc);

            if (tgc.TestGroupCaseOc.Count < 1)
                return FindTestGroupCaseDisplayIndex(tgc);

            TestGroupCase tgcp = tgc.TestGroupCaseOc[tgc.TestGroupCaseOc.Count - 1];
            while (tgcp.IsCase == false && tgcp.TestGroupCaseOc.Count > 0)
            {
                tgcp = tgcp.TestGroupCaseOc[tgcp.TestGroupCaseOc.Count - 1];
            }
            return FindTestGroupCaseDisplayIndex(tgcp);
        }

        public int FindTestGroupCaseDisplayIndex(TestGroupCase tgc)
        {
            if (tgc == null)
                return -1;

            for (int i = 0; i < TestGroupCaseOcDisp.Count; i++)
            {
                if (TestGroupCaseOcDisp[i] == tgc)
                    return i;
            }

            return -1;
        }

        public int FindTestGroupCaseIndex(TestGroupCase tgc)
        {
            if (tgc == null)
                return -1;

            if (tgc.TestGroupCaseParent == null)
            {
                for (int i = 0; i < TestGroupCaseOc.Count; i++)
                {
                    if (TestGroupCaseOc[i] == tgc)
                        return i;
                }
            }
            else
            {
                for (int i = 0; i < tgc.TestGroupCaseParent.TestGroupCaseOc.Count; i++)
                {
                    if (tgc.TestGroupCaseParent.TestGroupCaseOc[i] == tgc)
                        return i;
                }
            }

            return -1;
        }

        public void ConfigTestGroupCaseFromXML(TestGroupCase tgc, XElement xeCofig)
        {
            iTestBase tb = tgc.TestBaseInstance;
            Type t = tb.GetType();
            PropertyInfo[] pia = t.GetProperties();
            //FieldInfo[] fia = t.GetFields();
            foreach (XElement xe in xeCofig.Elements("item"))
            {
                string sn = CommonOperations.ConvertXmlString2FileString(xe.Attribute("name").Value);
                string sv = CommonOperations.ConvertXmlString2FileString(xe.Attribute("value").Value);
                foreach (PropertyInfo pi in pia)
                {
                    string pt = pi.PropertyType.Name;
                    string ptb = pi.PropertyType.BaseType.Name;
                    if (string.Compare(ptb, "Enum") != 0
                        && !pi.PropertyType.IsValueType
                        && string.Compare(pt, "string") != 0)
                        break;

                    if (string.Compare(pi.Name, sn, true) == 0)
                    {
                        try
                        {
                            if (string.Compare(pt, "SByte") == 0)
                            {
                                pi.SetValue(tb, sbyte.Parse(sv), null);
                            }
                            else if (string.Compare(pt, "Byte") == 0)
                            {
                                pi.SetValue(tb, byte.Parse(sv), null);
                            }
                            else if (string.Compare(pt, "Char") == 0)
                            {
                                pi.SetValue(tb, char.Parse(sv), null);
                            }
                            else if (string.Compare(pt, "Int16") == 0)
                            {
                                pi.SetValue(tb, Int16.Parse(sv), null);
                            }
                            else if (string.Compare(pt, "UInt16") == 0)
                            {
                                pi.SetValue(tb, UInt16.Parse(sv), null);
                            }
                            else if (string.Compare(pt, "Int32") == 0)
                            {
                                pi.SetValue(tb, Int32.Parse(sv), null);
                            }
                            else if (string.Compare(pt, "UInt32") == 0)
                            {
                                pi.SetValue(tb, UInt32.Parse(sv), null);
                            }
                            else if (string.Compare(pt, "Int64") == 0)
                            {
                                pi.SetValue(tb, Int64.Parse(sv), null);
                            }
                            else if (string.Compare(pt, "UInt64") == 0)
                            {
                                pi.SetValue(tb, UInt64.Parse(sv), null);
                            }
                            else if (string.Compare(pt, "Decimal") == 0)
                            {
                                pi.SetValue(tb, decimal.Parse(sv), null);
                            }
                            else if (string.Compare(pt, "float") == 0)
                            {
                                pi.SetValue(tb, float.Parse(sv), null);
                            }
                            else if (string.Compare(pt, "double") == 0)
                            {
                                pi.SetValue(tb, double.Parse(sv), null);
                            }
                            else if (string.Compare(pt, "Boolean") == 0)
                            {
                                pi.SetValue(tb, bool.Parse(sv), null);
                            }
                            else if (string.Compare(ptb, "Enum") == 0)
                            {
                                pi.SetValue(tb, Enum.Parse(pi.PropertyType, sv), null);
                            }
                            else if (string.Compare(pt, "String") == 0)
                            {
                                pi.SetValue(tb, sv, null);
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Cannot parse \"" + sv + "\" for \" " + sn + "\".\nError message :\n" + ex.Message, "Parse Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                //foreach (FieldInfo fi in fia)
                //{
                //    string pt = fi.FieldType.ToString();
                //    if (!fi.FieldType.IsValueType && string.Compare(pt, "string", true) != 0)
                //        break;

                //    if (string.Compare(fi.Name, sn, true) == 0)
                //    {
                //    }
                //}
            }
        }
    }

    public class TestGroupCase : INotifyPropertyChangedClass
    {
        public const int LISTVIEW_IMAGE_SIZE = 18;

        #region Properties

        #region Test Related

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
		
		private bool _alreadyRun = false;
        public bool AlreadyRun
        {
            get
            {
                return _alreadyRun;
            }
            set
            {
                _alreadyRun = value;
				NotifyPropertyChanged("AlreadyRun");
			}
        }

        private TestGroupCase _testGroupCaseParent = null;
        public TestGroupCase TestGroupCaseParent
        {
            get
            {
                return _testGroupCaseParent;
            }
            set
            {
                _testGroupCaseParent = value;
            }
        }

        private iTestBase _testBaseInstace = null;
        public iTestBase TestBaseInstance
        {
            get
            {
                return _testBaseInstace;
            }
            set
            {
                _testBaseInstace = value;
            }
        }

        private TestCollection _testColInstance = null;
        public TestCollection TestColInstance
        {
            get
            {
                return _testColInstance;
            }
            set
            {
                _testColInstance = value;
            }
        }

        private XElement _testGroupCaseXE = null;
        public XElement TestGroupCaseXE
        {
            get
            {
                return _testGroupCaseXE;
            }
            set
            {
                _testGroupCaseXE = value;
            }
        }

        #region Case Necessary

        private string _groupCaseName = "";
        public string GroupCaseName
        {
            get
            {
                return _groupCaseName;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    _groupCaseName = "No Name";
                else
                    _groupCaseName = value;
                NotifyPropertyChanged("GroupCaseName");
            }
        }

        private SolidColorBrush _passBackground = new SolidColorBrush(Colors.Transparent);
        public SolidColorBrush PassBackground
        {
            get
            {
                return _passBackground;
            }
            set
            {
                _passBackground = value;
                NotifyPropertyChanged("PassBackground");
            }
        }

        private BitmapImage _passImage = null;
        public BitmapImage PassImage
        {
            get
            {
                return _passImage;
            }
            set
            {
                _passImage = value;
                NotifyPropertyChanged("PassImage");
            }
        }

        private int _passCount = 0;
        public int PassCount
        {
            get
            {
                return _passCount;
            }
            set
            {
                if (value < 0)
                    _passCount = 0;
                else
                    _passCount = value;
                int sum = _passCount + _failCount + _errorCount;
                if (sum != 0.0)
                    PassRate = 100.0 * _passCount / sum;
                else
                    PassRate = 0.0;
                NotifyPropertyChanged("PassCount");
                NotifyPropertyChanged("PassCountString");
                if (IsCase == false)
                {
                    if (_passCount > 0)
                        _passBackground = new SolidColorBrush(Colors.Green);
                    else
                        _passBackground = new SolidColorBrush(Colors.Transparent);
                    NotifyPropertyChanged("PassBackground");
                }
            }
        }

        public string PassCountString
        {
            get
            {
                if (IsCase)
                    return "";
                else
                    return _passCount.ToString();
            }
        }

        private SolidColorBrush _failBackground = new SolidColorBrush(Colors.Transparent);
        public SolidColorBrush FailBackground
        {
            get
            {
                return _failBackground;
            }
            set
            {
                _failBackground = value;
                NotifyPropertyChanged("FailBackground");
            }
        }

        private BitmapImage _failImage = null;
        public BitmapImage FailImage
        {
            get
            {
                //if (IsCase == true && FailCount > 0)
                return _failImage;
                //else
                //    return null;
            }
            set
            {
                _failImage = value;
                NotifyPropertyChanged("FailImage");
            }
        }

        private int _failCount = 0;
        public int FailCount
        {
            get
            {
                return _failCount;
            }
            set
            {
                if (value < 0)
                    _failCount = 0;
                else
                    _failCount = value;
                int sum = _passCount + _failCount + _errorCount;
                if (sum != 0.0)
                    PassRate = 100.0 * _passCount / sum;
                else
                    PassRate = 0.0;
                NotifyPropertyChanged("FailCount");
                NotifyPropertyChanged("FailCountString");
                //NotifyPropertyChanged("PassImage");
                if (IsCase == false)
                {
                    if (_failCount > 0)
                        _failBackground = new SolidColorBrush(Colors.Red);
                    else
                        _failBackground = new SolidColorBrush(Colors.Transparent);
                    NotifyPropertyChanged("FailBackground");
                }
            }
        }

        public string FailCountString
        {
            get
            {
                if (IsCase)
                    return "";
                else
                    return _failCount.ToString();
            }
        }

        private SolidColorBrush _errorBackground = new SolidColorBrush(Colors.Transparent);
        public SolidColorBrush ErrorBackground
        {
            get
            {
                return _errorBackground;
            }
            set
            {
                _errorBackground = value;
                NotifyPropertyChanged("ErrorBackground");
            }
        }

        private BitmapImage _errorImage = null;
        public BitmapImage ErrorImage
        {
            get
            {
                //if (IsCase == true && ErrorCount > 0)
                return _errorImage;
                //else
                //    return null;
            }
            set
            {
                _errorImage = value;
                NotifyPropertyChanged("ErrorImage");
            }
        }

        private int _errorCount = 0;
        public int ErrorCount
        {
            get
            {
                return _errorCount;
            }
            set
            {
                if (value < 0)
                    _errorCount = 0;
                else
                    _errorCount = value;
                int sum = _passCount + _failCount + _errorCount;
                if (sum != 0.0)
                    PassRate = 100.0 * _passCount / sum;
                else
                    PassRate = 0.0;
                NotifyPropertyChanged("ErrorCount");
                NotifyPropertyChanged("ErrorCountString");
                if (IsCase == false)
                {
                    if (_errorCount > 0)
                        _errorBackground = new SolidColorBrush(Colors.Yellow);
                    else
                        _errorBackground = new SolidColorBrush(Colors.Transparent);
                    NotifyPropertyChanged("ErrorBackground");
                }
            }
        }

        public string ErrorCountString
        {
            get
            {
                if (IsCase)
                    return "";
                else
                    return _errorCount.ToString();
            }
        }

        private int _totalTests = 0;
        public int TotalTests
        {
            get
            {
                return _totalTests;
            }
            set
            {
                if (value < 0)
                    _totalTests = 0;
                else
                    _totalTests = value;
                NotifyPropertyChanged("TotalTests");
                NotifyPropertyChanged("TotalTestsString");
            }
        }

        public string TotalTestsString
        {
            get
            {
                if (IsCase)
                    return "";
                else
                    return _totalTests.ToString();
            }
        }

        private double _passRate = 0.0;
        public double PassRate
        {
            get
            {
                if (IsCase)
                    return 0.0;
                else
                    return _passRate;
            }
            set
            {
                if (value < 0.0)
                    _passRate = 0.0;
                else if (value > 100.0)
                    _passRate = 100.0;
                else
                    _passRate = value;
                NotifyPropertyChanged("PassRate");
                NotifyPropertyChanged("PassRateString");
            }
        }

        public string PassRateString
        {
            get
            {
                if (IsCase)
                    return "";
                else
                    return _passRate.ToString("F2") + "%";
            }
        }

        private TimeSpan _duration = new TimeSpan(0, 0, 0, 0, 0);
        public TimeSpan Duration
        {
            get
            {
                return _duration;
            }
            set
            {
                _duration = value;
                NotifyPropertyChanged("Duration");
                NotifyPropertyChanged("DurationString");
            }
        }

        public string DurationString
        {
            get
            {
                int intMs = (int)(_duration.Milliseconds / 100.0);
                return _duration.Days.ToString() + "." + _duration.Hours.ToString("D2") + ":"
                    + _duration.Minutes.ToString("D2") + ":" + _duration.Seconds.ToString("D2") + "."
                    + intMs.ToString("D1");
            }
        }

        private int _runCount = 1;
        public int RunCount
        {
            get
            {
                return _runCount;
            }
            set
            {
                if (value < 1)
                    _runCount = 1;
                else
                    _runCount = value;
                NotifyPropertyChanged("RunCount");
                NotifyPropertyChanged("RunCountString");
            }
        }

        public string RunCountString
        {
            get
            {
                if (IsCase)
                    return _runCount.ToString();
                else
                    return "";
            }
        }

        private string _testClass = "";
        public string TestClass
        {
            get
            {
                if (IsCase)
                    return _testClass;
                else
                    return "";
            }
            set
            {
                _testClass = value;
                NotifyPropertyChanged("TestClass");
            }
        }

        private string _testFilePath = "";
        public string TestFilePath
        {
            get
            {
                return _testFilePath;
            }
            set
            {
                _testFilePath = value;
                NotifyPropertyChanged("TestFilePath");
            }
        }

        public enum TestPriorityType
        {
            High,
            Medium,
            Low
        }

        private TestPriorityType _testPriority = TestPriorityType.High;
        public TestPriorityType TestPriority
        {
            get
            {
                return _testPriority;
            }
            set
            {
                _testPriority = value;
                NotifyPropertyChanged("TestPriority");
                NotifyPropertyChanged("TestPriorityString");
            }
        }

        public string TestPriorityString
        {
            get
            {
                return _testPriority.ToString();
            }
        }

        #endregion

        #region Group Necessary

        private ObservableCollection<TestGroupCase> _testGroupCaseOc = new ObservableCollection<TestGroupCase>();
        public ObservableCollection<TestGroupCase> TestGroupCaseOc
        {
            get
            {
                return _testGroupCaseOc;
            }
            set
            {
                _testGroupCaseOc = value;
            }
        }

        //public int GroupCount
        //{
        //    get
        //    {
        //        int count = 0;
        //        foreach (TestGroupCase tgc in TestGroupCaseOc)
        //        {
        //            if (tgc.IsCase == false)
        //                count++;
        //        }
        //        return count;
        //    }
        //}

        //public int AllCount
        //{
        //    get
        //    {
        //        int count = 0;
        //        foreach (TestGroupCase tgc in TestGroupCaseOc)
        //        {
        //            if (tgc.IsCase == false)
        //                count = count + tgc.AllCount;
        //        }
        //        return count + DirectCount;
        //    }
        //}

        //public int DirectCount
        //{
        //    get
        //    {
        //        int count = 0;
        //        foreach (TestGroupCase tgc in TestGroupCaseOc)
        //        {
        //            if (tgc.IsCase == true)
        //                count++;
        //        }
        //        return count;
        //    }
        //}

        #endregion

        #endregion

        #region Display Related

        private bool _testGroupCaseIsVisible = true;
        public bool TestGroupCaseIsVisible
        {
            get
            {
                return _testGroupCaseIsVisible;
            }
            set
            {
                _testGroupCaseIsVisible = value;
                NotifyPropertyChanged("TestGroupCaseIsVisible");
            }
        }

        private bool _isCase = true;
        public bool IsCase
        {
            get
            {
                return _isCase;
            }
            set
            {
                _isCase = value;
                NotifyPropertyChanged("IsCase");
            }
        }

        private bool _notRun = true;
        public bool NotRun
        {
            get
            {
                return _notRun;
            }
            set
            {
                _notRun = value;
                NotifyPropertyChanged("NotRun");
                NotifyPropertyChanged("PassCountString");
                NotifyPropertyChanged("FailCountString");
                NotifyPropertyChanged("ErrorCountString");
                NotifyPropertyChanged("TotalTestsString");
                NotifyPropertyChanged("PassRateString");
            }
        }

        private int _displayLevel = 0;
        public int DisplayLevel
        {
            get
            {
                return _displayLevel;
            }
            set
            {
                if (value < 0)
                    _displayLevel = 0;
                else
                    _displayLevel = value;
                NotifyPropertyChanged("DisplayLevel");
                NotifyPropertyChanged("DisplayLevelLength");
            }
        }

        public int DisplayLevelLength
        {
            get
            {
                return LISTVIEW_IMAGE_SIZE * (DisplayLevel + 1);
            }
        }

        private bool _selState = true;
        public bool SelectedState
        {
            get
            {
                return _selState;
            }
            set
            {
                _selState = value;
                NotifyPropertyChanged("SelectedState");
                //UpdateChildSelectedState();
            }
        }

        private SolidColorBrush _selectedStateBackColor = new SolidColorBrush(Colors.Transparent);
        public SolidColorBrush SelectedStateBackColor
        {
            get
            {
                return _selectedStateBackColor;
            }
            set
            {
                _selectedStateBackColor = value;
                NotifyPropertyChanged("SelectedStateBackColor");
            }
        }

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get
            {
                return _isExpanded;
            }
            set
            {
                _isExpanded = value;
                NotifyPropertyChanged("IsExpanded");
                GroupCaseImage = new BitmapImage();
                GroupCaseImage.BeginInit();
                if (IsExpanded == true)
					GroupCaseImage.UriSource = new Uri("pack://application:,,,/Testempo;component/resources/group_expand.png");
                else
					GroupCaseImage.UriSource = new Uri("pack://application:,,,/Testempo;component/resources/group_collaps.png");
                GroupCaseImage.EndInit();
                NotifyPropertyChanged("GroupCaseImage");
                UpdateAllChildrenVisible(IsExpanded);
            }
        }

        private BitmapImage _groupCaseImage = null;
        public BitmapImage GroupCaseImage
        {
            get
            {
                return _groupCaseImage;
            }
            set
            {
                _groupCaseImage = value;
                NotifyPropertyChanged("GroupCaseImage");
            }
        }

        private long _globalIndex = 0;
        public long GlobalIndex
        {
            get
            {
                return _globalIndex;
            }
            set
            {
                _globalIndex = value;
                NotifyPropertyChanged("GlobalIndex");
                NotifyPropertyChanged("GlobalIndexString");
            }
        }

        public string GlobalIndexString
        {
            get
            {
                return _globalIndex.ToString();
            }
        }

        private bool _isGlobalIndexVisible = false;
        public bool IsGlobalIndexVisible
        {
            get
            {
                return _isGlobalIndexVisible;
            }
            set
            {
                _isGlobalIndexVisible = false;
                NotifyPropertyChanged("IsGlobalIndexVisible");
            }
        }

        #endregion

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="testColInst"></param>
        /// <param name="groupCaseName"></param>
        /// <param name="level"></param>
        /// <param name="isCase"></param>
        /// <param name="testBaseInst"></param>
        /// <param name="testFilePath"></param>
        public TestGroupCase(TestCollection testColInst, TestGroupCase parent, string groupCaseName, bool isCase, iTestBase testBaseInst, string testFilePath)
        {
            if (testColInst == null)
                throw new ArgumentNullException("Cannot initialize TestGroupCase() because of the null parameter.");
            GlobalIndex = TestCollection.GetGlobalIndex();
            TestColInstance = testColInst;
            GroupCaseName = groupCaseName;
            int level = 0;
            TestGroupCase tgc = parent;
            while (tgc != null)
            {
                level++;
                tgc = tgc.TestGroupCaseParent;
            }
            DisplayLevel = level;
            IsCase = isCase;
            TestBaseInstance = testBaseInst;
            if (IsCase == true)
                TestBaseInstance.TestGroupCaseLocal = this;
            TestFilePath = testFilePath;

            GroupCaseImage = new BitmapImage();
            GroupCaseImage.BeginInit();
            if (IsCase == true)
				GroupCaseImage.UriSource = new Uri("pack://application:,,,/Testempo;component/resources/group_case.png");
            else
            {
                if (TestGroupCaseOc.Count > 1)
                {
					GroupCaseImage.UriSource = new Uri("pack://application:,,,/Testempo;component/resources/group_expand.png");
                    _isExpanded = true;
                }
                else
                {
					GroupCaseImage.UriSource = new Uri("pack://application:,,,/Testempo;component/resources/group_collaps.png");
                    _isExpanded = false;
                }
            }
            GroupCaseImage.EndInit();

            //if (IsCase == true)
            //{
            //    PassImage = new BitmapImage();
            //    PassImage.BeginInit();
			//    PassImage.UriSource = new Uri("pack://application:,,,/Testempo;component/resources/status_ok.png");
            //    PassImage.EndInit();

            //    FailImage = new BitmapImage();
            //    FailImage.BeginInit();
			//    FailImage.UriSource = new Uri("pack://application:,,,/Testempo;component/resources/status_error.png");
            //    FailImage.EndInit();
            //}

            if (parent != null)
            {
                TestGroupCaseParent = parent;
                int index = TestColInstance.FindLastSubGroupCaseDisplayIndex(parent);
                parent.TestGroupCaseOc.Add(this);
                TestGroupCaseParent.IsExpanded = true;
                TestColInstance.TestGroupCaseOcDisp.Insert(index + 1, this);
            }
            else
            {
                TestColInstance.TestGroupCaseOc.Add(this);
                TestColInstance.TestGroupCaseOcDisp.Add(this);
            }

            if (IsCase == true)
                TestBaseInstance.RunCountChangedEvent += new EventHandler<RunCountChangedEventArgs>(TestBaseInstance_RunCountChangedEvent_EventHandler);
        }

        void TestBaseInstance_RunCountChangedEvent_EventHandler(object sender, RunCountChangedEventArgs e)
        {
            RunCount = e.RunCount;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isVis"></param>
        private void UpdateAllChildrenVisible(bool isVis)
        {
            Stack<TestGroupCase> tcStack = new Stack<TestGroupCase>();

            foreach (TestGroupCase tgc in TestGroupCaseOc)
            {
                tcStack.Push(tgc);
            }

            for (; ; )
            {
                if (tcStack.Count < 1)
                    return;

                TestGroupCase tgc = tcStack.Pop();
                tgc.TestGroupCaseIsVisible = isVis;
                if (tgc.IsCase == false && tgc.IsExpanded == true)
                {
                    foreach (TestGroupCase tgci in tgc.TestGroupCaseOc)
                    {
                        tcStack.Push(tgci);
                    }
                }
            }
        }
    }

    public class RunCountChangedEventArgs : EventArgs
    {
        private int _runCount = 1;
        public int RunCount
        {
            get
            {
                return _runCount;
            }
            set
            {
                _runCount = value;
                if (_runCount < 1)
                    _runCount = 1;
            }
        }
    }

    [DefaultProperty("RunCount")]
    public abstract class iTestBase
    {
        public enum TestStateEnum
        {
            None,
            Pass,
            Fail,
            Error,
            Information
        }

        public event EventHandler<RunCountChangedEventArgs> RunCountChangedEvent;

        public delegate void SendMessageDelegate(TestStateEnum tse, string testName, string category, string dataKey, string dataValue, string constraint, bool logDb = true);

        //public EventHandler PauseStateChangedEventHandler;

        /// <summary>
        /// Do NOT call it directly, call DoSendMessage instead
        /// </summary>
        public SendMessageDelegate SendMessage;

        public iTestBase()
        {
        }

        private bool _doPause = false;
        [Browsable(false)]
        public bool DoPause
        {
            get
            {
                return _doPause;
            }
            set
            {
                _doPause = value;
            }
        }

        private string _testName = "No Name";
        [ReadOnly(true), Browsable(true)]
        public string TestName
        {
            get
            {
                return _testName;
            }
            set
            {
                _testName = value;
            }
        }

        private string _contactEngineer = "";
        public string ContactEngineer
        {
            get
            {
                return _contactEngineer;
            }
            set
            {
                _contactEngineer = value;
            }
        }

        private int _runCount = 1;
        public int RunCount
        {
            get
            {
                return _runCount;
            }
            set
            {
                if (value < 1)
                    _runCount = 1;
                else
                    _runCount = value;
                RunCountChangedEvent(this, new RunCountChangedEventArgs() { RunCount = _runCount });
            }
        }

        private TestGroupCase _tgcLocal = null;
        [Browsable(false)]
        public TestGroupCase TestGroupCaseLocal
        {
            get
            {
                return _tgcLocal;
            }
            set
            {
                _tgcLocal = value;
            }
        }

        public virtual void Setup()
        {
            throw new NotImplementedException("TestBase : Setup()");
        }

        public virtual void Clean()
        {
            throw new NotImplementedException("TestBase : Clean()");
        }

        public virtual void Run()
        {
            throw new NotImplementedException("TestBase : Run()");
        }

        protected void DoSendMessageOnly(TestStateEnum tse = TestStateEnum.None, string message = "")
        {
            DoSendMessage(tse, "", message, "", "");
        }

        protected void DoSendMessageData(TestStateEnum tse = TestStateEnum.None, string message = "", string dataValue = "", string constraint = "")
        {
            DoSendMessage(tse, "", message, dataValue, constraint); ;
        }

        protected void DoSendMessage(string category, string message = "", string dataValue = "")
        {
            DoSendMessage(TestStateEnum.None, category, message, dataValue, "");
        }

        protected void DoSendMessage(TestStateEnum tse, string category, string message, string dataValue = "", string constraint = "")
        {
            switch (tse)
            {
                default:
                case TestStateEnum.None:
                    break;
                case TestStateEnum.Information:
                    break;
                case TestStateEnum.Pass:
                    TestGroupCaseLocal.PassCount++;
                    break;
                case TestStateEnum.Fail:
                    TestGroupCaseLocal.FailCount++;
                    break;
                case TestStateEnum.Error:
                    TestGroupCaseLocal.ErrorCount++;
                    break;
            }

            if (SendMessage != null)
                SendMessage(tse, TestName, category, message, dataValue, constraint);
        }

        protected void DoSendFail(string category = "", string message = "", string dataValue = "", string constraint = "")
        {
            DoSendMessage(TestStateEnum.Fail, category, message, dataValue, constraint);
        }

        protected void DoSendInformation(string category = "", string message = "", string dataValue = "", string constraint = "")
        {
            DoSendMessage(TestStateEnum.Information, category, message, dataValue, constraint);
        }

        protected void DoSendError(string category = "", string message = "", string dataValue = "", string constraint = "")
        {
            DoSendMessage(TestStateEnum.Error, category, message, dataValue, constraint);
        }

        protected void DoSendPass(string category = "", string message = "", string dataValue = "", string constraint = "")
        {
            DoSendMessage(TestStateEnum.Pass, category, message, dataValue, constraint);
        }

        public virtual bool Send(object o)
        {
            throw new NotImplementedException();
        }

        public virtual bool Read()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="o">object to be compared</param>
        public virtual void Compare(object o)
        {
        }
    }

    public class CommonOperations
    {
        #region XML Specific Chars

        public static string[] XMLFileStringMap = new string[] {
            "&", "&amp;",
            "<", "&lt;",
            ">", "&gt;",
            "\"", "&quot;",
            "'", "&apos;"
        };

        public static string ConvertXmlString2FileString(string strXml)
        {
            if (strXml == null)
                return null;
            int intCount = XMLFileStringMap.Length / 2;
            for (int intIdx = 0; intIdx < intCount; intIdx++)
            {
                while (strXml.IndexOf(XMLFileStringMap[intIdx * 2]) >= 0)
                {
                    strXml = strXml.Replace(XMLFileStringMap[intIdx * 2], "TEMPVAR4XML");
                }
                while (strXml.IndexOf(XMLFileStringMap[intIdx * 2]) >= 0)
                {
                    strXml = strXml.Replace("TEMPVAR4XML", XMLFileStringMap[intIdx * 2 + 1]);
                }
            }

            return strXml;
        }

        public static string ConvertFileString2XmlString(string strXml)
        {
            if (strXml == null)
                return null;
            int intCount = XMLFileStringMap.Length / 2;
            for (int intIdx = 0; intIdx < intCount; intIdx++)
            {
                while (strXml.IndexOf(XMLFileStringMap[intIdx * 2 + 1]) >= 0)
                {
                    strXml = strXml.Replace(XMLFileStringMap[intIdx * 2] + 1, "TEMPVAR4XML");
                }
                while (strXml.IndexOf(XMLFileStringMap[intIdx * 2 + 1]) >= 0)
                {
                    strXml = strXml.Replace("TEMPVAR4XML", XMLFileStringMap[intIdx * 2]);
                }
            }

            return strXml;
        }

        #endregion

        public static string GetValidDatabaseName(string src)
        {
            if (string.IsNullOrWhiteSpace(src))
                return "";
            src = src.Trim().ToLower();
            string dest = "";
            while (src.Length > 0)
            {
                string c = src.Substring(0, 1);
                src = src.Substring(1);
                if (Regex.Match(c, "[a-zA-Z0-9]") == Match.Empty)
                    continue;
                dest = dest + c;
            }
            return dest;
        }
    }

    public abstract class iSerialTestBase : iTestBase
    {
    }

    public abstract class iNetworkTestBase : iTestBase
    {
    }

    public abstract class iTCPTestBase : iNetworkTestBase
    {
    }

    public abstract class iUDPPTestBase : iNetworkTestBase
    {
    }

    public abstract class iHTTPTestBase : iNetworkTestBase
    {
    }
}
