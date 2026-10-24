using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Mycelia.Balancing;
using Mycelia.Domain.Levels;
using UnityEditor;
using UnityEngine;

namespace Mycelia.Content.Editor
{
    /// <summary>
    /// Plays every catalog level with the balancing bot and reports win rates and stars as a Markdown table
    /// (design §8.2 slice 7). From the menu, or headless: <c>-executeMethod Mycelia.Content.Editor.BalancingRun.RunBatch</c>
    /// with optional env <c>SIM_SEEDS</c> (default 500) and <c>BALANCE_OUT</c> (a file to write the table to).
    /// </summary>
    public static class BalancingRun
    {
        const int DefaultSeeds = 500;

        [MenuItem("Mycelia/Simulate Levels")]
        static void RunFromMenu() => Debug.Log(Report(DefaultSeeds));

        public static void RunBatch()
        {
            try
            {
                int seeds = int.TryParse(Environment.GetEnvironmentVariable("SIM_SEEDS"), out int parsed) ? parsed : DefaultSeeds;
                string report = Report(seeds);
                string output = Environment.GetEnvironmentVariable("BALANCE_OUT");
                if (!string.IsNullOrEmpty(output))
                    File.WriteAllText(output, report);
                Debug.Log("[Balancing]\n" + report);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static string Report(int seeds)
        {
            LevelCatalog levels = Single<LevelCatalog>();
            SporeCatalog spores = Single<SporeCatalog>();

            var table = new StringBuilder();
            table.AppendLine($"Bot: greedy, one move ahead. {seeds} seeds per level (1..{seeds}).");
            table.AppendLine();
            table.AppendLine("| Level | Board | Moves | Types | Goals | Bot wins | ★ / ★★ / ★★★ (of wins) | Moves left (wins) |");
            table.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (LevelDefinition definition in levels.Levels)
            {
                LevelConfig config = LevelDefinitionMapper.ToConfig(definition, spores);
                SimulationReport report = LevelSimulator.Run(config, seeds);
                string goals = string.Join(" + ", definition.Objectives.Select(o => $"{o.target} {Short(o.sporeType.Key)}"));
                table.AppendLine(
                    $"| {config.Key} | {config.Width}×{config.Height} | {config.Moves} | {config.SporeTypes.Count} | {goals} " +
                    $"| {Percent(report.WinRate)} | {Share(report, 1)} / {Share(report, 2)} / {Share(report, 3)} " +
                    $"| {report.MeanMovesLeftOnWins.ToString("0.0", CultureInfo.InvariantCulture)} |");
            }
            return table.ToString();
        }

        static string Short(string sporeKey) => sporeKey.StartsWith("spore-") ? sporeKey.Substring(6) : sporeKey;

        static string Percent(double share) => (share * 100).ToString("0", CultureInfo.InvariantCulture) + " %";

        static string Share(SimulationReport report, int stars) =>
            report.Wins == 0 ? "–" : Percent((double)report.StarCounts[stars] / report.Wins);

        static T Single<T>() where T : UnityEngine.Object =>
            AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { "Assets" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .Single();
    }
}
