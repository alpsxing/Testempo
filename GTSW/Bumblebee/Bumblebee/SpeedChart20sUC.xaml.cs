using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace Bumblebee
{
    /// <summary>
    /// Interaction logic for SpeedChartUC.xaml
    /// </summary>
    public partial class SpeedChart20sUC : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region Variables

        ObservableCollection<Tuple<int, byte>> _records = null;

        #endregion

        #region Properties

        private Point[] _points = null;
        public Point[] Points
        {
            get
            {
                return _points;
            }
            set
            {
                _points = value;
                NotifyPropertyChanged("Points");
            }
        }

        private string _speed5 = "";
        public string Speed5
        {
            get
            {
                return _speed5;
            }
            set
            {
                _speed5 = value;
                NotifyPropertyChanged("Speed5");
            }
        }

        private string _speed4 = "";
        public string Speed4
        {
            get
            {
                return _speed4;
            }
            set
            {
                _speed4 = value;
                NotifyPropertyChanged("Speed4");
            }
        }

        private string _speed3 = "";
        public string Speed3
        {
            get
            {
                return _speed3;
            }
            set
            {
                _speed3 = value;
                NotifyPropertyChanged("Speed3");
            }
        }

        private string _speed2 = "";
        public string Speed2
        {
            get
            {
                return _speed2;
            }
            set
            {
                _speed2 = value;
                NotifyPropertyChanged("Speed2");
            }
        }

        private string _speed1 = "";
        public string Speed1
        {
            get
            {
                return _speed1;
            }
            set
            {
                _speed1 = value;
                NotifyPropertyChanged("Speed1");
            }
        }

        private string _speed0 = "";
        public string Speed0
        {
            get
            {
                return _speed0;
            }
            set
            {
                _speed0 = value;
                NotifyPropertyChanged("Speed0");
            }
        }

        private int _minSpeed = 0;
        public int MinSpeed
        {
            get
            {
                return _minSpeed;
            }
            set
            {
                _minSpeed = value;
                NotifyPropertyChanged("MinSpeed");
            }
        }

        private int _maxSpeed = 0;
        public int MaxSpeed
        {
            get
            {
                return _maxSpeed;
            }
            set
            {
                _maxSpeed = value;
                NotifyPropertyChanged("MaxSpeed");
            }
        }

        #endregion

        public SpeedChart20sUC(int minSpeed, int maxSpeed, ObservableCollection<Tuple<int, byte>> records)
        {
            InitializeComponent();

            DataContext = this;

            _records = new ObservableCollection<Tuple<int, byte>>();
            for (int i = records.Count - 1; i >= 0; i--)
            {
                _records.Add(records[i]);
            }

            double step = (maxSpeed - minSpeed) / 5.0;
            int iStep = (int)(step * 10.0);
            step = iStep /10;

            Speed0 = minSpeed.ToString();
            Speed1 = (minSpeed + step).ToString();
            Speed2 = (minSpeed + step * 2).ToString();
            Speed3 = (minSpeed + step * 3).ToString();
            Speed4 = (minSpeed + step * 4).ToString();
            Speed5 = maxSpeed.ToString();
            lblSpeed5.Content = Speed5 + "km/h";
            lblSpeed4.Content = Speed4 + "km/h";
            lblSpeed3.Content = Speed3 + "km/h";
            lblSpeed2.Content = Speed2 + "km/h";
            lblSpeed1.Content = Speed1 + "km/h";
            lblSpeed0.Content = Speed0 + "km/h";

            MaxSpeed = maxSpeed;
            MinSpeed = minSpeed;

            if(MaxSpeed != MinSpeed)
                DrawLine();
        }

        private void DrawLine()
        {
            List<PathFigure> listPF = new List<PathFigure>();
            PathFigure pf = null;
            for (int i = 0; i < _records.Count; i++)
            {
                double x = (canvasTraces.Width / 100.0) * i;
                double y = 0.0;
                if (MaxSpeed - MinSpeed == 0.0)
                    y = canvasTraces.Height;
                else
                    y = canvasTraces.Height * (1.0 - (((double)_records[i].Item1 - MinSpeed) / (double)(MaxSpeed - MinSpeed)));
                Point pt = new Point(x, y);
                if (pf == null)
                {
                    pf = new PathFigure();
                    pf.StartPoint = pt;
                }
                else
                {
                    if (_records[i].Item1 == 0xFF)
                    {
                        pf.IsClosed = false;
                        listPF.Add(pf);
                        pf = null;
                    }
                    else
                    {
                        pf.Segments.Add(new LineSegment(pt, true));
                    }
                }
            }
            if (pf != null)
            {
                pf.IsClosed = false;
                listPF.Add(pf);
            }
            PathGeometry pg = new PathGeometry(listPF);
            pathSpeed.Data = pg;
        }
    }
}
