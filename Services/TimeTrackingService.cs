using System;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using DochazkaTracker.Models;
using DochazkaTracker.Services;


namespace DochazkaTracker.Services
{
    public class TimeTrackingService
    {
        private static readonly Lazy<TimeTrackingService> _instance = new Lazy<TimeTrackingService>(() => new TimeTrackingService());
        public static TimeTrackingService Instance => _instance.Value;

        private Timer _timer;
        private DateTime? _startTime;
        private bool _isTracking;

        public event Action<TimeSpan> OnTimeUpdated;
        public event Action<TimeSpan> OnTrackingStopped;

        public bool IsTracking => _isTracking;
        public TimeSpan CurrentElapsed => _startTime.HasValue ? DateTime.Now - _startTime.Value : TimeSpan.Zero;

        public void StartTracking()
        {
            if (_isTracking) return;

            _startTime = DateTime.Now;
            _isTracking = true;
            _timer = new Timer(UpdateTime, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        public TimeSpan StopTracking()
        {
            if (!_isTracking) return TimeSpan.Zero;

            _timer?.Dispose();
            _isTracking = false;

            var elapsed = _startTime.HasValue ? DateTime.Now - _startTime.Value : TimeSpan.Zero;
            OnTrackingStopped?.Invoke(elapsed);

            _startTime = null;
            return elapsed;
        }

        private void UpdateTime(object state)
        {
            if (_startTime.HasValue)
            {
                var elapsed = DateTime.Now - _startTime.Value;
                OnTimeUpdated?.Invoke(elapsed);
            }
        }
    }
}
