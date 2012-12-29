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

namespace InformationTransferLibrary
{
    /// <summary>
    /// Interaction logic for View4DTUsUC.xaml
    /// </summary>
    public partial class View4DTUsUC : UserControl
    {
        public TabControl TabControl00
        {
            get
            {
                return tcCol00;
            }
        }

        public TabControl TabControl01
        {
            get
            {
                return tcCol01;
            }
        }

        public TabControl TabControl10
        {
            get
            {
                return tcCol10;
            }
        }

        public TabControl TabControl11
        {
            get
            {
                return tcCol11;
            }
        }

        public View4DTUsUC()
        {
            InitializeComponent();
        }
    }
}
