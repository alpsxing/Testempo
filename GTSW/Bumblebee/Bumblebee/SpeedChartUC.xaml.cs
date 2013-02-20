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
    public partial class SpeedChartUC : UserControl, INotifyPropertyChanged
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

        public SpeedChartUC(int minSpeed, int maxSpeed, ObservableCollection<Tuple<int, byte>> records)
        {
            InitializeComponent();

            DataContext = this;

            _records = records;

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
            List<PathFigure> _listPathFigure = new List<PathFigure>();
            PathFigure pf = new PathFigure();
            for (int i = 0; i < _records.Count; i++)
            {
                double x = (canvasTraces.Width / 60.0) * i;
                double y = 0.0;
                if (MaxSpeed - MinSpeed == 0.0)
                    y = canvasTraces.Height;
                else
                    y = canvasTraces.Height * (1.0 - (((double)_records[i].Item1 - MinSpeed) / (double)(MaxSpeed - MinSpeed)));
                Point pt = new Point(x, y);
                if (i == 0)
                    pf.StartPoint = pt;
                else
                {
                    pf.Segments.Add(new LineSegment(pt, true));
                }
            }
            pf.IsClosed = false;
            List<PathFigure> listPF = new List<PathFigure>();
            listPF.Add(pf);
            PathGeometry pg = new PathGeometry(listPF);
            pathSpeed.Data = pg;
        }
    }

    [ValueConversion(typeof(Point[]), typeof(Geometry))]
    public class PointsToPathConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Point[] points = (Point[])value;
            if (points.Length > 0)
            {
                Point start = points[0];
                List<LineSegment> segments = new List<LineSegment>();
                for (int i = 1; i < points.Length; i++)
                {
                    segments.Add(new LineSegment(points[i], true));
                }
                PathFigure figure = new PathFigure(start, segments, false); //true if closed
                PathGeometry geometry = new PathGeometry();
                geometry.Figures.Add(figure);
                return geometry;
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
