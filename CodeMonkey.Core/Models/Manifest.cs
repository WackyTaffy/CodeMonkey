using System;
using System.Collections.Generic;

namespace CodeMonkey.Core.Models
{
    public enum RiskLevel
    {
        Low,
        Medium,
        High
    }

    public enum TrustProfile
    {
        Strict,
        Balanced,
        Trusting
    }

    public class Manifest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string ActionName { get; set; }
        public RiskLevel Risk { get; set; }
        public List<string> Arguments { get; set; } = new();
        public required string Description { get; set; }
        public bool IsApproved { get; set; } = false;

        public override string ToString() => $"[{Risk}] {ActionName}: {Description}";
    }
}
