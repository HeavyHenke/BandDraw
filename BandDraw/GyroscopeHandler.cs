using System;
using Microsoft.Band.Sensors;

namespace BandDraw
{
    class GyroscopeHandler
    {
        private Vector _orientation;
        private Vector _lastVelocity;

        private DateTimeOffset _lastGyroStamp;

        public Vector Orientation => _orientation;


        public void AddMeasurement(IBandGyroscopeReading reading)
        {
            var calibratedXReading = reading.AngularVelocityX - 0.0232528209480614;
            var calibratedYReading = reading.AngularVelocityY + 0.648518794348248;
            var calibratedZReading = reading.AngularVelocityZ - 0.162816270729729;

            if (_lastGyroStamp == default(DateTimeOffset))
            {
                _lastVelocity = new Vector(calibratedXReading, calibratedYReading, calibratedZReading);
                _lastGyroStamp = reading.Timestamp;
                return;
            }

            var msSinceLast = (reading.Timestamp - _lastGyroStamp).TotalMilliseconds;
            _lastGyroStamp = reading.Timestamp;
            if (msSinceLast < 0.01)
                return;

            var orientationX = (calibratedXReading + (calibratedXReading - _lastVelocity.X) / 2.0) * msSinceLast / 1000.0;
            var orientationY = (calibratedYReading + (calibratedYReading - _lastVelocity.Y) / 2.0) * msSinceLast / 1000.0;
            var orientationZ = (calibratedZReading + (calibratedZReading - _lastVelocity.Z) / 2.0) * msSinceLast / 1000.0;

            _orientation = _orientation.Add(orientationX, orientationY, orientationZ);
            _lastVelocity = new Vector(calibratedXReading, calibratedYReading, calibratedZReading);
        }
    }
}