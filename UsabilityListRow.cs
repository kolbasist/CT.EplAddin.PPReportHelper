namespace CT.Epladdin.PPReportHelper
{
    internal sealed class UsabilityListRow
    {
        internal int Number { get; private set; }

        internal string StructureSegmentDesignation { get; private set; }
        internal string StructureSegmentMountingPlace { get; private set; }
        internal string StructureSegmentDescription { get; private set; }

        internal bool IsPlaceholder { get; private set; }
        internal string PlaceholderText { get; private set; }

        internal UsabilityListRow(
            int number,
            string structureSegmentDesignation,
            string structureSegmentMountingPlace,
            string structureSegmentDescription)
        {
            Number = number;

            StructureSegmentDesignation =
                structureSegmentDesignation ?? string.Empty;

            StructureSegmentMountingPlace =
                structureSegmentMountingPlace ?? string.Empty;

            StructureSegmentDescription =
                structureSegmentDescription ?? string.Empty;

            IsPlaceholder = false;
            PlaceholderText = string.Empty;
        }

        private UsabilityListRow(
            string placeholderText)
        {
            Number = 0;

            StructureSegmentDesignation = string.Empty;
            StructureSegmentMountingPlace = string.Empty;
            StructureSegmentDescription = string.Empty;

            IsPlaceholder = true;
            PlaceholderText = placeholderText ?? string.Empty;
        }

        internal static UsabilityListRow CreatePlaceholder(
            string text)
        {
            return new UsabilityListRow(text);
        }
    }
}