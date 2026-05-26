using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Quests.Careers
{
    public class WaywatcherQuest : QuestBase
    {
        private const int RequiredBowSkill = 100;
        private const int RequiredAthleticsSkill = 100;
        private const int RequiredForestHarmony = 500;
        private const int RequiredOakUpgrades = 3;

        [SaveableField(1)]
        private JournalLog _taskBowSkill = null;
        [SaveableField(2)]
        private JournalLog _taskAthleticsSkill = null;
        [SaveableField(8)]
        private JournalLog _taskForestHarmony = null;
        [SaveableField(9)]
        private JournalLog _taskOakUpgrades = null;

        [SaveableField(4)]
        private int _currentBowSkillLevel = 0;
        [SaveableField(5)]
        private int _currentAthleticsSkillLevel = 0;
        [SaveableField(7)]
        private bool _readyToComplete = false;

        public WaywatcherQuest(string questId, Hero questGiver, CampaignTime duration, int rewardGold) : base(questId, questGiver, duration, rewardGold)
        {
            InitializeQuest();
        }

        private void InitializeQuest()
        {
            _currentBowSkillLevel = Hero.MainHero?.GetSkillValue(DefaultSkills.Bow) ?? 0;
            _currentAthleticsSkillLevel = Hero.MainHero?.GetSkillValue(DefaultSkills.Athletics) ?? 0;

            _taskBowSkill = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_waywatcher_quest_log_bow", "Reach {REQUIRED} in the Bow skill")
                    .SetTextVariable("REQUIRED", RequiredBowSkill),
                TORTextHelper.GetTextObject("tor_waywatcher_quest_task_bow", "Bow Skill"),
                _currentBowSkillLevel,
                RequiredBowSkill);

            _taskAthleticsSkill = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_waywatcher_quest_log_athletics", "Reach {REQUIRED} in the Athletics skill")
                    .SetTextVariable("REQUIRED", RequiredAthleticsSkill),
                TORTextHelper.GetTextObject("tor_waywatcher_quest_task_athletics", "Athletics Skill"),
                _currentAthleticsSkillLevel,
                RequiredAthleticsSkill);

            var currentHarmony = (int)Hero.MainHero.GetCustomResourceValue("ForestHarmony");
            _taskForestHarmony = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_waywatcher_quest_log_harmony", "Reach {REQUIRED} Forest Harmony")
                    .SetTextVariable("REQUIRED", RequiredForestHarmony),
                TORTextHelper.GetTextObject("tor_waywatcher_quest_task_harmony", "Forest Harmony"),
                currentHarmony,
                RequiredForestHarmony);

            var currentOakUpgrades = GetOakUpgradeCount();
            _taskOakUpgrades = AddDiscreteLog(
                TORTextHelper.GetTextObject("tor_waywatcher_quest_log_oak", "Purchase {REQUIRED} upgrades from the Oak of Ages")
                    .SetTextVariable("REQUIRED", RequiredOakUpgrades),
                TORTextHelper.GetTextObject("tor_waywatcher_quest_task_oak", "Oak of Ages Upgrades"),
                currentOakUpgrades,
                RequiredOakUpgrades);
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();

            CampaignEvents.HeroGainedSkill.AddNonSerializedListener(this, OnSkillIncreased);
        }

        private void OnSkillIncreased(Hero hero, SkillObject skill, int skillValueBefore, bool arg4)
        {
            if (hero != Hero.MainHero) return;

            if (skill == DefaultSkills.Bow)
            {
                _currentBowSkillLevel = Hero.MainHero.GetSkillValue(DefaultSkills.Bow);
                _taskBowSkill.UpdateCurrentProgress(_currentBowSkillLevel);
                UpdateQuest();
            }
            else if (skill == DefaultSkills.Athletics)
            {
                _currentAthleticsSkillLevel = Hero.MainHero.GetSkillValue(DefaultSkills.Athletics);
                _taskAthleticsSkill.UpdateCurrentProgress(_currentAthleticsSkillLevel);
                UpdateQuest();
            }
        }

        public override string SpecialQuestType => "WaywatcherQuest";

        private void UpdateQuest()
        {
            var currentHarmony = (int)Hero.MainHero.GetCustomResourceValue("ForestHarmony");
            _taskForestHarmony.UpdateCurrentProgress(currentHarmony);

            _taskOakUpgrades.UpdateCurrentProgress(GetOakUpgradeCount());

            if (AreAllTasksFinished() && !_readyToComplete)
            {
                _readyToComplete = true;
            }
        }

        private static int GetOakUpgradeCount()
        {
            var behavior = Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>();
            if (behavior == null) return 0;
            return behavior.GetUnlockedOakUpgradeCategory("WEPartySizeUpgrade").Count
                 + behavior.GetUnlockedOakUpgradeCategory("WEHealthUpgrade").Count
                 + behavior.GetUnlockedOakUpgradeCategory("WEGainUpgrade").Count;
        }

        private bool AreAllTasksFinished()
        {
            return JournalEntries.All(entry => entry.HasBeenCompleted());
        }

        protected override void OnCompleteWithSuccess()
        {
            Hero.MainHero.AddAttribute("WaywatcherQuestComplete");
            Hero.MainHero.HeroDeveloper.AddAttribute(DefaultCharacterAttributes.Control, 1, false);
            var bow = MBObjectManager.Instance.GetObject<ItemObject>("tor_we_weapon_bow_legendary");
            if (bow != null)
                MobileParty.MainParty.ItemRoster.Add(new ItemRosterElement(bow, 1));
        }

        protected override void SetDialogs() { }

        protected override void InitializeQuestOnGameLoad() { }

        protected override void HourlyTick()
        {
            if (_readyToComplete)
            {
                CompleteQuestWithSuccess();
            }
            else
            {
                UpdateQuest();
            }
        }

        public override TextObject Title => TORTextHelper.GetTextObject("tor_waywatcher_quest_title", "The Path of the Waywatcher");

        public override bool IsRemainingTimeHidden => true;

    }
}
