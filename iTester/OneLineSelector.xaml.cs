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
using System.Diagnostics;

using System.Collections.ObjectModel;

namespace iTester
{
	/// <summary>
	/// Interaction logic for OneLineEditor.xaml
	/// </summary>
	public partial class OneLineSelector : Window
	{
        public OneLineSelector(string title, string name, ObservableCollection<Tuple<string, string>> sourceList, string strContent = null)
		{
			Debug.Assert(sourceList != null);
			Debug.Assert(sourceList.Count > 0);

			InitializeComponent();

			Title = title;
			lblName.Content = name;
			_userCompareList = sourceList;
            
            foreach (Tuple<string, string> t in _userCompareList)
            {
                cboxContent.Items.Add(t.Item2);
            }
			
            if (string.IsNullOrWhiteSpace(strContent))
				cboxContent.SelectedIndex = 0;
			else
			{
				int index = 0;
				bool found = false;
                foreach (Tuple<string, string> t in _userCompareList)
				{
					if (t.Item2 == strContent)
					{
						cboxContent.SelectedIndex = index;
						found = true;
					}
					else
						index++;
				}
				if(found == false)
					cboxContent.SelectedIndex = 0;
			}
		}

        private ObservableCollection<Tuple<string, string>> _userCompareList = null;

		public int UserSelectIndex
		{
			get
			{
				return cboxContent.SelectedIndex;
			}
		}

        public string UserSelectContent
        {
            get
            {
                return _userCompareList[UserSelectIndex].Item2;
            }
        }

        public string UserSelectContentDB
        {
            get
            {
                return _userCompareList[UserSelectIndex].Item1;
            }
        }

		private void OK_Button_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		private void Cancel_Button_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}
	}
}
