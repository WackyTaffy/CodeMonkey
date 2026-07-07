using NUnit.Framework;
using NSubstitute;
using CodeMonkey.Core.Services;
using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Utility;

namespace CodeMonkey.Tests
{
    [TestFixture]
    public class ContextGuardTests
    {
        private ITokenHelper _mockTokenHelper;
        private ContextGuard _contextGuard;

        [SetUp]
        public void SetUp()
        {
            _mockTokenHelper = Substitute.For<ITokenHelper>();
            _contextGuard = new ContextGuard(_mockTokenHelper);
        }

        [Test]
        public void Guard_ReturnsOriginalString_WhenInputIsBelowLimit()
        {
            // Arrange
            string input = "Hello world";
            int maxTokens = 10;
            _mockTokenHelper.GetTokenCount(input).Returns(3);

            // Act
            string result = _contextGuard.Guard(input, maxTokens);

            // Assert
            Assert.That(result, Is.EqualTo(input));
        }

        [Test]
        public void Guard_ReturnsOriginalString_WhenInputIsExactlyAtLimit()
        {
            // Arrange
            string input = "Hello world";
            int maxTokens = 3;
            _mockTokenHelper.GetTokenCount(input).Returns(3);

            // Act
            string result = _contextGuard.Guard(input, maxTokens);

            // Assert
            Assert.That(result, Is.EqualTo(input));
        }

        [Test]
        public void Guard_TruncatesAndAppendsNotice_WhenInputExceedsLimit()
        {
            // Arrange
            string input = "This is a very long string that should be truncated because it exceeds the token limit.";
            int maxTokens = 5; // approx 20 chars
            _mockTokenHelper.GetTokenCount(input).Returns(20);

            // Act
            string result = _contextGuard.Guard(input, maxTokens);

            // Assert
            Assert.That(result, Does.Contain(ContextConstants.TruncationNotice));
            Assert.That(result.Length, Is.LessThan(input.Length + ContextConstants.TruncationNotice.Length));
            // Check if it's truncated (approx 5 * 4 = 20 chars)
            string expectedStart = input.Substring(0, 20);
            Assert.That(result, Does.StartWith(expectedStart));
        }

        [Test]
        public void Guard_ReturnsInput_WhenInputIsNullOrEmpty()
        {
            // Act & Assert
            Assert.That(_contextGuard.Guard(null!, 10), Is.Null);
            Assert.That(_contextGuard.Guard(string.Empty, 10), Is.EqualTo(string.Empty));
        }

        [Test]
        public void GetTokenCount_ReturnsValueFromHelper()
        {
            // Arrange
            string text = "Test text";
            _mockTokenHelper.GetTokenCount(text).Returns(2);

            // Act
            int count = _contextGuard.GetTokenCount(text);

            // Assert
            Assert.That(count, Is.EqualTo(2));
            _mockTokenHelper.Received(1).GetTokenCount(text);
        }
    }
}
