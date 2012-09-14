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

namespace iTester
{
	/// <summary>
	/// Interaction logic for OneLineEditor.xaml
	/// </summary>
	public partial class OneLineEditor : Window
	{
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

            _maxLength = maxLength;
            Title = title;
            lblName.Content = name;
            if (!string.IsNullOrEmpty(userContent))
                txtContent.Text = userContent.Trim();
        }

        private bool _compareList = true;

        private ObservableCollection<string> _userCompareList = null;
        private ObservableCollection<Tuple<string, string>> _userCompareListTuple = null;
        private int _maxLength = -1;

        private string _userContent = "";
        public string UserContent
        {
            get
            {
                return _userContent;
            }
        }

        public string UserContentDB
        {
            get
            {
                return iTestBase.CommonOperations.GetValidDatabaseName(_userContent);
            }
        }

		private void OK_Button_Click(object sender, RoutedEventArgs e)
		{
			_userContent = txtContent.Text;//.Trim();
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
                txtContent.SelectedText = s;//.Trim();
            }
			Content_TextBox_TextChanged(txtContent, null);
		}

		private void Content_TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
            if (_compareList == true)
            {
                #region List

                if (_userCompareList == null || _userCompareList.Count < 1)
                {
                    if (_maxLength > 0)
                    {
                        if (txtContent.Text.Length > _maxLength)
                            btnOK.IsEnabled = false;
                        else
                            btnOK.IsEnabled = !string.IsNullOrWhiteSpace(txtContent.Text);
                    }
                    else
                        btnOK.IsEnabled = !string.IsNullOrWhiteSpace(txtContent.Text);
                }
                else
                {
                    bool found = false;
                    foreach (string s in _userCompareList)
                    {
                        if (string.Compare(txtContent.Text, s) == 0)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found == true)
                        btnOK.IsEnabled = false;
                    else
                    {
                        if (_maxLength > 0)
                        {
                            if (txtContent.Text.Length > _maxLength)
                                btnOK.IsEnabled = false;
                            else
                                btnOK.IsEnabled = !string.IsNullOrWhiteSpace(txtContent.Text);
                        }
                        else
                            btnOK.IsEnabled = !string.IsNullOrWhiteSpace(txtContent.Text);
                    }
                }

                #endregion
            }
            else
            {
                #region List Tuple

                if (_userCompareListTuple == null || _userCompareListTuple.Count < 1)
                {
                    if (_maxLength > 0)
                    {
                        if (txtContent.Text.Length > _maxLength)
                            btnOK.IsEnabled = false;
                        else
                            btnOK.IsEnabled = !string.IsNullOrWhiteSpace(txtContent.Text);
                    }
                    else
                        btnOK.IsEnabled = !string.IsNullOrWhiteSpace(txtContent.Text);
                }
                else
                {
                    bool found = false;
                    foreach (Tuple<string, string> s in _userCompareListTuple)
                    {
                        if (string.Compare(txtContent.Text, s.Item2) == 0
                            || string.Compare(iTestBase.CommonOperations.GetValidDatabaseName(txtContent.Text), s.Item2) == 0)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found == true)
                        btnOK.IsEnabled = false;
                    else
                    {
                        if (_maxLength > 0)
                        {
                            if (txtContent.Text.Length > _maxLength)
                                btnOK.IsEnabled = false;
                            else
                                btnOK.IsEnabled = !string.IsNullOrWhiteSpace(txtContent.Text);
                        }
                        else
                            btnOK.IsEnabled = !string.IsNullOrWhiteSpace(txtContent.Text);
                    }
                }

                #endregion
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

		private void Content_TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			txtContent.Text = txtContent.Text.Trim();
		}
	}
}
