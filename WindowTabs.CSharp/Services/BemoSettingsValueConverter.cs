using System;
using System.Collections.Generic;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Reflection;
using Bemo;
using ManagedTabAppearanceInfo = WindowTabs.CSharp.Models.TabAppearanceInfo;

namespace WindowTabs.CSharp.Services
{
    internal sealed class BemoSettingsValueConverter
    {
        public Set2<string> ToSet2(IEnumerable<string> values)
        {
            return new Set2<string>(
                new List2<string>(FSharpOption<IEnumerable<string>>.Some(values ?? Array.Empty<string>())));
        }

        public List<string> ToStringList(Set2<string> values)
        {
            return values?.items?.list == null ? new List<string>() : new List<string>(values.items.list);
        }

        public Bemo.TabAppearanceInfo ToBemoTabAppearance(ManagedTabAppearanceInfo appearance)
        {
            var source = appearance ?? SettingsDefaults.CreateDefaultTabAppearance();
            var recordValues = new object[]
            {
                source.TabHeight,
                source.TabMaxWidth,
                source.TabPinnedTabWidth,
                source.TabPinnedTabWidthIcon,
                source.TabOverlap,
                source.TabHeightOffset,
                source.TabIndentFlipped,
                source.TabIndentNormal,
                source.TabInactiveTextColor,
                source.TabMouseOverTextColor,
                source.TabActiveTextColor,
                source.TabFlashTextColor,
                source.TabInactiveTabColor,
                source.TabMouseOverTabColor,
                source.TabActiveTabColor,
                source.TabFlashTabColor,
                source.TabInactiveBorderColor,
                source.TabMouseOverBorderColor,
                source.TabActiveBorderColor,
                source.TabFlashBorderColor
            };

            return (Bemo.TabAppearanceInfo)FSharpValue.MakeRecord(typeof(Bemo.TabAppearanceInfo), recordValues, null);
        }

        public ManagedTabAppearanceInfo ToManagedTabAppearance(Bemo.TabAppearanceInfo appearance)
        {
            if (appearance == null)
            {
                return SettingsDefaults.CreateDefaultTabAppearance();
            }

            return new ManagedTabAppearanceInfo
            {
                TabHeight = appearance.tabHeight,
                TabMaxWidth = appearance.tabMaxWidth,
                TabPinnedTabWidth = appearance.tabPinnedTabWidth,
                TabPinnedTabWidthIcon = appearance.tabPinnedTabWidthIcon,
                TabOverlap = appearance.tabOverlap,
                TabHeightOffset = appearance.tabHeightOffset,
                TabIndentFlipped = appearance.tabIndentFlipped,
                TabIndentNormal = appearance.tabIndentNormal,
                TabInactiveTextColor = appearance.tabInactiveTextColor,
                TabMouseOverTextColor = appearance.tabMouseOverTextColor,
                TabActiveTextColor = appearance.tabActiveTextColor,
                TabFlashTextColor = appearance.tabFlashTextColor,
                TabInactiveTabColor = appearance.tabInactiveTabColor,
                TabMouseOverTabColor = appearance.tabMouseOverTabColor,
                TabActiveTabColor = appearance.tabActiveTabColor,
                TabFlashTabColor = appearance.tabFlashTabColor,
                TabInactiveBorderColor = appearance.tabInactiveBorderColor,
                TabMouseOverBorderColor = appearance.tabMouseOverBorderColor,
                TabActiveBorderColor = appearance.tabActiveBorderColor,
                TabFlashBorderColor = appearance.tabFlashBorderColor
            };
        }
    }
}
