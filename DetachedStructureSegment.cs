namespace CT.Epladdin.PPReportHelper
{
    internal sealed class DetachedStructureSegment
    {
        public string Description44005 { get; private set; }
        public string Designation44004 { get; private set; }
        public string MountingPlace1220 { get; private set; }

        /*
         * Алиасы для старого кода таблицы.
         */
        public string DescriptionText
        {
            get { return Description44005; }
        }

        public string Designation
        {
            get { return Designation44004; }
        }

        public string MountingPlace
        {
            get { return MountingPlace1220; }
        }

        internal DetachedStructureSegment(
            string description44005,
            string designation44004,
            string mountingPlace1220)
        {
            Description44005 = Normalize(description44005);
            Designation44004 = Normalize(designation44004);
            MountingPlace1220 = Normalize(mountingPlace1220);
        }

        private static string Normalize(
            string value)
        {
            return (value ?? string.Empty)
                .Replace("\r\n", "\n")
                .Trim();
        }
    }
}