using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimTalkDistanceControl
{
    // ============================================================
    // Cached reflection lookups — initialized once at startup
    // ============================================================
    public static class ReflectionCache
    {
        // RimTalk.Data.Cache
        public static readonly Type CacheType;
        public static readonly PropertyInfo KeysProperty;
        public static readonly MethodInfo GetMethod;
        public static readonly MethodInfo GetPlayerMethod;

        // RimTalk.Util.PawnUtil
        public static readonly Type PawnUtilType;
        public static readonly MethodInfo IsTalkEligibleMethod;
        public static readonly MethodInfo HasVocalLinkMethod;

        // Cached player pawn reference (doesn't change during game)
        private static Pawn _playerPawn;
        private static bool _playerPawnResolved;

        // RimTalk PawnState.CanGenerateTalk — lazy init (needs runtime type)
        private static MethodInfo _canGenerateTalkMethod;
        private static bool _canGenerateTalkResolved;
        private static bool _canGenerateTalkWarned;

        // RimTalk Settings reflection (for MaxPawnContextCount)
        private static readonly Type SettingsType;
        private static readonly MethodInfo SettingsGetMethod;
        private static readonly FieldInfo ContextField;
        private static readonly FieldInfo MaxPawnCountField;

        /// <summary>
        /// Get RimTalk's MaxPawnContextCount via reflection. Falls back to 10.
        /// RimTalkSettings.Context and ContextSettings.MaxPawnContextCount are public fields.
        /// </summary>
        public static int MaxPawnContextCount
        {
            get
            {
                if (SettingsGetMethod == null || ContextField == null || MaxPawnCountField == null) return 10;
                try
                {
                    var settings = SettingsGetMethod.Invoke(null, null);
                    var ctx = ContextField.GetValue(settings);
                    if (ctx == null) return 10;
                    var val = MaxPawnCountField.GetValue(ctx);
                    return val is int i && i > 0 ? i : 10;
                }
                catch { return 10; }
            }
        }

        /// <summary>
        /// Get the invisible player pawn. Cached after first successful resolution.
        /// </summary>
        public static Pawn PlayerPawn
        {
            get
            {
                if (!_playerPawnResolved && GetPlayerMethod != null)
                {
                    _playerPawn = GetPlayerMethod.Invoke(null, null) as Pawn;
                    _playerPawnResolved = (_playerPawn != null);
                }
                return _playerPawn;
            }
        }

        /// <summary>
        /// Get CanGenerateTalk method info. Retries until successfully resolved.
        /// Logs warning only once to avoid log spam.
        /// </summary>
        public static MethodInfo CanGenerateTalkMethod
        {
            get
            {
                if (!_canGenerateTalkResolved)
                {
                    try
                    {
                        var samplePawn = KeysProperty?.GetValue(null) as IEnumerable<Pawn>;
                        if (samplePawn != null)
                        {
                            foreach (var p in samplePawn)
                            {
                                _invokeArgs[0] = p;
                                var state = GetMethod?.Invoke(null, _invokeArgs);
                                if (state != null)
                                {
                                    _canGenerateTalkMethod = AccessTools.Method(state.GetType(), "CanGenerateTalk");
                                    if (_canGenerateTalkMethod != null)
                                    {
                                        _canGenerateTalkResolved = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!_canGenerateTalkWarned)
                        {
                            Log.Warning($"[RimTalk Distance Control] Failed to resolve CanGenerateTalk: {ex.Message}");
                            _canGenerateTalkWarned = true;
                        }
                    }

                    // Log warning only once if resolution fails
                    if (!_canGenerateTalkResolved && _canGenerateTalkMethod == null && !_canGenerateTalkWarned)
                    {
                        Log.Warning("[RimTalk Distance Control] CanGenerateTalk method not found. " +
                            "This may resolve once pawns are loaded. Will retry on next access.");
                        _canGenerateTalkWarned = true;
                    }
                }
                return _canGenerateTalkMethod;
            }
        }

        // Shared reusable array for single-arg reflection invocations
        private static readonly object[] _invokeArgs = new object[1];

        /// <summary>
        /// Thread-safe helper for single-arg MethodInfo.Invoke using shared array.
        /// Only safe on main thread (which is where all RimWorld game logic runs).
        /// </summary>
        public static object InvokeSingleArg(MethodInfo method, object arg)
        {
            _invokeArgs[0] = arg;
            return method?.Invoke(null, _invokeArgs);
        }

        /// <summary>
        /// Debug-only log: only outputs when DevMode is enabled.
        /// Used for high-frequency runtime checks to avoid log spam for normal players.
        /// </summary>
        public static void DebugLog(string message)
        {
            if (Prefs.DevMode)
                Log.Message($"[RimTalk Distance Control] {message}");
        }

        static ReflectionCache()
        {
            CacheType = AccessTools.TypeByName("RimTalk.Data.Cache");
            if (CacheType != null)
            {
                KeysProperty = AccessTools.Property(CacheType, "Keys");
                GetMethod = AccessTools.Method(CacheType, "Get", new[] { typeof(Pawn) });
                GetPlayerMethod = AccessTools.Method(CacheType, "GetPlayer");
            }

            PawnUtilType = AccessTools.TypeByName("RimTalk.Util.PawnUtil");
            if (PawnUtilType != null)
            {
                IsTalkEligibleMethod = AccessTools.Method(PawnUtilType, "IsTalkEligible", new[] { typeof(Pawn) });
                HasVocalLinkMethod = AccessTools.Method(PawnUtilType, "HasVocalLink", new[] { typeof(Pawn) });
            }

            SettingsType = AccessTools.TypeByName("RimTalk.Settings");
            if (SettingsType != null)
            {
                SettingsGetMethod = AccessTools.Method(SettingsType, "Get");
                if (SettingsGetMethod != null)
                {
                    try
                    {
                        var settings = SettingsGetMethod.Invoke(null, null);
                        if (settings != null)
                        {
                            // Context is a public FIELD on RimTalkSettings (not a property)
                            ContextField = AccessTools.Field(settings.GetType(), "Context");
                            if (ContextField != null)
                            {
                                // MaxPawnContextCount is a public FIELD on ContextSettings
                                MaxPawnCountField = AccessTools.Field(ContextField.FieldType, "MaxPawnContextCount");
                            }
                        }
                    }
                    catch { }
                }
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            // Force ReflectionCache initialization before patching
            _ = ReflectionCache.CacheType;

            var harmony = new Harmony("youyu.rimtalk.distancecontrol");

            int total = 0, success = 0;
            success += TryPatch(harmony, "CustomDialogueService.CanTalk", typeof(Patch_CustomDialogueService_CanTalk)) ? 1 : 0; total++;
            success += TryPatch(harmony, "PawnSelector.GetNearbyPawnsInternal", typeof(Patch_PawnSelector_GetNearbyPawnsInternal)) ? 1 : 0; total++;
            success += TryPatch(harmony, "ContextHelper.CollectNearbyContext", typeof(Patch_ContextHelper_CollectNearbyContext)) ? 1 : 0; total++;
            success += TryPatch(harmony, "MemoryThoughtHandler.TryGainMemory", typeof(Patch_MemoryThoughtHandler_TryGainMemory)) ? 1 : 0; total++;

            if (success == total)
                Log.Message($"[RimTalk Distance Control] All {total} Harmony patches applied successfully.");
            else
                Log.Warning($"[RimTalk Distance Control] {success}/{total} Harmony patches applied. Some features may not work due to RimTalk version compatibility.");
        }

        private static bool TryPatch(Harmony harmony, string name, Type patchType)
        {
            try
            {
                new PatchClassProcessor(harmony, patchType).Patch();
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimTalk Distance Control] Failed to patch {name}: {ex.Message}");
                return false;
            }
        }
    }

    // ============================================================
    // 1. Patch CustomDialogueService.CanTalk
    //    Replace distance + same-room check with configurable values
    // ============================================================
    [HarmonyPatch]
    public static class Patch_CustomDialogueService_CanTalk
    {
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("RimTalk.Service.CustomDialogueService");
            if (type == null)
            {
                Log.Warning("[RimTalk Distance Control] Type not found: RimTalk.Service.CustomDialogueService");
                return null;
            }
            return AccessTools.Method(type, "CanTalk", new[] { typeof(Pawn), typeof(Pawn) });
        }

        static bool Prefix(Pawn initiator, Pawn recipient, ref bool __result)
        {
            try
            {
                // Null guard to prevent NRE and null==null false positive
                if (initiator == null || recipient == null)
                {
                    ReflectionCache.DebugLog($"CanTalk denied: null argument. initiator={initiator}, recipient={recipient}");
                    __result = false;
                    return false;
                }

                var settings = DistanceControlMod.Settings;

                // Player pawn (invisible selector) talking to a pawn is always allowed
                var playerPawn = ReflectionCache.PlayerPawn;
                bool isPlayerPawn = (playerPawn != null && initiator == playerPawn)
                                    || !initiator.Spawned;
                if (isPlayerPawn)
                {
                    __result = true;
                    return false;
                }

                float distance = initiator.Position.DistanceTo(recipient.Position);

                // Check distance (0 = unlimited)
                if (settings.TalkDistance > 0 && distance > settings.TalkDistance)
                {
                    ReflectionCache.DebugLog($"CanTalk denied: distance {distance:F1} exceeds TalkDistance {settings.TalkDistance}. " +
                        $"initiator={initiator.LabelShort}({initiator.Position}), recipient={recipient.LabelShort}({recipient.Position})");
                    __result = false;
                    return false;
                }

                // Check same-room requirement
                if (settings.RequireSameRoom)
                {
                    var room1 = initiator.GetRoom();
                    var room2 = recipient.GetRoom();
                    bool sameRoom = (room1 != null && room2 != null && room1 == room2) ||
                                    (room1 == null && room2 == null);
                    if (!sameRoom)
                    {
                        string room1Desc = room1 != null ? $"indoors(psychOutdoors={room1.PsychologicallyOutdoors})" : "outdoors";
                        string room2Desc = room2 != null ? $"indoors(psychOutdoors={room2.PsychologicallyOutdoors})" : "outdoors";
                        ReflectionCache.DebugLog($"CanTalk denied: not in same room. " +
                            $"initiator={initiator.LabelShort}({room1Desc}), recipient={recipient.LabelShort}({room2Desc})");
                        __result = false;
                        return false;
                    }
                }

                __result = true;
                return false;
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimTalk Distance Control] CanTalk error: {ex.Message}");
                return true; // 回退到原始方法
            }
        }
    }

    // ============================================================
    // 2. Patch PawnSelector.GetNearbyPawnsInternal
    //    Replace HearingRange/ViewingRange constants and room check
    //    Uses manual loop to avoid LINQ GC allocations in hot path
    // ============================================================
    [HarmonyPatch]
    public static class Patch_PawnSelector_GetNearbyPawnsInternal
    {
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("RimTalk.Service.PawnSelector");
            if (type == null)
            {
                Log.Warning("[RimTalk Distance Control] Type not found: RimTalk.Service.PawnSelector");
                return null;
            }
            var detectionType = AccessTools.Inner(type, "DetectionType");
            return AccessTools.Method(type, "GetNearbyPawnsInternal",
                new[] { typeof(Pawn), typeof(Pawn), detectionType, typeof(bool), typeof(bool) });
        }

        static bool Prefix(Pawn pawn1, Pawn pawn2, object detectionType, bool onlyTalkable, bool isAnnouncement,
            ref List<Pawn> __result)
        {
            try
            {
                int maxResults = Math.Max(10, ReflectionCache.MaxPawnContextCount);
                var settings = DistanceControlMod.Settings;

                // Determine which range to use
                bool isHearing = detectionType.ToString() == "Hearing";
                float baseRange = isHearing
                    ? (isAnnouncement ? settings.AnnouncementHearingRange : settings.HearingRange)
                    : settings.ViewingRange;
                var capacityDef = isHearing ? PawnCapacityDefOf.Hearing : PawnCapacityDefOf.Sight;

                // Access Cache.Keys via cached reflection
                var allPawns = ReflectionCache.KeysProperty?.GetValue(null) as IEnumerable<Pawn>;
                if (allPawns == null)
                {
                    __result = new List<Pawn>();
                    return false;
                }

                // Cache CanGenerateTalk method once (outside the loop)
                var canGenMethod = onlyTalkable ? ReflectionCache.CanGenerateTalkMethod : null;

                // Cache rooms once (outside the loop)
                var pawn1Room = settings.RequireSameRoom ? pawn1.GetRoom() : null;
                var pawn2Room = (settings.RequireSameRoom && pawn2 != null) ? pawn2.GetRoom() : null;

                // Use manual loop + pre-allocated list to avoid LINQ GC pressure
                var candidates = new List<Pawn>(64);

                foreach (var p in allPawns)
                {
                    if (p == pawn1 || p == pawn2) continue;

                    // Check talkable
                    if (onlyTalkable)
                    {
                        var pawnState = ReflectionCache.InvokeSingleArg(ReflectionCache.GetMethod, p);
                        if (pawnState == null) continue;
                        if (canGenMethod == null || !(bool)canGenMethod.Invoke(pawnState, null)) continue;
                    }

                    // Check capacity
                    float capacityLevel = p.health.capacities.GetLevel(capacityDef);
                    if (capacityLevel <= 0f) continue;

                    // Check distance and room
                    float detectionDistance = baseRange * capacityLevel;
                    var pRoom = settings.RequireSameRoom ? p.GetRoom() : null;

                    bool nearPawn1;
                    if (settings.RequireSameRoom)
                        nearPawn1 = pRoom == pawn1Room && p.Position.InHorDistOf(pawn1.Position, detectionDistance);
                    else
                        nearPawn1 = p.Position.InHorDistOf(pawn1.Position, detectionDistance);

                    bool nearPawn2;
                    if (pawn2 != null)
                    {
                        if (settings.RequireSameRoom)
                            nearPawn2 = pRoom == pawn2Room && p.Position.InHorDistOf(pawn2.Position, detectionDistance);
                        else
                            nearPawn2 = p.Position.InHorDistOf(pawn2.Position, detectionDistance);
                    }
                    else
                    {
                        nearPawn2 = false;
                    }

                    if (!nearPawn1 && !nearPawn2) continue;

                    candidates.Add(p);
                }

                // Sort by distance and take top N
                candidates.Sort((a, b) =>
                {
                    float distA = pawn2 == null
                        ? pawn1.Position.DistanceTo(a.Position)
                        : Math.Min(pawn1.Position.DistanceTo(a.Position), pawn2.Position.DistanceTo(a.Position));
                    float distB = pawn2 == null
                        ? pawn1.Position.DistanceTo(b.Position)
                        : Math.Min(pawn1.Position.DistanceTo(b.Position), pawn2.Position.DistanceTo(b.Position));
                    return distA.CompareTo(distB);
                });

                if (candidates.Count > maxResults)
                    candidates.RemoveRange(maxResults, candidates.Count - maxResults);

                __result = candidates;
                return false;
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimTalk Distance Control] GetNearbyPawnsInternal error: {ex.Message}\n{ex.StackTrace}");
                return true; // 回退到原始方法，不阻断游戏
            }
        }
    }

    // ============================================================
    // 3. Patch ContextHelper.CollectNearbyContext
    //    Override distance parameter
    // ============================================================
    [HarmonyPatch]
    public static class Patch_ContextHelper_CollectNearbyContext
    {
        static MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("RimTalk.Util.ContextHelper");
            if (type == null)
            {
                Log.Warning("[RimTalk Distance Control] Type not found: RimTalk.Util.ContextHelper");
                return null;
            }
            var method = AccessTools.Method(type, "CollectNearbyContext");
            if (method == null)
            {
                Log.Warning("[RimTalk Distance Control] Method not found: CollectNearbyContext");
                return null;
            }
            return method;
        }

        static void Prefix(ref int distance)
        {
            distance = DistanceControlMod.Settings.ContextDistance;
        }
    }


    // ============================================================
    // 4. Patch MemoryThoughtHandler.TryGainMemory
    //    Block RimTalk_Slighted debuff when setting is enabled
    // ============================================================
    [HarmonyPatch(typeof(MemoryThoughtHandler), nameof(MemoryThoughtHandler.TryGainMemory))]
    [HarmonyPatch(new[] { typeof(Thought_Memory), typeof(Pawn) })]
    public static class Patch_MemoryThoughtHandler_TryGainMemory
    {
        static bool Prefix(Thought_Memory newThought)
        {
            if (!DistanceControlMod.Settings.BlockSlightedDebuff)
                return true;

            if (newThought?.def?.defName == "RimTalk_Slighted")
                return false;

            return true;
        }
    }

}
