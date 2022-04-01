using AutoSwitchCam.Model;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AutoSwitchCam
{
    class KinectDetector
    {
        private readonly MainWindow _main;

        public event EventHandler<Head[]> NewPosition;

        private const double FaceRotationIncrementInDegrees = 5.0;
        private const double HeadThickness = 9;
        private const double JointThickness = 3;
        private const float InferredZPositionClamp = 0.1f;
        private const int RatioZ = 100;

        private readonly Brush _trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        private readonly Brush _inferredJointBrush = Brushes.Yellow;
        private readonly Pen _inferredBonePen = new Pen(Brushes.Gray, 1);

        private List<Brush> _zonesColors;

        private WriteableBitmap _colorBitmap = null;

        private int _bodiesCount;
        private Body[] _bodies;
        private List<Brush> _bodiesColors;
        private DrawingGroup _bodiesDrawingGroup;
        private DrawingImage _bodiesImageSource;

        private FaceFrameSource[] _faceFrameSources;
        private FaceFrameReader[] _faceFrameReaders;
        private FaceFrameResult[] _faceFrameResults;

        private Head[] _heads;
        private DrawingGroup _headsDrawingGroup;
        private DrawingImage _headsImageSource;

        private KinectSensor _kinectSensor = null;
        private CoordinateMapper _coordinateMapper = null;
        private MultiSourceFrameReader _multiFrameReader = null;
        private List<Tuple<JointType, JointType>> bones;

        private int _decalageX = 1;
        private double _ratioX = 1;
        private double _ratioY = 1;

        private int _displayWidth;
        private int _displayHeight;
        private int _calculatedMaxHeight;

        public int BodiesCount
        {
            get { return _bodiesCount; }
        }

        public ImageSource BodiesImageSource
        {
            get { return _bodiesImageSource; }
        }

        public ImageSource HeadsImageSource
        {
            get { return _headsImageSource; }
        }

        public List<Brush> BodiesColors
        {
            get { return _bodiesColors; }
        }

        public KinectDetector(MainWindow main)
        {
            _main = main;

            InitBones();
            InitColors();

            _kinectSensor = KinectSensor.GetDefault();

            _coordinateMapper = _kinectSensor.CoordinateMapper;

            FrameDescription frameDescription = _kinectSensor.DepthFrameSource.FrameDescription;
            _displayWidth = frameDescription.Width;
            _displayHeight = frameDescription.Height;
            _calculatedMaxHeight = _displayHeight;

            _decalageX = (frameDescription.Width - frameDescription.Height) / 2;
            _ratioX = (double)frameDescription.Width / frameDescription.Height;

            FrameDescription colorFrameDescription = _kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);

            _colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            _multiFrameReader = _kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Body);

            // set the maximum number of bodies that would be tracked by Kinect
            _bodiesCount = _kinectSensor.BodyFrameSource.BodyCount;

            // allocate storage to store body objects
            _bodies = new Body[_bodiesCount];
            
            // create a face frame source + reader to track each face in the FOV
            _faceFrameSources = new FaceFrameSource[_bodiesCount];
            _faceFrameReaders = new FaceFrameReader[_bodiesCount];
            _faceFrameResults = new FaceFrameResult[_bodiesCount];

            // allocate storage to store head objects
            _heads = new Head[_bodiesCount];

            for (int i = 0; i < _bodiesCount; i++)
            {
                // create the face frame source with the required face frame features and an initial tracking Id of 0
                _faceFrameSources[i] = new FaceFrameSource(_kinectSensor, 0, FaceFrameFeatures.RotationOrientation);

                // open the corresponding reader
                _faceFrameReaders[i] = _faceFrameSources[i].OpenReader();

                _faceFrameReaders[i].FrameArrived += Reader_FaceFrameArrived;
            }

            _kinectSensor.Open();

            _bodiesDrawingGroup = new DrawingGroup();
            _bodiesImageSource = new DrawingImage(_bodiesDrawingGroup);

            _headsDrawingGroup = new DrawingGroup();
            _headsImageSource = new DrawingImage(_headsDrawingGroup);

            if (_multiFrameReader != null)
            {
                _multiFrameReader.MultiSourceFrameArrived += Reader_FrameArrived;
            }
        }

        public void Stop()
        {
            for (int i = 0; i < _bodiesCount; i++)
            {
                if (_faceFrameReaders[i] != null)
                {
                    _faceFrameReaders[i].Dispose();
                    _faceFrameReaders[i] = null;
                }

                if (_faceFrameSources[i] != null)
                {
                    _faceFrameSources[i].Dispose();
                    _faceFrameSources[i] = null;
                }
            }

            if (_multiFrameReader != null)
            {
                _multiFrameReader.Dispose();
                _multiFrameReader = null;
            }

            if (_kinectSensor != null)
            {
                _kinectSensor.Close();
                _kinectSensor = null;
            }
        }

        private void InitBones()
        {
            bones = new List<Tuple<JointType, JointType>>();

            // Torso
            bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));
        }

        private void InitColors()
        {
            _zonesColors = new List<Brush>();
            _zonesColors.Add(new SolidColorBrush(Color.FromArgb(127, 255, 0, 0)));
            _zonesColors.Add(new SolidColorBrush(Color.FromArgb(127, 255, 165, 0)));
            _zonesColors.Add(new SolidColorBrush(Color.FromArgb(127, 0, 128, 0)));
            _zonesColors.Add(new SolidColorBrush(Color.FromArgb(127, 0, 0, 255)));
            _zonesColors.Add(new SolidColorBrush(Color.FromArgb(127, 75, 0, 130)));
            _zonesColors.Add(new SolidColorBrush(Color.FromArgb(127, 238, 130, 238)));

            _bodiesColors = new List<Brush>();
            _bodiesColors.Add(Brushes.Red);
            _bodiesColors.Add(Brushes.Orange);
            _bodiesColors.Add(Brushes.Green);
            _bodiesColors.Add(Brushes.Blue);
            _bodiesColors.Add(Brushes.Indigo);
            _bodiesColors.Add(Brushes.Violet);
        }

        private void Reader_FrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame reference = e.FrameReference.AcquireFrame();

            using (ColorFrame colorFrame = reference.ColorFrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        _colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == _colorBitmap.PixelWidth) && (colorFrameDescription.Height == _colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                _colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra
                            );

                            _colorBitmap.AddDirtyRect(new Int32Rect(0, 0, _colorBitmap.PixelWidth, _colorBitmap.PixelHeight));

                            _calculatedMaxHeight = (int)Math.Floor(_colorBitmap.PixelHeight / ((double)_colorBitmap.PixelWidth / _displayWidth));
                            _ratioY = (double)_displayHeight / _calculatedMaxHeight;
                        }

                        _colorBitmap.Unlock();
                    }
                }
            }

            using (BodyFrame bodyFrame = reference.BodyFrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(_bodies);

                    for (int i = 0; i < _bodiesCount; i++)
                    {
                        Body body = _bodies[i];
                        Head head = _heads[i];

                        if (body != null && body.IsTracked)
                        {
                            if (!_faceFrameSources[i].IsTrackingIdValid)
                            {
                                if (body != null)
                                {
                                    _faceFrameSources[i].TrackingId = body.TrackingId;
                                }
                            }

                            CameraSpacePoint headPosition = body.Joints[JointType.Head].Position;

                            if (head == null)
                            {
                                _heads[i] = new Head
                                {
                                    Color = _bodiesColors[i],
                                    X = headPosition.X,
                                    Y = headPosition.Y,
                                    Z = headPosition.Z
                                };
                            }
                            else
                            {
                                head.X = headPosition.X;
                                head.Y = headPosition.Y;
                                head.Z = headPosition.Z;
                            }
                        }
                        else
                        {
                            _heads[i] = null;
                        }
                    }
                }
            }

            DrawColorAndBodiesFrame();
            DrawHeadsFrame();
        }

        private void Reader_FaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (FaceFrame faceFrame = e.FrameReference.AcquireFrame())
            {
                if (faceFrame != null)
                {
                    // get the index of the face source from the face source array
                    int index = GetFaceSourceIndex(faceFrame.FaceFrameSource);

                    Head head = _heads[index];

                    // check if this face frame has valid face frame results
                    if (faceFrame.FaceFrameResult != null)
                    {
                        // store this face frame result to draw later
                        _faceFrameResults[index] = faceFrame.FaceFrameResult;

                        int yaw;
                        ExtractFaceRotationInDegrees(faceFrame.FaceFrameResult.FaceRotationQuaternion, out _, out yaw, out _);

                        if (head == null)
                        {
                            _heads[index] = new Head
                            {
                                Color = _bodiesColors[index],
                                Angle = yaw
                            };
                        }
                        else
                        {
                            head.Angle = yaw;
                        }
                    }
                    else
                    {
                        // indicates that the latest face frame result from this reader is invalid
                        _faceFrameResults[index] = null;

                        if (head != null)
                        {
                            head.Angle = null;
                        }
                    }
                }
            }
        }

        private void DrawColorAndBodiesFrame()
        {
            using (DrawingContext dc = _bodiesDrawingGroup.Open())
            {
                if (_colorBitmap != null)
                {
                    dc.DrawImage(_colorBitmap, new Rect(0.0, 0, _displayWidth, _calculatedMaxHeight));
                }
                else
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, _displayWidth, _calculatedMaxHeight));
                }

                for (int i = 0; i < _bodiesCount; i++)
                {
                    Body body = _bodies[i];
                    Pen drawPen = new Pen(_bodiesColors[i], 6);

                    if (body != null && body.IsTracked)
                    {
                        IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                        // convert the joint points to depth (display) space
                        Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                        foreach (JointType jointType in joints.Keys)
                        {
                            // sometimes the depth(Z) of an inferred joint may show as negative
                            // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                            CameraSpacePoint position = joints[jointType].Position;
                            if (position.Z < 0)
                            {
                                position.Z = InferredZPositionClamp;
                            }

                            DepthSpacePoint depthSpacePoint = _coordinateMapper.MapCameraPointToDepthSpace(position);
                            jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                        }

                        DrawBody(joints, jointPoints, dc, drawPen);
                    }
                }

                // prevent drawing outside of our render area
                _bodiesDrawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, _displayWidth, _calculatedMaxHeight));
            }
        }

        private void DrawHeadsFrame()
        {
            using (DrawingContext dc = _headsDrawingGroup.Open())
            {
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, _displayWidth, _calculatedMaxHeight));

                for (int i = 0; i < _main.Get_ListZones().Count; i++)
                {
                    Zone zone = _main.Get_ListZones()[i];
                    Brush drawBrush = _zonesColors[i]; // @TODO: Can be > than lenght !

                    Point point1 = new Point(
                        x: (_displayWidth / 2) + (zone.X1 * (_displayWidth / 2)),
                        y: zone.Z1 * RatioZ
                    );

                    Point point2 = new Point(
                        x: (_displayWidth / 2) + (zone.X2 * (_displayWidth / 2)),
                        y: zone.Z2 * RatioZ
                    );

                    Point point3 = new Point(
                        x: (_displayWidth / 2) + (zone.X3 * (_displayWidth / 2)),
                        y: zone.Z3 * RatioZ
                    );

                    Point point4 = new Point(
                        x: (_displayWidth / 2) + (zone.X4 * (_displayWidth / 2)),
                        y: zone.Z4 * RatioZ
                    );

                    StreamGeometry geometry = new StreamGeometry();

                    using (StreamGeometryContext ctx = geometry.Open())
                    {
                        ctx.BeginFigure(point1, true, true);
                        ctx.LineTo(point2, true, false);
                        ctx.LineTo(point3, true, false);
                        ctx.LineTo(point4, true, false);
                    }

                    dc.DrawGeometry(drawBrush, null, geometry);
                }

                for (int i = 0; i < _bodiesCount; i++)
                {
                    Head head = _heads[i];
                    Brush drawBrush = _bodiesColors[i];

                    if (head != null && head.X != null && head.Z != null)
                    {
                        Point point = new Point(
                            x: (_displayWidth / 2) + (head.X.Value * (_displayWidth / 2)),
                            y: head.Z.Value * RatioZ
                        );

                        dc.DrawEllipse(drawBrush, null, point, HeadThickness, HeadThickness);
                    }
                }

                _headsDrawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, _displayWidth, _calculatedMaxHeight));
            }

            NewPosition?.Invoke(this, _heads);
        }

        private int GetFaceSourceIndex(FaceFrameSource faceFrameSource)
        {
            int index = -1;

            for (int i = 0; i < _bodiesCount; i++)
            {
                if (_faceFrameSources[i] == faceFrameSource)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in bones)
            {
                DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = _trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = _inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, CalclulatePoint(jointPoints[jointType]), JointThickness, JointThickness);
                }
            }
        }

        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked || joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = _inferredBonePen;

            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, CalclulatePoint(jointPoints[jointType0]), CalclulatePoint(jointPoints[jointType1]));
        }

        private Point CalclulatePoint(Point point)
        {
            return new Point(_decalageX + point.X / _ratioX, point.Y / _ratioY);
        }

        private static void ExtractFaceRotationInDegrees(Vector4 rotQuaternion, out int pitch, out int yaw, out int roll)
        {
            double x = rotQuaternion.X;
            double y = rotQuaternion.Y;
            double z = rotQuaternion.Z;
            double w = rotQuaternion.W;

            // convert face rotation quaternion to Euler angles in degrees
            double yawD, pitchD, rollD;
            pitchD = Math.Atan2(2 * ((y * z) + (w * x)), (w * w) - (x * x) - (y * y) + (z * z)) / Math.PI * 180.0;
            yawD = Math.Asin(2 * ((w * y) - (x * z))) / Math.PI * 180.0;
            rollD = Math.Atan2(2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z)) / Math.PI * 180.0;

            // clamp the values to a multiple of the specified increment to control the refresh rate
            double increment = FaceRotationIncrementInDegrees;
            pitch = (int)(Math.Floor((pitchD + ((increment / 2.0) * (pitchD > 0 ? 1.0 : -1.0))) / increment) * increment);
            yaw = (int)(Math.Floor((yawD + ((increment / 2.0) * (yawD > 0 ? 1.0 : -1.0))) / increment) * increment);
            roll = (int)(Math.Floor((rollD + ((increment / 2.0) * (rollD > 0 ? 1.0 : -1.0))) / increment) * increment);
        }
    }
}
