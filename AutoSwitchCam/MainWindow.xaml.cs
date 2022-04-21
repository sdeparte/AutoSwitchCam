using AutoSwitchCam.Helper;
using AutoSwitchCam.Model;
using AutoSwitchCam.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AutoSwitchCam
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ConfigReader _configReader;
        private readonly OBSLinker _obsLinker;

        private ObservableCollection<ObservableScene> _listScenes = new ObservableCollection<ObservableScene>();
        public ObservableCollection<ObservableScene> ListScenes { get { return _listScenes; } }

        private ObservableCollection<ObservableSceneItem> _listSceneItems = new ObservableCollection<ObservableSceneItem>();
        public ObservableCollection<ObservableSceneItem> ListSceneItems { get { return _listSceneItems; } }

        private ObservableCollection<Zone> _listZones = new ObservableCollection<Zone>();
         public ObservableCollection<Zone> ListZones { get { return _listZones; } }

        private readonly ObservableCollection<Head> _listHeads = new ObservableCollection<Head>();
        public ObservableCollection<Head> ListHeads { get { return _listHeads; } }

        private KinectDetector _kinectDetector;

        public ImageSource BodiesImageSource { get { return _kinectDetector?.BodiesImageSource; } }

        public ImageSource HeadsImageSource { get { return _kinectDetector?.HeadsImageSource; } }

        public MainWindow()
        {
            InitializeComponent();

            _configReader = new ConfigReader();

            _obsLinker = new OBSLinker();
            _obsLinker.OBSLinker_Connected += OBSLinker_Connected;
            _obsLinker.Connect();
        }

        private void OBSLinker_Connected(object sender, ObservableCollection<ObservableScene> listScenes)
        {
            _listScenes = listScenes;

            Config config = _configReader.readConfigFiles();

            if (config != null)
            {
                foreach (ObservableScene scene in _listScenes)
                {
                    if (scene.OBSScene.Name == config.SceneName)
                    {
                        PrimarySceneSelectBox.SelectedItem = scene;
                    }
                }

                _listZones = config.Zones;
            }

            _kinectDetector = new KinectDetector(this);
            _kinectDetector.NewPosition += Kinect_NewPosition;

            InitHelpersSelectors();
        }

        private void InitHelpersSelectors()
        {
            ObservableCollection<String> heads = new ObservableCollection<String>();
            for (int i = 1; i <= _kinectDetector.BodiesCount; i++) { heads.Add($"Tête n°{i}"); }
            HeadsSelectBox.ItemsSource = heads;
            HeadsSelectBox.SelectedIndex = 0;

            ObservableCollection<String> points = new ObservableCollection<String>();
            for (int i = 1; i <= 4; i++) { points.Add($"Point n°{i}"); }
            PointsSelectBox.ItemsSource = points;
            PointsSelectBox.SelectedIndex = 0;
        }

        private void Kinect_NewPosition(object sender, Head[] heads)
        {
            _listHeads.Clear();

            int countHeads = 0;

            for (int i = 0; i < heads.Length; i++)
            {
                Head head = heads[i];

                if (head != null)
                {
                    _listHeads.Add(head);
                    countHeads++;
                }
            }

            if (countHeads == 0)
            {
                return;
            }

            Zone zoneToDisplay = ZoneHelper.GetZoneToDisplay(_listZones, _listHeads);

            foreach (ObservableSceneItem sceneItem in _listSceneItems)
            {
                if (sceneItem.SceneItem.SourceName == zoneToDisplay?.SourceName)
                {
                    CurrentSceneItemSelectBox.SelectedItem = sceneItem;
                    break;
                }
            }
        }

        private void SendToPoint_Click(object sender, RoutedEventArgs e)
        {
            if (_listHeads.Count <= HeadsSelectBox.SelectedIndex)
            {
                return;
            }

            Head head = _listHeads[HeadsSelectBox.SelectedIndex];

            switch (PointsSelectBox.SelectedIndex)
            {
                case 0:
                    AddX1TextBox.Text = head.X.ToString();
                    AddZ1TextBox.Text = head.Z.ToString();
                    break;

                case 1:
                    AddX2TextBox.Text = head.X.ToString();
                    AddZ2TextBox.Text = head.Z.ToString();
                    break;

                case 2:
                    AddX3TextBox.Text = head.X.ToString();
                    AddZ3TextBox.Text = head.Z.ToString();
                    break;

                case 3:
                    AddX4TextBox.Text = head.X.ToString();
                    AddZ4TextBox.Text = head.Z.ToString();
                    break;
            }
        }

        private void AddZone_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(AddX1TextBox.Text) || string.IsNullOrEmpty(AddZ1TextBox.Text) ||
                string.IsNullOrEmpty(AddX2TextBox.Text) || string.IsNullOrEmpty(AddZ2TextBox.Text) ||
                string.IsNullOrEmpty(AddX3TextBox.Text) || string.IsNullOrEmpty(AddZ3TextBox.Text) ||
                string.IsNullOrEmpty(AddX4TextBox.Text) || string.IsNullOrEmpty(AddZ4TextBox.Text) ||
                !ColorHelper.IsColorString.IsMatch(AddColorTextBox.Text))
            {
                return;
            }

            _listZones.Add(new Zone()
            {
                Color = AddColorTextBox.Text,
                SourceName = ((ObservableSceneItem)AddSceneItemSelectBox.SelectedItem).SceneItem.SourceName,
                X1 = double.Parse(AddX1TextBox.Text, CultureInfo.InvariantCulture),
                Z1 = double.Parse(AddZ1TextBox.Text, CultureInfo.InvariantCulture),
                X2 = double.Parse(AddX2TextBox.Text, CultureInfo.InvariantCulture),
                Z2 = double.Parse(AddZ2TextBox.Text, CultureInfo.InvariantCulture),
                X3 = double.Parse(AddX3TextBox.Text, CultureInfo.InvariantCulture),
                Z3 = double.Parse(AddZ3TextBox.Text, CultureInfo.InvariantCulture),
                X4 = double.Parse(AddX4TextBox.Text, CultureInfo.InvariantCulture),
                Z4 = double.Parse(AddZ4TextBox.Text, CultureInfo.InvariantCulture),
            });

            Config config = new Config
            {
                SceneName = ((ObservableScene)PrimarySceneSelectBox.SelectedItem).OBSScene.Name,
                Zones = _listZones
            };

            _configReader.updateConfigFiles(config);
        }

        private void AddColorTextBlock_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ColorHelper.IsColorString.IsMatch(AddColorTextBox.Text))
            {
                System.Drawing.Color color = ColorHelper.ToColor(AddColorTextBox.Text);
                AddPreviewColorRectangle.Fill = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B));
            }
            else
            {
                AddPreviewColorRectangle.Fill = Brushes.Black;
            }
        }

        private void EditZone_Click(object sender, RoutedEventArgs e)
        {
            if (ZonesDataGrid.SelectedIndex >= 0)
            {
                if (string.IsNullOrEmpty(EditX1TextBox.Text) || string.IsNullOrEmpty(EditZ1TextBox.Text) ||
                    string.IsNullOrEmpty(EditX2TextBox.Text) || string.IsNullOrEmpty(EditZ2TextBox.Text) ||
                    string.IsNullOrEmpty(EditX3TextBox.Text) || string.IsNullOrEmpty(EditZ3TextBox.Text) ||
                    string.IsNullOrEmpty(EditX4TextBox.Text) || string.IsNullOrEmpty(EditZ4TextBox.Text) ||
                    !ColorHelper.IsColorString.IsMatch(EditColorTextBox.Text))
                {
                    return;
                }

                Zone zone = _listZones[ZonesDataGrid.SelectedIndex];

                zone.Color = EditColorTextBox.Text;
                zone.SourceName = ((ObservableSceneItem)EditSceneItemSelectBox.SelectedItem).SceneItem.SourceName;
                zone.X1 = double.Parse(EditX1TextBox.Text, CultureInfo.InvariantCulture);
                zone.Z1 = double.Parse(EditZ1TextBox.Text, CultureInfo.InvariantCulture);
                zone.X2 = double.Parse(EditX2TextBox.Text, CultureInfo.InvariantCulture);
                zone.Z2 = double.Parse(EditZ2TextBox.Text, CultureInfo.InvariantCulture);
                zone.X3 = double.Parse(EditX3TextBox.Text, CultureInfo.InvariantCulture);
                zone.Z3 = double.Parse(EditZ3TextBox.Text, CultureInfo.InvariantCulture);
                zone.X4 = double.Parse(EditX4TextBox.Text, CultureInfo.InvariantCulture);
                zone.Z4 = double.Parse(EditZ4TextBox.Text, CultureInfo.InvariantCulture);

                Config config = new Config
                {
                    SceneName = ((ObservableScene)PrimarySceneSelectBox.SelectedItem).OBSScene.Name,
                    Zones = _listZones
                };

                _configReader.updateConfigFiles(config);
            }

            ClosePopInButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
        }

        private void EditColorTextBlock_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ColorHelper.IsColorString.IsMatch(EditColorTextBox.Text))
            {
                System.Drawing.Color color = ColorHelper.ToColor(EditColorTextBox.Text);
                EditPreviewColorRectangle.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(color.R, color.G, color.B));
            }
            else
            {
                EditPreviewColorRectangle.Fill = Brushes.Black;
            }
        }

        private void DeleteZone_Click(object sender, RoutedEventArgs e)
        {
            if (ZonesDataGrid.SelectedIndex >= 0)
            {
                _listZones.RemoveAt(ZonesDataGrid.SelectedIndex);

                Config config = new Config
                {
                    SceneName = ((ObservableScene)PrimarySceneSelectBox.SelectedItem).OBSScene.Name,
                    Zones = _listZones
                };

                _configReader.updateConfigFiles(config);
            }

            ClosePopInButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private void ColorValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !ColorHelper.IsAcceptableColorString.IsMatch(((TextBox)sender).Text + e.Text);
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^\\-?[0-9]*\\.?[0-9]*$");
            e.Handled = !regex.IsMatch(((TextBox)sender).Text + e.Text);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_kinectDetector != null)
            {
                _kinectDetector.Stop();
                _kinectDetector = null;
            }
        }

        private void ZonesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListView listView = (ListView)sender;
            Zone zone = (Zone)listView.SelectedItem;

            if (zone != null)
            {
                EditGrid.Visibility = Visibility.Visible;

                foreach (ObservableSceneItem sceneItem in _listSceneItems)
                {
                    if (sceneItem.SceneItem.SourceName == zone.SourceName)
                    {
                        EditSceneItemSelectBox.SelectedItem = sceneItem;
                        break;
                    }
                }

                EditColorTextBox.Text = zone.Color;
                EditX1TextBox.Text = zone.X1.ToString(null, CultureInfo.InvariantCulture);
                EditZ1TextBox.Text = zone.Z1.ToString(null, CultureInfo.InvariantCulture);
                EditX2TextBox.Text = zone.X2.ToString(null, CultureInfo.InvariantCulture);
                EditZ2TextBox.Text = zone.Z2.ToString(null, CultureInfo.InvariantCulture);
                EditX3TextBox.Text = zone.X3.ToString(null, CultureInfo.InvariantCulture);
                EditZ3TextBox.Text = zone.Z3.ToString(null, CultureInfo.InvariantCulture);
                EditX4TextBox.Text = zone.X4.ToString(null, CultureInfo.InvariantCulture);
                EditZ4TextBox.Text = zone.Z4.ToString(null, CultureInfo.InvariantCulture);
            }
        }

        private void ClosePopInButton_Click(object sender, RoutedEventArgs e)
        {
            ZonesDataGrid.SelectedIndex = -1;
            ZonesDataGrid.Items.Refresh();
            EditGrid.Visibility = Visibility.Hidden;
        }

        private void PrimaryScene_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _listSceneItems = _obsLinker.LoadOBSSceneItems((ObservableScene)PrimarySceneSelectBox.SelectedItem);
            
            CurrentSceneItemSelectBox.ItemsSource = _listSceneItems;
            CurrentSceneItemSelectBox.SelectedIndex = 0;

            AddSceneItemSelectBox.ItemsSource = _listSceneItems;
            EditSceneItemSelectBox.ItemsSource = _listSceneItems;
        }

        private void CurrentSceneItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrentSceneItemSelectBox.SelectedItem != null)
            {
                foreach (ObservableSceneItem sceneItem in _listSceneItems)
                {
                    _obsLinker.SetSceneVisibility(
                        ((ObservableScene)PrimarySceneSelectBox.SelectedItem).OBSScene.Name,
                        sceneItem.SceneItem.SourceName,
                        sceneItem.SceneItem.SourceName == ((ObservableSceneItem)CurrentSceneItemSelectBox.SelectedItem).SceneItem.SourceName
                    );
                }
            }
        }
    }
}
