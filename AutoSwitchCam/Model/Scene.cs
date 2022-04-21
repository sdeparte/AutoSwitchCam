using OBSWebsocketDotNet.Types;

namespace AutoSwitchCam.Model
{
    public class ObservableScene
    {
        public OBSScene OBSScene { get; set; }

        public override string ToString()
        {
            return OBSScene.Name;
        }
    }
}
