using AForge.Video;
using AForge.Video.DirectShow;
using AutoSwitchCam.Model;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace AutoSwitchCam
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FilterInfoCollection _filterInfoCollection;
        private VideoCaptureDevice _videoCaptureDevice;

        private ObservableCollection<Camera> _listCameras = new ObservableCollection<Camera>();

        private MjpegServer _mjpegServer;

        public MainWindow()
        {
            InitializeComponent();

            _filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            foreach (FilterInfo filterInfo in _filterInfoCollection)
            {
                _listCameras.Add(new Camera() { FilterInfo = filterInfo });
            }

            Cameras.ItemsSource = _listCameras;
            Cameras.SelectedIndex = 0;

            _mjpegServer = new MjpegServer("http://+:80/");
            _mjpegServer.Start();
        }

        private void Camera_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_videoCaptureDevice != null && _videoCaptureDevice.IsRunning)
            {
                _videoCaptureDevice.SignalToStop();
            }

            _videoCaptureDevice = new VideoCaptureDevice(((Camera)Cameras.SelectedItem).FilterInfo.MonikerString);

            foreach (VideoCapabilities videoCapabilities in _videoCaptureDevice.VideoCapabilities)
            {
                if (videoCapabilities.FrameSize.Width <= 1920 && videoCapabilities.FrameSize.Height <= 1080 &&
                    (_videoCaptureDevice.VideoResolution == null || videoCapabilities.FrameSize.Width > _videoCaptureDevice.VideoResolution.FrameSize.Width))
                {
                    _videoCaptureDevice.VideoResolution = videoCapabilities;
                }
            }

            _videoCaptureDevice.NewFrame += videoCaptureNewFrame;
            _videoCaptureDevice.Start();
        }

        private void videoCaptureNewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap cameraImage = eventArgs.Frame;

            MemoryStream imageBuffer = new MemoryStream();

            EncoderParameters encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 100L);
            cameraImage.Save(imageBuffer, GetEncoder(ImageFormat.Jpeg), encoderParameters);

            _mjpegServer.NewFrame(imageBuffer);

            if (Application.Current != null)
            {
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

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _mjpegServer.Stop();

            if (_videoCaptureDevice != null && _videoCaptureDevice.IsRunning)
            {
                _videoCaptureDevice.SignalToStop();
            }
        }
    }
}
