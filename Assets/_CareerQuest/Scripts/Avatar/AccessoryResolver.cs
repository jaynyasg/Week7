using System.Collections.Generic;
using System.Linq;

namespace CareerQuest
{
    /// <summary>
    /// Pure accessory derivation (U6, R12/KTD8): earned accessories come from
    /// completed activities (best results / compact 2P facts) plus the unique
    /// completion count — never from a saved inventory or wardrobe state, and
    /// the derivation never touches career scoring. Calling it any number of
    /// times with the same inputs yields the same list (idempotent), so host
    /// and clients agree from the same completion facts.
    ///
    /// Earn order matters: completedActivityIds arrive in first-completion
    /// order (GameSession completion order on the host; the replicated
    /// completed-activity fact order on clients), and milestone accessories
    /// slot in right after the completion that crossed their threshold. The
    /// campus clutter rule then keeps the NEWEST earned accessory per slot.
    /// </summary>
    public static class AccessoryResolver
    {
        /// <summary>
        /// Every accessory earned so far, in earn order (station accessories at
        /// their completion position, milestones right after the completion
        /// that crossed their threshold).
        /// </summary>
        public static IReadOnlyList<AccessoryDefinition> ResolveEarned(
            IReadOnlyList<string> completedActivityIdsInOrder,
            int uniqueCompletedGames)
        {
            var earned = new List<AccessoryDefinition>();
            if (completedActivityIdsInOrder != null)
            {
                for (var index = 0; index < completedActivityIdsInOrder.Count; index++)
                {
                    if (AccessoryRewardConfig.TryGetForStation(completedActivityIdsInOrder[index], out var stationAccessory))
                    {
                        earned.Add(stationAccessory);
                    }

                    if (AccessoryRewardConfig.TryGetForMilestone(index + 1, out var milestone)
                        && index + 1 <= uniqueCompletedGames)
                    {
                        earned.Add(milestone);
                    }
                }
            }

            // Network read models may report a unique count beyond the listed
            // ids (legacy snapshot ordering) — milestones still derive from the
            // count alone, never from stored state.
            foreach (var milestone in AccessoryRewardConfig.MilestoneAccessories)
            {
                if (milestone.MilestoneCompletions <= uniqueCompletedGames && !earned.Contains(milestone))
                {
                    earned.Add(milestone);
                }
            }

            return earned;
        }

        /// <summary>Convenience over the session's derived read model (host or client).</summary>
        public static IReadOnlyList<AccessoryDefinition> ResolveEarned(GameSession session)
        {
            if (session == null)
            {
                return new List<AccessoryDefinition>();
            }

            return ResolveEarned(session.CompletedActivityIds, session.UniqueCompletedGames);
        }

        /// <summary>
        /// Visual clutter rule (design doc): at most ONE visible accessory per
        /// slot — the newest earned wins its slot — and ceremony-only items
        /// (star robe, reveal flourish) render only during the reveal
        /// ceremony, never in normal campus play. Result is in stable
        /// <see cref="AccessorySlot"/> order for deterministic rendering.
        /// </summary>
        public static IReadOnlyList<AccessoryDefinition> ResolveVisible(
            IReadOnlyList<AccessoryDefinition> earnedInOrder,
            bool ceremonyContext)
        {
            var bySlot = new Dictionary<AccessorySlot, AccessoryDefinition>();
            if (earnedInOrder != null)
            {
                foreach (var accessory in earnedInOrder)
                {
                    if (accessory == null || (accessory.CeremonyOnly && !ceremonyContext))
                    {
                        continue;
                    }

                    bySlot[accessory.Slot] = accessory; // newest earned wins the slot
                }
            }

            return bySlot.Values
                .OrderBy(accessory => accessory.Slot)
                .ToList();
        }

        /// <summary>The visible set for a session in one call (campus play or reveal).</summary>
        public static IReadOnlyList<AccessoryDefinition> ResolveVisible(GameSession session, bool ceremonyContext)
        {
            return ResolveVisible(ResolveEarned(session), ceremonyContext);
        }

        /// <summary>
        /// The earned accessories de-duplicated by id (U11, Gate B simplify
        /// Finding 2): the gear surfaces (gallery newest-first strip, passport
        /// earn-order grid) all need the distinct earned set in a stable order
        /// without re-walking a hand-rolled dedup loop. <paramref name="newestFirst"/>
        /// true reverses earn order (the gallery's most-recent-gear-first strip);
        /// false keeps earn order (the passport grid). Presentation only (KTD8) —
        /// reads the session read model, never scoring.
        /// </summary>
        public static IReadOnlyList<AccessoryDefinition> DistinctEarned(GameSession session, bool newestFirst)
        {
            var earned = ResolveEarned(session);
            var distinct = new List<AccessoryDefinition>(earned.Count);
            var seen = new HashSet<string>();

            if (newestFirst)
            {
                for (var i = earned.Count - 1; i >= 0; i--)
                {
                    if (seen.Add(earned[i].Id))
                    {
                        distinct.Add(earned[i]);
                    }
                }
            }
            else
            {
                foreach (var accessory in earned)
                {
                    if (seen.Add(accessory.Id))
                    {
                        distinct.Add(accessory);
                    }
                }
            }

            return distinct;
        }
    }
}
