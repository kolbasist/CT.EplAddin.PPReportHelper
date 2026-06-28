using Eplan.EplApi.ApplicationFramework;
using EplAction = Eplan.EplApi.ApplicationFramework.Action;

namespace CT.Epladdin.PPReportHelper
{
    internal sealed class UsabilityListPropertiesActionXGed : IEplAction
    {
        public bool Execute(
            ActionCallingContext context)
        {
            string specialGroupHandling = string.Empty;
            string commandLine = string.Empty;

            context.GetParameter(
                "specialGroupHandling",
                ref specialGroupHandling);

            context.GetParameter(
                "_cmdline",
                ref commandLine);

            bool isGedPropertiesCall =
                specialGroupHandling == "1" ||
                commandLine == "XGedEditPropertiesAction /specialGroupHandling:1";

            if (isGedPropertiesCall &&
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
            name = "XGedEditPropertiesAction";
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