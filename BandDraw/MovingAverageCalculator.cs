using System.Collections.Generic;
using System.Linq;

namespace BandDraw
{
    class MovingAverageCalculator
    {
        private readonly List<Vector> _movingAverage = new List<Vector>();

        public void Add(Vector pt)
        {
            _movingAverage.Add(pt);
            if (_movingAverage.Count > 10000)
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
                return _movingAverage.Sum(v => v.Value / count);
            }
        }
    }
}