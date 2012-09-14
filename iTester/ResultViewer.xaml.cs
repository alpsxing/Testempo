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

namespace iTester
{
	/// <summary>
	/// Interaction logic for ResultViewer.xaml
	/// </summary>
	public partial class ResultViewer : Window, INotifyPropertyChanged
	{
		public ResultViewer()
		{
			InitializeComponent();

			DataContext = this;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public void NotifyPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
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
                NotifyPropertyChanged("InformationCountString");
            }
		}

        public string InformationCountString
        {
            get
            {
                return _intInfoCount.ToString();
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
                NotifyPropertyChanged("PassCountString");
            }
		}

        public string PassCountString
        {
            get
            {
                return _intPassCount.ToString();
            }
        }

		private int _intUnknownCount = 0;
		public int UnknownCount
		{
			get
			{
                return _intUnknownCount;
			}
			set
			{
                _intUnknownCount = value;
                if (_intUnknownCount < 0) _intUnknownCount = 0;
                NotifyPropertyChanged("UnknownCount");
                NotifyPropertyChanged("UnknownCountString");
            }
		}

        public string UnknownCountString
        {
            get
            {
                return _intUnknownCount.ToString();
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
                NotifyPropertyChanged("ErrorCountString");
            }
		}

        public string ErrorCountString
        {
            get
            {
                return _intErrorCount.ToString();
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
                NotifyPropertyChanged("TotalCountString");
            }
		}

        public string TotalCountString
        {
            get
            {
                return _intTotalCount.ToString();
            }
        }
    }
}
