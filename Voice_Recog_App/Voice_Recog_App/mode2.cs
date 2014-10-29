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
using System.Windows.Forms;
using System.Threading;
namespace Voice_Recog_App
{
    public static class mode2
    {
        #region Member Variable
        //private static System.TimeSpan time_to_sleep = 70;
        private static bool modeOn = false;
        private const float MIN_THRESHOLD = 0.1f;
        private const float MAX_THRESHOLD = 1f;

        #endregion Member Variable

        #region Frame Update Function
        public static void Update(Skeleton[] skeletons)
        {
            if (skeletons != null)
            {
                Skeleton skeleton;
                for (int i = 0; i < skeletons.Length; i++)
                {
                    skeleton = skeletons[i];
                    if (skeleton.TrackingState != SkeletonTrackingState.NotTracked)
                    {
                        TrackGesture(skeleton);
                    }
                }
            }
        }

        #endregion Frame Update Function


        #region Alt button
        private static void SendUp()
        {
            // Thread.Sleep(100);
            VirtualKeyboard.KeyDown(Keys.Up);
            Thread.Sleep(40);
            VirtualKeyboard.KeyUp(Keys.Up);
        }
        private static void SendLeft()
        {
            //Thread.Sleep(150);
            VirtualKeyboard.KeyDown(Keys.Left);
            VirtualKeyboard.KeyDown(Keys.Up);
            Thread.Sleep(120);
            VirtualKeyboard.KeyUp(Keys.Left);
            VirtualKeyboard.KeyUp(Keys.Up);

        }
        private static void SendRight()
        {
            // Thread.Sleep(150);
            VirtualKeyboard.KeyDown(Keys.Right);
            VirtualKeyboard.KeyDown(Keys.Up);
            Thread.Sleep(120);
            VirtualKeyboard.KeyUp(Keys.Up);
            VirtualKeyboard.KeyUp(Keys.Right);

        }
        private static void SendDown()
        {
            //   Thread.Sleep(100);
            VirtualKeyboard.KeyDown(Keys.Down);
            Thread.Sleep(50);

            VirtualKeyboard.KeyUp(Keys.Down);
        }
        private static void SendShift()
        {
            VirtualKeyboard.KeyDown(Keys.ShiftKey);
            Thread.Sleep(40);
            VirtualKeyboard.KeyUp(Keys.ShiftKey);
        }

        #endregion Alt button


        #region Button Press
        private static void PressOneKey(Keys key)
        {
            VirtualKeyboard.KeyDown(key);
            Thread.Sleep(100);
            VirtualKeyboard.KeyUp(key);
        }

        private static void PressTwoKeys(Keys key1, Keys key2)
        {
            VirtualKeyboard.KeyDown(key1);
            VirtualKeyboard.KeyDown(key2);
            Thread.Sleep(100);
            VirtualKeyboard.KeyUp(key1);
            VirtualKeyboard.KeyUp(key2);
        }

        #endregion Button Press

        #region Gesture Tracking
        private static void TrackGesture(Skeleton skeleton)
        {
            Joint lhand = skeleton.Joints[JointType.HandLeft];
            Joint rhand = skeleton.Joints[JointType.HandRight];
            Joint lelbow = skeleton.Joints[JointType.ElbowLeft];
            Joint relbow = skeleton.Joints[JointType.ElbowRight];
            Joint head = skeleton.Joints[JointType.Head];
            Joint spine = skeleton.Joints[JointType.Spine];
            if (!modeOn)
            {
                if (lhand.Position.Y - head.Position.Y > MIN_THRESHOLD && rhand.Position.Y - head.Position.Y > MIN_THRESHOLD)
                {
                    modeOn = true;
                }
            }
            else
            {
                if (rhand.Position.Y > relbow.Position.Y && lhand.Position.Y > lelbow.Position.Y)
                {
                    SendUp();
                    //PressOneKey(Keys.Up);
                    if (Math.Abs(lhand.Position.Z - rhand.Position.Z) < 2 * MIN_THRESHOLD && spine.Position.Z - lhand.Position.Z > MAX_THRESHOLD / 4 && spine.Position.Z - rhand.Position.Z > MAX_THRESHOLD / 4)
                    {
                        //Nitro
                        SendShift();
                        //PressTwoKeys(Keys.Up, Keys.ShiftKey);
                        if (rhand.Position.Y - lhand.Position.Y > MIN_THRESHOLD)
                        {
                            SendLeft();
                            //PressOneKey(Keys.Left);
                        }
                        else if (lhand.Position.Y - rhand.Position.Y > MIN_THRESHOLD)
                        {
                            SendRight();
                            //PressOneKey(Keys.Right);
                        }
                    }
                    else if (Math.Abs(lhand.Position.Z - rhand.Position.Z) < 2 * MIN_THRESHOLD)
                    {
                        if (rhand.Position.Y - lhand.Position.Y > MIN_THRESHOLD)
                        {
                            //Left turn
                            SendLeft();
                            //PressTwoKeys(Keys.Left,Keys.Up);
                        }
                        else if (lhand.Position.Y - rhand.Position.Y > MIN_THRESHOLD)
                        {
                            //right turn
                            SendRight();
                            //PressTwoKeys(Keys.Right, Keys.Up);
                        }
                    }
                    else if (lhand.Position.Z - rhand.Position.Z > 2 * MIN_THRESHOLD)
                    {
                        //left drift
                        //                        PressOneKey(Keys.Left);
                        //                      PressOneKey(Keys.Down);
                        //                    PressOneKey(Keys.Up);
                        //SendDown();
                        SendLeft();
                    }
                    else if (rhand.Position.Z - lhand.Position.Z > 2 * MIN_THRESHOLD)
                    {
                        //right drift
                        //PressOneKey(Keys.Right);
                        //PressOneKey(Keys.Down);
                        //PressOneKey(Keys.Up);
                        SendRight();
                    }
                }
                else
                {
                    SendDown();
                    //PressOneKey(Keys.Down);
                    if (rhand.Position.X > lhand.Position.X && relbow.Position.X > lelbow.Position.X)
                    {
                        if (rhand.Position.Y - lhand.Position.Y > MIN_THRESHOLD)
                        {
                            //turn right
                            //PressTwoKeys(Keys.Right, Keys.Down);
                            SendLeft();
                            SendDown();
                        }
                        else if (lhand.Position.Y - rhand.Position.Y > MIN_THRESHOLD)
                        {
                            //turn left
                            //PressTwoKeys(Keys.Left, Keys.Down);
                            SendRight();
                            SendDown();
                        }
                    }
                    else if (lhand.Position.Y < lelbow.Position.Y && rhand.Position.Y < relbow.Position.Y && lhand.Position.X > rhand.Position.X && relbow.Position.X > lelbow.Position.X && rhand.Position.X > lelbow.Position.X && lhand.Position.X < relbow.Position.X)
                    {
                        modeOn = false;
                    }

                }
                //string text = head.Position.Y + "\n" + lhand.Position.Y + "\n" + rhand.Position.Y + "\n" + lelbow.Position.Y + "\n" + relbow.Position.Y + spine.Position.Y;

            }
        }


        #endregion Gesture Tracking
    }
}

