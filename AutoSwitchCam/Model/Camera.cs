using AForge.Video.DirectShow;

namespace AutoSwitchCam.Model
{
    public class Camera
    {
        public FilterInfo FilterInfo { get; set; }

        public override string ToString()
        {
            return this.FilterInfo.Name;
        }
    }
}
