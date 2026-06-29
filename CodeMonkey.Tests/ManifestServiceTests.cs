using NUnit.Framework;
using CodeMonkey.Core.Services;
using CodeMonkey.Core.Models;
using CodeMonkey.Core.Interfaces;
using System.Linq;
using System;

namespace CodeMonkey.Tests
{
    [TestFixture]
    public class ManifestServiceTests
    {
        private ManifestService _service;

        [SetUp]
        public void Setup()
        {
            _service = new ManifestService();
        }

        [Test]
        public void CreateManifest_ShouldAddManifestToPending()
        {
            var manifest = _service.CreateManifest("TestAction", RiskLevel.Low, "Test Description", "arg1", "arg2");

            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.ActionName, Is.EqualTo("TestAction"));
            Assert.That(manifest.Risk, Is.EqualTo(RiskLevel.Low));
            Assert.That(manifest.Arguments.Count, Is.EqualTo(2));
            Assert.That(_service.GetPendingManifests(), Does.Contain(manifest));
        }

        [TestCase(TrustProfile.Strict, RiskLevel.Low, true)]
        [TestCase(TrustProfile.Strict, RiskLevel.Medium, false)]
        [TestCase(TrustProfile.Strict, RiskLevel.High, false)]
        [TestCase(TrustProfile.Balanced, RiskLevel.Low, true)]
        [TestCase(TrustProfile.Balanced, RiskLevel.Medium, true)]
        [TestCase(TrustProfile.Balanced, RiskLevel.High, false)]
        [TestCase(TrustProfile.Trusting, RiskLevel.Low, true)]
        [TestCase(TrustProfile.Trusting, RiskLevel.Medium, true)]
        [TestCase(TrustProfile.Trusting, RiskLevel.High, true)]
        public void RequestApproval_ShouldFollowRiskRules(TrustProfile profile, RiskLevel risk, bool expectedApproved)
        {
            var manifest = new Manifest { ActionName = "SimpleAction", Risk = risk, Description = "Description" };
            bool result = _service.RequestApproval(manifest, profile);

            Assert.That(result, Is.EqualTo(expectedApproved));
            Assert.That(manifest.IsApproved, Is.EqualTo(expectedApproved));
        }

        [TestCase("DeleteFile", true)]
        [TestCase("ShellExecute", true)]
        [TestCase("ReadFile", false)]
        public void RequestApproval_TrustingProfile_ShouldRequireManualApprovalForDestructiveActions(string action, bool expectedManual)
        {
            var manifest = new Manifest { ActionName = action, Risk = RiskLevel.Low, Description = "Description" };
            bool result = _service.RequestApproval(manifest, TrustProfile.Trusting);

            // If expectedManual is true, RequestApproval should return false (manual required)
            Assert.That(result, Is.EqualTo(!expectedManual));
            Assert.That(manifest.IsApproved, Is.EqualTo(!expectedManual));
        }

        [Test]
        public void ApproveManifest_ShouldMarkAsApproved()
        {
            var manifest = _service.CreateManifest("TestAction", RiskLevel.High, "Description");
            var id = manifest.Id;

            _service.ApproveManifest(id);

            Assert.That(manifest.IsApproved, Is.True);
            Assert.That(_service.GetPendingManifests(), Does.Not.Contain(manifest));
        }

        [Test]
        public void RejectManifest_ShouldRemoveFromPending()
        {
            var manifest = _service.CreateManifest("TestAction", RiskLevel.High, "Description");
            var id = manifest.Id;

            _service.RejectManifest(id);

            Assert.That(_service.GetPendingManifests(), Does.Not.Contain(manifest));
        }
    }
}
