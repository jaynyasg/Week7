using System.Linq;
using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    /// <summary>
    /// Part B (#4) party toy art gate: every party-station seed object must
    /// resolve to FINAL art (a real Resources sprite from
    /// CareerQuestPartyToyArtBuilder), not the runtime tinted-token fallback —
    /// so the campus stops rendering toys as identical colored dots. Party toys
    /// are catalog-registered required:false (a missing PNG degrades gracefully
    /// to the handmade token rather than failing the global player-facing gate),
    /// so this dedicated test is what polices their final-art coverage.
    /// Fails loudly, naming the toys, until the builder has been run.
    /// </summary>
    public class PartyToyArtTests
    {
        [Test]
        public void EveryPartyToyResolvesToFinalArt()
        {
            var fallbacks = AssetCatalog.PartyToyEntries()
                .Select(entry => AssetCatalog.ResolveSprite(entry.Key))
                .Where(resolution => !resolution.IsFinalArt)
                .Select(resolution => resolution.RequestedId)
                .ToArray();

            Assert.That(
                fallbacks,
                Is.Empty,
                "Party toys must ship with final Resources art — run "
                + "CareerQuestPartyToyArtBuilder.Generate (menu: Career Quest/Art/"
                + "Generate Party Toy Art). Fallbacks: " + string.Join(", ", fallbacks));
        }

        [Test]
        public void PartyToyCatalogCoversEverySeedObjectId()
        {
            // The catalog is derived from the seeds, so every station's objects
            // across both seeds get a toy key (sanity: the set is non-trivial and
            // each key is unique).
            var keys = AssetCatalog.PartyToyEntries().Select(entry => entry.Key).ToArray();

            Assert.That(keys.Length, Is.GreaterThan(40), "Expected a full party-toy set across all stations.");
            Assert.That(keys.Distinct().Count(), Is.EqualTo(keys.Length), "Toy keys must be unique.");
            foreach (var key in keys)
            {
                Assert.That(key, Does.StartWith(AssetCatalog.PartyToyKeyPrefix), key);
            }
        }
    }
}
