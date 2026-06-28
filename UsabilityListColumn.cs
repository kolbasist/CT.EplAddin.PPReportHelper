namespace CT.Epladdin.PPReportHelper
{
    internal sealed class UsabilityListColumn
    {
        internal string Header { get; private set; }
        internal double Width { get; private set; }
        internal UsabilityListColumnContent Content { get; private set; }

        internal UsabilityListColumn(
            string header,
            double width,
            UsabilityListColumnContent content)
        {
            Header = header ?? string.Empty;
            Width = width;
            Content = content;
        }
    }
}