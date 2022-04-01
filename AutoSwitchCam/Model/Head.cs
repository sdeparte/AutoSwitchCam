using System;
using System.Windows.Media;

namespace AutoSwitchCam.Model
{
    public class Head
    {
        public Brush Color { get; set; }

        public double? X { get; set; }

        public double RoundedX { get { return Math.Round(X.Value, 2); } }

        public double? Y { get; set; }

        public double RoundedY { get { return Math.Round(Y.Value, 2); } }

        public double? Z { get; set; }

        public double RoundedZ { get { return Math.Round(Z.Value, 2); } }

        public double? Angle { get; set; }
    }
}
