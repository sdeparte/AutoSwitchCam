using System.Collections.ObjectModel;

namespace AutoSwitchCam.Model
{
    public class Config
    {
        public string SceneName { get; set; }

        public ObservableCollection<Zone> Zones { get; set; }
    }
}
