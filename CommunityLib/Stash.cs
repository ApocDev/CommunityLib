﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Loki.Bot;
using Loki.Common;
using Loki.Game;
using Loki.Game.Objects;
using GuildStashUI = Loki.Game.LokiPoe.InGameState.GuildStashUi;
using StashUI = Loki.Game.LokiPoe.InGameState.StashUi;

namespace CommunityLib
{
    public class Stash
    {
        /// <summary>
        /// Checks if the item will fit in the current stash tab. Only for use with FastMove
        /// </summary>
        /// <param name="item"></param>
        /// <param name="guild"></param>
        /// <returns></returns>
        public static bool FastMoveCanFitItem(Item item, bool guild = false)
        {
            //We can't go futher if the stash is not opened
            var canGo = guild
                ? !GuildStashUI.IsOpened || GuildStashUI.StashTabInfo.IsPublic || GuildStashUI.StashTabInfo.IsRemoveOnly ||
                  StashUI.StashTabInfo.IsPremiumDivination
                : !StashUI.IsOpened || StashUI.StashTabInfo.IsPublic || StashUI.StashTabInfo.IsRemoveOnly ||
                  StashUI.StashTabInfo.IsPremiumDivination;

            if (canGo)
                return false;

            //If it's regular tab then it's rather simple
            if (guild || (!StashUI.StashTabInfo.IsPremiumCurrency && !StashUI.StashTabInfo.IsPremiumEssence))
            {
                int column, row;
                return guild
                ? GuildStashUI.InventoryControl.Inventory.CanFitItem(item, out column, out row)
                : StashUI.InventoryControl.Inventory.CanFitItem(item, out column, out row);
            }

            if (StashUI.StashTabInfo.IsPremiumCurrency)
            {
                //We can only fit stackables in the currency tab
                if (item.MaxStackCount <= 1)
                    return false;

                var wrps = new List<InventoryControlWrapper>();
                //Wrapper especially for that one thing
                var wrapper = StashUI.CurrencyTab.GetInventoryControlForMetadata(item.Metadata);
                if (wrapper != null)
                    wrps.Add(wrapper);

                StashUI.CurrencyTabInventoryControlsMisc.ForEach(w => wrps.Add(w));

                foreach (var wrap in wrps)
                {
                    //There's no item, it's free to use
                    if (wrap.CustomTabItem == null)
                        return true;

                    //We can fit in here.
                    var freeSpace = wrap.CustomTabItem.MaxCurrencyTabStackCount - wrap.CustomTabItem.StackCount;
                    if (freeSpace - item.StackCount >= 0)
                        return true;
                }
            }
            else if (StashUI.StashTabInfo.IsPremiumEssence)
            {
                //We can only fit stackables in the essence tab
                if (item.MaxStackCount <= 1)
                    return false;

                var wrps = new List<InventoryControlWrapper>();
                //Wrapper especially for that one thing
                var wrapper = StashUI.EssenceTab.GetInventoryControlForMetadata(item.Metadata);
                if (wrapper != null)
                    wrps.Add(wrapper);

                //StashUI.EssenceTab.NonEssences.ForEach(w => wrps.Add(w));

                foreach (var wrap in wrps)
                {
                    //There's no item, it's free to use
                    if (wrap.CustomTabItem == null)
                        return true;

                    //We can fit in here.
                    var freeSpace = wrap.CustomTabItem.MaxCurrencyTabStackCount - wrap.CustomTabItem.StackCount;
                    if (freeSpace - item.StackCount >= 0)
                        return true;
                }
            }
            

            return false;
        }

        /// <summary>
        /// Return the InventoryControlWrapper for an item and its class
        /// </summary>
        /// <param name="itemName"></param>
        /// <returns>StashItem</returns>
        public static CachedItemObject FindItemInStashTab(string itemName)
        {
            return FindItemInStashTab(d => d.FullName.Equals(itemName));
        }

        /// <summary>
        /// Return the InventoryControlWrapper for an item and its class
        /// </summary>
        /// <param name="condition"></param>
        /// <returns>StashItem</returns>
        public static CachedItemObject FindItemInStashTab(CommunityLib.FindItemDelegate condition)
        {
            if (StashUI.StashTabInfo.IsPremiumDivination)
            {
                //Cant take item from Div cards tab.
                return null;
            }
            //If it's regular tab then it's rather simple
            if (!StashUI.StashTabInfo.IsPremiumCurrency && !StashUI.StashTabInfo.IsPremiumEssence)
            {
                // Gather the first item matching the condition
                var item = StashUI.InventoryControl.Inventory.Items.FirstOrDefault(d => condition(d));
                // Return it if this one is not null
                if (item != null)
                    return new CachedItemObject(StashUI.InventoryControl, item, StashUI.TabControl.CurrentTabName);
            }

            //Premium stash tab

            else if(StashUI.StashTabInfo.IsPremiumCurrency)
            {
                var wrapper = StashUI.CurrencyTab.All.FirstOrDefault(d => d.CustomTabItem != null && condition(d.CustomTabItem));
                var item = wrapper?.CustomTabItem;
                if (item != null)
                    return new CachedItemObject(wrapper, item, StashUI.TabControl.CurrentTabName);
            }
            //Esence shard tab
            else if (StashUI.StashTabInfo.IsPremiumEssence)
            {
                var wrapper = StashUI.EssenceTab.All.FirstOrDefault(d => d.CustomTabItem != null && condition(d.CustomTabItem));
                var item = wrapper?.CustomTabItem;
                if (item != null)
                    return new CachedItemObject(wrapper, item, StashUI.TabControl.CurrentTabName);
            }

            return null;
        }

        /// <summary>
        /// Return the InventoryControlWrapper for an item and its class
        /// </summary>
        /// <param name="itemName"></param>
        /// <returns>StashItem</returns>
        public static List<CachedItemObject> FindItemsInStashTab(string itemName)
        {
            return FindItemsInStashTab(d => d.FullName.Equals(itemName));
        }

        /// <summary>
        /// Find items in a stash tab matching a condition
        /// </summary>
        /// <param name="condition">Condition to pass item through</param>
        /// <returns>List of CachedItemObjects</returns>
        public static List<CachedItemObject> FindItemsInStashTab(CommunityLib.FindItemDelegate condition)
        {
            var ret = new List<CachedItemObject>();
            if (StashUI.StashTabInfo.IsPremiumDivination)
            {
                //Cant take item from Div cards tab.
                return null;
            }
            //If it's regular tab then it's rather simple
            if (!StashUI.StashTabInfo.IsPremiumCurrency && !StashUI.StashTabInfo.IsPremiumEssence)
            {
                // Gather the first item matching the condition
                var items = StashUI.InventoryControl.Inventory.Items.Where(d => condition(d)).ToList();
                ret.AddRange(items.Select(item => new CachedItemObject(StashUI.InventoryControl, item, StashUI.TabControl.CurrentTabName)));
            }

            //Premium stash tab
            else if (StashUI.StashTabInfo.IsPremiumCurrency)
            {
                var wrappers = StashUI.CurrencyTab.All.Where(d => d.CustomTabItem != null && condition(d.CustomTabItem)).ToList();
                foreach (var wrapper in wrappers)
                {
                    var item = wrapper?.CustomTabItem;
                    if (item != null)
                        ret.Add(new CachedItemObject(wrapper, item, StashUI.TabControl.CurrentTabName));
                }
            }
            //Esence shard tab
            else if (StashUI.StashTabInfo.IsPremiumEssence)
            {
                var wrappers = StashUI.EssenceTab.All.Where(d => d.CustomTabItem != null && condition(d.CustomTabItem)).ToList();
                foreach (var wrapper in wrappers)
                {
                    var item = wrapper?.CustomTabItem;
                    if (item != null)
                        ret.Add(new CachedItemObject(wrapper, item, StashUI.TabControl.CurrentTabName));
                }
            }

            return ret;
        }

        /// <summary>
        /// Overload for FindTabContainingItem to an item by its name
        /// </summary>
        /// <param name="itemName">The item name</param>
        /// <returns></returns>
        public static async Task<Tuple<Results.FindItemInTabResult, CachedItemObject>> FindTabContainingItem(string itemName)
        {
            return await FindTabContainingItem(d => d.FullName.Equals(itemName));
        }

        /// <summary>
        /// This function iterates through the stash to find an item by name
        /// If a tab is reached and the item is found, GUI will be stopped on this tab so you can directly interact with it.
        /// </summary>
        /// <param name="condition">Condition to pass item through</param>
        /// <returns></returns>
        public static async Task<Tuple<Results.FindItemInTabResult, CachedItemObject>> FindTabContainingItem(CommunityLib.FindItemDelegate condition)
        {
            // If stash isn't opened, abort this and return
            if (!await OpenStashTabTask())
                return new Tuple<Results.FindItemInTabResult, CachedItemObject>(Results.FindItemInTabResult.GuiNotOpened, null);

            // If we fail to go to first tab, return
            // if (GoToFirstTab() != SwitchToTabResult.None)
            //     return new Tuple<Results.FindItemInTabResult, StashItem>(Results.FindItemInTabResult.GoToFirstTabFailed, null);

            foreach (var tabName in StashUI.TabControl.TabNames)
            {             
                // If the item has no occurences in this tab, switch to the next one
                var it = FindItemInStashTab(condition);
                if (it == null)
                {
                    // On last tab? break execution
                    if (StashUI.TabControl.IsOnLastTab)
                        break;

                    int switchAttemptsPerTab = 0;
                    while (true)
                    {
                        // If we tried 3 times to switch and failed, return
                        if (switchAttemptsPerTab > 2)
                            return new Tuple<Results.FindItemInTabResult, CachedItemObject>(Results.FindItemInTabResult.SwitchToTabFailed, null);

                        var switchTab = StashUI.TabControl.SwitchToTabMouse(tabName);

                        // If the switch went fine, keep searching
                        if (switchTab == SwitchToTabResult.None)
                            break;

                        switchAttemptsPerTab++;
                        await Coroutines.LatencyWait();
                        await Coroutines.ReactionWait();
                    }

                    // Keep searching...
                    await Coroutines.LatencyWait();
                    await Coroutines.ReactionWait();
                    continue;
                }

                // We Found a tab, return informations
                return new Tuple<Results.FindItemInTabResult, CachedItemObject>(Results.FindItemInTabResult.None, it);
            }

            return new Tuple<Results.FindItemInTabResult, CachedItemObject>(Results.FindItemInTabResult.ItemNotFoundInTab, null);
        }

        /// <summary>
        /// Heads to the first tab in stash (stash must be opened)
        /// </summary>
        /// <returns>SwitchToTabResult enum entry</returns>
        public static SwitchToTabResult GoToFirstTab()
        {
            return StashUI.TabControl.SwitchToTabMouse(0);
        }

        /// <summary>
        /// This coroutines handle the whole NextTab behavior, including waiting for tab change
        /// </summary>
        /// <param name="guild">Guild stash or not ?</param>
        /// <returns>SwitchToTab result enum entry</returns>
        public static async Task<SwitchToTabResult> GoToNextTab(bool guild = false)
        {
            var opened = guild ? GuildStashUI.IsOpened : StashUI.IsOpened;
            if (!opened)
                return SwitchToTabResult.UiNotOpen;

            var lastTab = guild ? GuildStashUI.TabControl.IsOnLastTab : StashUI.TabControl.IsOnLastTab;
            if (lastTab)
                return SwitchToTabResult.NoMoreTabs;

            var currentId = guild ? GuildStashUI.StashTabInfo.InventoryId : StashUI.StashTabInfo.InventoryId;
            var err = guild ? GuildStashUI.TabControl.NextTabKeyboard() : StashUI.TabControl.NextTabKeyboard();
            if (err != SwitchToTabResult.None)
                return err;

            if (await WaitForStashTabChange(currentId, guild: guild))
                return SwitchToTabResult.None;

            return SwitchToTabResult.Failed;
        }

        /// <summary>
        /// Heads to the last tab in stash (stash must be opened)
        /// </summary>
        /// <returns>SwitchToTabResult enum entry</returns>
        public static SwitchToTabResult GoToLastTab()
        {
            return StashUI.TabControl.SwitchToTabMouse(StashUI.TabControl.LastTabIndex);
        }

        /// <summary>
        /// Opens the stash at typed tab name
        /// </summary>
        /// <param name="stashTabName">If set to null or empty, first tab of the stash will be opened</param>
        /// <returns></returns>
        public static async Task<bool> OpenStashTabTask(string stashTabName = "")
        {
            //open stash
            if (!StashUI.IsOpened)
            {
                var isOpenedErr = await LibCoroutines.OpenStash();
                await Dialog.WaitForPanel(Dialog.PanelType.Stash);
                if (isOpenedErr != Results.OpenStashError.None)
                {
                    CommunityLib.Log.ErrorFormat("[OpenStashTab] Fail to open the stash. Error: {0}", isOpenedErr);
                    return false;
                }
            }

            if (string.IsNullOrEmpty(stashTabName))
            {
                var isSwitchedFtErr = GoToFirstTab();
                if (isSwitchedFtErr != SwitchToTabResult.None)
                {
                    CommunityLib.Log.ErrorFormat("[OpenStashTab] Fail to switch to the first tab");
                    return false;
                }
                await WaitForStashTabChange();
                return true;
            }

            if (StashUI.TabControl.CurrentTabName != stashTabName)
            {
                var isSwitchedErr = StashUI.TabControl.SwitchToTabMouse(stashTabName);
                if (isSwitchedErr != SwitchToTabResult.None)
                {
                    CommunityLib.Log.ErrorFormat("[OpenStashTab] Fail to switch to the tab: {0}", isSwitchedErr);
                    return false;
                }
                await WaitForStashTabChange();
            }

            return true;
        }

        /// <summary>
        /// Waits for a stash tab to change. Pass -1 to lastId to wait for the initial tab.
        /// </summary>
        /// <param name="lastId">The last InventoryId before changing tabs.</param>
        /// <param name="timeout">The timeout of the function.</param>
        /// <param name="guild">Whether it's the guild stash or not</param>
        /// <returns>true if the tab was changed and false otherwise.</returns>
        public static async Task<bool> WaitForStashTabChange(int lastId = -1, int timeout = 10000, bool guild = false)
        {
            var sw = Stopwatch.StartNew();
            var invTab = guild ? GuildStashUI.StashTabInfo : StashUI.StashTabInfo;
            while (invTab == null || invTab.InventoryId == lastId)
            {
                await Coroutine.Sleep(1);

                if (guild)
                    if (!GuildStashUI.IsOpened)
                        return false;
                else
                    if (!StashUI.IsOpened)
                        return false;

                invTab = guild ? GuildStashUI.StashTabInfo : StashUI.StashTabInfo;
                if (sw.ElapsedMilliseconds > timeout)
                    return false;
            }

            await Coroutines.LatencyWait((float)MathEx.Random(.5d, 2d));
            //await Coroutines.ReactionWait();
            return true;
        }

        /// <summary>
        /// Returns the corresponding stash, depending on the parameter passed
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public static NetworkObject DetermineStash(bool guild = false)
        {
            var stash = LokiPoe.ObjectManager.Stash;
            if (guild)
                stash = LokiPoe.ObjectManager.GuildStash;

            return stash;
        }
    }
}
