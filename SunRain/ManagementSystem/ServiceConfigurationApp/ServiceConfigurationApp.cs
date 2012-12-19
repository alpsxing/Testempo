using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ServiceConfigurationApp
{
    public partial class ServiceConfigurationApp : Form
    {
        private FormWindowState _savedWindowState = FormWindowState.Normal;

        public ServiceConfigurationApp()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        private void Window_SizeChanged(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                ShowInTaskbar = false;
                Visible = false;
                niServiceConfiguration.Visible = true;
            }
            else
                _savedWindowState = WindowState;
        }

        private void NIServiceConfig_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            niServiceConfiguration.Visible = false;
            ShowInTaskbar = true;
            Visible = true;
            // It is a must to put this line at the end.
            WindowState = _savedWindowState;
            //WindowState = FormWindowState.Normal;
        }
    }
}
