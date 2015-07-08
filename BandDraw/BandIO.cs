using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Devices.Usb;
using Windows.Foundation;
using Windows.Media.Audio;
using Windows.UI.Core;
using Windows.UI.Popups;
using Microsoft.Band;
using Microsoft.Band.Sensors;
using static System.Math;

namespace BandDraw
{
    public class BandIO
    {
        private IBandClient _bandClient;

        public CoreDispatcher CoreDispatcher { get; set; }

        public event Action<Tuple<double, double>> NewPosition;

        public event Action Pulled;

        //public MemoryStream _msGyro = new MemoryStream();
        //public StreamWriter _swGyro;

        //public MemoryStream _msAcc = new MemoryStream();
        //public StreamWriter _swAcc;


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
            //_swGyro = new StreamWriter(_msGyro, Encoding.UTF8);
            //_swAcc = new StreamWriter(_msAcc, Encoding.UTF8);

            //var gyroMessage = $"Timestamp\tms since last\tAccelerationX\tAccelerationY\tAccelerationZ\tAngularVelocityX\tAngularVelocityY\tAngularVelocityZ\n";
            //_swGyro.Write(gyroMessage);

            //var accMessage = $"Timestamp\tms since last\tAccelerationX\tAccelerationY\tAcceleration\n";
            //_swAcc.Write(accMessage);

            //var id = "\\\\?\\BTHENUM#{a502ca9a-2ba5-413c-a4e0-13804e47b38f}_LOCALMFG&0002#7&21050e00&0&4C0BBEFE4CA4_C00000000#{b142fc3e-fa4e-460b-8abc-072b628b3c70}";
            //// Id = "\\\\?\\USB#VID_045E&PID_02D7#5&254b6016&0&2#{dee824ef-729b-4a0e-9c14-b7117d33a817}"
            //var id2 = @"BTHENUM\DEV_4C0BBEFE4CA4\7&21050E00&0&BLUETOOTHDEVICE_4C0BBEFE4CA4";

            // {Windows.Devices.Enumeration.DeviceInformation}

            //var device = (Windows.Devices.Enumeration.DeviceInformation) pairedBands[0].GetType().GetRuntimeProperty("Peer").GetValue(pairedBands[0]);
            //device.GetType().GetRuntimeFields();

            //Windows.Devices.Enumeration.DevicePicker p = new DevicePicker();
            //p.Filter.SupportedDeviceSelectors.Add("{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}");
            //p.Filter.SupportedDeviceClasses.Add(DeviceClass.All);
            //var dev2 = await p.PickSingleDeviceAsync(new Rect(0, 0, 300, 300), Placement.Default);

            // Id = "\\\\?\\BTHENUM#{a502ca97-2ba5-413c-a4e0-13804e47b38f}_LOCALMFG&0002#7&21050e00&0&4C0BBEFE4CA4_C00000000#{a502ca97-2ba5-413c-a4e0-13804e47b38f}"


            //var test = await RfcommDeviceService.FromIdAsync(dev2.Id);



            _bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]);



            //_bandClient.SensorManager.Accelerometer.ReportingInterval = TimeSpan.Parse("00:00:00.016");
            _bandClient.SensorManager.Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            await _bandClient.SensorManager.Accelerometer.StartReadingsAsync();

            _bandClient.SensorManager.Gyroscope.ReadingChanged += Gyroscope_ReadingChanged;
            await _bandClient.SensorManager.Gyroscope.StartReadingsAsync();
        }

        private double _bandOrientationX;
        private double _bandOrientationY;
        private double _bandOrientationZ;

        private DateTimeOffset _lastGyroStamp;
        private DateTimeOffset _lastAccStamp;

        private void Gyroscope_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandGyroscopeReading> e)
        {
            var s = e.SensorReading;
            var msSinceLast = (s.Timestamp - _lastGyroStamp).TotalMilliseconds;
            var gyroMessage = $"{s.Timestamp.ToString("o")}\t{msSinceLast}\t{s.AccelerationX}\t{s.AccelerationY}\t{s.AccelerationZ}\t{s.AngularVelocityX}\t{s.AngularVelocityY}\t{s.AngularVelocityZ}\n";
            //_swGyro.Write(gyroMessage);

            _lastGyroStamp = s.Timestamp;

            //Debug.WriteLine(gyroMessage);

            //_bandOrientationX = e.SensorReading.AngularVelocityX;
            //_bandOrientationY = e.SensorReading.AngularVelocityY;
            //_bandOrientationZ = e.SensorReading.AngularVelocityZ;

            //Debug.WriteLine($"{_bandOrientationX} {_bandOrientationY} {_bandOrientationZ}");
        }

        public string _result;
        public void GetResult(Stream ms)
        {
            ms.Seek(0, SeekOrigin.Begin);
            _result = new StreamReader(ms, Encoding.UTF8).ReadToEnd();
        }

        private Point _lastPoint = new Point(0, 0);
        private double _lastSampleInterval;

        private Point _velocity = new Point(0, 0);

        private DateTimeOffset _lastAccEvent;

        private bool _hasFirstSample;


        private bool _isCalibrating = true;
        private Point _offset;
        private int _calibPos;

        private MovingAverageCalculator _avgCalc = new MovingAverageCalculator();

        private void Accelerometer_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandAccelerometerReading> e)
        {
            var s = e.SensorReading;
            var msSinceLast = (s.Timestamp - _lastAccStamp).TotalMilliseconds;
            var accMessage = $"{s.Timestamp.ToString("o")}\t{msSinceLast}\t{s.AccelerationX}\t{s.AccelerationY}\t{s.AccelerationZ}\n";
            //_swAcc.Write(accMessage);

            _lastAccStamp = s.Timestamp;

            //Debug.WriteLine(accMessage);

            var totAcc = Sqrt(s.AccelerationX*s.AccelerationX + s.AccelerationY*s.AccelerationY + s.AccelerationZ*s.AccelerationZ);

            Debug.WriteLine(totAcc);



            /*
            var sampleInterval = (e.SensorReading.Timestamp - _lastAccEvent).TotalSeconds;
            if ((e.SensorReading.AccelerationX == 0 && e.SensorReading.AccelerationY == 0) || sampleInterval == 0)
                return;

            if(_isCalibrating)
            {
                var xAccCalib = e.SensorReading.AccelerationX * 9.82;
                var yAccCalib = e.SensorReading.AccelerationY * 9.82;
                _offset = _offset.Add(xAccCalib / 10, yAccCalib / 10);
                _calibPos++;
                if (_calibPos == 10)
                    _isCalibrating = false;

                _lastSampleInterval = sampleInterval;

                return;
            }

            var xAcc = e.SensorReading.AccelerationX * 9.82 - _offset.X;
            var yAcc = e.SensorReading.AccelerationY * 9.82 - _offset.Y;

            if (Abs(xAcc) < 0.02)
                xAcc = 0;
            if (Abs(yAcc) < 0.02)
                yAcc = 0;

            _avgCalc.Add(new Point(xAcc, yAcc));

            if (!_hasFirstSample)
            {
                _lastAccEvent = e.SensorReading.Timestamp;
                _lastPoint = new Point(xAcc, yAcc);
                _hasFirstSample = true;
                return;
            }

            var veloDeltaX = (xAcc + (xAcc - _lastPoint.X) / 2) * sampleInterval;
            var veloDeltaY = (yAcc + (yAcc - _lastPoint.Y) / 2) * sampleInterval;

            _lastAccEvent = e.SensorReading.Timestamp;
            _lastPoint = new Point(xAcc, yAcc);
            _lastSampleInterval = sampleInterval;

            _velocity = _velocity.Add(veloDeltaX, veloDeltaY);

            //Debug.WriteLine($"Velocity: {_velocity}, time since last sample {sampleInterval} last acc {_lastPoint}");
            */
        }
    }

    struct Point
    {
        public double X { get; private set; }
        public double Y { get; private set; }

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Point Add(Point other)
        {
            return new Point(X + other.X, Y + other.Y);
        }

        public Point Add(double x, double y)
        {
            return new Point(X + x, Y + y);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

    }


    class MovingAverageCalculator
    {
        private List<Point> _movingAverage = new List<Point>();

        public void Add(Point pt)
        {
            _movingAverage.Add(pt);
            if (_movingAverage.Count > 10)
                _movingAverage.RemoveAt(0);
        }

        public Point Average
        {
            get
            {
                var sumX = _movingAverage.Sum(a => a.X);
                var sumY = _movingAverage.Sum(a => a.Y);

                return new Point(sumX / _movingAverage.Count, sumY / _movingAverage.Count);
            }
        }
    }
}
