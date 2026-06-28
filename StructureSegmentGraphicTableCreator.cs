using System.Collections.Generic;
using Eplan.EplApi.Base;

namespace CT.Epladdin.PPReportHelper
{
    internal sealed class StructureSegmentGraphicTableCreator
    {
        internal void Create(DetachedStructureSegmentDictionary dictionary)
        {
            if (dictionary == null || dictionary.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<string, List<DetachedStructureSegment>> pair
                     in dictionary)
            {
                string description =
                    DetachedStructureSegmentDictionary.NormalizeDescription(pair.Key);

                List<DetachedStructureSegment> segments =
                    pair.Value;

                if (segments == null || segments.Count == 0)
                {
                    continue;
                }

                /*
                 * Здесь будет создание графической таблицы.
                 *
                 * Важно:
                 * - dictionary уже отсоединён от EPLAN-объектов;
                 * - внутри нет PpStructureSegment;
                 * - можно безопасно хранить строки Designation / DescriptionText;
                 * - нельзя рассчитывать на прямую связь с объектами проекта.
                 */
            }
        }
    }
}