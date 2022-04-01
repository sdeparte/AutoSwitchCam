using AForge.Video;
using AForge.Video.DirectShow;
using AutoSwitchCam.Helper;
using AutoSwitchCam.Model;
using AutoSwitchCam.Services;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
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
        private int _currentStreamIndex = -1;

        private readonly ConfigReader _configReader = new ConfigReader();

        private readonly ObservableCollection<Camera> _listCameras = new ObservableCollection<Camera>();
        private ObservableCollection<Zone> _listZones = new ObservableCollection<Zone>();
        private readonly ObservableCollection<Head> _listHeads = new ObservableCollection<Head>();

        private MjpegServer _mjpegServer;

        private KinectDetector _kinectDetector;

        public ImageSource BodiesImageSource
        {
            get
            {
                return _kinectDetector.BodiesImageSource;
            }
        }

        public ImageSource HeadsImageSource
        {
            get
            {
                return _kinectDetector.HeadsImageSource;
            }
        }

        public ObservableCollection<Zone> Get_ListZones()
        {
            return _listZones;
        }

        public void Set_ListZones(ObservableCollection<Zone> listZones)
        {
            _listZones = listZones;
        }

        public MainWindow()
        {
            InitializeComponent();

            InitCameras();

            _configReader.readConfigFiles(this);

            _mjpegServer = new MjpegServer("http://+:80/");
            _mjpegServer.Start();

            _kinectDetector = new KinectDetector(this);
            _kinectDetector.NewPosition += Kinect_NewPosition;

            ZonesDataGrid.ItemsSource = _listZones;
            HeadsDataGrid.ItemsSource = _listHeads;

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

            SwitchStreamToCamIndex(0);

            CamerasSelectBox.ItemsSource = _listCameras;
            CamerasSelectBox.SelectedIndex = 0;
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

            SwitchStreamToCamIndex(zoneToDisplay.CameraIndex);
        }

        private void SwitchStreamToCamIndex(int index)
        {
            if (_currentStreamIndex != index)
            {
                _currentStreamIndex = index;

                foreach (VideoCaptureDevice captureDevice in _videoCaptureDevices)
                {
                    captureDevice.NewFrame -= StreamVideoCapture_NewFrame;
                }

                VideoCaptureDevice videoCaptureDevice = _videoCaptureDevices[_currentStreamIndex];

                videoCaptureDevice.NewFrame += StreamVideoCapture_NewFrame;
            }
        }

        private void StreamVideoCapture_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            MemoryStream imageBuffer = JpegEncoderHelper.BitmapToJpeg(eventArgs.Frame);

            _mjpegServer?.NewFrame(imageBuffer);
        }

        private void Camera_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (VideoCaptureDevice captureDevice in _videoCaptureDevices)
            {
                captureDevice.NewFrame -= PreviewVideoCapture_NewFrame;
            }

            VideoCaptureDevice videoCaptureDevice = _videoCaptureDevices[CamerasSelectBox.SelectedIndex];

            videoCaptureDevice.NewFrame += PreviewVideoCapture_NewFrame;
        }

        private void PreviewVideoCapture_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (Application.Current != null)
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

                    Image.Source = bitmapImage;
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
                    X1.Text = head.X.ToString();
                    Z1.Text = head.Z.ToString();
                    break;

                case 1:
                    X2.Text = head.X.ToString();
                    Z2.Text = head.Z.ToString();
                    break;

                case 2:
                    X3.Text = head.X.ToString();
                    Z3.Text = head.Z.ToString();
                    break;

                case 3:
                    X4.Text = head.X.ToString();
                    Z4.Text = head.Z.ToString();
                    break;
            }
        }
        
        private void AddZone_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(X1.Text) || string.IsNullOrEmpty(Z1.Text) ||
                string.IsNullOrEmpty(X2.Text) || string.IsNullOrEmpty(Z2.Text) ||
                string.IsNullOrEmpty(X3.Text) || string.IsNullOrEmpty(Z3.Text) ||
                string.IsNullOrEmpty(X4.Text) || string.IsNullOrEmpty(Z4.Text))
            {
                return;
            }

            _listZones.Add(new Zone()
            {
                CameraName = ((Camera)CamerasSelectBox.SelectedItem).FilterInfo.Name,
                CameraIndex = CamerasSelectBox.SelectedIndex,
                X1 = Convert.ToDouble(X1.Text),
                Z1 = Convert.ToDouble(Z1.Text),
                X2 = Convert.ToDouble(X2.Text),
                Z2 = Convert.ToDouble(Z2.Text),
                X3 = Convert.ToDouble(X3.Text),
                Z3 = Convert.ToDouble(Z3.Text),
                X4 = Convert.ToDouble(X4.Text),
                Z4 = Convert.ToDouble(Z4.Text),
            });
            _configReader.updateConfigFiles(this);
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[0-9]*\\.?[0-9]*q$");
            e.Handled = regex.IsMatch(((TextBox)sender).Text + e.Text);
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
    }
}
