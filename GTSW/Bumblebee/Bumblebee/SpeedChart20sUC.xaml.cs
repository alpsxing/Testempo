﻿using System;
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

        ObservableCollection<Tuple<int, byte, string>> _records = null;
		private List<Path> _listPath = new List<Path>();

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

        public SpeedChart20sUC(int minSpeed, int maxSpeed, ObservableCollection<Tuple<int, byte, string>> records)
        {
            InitializeComponent();

			_listPath.Add(pathSpeed);
			_listPath.Add(pathSpeed1);
			_listPath.Add(pathSpeed2);
			_listPath.Add(pathSpeed3);
			_listPath.Add(pathSpeed4);
			_listPath.Add(pathSpeed5);

            DataContext = this;

			_records = new ObservableCollection<Tuple<int, byte, string>>();
            for (int i = records.Count - 1; i >= 0; i--)
            {
                _records.Add(records[i]);
            }

            if (maxSpeed == minSpeed)
            {
                if (minSpeed - 1 >= 0)
                {
                    minSpeed = minSpeed - 1;
                    maxSpeed = maxSpeed + 4;
                }
                else
                {
                    maxSpeed = maxSpeed + 5;
                }
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
			List<List<PathFigure>> listListPF = new List<List<PathFigure>>();
			List<PathFigure> listPF = null;
			PathFigure pf = null;
			string prevNumber = null;
			int indexPath = 0;
			for (int i = 0; i < _records.Count; i++)
            {
				if (prevNumber == null)
					listPF = new List<PathFigure>();
				else
				{
					if (string.Compare(prevNumber, _records[i].Item3, true) != 0)
					{
						if (pf != null)
						{
							int count = pf.Segments.Count;
							double x0 = 0.0;
							double y0 = 0.0;
							if (count == 0)
							{
								x0 = pf.StartPoint.X + (canvasTraces.Width / 100.0);
								y0 = pf.StartPoint.Y;
							}
							else
							{
								x0 = ((LineSegment)(pf.Segments[count - 1])).Point.X + (canvasTraces.Width / 100.0);
								y0 = ((LineSegment)(pf.Segments[count - 1])).Point.Y;
							}
							pf.Segments.Add(new LineSegment(new Point(x0, y0), true));
							pf.IsClosed = false;
							listPF.Add(pf);
						}
						PathGeometry pg = new PathGeometry(listPF);
						_listPath[indexPath].Data = pg;

						indexPath++;

						pf = null;
						listPF = new List<PathFigure>();
					}
				}

				prevNumber = _records[i].Item3;

                double x = (canvasTraces.Width / 100.0) * i;
                double y = 0.0;
                if (MaxSpeed - MinSpeed == 0.0)
                    y = canvasTraces.Height;
                else
                    y = canvasTraces.Height * (1.0 - (((double)_records[i].Item1 - MinSpeed) / (double)(MaxSpeed - MinSpeed)));
                Point pt = new Point(x, y);
                if (pf == null)
                {
                    if (_records[i].Item1 < 0xFF)
                    {
                        pf = new PathFigure();
                        pf.StartPoint = pt;
                    } 
                }
                else
                {
                    if (_records[i].Item1 == 0xFF)
                    {
                        int count = pf.Segments.Count;
                        double x0 = 0.0;
                        double y0 = 0.0;
                        if (count == 0)
                        {
                            x0 = pf.StartPoint.X + (canvasTraces.Width / 100.0);
                            y0 = pf.StartPoint.Y;
                        }
                        else
                        {
                            x0 = ((LineSegment)(pf.Segments[count - 1])).Point.X + (canvasTraces.Width / 100.0);
                            y0 = ((LineSegment)(pf.Segments[count - 1])).Point.Y;
                        }
                        pf.Segments.Add(new LineSegment(new Point(x0, y0), true));
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
                int count = pf.Segments.Count;
                double x0 = 0.0;
                double y0 = 0.0;
                if (count == 0)
                {
                    x0 = pf.StartPoint.X + (canvasTraces.Width / 100.0);
                    y0 = pf.StartPoint.Y;
                }
                else
                {
                    x0 = ((LineSegment)(pf.Segments[count - 1])).Point.X + (canvasTraces.Width / 100.0);
                    y0 = ((LineSegment)(pf.Segments[count - 1])).Point.Y;
                }
                pf.Segments.Add(new LineSegment(new Point(x0, y0), true));
                pf.IsClosed = false;
                listPF.Add(pf);
            }
            PathGeometry pgLast = new PathGeometry(listPF);
			_listPath[indexPath].Data = pgLast;// pathSpeed.Data = pgLast;
        }
    }
}
