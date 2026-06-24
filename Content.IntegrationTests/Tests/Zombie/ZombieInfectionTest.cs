using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Server.Zombies;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mobs.Systems;
using Content.Shared.Zombies;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Zombie;

/// <summary>
///     Integration tests related to zombie infections.
/// </summary>
public sealed class ZombieInfectionTest : GameTest
{
    private static readonly string[] Species = GameDataScrounger.PrototypesOfKind<SpeciesPrototype>();

    [SidedDependency(Side.Server)] private readonly EntityQuery<DamageableComponent> _damageableQuery = default!;
    [SidedDependency(Side.Server)] private readonly EntityQuery<PendingZombieComponent> _pendingQuery = default!;
    [SidedDependency(Side.Server)] private readonly EntityQuery<ZombifyOnDeathComponent> _zombifyOnDeathQuery = default!;
    [SidedDependency(Side.Server)] private readonly EntityQuery<ZombieImmuneComponent> _immuneQuery = default!;
    [SidedDependency(Side.Server)] private readonly EntityQuery<ZombieComponent> _zombieQuery = default!;

    private readonly TimeSpan _tickInterval = TimeSpan.FromSeconds(1.0f);

    [Test]
    [TestOf(typeof(ZombieSystem))]
    [TestOf(typeof(PendingZombieComponent))]
    [TestCaseSource(nameof(Species))]
    [Description("Tests that zombie infection will progress on all species.")]
    public async Task AllSpeciesCanZombify(string speciesId)
    {
        var mobStateSys = SEntMan.System<MobStateSystem>();
        var zombieSys = SEntMan.System<ZombieSystem>();

        // var damageableSys = SEntMan.System<DamageableSystem>();

        EntityUid mob = default;
        PendingZombieComponent pendingZombie = default;
        ZombifyOnDeathComponent zombifyOnDeath = default;

        await Server.WaitIdleAsync();

        await Server.WaitAssertion(() =>
        {
            var species = SProtoMan.Index<SpeciesPrototype>(speciesId);
            var mobProto = SProtoMan.Index<EntityPrototype>(species.Prototype);
            mob = SEntMan.Spawn(mobProto.ID);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(mobStateSys.IsAlive(mob), $"{SToPrettyString(mob)} was not alive when spawned!");
                Assert.That(_damageableQuery.HasComp(mob), $"{SToPrettyString(mob)} cannot take damage!");
                Assert.That(!_immuneQuery.HasComp(mob), $"{SToPrettyString(mob)} is immune to zombie infection!");
            }

            zombieSys.AfflictWithInfection(mob);
            Assert.That(!_zombieQuery.HasComp(mob), $"{SToPrettyString(mob)} zombified instantly on affliction!");

            pendingZombie = _pendingQuery.Get(mob);
            zombifyOnDeath = _zombifyOnDeathQuery.Get(mob);
        });
    }
}
