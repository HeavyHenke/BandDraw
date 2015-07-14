using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Core;
using Microsoft.Band;
using Microsoft.Band.Sensors;
using static System.Math;

namespace BandDraw
{
    class BandInputOutput
    {
        private IBandClient _bandClient;
        private readonly GyroscopeHandler _gyroscopeHandler = new GyroscopeHandler();
        private readonly AccelerometerHandler _accelerometerHandler = new AccelerometerHandler();

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

            _bandClient.SensorManager.Accelerometer.ReadingChanged += Accelerometer_ReadingChanged;
            await _bandClient.SensorManager.Accelerometer.StartReadingsAsync();

            Debug.WriteLine("Connected");
        }

        private void Gyroscope_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandGyroscopeReading> e)
        {
            IBandGyroscopeReading s = e.SensorReading;
            _gyroscopeHandler.AddMeasurement(s);

            if (e.SensorReading.AccelerationX > 0.5)
            {
                GotGyroHighXAcceleration?.Invoke();
            }

            GotGyroReading?.Invoke();
        }


        private void Accelerometer_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandAccelerometerReading> e)
        {
            _accelerometerHandler.AddMeasurement(e.SensorReading);
            if (_accelerometerHandler.IsCalibrated)
            {
                
            }
        }
    }

    struct Vector
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public double Value => Sqrt(X*X + Y*Y + Z*Z);

        public Vector(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector Add(Vector other)
        {
            return new Vector(X + other.X, Y + other.Y, Z + other.Z);
        }

        public Vector Add(double x, double y, double z)
        {
            return new Vector(X + x, Y + y, Z + z);
        }

        public Vector Multiply(double val)
        {
            return new Vector(X*val, Y*val, Z*val);
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }

        public Vector Max(Vector gyroReading)
        {
            return new Vector(Math.Max(X, gyroReading.X), Math.Max(Y, gyroReading.Y), Math.Max(Z, gyroReading.Z));
        }

        public Vector Min(Vector gyroReading)
        {
            return new Vector(Math.Min(X, gyroReading.X), Math.Min(Y, gyroReading.Y), Math.Min(Z, gyroReading.Z));
        }
    }


    class MovingAverageCalculator
    {
        private readonly List<Vector> _movingAverage = new List<Vector>();

        public void Add(Vector pt)
        {
            _movingAverage.Add(pt);
            if (_movingAverage.Count > 10)
                _movingAverage.RemoveAt(0);
        }

        public Vector Average
        {
            get
            {
                var sumX = _movingAverage.Sum(a => a.X);
                var sumY = _movingAverage.Sum(a => a.Y);
                var sumZ = _movingAverage.Sum(a => a.Z);

                return new Vector(sumX / _movingAverage.Count, sumY / _movingAverage.Count, sumZ / _movingAverage.Count);
            }
        }

        public double AverageValue
        {
            get
            {
                int count = _movingAverage.Count;
                return _movingAverage.Sum(v => v.Value/count);
            }
        }
    }

    class GyroscopeHandler
    {
        private Vector _orientation;
        private Vector _lastVelocity;

        private DateTimeOffset _lastGyroStamp;

        public Vector Orientation => _orientation;

        public void AddMeasurement(IBandGyroscopeReading reading)
        {
            if (_lastGyroStamp == default(DateTimeOffset))
            {
                _lastVelocity = new Vector(reading.AngularVelocityX, reading.AngularVelocityY, reading.AngularVelocityZ);
                _lastGyroStamp = reading.Timestamp;
                return;
            }

            var msSinceLast = (reading.Timestamp - _lastGyroStamp).TotalMilliseconds;
            _lastGyroStamp = reading.Timestamp;
            if (msSinceLast < 0.01)
                return;

            var orientationX = (reading.AngularVelocityX + (reading.AngularVelocityX - _lastVelocity.X) / 2.0) * msSinceLast / 1000.0;
            var orientationY = (reading.AngularVelocityY + (reading.AngularVelocityY - _lastVelocity.Y) / 2.0) * msSinceLast / 1000.0;
            var orientationZ = (reading.AngularVelocityZ + (reading.AngularVelocityZ - _lastVelocity.Z) / 2.0) * msSinceLast / 1000.0;

            _orientation = _orientation.Add(orientationX, orientationY, orientationZ);
            _lastVelocity = new Vector(reading.AngularVelocityX, reading.AngularVelocityY, reading.AngularVelocityZ);

            // Debug.WriteLine(_orientation);
            //Debug.WriteLine($"X: ({_orientation.X})");
            //Debug.WriteLine($"Y: ({_orientation.Y})");
            //Debug.WriteLine($"Z: ({_orientation.Z})\n");
        }
    }

    class AccelerometerHandler
    {
        private int _numMeasPoints;
        private double _calibrationValue;

        private MovingAverageCalculator _calibrationValues = new MovingAverageCalculator();
        private Vector _lastAccValue;

        public Vector Acceleration => _lastAccValue;

        public bool IsCalibrated => _numMeasPoints > 10;


        public void AddMeasurement(IBandAccelerometerReading reading)
        {
            _numMeasPoints++;
            var acc = new Vector(reading.AccelerationX, reading.AccelerationY, reading.AccelerationZ);
            if (_numMeasPoints < 10)
            {
                _calibrationValues.Add(acc);
                return;
            }
            if (_numMeasPoints == 10)
            {
                _calibrationValue = 1.0 / _calibrationValues.AverageValue;
                _calibrationValues = null;

                Debug.WriteLine($"Calibrated to {_calibrationValue}");
            }

            _lastAccValue = acc = acc.Multiply(9.81*_calibrationValue);
            //Debug.WriteLine($"Acc: {acc}, value: {acc.Value}");
        }
    }
}
