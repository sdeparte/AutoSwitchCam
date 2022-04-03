using AutoSwitchCam.Model;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;

namespace AutoSwitchCam.Services
{
    public class ConfigReader
    {
        private readonly XmlSerializer _zoneSerializer = new XmlSerializer(typeof(ObservableCollection<Zone>));

        public void readConfigFiles(MainWindow main)
        {
            using (FileStream fs = new FileStream(@"zones.xml", FileMode.OpenOrCreate))
            {
                try
                {
                    main.ListZones = _zoneSerializer.Deserialize(fs) as ObservableCollection<Zone>;
                }
                catch (Exception ex) { }
            }
        }

        public void updateConfigFiles(MainWindow main)
        {
            using (FileStream fs = new FileStream(@"zones.xml", FileMode.OpenOrCreate))
            {
                _zoneSerializer.Serialize(fs, main.ListZones);
            }
        }
    }
}
