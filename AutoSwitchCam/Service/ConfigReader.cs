using AutoSwitchCam.Model;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Xml.Serialization;

namespace AutoSwitchCam.Services
{
    public class ConfigReader
    {
        private readonly XmlSerializer _zoneSerializer = new XmlSerializer(typeof(Config));

        public Config readConfigFiles()
        {
            using (FileStream fs = new FileStream(@"zones.xml", FileMode.OpenOrCreate))
            {
                try
                {
                    return _zoneSerializer.Deserialize(fs) as Config;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Configuration failed : " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }

                return null;
            }
        }

        public void updateConfigFiles(Config config)
        {
            using (FileStream fs = new FileStream(@"zones.xml", FileMode.Create))
            {
                _zoneSerializer.Serialize(fs, config);
            }
        }
    }
}
