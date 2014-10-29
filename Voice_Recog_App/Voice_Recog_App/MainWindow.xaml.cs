using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.ComponentModel;
using System.IO;
using Microsoft.Speech;
using Microsoft.Speech.Recognition;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Synthesis;
using Microsoft.Speech.Text;
using Microsoft.Speech.Internal;
using System.Reflection;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Media.Effects;
using Coding4Fun.Kinect.Wpf.Controls;
using System.Runtime.InteropServices;
using System.Threading;
using Coding4Fun.Kinect.Wpf;
using System.Windows.Threading;
using System.Diagnostics;
using KinectMouseController;
using System.Drawing;
using Kinect.Toolbox;
using Kinect_Jigsaw1;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.Controls;
using Microsoft.Win32;



namespace Voice_Recog_App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        #region MemberVariable
        KinectSensor _kinectSensor;
        SpeechRecognitionEngine _sre;
        KinectAudioSource _source;
        double int_confidence;
        private Skeleton[] skel;
        bool _isQuit=false;
        private bool m1;
        private bool m2;
        private bool m3;
        private bool mmouse;
        private int m1_count;
        private int m1_count1;
        private int last;
        public event PropertyChangedEventHandler PropertyChanged;
        private const double ScrollErrorMargin = 0.001;

        private const int PixelScrollByAmount = 20;

        private readonly KinectSensorChooser sensorChooser;

        public Joint hand = new Joint();
        HoverButton h;
        bool closing = false;
        const int skeletonCount = 6;
        public static Skeleton[] allSkeletons = new Skeleton[skeletonCount];
        public const float SkeletonMaxX = 0.60f;
        public const float SkeletonMaxY = 0.40f;


        #endregion MemberVariable

        #region Variable
        private bool camMode;
        private int file_count;
        private bool modeOn;
        private const float MIN_THRESHOLD = 0.1f;
        private const float MAX_THRESHOLD = 1f;
        private int r_const;
        private int g_const;
        private int b_const;
        private int capture;
        private int fine_count;
        private int mode;
        private int m3_count;
        #endregion Variable
        public MainWindow()
        {
            InitializeComponent();
            this.sensorChooser = new KinectSensorChooser();
            this.sensorChooser.KinectChanged += SensorChooserOnKinectChanged;
            //this.sensorChooserUi.KinectSensorChooser = this.sensorChooser;
            this.sensorChooser.Start();
            this.camMode = true;
            this.mode = 0;
            this.last = 4;
            this.modeOn = false;
            this.r_const = 0x00;
            this.g_const = 0x00;
            this.b_const = 0x00;
            this.capture = 0;
            this.fine_count = 5;
            this.m3_count = 30;

            this.DataContext = this;
            this.file_count = 0;
            this.m1 = false;
            this.m2 = false;
            this.m3 = false;
            this.mmouse = false;
            this.m1_count = 25;
            this.m1_count1 = 15;
            this.Unloaded += delegate
            {
                _kinectSensor.ColorStream.Disable();
                _kinectSensor.SkeletonFrameReady -= KinectDevice_SkeletonFrameReady;
                _kinectSensor.ColorFrameReady -= KinectDevice_ColorFrameReady;
                _kinectSensor.SkeletonStream.Disable();
                _sre.RecognizeAsyncCancel();
                _sre.RecognizeAsyncStop();
            };
            this.Loaded += delegate
            {
                _kinectSensor = KinectSensor.KinectSensors[0];
                _kinectSensor.SkeletonStream.Enable(new TransformSmoothParameters());
                _kinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.skel = new Skeleton[this._kinectSensor.SkeletonStream.FrameSkeletonArrayLength];
                _kinectSensor.SkeletonFrameReady += KinectDevice_SkeletonFrameReady;
                _kinectSensor.ColorFrameReady += KinectDevice_ColorFrameReady;
                //_kinectSensor.
                _kinectSensor.Start();
                StartSpeechRecognition();
            };
        }

        #region MouseControl
        public Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return null;
                }


                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                //get the first tracked skeleton
                Skeleton first = (from s in allSkeletons
                                  where s.TrackingState == SkeletonTrackingState.Tracked
                                  select s).FirstOrDefault();

                return first;

            }
        }
        private void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs args)
        {
            if (args.OldSensor != null)
            {
                try
                {
                    args.OldSensor.DepthStream.Range = DepthRange.Default;
                    args.OldSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    args.OldSensor.DepthStream.Disable();
                    args.OldSensor.SkeletonStream.Disable();
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            if (args.NewSensor != null)
            {
                try
                {
                    args.NewSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                    args.NewSensor.SkeletonStream.Enable();

                    try
                    {

                        if (args.NewSensor.IsRunning == true)
                        {
                            // args.NewSensor.DepthStream.Range = DepthRange.Near;
                            args.NewSensor.SkeletonStream.EnableTrackingInNearRange = true;
                            args.NewSensor.AllFramesReady += NewSensor_AllFramesReady;
                        }

                    }
                    catch (InvalidOperationException)
                    {
                        // Non Kinect for Windows devices do not support Near mode, so reset back to default mode.
                        args.NewSensor.DepthStream.Range = DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }
        }
        private void NewSensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {


            //Get a skeleton
            Skeleton first = GetFirstSkeleton(e);

            if (first == null)
            {
                return;
            }

            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {

                if (skeletonFrameData == null)
                {
                    return;
                }

                Skeleton[] allSkeletons = new Skeleton[skeletonFrameData.SkeletonArrayLength];

                skeletonFrameData.CopySkeletonDataTo(allSkeletons);


                foreach (Skeleton sd in allSkeletons)
                {
                    // the first found/tracked skeleton moves the mouse cursor
                    if (sd.TrackingState == SkeletonTrackingState.Tracked)
                    {


                        // make sure both hands are tracked
                        if (sd.Joints[JointType.HandLeft].TrackingState == JointTrackingState.Tracked &&
                            sd.Joints[JointType.HandRight].TrackingState == JointTrackingState.Tracked)
                        {

                            int cursorX, cursorY;

                            // get the left and right hand Joints
                            Joint jointRight = sd.Joints[JointType.HandRight];
                            Joint jointLeft = sd.Joints[JointType.HandLeft];

                            // scale those Joints to the primary screen width and height
                            Joint scaledRight = jointRight.ScaleTo((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, SkeletonMaxX, SkeletonMaxY);
                            Joint scaledLeft = jointLeft.ScaleTo((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, SkeletonMaxX, SkeletonMaxY);

                            /*  if (LeftHand.IsChecked.GetValueOrDefault())
                              {

                                  cursorX = (int)scaledRight.Position.X;
                                  cursorY = (int)scaledRight.Position.Y;

                              }
                              else
                              {*/
                            cursorX = (int)scaledRight.Position.X;
                            cursorY = (int)scaledRight.Position.Y;
                            // }



                            if (this.mmouse) 
                            { 
                                NativeMethods.SendMouseInput(cursorX, cursorY, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, false, false);
                                runevent(first, e);
                            }
                            return;
                        }
                    }
                }

                Thread.Sleep(1);
                //  throw new NotImplementedException();
            }
        }
        void runevent(Skeleton first, AllFramesReadyEventArgs e)
        {
            int cursorX, cursorY;
            Joint wristLeft = first.Joints[JointType.WristLeft];
            Joint handRight = first.Joints[JointType.HandRight];
            Joint handLeft = first.Joints[JointType.HandLeft];
            Joint shoulderRight = first.Joints[JointType.ShoulderRight];
            Joint shoulderLeft = first.Joints[JointType.ShoulderLeft];
            Joint hipLeft = first.Joints[JointType.HipLeft];
            Joint hipRight = first.Joints[JointType.HipRight];
            Joint kneeLeft = first.Joints[JointType.KneeLeft];
            // scale those Joints to the primary screen width and height
            Joint scaledRight = handRight.ScaleTo((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, SkeletonMaxX, SkeletonMaxY);
            Joint scaledLeft = handLeft.ScaleTo((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, SkeletonMaxX, SkeletonMaxY);

            cursorX = (int)scaledRight.Position.X;
            cursorY = (int)scaledRight.Position.Y;


            float var = 0.2f;
            if (wristLeft.Position.Y - shoulderLeft.Position.Y > 0.2f)
            {
                Thread.Sleep(80);
                //MessageBox.Show("Left Clicked");
                NativeMethods.SendMouseInput(cursorX, cursorY, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, true, false);
            }
            if (jointDistance(wristLeft, shoulderLeft) <= var)
            {
                Thread.Sleep(80);
                //MessageBox.Show("Right Clicked");
                NativeMethods.SendMouseInput(cursorX, cursorY, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, false, true);
            }


        }

        // Gestures detection for controlling clicks
        private float jointDistance(Joint first, Joint second)
        {
            float dX = first.Position.X - second.Position.X;
            float dY = first.Position.Y - second.Position.Y;
            float dZ = first.Position.Z - second.Position.Z;

            return (float)Math.Sqrt((dX * dX) + (dY * dY) + (dZ * dZ));
        }


        #endregion MouseControl

        private void KinectDevice_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    byte[] pixelData = new byte[frame.PixelDataLength];
                    frame.CopyPixelDataTo(pixelData);
                    switch (this.mode)
                    {
                        case 0:
                            break;
                        case 1:
                            for (int i = 0; i < pixelData.Length; i += frame.BytesPerPixel)
                            {
                                pixelData[i] = (byte)(this.r_const); //Blue
                                pixelData[i + 1] = (byte)(this.g_const); //Green
                            }
                            break;
                        case 2:
                            for (int i = 0; i < pixelData.Length; i += frame.BytesPerPixel)
                            {
                                pixelData[i] = (byte)(this.r_const); //Blue
                                pixelData[i + 2] = (byte)(this.b_const); //Green
                            }
                            break;
                        case 3:
                            for (int i = 0; i < pixelData.Length; i += frame.BytesPerPixel)
                            {
                                pixelData[i + 2] = (byte)(this.b_const); //Blue
                                pixelData[i + 1] = (byte)(this.g_const); //Green
                            }
                            break;
                        case 4:
                            for (int i = 0; i < pixelData.Length; i += frame.BytesPerPixel)
                            {
                                pixelData[i] = (byte)(~pixelData[i] + this.r_const);
                                pixelData[i + 1] = (byte)(~pixelData[i + 1] + this.g_const);
                                pixelData[i + 2] = (byte)(~pixelData[i + 2] + this.b_const);
                            }
                            break;
                        case 5:
                            for (int i = 0; i < pixelData.Length; i += frame.BytesPerPixel)
                            {
                                pixelData[i] = (byte)(pixelData[i + 1] + (byte)(this.r_const));
                                pixelData[i + 1] = (byte)(pixelData[i] + (byte)(this.g_const));
                                pixelData[i + 2] = (byte)(~pixelData[i + 2] + this.b_const);
                            }
                            break;
                        case 6:
                            for (int i = 0; i < pixelData.Length; i += frame.BytesPerPixel)
                            {
                                byte gray = Math.Max(pixelData[i], pixelData[i + 1]);
                                gray = Math.Max(gray, pixelData[i + 2]);
                                pixelData[i] = (byte)(gray + (byte)(this.r_const));
                                pixelData[i + 1] = (byte)(gray + (byte)(this.g_const));
                                pixelData[i + 2] = (byte)(gray + (byte)(this.b_const));
                            }
                            break;
                        case 7:
                            for (int i = 0; i < pixelData.Length; i += frame.BytesPerPixel)
                            {
                                byte gray = Math.Min(pixelData[i], pixelData[i + 1]);
                                gray = Math.Min(gray, pixelData[i + 2]);
                                pixelData[i] = (byte)(gray + (byte)(this.r_const));
                                pixelData[i + 1] = (byte)(gray + (byte)(this.g_const));
                                pixelData[i + 2] = (byte)(gray + (byte)(this.b_const));
                            }
                            break;
                        case 8:
                            for (int i = 0; i < pixelData.Length; i += frame.BytesPerPixel)
                            {
                                double gray = (pixelData[i] * 0.11) + (pixelData[i + 1] * 0.59) + (pixelData[i + 2] * 0.3);
                                double desaturation = 0.75;
                                pixelData[i] = (byte)(pixelData[i] + desaturation * (gray - pixelData[i]) + this.r_const);
                                pixelData[i + 1] = (byte)(pixelData[i + 1] + desaturation * (gray - pixelData[i + 1]) + this.g_const);
                                pixelData[i + 2] = (byte)(pixelData[i + 2] + desaturation * (gray - pixelData[i + 2]) + this.b_const);
                            }
                            break;
                        case 9:
                            for (int i = 0; i < pixelData.Length; i += frame.BytesPerPixel)
                            {
                                if (pixelData[i] < 0x33 || pixelData[i] > 0xE5)
                                {
                                    pixelData[i] = 0x00;
                                }
                                else
                                {
                                    pixelData[i] = 0xFF;
                                }
                                if (pixelData[i + 1] < 0x33 || pixelData[i + 1] > 0xE5)
                                {
                                    pixelData[i + 1] = 0x00;
                                }
                                else
                                {
                                    pixelData[i + 1] = 0xFF;
                                }
                                if (pixelData[i + 2] < 0x33 || pixelData[i + 2] > 0xE5)
                                {
                                    pixelData[i + 2] = 0x00;
                                }
                                else
                                {
                                    pixelData[i + 2] = 0xFF;
                                }
                            }
                            break;
                    }
                    if (this.capture < 15 && !camMode && this.capture > 7)
                    {
                        for (int i = 0; i < pixelData.Length; i += frame.BytesPerPixel)
                        {
                            pixelData[i] = 0xff;
                            pixelData[i + 1] = 0xff;
                            pixelData[i + 2] = 0xff;
                        }
                    }
                    Self.Source = BitmapImage.Create(frame.Width, frame.Height, 196, 196,
                    PixelFormats.Bgr32, null, pixelData,
                    frame.Width * frame.BytesPerPixel);

                }
            }
        }

        #region m3Update
        public void m3_Update(Skeleton[] skeletons)
        {
            if (skeletons != null)
            {
                Skeleton skeleton;
                for (int i = 0; i < skeletons.Length; i++)
                {
                    skeleton = skeletons[i];
                    if (skeleton.TrackingState != SkeletonTrackingState.NotTracked)
                    {
                        m3_TrackGesture(skeleton);
                    }
                }
            }
        }

        public void m3_Update1(Skeleton[] skeletons)
        {
            if (skeletons != null)
            {
                Skeleton skeleton;
                for (int i = 0; i < skeletons.Length; i++)
                {
                    skeleton = skeletons[i];
                    if (skeleton.TrackingState != SkeletonTrackingState.NotTracked)
                    {
                        m3_TrackGesture1(skeleton);
                    }
                }
            }
        }
        #region TrackGesture
        private void m3_TrackGesture(Skeleton skeleton)
        {
            //            PixelDepth4.Text = string.Format(mode + "");
            Joint lhand = skeleton.Joints[JointType.HandLeft];
            Joint rhand = skeleton.Joints[JointType.HandRight];
            Joint lelbow = skeleton.Joints[JointType.ElbowLeft];
            Joint relbow = skeleton.Joints[JointType.ElbowRight];
            Joint head = skeleton.Joints[JointType.Head];
            Joint spine = skeleton.Joints[JointType.Spine];
            if (!this.modeOn && this.camMode)
            {
                //PixelDepth.Text = string.Format("Mode off!");
                if (lhand.Position.Y - head.Position.Y > MIN_THRESHOLD && rhand.Position.Y - head.Position.Y > MIN_THRESHOLD)
                {
                    modeOn = true;
                    Panel.SetZIndex(Self, 3);
                    Panel.SetZIndex(Prev, 3);
                    Panel.SetZIndex(Back_Img, 2);
                    //Canvas.SetZIndex(Self, 3);
                    //Canvas.SetZIndex(Prev, 3);
                    //Canvas.SetZIndex(Back_Img, 2);

                    
                }
                else if (spine.Position.Z - rhand.Position.Z > MAX_THRESHOLD / 3 && spine.Position.Z - lhand.Position.Z > MAX_THRESHOLD / 3 && Math.Abs(lhand.Position.X - rhand.Position.X) < 2 * MIN_THRESHOLD)
                {
                    this.capture = 150;
                    this.camMode = false;
                }
            }
            else if (this.camMode)
            {

                //PixelDepth.Text = string.Format("mode on!");
                if (lelbow.Position.X - rhand.Position.X > MIN_THRESHOLD)
                {
                    //PixelDepth4.Text = string.Format(mode + " changing 1");
                    this.mode = (this.mode + 1) % 10;
                    this.r_const = 0x00;
                    this.g_const = 0x00;
                    this.b_const = 0x00;
                }
                else if (lhand.Position.X - relbow.Position.X > MIN_THRESHOLD)
                {
                    //PixelDepth4.Text = string.Format(mode + " changing 2");
                    this.mode = (this.mode - 1) % 10;
                    this.r_const = 0x00;
                    this.g_const = 0x00;
                    this.b_const = 0x00;
                }
                else if (lhand.Position.Y < lelbow.Position.Y && rhand.Position.Y < relbow.Position.Y && lhand.Position.X > rhand.Position.X && relbow.Position.X > lelbow.Position.X && rhand.Position.X > lelbow.Position.X && lhand.Position.X < relbow.Position.X)
                {
                    Panel.SetZIndex(Self, 2);
                    Panel.SetZIndex(Prev, 2);
                    Panel.SetZIndex(Back_Img, 3);
                    //Canvas.SetZIndex(Self, 2);
                    //Canvas.SetZIndex(Prev, 2);
                    //Canvas.SetZIndex(Back_Img, 3);
                    this.modeOn = false;
                }
                else if (spine.Position.Z - rhand.Position.Z > MAX_THRESHOLD / 3 && spine.Position.Z - lhand.Position.Z > MAX_THRESHOLD / 3 && Math.Abs(lhand.Position.X - rhand.Position.X) < 2 * MIN_THRESHOLD)
                {
                    this.capture = 150;
                    this.camMode = false;
                }
            }
            //    else if (!camMode&&modeOn)
            //    {
            //        //for (int i = 0; i < 100000000; i++)
            //        //{
            //        //    PixelDepth.Text = string.Format((int)(i/100000000) + "");
            //        //}

            //        string fileName = filepath.Text;
            //        if (fileName == "")
            //        {
            //            fileName = "snapshot" + file_count +".jpg";
            //            file_count = file_count + 1;
            //        }
            //        if (File.Exists(fileName))
            //        {
            //            File.Delete(fileName);
            //        }
            //        using (FileStream savedSnapshot = new FileStream(fileName, FileMode.CreateNew))
            //        {
            //            BitmapSource image = (BitmapSource)ColorImageElement.Source;
            //            JpegBitmapEncoder jpgEncoder = new JpegBitmapEncoder();
            //            jpgEncoder.QualityLevel = 70;
            //            jpgEncoder.Frames.Add(BitmapFrame.Create(image));
            //            jpgEncoder.Save(savedSnapshot);
            //            savedSnapshot.Flush();
            //            savedSnapshot.Close();
            //            savedSnapshot.Dispose();
            //        }
            //        camMode = true;
            //    }
        }

        private void m3_TrackGesture1(Skeleton skeleton)
        {
            Joint lhand = skeleton.Joints[JointType.HandLeft];
            Joint rhand = skeleton.Joints[JointType.HandRight];
            Joint lelbow = skeleton.Joints[JointType.ElbowLeft];
            Joint relbow = skeleton.Joints[JointType.ElbowRight];
            Joint head = skeleton.Joints[JointType.Head];
            Joint spine = skeleton.Joints[JointType.Spine];
            Joint rknee = skeleton.Joints[JointType.KneeRight];
            Joint lknee = skeleton.Joints[JointType.KneeLeft];
            if (!this.modeOn && this.camMode)
            {
                //test.Text = string.Format("Inactive");
                //PixelDepth.Text = string.Format("Mode off 1!");
                if (lhand.Position.Y - head.Position.Y > MIN_THRESHOLD && rhand.Position.Y - head.Position.Y > MIN_THRESHOLD)
                {
                    //Self.
////                    Canvas newCanvas = MainStage;
//                    Canvas.SetZIndex(Self, 3);
//                    Canvas.SetZIndex(Prev, 3);
                    //Canvas.SetZIndex(Back_Img, 2);
                    this.modeOn = true;
                    Panel.SetZIndex(Self, 3);
                    Panel.SetZIndex(Prev, 3);
                    Panel.SetZIndex(Back_Img, 2);
                }
                else if (spine.Position.Z - rhand.Position.Z > MAX_THRESHOLD / 3 && spine.Position.Z - lhand.Position.Z > MAX_THRESHOLD / 3 && Math.Abs(lhand.Position.X - rhand.Position.X) < 2 * MIN_THRESHOLD)
                {
                    this.capture = 150;
                    this.camMode = false;
                }
            }
            else if (this.camMode)
            {
                //test.Text = string.Format("Active");
                //PixelDepth.Text = string.Format("Mode on 1!");
                if (lhand.Position.Y > head.Position.Y && rhand.Position.Y < head.Position.Y && rhand.Position.Y > relbow.Position.Y)
                {
                    //  PixelDepth.Text = string.Format("R changing!");
                    this.r_const = (this.r_const + 1) % 0xff;
                }
                else if (lhand.Position.Y > head.Position.Y && rhand.Position.Y < head.Position.Y && relbow.Position.Y > rhand.Position.Y)
                {
                    this.r_const = (this.r_const - 1) % 0xff;
                }
                else if (rhand.Position.Y > head.Position.Y && lhand.Position.Y < head.Position.Y && lhand.Position.Y > lelbow.Position.Y)
                {
                    //PixelDepth.Text = string.Format("G changing!");
                    this.g_const = (this.g_const + 1) % 0xff;
                }
                else if (rhand.Position.Y > head.Position.Y && lhand.Position.Y < head.Position.Y && lelbow.Position.Y > lhand.Position.Y)
                {
                    this.g_const = (this.g_const - 1) % 0xff;
                }
                else if (rhand.Position.X > spine.Position.X && lhand.Position.X < spine.Position.X && rhand.Position.Y > relbow.Position.Y && lhand.Position.Y > lelbow.Position.Y)
                {
                    //PixelDepth.Text = string.Format("B changing!");
                    this.b_const = (this.b_const + 1) % 0xff;
                }
                else if (rhand.Position.Y < rknee.Position.Y && lhand.Position.Y < lknee.Position.Y)
                {
                    this.b_const = (this.b_const - 1) % 0xff;
                }
                else if (lhand.Position.Y < lelbow.Position.Y && rhand.Position.Y < relbow.Position.Y && lhand.Position.X > rhand.Position.X && relbow.Position.X > lelbow.Position.X && rhand.Position.X > lelbow.Position.X && lhand.Position.X < relbow.Position.X)
                {
                    Panel.SetZIndex(Self, 2);
                    Panel.SetZIndex(Prev, 2);
                    Panel.SetZIndex(Back_Img, 3);
                    //Canvas.SetZIndex(Self, 2);
                    //Canvas.SetZIndex(Prev, 2);
                    //Canvas.SetZIndex(Back_Img, 3);
                    //PixelDepth.Text = string.Format("switching off!");
                    this.modeOn = false;
                }
                else if (spine.Position.Z - rhand.Position.Z > MAX_THRESHOLD / 3 && spine.Position.Z - lhand.Position.Z > MAX_THRESHOLD / 3 && Math.Abs(lhand.Position.X - rhand.Position.X) < 2 * MIN_THRESHOLD)
                {
                    this.capture = 150;
                    this.camMode = false;
                }
            }
            //            else if (!camMode&& modeOn)
            //            {
            //                this.capture = 300;
            //                //for (int i = 0; i < 100000000; i++)
            //                //{
            //                //    PixelDepth.Text = string.Format(i / 100000000 + "");
            //                //}
            ////                Thread.Sleep(10000);
            //                string fileName = filepath.Text;
            //                if (fileName == "")
            //                {
            //                    fileName = "snapshot" + file_count + ".jpg";
            //                    file_count = file_count + 1;
            //                }
            //                if (File.Exists(fileName))
            //                {
            //                    File.Delete(fileName);
            //                }
            //                using (FileStream savedSnapshot = new FileStream(fileName, FileMode.CreateNew))
            //                {
            //                    BitmapSource image = (BitmapSource)ColorImageElement.Source;
            //                    JpegBitmapEncoder jpgEncoder = new JpegBitmapEncoder();
            //                    jpgEncoder.QualityLevel = 70;
            //                    jpgEncoder.Frames.Add(BitmapFrame.Create(image));
            //                    jpgEncoder.Save(savedSnapshot);
            //                    savedSnapshot.Flush();
            //                    savedSnapshot.Close();
            //                    savedSnapshot.Dispose();
            //                }
            //                camMode = true;
            //            }
        }





        #endregion TrackGesture
        #endregion m3Update



        #region CaptureImage
        private void Capture(int countdown)
        {
            if (countdown == 4 && this.m3)
            {
                //string fileName = filepath.Text;
                //if (fileName == "")
                //{
                string fileName = "snapshot" + this.file_count + ".jpg";
                this.file_count = this.file_count + 1;
                //}
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
                using (FileStream savedSnapshot = new FileStream(fileName, FileMode.CreateNew))
                {
                    BitmapSource image = (BitmapSource)Self.Source;
                    JpegBitmapEncoder jpgEncoder = new JpegBitmapEncoder();
                    jpgEncoder.QualityLevel = 70;
                    jpgEncoder.Frames.Add(BitmapFrame.Create(image));
                    jpgEncoder.Save(savedSnapshot);
                    savedSnapshot.Flush();
                    savedSnapshot.Close();
                    savedSnapshot.Dispose();

                    ////////getting preview of the snapshot//////////
                    if (this.file_count > 0)
                    {
                        string prev_path = "C:/Users/Kunal Kishore/Documents/Visual Studio 2013/Projects/Voice_Recog_App/Voice_Recog_App/bin/Debug/snapshot" + (this.file_count - 1) + ".jpg";
                        Prev.Source = new BitmapImage(new Uri(prev_path, UriKind.RelativeOrAbsolute));

                    }

                }
                this.camMode = true;
            }
        }
        #endregion CaptureImage

        private void KinectDevice_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (this.m3)
            {
                this.fine_count = this.fine_count - 1;
                this.m3_count = this.m3_count - 1;
                this.capture = this.capture - 1;
            }
            else if (this.m1)
            {
                this.m1_count = this.m1_count - 1;
                this.m1_count1 = this.m1_count1 - 1;
            }
            
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame != null)
                {
                    frame.CopySkeletonDataTo(this.skel);
                    if (this.m2)
                    {
                        mode2.Update(skel);
                    }
                    else if (this.m1)
                    {
                        //mode1 m = new mode1();
                        if (this.m1_count <= 0)
                        {
                            mode1.Update(skel);
                            this.m1_count = 25;
                        }
                        if (this.m1_count1 < 0)
                        {
                            mode1.UpdateVolume(skel);
                            this.m1_count1 = 15;
                        }
                    }
                    else if (this.m3&&this.capture<=0)
                    {
                        //test.Text = string.Format("Edit the Image");
                        if (this.m3_count <= 0)
                        {
                            m3_Update(skel);
                            this.m3_count = 30;
                        }
                        if (this.fine_count <= 0)
                        {
                            m3_Update1(skel);
                            this.fine_count = 5;
                        }
                    }
                    else if (this.m3 && this.capture>0)
                    {
                        //test.Text = string.Format("Pose for a nice selfie!");
                        Capture(this.capture);
                    }

                }
            }
        }

        #region AudioCommand
        private void StartSpeechRecognition()
        {
            _source = CreateAudioSource();
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase)
                && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };
            RecognizerInfo ri = SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();

            if (ri != null)
            {
                _sre = new SpeechRecognitionEngine(ri.Id);
                CreateGrammars(ri);
                _sre.SpeechRecognized += sre_SpeechRecognized;
                _sre.SpeechHypothesized += sre_SpeechHypothesized;
                _sre.SpeechRecognitionRejected += sre_SpeechRecognitionRejected;
                Stream s = _source.Start();
                _sre.SetInputToAudioStream(s, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                _sre.RecognizeAsync(RecognizeMode.Multiple);
            }
        }
        void sre_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            HypothesizedText += " Rejected";
            int_confidence = Math.Round(e.Result.Confidence, 2);
            Confidence = int_confidence.ToString();
        }
        void sre_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            HypothesizedText = e.Result.Text;
        }
        void sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            //MessageBox.Show("Func1");
            int_confidence = Math.Round(e.Result.Confidence, 2);
            Dispatcher.BeginInvoke(new Action<SpeechRecognizedEventArgs>(InterpretCommand), e);
        }
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        private string _hypothesizedText;
        public string HypothesizedText
        {
            get { return _hypothesizedText; }
            set
            {
                _hypothesizedText = value;
                OnPropertyChanged("HypothesizedText");
            }
        }
        private string _confidence;
        public string Confidence
        {
            get { return _confidence; }
            set
            {
                _confidence = value;
                OnPropertyChanged("Confidence");
            }
        }
        private KinectAudioSource CreateAudioSource()
        {
            var source = KinectSensor.KinectSensors[0].AudioSource;
            source.AutomaticGainControlEnabled = false;
            source.EchoCancellationMode = EchoCancellationMode.None;
            return source;
        }
        private void CreateGrammars(RecognizerInfo ri)
        {
            var mode = new Choices();
            mode.Add("one");
            mode.Add("two");
            mode.Add("three");
            mode.Add("mouse on");
            mode.Add("mouse off");
            //colors.Add("blue");
            //colors.Add("green");
            //colors.Add("red");

            //var create = new Choices();
            //create.Add("create");
            //create.Add("put");

            //var shapes = new Choices();
            //shapes.Add("circle");
            //shapes.Add("triangle");
            //shapes.Add("square");
            //shapes.Add("diamond");

            var gb = new GrammarBuilder();
            gb.Culture = ri.Culture;
            gb.Append("mode");
            //gb.AppendWildcard();
            gb.Append(mode);
            var g = new Grammar(gb);
            _sre.LoadGrammar(g);
            var q = new GrammarBuilder();
            q.Append("quit application");
            var quit = new Grammar(q);
            _sre.LoadGrammar(quit);
        }
        private void InterpretCommand(SpeechRecognizedEventArgs e)
        {
            //MessageBox.Show("Func");
            var result = e.Result;
            Confidence = Math.Round(result.Confidence,2).ToString();
            if (result.Words[0].Text == "quit" && (this.int_confidence>0.50))
            {
                _isQuit = true;
                Application.Current.Shutdown();
                //MessageBox.Show("Quitting your application");
                return;
            }
            if (result.Words[0].Text == "mode" && (this.int_confidence>0.50))
            {
                //MessageBox.Show("Mode");
                var choice = result.Words[1].Text;
                switch (choice)
                {

                    case "one":
                        
                        //string relLogo = new Uri(logoimage).LocalPath;
                        //MessageBox.Show(int_confidence.ToString() + " mode1");
                        //@"C:\Users\Kunal Kishore\Documents\Visual Studio 2013\Projects\Voice_Recog_App\Voice_Recog_App\Resources\Kinect_Mode_01.jpg"
                        string p = "/Resources/Kinect_Mode_01.jpg";
                        //string p = "/Resources/Kinect_Mode_01.jpg";
                        BitmapImage bm = new BitmapImage(new Uri(p, UriKind.RelativeOrAbsolute));
                        Back_Img.Source = bm;
                        this.m1 = true;
                        this.m2 = false;
                        this.m3 = false;
                        this.mmouse = false;
                        this.last = 1;
                        Panel.SetZIndex(Self, 2);
                        Panel.SetZIndex(Prev, 2);
                        Panel.SetZIndex(Back_Img, 3);
                        break;
                    case "two":
                        //MessageBox.Show(int_confidence.ToString() + " mode2");
                        string p2 = "/Resources/Kinect_Mode_02.jpg";
                        BitmapImage bm2 = new BitmapImage(new Uri(p2, UriKind.RelativeOrAbsolute));
                        Back_Img.Source = bm2;
                        this.m1 = false;
                        this.m2 = true;
                        this.m3 = false;
                        this.mmouse = false;
                        this.last = 2;
                        Panel.SetZIndex(Self, 2);
                        Panel.SetZIndex(Prev, 2);
                        Panel.SetZIndex(Back_Img, 3);
                        break;
                    case "three":
                        //MessageBox.Show(int_confidence.ToString() + " mode3");
                        string p3 = "/Resources/Kinect_Mode_03.jpg";
                        BitmapImage bm3 = new BitmapImage(new Uri(p3, UriKind.RelativeOrAbsolute));
                        Back_Img.Source = bm3;
                        this.m1 = false;
                        this.m2 = false;
                        Panel.SetZIndex(Self, 2);
                        Panel.SetZIndex(Prev, 2);
                        Panel.SetZIndex(Back_Img, 3);
                        this.m3 = true;
                        this.last = 3;
                        this.mmouse = false;
                        break;
                    case "mouse":

                        var c = result.Words[2].Text;
                        if (c == "on")
                        {
                            this.m1 = false;
                            this.m2 = false;
                            this.m3 = false;
                            this.mmouse = true;
                        }
                        else if(c=="off")
                        {
                            this.mmouse = false;
                            if (this.last == 1)
                            {
                                Panel.SetZIndex(Self, 2);
                                Panel.SetZIndex(Prev, 2);
                                Panel.SetZIndex(Back_Img, 3);
                                this.m1 = true;
                                this.m2 = false;
                                this.m3 = false;
                            }
                            else if (this.last == 2)
                            {
                                Panel.SetZIndex(Self, 2);
                                Panel.SetZIndex(Prev, 2);
                                Panel.SetZIndex(Back_Img, 3);
                                this.m1 = false;
                                this.m2 = true;
                                this.m3 = false;
                            }
                            else if (this.last == 3)
                            {
                                this.m1 = false;
                                this.m2 = false;
                                this.m3 = true;
                            }
                            else
                            {
                                this.m1 = false;
                                this.m2 = false;
                                this.m3 = false;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            /*if (result.Words[0].Text == "put" || result.Words[0].Text == "create")
            {
                var colorString = result.Words[2].Text;
                Color color;
                switch (colorString)
                {
                    case "cyan": color = Colors.Cyan;
                        break;
                    case "yellow": color = Colors.Yellow;
                        break;
                    case "magenta": color = Colors.Magenta;
                        break;
                    case "blue": color = Colors.Blue;
                        break;
                    case "green": color = Colors.Green;
                        break;
                    case "red": color = Colors.Red;
                        break;
                    default:
                        return;
                }
                var shapeString = result.Words[3].Text;
                Shape shape;
                switch (shapeString)
                {
                    case "circle":
                        shape = new Ellipse();
                        shape.Width = 150;
                        shape.Height = 150;
                        break;
                    case "square":
                        shape = new Rectangle();
                        shape.Width = 150;
                        shape.Height = 150;
                        break;
                    case "triangle":
                        var poly = new Polygon();
                        poly.Points.Add(new Point(0, 0));
                        poly.Points.Add(new Point(150, 0));
                        poly.Points.Add(new Point(75, -150));
                        shape = poly;
                        break;
                    case "diamond":
                        var poly2 = new Polygon();
                        poly2.Points.Add(new Point(0, 0));
                        poly2.Points.Add(new Point(75, 150));
                        poly2.Points.Add(new Point(150, 0));
                        poly2.Points.Add(new Point(75, -150));
                        shape = poly2;
                        break;
                    default:
                        return;
                }
                shape.SetValue(Canvas.LeftProperty, HandLeft);
                shape.SetValue(Canvas.TopProperty, HandTop);
                shape.Fill = new SolidColorBrush(color);
                MainStage.Children.Add(shape);
            }*/
        }

        private void WindowClosed(object sender, CancelEventArgs e)
        {
            _kinectSensor.Stop();
        }

        #endregion AudioCommand
    }
}
