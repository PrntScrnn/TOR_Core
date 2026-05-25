using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Ink;
using TOR_Core.Quests;

namespace TOR_Core.CampaignMechanics.CustomEvents
{
    /// <summary>
    /// Campaign behavior that launches career-specific ink stories at the start of the game
    /// after character creation is complete.
    /// </summary>
    public class SimpleCareerQuestBehavior : CampaignBehaviorBase
    {
        private readonly Dictionary<string, string> _careerStories = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _careerQuestIds = new Dictionary<string, string>();

        private bool _hasShownCareerStory = false;

        public override void RegisterEvents()
        {
            if (!_hasShownCareerStory)
            {
                CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
            }

            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
        }

        private void OnSessionStart(CampaignGameStarter starter)
        {
            InitializeCareerStories();
        }

        /// <summary>
        /// Initialize the dictionary of career IDs to story paths
        /// </summary>
        private void InitializeCareerStories()
        {
            _careerStories.Clear();

            // OrcBoss career has an initial story that launches immediately
            _careerStories.Add("OrcBoss", "OrcBossQuest1");

            // OrcShaman career requires praying at a shrine first
            // The initial story "OrcShamanPrayerPrompt" is handled separately
            _careerStories.Add("OrcShaman", "OrcShamanPrayerPrompt");

            _careerQuestIds.Clear();
            _careerQuestIds.Add("Waywatcher", "Quests.Careers.WaywatcherQuest");
        }

        private void HourlyTick()
        {
            var playerCareer = Hero.MainHero.GetCareer();
            if (playerCareer == null) return;

            // Direct quest starts — idempotent, safe to call every tick
            if (_careerQuestIds.TryGetValue(playerCareer.StringId, out string questPath))
            {
                TORQuestHelper.StartCareerQuest(questPath);
            }

            // Ink story launches (once only, requires player to be on the move)
            if (!_hasShownCareerStory && _careerStories.ContainsKey(playerCareer.StringId))
            {
                if (!StandardMovingCheck())
                    return;

                if (TryLaunchCareerStory())
                    _hasShownCareerStory = true;
            }
        }

        private bool StandardMovingCheck()
        {
            return MobileParty.MainParty.IsMoving &&
                   MobileParty.MainParty.Army == null &&
                   !Hero.MainHero.IsPrisoner &&
                   MobileParty.MainParty.CurrentSettlement == null &&
                   MobileParty.MainParty.BesiegedSettlement == null;
        }

        private bool TryLaunchCareerStory()
        {
            if (Hero.MainHero == null)
                return false;

            var playerCareer = Hero.MainHero.GetCareer();
            if (playerCareer == null)
                return false;

            // Check if this career has an associated story
            if (_careerStories.TryGetValue(playerCareer.StringId, out string storyId))
            {
                InkStoryManager.OpenStory(storyId);
                return true;
            }

            return false;
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_hasShownCareerStory", ref _hasShownCareerStory);
        }
    }
}