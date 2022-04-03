
using AutoSwitchCam.Helper;
using System.Drawing;
using System.Windows.Media;

namespace AutoSwitchCam.Model
{
    public class Zone
    {
        public string Color { get; set; }

        public SolidColorBrush BrushColor {
            get {
                System.Drawing.Color color = ColorHelper.ToColor(Color);
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(color.R, color.G, color.B));
            }
        }
        
        public SolidColorBrush TransparentBrushColor {
            get
            {
                System.Drawing.Color color = ColorHelper.ToColor(Color);
                return new SolidColorBrush(System.Windows.Media.Color.FromArgb(127, color.R, color.G, color.B));
            }
        }

        public string CameraName { get; set; }

        public int CameraIndex { get; set; }

        public string Camera { get { return $"{CameraName} ({CameraIndex})"; } }

        public double X1 { get; set; }

        public double Z1 { get; set; }

        public double X2 { get; set; }

        public double Z2 { get; set; }

        public double X3 { get; set; }

        public double Z3 { get; set; }

        public double X4 { get; set; }

        public double Z4 { get; set; }
    }
}
