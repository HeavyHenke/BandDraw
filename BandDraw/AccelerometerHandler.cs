using System.Diagnostics;
using Microsoft.Band.Sensors;

namespace BandDraw
{
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

            _lastAccValue = acc.Multiply(9.81 * _calibrationValue);
            //Debug.WriteLine($"Acc: {acc}, value: {acc.Value}");
        }
    }
}