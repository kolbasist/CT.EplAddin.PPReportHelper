using System.Diagnostics;
using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Base;
using Eplan.EplApi.Gui;
using TraceListener = Eplan.EplApi.Base.TraceListener;

namespace CT.Epladdin.PPReportHelper
{
    public sealed class AddinModule : IEplAddIn
    {
        private TraceListener _traceListener;

        public bool OnInit()
        {
            _traceListener = new TraceListener();
            Trace.Listeners.Add(_traceListener);
            return true;
        }

        public bool OnInitGui()
        {
            RibbonBar ribbonBar =
                new RibbonBar();

            MultiLangString tabName =
                new MultiLangString();

            tabName.AddString(
                ISOCode.Language.L_en_US,
                "Insert");

            tabName.AddString(
                ISOCode.Language.L_ru_RU,
                "Вставить");

            RibbonTab tab =
                ribbonBar.GetTab(
                    tabName,
                    true);

            RibbonCommandGroup commandGroup =
                tab.AddCommandGroup("Таблицы");

            commandGroup.AddCommand(
                "Таблица применимости",
                "XGedStartInteractionAction /Name:" +
                UsabilityList_InsertInteraction.InteractionName);

            return true;
        }

        public bool OnExit()
        {
            if (_traceListener != null)
            {
                Trace.Listeners.Remove(_traceListener);
                _traceListener = null;
            }

            return true;
        }

        public bool OnRegister(
            ref bool loadOnStart)
        {
            loadOnStart = true;
            return true;
        }

        public bool OnUnregister()
        {
            return true;
        }
    }
}