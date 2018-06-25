// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace Blank
{
    [Register ("ViewController")]
    partial class ViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton PlayButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton RecordButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UISwitch TimeoutSwitch { get; set; }

        [Action ("PlayButton_TouchUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void PlayButton_TouchUpInside (UIKit.UIButton sender);

        [Action ("RecordButton_TouchUpInside:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void RecordButton_TouchUpInside (UIKit.UIButton sender);

        void ReleaseDesignerOutlets ()
        {
            if (PlayButton != null) {
                PlayButton.Dispose ();
                PlayButton = null;
            }

            if (RecordButton != null) {
                RecordButton.Dispose ();
                RecordButton = null;
            }

            if (TimeoutSwitch != null) {
                TimeoutSwitch.Dispose ();
                TimeoutSwitch = null;
            }
        }
    }
}