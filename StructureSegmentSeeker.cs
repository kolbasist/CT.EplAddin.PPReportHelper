using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Eplan.EplApi.Base;
using Eplan.EplApi.DataModel;
using Eplan.EplApi.DataModel.Graphics;
using Eplan.EplApi.DataModel.Planning;
using Eplan.EplApi.HEServices;
using PpStructureSegment = Eplan.EplApi.DataModel.Planning.StructureSegment;

namespace CT.Epladdin.PPReportHelper
{
    internal sealed class StructureSegmentSeeker
    {
        private const int StructureSegmentDescriptionPropertyId = 44005;
        private const int StructureSegmentDesignationPropertyId = 44004;
        private const int StructureSegmentMountingPlacePropertyId = 1220;

        private readonly List<StorableObject> _objectsToDispose =
            new List<StorableObject>();

        private Project _project;

        internal StructureSegmentSeeker()
        {
        }

        public DetachedStructureSegmentDictionary GetDictionaryCopyAndDispose()
        {
            try
            {
                StructureSegmentDictionary sourceDictionary =
                    BuildStructureSegmentDictionaryFromActivePage();

                if (sourceDictionary == null)
                {
                    WriteSystemMessage("StructureSegmentDictionary was not created.");
                    return new DetachedStructureSegmentDictionary();
                }

                using (sourceDictionary)
                {
                    DetachedStructureSegmentDictionary detachedCopy =
                        CreateDetachedDictionaryCopy(
                            sourceDictionary,
                            _project);

                    WriteDetachedDictionaryToSystemMessages(detachedCopy);

                    return detachedCopy;
                }
            }
            finally
            {
                Dispose();
            }
        }

        private StructureSegmentDictionary BuildStructureSegmentDictionaryFromActivePage()
        {
            if (!IsGedActive())
            {
                WriteSystemMessage("GED is not active.");
                return null;
            }

            Page activePage =
                GetActivePage();

            if (activePage == null || !activePage.IsValid)
            {
                WriteSystemMessage("Active page was not found.");
                return null;
            }

            Track(activePage);

            if (!IsActivePagePAndI(activePage))
            {
                WriteSystemMessage(
                    string.Format(
                        "Active page is not P&I / functional automation diagram. Page type: '{0}'.",
                        GetPageTypeText(activePage)));

                return null;
            }

            Project project =
                activePage.Project;

            if (project == null || !project.IsValid)
            {
                WriteSystemMessage("Active page project was not found.");
                return null;
            }

            _project = project;

            IReadOnlyList<PlanningSegment> pageFunctions =
                GetPagePreplanningFunctions(activePage);

            WriteSystemMessage(
                string.Format(
                    "Preplanning functions on active page: {0}",
                    pageFunctions.Count));

            IReadOnlyList<PpStructureSegment> pageStructureSegments =
                GetFunctionsSuperiorStructureSegments(
                    pageFunctions,
                    project);

            WriteSystemMessage(
                string.Format(
                    "Superior structure segments on active page: {0}",
                    pageStructureSegments.Count));

            if (pageStructureSegments.Count == 0)
            {
                WriteSystemMessage("No superior structure segments found on active page.");
                return null;
            }

            IReadOnlyList<PpStructureSegment> pageStructureSegmentsWithDescription =
                pageStructureSegments
                    .Where(segment => HasNonEmptyDescription44005(segment, project))
                    .ToList();

            WriteSystemMessage(
                string.Format(
                    "Superior structure segments on active page with non-empty 44005: {0}",
                    pageStructureSegmentsWithDescription.Count));

            if (pageStructureSegmentsWithDescription.Count == 0)
            {
                WriteSystemMessage("No structure segments with non-empty description 44005 found on active page.");
                return null;
            }

            HashSet<string> requiredDescriptionKeys =
                GetDescriptionKeys(
                    pageStructureSegmentsWithDescription,
                    project);

            WriteSystemMessage(
                string.Format(
                    "Different non-empty descriptions 44005 on active page: {0}",
                    requiredDescriptionKeys.Count));

            StructureSegmentDictionary dictionary =
                new StructureSegmentDictionary(
                    structureSegment =>
                        GetStructureSegmentDescription44005Text(
                            structureSegment,
                            project));

            IReadOnlyList<PpStructureSegment> allProjectStructureSegments =
                FindAllProjectStructureSegments(project);

            WriteSystemMessage(
                string.Format(
                    "All project structure segments: {0}",
                    allProjectStructureSegments.Count));

            IReadOnlyList<PpStructureSegment> projectStructureSegmentsWithSameDescription =
                FindStructureSegmentsWithDescriptionKeys(
                    allProjectStructureSegments,
                    requiredDescriptionKeys,
                    project);

            WriteSystemMessage(
                string.Format(
                    "Project structure segments with matching 44005: {0}",
                    projectStructureSegmentsWithSameDescription.Count));

            IReadOnlyList<PpStructureSegment> dictionarySegments =
                MergeUniqueStructureSegments(
                    pageStructureSegmentsWithDescription,
                    projectStructureSegmentsWithSameDescription,
                    project);

            dictionary.Fill(dictionarySegments);

            return dictionary;
        }

        private bool IsGedActive()
        {
            Page activePageFromGed =
                TryGetActivePageFromGraphicalEditor();

            if (activePageFromGed != null && activePageFromGed.IsValid)
            {
                Track(activePageFromGed);
                return true;
            }

            try
            {
                SelectionSet selectionSet =
                    new SelectionSet();

                Page[] selectedPages =
                    selectionSet.GetSelectedPages();

                if (selectedPages != null)
                {
                    foreach (Page page in selectedPages)
                    {
                        if (page != null && page.IsValid)
                        {
                            Track(page);
                            return true;
                        }
                    }
                }

                object selection =
                    TryGetPublicPropertyValue(
                        selectionSet,
                        "Selection");

                IEnumerable selectionEnumerable =
                    selection as IEnumerable;

                if (selectionEnumerable == null)
                {
                    return false;
                }

                foreach (object selectionEntry in selectionEnumerable)
                {
                    Page page =
                        GetPageFromSelectionEntry(selectionEntry);

                    if (page != null && page.IsValid)
                    {
                        Track(page);
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private Page GetActivePage()
        {
            Page activePageFromGed =
                TryGetActivePageFromGraphicalEditor();

            if (activePageFromGed != null && activePageFromGed.IsValid)
            {
                return activePageFromGed;
            }

            SelectionSet selectionSet =
                new SelectionSet();

            Page[] selectedPages =
                selectionSet.GetSelectedPages();

            if (selectedPages != null && selectedPages.Length > 0)
            {
                Page selectedPage =
                    selectedPages.FirstOrDefault(
                        page => page != null && page.IsValid);

                if (selectedPage != null)
                {
                    return selectedPage;
                }
            }

            object selection =
                TryGetPublicPropertyValue(
                    selectionSet,
                    "Selection");

            IEnumerable selectionEnumerable =
                selection as IEnumerable;

            if (selectionEnumerable == null)
            {
                return null;
            }

            foreach (object selectionEntry in selectionEnumerable)
            {
                Page page =
                    GetPageFromSelectionEntry(selectionEntry);

                if (page != null && page.IsValid)
                {
                    return page;
                }
            }

            return null;
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

                string[] propertyNames =
                {
                    "CurrentPage",
                    "ActivePage",
                    "Page"
                };

                foreach (string propertyName in propertyNames)
                {
                    object value =
                        TryGetPublicPropertyValue(
                            graphicalEditor,
                            propertyName);

                    Page page =
                        value as Page;

                    if (page != null && page.IsValid)
                    {
                        return page;
                    }
                }

                string[] methodNames =
                {
                    "GetCurrentPage",
                    "GetActivePage"
                };

                foreach (string methodName in methodNames)
                {
                    object value =
                        TryInvokePublicMethod(
                            graphicalEditor,
                            methodName);

                    Page page =
                        value as Page;

                    if (page != null && page.IsValid)
                    {
                        return page;
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
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

        private static bool IsActivePagePAndI(
            Page page)
        {
            if (page == null || !page.IsValid)
            {
                return false;
            }

            string pageTypeText =
                GetPageTypeText(page);

            if (string.IsNullOrWhiteSpace(pageTypeText))
            {
                return false;
            }

            return ContainsIgnoreCase(pageTypeText, "ProcessAndInstrumentationDiagram") ||
                   ContainsIgnoreCase(pageTypeText, "ProcessAndInstrumentation") ||
                   ContainsIgnoreCase(pageTypeText, "PAndI") ||
                   ContainsIgnoreCase(pageTypeText, "P&I") ||
                   ContainsIgnoreCase(pageTypeText, "PID") ||
                   ContainsIgnoreCase(pageTypeText, "PCT") ||
                   ContainsIgnoreCase(pageTypeText, "Functional");
        }

        private static string GetPageTypeText(
            Page page)
        {
            if (page == null || !page.IsValid)
            {
                return string.Empty;
            }

            object pageType =
                TryGetPublicPropertyValue(page, "PageType") ??
                TryGetPublicPropertyValue(page, "DocumentType") ??
                TryGetPublicPropertyValue(page, "Type");

            return pageType == null
                ? string.Empty
                : pageType.ToString();
        }

        private IReadOnlyList<PlanningSegment> GetPagePreplanningFunctions(
            Page page)
        {
            List<PlanningSegment> result =
                new List<PlanningSegment>();

            foreach (Placement placement in page.AllPlacements)
            {
                Track(placement);

                SegmentPlacement segmentPlacement =
                    placement as SegmentPlacement;

                if (segmentPlacement == null)
                {
                    continue;
                }

                PlanningSegment planningSegment =
                    segmentPlacement.Segment as PlanningSegment;

                if (planningSegment == null || !planningSegment.IsValid)
                {
                    continue;
                }

                Track(planningSegment);

                if (!IsPreplanningFunction(planningSegment))
                {
                    continue;
                }

                result.Add(planningSegment);
            }

            return result;
        }

        private static bool IsPreplanningFunction(
            PlanningSegment planningSegment)
        {
            if (planningSegment == null || !planningSegment.IsValid)
            {
                return false;
            }

            if (planningSegment is PpStructureSegment)
            {
                return false;
            }

            return true;
        }

        private IReadOnlyList<PpStructureSegment> GetFunctionsSuperiorStructureSegments(
            IEnumerable<PlanningSegment> pageFunctions,
            Project project)
        {
            Dictionary<string, PpStructureSegment> result =
                new Dictionary<string, PpStructureSegment>(StringComparer.Ordinal);

            foreach (PlanningSegment pageFunction in pageFunctions)
            {
                PpStructureSegment superiorStructureSegment =
                    GetSuperiorStructureSegment(pageFunction);

                if (superiorStructureSegment == null ||
                    !superiorStructureSegment.IsValid)
                {
                    continue;
                }

                string key =
                    GetStructureSegmentUniqueKey(
                        superiorStructureSegment,
                        project);

                if (!result.ContainsKey(key))
                {
                    result.Add(
                        key,
                        superiorStructureSegment);
                }
            }

            return result.Values.ToList();
        }

        private PpStructureSegment GetSuperiorStructureSegment(
            PlanningSegment planningSegment)
        {
            PlanningSegment currentSegment =
                planningSegment;

            HashSet<string> visitedKeys =
                new HashSet<string>(StringComparer.Ordinal);

            while (currentSegment != null && currentSegment.IsValid)
            {
                string currentKey =
                    GetStorableObjectUniqueKey(currentSegment);

                if (!string.IsNullOrWhiteSpace(currentKey) &&
                    visitedKeys.Contains(currentKey))
                {
                    return null;
                }

                if (!string.IsNullOrWhiteSpace(currentKey))
                {
                    visitedKeys.Add(currentKey);
                }

                PpStructureSegment structureSegment =
                    currentSegment as PpStructureSegment;

                if (structureSegment != null && structureSegment.IsValid)
                {
                    Track(structureSegment);
                    return structureSegment;
                }

                PlanningSegment superiorSegment =
                    GetSuperiorPlanningSegment(currentSegment);

                Track(superiorSegment);

                currentSegment =
                    superiorSegment;
            }

            return null;
        }

        private static PlanningSegment GetSuperiorPlanningSegment(
            PlanningSegment planningSegment)
        {
            if (planningSegment == null || !planningSegment.IsValid)
            {
                return null;
            }

            object value =
                TryGetPublicPropertyValue(planningSegment, "SuperiorSegment") ??
                TryGetPublicPropertyValue(planningSegment, "SuperiorPlanningSegment") ??
                TryGetPublicPropertyValue(planningSegment, "ParentSegment") ??
                TryGetPublicPropertyValue(planningSegment, "ParentPlanningSegment") ??
                TryGetPublicPropertyValue(planningSegment, "Parent") ??
                TryGetPublicPropertyValue(planningSegment, "StructureSegment") ??
                TryGetPublicPropertyValue(planningSegment, "SuperiorStructureSegment") ??
                TryGetPublicPropertyValue(planningSegment, "ParentStructureSegment") ??
                TryInvokePublicMethod(planningSegment, "GetSuperiorSegment") ??
                TryInvokePublicMethod(planningSegment, "GetParentSegment");

            return value as PlanningSegment;
        }

        private static bool HasNonEmptyDescription44005(
            PpStructureSegment structureSegment,
            Project project)
        {
            string description =
                GetStructureSegmentDescription44005Text(
                    structureSegment,
                    project);

            return !string.IsNullOrWhiteSpace(description);
        }

        private static HashSet<string> GetDescriptionKeys(
            IEnumerable<PpStructureSegment> structureSegments,
            Project project)
        {
            HashSet<string> result =
                new HashSet<string>(StringComparer.Ordinal);

            foreach (PpStructureSegment structureSegment in structureSegments)
            {
                string description =
                    GetStructureSegmentDescription44005Text(
                        structureSegment,
                        project);

                description =
                    StructureSegmentDictionary.NormalizeDescription(description);

                if (!string.IsNullOrWhiteSpace(description))
                {
                    result.Add(description);
                }
            }

            return result;
        }

        private static IReadOnlyList<PpStructureSegment> FindAllProjectStructureSegments(
            Project project)
        {
            List<PpStructureSegment> result =
                new List<PpStructureSegment>();

            if (project == null || !project.IsValid)
            {
                return result;
            }

            DMObjectsFinder finder =
                new DMObjectsFinder(project);

            object rawPlanningSegments =
                InvokeGetPlanningSegments(finder);

            IEnumerable planningSegments =
                rawPlanningSegments as IEnumerable;

            if (planningSegments == null)
            {
                return result;
            }

            foreach (object item in planningSegments)
            {
                PlanningSegment planningSegment =
                    item as PlanningSegment;

                if (planningSegment == null || !planningSegment.IsValid)
                {
                    SafeDispose(planningSegment);
                    continue;
                }

                PpStructureSegment structureSegment =
                    planningSegment as PpStructureSegment;

                if (structureSegment != null && structureSegment.IsValid)
                {
                    result.Add(structureSegment);
                }
                else
                {
                    SafeDispose(planningSegment);
                }
            }

            return result;
        }

        private static object InvokeGetPlanningSegments(
            DMObjectsFinder finder)
        {
            if (finder == null)
            {
                return null;
            }

            Type finderType =
                finder.GetType();

            MethodInfo[] methods =
                finderType.GetMethods();

            MethodInfo methodWithOneArgument =
                methods.FirstOrDefault(
                    method =>
                        method.Name == "GetPlanningSegments" &&
                        method.GetParameters().Length == 1);

            if (methodWithOneArgument != null)
            {
                return methodWithOneArgument.Invoke(
                    finder,
                    new object[] { null });
            }

            MethodInfo methodWithoutArguments =
                methods.FirstOrDefault(
                    method =>
                        method.Name == "GetPlanningSegments" &&
                        method.GetParameters().Length == 0);

            if (methodWithoutArguments != null)
            {
                return methodWithoutArguments.Invoke(
                    finder,
                    null);
            }

            return null;
        }

        private static IReadOnlyList<PpStructureSegment> FindStructureSegmentsWithDescriptionKeys(
            IEnumerable<PpStructureSegment> projectStructureSegments,
            HashSet<string> requiredDescriptionKeys,
            Project project)
        {
            List<PpStructureSegment> result =
                new List<PpStructureSegment>();

            if (projectStructureSegments == null ||
                requiredDescriptionKeys == null ||
                requiredDescriptionKeys.Count == 0)
            {
                return result;
            }

            foreach (PpStructureSegment projectStructureSegment in projectStructureSegments)
            {
                if (projectStructureSegment == null ||
                    !projectStructureSegment.IsValid)
                {
                    SafeDispose(projectStructureSegment);
                    continue;
                }

                string description =
                    GetStructureSegmentDescription44005Text(
                        projectStructureSegment,
                        project);

                description =
                    StructureSegmentDictionary.NormalizeDescription(description);

                if (requiredDescriptionKeys.Contains(description))
                {
                    result.Add(projectStructureSegment);
                }
                else
                {
                    SafeDispose(projectStructureSegment);
                }
            }

            return result;
        }

        private static IReadOnlyList<PpStructureSegment> MergeUniqueStructureSegments(
            IEnumerable<PpStructureSegment> pageStructureSegments,
            IEnumerable<PpStructureSegment> projectStructureSegments,
            Project project)
        {
            Dictionary<string, PpStructureSegment> result =
                new Dictionary<string, PpStructureSegment>(StringComparer.Ordinal);

            AddRangeToMerge(
                pageStructureSegments,
                result,
                project,
                disposeDuplicate: false);

            AddRangeToMerge(
                projectStructureSegments,
                result,
                project,
                disposeDuplicate: true);

            return result.Values.ToList();
        }

        private static void AddRangeToMerge(
            IEnumerable<PpStructureSegment> source,
            Dictionary<string, PpStructureSegment> target,
            Project project,
            bool disposeDuplicate)
        {
            if (source == null)
            {
                return;
            }

            foreach (PpStructureSegment structureSegment in source)
            {
                if (structureSegment == null || !structureSegment.IsValid)
                {
                    continue;
                }

                string key =
                    GetStructureSegmentUniqueKey(
                        structureSegment,
                        project);

                if (string.IsNullOrWhiteSpace(key))
                {
                    key =
                        string.Concat(
                            GetStructureSegmentDesignation44004Text(
                                structureSegment,
                                project),
                            "|",
                            GetStructureSegmentDescription44005Text(
                                structureSegment,
                                project),
                            "|",
                            GetStructureSegmentMountingPlace1220Text(
                                structureSegment,
                                project));
                }

                if (!target.ContainsKey(key))
                {
                    target.Add(
                        key,
                        structureSegment);
                }
                else if (disposeDuplicate &&
                         !ReferenceEquals(target[key], structureSegment))
                {
                    SafeDispose(structureSegment);
                }
            }
        }

        private static DetachedStructureSegmentDictionary CreateDetachedDictionaryCopy(
            StructureSegmentDictionary sourceDictionary,
            Project project)
        {
            DetachedStructureSegmentDictionary copy =
                new DetachedStructureSegmentDictionary();

            if (sourceDictionary == null || sourceDictionary.Count == 0)
            {
                return copy;
            }

            foreach (KeyValuePair<string, List<PpStructureSegment>> pair
                     in sourceDictionary)
            {
                string description44005 =
                    DetachedStructureSegmentDictionary.NormalizeDescription(pair.Key);

                if (string.IsNullOrWhiteSpace(description44005))
                {
                    continue;
                }

                List<DetachedStructureSegment> copiedSegments =
                    new List<DetachedStructureSegment>();

                if (pair.Value != null)
                {
                    foreach (PpStructureSegment structureSegment in pair.Value)
                    {
                        if (structureSegment == null || !structureSegment.IsValid)
                        {
                            continue;
                        }

                        string designation44004 =
                            GetStructureSegmentDesignation44004Text(
                                structureSegment,
                                project);

                        string mountingPlace1220 =
                            GetStructureSegmentMountingPlace1220Text(
                                structureSegment,
                                project);

                        DetachedStructureSegment copiedSegment =
                            new DetachedStructureSegment(
                                description44005,
                                designation44004,
                                mountingPlace1220);

                        copiedSegments.Add(copiedSegment);
                    }
                }

                if (copiedSegments.Count > 0)
                {
                    copy.Add(
                        description44005,
                        copiedSegments);
                }
            }

            return copy;
        }

        private static string GetStructureSegmentDescription44005Text(
            PpStructureSegment structureSegment,
            Project project)
        {
            return GetStructureSegmentPropertyAsProjectLanguageText(
                structureSegment,
                StructureSegmentDescriptionPropertyId,
                project);
        }

        private static string GetStructureSegmentDesignation44004Text(
            PpStructureSegment structureSegment,
            Project project)
        {
            return GetStructureSegmentPropertyAsProjectLanguageText(
                structureSegment,
                StructureSegmentDesignationPropertyId,
                project);
        }

        private static string GetStructureSegmentMountingPlace1220Text(
            PpStructureSegment structureSegment,
            Project project)
        {
            return GetStructureSegmentPropertyAsProjectLanguageText(
                structureSegment,
                StructureSegmentMountingPlacePropertyId,
                project);
        }

        private static string GetStructureSegmentPropertyAsProjectLanguageText(
            PpStructureSegment structureSegment,
            int propertyId,
            Project project)
        {
            if (structureSegment == null || !structureSegment.IsValid)
            {
                return string.Empty;
            }

            try
            {
                MultiLangString multiLangString =
                    structureSegment
                        .Properties[propertyId]
                        .ToMultiLangString();

                string value =
                    MultiLangStringTextExtractor.GetProjectLanguageText(
                        multiLangString,
                        project);

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
            catch
            {
            }

            try
            {
                MultiLangString rawValue = structureSegment.Properties[propertyId];

                return MultiLangStringTextExtractor.GetProjectLanguageText(
                    rawValue,
                    project);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static void WriteDetachedDictionaryToSystemMessages(
            DetachedStructureSegmentDictionary dictionary)
        {
            if (dictionary == null || dictionary.Count == 0)
            {
                WriteSystemMessage("DetachedStructureSegmentDictionary is empty.");
                return;
            }

            WriteSystemMessage(
                string.Format(
                    "DetachedStructureSegmentDictionary contains {0} description group(s).",
                    dictionary.Count));

            foreach (KeyValuePair<string, List<DetachedStructureSegment>> pair
                     in dictionary.OrderBy(item => item.Key ?? string.Empty))
            {
                string description =
                    pair.Key ?? string.Empty;

                WriteSystemMessage(
                    string.Format(
                        "Description 44005: '{0}', segment count: {1}",
                        description,
                        pair.Value.Count));

                foreach (DetachedStructureSegment segment
                         in pair.Value.OrderBy(item => item.Designation44004 ?? string.Empty))
                {
                    WriteSystemMessage(
                        string.Format(
                            "    Structure segment: 44004='{0}', 1220='{1}', 44005='{2}'",
                            segment.Designation44004 ?? string.Empty,
                            segment.MountingPlace1220 ?? string.Empty,
                            segment.Description44005 ?? string.Empty));
                }
            }
        }

        private static string GetStructureSegmentUniqueKey(
            PpStructureSegment structureSegment,
            Project project)
        {
            if (structureSegment == null || !structureSegment.IsValid)
            {
                return string.Empty;
            }

            string objectKey =
                GetStorableObjectUniqueKey(structureSegment);

            if (!string.IsNullOrWhiteSpace(objectKey))
            {
                return objectKey;
            }

            return string.Concat(
                GetStructureSegmentMountingPlace1220Text(structureSegment, project),
                "|",
                GetStructureSegmentDesignation44004Text(structureSegment, project),
                "|",
                GetStructureSegmentDescription44005Text(structureSegment, project));
        }

        private static string GetStorableObjectUniqueKey(
            StorableObject storableObject)
        {
            if (storableObject == null || !storableObject.IsValid)
            {
                return string.Empty;
            }

            string objectIdentifier =
                TryGetPublicPropertyAsString(
                    storableObject,
                    "ObjectIdentifier");

            if (!string.IsNullOrWhiteSpace(objectIdentifier))
            {
                return objectIdentifier;
            }

            string id =
                TryGetPublicPropertyAsString(
                    storableObject,
                    "Id");

            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }

            string idUpper =
                TryGetPublicPropertyAsString(
                    storableObject,
                    "ID");

            if (!string.IsNullOrWhiteSpace(idUpper))
            {
                return idUpper;
            }

            return storableObject.GetHashCode().ToString();
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

        private static string TryGetPublicPropertyAsString(
            object source,
            string propertyName)
        {
            object value =
                TryGetPublicPropertyValue(
                    source,
                    propertyName);

            return value == null
                ? string.Empty
                : value.ToString();
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

        private static bool ContainsIgnoreCase(
            string source,
            string value)
        {
            if (source == null || value == null)
            {
                return false;
            }

            return source.IndexOf(
                value,
                StringComparison.OrdinalIgnoreCase) >= 0;
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

        private void Track(
            StorableObject storableObject)
        {
            if (storableObject != null)
            {
                _objectsToDispose.Add(storableObject);
            }
        }

        private void Dispose()
        {
            DisposeStorableObjects();
            _project = null;
        }

        private void DisposeStorableObjects()
        {
            foreach (StorableObject storableObject in _objectsToDispose)
            {
                SafeDispose(storableObject);
            }

            _objectsToDispose.Clear();
        }

        private static void SafeDispose(
            StorableObject storableObject)
        {
            if (storableObject == null)
            {
                return;
            }

            try
            {
                storableObject.Dispose();
            }
            catch
            {
            }
        }
    }
}