using System;
using System.Collections.Generic;
using Eplan.EplApi.DataModel;
using PpStructureSegment = Eplan.EplApi.DataModel.Planning.StructureSegment;

namespace CT.Epladdin.PPReportHelper
{
    internal sealed class StructureSegmentDictionary
        : Dictionary<string, List<PpStructureSegment>>, IDisposable
    {
        private readonly Func<PpStructureSegment, string> _descriptionGetter;
        private readonly List<StorableObject> _objectsToDispose =
            new List<StorableObject>();

        private readonly bool _includeEmptyDescriptions;

        public StructureSegmentDictionary(
            Func<PpStructureSegment, string> descriptionGetter,
            bool includeEmptyDescriptions = false)
            : base(StringComparer.Ordinal)
        {
            _descriptionGetter = descriptionGetter
                ?? throw new ArgumentNullException(nameof(descriptionGetter));

            _includeEmptyDescriptions = includeEmptyDescriptions;
        }

        public void Fill(
            IEnumerable<PpStructureSegment> structureSegments)
        {
            ClearAndDispose();

            if (structureSegments == null)
            {
                return;
            }

            foreach (PpStructureSegment structureSegment in structureSegments)
            {
                AddStructureSegment(structureSegment);
            }
        }

        private void AddStructureSegment(
            PpStructureSegment structureSegment)
        {
            if (structureSegment == null || !structureSegment.IsValid)
            {
                SafeDispose(structureSegment);
                return;
            }

            string description =
                NormalizeDescription(
                    _descriptionGetter(structureSegment));

            if (!_includeEmptyDescriptions &&
                string.IsNullOrWhiteSpace(description))
            {
                SafeDispose(structureSegment);
                return;
            }

            List<PpStructureSegment> segments;

            if (!TryGetValue(description, out segments))
            {
                segments =
                    new List<PpStructureSegment>();

                Add(description, segments);
            }

            segments.Add(structureSegment);
            _objectsToDispose.Add(structureSegment);
        }

        public static string NormalizeDescription(
            string description)
        {
            return (description ?? string.Empty)
                .Replace("\r\n", "\n")
                .Trim();
        }

        private void ClearAndDispose()
        {
            foreach (StorableObject storableObject in _objectsToDispose)
            {
                SafeDispose(storableObject);
            }

            _objectsToDispose.Clear();
            Clear();
        }

        private static void SafeDispose(
            StorableObject storableObject)
        {
            if (storableObject == null)
            {
                return;
            }

            try
            {
                storableObject.Dispose();
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            ClearAndDispose();
        }
    }
}