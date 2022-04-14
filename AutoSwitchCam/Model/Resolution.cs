using AForge.Video.DirectShow;

namespace AutoSwitchCam.Model
{
    public class Resolution
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public override string ToString()
        {
            return $"{Width} x {Height}";
        }
    }
}
