using System;
using System.ComponentModel;
using Eplan.EplApi.Base;

namespace CT.Epladdin.PPReportHelper
{
    [RefreshProperties(RefreshProperties.All)]
    internal sealed class UsabilityListSettings
    {
        private static readonly Lazy<UsabilityListSettings> _lazy =
            new Lazy<UsabilityListSettings>(() => new UsabilityListSettings());

        private const string KeyPrefix = "USER.CT.USABILITYLIST.";

        private const string KeyTitle = KeyPrefix + "TITLE";
        private const string KeyNumberHeader = KeyPrefix + "NUMBER_HEADER";
        private const string KeyDesignationHeader = KeyPrefix + "DESIGNATION_HEADER";
        private const string KeyMountingPlaceHeader = KeyPrefix + "MOUNTING_PLACE_HEADER";
        private const string KeyDescriptionHeader = KeyPrefix + "DESCRIPTION_HEADER";

        private const string KeyTitleHeight = KeyPrefix + "TITLE_HEIGHT";
        private const string KeyHeaderHeight = KeyPrefix + "HEADER_HEIGHT";
        private const string KeyRowHeight = KeyPrefix + "ROW_HEIGHT";
        private const string KeyTextHeight = KeyPrefix + "TEXT_HEIGHT";
        private const string KeyLineWidth = KeyPrefix + "LINE_WIDTH";

        private const string KeyNumberWidth = KeyPrefix + "NUMBER_WIDTH";
        private const string KeyDesignationWidth = KeyPrefix + "DESIGNATION_WIDTH";
        private const string KeyMountingPlaceWidth = KeyPrefix + "MOUNTING_PLACE_WIDTH";
        private const string KeyDescriptionWidth = KeyPrefix + "DESCRIPTION_WIDTH";

        internal static UsabilityListSettings Instance
        {
            get { return _lazy.Value; }
        }

        [Category("Таблица")]
        [Description("Заголовок таблицы")]
        public string TITLE { get; set; } = "Перечень функциональных зон";

        [Category("Заголовки столбцов")]
        public string NUMBER_HEADER { get; set; } = "№\nп/п";

        [Category("Заголовки столбцов")]
        public string DESIGNATION_HEADER { get; set; } = "Обозначение\nвентустановки";

        [Category("Заголовки столбцов")]
        public string MOUNTING_PLACE_HEADER { get; set; } = "Обозначение\nщита АСУ";

        [Category("Заголовки столбцов")]
        public string DESCRIPTION_HEADER { get; set; } = "Примечание";

        [Category("Размеры")]
        public double TITLE_HEIGHT { get; set; } = 6.0;

        [Category("Размеры")]
        public double HEADER_HEIGHT { get; set; } = 10.0;

        [Category("Размеры")]
        public double ROW_HEIGHT { get; set; } = 5.0;

        [Category("Размеры")]
        public double TEXT_HEIGHT { get; set; } = 2.5;

        [Category("Размеры")]
        public double LINE_WIDTH { get; set; } = 0.25;

        [Category("Ширины столбцов")]
        public double NUMBER_WIDTH { get; set; } = 10.0;

        [Category("Ширины столбцов")]
        public double DESIGNATION_WIDTH { get; set; } = 35.0;

        [Category("Ширины столбцов")]
        public double MOUNTING_PLACE_WIDTH { get; set; } = 25.0;

        [Category("Ширины столбцов")]
        public double DESCRIPTION_WIDTH { get; set; } = 65.0;

        private UsabilityListSettings()
        {
        }

        internal void LoadSettings()
        {
            Settings settings =
                new Settings();

            TITLE =
                GetString(
                    settings,
                    KeyTitle,
                    TITLE);

            NUMBER_HEADER =
                GetString(
                    settings,
                    KeyNumberHeader,
                    NUMBER_HEADER);

            DESIGNATION_HEADER =
                GetString(
                    settings,
                    KeyDesignationHeader,
                    DESIGNATION_HEADER);

            MOUNTING_PLACE_HEADER =
                GetString(
                    settings,
                    KeyMountingPlaceHeader,
                    MOUNTING_PLACE_HEADER);

            DESCRIPTION_HEADER =
                GetString(
                    settings,
                    KeyDescriptionHeader,
                    DESCRIPTION_HEADER);

            TITLE_HEIGHT =
                GetDouble(
                    settings,
                    KeyTitleHeight,
                    TITLE_HEIGHT);

            HEADER_HEIGHT =
                GetDouble(
                    settings,
                    KeyHeaderHeight,
                    HEADER_HEIGHT);

            ROW_HEIGHT =
                GetDouble(
                    settings,
                    KeyRowHeight,
                    ROW_HEIGHT);

            TEXT_HEIGHT =
                GetDouble(
                    settings,
                    KeyTextHeight,
                    TEXT_HEIGHT);

            LINE_WIDTH =
                GetDouble(
                    settings,
                    KeyLineWidth,
                    LINE_WIDTH);

            NUMBER_WIDTH =
                GetDouble(
                    settings,
                    KeyNumberWidth,
                    NUMBER_WIDTH);

            DESIGNATION_WIDTH =
                GetDouble(
                    settings,
                    KeyDesignationWidth,
                    DESIGNATION_WIDTH);

            MOUNTING_PLACE_WIDTH =
                GetDouble(
                    settings,
                    KeyMountingPlaceWidth,
                    MOUNTING_PLACE_WIDTH);

            DESCRIPTION_WIDTH =
                GetDouble(
                    settings,
                    KeyDescriptionWidth,
                    DESCRIPTION_WIDTH);
        }

        internal void SaveSettings()
        {
            Settings settings =
                new Settings();

            SetString(
                settings,
                KeyTitle,
                TITLE);

            SetString(
                settings,
                KeyNumberHeader,
                NUMBER_HEADER);

            SetString(
                settings,
                KeyDesignationHeader,
                DESIGNATION_HEADER);

            SetString(
                settings,
                KeyMountingPlaceHeader,
                MOUNTING_PLACE_HEADER);

            SetString(
                settings,
                KeyDescriptionHeader,
                DESCRIPTION_HEADER);

            SetDouble(
                settings,
                KeyTitleHeight,
                TITLE_HEIGHT);

            SetDouble(
                settings,
                KeyHeaderHeight,
                HEADER_HEIGHT);

            SetDouble(
                settings,
                KeyRowHeight,
                ROW_HEIGHT);

            SetDouble(
                settings,
                KeyTextHeight,
                TEXT_HEIGHT);

            SetDouble(
                settings,
                KeyLineWidth,
                LINE_WIDTH);

            SetDouble(
                settings,
                KeyNumberWidth,
                NUMBER_WIDTH);

            SetDouble(
                settings,
                KeyDesignationWidth,
                DESIGNATION_WIDTH);

            SetDouble(
                settings,
                KeyMountingPlaceWidth,
                MOUNTING_PLACE_WIDTH);

            SetDouble(
                settings,
                KeyDescriptionWidth,
                DESCRIPTION_WIDTH);
        }

        private static string GetString(
            Settings settings,
            string key,
            string defaultValue)
        {
            try
            {
                if (settings.ExistSetting(key))
                {
                    return settings.GetStringSetting(key, 0);
                }
            }
            catch
            {
            }

            return defaultValue ?? string.Empty;
        }

        private static double GetDouble(
            Settings settings,
            string key,
            double defaultValue)
        {
            try
            {
                if (settings.ExistSetting(key))
                {
                    return settings.GetDoubleSetting(key, 0);
                }
            }
            catch
            {
            }

            return defaultValue;
        }

        private static void SetString(
            Settings settings,
            string key,
            string value)
        {
            value =
                value ?? string.Empty;

            if (!settings.ExistSetting(key))
            {
                settings.AddStringSetting(
                    key,
                    new string[] { string.Empty },
                    new string[] { value },
                    (ISettings.CreationFlag)0);
            }

            settings.SetStringSetting(
                key,
                value,
                0);
        }

        private static void SetDouble(
            Settings settings,
            string key,
            double value)
        {
            if (!settings.ExistSetting(key))
            {
                settings.AddDoubleSetting(
                    key,
                    new double[] { value },
                    new Eplan.EplApi.Base.Range[]
                    {
                        new Eplan.EplApi.Base.Range
                        {
                            FromValue = 0.0,
                            ToValue = 32768.0
                        }
                    },
                    (ISettings.CreationFlag)0);
            }

            settings.SetDoubleSetting(
                key,
                value,
                0);
        }
    }
}