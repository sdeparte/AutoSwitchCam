using AForge.Video.DirectShow;
using System.Windows.Media;

namespace AutoSwitchCam.Model
{
    public class Zone
    {
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
