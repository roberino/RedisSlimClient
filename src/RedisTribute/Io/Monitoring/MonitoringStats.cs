using System;

namespace RedisTribute.Io.Monitoring
{
    class MonitoringStats
    {
        readonly float _sampleWeight = 1000;

        volatile float _success;
        volatile float _duration;

        public float WeightedDuration => _duration;

        public float SuccessRate => _duration;

        public void AddSample(bool success, TimeSpan duration)
        {
            var d = (float)duration.TotalMilliseconds;

            _duration = ((_duration * (_sampleWeight - 1)) + d) / _sampleWeight;
            _success = ((_success * (_sampleWeight - 1)) + (success ? 1 : 0)) / _sampleWeight; 
        }
    }
}
