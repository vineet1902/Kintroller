using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Forms;
using System.Threading;


namespace Voice_Recog_App
{
    public static class mode1
    {
        #region MemberVariable
        private const float MIN_THRESHOLD = 0.1f;
        private const float MAX_THRESHOLD = 1f;
        private const int WAVE_MOVEMENT_TIMEOUT = 5000;
        private const int REQUIRED_ITERATIONS = 4;
        //private static bool fullscreen = true;
        //private static bool play = false;
        private static bool modeOn = false;
        //private static int count=15;
        #endregion MemberVariable

        #region Method
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

        public static void UpdateVolume(Skeleton[] skeletons)
        {
            if (skeletons != null)
            {
                Skeleton skeleton;
                for (int i = 0; i < skeletons.Length; i++)
                {
                    skeleton = skeletons[i];
                    if (skeleton.TrackingState != SkeletonTrackingState.NotTracked)
                    {
                        TrackGestureVolume(skeleton);
                    }
                }
            }
        }



        private static void PressOneKey(Keys key)
        {
            VirtualKeyboard.KeyDown(key);
            Thread.Sleep(70);
            VirtualKeyboard.KeyUp(key);
        }

        private static void PressTwoKeys(Keys key1, Keys key2)
        {
            VirtualKeyboard.KeyDown(key1);
            VirtualKeyboard.KeyDown(key2);
            Thread.Sleep(70);
            VirtualKeyboard.KeyUp(key1);
            VirtualKeyboard.KeyUp(key2);
        }

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

                //string text = head.Position.Y + "\n" + lhand.Position.Y + "\n" + rhand.Position.Y + "\n" + lelbow.Position.Y + "\n" + relbow.Position.Y + spine.Position.Y;
                if (Math.Abs(lhand.Position.X - rhand.Position.X) >= MAX_THRESHOLD && lhand.Position.Y < head.Position.Y && rhand.Position.Y < head.Position.Y)
                {
                    PressOneKey(Keys.F11);
                }
                else if (spine.Position.Z - rhand.Position.Z > MAX_THRESHOLD / 3 && spine.Position.Z - lhand.Position.Z > MAX_THRESHOLD / 3 && Math.Abs(lhand.Position.X - rhand.Position.X) < 2 * MIN_THRESHOLD)
                {
                    PressOneKey(Keys.Space);
                }
                //else if (lhand.Position.Y - head.Position.Y > MIN_THRESHOLD && rhand.Position.Y - head.Position.Y > MIN_THRESHOLD)
                //{
                //    PressOneKey(Keys.Space);
                //}
                else if (lelbow.Position.X > rhand.Position.X && lhand.Position.X < relbow.Position.X)
                {
                    PressOneKey(Keys.N);
                }
                else if (lhand.Position.X > relbow.Position.X && rhand.Position.X > lelbow.Position.X)
                {
                    PressOneKey(Keys.P);
                }
                else if (lhand.Position.Y < lelbow.Position.Y && rhand.Position.Y < relbow.Position.Y && lhand.Position.X > rhand.Position.X && relbow.Position.X > lelbow.Position.X && rhand.Position.X > lelbow.Position.X && lhand.Position.X < relbow.Position.X)
                {
                    modeOn = false;
                }
                /*else if (lhand.Position.X > spine.Position.X && rhand.Position.Y > relbow.Position.Y)
                {
                    PressTwoKeys(Keys.ControlKey, Keys.Up);
                }
                else if (lhand.Position.X > spine.Position.X && relbow.Position.Y > rhand.Position.Y)
                {
                    PressTwoKeys(Keys.ControlKey, Keys.Down);
                }*/
            }
        }


        private static void TrackGestureVolume(Skeleton skeleton)
        {
            Joint lhand = skeleton.Joints[JointType.HandLeft];
            Joint rhand = skeleton.Joints[JointType.HandRight];
            Joint lelbow = skeleton.Joints[JointType.ElbowLeft];
            Joint relbow = skeleton.Joints[JointType.ElbowRight];
            Joint head = skeleton.Joints[JointType.Head];
            Joint spine = skeleton.Joints[JointType.Spine];
            //string text = head.Position.Y + "\n" + lhand.Position.Y + "\n" + rhand.Position.Y + "\n" + lelbow.Position.Y + "\n" + relbow.Position.Y + spine.Position.Y;
            if (!modeOn)
            {
                if (lhand.Position.Y - head.Position.Y > MIN_THRESHOLD && rhand.Position.Y - head.Position.Y > MIN_THRESHOLD)
                {
                    modeOn = true;
                }
            }
            else
            {
                if (lhand.Position.X > spine.Position.X && rhand.Position.Y > relbow.Position.Y)
                {
                    PressTwoKeys(Keys.ControlKey, Keys.Up);
                }
                else if (lhand.Position.X > spine.Position.X && relbow.Position.Y > rhand.Position.Y)
                {
                    PressTwoKeys(Keys.ControlKey, Keys.Down);
                }
                else if (lhand.Position.Y < lelbow.Position.Y && rhand.Position.Y < relbow.Position.Y && lhand.Position.X > rhand.Position.X && relbow.Position.X > lelbow.Position.X && rhand.Position.X > lelbow.Position.X && lhand.Position.X < relbow.Position.X)
                {
                    modeOn = false;
                }
            }
        }


        #endregion Method

    }
}
