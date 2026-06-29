using System;
using System.Collections.Generic;
using System.Linq;
using CodeMonkey.Core.Interfaces;
using CodeMonkey.Core.Models;

namespace CodeMonkey.Core.Services
{
    public class ManifestService : IManifestService
    {
        private readonly List<Manifest> _pendingManifests = new();

        public Manifest CreateManifest(string action, RiskLevel risk, string description, params string[] args)
        {
            var manifest = new Manifest
            {
                ActionName = action,
                Risk = risk,
                Description = description,
                Arguments = args.ToList()
            };
            
            _pendingManifests.Add(manifest);
            return manifest;
        }

        public bool RequestApproval(Manifest manifest, TrustProfile profile)
        {
            if (profile == TrustProfile.Trusting)
            {
                if (manifest.ActionName.Contains("Delete") || manifest.ActionName.Contains("Shell"))
                {
                    return false;
                }
                manifest.IsApproved = true;
                return true;
            }

            if (profile == TrustProfile.Balanced)
            {
                if (manifest.Risk == RiskLevel.Low || manifest.Risk == RiskLevel.Medium)
                {
                    manifest.IsApproved = true;
                    return true;
                }
                return false;
            }

            if (profile == TrustProfile.Strict)
            {
                if (manifest.Risk == RiskLevel.Low)
                {
                    manifest.IsApproved = true;
                    return true;
                }
                return false;
            }

            return false;
        }

        public IEnumerable<Manifest> GetPendingManifests()
        {
            return _pendingManifests.Where(m => !m.IsApproved);
        }

        public void ApproveManifest(Guid id)
        {
            var manifest = _pendingManifests.FirstOrDefault(m => m.Id == id);
            if (manifest != null) manifest.IsApproved = true;
        }

        public void RejectManifest(Guid id)
        {
            _pendingManifests.RemoveAll(m => m.Id == id);
        }
    }
}
