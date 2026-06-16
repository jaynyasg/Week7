using System;
using System.Linq;
using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    /// <summary>
    /// Design-review (2026-06-16): every party-station seed names a subject in
    /// its copy ("the hiccuping dragon"); the playfield now draws that subject.
    /// These guard the data spine: full coverage, safe names, known kinds.
    /// </summary>
    public class StationSubjectCatalogTests
    {
        [Test]
        public void EverySeedMapsToASceneSubject()
        {
            var missing = StationSubjectCatalog.MissingSeedIds();
            Assert.That(missing, Is.Empty,
                $"Every authored seed needs a drawn subject. Missing: {string.Join(", ", missing)}");
        }

        [Test]
        public void EverySubjectHasASafeNonEmptyNameAndKnownKind()
        {
            foreach (var station in PartyStationDefinitions.All)
            {
                foreach (var seed in station.Seeds)
                {
                    Assert.That(StationSubjectCatalog.TryGet(seed.SeedId, out var subject), Is.True, seed.SeedId);
                    Assert.That(string.IsNullOrWhiteSpace(subject.Name), Is.False, $"{seed.SeedId}: empty subject name");
                    Assert.That(Enum.IsDefined(typeof(StationSubjectKind), subject.Kind), Is.True, $"{seed.SeedId}: unknown kind");

                    var safety = PartyStationValidator.CheckCopySafety(subject.Name, seed.SeedId);
                    Assert.That(safety, Is.Empty,
                        $"{seed.SeedId} subject name '{subject.Name}' tripped copy safety: {string.Join(" | ", safety)}");
                }
            }
        }

        [Test]
        public void VetClinicDefaultSubjectIsTheDragon()
        {
            // The exact user-reported case: "help the dragon" now resolves a dragon.
            Assert.That(StationSubjectCatalog.TryGet("vet_clinic.dragon_hiccups", out var subject), Is.True);
            Assert.That(subject.Kind, Is.EqualTo(StationSubjectKind.Dragon));
            Assert.That(subject.Name, Does.Contain("Dragon"));
        }

        [Test]
        public void ValidatorFlagsTheWholeDataSpineAsCleanWithSubjects()
        {
            // The subject coverage + name-safety gates are wired into ValidateAll.
            var issues = PartyStationValidator.ValidateAll();
            Assert.That(issues, Is.Empty, $"Data spine issues: {string.Join(" | ", issues.Take(10))}");
        }
    }
}
