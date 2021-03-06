﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Loki.Bot;
using Loki.Common;
using Loki.Game;

namespace CommunityLib
{
    public static class Data
    {
        public static readonly Dictionary<string, Vector2i> StashesLocations =
            new Dictionary<string, Vector2i>
            {
                {"1_town", new Vector2i(246, 266)},
                {"2_town", new Vector2i(178, 195)},
                {"3_town", new Vector2i(206, 306)},
                {"4_town", new Vector2i(199, 509)}
            };

        public static readonly List<Tuple<string, string, Vector2i>> TownNpcStaticLocations =
            new List<Tuple<string, string, Vector2i>>
            {
                new Tuple<string, string, Vector2i>("1_town", "Nessa", new Vector2i(268, 253)),
                new Tuple<string, string, Vector2i>("1_town", "Tarkleigh", new Vector2i(312, 189)),
                new Tuple<string, string, Vector2i>("2_town", "Greust", new Vector2i(192, 173)),
                new Tuple<string, string, Vector2i>("2_town", "Yeena", new Vector2i(162, 240)),
                new Tuple<string, string, Vector2i>("3_town", "Clarissa", new Vector2i(147, 326)),
                new Tuple<string, string, Vector2i>("3_town", "Hargan", new Vector2i(281, 357)),
                new Tuple<string, string, Vector2i>("4_town", "Kira", new Vector2i(169, 500)),
                new Tuple<string, string, Vector2i>("4_town", "Petarus and Vanja", new Vector2i(204, 546)),
                new Tuple<string, string, Vector2i>("4_town", "Tasuni", new Vector2i(407, 447)),
                new Tuple<string, string, Vector2i>("4_town", "Dialla", new Vector2i(555, 505)), //After finishing the quests she's Dialla not Lady Dialla.
                new Tuple<string, string, Vector2i>("4_town", "Lady Dialla", new Vector2i(555, 505)),
                new Tuple<string, string, Vector2i>("4_town", "Oyun", new Vector2i(566, 498))
            };

        public static readonly Dictionary<string, Vector2i> WaypointsLocations =
            new Dictionary<string, Vector2i>
            {
                {"1_town", new Vector2i(196, 172)},
                {"2_town", new Vector2i(188, 135)},
                {"3_town", new Vector2i(217, 226)},
                {"4_town", new Vector2i(286, 491)}
            };

        //Todo change to any Language compatible
        public static class CurrencyNames
        {
            public const string ScrollFragment = "Scroll Fragment";
            public const string TransmutationShard = "Transmutation Shard";
            public const string AlterationShard = "Alteration Shard";
            public const string AlchemyShard = "Alchemy Shard";
            public const string Wisdom = "Scroll of Wisdom";
            public const string Portal = "Portal Scroll";
            public const string Scrap = "Armourer's Scrap";
            public const string Whetstone = "Blacksmith's Whetstone";
            public const string Glassblower = "Glassblower's Bauble";
            public const string Chisel = "Cartographer's Chisel";
            public const string Transmutation = "Orb of Transmutation";
            public const string Augmentation = "Orb of Augmentation";
            public const string Alteration = "Orb of Alteration";
            public const string Chromatic = "Chromatic Orb";
            public const string Chance = "Orb of Chance";
            public const string Alchemy = "Orb of Alchemy";
            public const string Jeweller = "Jeweller's Orb";
            public const string Fusing = "Orb of Fusing";
            public const string Scouring = "Orb of Scouring";
            public const string Regret = "Orb of Regret";
            public const string Chaos = "Chaos Orb";
            public const string Blessed = "Blessed Orb";
            public const string Regal = "Regal Orb";
            public const string Vaal = "Vaal Orb";
            public const string Gemcutter = "Gemcutter's Prism";
            public const string Divine = "Divine Orb";
            public const string Exalted = "Exalted Orb";
            public const string Eternal = "Eternal Orb";
            public const string Mirror = "Mirror of Kalandra";
        }

        private static readonly string[] Currency =
        {
            "Scroll of Wisdom", "Portal Scroll", "Orb of Transmutation",
            "Orb of Augmentation", "Orb of Alteration", "Jeweller's Orb",
            "Armourer's Scrap", "Blacksmith's Whetstone", "Glassblower's Bauble",
            "Cartographer's Chisel", "Gemcutter's Prism", "Chromatic Orb",
            "Orb of Fusing", "Orb of Chance", "Orb of Alchemy", "Regal Orb",
            "Exalted Orb", "Chaos Orb", "Blessed Orb", "Divine Orb",
            "Orb of Scouring", "Orb of Regret", "Vaal Orb", "Mirror of Kalandra"
        };

        public static readonly ReadOnlyCollection<string> CurrencyList = new ReadOnlyCollection<string>(Currency);
        
        public static List<CachedItemObject> CachedItemsInStash = new List<CachedItemObject>();

        /// <summary>
        /// If set to true UpdateItemsInStash will have no effect. It's automatically setting to false on every area change. Use it only if you know what you are doing!
        /// </summary>
        public static bool ItemsInStashAlreadyCached;

        /// <summary>
        /// Goes to stash, parse every file and save's it in the CachedItemsInStash. It can be runned only once per area change. Other tries will not work (to save time)
        /// </summary>
        /// <param name="force">You can force updating. Use at your own risk!</param>
        /// <returns></returns>
        public static async Task<bool> UpdateItemsInStash( bool force = false )
        {
            //No need to do it again
            if (ItemsInStashAlreadyCached && !force)
                return true;

            if (CommunityLibSettings.Instance.CacheTabsCollection.Any(d => !string.IsNullOrEmpty(d.Name)))
            {
                var joined = string.Join(", ", CommunityLibSettings.Instance.CacheTabsCollection.Select(d => d.Name));
                CommunityLib.Log.Debug($"[CommunityLib][UpdateItemsInStash] Tabs to cache: {joined}");
                return await UpdateItemsInStash(CommunityLibSettings.Instance.CacheTabsCollection.Where(d => !string.IsNullOrEmpty(d.Name)));
            }
                
            // If stash isn't opened, abort this and return
            if (!await Stash.OpenStashTabTask())
                return false;

            //Delete current stuff
            CachedItemsInStash.Clear();

            while (true)
            {
                //Making sure we can count
                if (!LokiPoe.IsInGame)
                {
                    CommunityLib.Log.ErrorFormat("[CommunityLib][UpdateItemsInStash] Disconnected?");
                    return false;
                }

                //Stash not opened
                if (!LokiPoe.InGameState.StashUi.IsOpened)
                {
                    CommunityLib.Log.InfoFormat("[CommunityLib][UpdateItemsInStash] Stash not opened? Trying again.");
                    return await UpdateItemsInStash(force);
                }

                if (LokiPoe.InGameState.StashUi.StashTabInfo.IsPublic)
                {
                    CommunityLib.Log.Info($"[CommunityLib][UpdateItemsInStash] The tab \"{LokiPoe.InGameState.StashUi.TabControl.CurrentTabName}\" is Public and is not gonna be cached");
                    goto NextTab;
                }

                if (LokiPoe.InGameState.StashUi.StashTabInfo.IsRemoveOnly)
                {
                    CommunityLib.Log.Info($"[CommunityLib][UpdateItemsInStash] The tab \"{LokiPoe.InGameState.StashUi.TabControl.CurrentTabName}\" is RemoveOnly and is not gonna be cached");
                    goto NextTab;
                }

                //Civination Cards
                if (LokiPoe.InGameState.StashUi.StashTabInfo.IsPremiumDivination)
                {
                    CommunityLib.Log.Info($"[CommunityLib][UpdateItemsInStash] The tab \"{LokiPoe.InGameState.StashUi.TabControl.CurrentTabName}\" is Divination cards Tab and is not gonna be cached");
                    goto NextTab;
                }
                //Different handling for currency tabs
                if (LokiPoe.InGameState.StashUi.StashTabInfo.IsPremiumCurrency)
                {
                    foreach (var wrapper in LokiPoe.InGameState.StashUi.CurrencyTab.All
                        .Where(wrp => wrp.CustomTabItem != null))
                        CachedItemsInStash.Add( 
                            new CachedItemObject(wrapper, wrapper.CustomTabItem, 
                                LokiPoe.InGameState.StashUi.TabControl.CurrentTabName)
                            );
                }
                //Essence shards
                else if (LokiPoe.InGameState.StashUi.StashTabInfo.IsPremiumEssence)
                {
                    foreach (var wrapper in LokiPoe.InGameState.StashUi.EssenceTab.All
                        .Where(wrp => wrp.CustomTabItem != null))
                        CachedItemsInStash.Add(
                            new CachedItemObject(wrapper, wrapper.CustomTabItem,
                                LokiPoe.InGameState.StashUi.TabControl.CurrentTabName)
                            );
                }
                else
                {
                    foreach (var item in LokiPoe.InGameState.StashUi.InventoryControl.Inventory.Items)
                        CachedItemsInStash.Add(
                            new CachedItemObject(LokiPoe.InGameState.StashUi.InventoryControl, item,
                                LokiPoe.InGameState.StashUi.TabControl.CurrentTabName)
                            );
                }

                CommunityLib.Log.Debug("[CommunityLib][UpdateItemsInStash] Parsed items in the stash tab.");

                NextTab:
                if (LokiPoe.InGameState.StashUi.TabControl.IsOnLastTab)
                {
                    CommunityLib.Log.DebugFormat("[CommunityLib][UpdateItemsInStash] We're on the last tab: \"{0}\". Finishing.", 
                        LokiPoe.InGameState.StashUi.TabControl.CurrentTabName);
                    break;
                }

                CommunityLib.Log.DebugFormat("[CommunityLib][UpdateItemsInStash] Switching tabs. Current tab: \"{0}\"", 
                    LokiPoe.InGameState.StashUi.TabControl.CurrentTabName);
                var lastId = LokiPoe.InGameState.StashUi.StashTabInfo.InventoryId;
                if (LokiPoe.InGameState.StashUi.TabControl.NextTabKeyboard() != SwitchToTabResult.None)
                {
                    await Coroutines.ReactionWait();
                    CommunityLib.Log.ErrorFormat("[CommunityLib][UpdateItemsInStash] Failed to switch tabs.");
                    return false;
                }

                //Sleep to not look too bottish
                if (!await Stash.WaitForStashTabChange(lastId))
                    CommunityLib.Log.ErrorFormat("[CommunityLib][UpdateItemsInStash] Failed to wait for stash tab to change");
                //await Coroutines.LatencyWait(2);
            }

            ItemsInStashAlreadyCached = true;
            return true;
        }

        /// <summary>
        /// Overload for UpdateItemsInStash, taking a list of tabs as parameters
        /// </summary>
        /// <param name="tabs"></param>
        /// <returns></returns>
        private static async Task<bool> UpdateItemsInStash(IEnumerable<CommunityLibSettings.StringEntry> tabs)
        {
            foreach (var tab in tabs.Select(d => d.Name).Where(tab => !string.IsNullOrEmpty(tab)))
            {
                if (await UpdateSpecificTab(tab)) continue;
                CommunityLib.Log.Info($"[CommunityLib][UpdateItemsInStash (specific)] An error happend when caching the tab \"{tab}\"");
                //return false;
            }

            ItemsInStashAlreadyCached = true;
            await Coroutines.LatencyWait();
            return true;
        }

        /// <summary>
        /// Forces the update of a specific tab
        /// It first removes the items that were in this one last check
        /// Then just reparse the whole tab
        /// Note : This function doesn't take care about the ItemInStashAlreadyCached var
        /// </summary>
        /// <param name="tabName">Tab name to be re-parsed</param>
        /// <returns>true if everything went well</returns>
        public static async Task<bool> UpdateSpecificTab(string tabName)
        {
            if (string.IsNullOrEmpty(tabName))
                return false;

            if (!await Stash.OpenStashTabTask(tabName))
                return false;

            // Then process the tab
            if (!LokiPoe.IsInGame)
            {
                CommunityLib.Log.ErrorFormat("[CommunityLib][UpdateSpecificTab] Disconnected?");
                return false;
            }

            //Stash not opened
            if (!LokiPoe.InGameState.StashUi.IsOpened)
            {
                CommunityLib.Log.InfoFormat("[CommunityLib][UpdateSpecificTab] Stash not opened... returning false");
                return false;
            }

            // Handling of Public & RemoveOnly tabs for caching (we don't want to cache diz
            if (LokiPoe.InGameState.StashUi.StashTabInfo.IsPublic)
            {
                CommunityLib.Log.Error($"[CommunityLib][UpdateSpecificTab] The tab \"{LokiPoe.InGameState.StashUi.TabControl.CurrentTabName}\" is Public and is not gonna be cached");
                return false;
            }

            if (LokiPoe.InGameState.StashUi.StashTabInfo.IsRemoveOnly)
            {
                CommunityLib.Log.Error($"[CommunityLib][UpdateSpecificTab] The tab \"{LokiPoe.InGameState.StashUi.TabControl.CurrentTabName}\" is RemoveOnly and is not gonna be cached");
                return false;
            }
            //Civination Cards
            if (LokiPoe.InGameState.StashUi.StashTabInfo.IsPremiumDivination)
            {
                CommunityLib.Log.Info($"[CommunityLib][UpdateItemsInStash] The tab \"{LokiPoe.InGameState.StashUi.TabControl.CurrentTabName}\" is Divination cards Tab and is not gonna be cached");
                return false;
            }
            
            // Stash should be open, processing cached data in this tab
            // First remove every item in that one
            CachedItemsInStash.RemoveAll(i => i.TabName.Equals(tabName));

            if (LokiPoe.InGameState.StashUi.StashTabInfo.IsPremiumEssence)
            {
                foreach (var wrapper in LokiPoe.InGameState.StashUi.EssenceTab.All
                    .Where(wrp => wrp.CustomTabItem != null))
                    CachedItemsInStash.Add(
                        new CachedItemObject(wrapper, wrapper.CustomTabItem,
                            LokiPoe.InGameState.StashUi.TabControl.CurrentTabName)
                        );
            }
            else if (LokiPoe.InGameState.StashUi.StashTabInfo.IsPremiumCurrency)
            {
                foreach (var wrapper in LokiPoe.InGameState.StashUi.CurrencyTab.All.Where(wrp => wrp.CustomTabItem != null))
                    CachedItemsInStash.Add(new CachedItemObject(wrapper, wrapper.CustomTabItem, LokiPoe.InGameState.StashUi.TabControl.CurrentTabName));
            }
            else
            {
                foreach (var item in LokiPoe.InGameState.StashUi.InventoryControl.Inventory.Items)
                    CachedItemsInStash.Add(new CachedItemObject(LokiPoe.InGameState.StashUi.InventoryControl, item,LokiPoe.InGameState.StashUi.TabControl.CurrentTabName));
            }

            //Await here is not wanted if we'll want to make fast re-caching the updated stash
            //It's only re-caching, not item usage so no need to use it.
            //await Coroutines.LatencyWait(2);
            return true;
        }
    }
}
