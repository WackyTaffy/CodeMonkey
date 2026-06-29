using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace CodeMonkey.Core.Interfaces
{
    public interface IManifestService
    {
        Manifest CreateManifest(string action, RiskLevel risk, string description, params string[] args);
        bool RequestApproval(Manifest manifest, TrustProfile profile);
        IEnumerable<Manifest> GetPendingManifests();
        void ApproveManifest(Guid id);
        void RejectManifest(Guid id);
    }
}
