using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace BandDraw
{
    public sealed partial class MainPage
    {
        private readonly BandInputOutput _bandInputOutput = new BandInputOutput();

        private Action _tapAction;

        private Vector _maxValues;
        private Vector _minValues;
        private bool _gotFirstCalib;

        private double _zOffset;
        private double _zGain;

        private double _yOffset;
        private double _yGain;

        public MainPage()
        {
            InitializeComponent();

            _bandInputOutput.GotGyroReading += CalibratePointer;

            _tapAction = () =>
            {
                _bandInputOutput.Connect();
                Calibrate();
            };
        }

        private void CalibratePointer()
        {
            if (!_gotFirstCalib)
            {
                _maxValues = _bandInputOutput.GyroReading;
                _minValues = _bandInputOutput.GyroReading;
                _gotFirstCalib = true;
                return;
            }

            _maxValues = _maxValues.Max(_bandInputOutput.GyroReading);
            _minValues = _minValues.Min(_bandInputOutput.GyroReading);

            _zOffset = -_minValues.Z;
            _zGain = 1.0 / (_maxValues.Z - _minValues.Z);

            _yOffset = -_minValues.Y;
            _yGain = 1.0 / (_maxValues.Y - _minValues.Y);
        }


        private void MovePointer()
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, MovePointerInCorrectThread);
        }

        private void MovePointerInCorrectThread()
        {
            var gyro = _bandInputOutput.GyroReading;
            var x = (_zOffset + gyro.Z) * _zGain;
            var y = (_yOffset + gyro.Y) * _yGain;

            // Debug.WriteLine($"({x}, {y})");

            if (x < 0 || x > 1 || y < 0 || y > 1)
            {
                _marker.Visibility = Visibility.Collapsed;
                return;
            }

            _marker.Visibility = Visibility.Visible;
            _marker.SetValue(Canvas.LeftProperty, x * _canvas.ActualWidth);
            _marker.SetValue(Canvas.TopProperty, y * _canvas.ActualHeight);
        }

        private void Page_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _tapAction?.Invoke();
        }

        private void Calibrate()
        {
            _tapAction = () =>
            {
                _bandInputOutput.GotGyroReading -= CalibratePointer;
                _bandInputOutput.GotGyroReading += MovePointer;
                _bandInputOutput.GotGyroHighXAcceleration += BandInputOutputOnGotGyroHighXAcceleration;
                _tapAction = null;
            };
        }


        private async void BandInputOutputOnGotGyroHighXAcceleration()
        {
            DispatchedHandler a = () =>
            {
                var gyro = _bandInputOutput.GyroReading;
                _yOffset = 0.5/_yGain - gyro.Y;
                _zOffset = 0.5/_zGain - gyro.Z;


                var x = (_zOffset + gyro.Z)*_zGain;
                var y = (_yOffset + gyro.Y)*_yGain;
                Debug.WriteLine($"({x}, {y})");

                Debug.WriteLine("Recalibrated");
            };

            await Task.Delay(500);
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, a);
        }
    }
}
