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

namespace iTester
{
	/// <summary>
	/// Interaction logic for OneLineEditor.xaml
	/// </summary>
	public partial class TwoLineEditor : Window
	{
		public TwoLineEditor(string title, string name, string name2, 
			string userContent = null, string userContent2 = null, 
			List<Tuple<string, string>> userCompareList = null)
		{
			InitializeComponent();

			Title = title;
			lblName.Content = name;
			lblName2.Content = name2;
			if (!string.IsNullOrEmpty(userContent))
				txtContent.Text = userContent.Trim();
			if (!string.IsNullOrEmpty(userContent2))
				txtContent2.Text = userContent.Trim();
			_userCompareList = userCompareList;
		}

		private List<Tuple<string, string>> _userCompareList = null;

		private string _userContent = "";
		public string UserContent
		{
			get
			{
				return _userContent;
			}
		}

		private string _userContent2 = "";
		public string UserContent2
		{
			get
			{
				return _userContent2;
			}
		}

		private void OK_Button_Click(object sender, RoutedEventArgs e)
		{
			_userContent = txtContent.Text.Trim();
			_userContent2 = txtContent2.Text.Trim();
			DialogResult = true;
		}

		private void Cancel_Button_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}

		private void OneLineEditor_Loaded(object sender, RoutedEventArgs e)
		{
			txtContent.Focus();
			if (!string.IsNullOrWhiteSpace(txtContent.Text))
			{
				string s = txtContent.Text;
				txtContent.Text = "";
				txtContent.SelectedText = s;
			}
			if (!string.IsNullOrWhiteSpace(txtContent.Text))
			{
				string s = txtContent2.Text;
				txtContent2.Text = "";
				txtContent2.SelectedText = s;
			}
			Content_TextBox_TextChanged(txtContent, null);
		}

		private void Content_TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
            if (_userCompareList == null || _userCompareList.Count < 1)
				btnOK.IsEnabled = !string.IsNullOrWhiteSpace(txtContent.Text)
					&& !string.IsNullOrWhiteSpace(txtContent2.Text);
            else
            {
                bool found = false;
                foreach (Tuple<string, string> t in _userCompareList)
                {
                    if (string.Compare(txtContent.Text.Trim(), t.Item1.Trim()) == 0 &&
						string.Compare(txtContent2.Text.Trim(), t.Item2.Trim()) == 0)
                    {
                        found = true;
                        break;
                    }
                }
				btnOK.IsEnabled = !string.IsNullOrWhiteSpace(txtContent.Text) 
					&& !string.IsNullOrWhiteSpace(txtContent2.Text) 
					&& found != true;
            }
        
            if (btnOK.IsEnabled == true)
            {
                btnOK.IsDefault = true;
                btnCancel.IsDefault = false;
            }
            else
            {
                btnOK.IsDefault = false;
                btnCancel.IsDefault = true;
            }
        }
	}
}
