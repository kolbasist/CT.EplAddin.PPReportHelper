using Eplan.EplApi.ApplicationFramework;
using EplAction = Eplan.EplApi.ApplicationFramework.Action;

namespace CT.Epladdin.PPReportHelper
{
    internal sealed class UsabilityListPropertiesAction : IEplAction
    {
        public bool Execute(
            ActionCallingContext context)
        {
            string function = string.Empty;

            context.GetParameter(
                "function",
                ref function);

            if (function == "PropertiesRedundant" &&
                UsabilityListPropertiesService.TryOpenSelectedListProperties())
            {
                return true;
            }

            return ExecuteBaseAction(context);
        }

        public void GetActionProperties(
            ref ActionProperties actionProperties)
        {
        }

        public bool OnRegister(
            ref string name,
            ref int ordinal)
        {
            name = "GfDlgMgrActionIGfWind";
            ordinal = 99;
            return true;
        }

        private bool ExecuteBaseAction(
            ActionCallingContext context)
        {
            EplAction baseAction =
                new ActionManager().FindBaseAction(this, true);

            if (baseAction == null)
            {
                return false;
            }

            return baseAction.Execute(context);
        }
    }
}