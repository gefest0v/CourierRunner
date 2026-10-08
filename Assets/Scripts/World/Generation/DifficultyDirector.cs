using UnityEngine;

namespace CourierRunner
{
    /// <summary>
    /// Maps the distance into a run to a difficulty in [0, 1] and derives speed, row spacing and
    /// tower fire rate from it. Difficulty rises in steps so the game never jumps to an
    /// unfamiliar level of challenge mid-combination (GAME_DESIGN.md §5).
    /// </summary>
    public sealed class DifficultyDirector
    {
        private readonly GameConfig config;
        private readonly RunMode mode;

        public DifficultyDirector(GameConfig config, RunMode mode)
        {
            this.config = config;
            this.mode = mode;
        }

        public float DifficultyAt(float distance)
        {
            distance = Mathf.Max(0f, distance - config.safeStartDistance);
            float raw;
            if (mode == RunMode.Story)
            {
                float progress = Mathf.Clamp01(distance / Mathf.Max(1f, config.storyLevelLength - config.safeStartDistance));
                raw = Mathf.Lerp(config.storyDifficultyStart, config.storyDifficultyEnd, progress);
            }
            else
            {
                // Approaches 1 smoothly; endlessDifficultyDistance is where it reaches ~63 %.
                raw = 1f - Mathf.Exp(-distance / Mathf.Max(1f, config.endlessDifficultyDistance));
            }
            int steps = Mathf.Max(1, config.difficultySteps);
            return Mathf.Clamp01(Mathf.Floor(raw * steps) / steps);
        }

        /// <summary>Run speed at a distance. Smooth (not stepped) so the courier never lurches.</summary>
        public float SpeedAt(float distance)
        {
            float t;
            if (mode == RunMode.Story)
            {
                float progress = Mathf.Clamp01(Mathf.Max(0f, distance - config.safeStartDistance) /
                                               Mathf.Max(1f, config.storyLevelLength - config.safeStartDistance));
                t = Mathf.Lerp(config.storyDifficultyStart, config.storyDifficultyEnd, progress);
            }
            else
            {
                t = 1f - Mathf.Exp(-Mathf.Max(0f, distance) / Mathf.Max(1f, config.endlessDifficultyDistance));
            }
            return Mathf.Lerp(config.startSpeed, config.maxSpeed, t);
        }

        public float RowSpacingAt(float distance) =>
            Mathf.Lerp(config.rowSpacingEasy, config.rowSpacingHard, DifficultyAt(distance));

        public float TowerFireIntervalAt(float distance) =>
            Mathf.Lerp(config.towerFireIntervalEasy, config.towerFireIntervalHard, DifficultyAt(distance));
    }
}
