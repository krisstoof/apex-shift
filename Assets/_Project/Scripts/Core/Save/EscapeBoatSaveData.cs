using System;
using System.Collections.Generic;
using System.Linq;

namespace ApexShift.Core.Save
{
    [Serializable]
    public sealed class EscapeBoatSaveData
    {
        public List<string> collectedRequirementSourceIds = new List<string>();
        public IReadOnlyList<string> CollectedRequirementSourceIds =>
            (collectedRequirementSourceIds ?? new List<string>()).Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim().ToLowerInvariant()).Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal).ToArray();
        public static EscapeBoatSaveData Default => new EscapeBoatSaveData();
    }
}
