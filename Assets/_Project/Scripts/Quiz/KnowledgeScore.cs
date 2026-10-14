using System;
using System.Collections.Generic;
namespace DBP.Quiz
{
    // BR-25: millisecond timing, halfway-up rounding per answer before summation.
    public static class KnowledgeScore
    {
        public static int Calculate(int level, QuizForm form, QuizOutcome outcome, double remainingSeconds = 0, double limitSeconds = 0)
        {
            if (level < 1 || level > 5) throw new ArgumentOutOfRangeException(nameof(level));
            if (outcome != QuizOutcome.Correct) return 0;
            if (form == QuizForm.Bunker) return level * 100;
            if (double.IsNaN(remainingSeconds) || double.IsInfinity(remainingSeconds) || double.IsNaN(limitSeconds) || double.IsInfinity(limitSeconds) || limitSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(limitSeconds));
            decimal limitMs = Math.Round((decimal)limitSeconds * 1000m, 0, MidpointRounding.AwayFromZero);
            if (limitMs <= 0) throw new ArgumentOutOfRangeException(nameof(limitSeconds));
            decimal remainingMs = Math.Round((decimal)remainingSeconds * 1000m, 0, MidpointRounding.AwayFromZero);
            decimal fraction = Math.Max(0m, Math.Min(1m, remainingMs / limitMs));
            return (int)Math.Round(level * 100m * (1m + 0.5m * fraction), 0, MidpointRounding.AwayFromZero);
        }
        public static int ForAnswer(QuizResult answer) => Calculate(answer.level, answer.inBunker ? QuizForm.Bunker : QuizForm.Field, answer.outcome, answer.remaining, answer.timeLimit);
        public static int Total(IEnumerable<QuizResult> answers)
        {
            int total = 0;
            foreach (var answer in answers) total = checked(total + ForAnswer(answer));
            return total;
        }
    }
}
