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
using AndroidX.Core.App;
using Android;
using Android.Util;

namespace VibMap
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, ILocationListener
    {
        LocationManager lm;

        double LastAcc = -1;
        double SumDAcc = 0;
        bool runFlag = true;
        Android.Locations.Location LastLocation;

        Dictionary<double[], double> Points =
            new System.Collections.Generic.Dictionary<double[], double>();
        double minvib = double.MaxValue, maxvib = double.MinValue;

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
            input.Close();
            lm = (LocationManager)GetSystemService(LocationService);

            ActivityCompat.RequestPermissions(this, new String[] { Manifest.Permission.AccessFineLocation, Manifest.Permission.AccessMockLocation }, 1);

            Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            Accelerometer.Start(SensorSpeed.UI);
        }

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
            string colorcode = "", locationcode = "";
            var ratio = 0xFF / (maxvib - minvib);
            foreach (var p in Points)
            {
                if (colorcode.Length > 0) colorcode += ",";
                if (locationcode.Length > 0) locationcode += ",";
                var colorR = (p.Value - minvib) * ratio;
                string R = ((int)colorR).ToString("X2");
                colorcode += "\"#" + R + "0000\"";
                locationcode += "[" + p.Key[0] + "," + p.Key[1] + "]";
                //web.EvaluateJavascript("linevalue.push(\"#" + R + "0000\");pointArr.push(new BMap.Point( " + p.Key[0] + "," + p.Key[1] + "));", null);
            }
            StreamReader inp = new StreamReader(Assets.Open("glscript.js"));
            string script = inp.ReadToEnd()
                .Replace("{ $COLORS }", colorcode)
                .Replace("{ $CORDS }", locationcode);
            inp.Close();
            web.EvaluateJavascript(script, null);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode == 1 && grantResults[0] == Android.Content.PM.Permission.Granted)
            {
                Log.Info("permission", "LOCATION granted.");
                lm.RequestLocationUpdates(LocationManager.GpsProvider, 500, 1, this);
            }
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public void OnLocationChanged(Android.Locations.Location location)
        {
            WebView web = FindViewById<WebView>(Resource.Id.webview);
            var locationbd = LocationConverter.WGS84_to_BD09(location.Longitude, location.Latitude);
            Log.Info("Location", "GPS=>" + location.Latitude + "," + location.Longitude + ".");
            Log.Info("js-bridge", "Center=>" + locationbd[1] + "," + locationbd[0] + ".");

            web.EvaluateJavascript("map.setCenter(new BMap.Point(" + locationbd[0] + "," + locationbd[1] + "));", null);
            //lm.RequestLocationUpdates(LocationManager.FusedProvider, 1000, 1, this);
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
                if (dacc.Equals(double.NaN)) return;
                Points.Add(locationbd, dacc);
                if (dacc < minvib) minvib = dacc;
                if (dacc > maxvib) maxvib = dacc;
                Log.Info("js-bridge", "Vibraition Level=" + dacc);
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
