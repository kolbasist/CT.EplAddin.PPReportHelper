using System;
using System.Collections.Generic;

namespace CT.Epladdin.PPReportHelper
{
    internal sealed class DetachedStructureSegmentDictionary
        : Dictionary<string, List<DetachedStructureSegment>>
    {
        internal DetachedStructureSegmentDictionary()
            : base(StringComparer.Ordinal)
        {
        }

        internal static string NormalizeDescription(
            string description)
        {
            return (description ?? string.Empty)
                .Replace("\r\n", "\n")
                .Trim();
        }
    }
}