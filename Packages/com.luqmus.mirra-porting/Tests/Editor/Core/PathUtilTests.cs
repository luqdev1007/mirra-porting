using Luqmus.MirraPorting.Core;
using NUnit.Framework;

namespace Luqmus.MirraPorting.Tests.Core
{
    public class PathUtilTests
    {
        [Test]
        public void Normalize_TurnsBackslashesIntoForwardSlashesAndDropsTrailingOnes()
        {
            Assert.AreEqual("D:/ports/game/Assets", PathUtil.Normalize(@"D:\ports\game\Assets\"));
            Assert.AreEqual("Assets/Scripts", PathUtil.Normalize("Assets/Scripts//"));
            Assert.AreEqual(string.Empty, PathUtil.Normalize(null));
        }

        [Test]
        public void Join_UsesASingleSlashAndSurvivesEmptyParts()
        {
            Assert.AreEqual("Packages/com.x/Runtime/A.cs", PathUtil.Join("Packages/com.x", "Runtime/A.cs"));
            Assert.AreEqual("Assets", PathUtil.Join("Assets", string.Empty));
            Assert.AreEqual("Assets", PathUtil.Join(null, "Assets"));
        }

        [Test]
        public void IsHiddenDirectoryName_MatchesWhatUnitySkips()
        {
            Assert.IsTrue(PathUtil.IsHiddenDirectoryName(".git"));
            Assert.IsTrue(PathUtil.IsHiddenDirectoryName("Documentation~"));
            Assert.IsFalse(PathUtil.IsHiddenDirectoryName("Editor"));
            Assert.IsFalse(PathUtil.IsHiddenDirectoryName(string.Empty));
        }

        [Test]
        public void HasSegment_MatchesWholeSegmentsOnly()
        {
            Assert.IsTrue(PathUtil.HasSegment("Assets/Editor/Tool.cs", "Editor"));
            Assert.IsTrue(PathUtil.HasSegment("Editor/Tool.cs", "Editor"));
            Assert.IsTrue(PathUtil.HasSegment("Assets/Scripts/Editor", "Editor"));
            Assert.IsFalse(PathUtil.HasSegment("Assets/Editorial/Copy.cs", "Editor"));
            Assert.IsFalse(PathUtil.HasSegment("Assets/MyEditor/Copy.cs", "Editor"));
        }

        [Test]
        public void HasSegment_IgnoresCase()
        {
            Assert.IsTrue(PathUtil.HasSegment("assets/editor/tool.cs", "Editor"));
        }

        [Test]
        public void ToProjectRelative_RebasesOntoTheRootsProjectPath()
        {
            var root = new ScanRoot("Assets", "D:/ports/game/Assets", string.Empty);

            Assert.AreEqual(
                "Assets/Scripts/PauseMenu.cs",
                PathUtil.ToProjectRelative(root, @"D:\ports\game\Assets\Scripts\PauseMenu.cs"));
        }

        [Test]
        public void ToProjectRelative_LocalPackageOutsideTheProjectStillReportsUnderPackages()
        {
            // Matches what CompilationPipeline reports for a local package: the virtual path, not
            // the resolved one.
            var root = new ScanRoot("Packages/com.example.local", "C:/work/shared/local-package", "com.example.local");

            Assert.AreEqual(
                "Packages/com.example.local/Runtime/Lib.cs",
                PathUtil.ToProjectRelative(root, "C:/work/shared/local-package/Runtime/Lib.cs"));
        }

        [Test]
        public void ToProjectRelative_ReturnsTheRootItselfForTheRootPath()
        {
            var root = new ScanRoot("Assets", "D:/ports/game/Assets", string.Empty);

            Assert.AreEqual("Assets", PathUtil.ToProjectRelative(root, "D:/ports/game/Assets"));
        }

        [Test]
        public void ToProjectRelative_ReturnsNullOutsideTheRoot()
        {
            var root = new ScanRoot("Assets", "D:/ports/game/Assets", string.Empty);

            Assert.IsNull(PathUtil.ToProjectRelative(root, "D:/ports/game/Packages/com.x/A.cs"));
            Assert.IsNull(PathUtil.ToProjectRelative(root, "D:/ports/game/AssetsBackup/A.cs"));
            Assert.IsNull(PathUtil.ToProjectRelative(root, null));
        }
    }
}
