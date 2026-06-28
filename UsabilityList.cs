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
        public enum UsabilityListSortingMode
        {
            MountingPlaceThenDesignation,
            DesignationThenMountingPlace,
            DescriptionThenMountingPlaceThenDesignation,
            DescriptionThenDesignationThenMountingPlace,
            None
        }

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
            set { Title = NormalizeTableText(value); }
        }

        [Category("Положение")]
        [ReadOnly(true)]
        [Description("Текущая координата X верхнего левого угла таблицы. Значение вычисляется по размещённому блоку и не редактируется вручную.")]
        public double ORIGIN_X
        {
            get { return OriginX; }
        }

        [Category("Положение")]
        [ReadOnly(true)]
        [Description("Текущая координата Y верхнего левого угла таблицы. Значение вычисляется по размещённому блоку и не редактируется вручную.")]
        public double ORIGIN_Y
        {
            get { return OriginY; }
        }

        [Category("Сортировка")]
        [Description("Порядок сортировки строк таблицы применимости.")]
        public UsabilityListSortingMode SortingBy { get; set; } =
            UsabilityListSortingMode.MountingPlaceThenDesignation;

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
            RestoreSortingByFromBlockName(block.Name);
            RestoreStateFromBlock(block);
            RefreshCurrentPropertiesFromBlockGraphics();
            RefreshOriginFromBlockLocation();
            InitializeDefaultColumns();
        }

        internal void SetData(
            DetachedStructureSegmentDictionary dictionary)
        {
            _dictionary =
                dictionary ?? new DetachedStructureSegmentDictionary();
        }

        internal bool HasData
        {
            get
            {
                return _dictionary != null &&
                       _dictionary.Count > 0;
            }
        }

        internal void RefreshOriginFromBlockGeometry()
        {
            TryUpdateOriginFromBlockGeometry(_block);
        }

        internal void RefreshCurrentPropertiesFromBlockGraphics()
        {
            TryUpdateVisualSettingsFromBlockGraphics(_block);
        }

        internal void RefreshOriginFromBlockLocation()
        {
            TryUpdateOriginFromBlockLocation(_block);
        }

        internal bool TryReloadDataFromActivePage()
        {
            try
            {
                StructureSegmentSeeker seeker =
                    new StructureSegmentSeeker();

                DetachedStructureSegmentDictionary dictionary =
                    seeker.GetDictionaryCopyAndDispose();

                if (dictionary == null || dictionary.Count == 0)
                {
                    WriteSystemMessage(
                        "UsabilityList data reload skipped: source dictionary is empty.");

                    return false;
                }

                SetData(dictionary);

                return true;
            }
            catch
            {
                WriteSystemMessage(
                    "UsabilityList data reload failed.");

                return false;
            }
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

            if (!HasData)
            {
                WriteSystemMessage(
                    "UsabilityList render aborted: dictionary is empty. Previous table was not removed.");

                return;
            }

            /*
             * Для уже размещённой таблицы координаты берём с Block.Location.
             * Это координата верхнего левого угла блока в EPLAN и она надёжнее,
             * чем попытка восстановить абсолютное положение по SubPlacements.
             */
            TryUpdateOriginFromBlockLocation(_block);

            NormalizeVisualSettings();
            InitializeDefaultColumns();

            IReadOnlyList<UsabilityListRow> rows =
                BuildRows(_dictionary);

            using (SafetyPoint safetyPoint = SafetyPoint.Create())
            {
                /*
                 * Важно:
                 * старый блок удаляем только после успешного создания нового.
                 * Иначе любая ошибка в CreateTablePlacements / Block.Create
                 * физически удаляет существующую таблицу.
                 */
                Block oldBlock =
                    _block;

                List<Placement> placements =
                    CreateTablePlacements(rows);

                if (placements == null || placements.Count == 0)
                {
                    WriteSystemMessage(
                        "UsabilityList render aborted: no placements were created. Previous table was not removed.");

                    return;
                }

                Block newBlock =
                    new Block();

                newBlock.Create(
                    _ownerPage,
                    placements.ToArray());

                newBlock.Name =
                    BuildBlockName();

                if (!newBlock.IsValid)
                {
                    WriteSystemMessage(
                        "UsabilityList render aborted: new block was not created. Previous table was not removed.");

                    return;
                }

                _block =
                    newBlock;

                RemoveBlockGraphics(oldBlock);

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

            if (updatedDictionary == null || updatedDictionary.Count == 0)
            {
                WriteSystemMessage(
                    "UsabilityList refresh skipped: updated dictionary is empty. Previous table was not removed.");

                return;
            }

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

        private string BuildBlockName()
        {
            return string.Concat(
                TableMarkerPrefix,
                "SORT=",
                SortingBy.ToString(),
                "#",
                Guid.NewGuid().ToString("N"));
        }

        private void RestoreSortingByFromBlockName(
            string blockName)
        {
            if (string.IsNullOrWhiteSpace(blockName))
            {
                return;
            }

            int sortIndex =
                blockName.IndexOf(
                    "SORT=",
                    StringComparison.Ordinal);

            if (sortIndex < 0)
            {
                return;
            }

            string value =
                blockName.Substring(sortIndex + "SORT=".Length);

            int separatorIndex =
                value.IndexOf('#');

            if (separatorIndex >= 0)
            {
                value =
                    value.Substring(0, separatorIndex);
            }

            RestoreSortingByFromState(value);
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
            List<UsabilityListRowSource> rowSources =
                CreateRowSources(dictionary);

            if (rowSources.Count == 0)
            {
                return new List<UsabilityListRow>
                {
                    UsabilityListRow.CreatePlaceholder(
                        "Нет данных для отображения")
                };
            }

            List<UsabilityListRow> rows =
                new List<UsabilityListRow>();

            int rowNumber = 1;

            foreach (UsabilityListRowSource rowSource in SortRowSources(rowSources))
            {
                rows.Add(
                    new UsabilityListRow(
                        rowNumber,
                        rowSource.Segment.Designation44004,
                        rowSource.Segment.MountingPlace1220,
                        rowSource.Description));

                rowNumber++;
            }

            return rows;
        }

        private static List<UsabilityListRowSource> CreateRowSources(
            DetachedStructureSegmentDictionary dictionary)
        {
            List<UsabilityListRowSource> result =
                new List<UsabilityListRowSource>();

            if (dictionary == null || dictionary.Count == 0)
            {
                return result;
            }

            foreach (KeyValuePair<string, List<DetachedStructureSegment>> pair in dictionary)
            {
                string description =
                    DetachedStructureSegmentDictionary.NormalizeDescription(pair.Key);

                List<DetachedStructureSegment> segments =
                    pair.Value ?? new List<DetachedStructureSegment>();

                foreach (DetachedStructureSegment segment in segments)
                {
                    if (segment == null)
                    {
                        continue;
                    }

                    result.Add(
                        new UsabilityListRowSource(
                            description,
                            segment));
                }
            }

            return result;
        }

        private IEnumerable<UsabilityListRowSource> SortRowSources(
            IEnumerable<UsabilityListRowSource> rowSources)
        {
            if (rowSources == null)
            {
                return new List<UsabilityListRowSource>();
            }

            switch (SortingBy)
            {
                case UsabilityListSortingMode.DesignationThenMountingPlace:
                    return rowSources
                        .OrderBy(item => item.Segment.Designation44004 ?? string.Empty)
                        .ThenBy(item => item.Segment.MountingPlace1220 ?? string.Empty)
                        .ThenBy(item => item.Description ?? string.Empty);

                case UsabilityListSortingMode.DescriptionThenMountingPlaceThenDesignation:
                    return rowSources
                        .OrderBy(item => item.Description ?? string.Empty)
                        .ThenBy(item => item.Segment.MountingPlace1220 ?? string.Empty)
                        .ThenBy(item => item.Segment.Designation44004 ?? string.Empty);

                case UsabilityListSortingMode.DescriptionThenDesignationThenMountingPlace:
                    return rowSources
                        .OrderBy(item => item.Description ?? string.Empty)
                        .ThenBy(item => item.Segment.Designation44004 ?? string.Empty)
                        .ThenBy(item => item.Segment.MountingPlace1220 ?? string.Empty);

                case UsabilityListSortingMode.None:
                    return rowSources;

                case UsabilityListSortingMode.MountingPlaceThenDesignation:
                default:
                    return rowSources
                        .OrderBy(item => item.Segment.MountingPlace1220 ?? string.Empty)
                        .ThenBy(item => item.Segment.Designation44004 ?? string.Empty)
                        .ThenBy(item => item.Description ?? string.Empty);
            }
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

            string displayText =
                NormalizeTableText(text);

            graphicText.Create(
                _ownerPage,
                displayText,
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
                1.0);

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

                    Title = NormalizeTableText(Title),

                    TitleHeight = TitleHeight,
                    HeaderHeight = HeaderHeight,
                    RowHeight = RowHeight,
                    TextHeight = TextHeight,
                    LineWidth = LineWidth,

                    NumberHeader = NormalizeTableText(NUMBER_HEADER),
                    DesignationHeader = NormalizeTableText(DESIGNATION_HEADER),
                    MountingPlaceHeader = NormalizeTableText(MOUNTING_PLACE_HEADER),
                    DescriptionHeader = NormalizeTableText(DESCRIPTION_HEADER),

                    NumberColumnWidth = NUMBER_COLUMN_WIDTH,
                    DesignationColumnWidth = DESIGNATION_COLUMN_WIDTH,
                    MountingPlaceColumnWidth = MOUNTING_PLACE_COLUMN_WIDTH,
                    DescriptionColumnWidth = DESCRIPTION_COLUMN_WIDTH,

                    SortingBy = this.SortingBy.ToString(),

                    /*
                     * Данные таблицы не пишем в скрытый Text.
                     * В реальных проектах JSON с DataGroups может стать слишком длинным,
                     * EPLAN начинает его резать / портить, и последующее чтение состояния
                     * падает целиком. Данные при открытии свойств пересобираются seeker-ом
                     * с активной страницы, а здесь храним только компактные визуальные свойства.
                     */
                    DataGroups = null
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

                    Title =
                        NormalizeTableTextOrDefault(
                            state.Title,
                            Title);

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
                        NormalizeTableTextOrDefault(
                            state.NumberHeader,
                            NUMBER_HEADER);

                    DESIGNATION_HEADER =
                        NormalizeTableTextOrDefault(
                            state.DesignationHeader,
                            DESIGNATION_HEADER);

                    MOUNTING_PLACE_HEADER =
                        NormalizeTableTextOrDefault(
                            state.MountingPlaceHeader,
                            MOUNTING_PLACE_HEADER);

                    DESCRIPTION_HEADER =
                        NormalizeTableTextOrDefault(
                            state.DescriptionHeader,
                            DESCRIPTION_HEADER);

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

                    RestoreSortingByFromState(state.SortingBy);
                    RestoreDictionaryFromState(state.DataGroups);
                }
            }
            catch
            {
                WriteSystemMessage(
                    "UsabilityList state deserialize error.");
            }
        }


        private void RestoreSortingByFromState(
            string sortingBy)
        {
            if (string.IsNullOrWhiteSpace(sortingBy))
            {
                return;
            }

            try
            {
                SortingBy =
                    (UsabilityListSortingMode)Enum.Parse(
                        typeof(UsabilityListSortingMode),
                        sortingBy,
                        true);
            }
            catch
            {
                SortingBy =
                    UsabilityListSortingMode.MountingPlaceThenDesignation;
            }
        }

        private List<UsabilityListStateDataGroup> CreateStateDataGroups()
        {
            List<UsabilityListStateDataGroup> result =
                new List<UsabilityListStateDataGroup>();

            if (_dictionary == null || _dictionary.Count == 0)
            {
                return result;
            }

            foreach (KeyValuePair<string, List<DetachedStructureSegment>> pair
                     in _dictionary.OrderBy(item => item.Key ?? string.Empty))
            {
                string description =
                    DetachedStructureSegmentDictionary.NormalizeDescription(
                        pair.Key);

                if (string.IsNullOrWhiteSpace(description))
                {
                    continue;
                }

                UsabilityListStateDataGroup group =
                    new UsabilityListStateDataGroup
                    {
                        Description = description,
                        Segments = new List<UsabilityListStateDataSegment>()
                    };

                List<DetachedStructureSegment> segments =
                    pair.Value ?? new List<DetachedStructureSegment>();

                foreach (DetachedStructureSegment segment in segments)
                {
                    if (segment == null)
                    {
                        continue;
                    }

                    group.Segments.Add(
                        new UsabilityListStateDataSegment
                        {
                            Description44005 = segment.Description44005,
                            Designation44004 = segment.Designation44004,
                            MountingPlace1220 = segment.MountingPlace1220
                        });
                }

                if (group.Segments.Count > 0)
                {
                    result.Add(group);
                }
            }

            return result;
        }

        private void RestoreDictionaryFromState(
            List<UsabilityListStateDataGroup> groups)
        {
            if (groups == null || groups.Count == 0)
            {
                return;
            }

            DetachedStructureSegmentDictionary dictionary =
                new DetachedStructureSegmentDictionary();

            foreach (UsabilityListStateDataGroup group in groups)
            {
                if (group == null)
                {
                    continue;
                }

                string key =
                    DetachedStructureSegmentDictionary.NormalizeDescription(
                        group.Description);

                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                List<DetachedStructureSegment> segments =
                    new List<DetachedStructureSegment>();

                List<UsabilityListStateDataSegment> stateSegments =
                    group.Segments ?? new List<UsabilityListStateDataSegment>();

                foreach (UsabilityListStateDataSegment stateSegment in stateSegments)
                {
                    if (stateSegment == null)
                    {
                        continue;
                    }

                    string description =
                        DetachedStructureSegmentDictionary.NormalizeDescription(
                            stateSegment.Description44005);

                    if (string.IsNullOrWhiteSpace(description))
                    {
                        description = key;
                    }

                    segments.Add(
                        new DetachedStructureSegment(
                            description,
                            stateSegment.Designation44004,
                            stateSegment.MountingPlace1220));
                }

                if (segments.Count > 0)
                {
                    dictionary[key] = segments;
                }
            }

            if (dictionary.Count > 0)
            {
                _dictionary = dictionary;
            }
        }

        private void NormalizeVisualSettings()
        {
            HeaderHeight =
                NormalizePositive(HeaderHeight, 10.0);

            RowHeight =
                NormalizePositive(RowHeight, 5.0);

            TextHeight =
                NormalizePositive(TextHeight, 2.5);

            LineWidth =
                NormalizePositive(LineWidth, 0.25);

            NUMBER_COLUMN_WIDTH =
                NormalizePositive(NUMBER_COLUMN_WIDTH, 10.0);

            DESIGNATION_COLUMN_WIDTH =
                NormalizePositive(DESIGNATION_COLUMN_WIDTH, 35.0);

            MOUNTING_PLACE_COLUMN_WIDTH =
                NormalizePositive(MOUNTING_PLACE_COLUMN_WIDTH, 25.0);

            DESCRIPTION_COLUMN_WIDTH =
                NormalizePositive(DESCRIPTION_COLUMN_WIDTH, 65.0);
        }

        private static double NormalizePositive(
            double value,
            double defaultValue)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value) ||
                value <= 0.0)
            {
                return defaultValue;
            }

            return value;
        }

        private string NormalizeTableText(
            string value)
        {
            Project project =
                GetOwnerProject();

            return MultiLangStringTextExtractor.GetProjectLanguageTextFromSerializedString(
                value,
                project);
        }

        private string NormalizeTableTextOrDefault(
            string value,
            string defaultValue)
        {
            string normalizedValue =
                NormalizeTableText(value);

            return string.IsNullOrWhiteSpace(normalizedValue)
                ? defaultValue ?? string.Empty
                : normalizedValue;
        }

        private Project GetOwnerProject()
        {
            try
            {
                if (_ownerPage == null || !_ownerPage.IsValid)
                {
                    return null;
                }

                return _ownerPage.Project;
            }
            catch
            {
                return null;
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
                    GetTextContents(text);

                if (string.IsNullOrWhiteSpace(contents))
                {
                    continue;
                }

                int statePrefixIndex =
                    contents.IndexOf(
                        StateTextPrefix,
                        StringComparison.Ordinal);

                if (statePrefixIndex < 0)
                {
                    continue;
                }

                RestoreStateFromString(
                    contents.Substring(statePrefixIndex));

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

            int statePrefixIndex =
                state.IndexOf(
                    StateTextPrefix,
                    StringComparison.Ordinal);

            if (statePrefixIndex < 0)
            {
                return;
            }

            string raw =
                state.Substring(statePrefixIndex + StateTextPrefix.Length);

            if (string.IsNullOrWhiteSpace(raw))
            {
                return;
            }

            DeserializeState(raw);
        }

        private bool TryUpdateOriginFromBlockGeometry(
            Block block)
        {
            if (TryUpdateOriginFromBlockLocation(block))
            {
                return true;
            }

            return TryUpdateVisualSettingsFromBlockGraphics(block);
        }

        private bool TryUpdateOriginFromBlockLocation(
            Block block)
        {
            if (block == null || !block.IsValid)
            {
                return false;
            }

            try
            {
                PointD location =
                    block.Location;

                OriginX = location.X;
                OriginY = location.Y;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryUpdateVisualSettingsFromBlockGraphics(
            Block block)
        {
            if (block == null || !block.IsValid)
            {
                return false;
            }

            Placement[] subPlacements =
                block.SubPlacements;

            if (subPlacements == null || subPlacements.Length == 0)
            {
                return false;
            }

            List<double> verticalGridX =
                new List<double>();

            List<double> horizontalGridY =
                new List<double>();

            List<Text> visibleTexts =
                new List<Text>();

            foreach (Placement placement in subPlacements)
            {
                if (placement == null || !placement.IsValid)
                {
                    continue;
                }

                Line line =
                    placement as Line;

                if (line != null)
                {
                    CollectGridLineCoordinates(
                        line,
                        verticalGridX,
                        horizontalGridY);

                    TryRestoreLineWidthFromLine(line);
                    continue;
                }

                Text text =
                    placement as Text;

                if (text != null && !IsStateText(text))
                {
                    visibleTexts.Add(text);
                    TryRestoreTextHeightFromText(text);
                }
            }

            verticalGridX =
                NormalizeDistinctSortedValues(
                    verticalGridX,
                    ascending: true);

            horizontalGridY =
                NormalizeDistinctSortedValues(
                    horizontalGridY,
                    ascending: false);

            if (verticalGridX.Count < 2 ||
                horizontalGridY.Count < 2)
            {
                return false;
            }

            bool originRestoredFromBlockLocation =
                TryUpdateOriginFromBlockLocation(block);

            if (!originRestoredFromBlockLocation)
            {
                OriginX =
                    verticalGridX[0];

                OriginY =
                    horizontalGridY[0];
            }

            RestoreColumnWidthsFromGrid(verticalGridX);
            RestoreRowHeightsFromGrid(horizontalGridY);
            RestoreHeadersFromTextGeometry(
                visibleTexts,
                verticalGridX,
                horizontalGridY);

            return true;
        }

        private void CollectGridLineCoordinates(
            Line line,
            List<double> verticalGridX,
            List<double> horizontalGridY)
        {
            const double tolerance = 0.001;

            PointD startPoint =
                line.StartPoint;

            PointD endPoint =
                line.EndPoint;

            bool isVertical =
                Math.Abs(startPoint.X - endPoint.X) <= tolerance &&
                Math.Abs(startPoint.Y - endPoint.Y) > tolerance;

            if (isVertical)
            {
                verticalGridX.Add(startPoint.X);
                return;
            }

            bool isHorizontal =
                Math.Abs(startPoint.Y - endPoint.Y) <= tolerance &&
                Math.Abs(startPoint.X - endPoint.X) > tolerance;

            if (isHorizontal)
            {
                horizontalGridY.Add(startPoint.Y);
            }
        }

        private static List<double> NormalizeDistinctSortedValues(
            IEnumerable<double> values,
            bool ascending)
        {
            const double tolerance = 0.001;

            List<double> sortedValues =
                values
                    .OrderBy(value => value)
                    .ToList();

            List<double> result =
                new List<double>();

            foreach (double value in sortedValues)
            {
                if (result.Count == 0 ||
                    Math.Abs(result[result.Count - 1] - value) > tolerance)
                {
                    result.Add(value);
                }
            }

            if (!ascending)
            {
                result.Reverse();
            }

            return result;
        }

        private void RestoreColumnWidthsFromGrid(
            List<double> verticalGridX)
        {
            if (verticalGridX == null || verticalGridX.Count < 5)
            {
                return;
            }

            NUMBER_COLUMN_WIDTH =
                verticalGridX[1] - verticalGridX[0];

            DESIGNATION_COLUMN_WIDTH =
                verticalGridX[2] - verticalGridX[1];

            MOUNTING_PLACE_COLUMN_WIDTH =
                verticalGridX[3] - verticalGridX[2];

            DESCRIPTION_COLUMN_WIDTH =
                verticalGridX[4] - verticalGridX[3];
        }

        private void RestoreRowHeightsFromGrid(
            List<double> horizontalGridY)
        {
            if (horizontalGridY == null || horizontalGridY.Count < 2)
            {
                return;
            }

            double headerHeight =
                horizontalGridY[0] - horizontalGridY[1];

            if (headerHeight > 0.0)
            {
                HeaderHeight =
                    headerHeight;
            }

            if (horizontalGridY.Count < 3)
            {
                return;
            }

            List<double> rowHeights =
                new List<double>();

            for (int index = 1; index < horizontalGridY.Count - 1; index++)
            {
                double rowHeight =
                    horizontalGridY[index] - horizontalGridY[index + 1];

                if (rowHeight > 0.0)
                {
                    rowHeights.Add(rowHeight);
                }
            }

            if (rowHeights.Count > 0)
            {
                RowHeight =
                    rowHeights.Average();
            }
        }

        private void RestoreHeadersFromTextGeometry(
            List<Text> texts,
            List<double> verticalGridX,
            List<double> horizontalGridY)
        {
            if (texts == null ||
                verticalGridX == null || verticalGridX.Count < 5 ||
                horizontalGridY == null || horizontalGridY.Count < 2)
            {
                return;
            }

            double top =
                horizontalGridY[0];

            double headerBottom =
                horizontalGridY[1];

            List<Text> headerTexts =
                texts
                    .Where(text => text != null && text.IsValid)
                    .Where(text =>
                        text.Location.Y <= top + 0.001 &&
                        text.Location.Y >= headerBottom - 0.001)
                    .OrderBy(text => text.Location.X)
                    .ToList();

            for (int columnIndex = 0; columnIndex < 4; columnIndex++)
            {
                double left =
                    verticalGridX[columnIndex];

                double right =
                    verticalGridX[columnIndex + 1];

                Text headerText =
                    headerTexts
                        .FirstOrDefault(text =>
                            text.Location.X >= left - 0.001 &&
                            text.Location.X <= right + 0.001);

                if (headerText == null)
                {
                    continue;
                }

                string header =
                    GetTextContents(headerText);

                if (string.IsNullOrWhiteSpace(header))
                {
                    continue;
                }

                SetHeaderByColumnIndex(
                    columnIndex,
                    header);
            }
        }

        private void SetHeaderByColumnIndex(
            int columnIndex,
            string header)
        {
            string normalizedHeader =
                NormalizeTableText(header);

            if (string.IsNullOrWhiteSpace(normalizedHeader))
            {
                return;
            }

            switch (columnIndex)
            {
                case 0:
                    NUMBER_HEADER = normalizedHeader;
                    break;

                case 1:
                    DESIGNATION_HEADER = normalizedHeader;
                    break;

                case 2:
                    MOUNTING_PLACE_HEADER = normalizedHeader;
                    break;

                case 3:
                    DESCRIPTION_HEADER = normalizedHeader;
                    break;
            }
        }

        private void TryRestoreLineWidthFromLine(
            Line line)
        {
            if (line == null || !line.IsValid)
            {
                return;
            }

            try
            {
                if (line.Pen.Width > 0.0)
                {
                    LineWidth =
                        line.Pen.Width;
                }
            }
            catch
            {
            }
        }

        private void TryRestoreTextHeightFromText(
            Text text)
        {
            double value;

            if (TryGetPublicPropertyAsDouble(text, "Height", out value) ||
                TryGetPublicPropertyAsDouble(text, "TextHeight", out value) ||
                TryGetPublicPropertyAsDouble(text, "Size", out value))
            {
                if (value > 0.0)
                {
                    TextHeight =
                        value;
                }
            }
        }

        private static bool TryGetPublicPropertyAsDouble(
            object source,
            string propertyName,
            out double value)
        {
            value = 0.0;

            if (source == null)
            {
                return false;
            }

            try
            {
                PropertyInfo property =
                    source.GetType().GetProperty(propertyName);

                if (property == null)
                {
                    return false;
                }

                object rawValue =
                    property.GetValue(source, null);

                if (rawValue == null)
                {
                    return false;
                }

                if (rawValue is double)
                {
                    value =
                        (double)rawValue;

                    return true;
                }

                string text =
                    Convert.ToString(rawValue, CultureInfo.InvariantCulture);

                return double.TryParse(
                    text,
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out value);
            }
            catch
            {
                return false;
            }
        }

        private static void AddPointToBounds(
            PointD point,
            ref bool hasPoint,
            ref double minX,
            ref double maxY)
        {
            if (!hasPoint)
            {
                minX = point.X;
                maxY = point.Y;
                hasPoint = true;
                return;
            }

            if (point.X < minX)
            {
                minX = point.X;
            }

            if (point.Y > maxY)
            {
                maxY = point.Y;
            }
        }

        private static bool IsStateText(
            Text text)
        {
            string contents =
                GetTextContents(text);

            return !string.IsNullOrWhiteSpace(contents) &&
                   contents.IndexOf(
                       StateTextPrefix,
                       StringComparison.Ordinal) >= 0;
        }

        private static string GetTextContents(
            Text text)
        {
            if (text == null || !text.IsValid)
            {
                return string.Empty;
            }

            Project project =
                TryGetTextProject(text);

            try
            {
                return MultiLangStringTextExtractor.GetProjectLanguageText(
                    text.Contents,
                    project);
            }
            catch
            {
            }

            try
            {
                return MultiLangStringTextExtractor.GetProjectLanguageTextFromSerializedString(
                    text.Contents.GetAsString(),
                    project);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static Project TryGetTextProject(
            Text text)
        {
            if (text == null || !text.IsValid)
            {
                return null;
            }

            try
            {
                Page page =
                    text.Page;

                if (page == null || !page.IsValid)
                {
                    return null;
                }

                return page.Project;
            }
            catch
            {
                return null;
            }
        }

        private void RemovePreviousGraphics()
        {
            Block block =
                _block;

            _block =
                null;

            RemoveBlockGraphics(block);
        }

        private static void RemoveBlockGraphics(
            Block block)
        {
            if (block == null || !block.IsValid)
            {
                return;
            }

            Placement[] oldPlacements;

            try
            {
                oldPlacements =
                    block.BreakUp();
            }
            catch
            {
                return;
            }

            if (oldPlacements == null)
            {
                return;
            }

            foreach (Placement placement in oldPlacements)
            {
                SafeRemovePlacement(placement);
            }
        }

        private static void SafeRemovePlacement(
            Placement placement)
        {
            if (placement == null || !placement.IsValid)
            {
                return;
            }

            try
            {
                placement.Remove();
            }
            catch
            {
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

        private sealed class UsabilityListRowSource
        {
            internal string Description { get; private set; }
            internal DetachedStructureSegment Segment { get; private set; }

            internal UsabilityListRowSource(
                string description,
                DetachedStructureSegment segment)
            {
                Description = description ?? string.Empty;
                Segment = segment;
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

            [DataMember]
            public string SortingBy { get; set; }

            [DataMember]
            public List<UsabilityListStateDataGroup> DataGroups { get; set; }
        }

        [DataContract]
        private sealed class UsabilityListStateDataGroup
        {
            [DataMember]
            public string Description { get; set; }

            [DataMember]
            public List<UsabilityListStateDataSegment> Segments { get; set; }
        }

        [DataContract]
        private sealed class UsabilityListStateDataSegment
        {
            [DataMember]
            public string Description44005 { get; set; }

            [DataMember]
            public string Designation44004 { get; set; }

            [DataMember]
            public string MountingPlace1220 { get; set; }
        }
    }
}
