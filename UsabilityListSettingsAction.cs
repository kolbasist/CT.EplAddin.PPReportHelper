using Eplan.EplApi.ApplicationFramework;

namespace CT.Epladdin.PPReportHelper
{
    internal sealed class UsabilityListSettingsAction : IEplAction
    {
        public bool Execute(
            ActionCallingContext context)
        {
            UsabilityListSettingsDialog.ShowModal();
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
            name = "UsabilityListSettings";
            ordinal = 20;
            return true;
        }
    }
}