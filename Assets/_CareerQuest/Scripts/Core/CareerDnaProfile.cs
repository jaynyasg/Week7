using System.Collections.Generic;
using System.Linq;

namespace CareerQuest
{
    public class CareerDnaProfile
    {
        private readonly Dictionary<string, int> _traitTotals = new();

        public IReadOnlyDictionary<string, int> TraitTotals => _traitTotals;

        public void Recompute(IEnumerable<MiniGameResult> results)
        {
            _traitTotals.Clear();

            foreach (var trait in CareerConfig.AllTraits)
            {
                _traitTotals[trait] = 0;
            }

            foreach (var result in results.Where(result => result != null))
            {
                foreach (var delta in result.TraitDeltas)
                {
                    if (!_traitTotals.ContainsKey(delta.Trait))
                    {
                        _traitTotals[delta.Trait] = 0;
                    }

                    _traitTotals[delta.Trait] += delta.Delta;
                }
            }
        }

        public int GetTraitTotal(string trait)
        {
            return _traitTotals.TryGetValue(trait, out var total) ? total : 0;
        }

        public IReadOnlyList<TraitDelta> TopTraits(int count)
        {
            return _traitTotals
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key)
                .Take(count)
                .Select(pair => new TraitDelta(pair.Key, pair.Value))
                .ToList();
        }
    }
}
