using GuacamoleClient.Common.Settings;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GuacamoleClient.WinForms
{
    internal static class UITools
    {
        public static Color ResolveProfilePrimaryColor(GuacamoleServerProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var hex = ColorValueResolver.ResolveToHex(profile.ColorValue);
            return ParseHexColor(hex);
        }

        public static Color ParseHexColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex)) throw new ArgumentException("hex required", nameof(hex));
            var v = hex.Trim();
            if (v.StartsWith("#")) v = v.Substring(1);
            if (v.Length != 6) throw new FormatException("Expected #RRGGBB");

            int r = Convert.ToInt32(v.Substring(0, 2), 16);
            int g = Convert.ToInt32(v.Substring(2, 2), 16);
            int b = Convert.ToInt32(v.Substring(4, 2), 16);
            return Color.FromArgb(r, g, b);
        }

        /// <summary>
        /// Switch visibility of separator lines depending on visibility of ToolStripItems
        /// (prevent separator lines at the beginning or end of a menu or duplicate lines)
        /// </summary>
        /// <param name="menu"></param>
        public static void SwitchSeparatorLinesVisibility(ToolStripItemCollection menu)
        {
            bool previousVisibleItemIsSeparator = true;

            // Switch separator visibility ON if required
            for (int i = 0; i <= menu.Count - 1; i++)
            {
                if (menu[i].GetType() == typeof(ToolStripSeparator))
                {
                    if (previousVisibleItemIsSeparator)
                    {
                        // Current separator is not required
                        SwitchToolStripVisibility(menu[i], false, false);
                    }
                    else
                    {
                        // Current separator is required
                        SwitchToolStripVisibility(menu[i], true, false);
                        previousVisibleItemIsSeparator = true;
                    }
                }
                else
                {
                    if (menu[i].Available)
                    {
                        // Reset PreviousVisibleItemIsSeparator status
                        previousVisibleItemIsSeparator = false;
                    }
                }

                // Recursive call
                if (menu[i].Available
                    && typeof(ToolStripDropDownItem).IsInstanceOfType(menu[i])
                    && ((ToolStripDropDownItem)menu[i]).DropDownItems.Count > 0)
                {
                    SwitchSeparatorLinesVisibility(((ToolStripDropDownItem)menu[i]).DropDownItems);
                }
            }

            // Switch separator visibility OFF if last visible item
            for (int i = menu.Count - 1; i >= 0; i--)
            {
                if (menu[i].GetType() == typeof(ToolStripSeparator))
                {
                    if (menu[i].Available)
                    {
                        // Current separator is not required
                        SwitchToolStripVisibility(menu[i], false, false);
                        return;
                    }
                }
                else
                {
                    if (menu[i].Available)
                    {
                        // Another ToolStripItem type is visible, exit here
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Switch visibility of a ToolStripItem
        /// </summary>
        public static void SwitchToolStripVisibility(ToolStripItem item, bool visible, bool bold)
        {
            item.Available = visible;

            if (bold)
            {
                item.Font = new Font(item.Font, item.Font.Style | FontStyle.Bold);
            }
            else
            {
                // VB: item.Font.Style And FontStyle.Regular
                // In C#, this is "clear Bold flag" (and keep other flags as-is)
                item.Font = new Font(item.Font, item.Font.Style & ~FontStyle.Bold);
            }
        }

        /// <summary>
        /// Switch visibility of a ToolStripItem ON or OFF depending on visibility of at least 1 child entry
        /// </summary>
        public static void SwitchOnOffToolStripVisibilityBasedOnChildrenVisibility(ToolStripMenuItem item, bool forceBold = false)
        {
            SwitchOnOffToolStripVisibilityBasedOnChildrenVisibility(item, item.DropDownItems, forceBold);
        }

        /// <summary>
        /// Switch visibility of a ToolStripItem ON or OFF depending on visibility of at least 1 child entry
        /// </summary>
        public static void SwitchOnOffToolStripVisibilityBasedOnChildrenVisibility(
            ToolStripItem item,
            ToolStripItemCollection children,
            bool forceBold = false)
        {
            bool hasVisibleItems = false;
            bool hasBoldItems = false;

            foreach (ToolStripItem child in children)
            {
                if (child.Available)
                {
                    hasVisibleItems = true;

                    if (child.Font.Bold)
                        hasBoldItems = true;

                    // forces an independent copy of font style which is not switched with the parent's font setting
                    child.Font = new Font(child.Font, child.Font.Style);
                }
            }

            SwitchToolStripVisibility(item, hasVisibleItems, hasBoldItems || forceBold);
        }

        /// <summary>
        /// Are there 1 or more visible child items in a ToolStripItem?
        /// </summary>
        public static bool HasVisibleToolStripItems(ToolStripItemCollection children)
        {
            foreach (ToolStripItem child in children)
            {
                if (child.Available)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Switch off visibility of all children of a ToolStripItem
        /// </summary>
        public static void SwitchOffToolStripChildrenVisibility(ToolStripItemCollection items)
        {
            foreach (ToolStripItem child in items)
            {
                SwitchToolStripVisibility(child, false, false);
            }
        }

        /// <summary>
        /// Switch on visibility of all children of a ToolStripItem (without bold)
        /// </summary>
        public static void SwitchOnToolStripChildrenVisibility(ToolStripItemCollection items)
        {
            foreach (ToolStripItem child in items)
            {
                SwitchToolStripVisibility(child, true, false);
            }
        }

        /// <summary>
        /// Remove separator lines depending on visibility of ToolStripItems
        /// (prevent separator lines at the beginning or end of a menu or duplicate lines)
        /// </summary>
        public static void RemoveUnnecessarySeparatorLines(ToolStripItemCollection menu)
        {
            // First normalize separator visibility
            SwitchSeparatorLinesVisibility(menu);

            // Remove all separator lines which are not visible
            for (int i = menu.Count - 1; i >= 0; i--)
            {
                if (menu[i] is ToolStripSeparator)
                {
                    if (!menu[i].Available)
                    {
                        // Current separator is not required
                        menu.RemoveAt(i);
                    }
                }
            }
        }

    }
}