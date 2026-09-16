using System.IO;
using Luqmus.MirraPorting.Core;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Core
{
    public class AsmdefProbeTests
    {
        [Test]
        public void ReadAssemblyNameFromJson_TakesTheNameAndIgnoresTheRest()
        {
            const string json = "{\"name\": \"MirraGames.SDK.Common\", \"references\": [\"A\", \"B\"], " +
                                "\"includePlatforms\": [], \"autoReferenced\": false}";

            Assert.AreEqual("MirraGames.SDK.Common", AsmdefProbe.ReadAssemblyNameFromJson(json));
        }

        [Test]
        public void ReadAssemblyNameFromJson_BrokenOrEmptyJsonGivesAnEmptyName()
        {
            Assert.AreEqual(string.Empty, AsmdefProbe.ReadAssemblyNameFromJson("{"));
            Assert.AreEqual(string.Empty, AsmdefProbe.ReadAssemblyNameFromJson("not json at all"));
            Assert.AreEqual(string.Empty, AsmdefProbe.ReadAssemblyNameFromJson("{}"));
            Assert.AreEqual(string.Empty, AsmdefProbe.ReadAssemblyNameFromJson(string.Empty));
            Assert.AreEqual(string.Empty, AsmdefProbe.ReadAssemblyNameFromJson(null));
        }

        [Test]
        public void ReadAssemblyName_MissingFileGivesAnEmptyName()
        {
            string missing = Path.Combine(Path.GetTempPath(), "no-such-folder-42", "Some.asmdef");

            Assert.AreEqual(string.Empty, AsmdefProbe.ReadAssemblyName(missing));
        }

        [Test]
        public void IsSdkAssemblyName_MatchesEverySdkAssemblyByPrefix()
        {
            Assert.IsTrue(AsmdefProbe.IsSdkAssemblyName("MirraGames.SDK"));
            Assert.IsTrue(AsmdefProbe.IsSdkAssemblyName("MirraGames.SDK.Common"));
            Assert.IsTrue(AsmdefProbe.IsSdkAssemblyName("MirraGames.SDK.MirraWeb"));
        }

        [Test]
        public void IsSdkAssemblyName_RejectsGameAssembliesAndIsCaseSensitive()
        {
            Assert.IsFalse(AsmdefProbe.IsSdkAssemblyName("Assembly-CSharp"));
            Assert.IsFalse(AsmdefProbe.IsSdkAssemblyName("MyGame.Mirra"));
            Assert.IsFalse(AsmdefProbe.IsSdkAssemblyName("mirragames.sdk"));
            Assert.IsFalse(AsmdefProbe.IsSdkAssemblyName(null));
        }
    }
}
