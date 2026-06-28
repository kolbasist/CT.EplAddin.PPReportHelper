using Eplan.EplApi.ApplicationFramework;
using Eplan.EplApi.Base;
using Action = Eplan.EplApi.ApplicationFramework.Action;

namespace CT.Epladdin.PPReportHelper
{
    internal sealed class UpdateUsabilityListsAction : IEplAction
    {
        public bool Execute(
            ActionCallingContext context)
        {
            Action baseAction =
                new ActionManager().FindBaseAction(this, true);

            bool baseResult = true;

            if (baseAction != null)
            {
                baseResult =
                    baseAction.Execute(context);
            }

            try
            {
                UsabilityList.RefreshPlacedListsOnReportsUpdated();
            }
            catch
            {
                WriteSystemMessage("Usability lists update failed.");
            }

            return baseResult;
        }

        public void GetActionProperties(
            ref ActionProperties actionProperties)
        {
        }

        public bool OnRegister(
            ref string name,
            ref int ordinal)
        {
            name = "XFgUpdateEvaluationAction";
            ordinal = 99;
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