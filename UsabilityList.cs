using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Eplan.EplApi.Base;
using Eplan.EplApi.DataModel;
using Eplan.EplApi.DataModel.Graphics;
using Eplan.EplApi.HEServices;

namespace CT.Epladdin.PPReportHelper
{
    internal sealed class UsabilityList
    {
        internal const string TableMarkerPrefix = "CT_USABILITY_LIST#";

        private const string StateTextPrefix = "CT_USABILITY_LIST_STATE|";

        private readonly List<UsabilityListColumn> _columns =
            new List<UsabilityListColumn>();

        private DetachedStructureSegmentDictionary _dictionary;

        private Page _ownerPage;
        private Block _block;

        internal double OriginX { get; private set; }
        internal double OriginY { get; private set; }

        internal double HeaderHeight { get; private set; }
        internal double RowHeight { get; private set; }
        internal double TitleHeight { get; private set; }

        internal double TextHeight { get; private set; }
        internal double LineWidth { get; private set; }

        internal string Title { get; private set; }

        [Category("Таблица")]
        [Description("Заголовок таблицы")]
        public string TABLE_TITLE
        {
            get { return Title; }
            set { Title = value ?? string.Empty; }
        }

        [Category("Положение")]
        public double ORIGIN_X
        {
            get { return OriginX; }
            set { OriginX = value; }
        }

        [Category("Положение")]
        public double ORIGIN_Y
        {
            get { return OriginY; }
            set { OriginY = value; }
        }

        [Category("Размеры")]
        public double TITLE_HEIGHT
        {
            get { return TitleHeight; }
            set { TitleHeight = value; }
        }

        [Category("Размеры")]
        public double HEADER_HEIGHT
        {
            get { return HeaderHeight; }
            set { HeaderHeight = value; }
        }

        [Category("Размеры")]
        public double ROW_HEIGHT
        {
            get { return RowHeight; }
            set { RowHeight = value; }
        }

        [Category("Размеры")]
        public double TEXT_HEIGHT
        {
            get { return TextHeight; }
            set { TextHeight = value; }
        }

        [Category("Размеры")]
        public double LINE_WIDTH
        {
            get { return LineWidth; }
            set { LineWidth = value; }
        }

        [Category("Заголовки столбцов")]
        public string NUMBER_HEADER { get; set; } = "№\nп/п";

        [Category("Заголовки столбцов")]
        public string DESIGNATION_HEADER { get; set; } = "Обозначение\nвентустановки";

        [Category("Заголовки столбцов")]
        public string MOUNTING_PLACE_HEADER { get; set; } = "Обозначние\nщита АСУ";

        [Category("Заголовки столбцов")]
        public string DESCRIPTION_HEADER { get; set; } = "Примечание";

        [Category("Ширины столбцов")]
        public double NUMBER_COLUMN_WIDTH { get; set; } = 10.0;

        [Category("Ширины столбцов")]
        public double DESIGNATION_COLUMN_WIDTH { get; set; } = 35.0;

        [Category("Ширины столбцов")]
        public double MOUNTING_PLACE_COLUMN_WIDTH { get; set; } = 25.0;

        [Category("Ширины столбцов")]
        public double DESCRIPTION_COLUMN_WIDTH { get; set; } = 65.0;

        internal UsabilityList(
            Page ownerPage,
            DetachedStructureSegmentDictionary dictionary)
        {
            if (ownerPage == null || !ownerPage.IsValid)
            {
                throw new ArgumentException(
                    "Owner page is null or invalid.",
                    nameof(ownerPage));
            }

            _ownerPage = ownerPage;
            _dictionary = dictionary ?? new DetachedStructureSegmentDictionary();

            OriginX = 20.0;
            OriginY = 20.0;

            ApplyDefaultSettings();
            InitializeDefaultColumns();
        }

        internal UsabilityList(
            Block block)
        {
            if (block == null || !block.IsValid)
            {
                throw new ArgumentException(
                    "Block is null or invalid.",
                    nameof(block));
            }

            _block = block;
            _ownerPage = block.Page;
            _dictionary = new DetachedStructureSegmentDictionary();

            OriginX = 20.0;
            OriginY = 20.0;

            ApplyDefaultSettings();
            InitializeDefaultColumns();
            RestoreStateFromBlock(block);
        }

        internal void SetData(
            DetachedStructureSegmentDictionary dictionary)
        {
            _dictionary =
                dictionary ?? new DetachedStructureSegmentDictionary();
        }

        internal void SetInsertionPoint(
            PointD insertionPoint)
        {
            OriginX = insertionPoint.X;
            OriginY = insertionPoint.Y;
        }

        internal void Render()
        {
            if (_ownerPage == null || !_ownerPage.IsValid)
            {
                WriteSystemMessage(
                    "UsabilityList render skipped: owner page is invalid.");

                return;
            }

            InitializeDefaultColumns();

            IReadOnlyList<UsabilityListRow> rows =
                BuildRows(_dictionary);

            using (SafetyPoint safetyPoint = SafetyPoint.Create())
            {
                RemovePreviousGraphics();

                List<Placement> placements =
                    CreateTablePlacements(rows);

                _block =
                    new Block();

                _block.Create(
                    _ownerPage,
                    placements.ToArray());

                _block.Name =
                    TableMarkerPrefix + Guid.NewGuid().ToString("N");

                safetyPoint.Commit();
            }

            RedrawGed();
        }

        internal void RefreshOnReportsUpdated()
        {
            if (_ownerPage == null || !_ownerPage.IsValid)
            {
                return;
            }

            if (!IsOwnerPageActiveInGedOrSelectedInPageNavigator())
            {
                return;
            }

            StructureSegmentSeeker seeker =
                new StructureSegmentSeeker();

            DetachedStructureSegmentDictionary updatedDictionary =
                seeker.GetDictionaryCopyAndDispose();

            SetData(updatedDictionary);
            Render();
        }

        internal static void RefreshPlacedListsOnReportsUpdated()
        {
            Project project =
                GetCurrentProject();

            if (project == null || !project.IsValid)
            {
                return;
            }

            DMObjectsFinder finder =
                new DMObjectsFinder(project);

            StorableObject[] storableObjects =
                finder.GetStorableObjects((StorableObjectsFilter)null);

            if (storableObjects == null)
            {
                return;
            }

            foreach (Block block in storableObjects
                         .OfType<Block>()
                         .Where(IsUsabilityListBlock)
                         .ToArray())
            {
                try
                {
                    UsabilityList list =
                        new UsabilityList(block);

                    list.RefreshOnReportsUpdated();
                }
                catch
                {
                }
            }
        }

        internal static bool IsUsabilityListBlock(
            Block block)
        {
            return block != null &&
                   block.IsValid &&
                   !string.IsNullOrWhiteSpace(block.Name) &&
                   block.Name.StartsWith(
                       TableMarkerPrefix,
                       StringComparison.Ordinal);
        }

        private void ApplyDefaultSettings()
        {
            UsabilityListSettings settings =
                UsabilityListSettings.Instance;

            settings.LoadSettings();

            Title = settings.TITLE;

            TitleHeight = settings.TITLE_HEIGHT;
            HeaderHeight = settings.HEADER_HEIGHT;
            RowHeight = settings.ROW_HEIGHT;
            TextHeight = settings.TEXT_HEIGHT;
            LineWidth = settings.LINE_WIDTH;

            NUMBER_HEADER = settings.NUMBER_HEADER;
            DESIGNATION_HEADER = settings.DESIGNATION_HEADER;
            MOUNTING_PLACE_HEADER = settings.MOUNTING_PLACE_HEADER;
            DESCRIPTION_HEADER = settings.DESCRIPTION_HEADER;

            NUMBER_COLUMN_WIDTH = settings.NUMBER_WIDTH;
            DESIGNATION_COLUMN_WIDTH = settings.DESIGNATION_WIDTH;
            MOUNTING_PLACE_COLUMN_WIDTH = settings.MOUNTING_PLACE_WIDTH;
            DESCRIPTION_COLUMN_WIDTH = settings.DESCRIPTION_WIDTH;
        }

        private void InitializeDefaultColumns()
        {
            _columns.Clear();

            _columns.Add(
                new UsabilityListColumn(
                    NUMBER_HEADER,
                    NUMBER_COLUMN_WIDTH,
                    UsabilityListColumnContent.RowNumber));

            _columns.Add(
                new UsabilityListColumn(
                    DESIGNATION_HEADER,
                    DESIGNATION_COLUMN_WIDTH,
                    UsabilityListColumnContent.StructureSegmentDesignation));

            _columns.Add(
                new UsabilityListColumn(
                    MOUNTING_PLACE_HEADER,
                    MOUNTING_PLACE_COLUMN_WIDTH,
                    UsabilityListColumnContent.StructureSegmentMountingPlace));

            _columns.Add(
                new UsabilityListColumn(
                    DESCRIPTION_HEADER,
                    DESCRIPTION_COLUMN_WIDTH,
                    UsabilityListColumnContent.StructureSegmentDescription));
        }

        private IReadOnlyList<UsabilityListRow> BuildRows(
            DetachedStructureSegmentDictionary dictionary)
        {
            List<UsabilityListRow> rows =
                new List<UsabilityListRow>();

            if (dictionary == null || dictionary.Count == 0)
            {
                rows.Add(
                    UsabilityListRow.CreatePlaceholder(
                        "Нет данных для отображения"));

                return rows;
            }

            int rowNumber = 1;

            foreach (KeyValuePair<string, List<DetachedStructureSegment>> pair
                     in dictionary.OrderBy(item => item.Key ?? string.Empty))
            {
                string description =
                    DetachedStructureSegmentDictionary.NormalizeDescription(pair.Key);

                List<DetachedStructureSegment> segments =
                    pair.Value ?? new List<DetachedStructureSegment>();

                foreach (DetachedStructureSegment segment
                         in segments
                             .OrderBy(item => item.MountingPlace1220 ?? string.Empty)
                             .ThenBy(item => item.Designation44004 ?? string.Empty))
                {
                    rows.Add(
                        new UsabilityListRow(
                            rowNumber,
                            segment.Designation44004,
                            segment.MountingPlace1220,
                            description));

                    rowNumber++;
                }
            }

            if (rows.Count == 0)
            {
                rows.Add(
                    UsabilityListRow.CreatePlaceholder(
                        "Нет данных для отображения"));
            }

            return rows;
        }

        private List<Placement> CreateTablePlacements(
     IReadOnlyList<UsabilityListRow> rows)
        {
            List<Placement> placements =
                new List<Placement>();

            double totalWidth =
                GetTotalWidth();

            /*
             * Заголовок таблицы больше не рисуем.
             * Высота таблицы = шапка столбцов + строки данных.
             */
            double totalHeight =
                HeaderHeight + rows.Count * RowHeight;

            CreateFrameLines(
                placements,
                totalWidth,
                totalHeight,
                rows.Count);

            CreateHeaderTexts(placements);
            CreateRowTexts(placements, rows);
            CreateStateText(placements);

            return placements;
        }

        private void CreateFrameLines(
    List<Placement> placements,
    double totalWidth,
    double totalHeight,
    int rowCount)
        {
            /*
             * OriginX / OriginY теперь считаем верхним левым углом таблицы.
             * Таблица строится сверху вниз, то есть по Y в минус.
             */

            double left = OriginX;
            double right = OriginX + totalWidth;

            double top = OriginY;
            double bottom = OriginY - totalHeight;

            AddLine(placements, left, top, right, top);
            AddLine(placements, left, bottom, right, bottom);
            AddLine(placements, left, top, left, bottom);
            AddLine(placements, right, top, right, bottom);

            double headerBottom =
                OriginY - HeaderHeight;

            AddLine(placements, left, headerBottom, right, headerBottom);

            for (int i = 0; i < rowCount; i++)
            {
                double y =
                    headerBottom - (i + 1) * RowHeight;

                AddLine(placements, left, y, right, y);
            }

            double x =
                OriginX;

            foreach (UsabilityListColumn column in _columns)
            {
                x += column.Width;

                if (x < right)
                {
                    AddLine(placements, x, top, x, bottom);
                }
            }
        }

        private void CreateHeaderTexts(
     List<Placement> placements)
        {
            double x = OriginX;
            double y = OriginY;

            foreach (UsabilityListColumn column in _columns)
            {
                AddTextInCellCenter(
                    placements,
                    x,
                    y,
                    column.Width,
                    HeaderHeight,
                    column.Header,
                    TextHeight);

                x += column.Width;
            }
        }

        private void CreateRowTexts(
     List<Placement> placements,
     IReadOnlyList<UsabilityListRow> rows)
        {
            double y =
                OriginY - HeaderHeight;

            foreach (UsabilityListRow row in rows)
            {
                double x =
                    OriginX;

                foreach (UsabilityListColumn column in _columns)
                {
                    string text =
                        GetCellText(
                            row,
                            column);

                    AddTextInCellCenter(
                        placements,
                        x,
                        y,
                        column.Width,
                        RowHeight,
                        text,
                        TextHeight);

                    x += column.Width;
                }

                y -= RowHeight;
            }
        }

        private void AddTextInCellCenter(
    List<Placement> placements,
    double cellX,
    double cellY,
    double cellWidth,
    double cellHeight,
    string text,
    double textHeight)
        {
            double textX =
                cellX + cellWidth / 2.0;

            double textY =
                cellY - cellHeight / 2.0;

            AddText(
                placements,
                textX,
                textY,
                text ?? string.Empty,
                textHeight,
                TextBase.JustificationType.MiddleCenter);
        }

        private void AddText(
            List<Placement> placements,
            double x,
            double y,
            string text,
            double height,
            TextBase.JustificationType justification)
        {
            Text graphicText =
                new Text();

            graphicText.Create(
                _ownerPage,
                text ?? string.Empty,
                height);

            graphicText.Location =
                new PointD(x, y);

            graphicText.Justification =
                justification;

            placements.Add(graphicText);
        }

        private void AddLine(
            List<Placement> placements,
            double x1,
            double y1,
            double x2,
            double y2)
        {
            Line line =
                new Line();

            line.Create(_ownerPage);

            line.StartPoint =
                new PointD(x1, y1);

            line.EndPoint =
                new PointD(x2, y2);

            Pen pen =
                new Pen
                {
                    ColorId = 0,
                    StyleId = 0,
                    StyleFactor = -16002.0,
                    Width = LineWidth,
                    LineEndType = 0
                };

            line.Pen =
                pen;

            placements.Add(line);
        }

        private static string GetCellText(
            UsabilityListRow row,
            UsabilityListColumn column)
        {
            if (row == null || column == null)
            {
                return string.Empty;
            }

            if (row.IsPlaceholder)
            {
                return column.Content == UsabilityListColumnContent.StructureSegmentDescription
                    ? row.PlaceholderText
                    : string.Empty;
            }

            switch (column.Content)
            {
                case UsabilityListColumnContent.RowNumber:
                    return row.Number.ToString();

                case UsabilityListColumnContent.StructureSegmentDesignation:
                    return row.StructureSegmentDesignation ?? string.Empty;

                case UsabilityListColumnContent.StructureSegmentMountingPlace:
                    return row.StructureSegmentMountingPlace ?? string.Empty;

                case UsabilityListColumnContent.StructureSegmentDescription:
                    return row.StructureSegmentDescription ?? string.Empty;

                default:
                    return string.Empty;
            }
        }

        private void CreateStateText(
            List<Placement> placements)
        {
            Text stateText =
                new Text();

            stateText.Create(
                _ownerPage,
                StateTextPrefix + SerializeState(),
                0.0);

            stateText.Location =
                new PointD(OriginX, OriginY);

            stateText.IsSetAsVisible =
                Placement.Visibility.Invisible;

            placements.Add(stateText);
        }

        private string SerializeState()
        {
            UsabilityListState state =
                new UsabilityListState
                {
                    OriginX = OriginX,
                    OriginY = OriginY,

                    Title = Title,

                    TitleHeight = TitleHeight,
                    HeaderHeight = HeaderHeight,
                    RowHeight = RowHeight,
                    TextHeight = TextHeight,
                    LineWidth = LineWidth,

                    NumberHeader = NUMBER_HEADER,
                    DesignationHeader = DESIGNATION_HEADER,
                    MountingPlaceHeader = MOUNTING_PLACE_HEADER,
                    DescriptionHeader = DESCRIPTION_HEADER,

                    NumberColumnWidth = NUMBER_COLUMN_WIDTH,
                    DesignationColumnWidth = DESIGNATION_COLUMN_WIDTH,
                    MountingPlaceColumnWidth = MOUNTING_PLACE_COLUMN_WIDTH,
                    DescriptionColumnWidth = DESCRIPTION_COLUMN_WIDTH
                };

            DataContractJsonSerializer serializer =
                new DataContractJsonSerializer(typeof(UsabilityListState));

            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(
                    stream,
                    state);

                return Encoding.UTF8.GetString(
                    stream.ToArray());
            }
        }

        private void DeserializeState(
            string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            try
            {
                DataContractJsonSerializer serializer =
                    new DataContractJsonSerializer(typeof(UsabilityListState));

                using (MemoryStream stream =
                       new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    UsabilityListState state =
                        serializer.ReadObject(stream) as UsabilityListState;

                    if (state == null)
                    {
                        return;
                    }

                    OriginX = state.OriginX;
                    OriginY = state.OriginY;

                    Title = state.Title ?? Title;

                    if (state.TitleHeight > 0.0)
                    {
                        TitleHeight = state.TitleHeight;
                    }

                    if (state.HeaderHeight > 0.0)
                    {
                        HeaderHeight = state.HeaderHeight;
                    }

                    if (state.RowHeight > 0.0)
                    {
                        RowHeight = state.RowHeight;
                    }

                    if (state.TextHeight > 0.0)
                    {
                        TextHeight = state.TextHeight;
                    }

                    if (state.LineWidth > 0.0)
                    {
                        LineWidth = state.LineWidth;
                    }

                    NUMBER_HEADER =
                        state.NumberHeader ?? NUMBER_HEADER;

                    DESIGNATION_HEADER =
                        state.DesignationHeader ?? DESIGNATION_HEADER;

                    MOUNTING_PLACE_HEADER =
                        state.MountingPlaceHeader ?? MOUNTING_PLACE_HEADER;

                    DESCRIPTION_HEADER =
                        state.DescriptionHeader ?? DESCRIPTION_HEADER;

                    if (state.NumberColumnWidth > 0.0)
                    {
                        NUMBER_COLUMN_WIDTH =
                            state.NumberColumnWidth;
                    }

                    if (state.DesignationColumnWidth > 0.0)
                    {
                        DESIGNATION_COLUMN_WIDTH =
                            state.DesignationColumnWidth;
                    }

                    if (state.MountingPlaceColumnWidth > 0.0)
                    {
                        MOUNTING_PLACE_COLUMN_WIDTH =
                            state.MountingPlaceColumnWidth;
                    }

                    if (state.DescriptionColumnWidth > 0.0)
                    {
                        DESCRIPTION_COLUMN_WIDTH =
                            state.DescriptionColumnWidth;
                    }
                }
            }
            catch
            {
                WriteSystemMessage(
                    "UsabilityList state deserialize error.");
            }
        }

        private void RestoreStateFromBlock(
            Block block)
        {
            if (block == null || !block.IsValid)
            {
                return;
            }

            Placement[] subPlacements =
                block.SubPlacements;

            if (subPlacements == null)
            {
                return;
            }

            foreach (Text text in subPlacements.OfType<Text>())
            {
                string contents =
                    string.Empty;

                try
                {
                    contents =
                        text.Contents.GetAsString();
                }
                catch
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(contents))
                {
                    continue;
                }

                if (!contents.StartsWith(
                        StateTextPrefix,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                RestoreStateFromString(contents);
                return;
            }
        }

        private void RestoreStateFromString(
            string state)
        {
            if (string.IsNullOrWhiteSpace(state))
            {
                return;
            }

            if (!state.StartsWith(
                    StateTextPrefix,
                    StringComparison.Ordinal))
            {
                return;
            }

            string raw =
                state.Substring(StateTextPrefix.Length);

            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            DeserializeState(raw);
        }

        private void RemovePreviousGraphics()
        {
            if (_block == null || !_block.IsValid)
            {
                return;
            }

            Placement[] oldPlacements =
                _block.BreakUp();

            _block = null;

            if (oldPlacements == null)
            {
                return;
            }

            foreach (Placement placement in oldPlacements)
            {
                if (placement == null || !placement.IsValid)
                {
                    continue;
                }

                try
                {
                    placement.Remove();
                }
                catch
                {
                }
            }
        }

        private bool IsOwnerPageActiveInGedOrSelectedInPageNavigator()
        {
            Page activePage =
                TryGetActivePageFromGraphicalEditor();

            if (IsSamePage(_ownerPage, activePage))
            {
                return true;
            }

            foreach (Page selectedPage in GetSelectedPages())
            {
                if (IsSamePage(_ownerPage, selectedPage))
                {
                    return true;
                }
            }

            return false;
        }

        private double GetTotalWidth()
        {
            return _columns.Sum(
                column => column.Width);
        }

        private static Project GetCurrentProject()
        {
            try
            {
                SelectionSet selectionSet =
                    new SelectionSet
                    {
                        LockProjectByDefault = false,
                        LockSelectionByDefault = false
                    };

                return selectionSet.GetCurrentProject(true);
            }
            catch
            {
                return null;
            }
        }

        private static Page TryGetActivePageFromGraphicalEditor()
        {
            try
            {
                Type graphicalEditorType =
                    FindType("Eplan.EplApi.Gui.GraphicalEditor");

                if (graphicalEditorType == null)
                {
                    return null;
                }

                object graphicalEditor =
                    Activator.CreateInstance(graphicalEditorType);

                object value =
                    TryGetPublicPropertyValue(graphicalEditor, "CurrentPage") ??
                    TryGetPublicPropertyValue(graphicalEditor, "ActivePage") ??
                    TryGetPublicPropertyValue(graphicalEditor, "Page") ??
                    TryInvokePublicMethod(graphicalEditor, "GetCurrentPage") ??
                    TryInvokePublicMethod(graphicalEditor, "GetActivePage");

                return value as Page;
            }
            catch
            {
                return null;
            }
        }

        private static IEnumerable<Page> GetSelectedPages()
        {
            SelectionSet selectionSet =
                new SelectionSet
                {
                    LockProjectByDefault = false,
                    LockSelectionByDefault = false
                };

            Page[] selectedPages =
                selectionSet.GetSelectedPages();

            if (selectedPages != null)
            {
                foreach (Page selectedPage in selectedPages)
                {
                    if (selectedPage != null && selectedPage.IsValid)
                    {
                        yield return selectedPage;
                    }
                }
            }

            object selection =
                TryGetPublicPropertyValue(
                    selectionSet,
                    "Selection");

            IEnumerable enumerable =
                selection as IEnumerable;

            if (enumerable == null)
            {
                yield break;
            }

            foreach (object entry in enumerable)
            {
                Page page =
                    GetPageFromSelectionEntry(entry);

                if (page != null && page.IsValid)
                {
                    yield return page;
                }
            }
        }

        private static Page GetPageFromSelectionEntry(
            object selectionEntry)
        {
            Page page =
                GetPage(selectionEntry);

            if (page != null)
            {
                return page;
            }

            object selectedObject =
                TryGetPublicPropertyValue(
                    selectionEntry,
                    "Object");

            page =
                GetPage(selectedObject);

            if (page != null)
            {
                return page;
            }

            object parentObject =
                TryGetPublicPropertyValue(
                    selectionEntry,
                    "ParentObject");

            return GetPage(parentObject);
        }

        private static Page GetPage(
            object source)
        {
            Page page =
                source as Page;

            if (page != null)
            {
                return page;
            }

            Placement placement =
                source as Placement;

            if (placement != null)
            {
                return placement.Page;
            }

            return null;
        }

        private static bool IsSamePage(
            Page first,
            Page second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            if (!first.IsValid || !second.IsValid)
            {
                return false;
            }

            return string.Equals(
                first.Name ?? string.Empty,
                second.Name ?? string.Empty,
                StringComparison.Ordinal);
        }

        private static Type FindType(
            string fullTypeName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type type =
                        assembly.GetType(fullTypeName);

                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private static object TryGetPublicPropertyValue(
            object source,
            string propertyName)
        {
            if (source == null)
            {
                return null;
            }

            try
            {
                PropertyInfo property =
                    source.GetType().GetProperty(propertyName);

                if (property == null)
                {
                    return null;
                }

                return property.GetValue(source, null);
            }
            catch
            {
                return null;
            }
        }

        private static object TryInvokePublicMethod(
            object source,
            string methodName)
        {
            if (source == null)
            {
                return null;
            }

            try
            {
                MethodInfo method =
                    source
                        .GetType()
                        .GetMethod(
                            methodName,
                            Type.EmptyTypes);

                if (method == null)
                {
                    return null;
                }

                return method.Invoke(source, null);
            }
            catch
            {
                return null;
            }
        }

        private static void RedrawGed()
        {
            try
            {
                new Edit().RedrawGed();
            }
            catch
            {
            }
        }

        private static void WriteSystemMessage(
            string text)
        {
            try
            {
                new BaseException(
                    text ?? string.Empty,
                    MessageLevel.Message).FixMessage();
            }
            catch
            {
            }
        }

        [DataContract]
        private sealed class UsabilityListState
        {
            [DataMember]
            public double OriginX { get; set; }

            [DataMember]
            public double OriginY { get; set; }

            [DataMember]
            public string Title { get; set; }

            [DataMember]
            public double TitleHeight { get; set; }

            [DataMember]
            public double HeaderHeight { get; set; }

            [DataMember]
            public double RowHeight { get; set; }

            [DataMember]
            public double TextHeight { get; set; }

            [DataMember]
            public double LineWidth { get; set; }

            [DataMember]
            public string NumberHeader { get; set; }

            [DataMember]
            public string DesignationHeader { get; set; }

            [DataMember]
            public string MountingPlaceHeader { get; set; }

            [DataMember]
            public string DescriptionHeader { get; set; }

            [DataMember]
            public double NumberColumnWidth { get; set; }

            [DataMember]
            public double DesignationColumnWidth { get; set; }

            [DataMember]
            public double MountingPlaceColumnWidth { get; set; }

            [DataMember]
            public double DescriptionColumnWidth { get; set; }
        }
    }
}