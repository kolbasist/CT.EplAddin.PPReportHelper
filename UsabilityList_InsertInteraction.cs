using Eplan.EplApi.Base;
using Eplan.EplApi.DataModel;
using Eplan.EplApi.EServices.Ged;
using Eplan.EplApi.HEServices;

namespace CT.Epladdin.PPReportHelper
{
    [Interaction(
        Name = InteractionName,
        Ordinal = 50,
        Prio = 20)]
    public sealed class UsabilityList_InsertInteraction : Interaction
    {
        public const string InteractionName = "CTUsabilityListInsert";

        private const int RequestAbort = 1;
        private const int RequestPoint = 16;
        private const int RequestSuccess = 1024;

        private DetachedStructureSegmentDictionary _dictionary;
        private PointD _insertionPoint = new PointD(0.0, 0.0);

        public override bool IsAutorestartEnabled
        {
            get { return false; }
        }

        public override RequestCode OnStart(
            InteractionContext context)
        {
            WriteSystemMessage("Usability list interaction OnStart.");

            Description = "Вставить таблицу применимости";
            PromptForStatusLine = "Укажите точку вставки таблицы применимости.";
            IsPlacementFilterActive = false;

            StructureSegmentSeeker seeker =
                new StructureSegmentSeeker();

            _dictionary =
                seeker.GetDictionaryCopyAndDispose();

            if (_dictionary == null || _dictionary.Count == 0)
            {
                WriteSystemMessage("Usability list insertion cancelled: dictionary is empty.");
                return (RequestCode)RequestAbort;
            }

            WriteSystemMessage(
                string.Format(
                    "Usability list dictionary created. Description group count: {0}",
                    _dictionary.Count));

            return (RequestCode)RequestPoint;
        }

        public override RequestCode OnPoint(
            Position position)
        {
            WriteSystemMessage("Usability list interaction OnPoint.");

            PointD finalPosition =
                position.FinalPosition;

            _insertionPoint =
                new PointD(
                    finalPosition.X,
                    finalPosition.Y);

            WriteSystemMessage(
                string.Format(
                    "Usability list insertion point: X={0}, Y={1}",
                    _insertionPoint.X,
                    _insertionPoint.Y));

            return (RequestCode)RequestSuccess;
        }

        public override void OnSuccess(
            InteractionContext context)
        {
            WriteSystemMessage("Usability list interaction OnSuccess.");

            Page page = Page;

            if (page == null || !page.IsValid)
            {
                WriteSystemMessage("Usability list insertion failed: interaction page is invalid.");
                return;
            }

            UsabilityList usabilityList =
                new UsabilityList(
                    page,
                    _dictionary);

            usabilityList.SetInsertionPoint(_insertionPoint);
            usabilityList.Render();

            try
            {
                new Edit().RedrawGed();
            }
            catch
            {
            }
        }

        public override void OnCancel()
        {
            WriteSystemMessage("Usability list interaction OnCancel.");
            base.OnCancel();
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