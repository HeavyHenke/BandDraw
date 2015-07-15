using System;
using System.Diagnostics;
using Windows.UI.Core;
using Microsoft.Band;
using Microsoft.Band.Sensors;

namespace BandDraw
{
    class BandInputOutput
    {
        private IBandClient _bandClient;
        private readonly GyroscopeHandler _gyroscopeHandler = new GyroscopeHandler();
        //private readonly AccelerometerHandler _accelerometerHandler = new AccelerometerHandler();

        public CoreDispatcher CoreDispatcher { get; set; }

        public Vector GyroReading => _gyroscopeHandler.Orientation;

        public event Action GotGyroReading;
        public event Action GotGyroHighXAcceleration;


        public async void Connect()
        {
            if (_bandClient != null)
                return;

            IBandInfo[] pairedBands = await BandClientManager.Instance.GetBandsAsync();
            if (pairedBands.Length == 0)
            {
                return;
            }

            Debug.WriteLine("Connecting");
            _bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]);

            _bandClient.SensorManager.Gyroscope.ReadingChanged += Gyroscope_ReadingChanged;
            await _bandClient.SensorManager.Gyroscope.StartReadingsAsync();

            Debug.WriteLine("Connected");
        }

        private void Gyroscope_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandGyroscopeReading> e)
        {
            IBandGyroscopeReading s = e.SensorReading;
            _gyroscopeHandler.AddMeasurement(s);

            if (e.SensorReading.AccelerationX > 1.2)
            {
                GotGyroHighXAcceleration?.Invoke();
            }

            GotGyroReading?.Invoke();
        }
    }
}
