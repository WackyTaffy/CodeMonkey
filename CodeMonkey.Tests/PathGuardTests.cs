using CodeMonkey.Core.Utility;

namespace CodeMonkey.Tests
{
    [TestFixture]
    public class PathGuardTests
    {
        private string _root = @"C:\Project";

        [Test]
        public void ValidateAndNormalize_WithinRoot_ReturnsPath()
        {
            string path = @"C:\Project\file.txt";
            Assert.That(PathGuard.ValidateAndNormalize(_root, path), Is.EqualTo(Path.GetFullPath(path)));
        }

        [Test]
        public void ValidateAndNormalize_ExactRoot_ReturnsPath()
        {
            string path = @"C:\Project";
            Assert.That(PathGuard.ValidateAndNormalize(_root, path), Is.EqualTo(Path.GetFullPath(path)));
        }

        [Test]
        public void ValidateAndNormalize_Subdirectory_ReturnsPath()
        {
            string path = @"C:\Project\SubDir\file.txt";
            Assert.That(PathGuard.ValidateAndNormalize(_root, path), Is.EqualTo(Path.GetFullPath(path)));
        }

        [Test]
        public void ValidateAndNormalize_OutsideRoot_ThrowsUnauthorized()
        {
            string path = @"C:\Windows\System32\cmd.exe";
            Assert.Throws<UnauthorizedAccessException>(() => PathGuard.ValidateAndNormalize(_root, path));
        }

        [Test]
        public void ValidateAndNormalize_DirectoryTraversal_ThrowsUnauthorized()
        {
            string path = @"C:\Project\..\Windows\System32\cmd.exe";
            Assert.Throws<UnauthorizedAccessException>(() => PathGuard.ValidateAndNormalize(_root, path));
        }

        [Test]
        public void ValidateAndNormalize_SimilarRootName_ThrowsUnauthorized()
        {
            // Testing the "C:\Project" vs "C:\ProjectFiles" case
            string root = @"C:\Project";
            string path = @"C:\ProjectFiles\file.txt";
            Assert.Throws<UnauthorizedAccessException>(() => PathGuard.ValidateAndNormalize(root, path));
        }

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        [Test]
        public void ValidateAndNormalize_NullOrEmptyRoot_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => PathGuard.ValidateAndNormalize(null!, @"C:\Project\file.txt"));
            Assert.Throws<ArgumentException>(() => PathGuard.ValidateAndNormalize("", @"C:\Project\file.txt"));
            Assert.Throws<ArgumentException>(() => PathGuard.ValidateAndNormalize(" ", @"C:\Project\file.txt"));
        }

        [Test]
        public void ValidateAndNormalize_NullOrEmptyPath_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => PathGuard.ValidateAndNormalize(_root, null!));
            Assert.Throws<ArgumentException>(() => PathGuard.ValidateAndNormalize(_root, ""));
            Assert.Throws<ArgumentException>(() => PathGuard.ValidateAndNormalize(_root, " "));
        }
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        [Test]
        public void IsWithinRoot_ValidPath_ReturnsTrue()
        {
            string path = @"C:\Project\file.txt";
            Assert.That(PathGuard.IsWithinRoot(_root, path), Is.True);
        }

        [Test]
        public void IsWithinRoot_InvalidPath_ReturnsFalse()
        {
            string path = @"C:\Windows\System32\cmd.exe";
            Assert.That(PathGuard.IsWithinRoot(_root, path), Is.False);
        }

        [Test]
        public void IsWithinRoot_TraversalPath_ReturnsFalse()
        {
            string path = @"C:\Project\..\Windows\System32\cmd.exe";
            Assert.That(PathGuard.IsWithinRoot(_root, path), Is.False);
        }
    }
}
