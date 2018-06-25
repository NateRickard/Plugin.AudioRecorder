using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using System;

namespace AudioRecord.Forms.Droid
{
	[Activity(Label = "AudioRecord.Forms", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App());

			if (ContextCompat.CheckSelfPermission (this, Manifest.Permission.RecordAudio) != Permission.Granted)
			{
				ActivityCompat.RequestPermissions (this, new String []{ Manifest.Permission.RecordAudio }, 1);
			}
		}
    }
}