using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Android.Webkit;
using System.IO;
using Android.Locations;
using Xamarin.Essentials;
using System.Collections.Generic;

namespace VibMap
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, ILocationListener
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
            fab.Click += FabOnClick;

            WebView web = FindViewById<WebView>(Resource.Id.webview);
            web.Settings.JavaScriptEnabled = true;
            web.SetWebViewClient(new MyWebViewClient());
            StreamReader input = new StreamReader(Assets.Open("webmap.html"));
            web.LoadDataWithBaseURL("https://mapv.baidu.com/gl/examples/editor.html#line-color.html", input.ReadToEnd(), "text/html", "utf-8", "https://mapv.baidu.com/gl/examples/editor.html");
            LocationManager lm = (LocationManager)GetSystemService(LocationService);
            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            Accelerometer.Start(SensorSpeed.UI);
        }

        double LastAcc = -1;
        double SumDAcc = 0;

        private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            var len = Math.Sqrt((e.Reading.Acceleration.X * e.Reading.Acceleration.X)
                + (e.Reading.Acceleration.Y * e.Reading.Acceleration.Y)
                + (e.Reading.Acceleration.Z * e.Reading.Acceleration.Z)
                );
            var Dacc = Math.Abs(LastAcc - len);
            SumDAcc += Dacc;
            LastAcc = len;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            WebView web = FindViewById<WebView>(Resource.Id.webview);
            var ratio = 0xFF / (maxvib - minvib);
            foreach (var p in Points)
            {
                var colorR = (p.Value - minvib) * ratio;
                string R = colorR.ToString("X");
                web.EvaluateJavascript("AddData(" + p.Key.Latitude + "," + p.Key.Longitude + ",\"#" + R + "0000\");", null);
            }
            web.EvaluateJavascript("Finish();", null);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        bool runFlag = false;
        Android.Locations.Location LastLocation;

        Dictionary<Android.Locations.Location, double> Points =
            new System.Collections.Generic.Dictionary<Android.Locations.Location, double>();
        double minvib = double.MaxValue, maxvib = double.MinValue;

        public void OnLocationChanged(Android.Locations.Location location)
        {
            WebView web = FindViewById<WebView>(Resource.Id.webview);
            web.EvaluateJavascript("Center(" + location.Latitude + "," + location.Longitude + ");", null);
            if (runFlag)
            {
                var dacc = SumDAcc;
                SumDAcc = 0;
                if (LastLocation == null)
                {
                    LastLocation = location;
                    return;
                }
                var Distance = DistanceBetween(LastLocation, location);
                LastLocation = location;
                dacc = dacc / Distance;
                Points.Add(location, dacc);
                if (dacc > minvib) minvib = dacc;
                if (dacc < maxvib) maxvib = dacc;
            }
        }

        public double DistanceBetween(Android.Locations.Location a, Android.Locations.Location b)
        {
            return Math.Sqrt(((a.Latitude - b.Latitude) * (a.Latitude - b.Latitude))
                + ((a.Longitude - b.Longitude) * (a.Longitude - b.Longitude)));
        }

        public void OnProviderDisabled(string provider)
        {
        }

        public void OnProviderEnabled(string provider)
        {
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
        }
    }
}
