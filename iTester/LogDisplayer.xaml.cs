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
using System.Collections.ObjectModel;
using System.Xml.Linq;
using System.Reflection;

using iTestBase;

namespace iTester
{
	/// <summary>
	/// Interaction logic for LogDisplayer.xaml
	/// </summary>
	public partial class LogDisplayer : Window, INotifyPropertyChanged
	{
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

		private ObservableCollection<LogDisplayerItem> _logDispOc = new ObservableCollection<LogDisplayerItem>();
		public ObservableCollection<LogDisplayerItem> LogDispOc
		{
			get
			{
				return _logDispOc;
			}
		}

		private string _itcFile = "";
		public string ITCFile
		{
			get
			{
				return _itcFile;
			}
		}

		private bool _itcHasError = false;
		public bool ITCHasError
		{
			get
			{
				return _itcHasError;
			}
			set
			{
				_itcHasError = value;
				if (ITCHasError == true)
					StateMessage = "Has Error!";
				else
					StateMessage = "No Error.";
				NotifyPropertyChanged("ITCHasError");
				NotifyPropertyChanged("StateMessage");
			}
		}

		private string _stateMessage = "No Error.";
		public string StateMessage
		{
			get
			{
				return _stateMessage;
			}
			set
			{
				_stateMessage = value;
				if(_stateMessage != "No Error.")
					StateForeground = new SolidColorBrush(Colors.Red);
				else
					StateForeground = new SolidColorBrush(Colors.Black);
				NotifyPropertyChanged("StateMessage");
				NotifyPropertyChanged("StateForeground");
			}
		}

		private SolidColorBrush _stateForegound = new SolidColorBrush(Colors.Black);
		public SolidColorBrush StateForeground
		{
			get
			{
				return _stateForegound;
			}
			set
			{
				_stateForegound = value;
				NotifyPropertyChanged("StateForeground");
			}
		}

		public LogDisplayer(string itcFile)
		{
			_itcFile = itcFile;

			InitializeComponent();

			DataContext = this;
			dgLogDisplayer.DataContext = LogDispOc;
			LogDispOc.Clear();
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public void NotifyPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		private void Save_Button_Click(object sender, RoutedEventArgs e)
		{

		}

		private void OK_Button_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		private void LogDisplayer_Loaded(object sender, RoutedEventArgs e)
		{
			InRun = true;
			DoOpen();
			InRun = false;
		}

		private void DoOpen()
		{
			try
			{
				XDocument xd = XDocument.Load(ITCFile);
				XElement xeItester = xd.Element("itester");
				if (xeItester == null)
				{
					LogDispOc.Add(
						new LogDisplayerItem()
						{
							State = LogDisplayerItem.StateEnum.Error,
							Message = "Cannot parse " + ITCFile + "."
						});
					ITCHasError = true;
					return;
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
						ParseXMlToTestGroupCase(xe);
					}
				}

				#endregion
			}
			catch (Exception ex)
			{
				LogDispOc.Add(
					new LogDisplayerItem()
					{
						State = LogDisplayerItem.StateEnum.Error,
						Message = "Cannot open " + ITCFile + "."
					});
				LogDispOc.Add(
					new LogDisplayerItem()
					{
						State = LogDisplayerItem.StateEnum.Error,
						Message = "Error message :"
					});
				LogDispOc.Add(
					new LogDisplayerItem()
					{
						State = LogDisplayerItem.StateEnum.Error,
						Message = ex.Message
					});
				ITCHasError = true;
			}
		}

		private void ParseXMlToTestGroupCase(XElement xeSuite)
		{
			string groupOrCase = CommonOperations.ConvertXmlString2FileString(xeSuite.Attribute("type").Value);
            string groupCaseName = CommonOperations.ConvertXmlString2FileString(xeSuite.Attribute("name").Value);
			if (string.Compare(groupOrCase, "case") == 0)
			{
				LogDispOc.Add(
				new LogDisplayerItem()
				{
					State = LogDisplayerItem.StateEnum.Information,
					Message = "Parse case : " + groupCaseName
				});
				string filePath = CommonOperations.ConvertXmlString2FileString(xeSuite.Attribute("path").Value);
				iTestBase.iTestBase tb = LoadTestBaseDllFromFile(filePath);
				if (tb == null)
					return;
				LogDispOc.Add(
					new LogDisplayerItem()
					{
						State = LogDisplayerItem.StateEnum.None,
						Message = filePath + " can be loaded."
					});
				ConfigTestGroupCaseFromXML(tb, xeSuite);
				LogDispOc.Add(
					new LogDisplayerItem()
					{
						State = LogDisplayerItem.StateEnum.None,
						Message = filePath + " can be configured."
					});
			}
			else
			{
				foreach (XElement xe in xeSuite.Elements("groupcase"))
				{
					LogDispOc.Add(
					new LogDisplayerItem()
					{
						State = LogDisplayerItem.StateEnum.Information,
						Message = "Enter group : " + groupCaseName
					});
					ParseXMlToTestGroupCase(xe);
				}
			}
		}

		/// <summary>
		/// If modified, two places are needed because there are two copies of thie medthod in this solution
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="fromFile"></param>
		/// <returns></returns>
		private iTestBase.iTestBase LoadTestBaseDllFromFile(string fileName, bool fromFile = true)
		{
			iTestBase.iTestBase tb = null;
			try
			{
				Assembly asm = null;
				if (fromFile == true)
				{
					int index = ITCFile.LastIndexOf("\\");
					string sf = ITCFile.Substring(0, index);
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

				return tb;
			}
			catch (Exception ex)
			{
				LogDispOc.Add(
					new LogDisplayerItem()
					{
						State = LogDisplayerItem.StateEnum.Error,
						Message = "Cannot load " + fileName + "."
					});
				LogDispOc.Add(
					new LogDisplayerItem()
					{
						State = LogDisplayerItem.StateEnum.Error,
						Message = "Please check whether it is a right dll file."
					});
				LogDispOc.Add(
					new LogDisplayerItem()
					{
						State = LogDisplayerItem.StateEnum.Error,
						Message = "Error message :"
					});
				LogDispOc.Add(
					new LogDisplayerItem()
					{
						State = LogDisplayerItem.StateEnum.Error,
						Message = ex.Message
					});
				ITCHasError = true;
				return null;
			}
		}

		public void ConfigTestGroupCaseFromXML(iTestBase.iTestBase tb, XElement xeCofig)
		{
			Type t = tb.GetType();
			PropertyInfo[] pia = t.GetProperties();
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
							LogDispOc.Add(
								new LogDisplayerItem()
								{
									State = LogDisplayerItem.StateEnum.Error,
									Message = "Cannot parse \"" + sv + "\" for \" " + sn + "\"."
								});
							LogDispOc.Add(
								new LogDisplayerItem()
								{
									State = LogDisplayerItem.StateEnum.Error,
									Message = "Error message :"
								});
							LogDispOc.Add(
								new LogDisplayerItem()
								{
									State = LogDisplayerItem.StateEnum.Error,
									Message = ex.Message
								});
							ITCHasError = true;
						}
					}
				}
			}
		}
	}

	public class LogDisplayerItem : iTestBase.INotifyPropertyChangedClass
	{
		public enum StateEnum
		{
			None,
			Pass,
			Fail,
			Error,
			Information
		}

		private StateEnum _state = StateEnum.None;
		public StateEnum State
		{
			get
			{
				return _state;
			}
			set
			{
				_state = value;
				if (_state != StateEnum.None)
				{
					_stateImage = new BitmapImage();
					_stateImage.BeginInit();
					switch (_state)
					{
						default:
						case StateEnum.None:
							break;
						case StateEnum.Information:
							_stateImage.UriSource = new Uri("pack://application:,,,/Testempo;component/resources/status_info.png");
							break;
						case StateEnum.Pass:
							_stateImage.UriSource = new Uri("pack://application:,,,/Testempo;component/resources/status_ok.png");
							break;
						case StateEnum.Fail:
							_stateImage.UriSource = new Uri("pack://application:,,,/Testempo;component/resources/status_error.png");
							break;
						case StateEnum.Error:
							_stateImage.UriSource = new Uri("pack://application:,,,/Testempo;component/resources/status_ques.ico");
							break;
					}
					_stateImage.EndInit();
				}
				switch (_state)
				{
					default:
					case StateEnum.None:
						_stateBackgound = new SolidColorBrush(Colors.Transparent);
						break;
					case StateEnum.Information:
						_stateBackgound = new SolidColorBrush(Colors.LightBlue);
						break;
					case StateEnum.Pass:
						_stateBackgound = new SolidColorBrush(Colors.LightGreen);
						break;
					case StateEnum.Fail:
						_stateBackgound = new SolidColorBrush(Colors.Red);
						break;
					case StateEnum.Error:
						_stateBackgound = new SolidColorBrush(Colors.Yellow);
						break;
				}
				NotifyPropertyChanged("State");
				NotifyPropertyChanged("StateImage");
				NotifyPropertyChanged("StateBackground");
			}
		}

		private SolidColorBrush _stateBackgound = new SolidColorBrush(Colors.Transparent);
		public SolidColorBrush StateBackground
		{
			get
			{
				return _stateBackgound;
			}
			set
			{
				_stateBackgound = value;
				NotifyPropertyChanged("StateBackground");
			}
		}

		private BitmapImage _stateImage = null;
		public BitmapImage StateImage
		{
			get
			{
				return _stateImage;
			}
			set
			{
				_stateImage = value;
				NotifyPropertyChanged("StateImage");
			}
		}

		private string _message = null;
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
	}
}
