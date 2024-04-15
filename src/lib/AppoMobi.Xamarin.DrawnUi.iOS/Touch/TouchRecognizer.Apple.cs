﻿using AppoMobi.Forms.Gestures;
using AppoMobi.Framework.Forms.UI.Touch;
using CoreGraphics;
using Foundation;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using UIKit;

namespace AppoMobi.Maui.Gestures
{
    class TouchRecognizer : UIGestureRecognizer
    {

        volatile PlatformTouchEffect _parent;
        UIView _view;

        UIPinchGestureRecognizer recognizer;
        private bool _disposed;

        public TouchRecognizer(UIView view, PlatformTouchEffect parent)
        {
            _view = view;
            _parent = parent;
        }


        public void Detach()
        {

            _view.RemoveGestureRecognizer(recognizer);

            recognizer?.Dispose();

            recognizer = null;

            _view.RemoveGestureRecognizer(this);
        }

        public void Attach()
        {
            _view.AddGestureRecognizer(this);

            recognizer = new UIPinchGestureRecognizer(() =>
            {

                if (recognizer.NumberOfTouches == 2 && recognizer.State == UIGestureRecognizerState.Began)
                {
                    _parent.CountFingers = 1;
                    _parent.FireEvent(0, TouchActionType.Cancelled, _lastPoint);
                }

                _parent.CountFingers = (int)recognizer.NumberOfTouches;
                if (recognizer.NumberOfTouches < 2 || recognizer.State == UIGestureRecognizerState.Ended || recognizer.State == UIGestureRecognizerState.Cancelled)
                {
                    _parent.FireEvent(0, TouchActionType.Cancelled, _lastPoint);
                    return;
                }

                CGPoint point1 = recognizer.LocationOfTouch(0, recognizer.View);
                CGPoint point2 = recognizer.LocationOfTouch(1, recognizer.View);

                // Calculate the center point
                var centerX = (point1.X + point2.X) / 2;
                var centerY = (point1.Y + point2.Y) / 2;

                _parent.Pinch = new ScaleEventArgs()
                {
                    Scale = (float)recognizer.Scale,
                    Center = new((float)centerX * TouchEffect.Density, (float)centerY * TouchEffect.Density)
                };

                _lastPoint = new PointF((float)point1.X, (float)point1.Y);
                _parent.FireEvent(0, TouchActionType.Pinch, _lastPoint);

            });


            _view.AddGestureRecognizer(recognizer);
        }

        PointF _lastPoint;

        private bool ShouldRecLocked(UIGestureRecognizer gesturerecognizer, UIGestureRecognizer othergesturerecognizer)
        {
            return true;
        }
        private bool ShouldRecUnlocked(UIGestureRecognizer gesturerecognizer, UIGestureRecognizer othergesturerecognizer)
        {
            return false;
        }

        private bool ShouldFailLocked(UIGestureRecognizer gesturerecognizer, UIGestureRecognizer othergesturerecognizer)
        {
            return false;
        }

        void ShareTouch()
        {
            ShouldBeRequiredToFailBy = ShouldRecUnlocked;
            ShouldRecognizeSimultaneously = ShouldRecLocked;
        }
        void LockTouch()
        {
            ShouldBeRequiredToFailBy = ShouldFailLocked;
            ShouldRecognizeSimultaneously = ShouldRecLocked;

            //if (UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
            //{
            //    ShouldReceiveEvent = ShouldEvent;
            //}

            //  Debug.WriteLine("[TOUCH] LOCKED!");
        }

        private bool ShouldEvent(UIGestureRecognizer gesturerecognizer, UIEvent @event)
        {
            return true;
        }

        void UnlockTouch()
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
            {
                ShouldReceiveEvent = ShouldEvent;
            }

            ShouldBeRequiredToFailBy = null;
            ShouldRecognizeSimultaneously = null;

            //  Debug.WriteLine("[TOUCH] UNlocked!");
        }

        private bool IsViewOrAncestorHidden(UIView view)
        {
            if (view == null)
            {
                return false;
            }
            return view.Hidden || view.Alpha == 0 || IsViewOrAncestorHidden(view.Superview);
        }

        // touches = touches of interest; evt = all touches of type UITouch
        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            base.TouchesBegan(touches, evt);

            if (IsViewOrAncestorHidden(this.View))
            {
                this.State = UIGestureRecognizerState.Failed;
                return;
            }

            //Console.WriteLine("TouchesBegan");

            _parent.CountFingers = (int)NumberOfTouches;


            foreach (UITouch touch in touches.Cast<UITouch>())
            {
                long id = ((IntPtr)touch.Handle).ToInt64();
                _parent.FireEvent(id, TouchActionType.Pressed, touch);
            }

            // Save the setting of the Capture property
            //capture = TouchEffect.Capture;
            _parent.isInsideView = true;

            if (_parent.FormsEffect.TouchMode == TouchHandlingStyle.Lock)
            {
                LockTouch();
            }
            else
            if (_parent.FormsEffect.TouchMode == TouchHandlingStyle.Share)
            {
                ShareTouch();
            }
            else
            {
                UnlockTouch();
            }
        }

        public override void TouchesMoved(NSSet touches, UIEvent evt)
        {
            //Console.WriteLine("TouchesMoved");

            base.TouchesMoved(touches, evt);

            _parent.CountFingers = (int)NumberOfTouches;

            foreach (UITouch touch in touches.Cast<UITouch>())
            {
                long id = ((IntPtr)touch.Handle).ToInt64();
                _parent.FireEvent(id, TouchActionType.Moved, touch);
            }
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            base.TouchesEnded(touches, evt);

            _parent.CountFingers = (int)NumberOfTouches;

            var uiTouches = touches.Cast<UITouch>();
            if (uiTouches.Count() > 0)
            {
                foreach (UITouch touch in uiTouches)
                {
                    CGPoint cgPoint = touch.LocationInView(this.View);
                    var xfPoint = new PointF((float)cgPoint.X, (float)cgPoint.Y);
                    bool isInside = CheckPointIsInsideRecognizer(xfPoint, this);
                    long id = ((IntPtr)touch.Handle).ToInt64();

                    if (isInside)
                        _parent.FireEvent(id, TouchActionType.Released, touch);
                    else
                        _parent.FireEvent(id, TouchActionType.Exited, touch);
                }
            }
            else
            {
                _parent.FireEvent(0, TouchActionType.Released, PointF.Empty);
            }

            UnlockTouch();
        }

        public override void TouchesCancelled(NSSet touches, UIEvent evt)
        {
            base.TouchesCancelled(touches, evt);

            foreach (UITouch touch in touches.Cast<UITouch>())
            {
                long id = ((IntPtr)touch.Handle).ToInt64();
                _parent.FireEvent(id, TouchActionType.Cancelled, touch);
            }

            UnlockTouch();

        }

        public override bool CancelsTouchesInView
        {
            get
            {
                return false; //todo if TRUE allowes scrollView to cancel our touches
            }
        }

        //void CheckForBoundaryHop(UITouch touch)
        //{
        //	long id = ((IntPtr)touch.Handle).ToInt64();

        //	// TODO: Might require converting to a List for multiple hits
        //	TouchRecognizer touchEffectHit = null;

        //	foreach (UIView view in viewDictionary.Keys)
        //	{
        //		try
        //		{
        //			CGPoint location = touch.LocationInView(view);
        //			if (new CGRect(new CGPoint(), view.Frame.Size).Contains(location))
        //			{
        //				touchEffectHit = viewDictionary[view];
        //			}
        //		}
        //		catch (Exception e)
        //		{
        //			continue; //view might be disposed
        //		}
        //	}

        //	if (touchEffectHit != idToEffectDictionary[id])
        //	{
        //		if (touchEffectHit == null)
        //		{
        //			idToEffectDictionary[id] = null;
        //		}
        //	}
        //}

        bool CheckPointIsInsideRecognizer(PointF xfPoint, TouchRecognizer recognizer)
        {
            if (xfPoint.Y < 0 || xfPoint.Y > recognizer.View.Bounds.Height)
            {
                return false;
            }

            if (xfPoint.X < 0 || xfPoint.X > recognizer.View.Bounds.Width)
            {
                return false;
            }

            return true;
        }



        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _disposed = true;
        }

    }
}
