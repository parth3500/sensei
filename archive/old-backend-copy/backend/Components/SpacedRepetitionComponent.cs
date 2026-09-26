using System;
using Sensei.Core.Interfaces;

namespace Sensei.Components;

public class SpacedRepetitionComponent : ISpacedRepetitionComponent
{
    public (double NewEase, int NewInterval, int NewReps, string NextDueDate) CalculateNextReview(
        double currentEase, int currentInterval, int currentReps, bool isCorrect)
    {
        double ease = currentEase > 0 ? currentEase : 2.5;
        int interval;
        int reps;

        if (isCorrect)
        {
            if (currentReps == 0)
            {
                interval = 1;
            }
            else if (currentReps == 1)
            {
                interval = 6;
            }
            else
            {
                interval = (int)Math.Round(currentInterval * ease);
                if (interval <= currentInterval) interval = currentInterval + 1;
            }
            reps = currentReps + 1;
            ease = Math.Max(1.3, ease + 0.1);
        }
        else
        {
            reps = 0;
            interval = 1;
            ease = Math.Max(1.3, ease - 0.2);
        }

        string nextDueDate = DateTime.UtcNow.AddDays(interval).ToString("yyyy-MM-dd");
        return (Math.Round(ease, 2), interval, reps, nextDueDate);
    }
}
