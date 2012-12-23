using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Diagnostics;

using InformationTransferLibrary;

using System.IO.Compression;
using System.Windows.Forms;

using SharpCompress.Archive.Zip;

using System.Runtime.InteropServices;
using IWshRuntimeLibrary;

namespace MSIProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
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

        private string _installPath = System.Environment.CurrentDirectory;
        public string InstallPath
        {
            get
            {
                return _installPath;
            }
            set
            {
                _installPath = value;
                NotifyPropertyChanged("InstallPath");
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
        }

        private void InstallService_Button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("InstallUtil.exe", "SystemService.exe");
        }

        private void UninstallService_Button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("InstallUtil.exe", "-u SystemService.exe");
        }

        private void Install_Button_Click(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
                {
                    try
                    {
                        InRun = true;
                        bool ret = InstallTask();
                        Dispatcher.Invoke((ThreadStart)delegate()
                        {
                            if(ret == true)
                                System.Windows.MessageBox.Show(this, "Installation OK.", "Installation", MessageBoxButton.OK, MessageBoxImage.Information);
                            else
                                System.Windows.MessageBox.Show(this, "Installation fails.", "Installation", MessageBoxButton.OK, MessageBoxImage.Error);
                        }, null);
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke((ThreadStart)delegate()
                        {
                            System.Windows.MessageBox.Show(this, "Installation fails.\n" + ex.Message, "Installation", MessageBoxButton.OK, MessageBoxImage.Error);
                        }, null);
                    }
                    InRun = false;
                });
        }

        private bool InstallTask()
        {
            if (string.Compare(System.Environment.CurrentDirectory, InstallPath, true) == 0)
            {
                Dispatcher.Invoke((ThreadStart)delegate()
                {
                    System.Windows.MessageBox.Show(this, "Cannot install Management System in the installation source folder.", "Installation", MessageBoxButton.OK, MessageBoxImage.Error);
                }, null);

                return false;
            }

            if (Directory.Exists("data") == false)
                Directory.CreateDirectory("data");
            string[] sa = Directory.GetFiles(System.Environment.CurrentDirectory);
            foreach (string s in sa)
            {
                if (s.EndsWith(".log", StringComparison.CurrentCultureIgnoreCase) == true ||
                    s.EndsWith(".lnk", StringComparison.CurrentCultureIgnoreCase) == true)
                    continue;
                int index = s.LastIndexOf(@"\");
                string newfolder = s.Substring(0, index + 1) + @"data\";
                string fileName = s.Substring(index + 1);
                if (System.IO.File.Exists(newfolder + fileName) == true)
                    System.IO.File.Delete(newfolder + fileName);
                if ((s.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase) == true &&
                    s.EndsWith("MSIProject.exe", StringComparison.CurrentCultureIgnoreCase) == false) ||
                    s.EndsWith(".bat", StringComparison.CurrentCultureIgnoreCase) == true)
                    System.IO.File.Move(s, newfolder + fileName);
                else
                    System.IO.File.Copy(s, newfolder + fileName);
            }

            if (Directory.Exists(InstallPath) == false)
                Directory.CreateDirectory(InstallPath);
            if (System.IO.File.Exists(InstallPath + @"\installed.dat") == false)
                System.IO.File.Create(InstallPath + @"\installed.dat");

            sa = Directory.GetFiles(System.Environment.CurrentDirectory + @"\data");
            foreach (string s in sa)
            {
                if (s.EndsWith(".log", StringComparison.CurrentCultureIgnoreCase) == true ||
                    s.EndsWith(".lnk", StringComparison.CurrentCultureIgnoreCase) == true)
                    continue;
                int index = s.LastIndexOf(@"\");
                string fileName = s.Substring(index + 1);
                if (System.IO.File.Exists(InstallPath + @"\" + fileName) == true)
                    System.IO.File.Delete(InstallPath + @"\" + fileName);
                if (fileName.EndsWith(".bat", StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    StreamReader reader = new StreamReader(s, Encoding.Default);
                    String a = reader.ReadToEnd();
                    a = a.Replace("#DIR#", InstallPath + @"\");
                    StreamWriter readTxt = new StreamWriter(InstallPath + @"\" + fileName, false, Encoding.Default);
                    readTxt.Write(a);
                    readTxt.Flush();
                    readTxt.Close();
                    reader.Close();
                }
                else
                    System.IO.File.Copy(s, InstallPath + @"\" + fileName);
                if (fileName.EndsWith("ManagementSystem.exe", StringComparison.CurrentCultureIgnoreCase) == true ||
                    fileName.EndsWith("ServiceConfiguration.exe", StringComparison.CurrentCultureIgnoreCase) == true ||
                    fileName.EndsWith("TerminalConfiguration.exe", StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    WshShell shell = new WshShell();
                    string onlyFileName = fileName.Substring(0, fileName.Length - 4);
                    IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(InstallPath + @"/" + onlyFileName + ".lnk");
                    shortcut.TargetPath = InstallPath + @"\" + fileName;
                    shortcut.WorkingDirectory = InstallPath;
                    shortcut.WindowStyle = 1;
                    shortcut.Description = onlyFileName;
                    shortcut.Save();
                }
            }

            bool isService = false;
            Dispatcher.Invoke((ThreadStart)delegate()
            {
                if (radService.IsChecked == null)
                    isService = false;
                else
                    isService = (bool)radService.IsChecked;
            }, null);

            if (isService == true)
            {
                Process p = System.Diagnostics.Process.Start(InstallPath + @"\install.bat");
                p.WaitForExit();
            }

            bool isApp = false;
            Dispatcher.Invoke((ThreadStart)delegate()
            {
                if (radApp.IsChecked == null)
                    isApp = false;
                else
                    isApp = (bool)radApp.IsChecked;
            }, null);

            if (isApp == true)
            {
                string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                if (Directory.Exists(folder + @"\Management System") == false)
                    Directory.CreateDirectory(folder + @"\Management System");
                sa = Directory.GetFiles(InstallPath);
                foreach (string s in sa)
                {
                    int index = s.LastIndexOf(@"\");
                    string fileName = s.Substring(index + 1);
                    if (s.EndsWith(".lnk", StringComparison.CurrentCultureIgnoreCase) == true)
                    {
                        if (System.IO.File.Exists(folder + @"\Management System\" + fileName) == true)
                            System.IO.File.Delete(folder + @"\Management System\" + fileName);
                        System.IO.File.Copy(s, folder + @"\Management System\" + fileName);
                    }
                }
            }

            return true;
        }

        private void SelectFolder_Button_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDlg = new FolderBrowserDialog();
            folderDlg.RootFolder = Environment.SpecialFolder.MyComputer;
            if (folderDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            string s = folderDlg.SelectedPath;
            if (s.EndsWith(@"\", StringComparison.CurrentCultureIgnoreCase) == true)
                s = s.Substring(0, s.Length - 1);
            if (s.EndsWith("Comway", StringComparison.CurrentCultureIgnoreCase) == false)
                s = s + @"\Comway";
            InstallPath = s;
        }

        private void Uninstall_Button_Click(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    InRun = true;
                    UninstallTask();
                    Dispatcher.Invoke((ThreadStart)delegate()
                    {
                        System.Windows.MessageBox.Show(this, "Uninstallation OK.", "Installation", MessageBoxButton.OK, MessageBoxImage.Information);
                    }, null);
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke((ThreadStart)delegate()
                    {
                        System.Windows.MessageBox.Show(this, "Uninstallation fails.\n" + ex.Message, "Installation", MessageBoxButton.OK, MessageBoxImage.Error);
                    }, null);
                }
                InRun = false;
            });
        }

        private void UninstallTask()
        {
            MessageBoxResult mbr = MessageBoxResult.No;
            Dispatcher.Invoke((ThreadStart)delegate()
            {
                mbr = System.Windows.MessageBox.Show("Are you sure to uninstall Management System?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            }, null);
            if (mbr != MessageBoxResult.Yes)
                return;

            bool isService = false;
            Dispatcher.Invoke((ThreadStart)delegate()
            {
                if (radService.IsChecked == null)
                    isService = false;
                else
                    isService = (bool)radService.IsChecked;
            }, null);

            if (isService == true)
            {
                Process p = System.Diagnostics.Process.Start(InstallPath + @"\uninstall.bat");
                p.WaitForExit();
            }

            bool isApp = false;
            Dispatcher.Invoke((ThreadStart)delegate()
            {
                if (radApp.IsChecked == null)
                    isApp = false;
                else
                    isApp = (bool)radApp.IsChecked;
            }, null);

            if (isApp == true)
            {
                string folder = System.Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                if (Directory.Exists(folder + @"\Management System") == true)
                    Directory.Delete(folder + @"\Management System", true);
            }
        }

        private void Window_Load(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists(InstallPath + @"\installed.dat") == true)
                btnInstall.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            System.Environment.Exit(0);
        }
    }
}
