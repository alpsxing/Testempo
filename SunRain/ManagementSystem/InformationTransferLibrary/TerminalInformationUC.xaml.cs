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

//using WalburySoftware;

namespace InformationTransferLibrary
{
    /// <summary>
    /// Interaction logic for TerminalInformationUC.xaml
    /// </summary>
    public partial class TerminalInformationUC : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _noInfo = true;

        private TerminalInformation _ti = null;
        public TerminalInformation TI
        {
            get
            {
                return _ti;
            }
        }

        private string _dtuCommand = "";
        public string DTUCommand
        {
            get
            {
                return _dtuCommand;
            }
            set
            {
                _dtuCommand = value;
                NotifyPropertyChanged("DTUCommand");
            }
        }

        public TerminalInformationUC(TerminalInformation ti)
        {
            InitializeComponent();

            DataContext = this;

            _ti = ti;
        }

        private void DTUCommand_TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ProcessDTUCommand(DTUCommand);
                DTUCommand = "";
            }
        }

        private void DTUCommand_TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Return)
            //{
            //    ProcessDTUCommand(DTUCommand);
            //    DTUCommand = "";
            //}
        }

        private void Send_Button_Click(object sender, RoutedEventArgs e)
        {
            rtxtTerminal.AppendText("\n");

            ProcessDTUCommand(DTUCommand);
            DTUCommand = "";
        }

        private void TermInfoUCControl_Load(object sender, RoutedEventArgs e)
        {
            txtDTUCmd.Focus();
            //ProcessDTUCommand(null);
        }

        private void ProcessDTUCommand(string s)
        {
            if (s == null)
                return;

            Dispatcher.Invoke((ThreadStart)delegate()
            {
                if (_noInfo == false)
                {
                    Run runReturn = new Run("\n");
                    runReturn.Foreground = Brushes.Black;
                    Paragraph parReturn = new Paragraph(runReturn);
                    fdTerminal.Blocks.Add(parReturn);
                }
                else
                {
                    _noInfo = false;
                    fdTerminal.Blocks.Clear();
                }
                Run runNewLine = new Run(TI.CurrentDTU.DtuId + " => ");
                runNewLine.Foreground = Brushes.Blue;
              
                Run runDTUCmd = new Run(s);
                runDTUCmd.Foreground = Brushes.Black;

                Run runReturn2 = new Run("\n");
                runReturn2.Foreground = Brushes.Black;

                Paragraph parNewLine = new Paragraph();
                parNewLine.Inlines.Add(runNewLine);
                parNewLine.Inlines.Add(runDTUCmd);
                parNewLine.Inlines.Add(runReturn2);
                fdTerminal.Blocks.Add(parNewLine);

                rtxtTerminal.ScrollToEnd();

                TI.PutReq(s);
            }, null);
        }

        public void MessageReceivedEventHandler(object sender, TerminalInformationEventArgs args)
        {
            Dispatcher.Invoke((ThreadStart)delegate()
            {
                if (args.MessageByteLength < 1)
                    return;
                int len = 0;
                if (args.MessageByte.Length < args.MessageByteLength)
                    len = args.MessageByte.Length;
                else
                    len = args.MessageByteLength;
                byte[] ba = new byte[len];
                for (int i = 0; i < len; i++)
                {
                    ba[i] = args.MessageByte[i];
                }
                string resp = System.Text.Encoding.ASCII.GetString(ba, 0, len);
                if (resp.StartsWith(Consts.TERM_INVALID_REQUEST, StringComparison.CurrentCultureIgnoreCase) == true)
                {
                    Run runDTUCmd = new Run("Connection to DTU is broken.");
                    Paragraph parDTUCmd = new Paragraph(runDTUCmd);
                    parDTUCmd.Foreground = Brushes.Red;
                    fdTerminal.Blocks.Add(parDTUCmd);
                }
                else
                    rtxtTerminal.AppendText(resp);

                rtxtTerminal.ScrollToEnd();
            }, null);
        }

        public void SocketStateChangeEventHandler(object sender, TerminalInformationEventArgs args)
        {
            string s = args.Message;
            if (s == null)
                return;

            Dispatcher.Invoke((ThreadStart)delegate()
            {
                if (fdTerminal.Blocks.Count > 0)
                {
                    Run runReturn = new Run("\n");
                    Paragraph parReturn = new Paragraph(runReturn);
                    parReturn.Foreground = Brushes.Black;
                    fdTerminal.Blocks.Add(parReturn);
                }
                Run runDTUCmd = new Run(s);
                Paragraph parDTUCmd = new Paragraph(runDTUCmd);
                parDTUCmd.Foreground = Brushes.Red;
                fdTerminal.Blocks.Add(parDTUCmd);

                rtxtTerminal.ScrollToEnd();
            }, null);
        }
    }
}
