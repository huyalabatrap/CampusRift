using System;

namespace CampusRift.Learning
{
    // Time source for reviews, exam lock-outs and daily limits. The real one is the system clock; tests use a fake.
    public interface ILearningClock { DateTime UtcNow { get; } }

    public sealed class SystemLearningClock : ILearningClock { public DateTime UtcNow => DateTime.UtcNow; }

    public sealed class FakeLearningClock : ILearningClock
    {
        public DateTime Now;
        public FakeLearningClock(DateTime start) { Now = start; }
        public DateTime UtcNow => Now;
        public void Advance(TimeSpan span) { Now += span; }
    }
}
