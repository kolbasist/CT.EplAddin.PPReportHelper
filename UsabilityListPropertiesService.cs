using System;
using System.Reflection;
using Eplan.EplApi.Base;
using Eplan.EplApi.DataModel;
using Eplan.EplApi.HEServices;

namespace CT.Epladdin.PPReportHelper
{
    internal static class UsabilityListPropertiesService
    {
        internal static bool TryOpenSelectedListProperties()
        {
            Block block =
                TryGetSelectedUsabilityListBlock();

            if (block == null || !block.IsValid)
            {
                return false;
            }

            UsabilityList list =
                new UsabilityList(block);

            list.RefreshCurrentPropertiesFromBlockGraphics();
            list.RefreshOriginFromBlockLocation();

            if (!list.HasData)
            {
                list.TryReloadDataFromActivePage();
            }

            UsabilityListPropertiesDialog.ShowModal(list);

            return true;
        }

        private static Block TryGetSelectedUsabilityListBlock()
        {
            SelectionSet selectionSet =
                new SelectionSet
                {
                    LockProjectByDefault = false,
                    LockSelectionByDefault = false
                };

            try
            {
                selectionSet.GetCurrentProject(true);
            }
            catch
            {
            }

            StorableObject selectedObject = null;

            try
            {
                selectedObject =
                    selectionSet.GetSelectedObject(true);
            }
            catch
            {
                return null;
            }

            Block block =
                selectedObject as Block;

            if (UsabilityList.IsUsabilityListBlock(block))
            {
                return block;
            }

            object group =
                TryGetPublicPropertyValue(selectedObject, "Group");

            block = group as Block;

            if (UsabilityList.IsUsabilityListBlock(block))
            {
                return block;
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
    }
}