using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Mycelia.App.Tests
{
    public class FileAnalyticsTests
    {
        static readonly DateTime Now = new DateTime(2026, 10, 24, 14, 22, 4, DateTimeKind.Utc);

        string directory;
        readonly List<string> echoed = new List<string>();

        string EventsFile => Path.Combine(directory, "events.jsonl");
        string OldFile => Path.Combine(directory, "events.old.jsonl");

        [SetUp]
        public void CreateFolder()
        {
            directory = Path.Combine(Path.GetTempPath(), "mycelia-events-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            echoed.Clear();
        }

        [TearDown]
        public void DeleteFolder()
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }

        FileAnalytics Analytics(long capBytes = 256 * 1024, string folder = null) =>
            new FileAnalytics(folder ?? directory, "s1", () => Now, echoed.Add, capBytes);

        [Test]
        public void EachEvent_IsOneJsonLine_WithTimeSessionNameAndFields()
        {
            Analytics().Log(new AnalyticsEvent("level_end")
                .With("level", "level-01").With("outcome", "won").With("stars", 2).With("moves_left", 6));

            Assert.That(File.ReadAllLines(EventsFile), Is.EqualTo(new[]
            {
                "{\"t\":\"2026-10-24T14:22:04Z\",\"session\":\"s1\",\"event\":\"level_end\",\"level\":\"level-01\",\"outcome\":\"won\",\"stars\":2,\"moves_left\":6}",
            }));
        }

        [Test]
        public void Events_AreAppended_InOrder()
        {
            FileAnalytics analytics = Analytics();

            analytics.Log(new AnalyticsEvent("app_start"));
            analytics.Log(new AnalyticsEvent("level_start"));

            string[] lines = File.ReadAllLines(EventsFile);
            Assert.That(lines.Length, Is.EqualTo(2));
            Assert.That(lines[0], Does.Contain("\"event\":\"app_start\""));
            Assert.That(lines[1], Does.Contain("\"event\":\"level_start\""));
        }

        [Test]
        public void TextValues_AreEscaped()
        {
            Analytics().Log(new AnalyticsEvent("note").With("text", "a \"quote\", a \\ and a\nnew line"));

            Assert.That(File.ReadAllText(EventsFile), Does.Contain("\"text\":\"a \\\"quote\\\", a \\\\ and a\\nnew line\""));
        }

        [Test]
        public void EveryLine_IsEchoed_ForLogcat()
        {
            Analytics().Log(new AnalyticsEvent("app_start"));

            Assert.That(echoed.Count, Is.EqualTo(1));
            Assert.That(echoed[0], Does.StartWith("[Analytics] {"));
        }

        [Test]
        public void AFullFile_MovesAside_AndANewOneStarts()
        {
            FileAnalytics analytics = Analytics(capBytes: 200);
            for (int i = 0; i < 3; i++)
                analytics.Log(new AnalyticsEvent("level_start").With("level", "level-01"));

            Assert.That(File.Exists(OldFile), Is.True);
            Assert.That(new FileInfo(EventsFile).Length, Is.LessThanOrEqualTo(200));
            Assert.That(File.ReadAllLines(OldFile).Length + File.ReadAllLines(EventsFile).Length, Is.EqualTo(3), "No event is lost by moving aside.");
        }

        [Test]
        public void Logging_NeverThrows_WhenTheFileCannotBeWritten()
        {
            string blocked = Path.Combine(directory, "not-a-folder");
            File.WriteAllText(blocked, "a file where the folder should be");

            Assert.DoesNotThrow(() => Analytics(folder: blocked).Log(new AnalyticsEvent("app_start")));
        }
    }
}
