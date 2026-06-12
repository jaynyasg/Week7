using System.Linq;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U8 cue-coverage contract: every cue ID the code can play (the
    /// AudioCueIds registry — call sites reference its constants, never raw
    /// strings) must resolve to a curated clip under Resources/Audio. These
    /// tests fail LOUDLY until CareerQuestAudioCurator.Curate has been run.
    /// </summary>
    public class AudioCueCoverageTests
    {
        [Test]
        public void EveryRegisteredCueIdResolvesToAClipUnderResourcesAudio()
        {
            var missing = AudioCueIds.All
                .Where(cueId => Resources.Load<AudioClip>($"Audio/{cueId}") == null)
                .ToList();

            Assert.That(missing, Is.Empty,
                "Missing curated clips for cue IDs — run CareerQuestAudioCurator.Curate "
                + "(menu: Career Quest/Audio/Curate Audio Cues). Missing: "
                + string.Join(", ", missing));
        }

        [Test]
        public void RegistryHasNoDuplicateOrBlankCueIds()
        {
            Assert.That(AudioCueIds.All.Distinct().Count(), Is.EqualTo(AudioCueIds.All.Length),
                "Duplicate cue IDs in AudioCueIds.All.");
            Assert.That(AudioCueIds.All.Any(string.IsNullOrWhiteSpace), Is.False,
                "Blank cue ID in AudioCueIds.All.");
        }

        [Test]
        public void CeremonyCueTemplateStaysInsideTheRegistryForEveryCoreActivity()
        {
            // FeedbackController generates ceremony cues through this template;
            // if a core activity/tier combination ever leaves the registry, the
            // coverage gate above could no longer protect it.
            var coreActivityIds = new[]
            {
                CareerConfig.DesignBuildId,
                CareerConfig.HealthHeroId,
                CareerConfig.LogicCourtId
            };

            foreach (var activityId in coreActivityIds)
            {
                foreach (var success in new[] { true, false })
                {
                    var cueId = AudioCueIds.CeremonyCue(activityId, success);
                    Assert.That(AudioCueIds.All, Does.Contain(cueId),
                        $"Generated ceremony cue '{cueId}' is not in AudioCueIds.All.");
                }
            }
        }

        [Test]
        public void LoopClassificationCoversExactlyTheAmbientAndMusicCues()
        {
            foreach (var cueId in AudioCueIds.All)
            {
                var expected = cueId.StartsWith("ambient_") || cueId.StartsWith("music_");
                Assert.That(AudioCueIds.IsLoopCue(cueId), Is.EqualTo(expected),
                    $"Loop classification disagrees with the naming convention for '{cueId}'.");
            }
        }
    }
}
