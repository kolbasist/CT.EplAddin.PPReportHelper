using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Base;

namespace CT.Epladdin.PPReportHelper
{
    internal sealed class UsabilityListPropertiesCommand : IEplAction
    {
        public bool Execute(
            ActionCallingContext context)
        {
            if (!UsabilityListPropertiesService.TryOpenSelectedListProperties())
            {
                WriteSystemMessage("Выбранная таблица применимости не найдена.");
            }

            return true;
        }

        public void GetActionProperties(
            ref ActionProperties actionProperties)
        {
        }

        public bool OnRegister(
            ref string name,
            ref int ordinal)
        {
            name = "UsabilityListProperties";
            ordinal = 20;
            return true;
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
    }
}