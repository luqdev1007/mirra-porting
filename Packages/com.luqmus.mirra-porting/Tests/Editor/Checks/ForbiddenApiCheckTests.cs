using System.Collections.Generic;
using System.Linq;
using Luqmus.MirraPorting.Core;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Checks
{
    /// <summary>The rules of the table, one source line each.</summary>
    public class ForbiddenApiCheckTests
    {
        [Test]
        public void TimeScale()
        {
            Assert.AreEqual("API.TIMESCALE_WRITE", CheckFixture.Single("Time.timeScale = 0f;").RuleId);
            Assert.AreEqual("API.TIMESCALE_READ", CheckFixture.Single("var x = Time.timeScale;").RuleId);
        }

        [Test]
        public void AudioListener()
        {
            Assert.AreEqual("API.AUDIO_VOLUME_WRITE", CheckFixture.Single("AudioListener.volume = 0f;").RuleId);
            Assert.AreEqual("API.AUDIO_VOLUME_READ", CheckFixture.Single("var v = AudioListener.volume;").RuleId);
            Assert.AreEqual("API.AUDIO_PAUSE_WRITE", CheckFixture.Single("AudioListener.pause = true;").RuleId);
            Assert.AreEqual("API.AUDIO_PAUSE_READ", CheckFixture.Single("var p = AudioListener.pause;").RuleId);
        }

        [Test]
        public void Cursor()
        {
            Assert.AreEqual("API.CURSOR_VISIBLE_WRITE", CheckFixture.Single("Cursor.visible = false;").RuleId);
            Assert.AreEqual("API.CURSOR_VISIBLE_READ", CheckFixture.Single("var v = Cursor.visible;").RuleId);
            Assert.AreEqual("API.CURSOR_LOCK_WRITE", CheckFixture.Single("Cursor.lockState = CursorLockMode.Locked;").RuleId);
            Assert.AreEqual("API.CURSOR_LOCK_READ", CheckFixture.Single("var l = Cursor.lockState;").RuleId);
        }

        [Test]
        public void PlayerPrefs_EveryMemberCounts()
        {
            IReadOnlyList<Finding> findings = CheckFixture.Run("PlayerPrefs.Save();\nPlayerPrefs.GetInt(\"a\");");

            CollectionAssert.AreEqual(
                new[] { "API.PLAYERPREFS", "API.PLAYERPREFS" },
                findings.Select(f => f.RuleId).ToList());
            CollectionAssert.AreEqual(new[] { 1, 2 }, findings.Select(f => f.Line).ToList());
        }

        [Test]
        public void ApplicationMembers()
        {
            Assert.AreEqual("API.OPEN_URL", CheckFixture.Single("Application.OpenURL(\"https://x\");").RuleId);
            Assert.AreEqual("API.QUIT", CheckFixture.Single("Application.Quit();").RuleId);
            Assert.AreEqual("API.SYSTEM_LANGUAGE", CheckFixture.Single("var l = Application.systemLanguage;").RuleId);
            Assert.AreEqual("API.IS_MOBILE", CheckFixture.Single("var m = Application.isMobilePlatform;").RuleId);
            Assert.AreEqual("API.IS_MOBILE", CheckFixture.Single("var d = SystemInfo.deviceType;").RuleId);
            Assert.AreEqual(
                "API.PERSISTENT_DATA_PATH", CheckFixture.Single("var p = Application.persistentDataPath;").RuleId);
        }

        [Test]
        public void MethodGroupCountsAsACall()
        {
            Assert.AreEqual("API.QUIT", CheckFixture.Single("onClick.AddListener(Application.Quit);").RuleId);
        }

        [Test]
        public void RunInBackground_OnlyOnWrite()
        {
            Assert.AreEqual(
                "API.RUN_IN_BACKGROUND_WRITE", CheckFixture.Single("Application.runInBackground = false;").RuleId);
            CheckFixture.None("var r = Application.runInBackground;");
        }

        [Test]
        public void Orientation_OnlyOnWrite()
        {
            Assert.AreEqual(
                "API.ORIENTATION_WRITE",
                CheckFixture.Single("Screen.orientation = ScreenOrientation.Portrait;").RuleId);
            Assert.AreEqual(
                "API.ORIENTATION_WRITE", CheckFixture.Single("Screen.autoRotateToPortrait = true;").RuleId);
            Assert.AreEqual(
                "API.ORIENTATION_WRITE", CheckFixture.Single("Screen.autoRotateToLandscapeLeft = false;").RuleId);
            CheckFixture.None("var o = Screen.orientation;");
        }

        [Test]
        public void FileWrites()
        {
            Assert.AreEqual(
                "API.FILE_WRITE", CheckFixture.Single("System.IO.File.WriteAllText(path, text);").RuleId);
            Assert.AreEqual(
                "API.FILE_WRITE", CheckFixture.Single("using System.IO;\nFile.AppendAllText(path, text);").RuleId);
            CheckFixture.None("System.IO.File.ReadAllText(path);");
        }

        [Test]
        public void MembersThatAreNotInTheTable()
        {
            CheckFixture.None("var d = Time.deltaTime;");
            CheckFixture.None("Debug.Log(\"x\");");
            CheckFixture.None("var w = Screen.width;");
        }

        [Test]
        public void CommentsAndStringsAreNotCode()
        {
            CheckFixture.None("// Time.timeScale = 0;");
            CheckFixture.None("var s = \"Time.timeScale = 0\";");
            CheckFixture.None("/* Time.timeScale = 0; */");
        }

        [Test]
        public void QualifiedNames()
        {
            Assert.AreEqual("API.TIMESCALE_WRITE", CheckFixture.Single("UnityEngine.Time.timeScale = 1;").RuleId);
            Assert.AreEqual(
                "API.QUIT", CheckFixture.Single("global::UnityEngine.Application.Quit();").RuleId);
        }

        [Test]
        public void ForeignQualifierIsNotAMatch()
        {
            CheckFixture.None("MyGame.Time.timeScale = 1;");
            CheckFixture.None("x.Time.timeScale = 1;");
            CheckFixture.None("x?.Time.timeScale = 1;");
        }

        [Test]
        public void QualifiedChainIsMatchedOnce()
        {
            Assert.AreEqual(1, CheckFixture.Run("UnityEngine.Time.timeScale = 0;").Count);
            Assert.AreEqual(1, CheckFixture.Run("global::UnityEngine.Time.timeScale = 0;").Count);
        }

        [Test]
        public void NameofNamesTheMemberWithoutTouchingIt()
        {
            CheckFixture.None("var n = nameof(Time.timeScale);");
            CheckFixture.None("var n = nameof(UnityEngine.Time.timeScale);");
            CheckFixture.None("var n = nameof(global::UnityEngine.Time.timeScale);");
        }
    }
}
