using AForge.Video;
using AForge.Video.DirectShow;
using AutoSwitchCam.Helper;
using AutoSwitchCam.Model;
using AutoSwitchCam.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AutoSwitchCam
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FilterInfoCollection _filterInfoCollection;
        private readonly List<VideoCaptureDevice> _videoCaptureDevices = new List<VideoCaptureDevice>();

        private readonly ConfigReader _configReader = new ConfigReader();

        private readonly ObservableCollection<Camera> _listCameras = new ObservableCollection<Camera>();
        public ObservableCollection<Camera> ListCameras { get { return _listCameras; } }

        private ObservableCollection<Zone> _listZones = new ObservableCollection<Zone>();

        public ObservableCollection<Zone> ListZones
        {
            get
            {
                return _listZones;
            }

            set
            {
                _listZones = value;
            }
        }

        private readonly ObservableCollection<Head> _listHeads = new ObservableCollection<Head>();
        public ObservableCollection<Head> ListHeads { get { return _listHeads; } }

        private MjpegServer _mjpegServer;

        private KinectDetector _kinectDetector;

        public ImageSource BodiesImageSource { get { return _kinectDetector.BodiesImageSource; } }

        public ImageSource HeadsImageSource { get { return _kinectDetector.HeadsImageSource; } }

        public MainWindow()
        {
            InitializeComponent();

            InitCameras();

            _configReader.readConfigFiles(this);

            _mjpegServer = new MjpegServer("http://+:80/");

            _kinectDetector = new KinectDetector(this);
            _kinectDetector.NewPosition += Kinect_NewPosition;

            InitHelpersSelectors();
        }

        private void InitCameras()
        {
            _filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            foreach (FilterInfo filterInfo in _filterInfoCollection)
            {
                _listCameras.Add(new Camera() { FilterInfo = filterInfo });

                VideoCaptureDevice videoCaptureDevice = new VideoCaptureDevice(filterInfo.MonikerString);

                foreach (VideoCapabilities videoCapabilities in videoCaptureDevice.VideoCapabilities)
                {
                    if (videoCapabilities.FrameSize.Width <= 1920 && videoCapabilities.FrameSize.Height <= 1080 &&
                        (videoCaptureDevice.VideoResolution == null || videoCapabilities.FrameSize.Width > videoCaptureDevice.VideoResolution.FrameSize.Width))
                    {
                        videoCaptureDevice.VideoResolution = videoCapabilities;
                    }
                }

                videoCaptureDevice.Start();

                _videoCaptureDevices.Add(videoCaptureDevice);
            }

            CameraStreamSelectBox.SelectedIndex = 0;
            AddCameraZoneSelectBox.SelectedIndex = 0;
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

        private void StartStopStream_Click(object sender, RoutedEventArgs e)
        {
            if (_mjpegServer.IsStarted)
            {
                StartStopStreamButton.Content = "Démarrer le stream";
                _mjpegServer.Stop();
            }
            else
            {
                StartStopStreamButton.Content = "Arrêter le stream";
                _mjpegServer.Start();
            }
        }

        private void StopStream_Click(object sender, RoutedEventArgs e)
        {
            _mjpegServer.Stop();
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

            CameraStreamSelectBox.SelectedIndex = zoneToDisplay?.CameraIndex ?? -1;
        }

        private void CameraStream_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CameraStreamSelectBox.SelectedIndex >= 0)
            {
                foreach (VideoCaptureDevice captureDevice in _videoCaptureDevices)
                {
                    captureDevice.NewFrame -= StreamVideoCapture_NewFrame;
                }

                VideoCaptureDevice videoCaptureDevice = _videoCaptureDevices[CameraStreamSelectBox.SelectedIndex];

                videoCaptureDevice.NewFrame += StreamVideoCapture_NewFrame;
            }
        }

        private void StreamVideoCapture_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (_mjpegServer != null && _mjpegServer.IsStarted)
            {
                MemoryStream imageBuffer = JpegEncoderHelper.BitmapToJpeg(eventArgs.Frame);

                _mjpegServer?.NewFrame(imageBuffer);
            }
        }

        private void CameraZone_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (VideoCaptureDevice captureDevice in _videoCaptureDevices)
            {
                captureDevice.NewFrame -= PreviewVideoCapture_NewFrame;
            }

            VideoCaptureDevice videoCaptureDevice = _videoCaptureDevices[AddCameraZoneSelectBox.SelectedIndex];

            videoCaptureDevice.NewFrame += PreviewVideoCapture_NewFrame;
        }

        private void PreviewVideoCapture_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (_mjpegServer != null && !_mjpegServer.IsStarted)
            {
                MemoryStream imageBuffer = JpegEncoderHelper.BitmapToJpeg(eventArgs.Frame);

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    imageBuffer.Position = 0;

                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = imageBuffer;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();

                    AddPreviewImage.Source = bitmapImage;
                    EditPreviewImage.Source = bitmapImage;
                }));
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
                CameraName = ((Camera)AddCameraZoneSelectBox.SelectedItem).FilterInfo.Name,
                CameraIndex = AddCameraZoneSelectBox.SelectedIndex,
                X1 = double.Parse(AddX1TextBox.Text, CultureInfo.InvariantCulture),
                Z1 = double.Parse(AddZ1TextBox.Text, CultureInfo.InvariantCulture),
                X2 = double.Parse(AddX2TextBox.Text, CultureInfo.InvariantCulture),
                Z2 = double.Parse(AddZ2TextBox.Text, CultureInfo.InvariantCulture),
                X3 = double.Parse(AddX3TextBox.Text, CultureInfo.InvariantCulture),
                Z3 = double.Parse(AddZ3TextBox.Text, CultureInfo.InvariantCulture),
                X4 = double.Parse(AddX4TextBox.Text, CultureInfo.InvariantCulture),
                Z4 = double.Parse(AddZ4TextBox.Text, CultureInfo.InvariantCulture),
            });

            _configReader.updateConfigFiles(this);
        }

        private void AddColorTextBlock_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ColorHelper.IsColorString.IsMatch(AddColorTextBox.Text))
            {
                System.Drawing.Color color = ColorHelper.ToColor(AddColorTextBox.Text);
                AddPreviewColorRectangle.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(color.R, color.G, color.B));
            }
            else
            {
                AddPreviewColorRectangle.Fill = System.Windows.Media.Brushes.Black;
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
                zone.CameraName = ((Camera)EditCameraZoneSelectBox.SelectedItem).FilterInfo.Name;
                zone.CameraIndex = EditCameraZoneSelectBox.SelectedIndex;
                zone.X1 = double.Parse(EditX1TextBox.Text, CultureInfo.InvariantCulture);
                zone.Z1 = double.Parse(EditZ1TextBox.Text, CultureInfo.InvariantCulture);
                zone.X2 = double.Parse(EditX2TextBox.Text, CultureInfo.InvariantCulture);
                zone.Z2 = double.Parse(EditZ2TextBox.Text, CultureInfo.InvariantCulture);
                zone.X3 = double.Parse(EditX3TextBox.Text, CultureInfo.InvariantCulture);
                zone.Z3 = double.Parse(EditZ3TextBox.Text, CultureInfo.InvariantCulture);
                zone.X4 = double.Parse(EditX4TextBox.Text, CultureInfo.InvariantCulture);
                zone.Z4 = double.Parse(EditZ4TextBox.Text, CultureInfo.InvariantCulture);

                _configReader.updateConfigFiles(this);
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
                EditPreviewColorRectangle.Fill = System.Windows.Media.Brushes.Black;
            }
        }

        private void DeleteZone_Click(object sender, RoutedEventArgs e)
        {
            if (ZonesDataGrid.SelectedIndex >= 0)
            {
                _listZones.RemoveAt(ZonesDataGrid.SelectedIndex);

                _configReader.updateConfigFiles(this);
            }

            ClosePopInButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private void ColorValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !ColorHelper.IsAcceptableColorString.IsMatch(((TextBox)sender).Text + e.Text);
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[0-9]*\\.?[0-9]*$");
            e.Handled = !regex.IsMatch(((TextBox)sender).Text + e.Text);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (VideoCaptureDevice videoCaptureDevice in _videoCaptureDevices)
            {
                videoCaptureDevice.SignalToStop();
            }

            if (_kinectDetector != null)
            {
                _kinectDetector.Stop();
                _kinectDetector = null;
            }

            if (_mjpegServer != null)
            {
                _mjpegServer.Stop();
                _mjpegServer = null;
            }
        }

        private void ZonesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListView listView = (ListView)sender;
            Zone zone = (Zone)listView.SelectedItem;

            if (zone != null)
            {
                EditGrid.Visibility = Visibility.Visible;

                EditCameraZoneSelectBox.SelectedIndex = zone.CameraIndex;
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
    }
}
