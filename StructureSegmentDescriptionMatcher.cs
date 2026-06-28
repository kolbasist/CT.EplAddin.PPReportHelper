using System;
using System.Collections.Generic;
using System.Linq;
using Eplan.EplApi.Base;
using Eplan.EplApi.DataModel;
using Eplan.EplApi.DataModel.Planning;
using PpStructureSegment = Eplan.EplApi.DataModel.Planning.StructureSegment;

namespace CT.Epladdin.PPReportHelper
{
    internal sealed class StructureSegmentDescriptionMatcher : IDisposable
    {
        private readonly Func<PpStructureSegment, MultiLangString> _descriptionGetter;
        private readonly List<StorableObject> _objectsToDispose = new List<StorableObject>();

        private readonly bool _includeEmptyDescriptionMatches;
        private readonly bool _includeSourceSegment;
        private readonly StringComparison _descriptionComparison;

        public StructureSegmentDescriptionMatcher(
            Func<PpStructureSegment, MultiLangString> descriptionGetter,
            bool includeEmptyDescriptionMatches = false,
            bool includeSourceSegment = true,
            StringComparison descriptionComparison = StringComparison.Ordinal)
        {
            _descriptionGetter = descriptionGetter ?? throw new ArgumentNullException(nameof(descriptionGetter));
            _includeEmptyDescriptionMatches = includeEmptyDescriptionMatches;
            _includeSourceSegment = includeSourceSegment;
            _descriptionComparison = descriptionComparison;
        }

        public IReadOnlyList<PpStructureSegment> FindSegmentsWithSameDescription(PpStructureSegment sourceSegment)
        {
            if (sourceSegment == null || !sourceSegment.IsValid)
            {
                return Array.Empty<PpStructureSegment>();
            }

            Project project = sourceSegment.Project;

            if (project == null || !project.IsValid)
            {
                return Array.Empty<PpStructureSegment>();
            }

            string sourceDescriptionKey = GetDescriptionKey(sourceSegment);

            if (!_includeEmptyDescriptionMatches && string.IsNullOrWhiteSpace(sourceDescriptionKey))
            {
                return Array.Empty<PpStructureSegment>();
            }

            List<PpStructureSegment> result = new List<PpStructureSegment>();

            foreach (PpStructureSegment candidate in FindAllStructureSegments(project))
            {
                if (candidate == null || !candidate.IsValid)
                {
                    SafeDispose(candidate);
                    continue;
                }

                if (!_includeSourceSegment && IsSameSegment(sourceSegment, candidate))
                {
                    SafeDispose(candidate);
                    continue;
                }

                string candidateDescriptionKey = GetDescriptionKey(candidate);

                if (!string.Equals(sourceDescriptionKey, candidateDescriptionKey, _descriptionComparison))
                {
                    SafeDispose(candidate);
                    continue;
                }

                result.Add(candidate);
                _objectsToDispose.Add(candidate);
            }

            return result;
        }

        private string GetDescriptionKey(PpStructureSegment segment)
        {
            MultiLangString description = _descriptionGetter(segment);
            return NormalizeDescription(description);
        }

        private static string NormalizeDescription(MultiLangString description)
        {
            if (description == null)
            {
                return string.Empty;
            }

            return (description.GetAsString() ?? string.Empty)
                .Replace("\r\n", "\n")
                .Trim();
        }

        private static bool IsSameSegment(PpStructureSegment first, PpStructureSegment second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            if (ReferenceEquals(first, second))
            {
                return true;
            }

            // В большинстве случаев Name для структурного сегмента — это его обозначение.
            // Этого достаточно, чтобы не тащить Properties.DESIGNATION_FULLLOCATION.
            return string.Equals(first.Name, second.Name, StringComparison.Ordinal);
        }

        private static IEnumerable<PpStructureSegment> FindAllStructureSegments(Project project)
        {
            DMObjectsFinder finder = new DMObjectsFinder(project);

            // Проверь только эту строку по своей версии API.
            // Если GetPlanningSegments(null) не компилируется, заменим на вариант с фильтром.
            PlanningSegment[] planningSegments = finder.GetPlanningSegments(null);

            if (planningSegments == null)
            {
                yield break;
            }

            foreach (PlanningSegment planningSegment in planningSegments)
            {
                PpStructureSegment structureSegment = planningSegment as PpStructureSegment;

                if (structureSegment != null)
                {
                    yield return structureSegment;
                }
                else
                {
                    SafeDispose(planningSegment);
                }
            }
        }

        private static void SafeDispose(StorableObject storableObject)
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
                // Dispose не должен ломать выполнение action-а.
            }
        }

        public void Dispose()
        {
            foreach (StorableObject storableObject in _objectsToDispose)
            {
                SafeDispose(storableObject);
            }

            _objectsToDispose.Clear();
        }
    }
}