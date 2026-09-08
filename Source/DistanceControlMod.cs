using UnityEngine;
using Verse;

namespace RimTalkDistanceControl
{
    public class DistanceControlMod : Mod
    {
        public static DistanceControlSettings Settings;
        private Vector2 _scrollPosition = Vector2.zero;

        // Text buffer strings for input fields
        private string _talkDistBuf = "";
        private string _hearingRangeBuf = "";
        private string _viewingRangeBuf = "";
        private string _announceHearingBuf = "";
        private string _contextDistBuf = "";

        public DistanceControlMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<DistanceControlSettings>();
            _talkDistBuf = Settings.TalkDistance.ToString("F0");
            _hearingRangeBuf = Settings.HearingRange.ToString("F0");
            _viewingRangeBuf = Settings.ViewingRange.ToString("F0");
            _announceHearingBuf = Settings.AnnouncementHearingRange.ToString("F0");
            _contextDistBuf = Settings.ContextDistance.ToString();
        }

        public override string SettingsCategory()
        {
            return "RimTalkDC.Category.Title".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // Calculate content height via estimation (avoids double-draw of interactive widgets)
            float contentHeight = CalculateContentHeight();

            // Draw with scroll view
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, contentHeight);
            _scrollPosition = GUI.BeginScrollView(inRect, _scrollPosition, viewRect);

            Text.Font = GameFont.Small;
            var listing = new Listing_Standard();
            listing.Begin(viewRect);
            DrawSettingsContent(listing);
            listing.End();

            GUI.EndScrollView();
        }

        /// <summary>
        /// Estimate content height without drawing interactive widgets twice.
        /// New layout: Row1 (label+reset) + Row2 (description) + Row3 (slider+input for numeric)
        /// </summary>
        private float CalculateContentHeight()
        {
            float h = 0f;
            float lh = Text.LineHeight;

            // 5 numeric settings: (label row + description row + slider row) * 5 + gap
            h += (lh + lh + lh + 4f + 16f) * 5f;    // 5 Float/IntSettings: Label + Desc + Slider + Gap(16)
            // 2 bool settings: (label+checkbox row + description row) + gap
            h += (lh + lh + 16f) * 2f;                   // 2 BoolSetting: Label+Checkbox + Desc + Gap(16)
            h += 15f;                              // Gap(15) before button
            h += lh + 10f;                         // Reset button
            h += 20f;                              // Bottom margin

            return h;
        }

        private void DrawSettingsContent(Listing_Standard listing)
        {
            // Talk Distance (float, 0-100)
            DrawFloatSetting(listing,
                "RimTalkDC.Label.TalkDistance".Translate(),
                "RimTalkDC.Desc.TalkDistance".Translate(),
                ref Settings.TalkDistance, ref _talkDistBuf, DefaultValues.TalkDistance, 0f, 100f);
            listing.Gap(16f);

            // Require Same Room (bool)
            DrawBoolSetting(listing,
                "RimTalkDC.Label.RequireSameRoom".Translate(),
                "RimTalkDC.Desc.RequireSameRoom".Translate(),
                ref Settings.RequireSameRoom, DefaultValues.RequireSameRoom);
            listing.Gap(16f);

            // Hearing Range (float, 1-50)
            DrawFloatSetting(listing,
                "RimTalkDC.Label.HearingRange".Translate(),
                "RimTalkDC.Desc.HearingRange".Translate(),
                ref Settings.HearingRange, ref _hearingRangeBuf, DefaultValues.HearingRange, 1f, 50f);
            listing.Gap(16f);

            // Viewing Range (float, 1-50)
            DrawFloatSetting(listing,
                "RimTalkDC.Label.ViewingRange".Translate(),
                "RimTalkDC.Desc.ViewingRange".Translate(),
                ref Settings.ViewingRange, ref _viewingRangeBuf, DefaultValues.ViewingRange, 1f, 50f);
            listing.Gap(16f);

            // Announcement Hearing Range (float, 1-100)
            DrawFloatSetting(listing,
                "RimTalkDC.Label.AnnouncementHearingRange".Translate(),
                "RimTalkDC.Desc.AnnouncementHearingRange".Translate(),
                ref Settings.AnnouncementHearingRange, ref _announceHearingBuf, DefaultValues.AnnouncementHearingRange, 1f, 100f);
            listing.Gap(16f);

            // Context Distance (int, 1-20)
            DrawIntSetting(listing,
                "RimTalkDC.Label.ContextDistance".Translate(),
                "RimTalkDC.Desc.ContextDistance".Translate(),
                ref Settings.ContextDistance, ref _contextDistBuf, DefaultValues.ContextDistance, 1, 20);
            listing.Gap(16f);

            // Block Slighted Debuff (bool)
            DrawBoolSetting(listing,
                "RimTalkDC.Label.BlockSlightedDebuff".Translate(),
                "RimTalkDC.Desc.BlockSlightedDebuff".Translate(),
                ref Settings.BlockSlightedDebuff, DefaultValues.BlockSlightedDebuff);
            listing.Gap(16f);

            listing.Gap(15f);

            // Reset All button
            if (listing.ButtonText("RimTalkDC.Button.ResetAll".Translate()))
            {
                Settings.TalkDistance = DefaultValues.TalkDistance;
                Settings.RequireSameRoom = DefaultValues.RequireSameRoom;
                Settings.HearingRange = DefaultValues.HearingRange;
                Settings.ViewingRange = DefaultValues.ViewingRange;
                Settings.AnnouncementHearingRange = DefaultValues.AnnouncementHearingRange;
                Settings.ContextDistance = DefaultValues.ContextDistance;
                Settings.BlockSlightedDebuff = DefaultValues.BlockSlightedDebuff;
                SyncBuffers();
            }
        }

        private void SyncBuffers()
        {
            _talkDistBuf = Settings.TalkDistance.ToString("F0");
            _hearingRangeBuf = Settings.HearingRange.ToString("F0");
            _viewingRangeBuf = Settings.ViewingRange.ToString("F0");
            _announceHearingBuf = Settings.AnnouncementHearingRange.ToString("F0");
            _contextDistBuf = Settings.ContextDistance.ToString();
        }

        /// <summary>
        /// Draw a float setting with layout:
        /// Row 1: Label
        /// Row 2: Description (gray, small)
        /// Row 3: [Slider][Value][Reset]
        /// </summary>
        private void DrawFloatSetting(Listing_Standard listing, string label, string hint,
            ref float value, ref string buffer, float defaultValue, float min, float max)
        {
            // Row 1: Label
            Rect row1 = listing.GetRect(Text.LineHeight);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(row1, label);
            Text.Anchor = TextAnchor.UpperLeft;

            // Row 2: Description
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            listing.Label(hint);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // Row 3: Slider + Value + Reset button
            Rect row3 = listing.GetRect(Text.LineHeight + 4f);
            float resetBtnWidth = 50f;
            float inputWidth = 60f;
            float gap = 6f;
            float sliderWidth = row3.width - inputWidth - resetBtnWidth - gap * 2;

            Rect sliderRect = new Rect(row3.x, row3.y + 2f, sliderWidth, row3.height);
            Rect inputRect = new Rect(row3.x + sliderWidth + gap, row3.y + 2f, inputWidth, row3.height);
            Rect resetBtnRect = new Rect(row3.x + sliderWidth + inputWidth + gap * 2, row3.y + 2f, resetBtnWidth, row3.height);

            float oldValue = value;
            value = GUI.HorizontalSlider(sliderRect, value, min, max);
            value = Mathf.Clamp(value, min, max);
            if (Mathf.Abs(oldValue - value) > 0.01f)
            {
                buffer = value.ToString("F0");
            }

            Widgets.TextFieldNumeric(inputRect, ref value, ref buffer, min, max);

            if (Widgets.ButtonText(resetBtnRect, "RimTalkDC.Button.Reset".Translate()))
            {
                value = defaultValue;
                buffer = value.ToString("F0");
            }
        }

        /// <summary>
        /// Draw an int setting with layout:
        /// Row 1: Label
        /// Row 2: Description (gray, small)
        /// Row 3: [Slider][Value][Reset]
        /// </summary>
        private void DrawIntSetting(Listing_Standard listing, string label, string hint,
            ref int value, ref string buffer, int defaultValue, int min, int max)
        {
            // Row 1: Label
            Rect row1 = listing.GetRect(Text.LineHeight);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(row1, label);
            Text.Anchor = TextAnchor.UpperLeft;

            // Row 2: Description
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            listing.Label(hint);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // Row 3: Slider + Value + Reset button
            Rect row3 = listing.GetRect(Text.LineHeight + 4f);
            float resetBtnWidth = 50f;
            float inputWidth = 60f;
            float gap = 6f;
            float sliderWidth = row3.width - inputWidth - resetBtnWidth - gap * 2;

            Rect sliderRect = new Rect(row3.x, row3.y + 2f, sliderWidth, row3.height);
            Rect inputRect = new Rect(row3.x + sliderWidth + gap, row3.y + 2f, inputWidth, row3.height);
            Rect resetBtnRect = new Rect(row3.x + sliderWidth + inputWidth + gap * 2, row3.y + 2f, resetBtnWidth, row3.height);

            int oldValue = value;
            float fValue = value;
            fValue = GUI.HorizontalSlider(sliderRect, fValue, min, max);
            value = Mathf.Clamp(Mathf.RoundToInt(fValue), min, max);
            if (oldValue != value)
            {
                buffer = value.ToString();
            }

            Widgets.TextFieldNumeric(inputRect, ref value, ref buffer, min, max);

            if (Widgets.ButtonText(resetBtnRect, "RimTalkDC.Button.Reset".Translate()))
            {
                value = defaultValue;
                buffer = value.ToString();
            }
        }

        /// <summary>
        /// Draw a bool setting with top-bottom layout:
        /// Row 1: Label (left) + Checkbox (middle) + Reset button (right)
        /// Row 2: Description (gray, small)
        /// </summary>
        private void DrawBoolSetting(Listing_Standard listing, string label, string hint,
            ref bool value, bool defaultValue)
        {
            // Row 1: Label + Checkbox + Reset button
            Rect row1 = listing.GetRect(Text.LineHeight);
            float labelWidth = 150f;
            Rect labelRect = new Rect(row1.x, row1.y, labelWidth, row1.height);
            Rect checkRect = new Rect(row1.x + labelWidth, row1.y, row1.width - labelWidth - 60f, row1.height);
            Rect resetBtnRect = new Rect(row1.xMax - 55f, row1.y, 55f, row1.height);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;

            Widgets.CheckboxLabeled(checkRect, "", ref value);

            if (Widgets.ButtonText(resetBtnRect, "RimTalkDC.Button.Reset".Translate()))
            {
                value = defaultValue;
            }

            // Row 2: Description (gray, small)
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            listing.Label(hint);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }
    }
}
