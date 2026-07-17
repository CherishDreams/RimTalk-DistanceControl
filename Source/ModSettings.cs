using Verse;

namespace RimTalkDistanceControl
{
    /// <summary>
    /// Default values shared between ModSettings and UI.
    /// Single source of truth to prevent desync.
    /// </summary>
    public static class DefaultValues
    {
        public const float TalkDistance = 20f;
        public const bool RequireSameRoom = true;
        public const float HearingRange = 10f;
        public const float ViewingRange = 20f;
        public const int ContextDistance = 5;
        public const int NearbyCellsDistance = 5;
        public const bool BlockSlightedDebuff = false;
    }

    public class DistanceControlSettings : ModSettings
    {
        /// <summary>
        /// 对话最大距离（0 表示无限制）
        /// </summary>
        public float TalkDistance = DefaultValues.TalkDistance;

        /// <summary>
        /// 是否要求同房间才能对话
        /// </summary>
        public bool RequireSameRoom = DefaultValues.RequireSameRoom;

        /// <summary>
        /// 听觉检测范围
        /// </summary>
        public float HearingRange = DefaultValues.HearingRange;

        /// <summary>
        /// 视觉检测范围
        /// </summary>
        public float ViewingRange = DefaultValues.ViewingRange;

        /// <summary>
        /// 环境上下文采集距离
        /// </summary>
        public int ContextDistance = DefaultValues.ContextDistance;

        /// <summary>
        /// 附近格子采集距离（用于美容等上下文）
        /// </summary>
        public int NearbyCellsDistance = DefaultValues.NearbyCellsDistance;

        /// <summary>
        /// 是否拦截 RimTalk 的"被忽视"(Slighted) debuff 施加
        /// </summary>
        public bool BlockSlightedDebuff = DefaultValues.BlockSlightedDebuff;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref TalkDistance, "talkDistance", DefaultValues.TalkDistance);
            Scribe_Values.Look(ref RequireSameRoom, "requireSameRoom", DefaultValues.RequireSameRoom);
            Scribe_Values.Look(ref HearingRange, "hearingRange", DefaultValues.HearingRange);
            Scribe_Values.Look(ref ViewingRange, "viewingRange", DefaultValues.ViewingRange);
            Scribe_Values.Look(ref ContextDistance, "contextDistance", DefaultValues.ContextDistance);
            Scribe_Values.Look(ref NearbyCellsDistance, "nearbyCellsDistance", DefaultValues.NearbyCellsDistance);
            Scribe_Values.Look(ref BlockSlightedDebuff, "blockSlightedDebuff", DefaultValues.BlockSlightedDebuff);
        }
    }
}
