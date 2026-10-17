using System;
using System.Collections.Generic;
namespace DBP.Ranking
{
    // A union of named mandatory exclusions prevents nested flows subtracting time twice.
    // Quiz, pause and focus loss never call Exclude.
    public sealed class RankingClock
    {
        readonly HashSet<string> exclusions = new HashSet<string>(StringComparer.Ordinal);
        double start, excluded, exclusionStart;
        bool started, stopped;
        long result;
        public void Begin(double now) { if (started) return; started = true; start = now; }
        public void Exclude(string reason, double now)
        {
            if (!started || stopped || string.IsNullOrEmpty(reason)) return;
            if (exclusions.Add(reason) && exclusions.Count == 1) exclusionStart = now;
        }
        public void Include(string reason, double now)
        {
            if (!started || stopped || !exclusions.Remove(reason)) return;
            if (exclusions.Count == 0) excluded += Math.Max(0,now-exclusionStart);
        }
        public long Stop(double now)
        {
            if (stopped) return result;
            stopped = true;
            double seconds = started ? Math.Max(0, now-start-excluded-(exclusions.Count > 0 ? Math.Max(0,now-exclusionStart) : 0)) : 0;
            result = checked((long)Math.Round((decimal)seconds*1000m,0,MidpointRounding.AwayFromZero));
            return result;
        }
    }
}
