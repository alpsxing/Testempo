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
    /// Interaction logic for View2DTUsUC.xaml
    /// </summary>
    public partial class View2DTUsUC : UserControl
    {
        public TabControl TabControl0
        {
            get
            {
                return tcCol0;
            }
        }

        public TabControl TabControl1
        {
            get
            {
                return tcCol1;
            }
        }

        public View2DTUsUC()
        {
            InitializeComponent();
        }
    }
}
