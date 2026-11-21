using LegendsViewer.Backend.Contracts;
using LegendsViewer.Backend.Legends.WorldObjects;
using Microsoft.AspNetCore.Mvc;
using LegendsViewer.Backend.Legends.EventCollections;
using LegendsViewer.Backend.Legends.Enums;
using LegendsViewer.Backend.Legends.Events;
using LegendsViewer.Backend.DataAccess.Repositories.Interfaces;
using LegendsViewer.Backend.Legends.Various;

namespace LegendsViewer.Backend.Controllers;


public class DanceFormController(IWorldObjectRepository<DanceForm> repository) : WorldObjectGenericController<DanceForm>(repository)
{
}

public class MusicalFormController(IWorldObjectRepository<MusicalForm> repository) : WorldObjectGenericController<MusicalForm>(repository)
{
}

public class PoeticFormController(IWorldObjectRepository<PoeticForm> repository) : WorldObjectGenericController<PoeticForm>(repository)
{
}

public class WrittenContentController(IWorldObjectRepository<WrittenContent> repository) : WorldObjectGenericController<WrittenContent>(repository)
{
}

public class LandmassController(IWorldObjectRepository<Landmass> repository) : WorldObjectGenericController<Landmass>(repository)
{
}

public class RiverController(IWorldObjectRepository<River> repository) : WorldObjectGenericController<River>(repository)
{
}

public class SiteController(IWorldObjectRepository<Site> repository) : WorldObjectGenericController<Site>(repository)
{
}

public class RegionController(IWorldObjectRepository<WorldRegion> repository) : WorldObjectGenericController<WorldRegion>(repository)
{
}

public class UndergroundRegionController(IWorldObjectRepository<UndergroundRegion> repository) : WorldObjectGenericController<UndergroundRegion>(repository)
{
}

public class ArtifactController(IWorldObjectRepository<Artifact> repository) : WorldObjectGenericController<Artifact>(repository)
{
}

public class EntityController(IWorldObjectRepository<Entity> repository) : WorldObjectGenericController<Entity>(repository)
{
    [HttpGet("civs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<List<Entity>> GetMainCivilizations()
    {
        return Ok(Repository.GetAllElements().Where(x => x.IsCiv || (x.EntityType == Legends.Enums.EntityType.Civilization && x.SiteHistory.Count > 0)));
    }

    [HttpGet("{id}/monarchs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<List<LeaderTimelineDto>> GetMonarchTimelines([FromRoute] int id)
    {
        Entity? entity = Repository.GetById(id);
        if (entity == null)
        {
            return NotFound();
        }
        return Ok(entity.GetLeaderTimelines());
    }
}

public class HistoricalFigureController(IWorldObjectRepository<HistoricalFigure> repository) : WorldObjectGenericController<HistoricalFigure>(repository)
{
    [HttpGet("dragons")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<List<DragonHistoricalFigureDto>> GetDragons()
    {
        var figures = Repository.GetAllElements();
        var volcanoCoordinates = figures.FirstOrDefault()?.World?.MountainPeaks
            .Where(peak => peak.IsVolcano)
            .SelectMany(peak => peak.Coordinates)
            .ToList() ?? [];
        var worldSites = figures.FirstOrDefault()?.World?.Sites ?? [];
        var dragons = figures
            .Where(figure => string.Equals(figure.Race.Id, "DRAGON", StringComparison.OrdinalIgnoreCase))
            .Select(figure => new DragonHistoricalFigureDto(
                figure.Id,
                figure.Name,
                figure.Caste.ToLowerInvariant(),
                figure.IsAlive,
                GetDistanceToNearestVolcano(figure, volcanoCoordinates),
                figure.RelatedSites
                    .Where(siteLink => siteLink.Site != null)
                    .Select(siteLink => CreateDragonRelatedSiteDto(siteLink.Site!))
                    .DistinctBy(site => site.Id)
                    .ToList(),
                figure.Events
                    .OfType<ChangeHfState>()
                    .Where(worldEvent => worldEvent.State == HfState.Settled && worldEvent.Site != null)
                    .Select(worldEvent => worldEvent.Site!)
                    .DistinctBy(site => site.Id)
                    .Select(CreateDragonRelatedSiteDto)
                    .ToList(),
                GetDragonInvolvedSites(figure, worldSites)
                    .Select(CreateDragonRelatedSiteDto)
                    .ToList(),
                GetDragonSiteAttacks(figure, worldSites),
                figure.Events
                    .OfType<ChangeHfState>()
                    .Where(worldEvent => worldEvent.State == HfState.Settled && worldEvent.SubState is not (45 or 46 or 47))
                    .OrderBy(worldEvent => worldEvent.Year)
                    .ThenBy(worldEvent => worldEvent.Id)
                    .Select(worldEvent => new WorldEventDto(worldEvent, figure))
                    .ToList()))
            .ToList();

        return Ok(dragons);
    }

    private static List<Site> GetDragonInvolvedSites(HistoricalFigure figure, IEnumerable<Site> worldSites)
    {
        // Dragon participation may be recorded as travel, combat, a rampage, or only a site link.
        var sites = figure.RelatedSites
            .Where(siteLink => siteLink.Site != null)
            .Select(siteLink => siteLink.Site!)
            .Concat(figure.Events.OfType<ChangeHfState>().Where(worldEvent => worldEvent.Site != null).Select(worldEvent => worldEvent.Site!))
            .Concat(figure.Events.OfType<HfTravel>().Where(worldEvent => worldEvent.Site != null).Select(worldEvent => worldEvent.Site!))
            .Concat(figure.Events.OfType<HfAttackedSite>().Where(worldEvent => worldEvent.Site != null).Select(worldEvent => worldEvent.Site!))
            .Concat(figure.Events.OfType<HfDestroyedSite>().Where(worldEvent => worldEvent.Site != null).Select(worldEvent => worldEvent.Site!))
            .Concat(figure.Events.OfType<HfSimpleBattleEvent>().Where(worldEvent => worldEvent.Site != null).Select(worldEvent => worldEvent.Site!))
            .Concat(figure.BeastAttacks.Where(attack => attack.Site != null).Select(attack => attack.Site!))
            .Concat(worldSites
                .SelectMany(site => site.Events.OfType<AttackedSite>())
                .Where(attack => attack.AttackerGeneral == figure && attack.Site != null)
                .Select(attack => attack.Site!));

        return sites.DistinctBy(site => site.Id).ToList();
    }

    private static List<DragonSiteAttackDto> GetDragonSiteAttacks(HistoricalFigure figure, IEnumerable<Site> worldSites)
    {
        // Rampage sub-events can also appear in the figure/site event lists; exclude them there to avoid duplicate arrows.
        var beastAttackEventIds = figure.BeastAttacks
            .SelectMany(attack => attack.AllEvents)
            .Select(worldEvent => worldEvent.Id)
            .ToHashSet();
        var attacks = new List<(int EventId, int Year, Site Site, WorldEvent? ReferenceEvent)>();

        // BeastAttack collections identify the target site even when no individual attack event does.
        attacks.AddRange(figure.BeastAttacks
            .Where(attack => attack.Site != null)
            .Select(attack => (
                attack.Id,
                attack.StartYear,
                attack.Site!,
                attack.AllEvents.OrderBy(worldEvent => worldEvent.Year).ThenBy(worldEvent => worldEvent.Id).FirstOrDefault())));

        attacks.AddRange(worldSites
            .SelectMany(site => site.Events.OfType<AttackedSite>())
            .Where(attack => attack.AttackerGeneral == figure && attack.Site != null && !beastAttackEventIds.Contains(attack.Id))
            .Select(attack => (attack.Id, attack.Year, attack.Site!, (WorldEvent?)attack)));

        attacks.AddRange(figure.Events
            .OfType<HfAttackedSite>()
            .Where(attack => attack.Attacker == figure && attack.Site != null && !beastAttackEventIds.Contains(attack.Id))
            .Select(attack => (attack.Id, attack.Year, attack.Site!, (WorldEvent?)attack)));

        attacks.AddRange(figure.Events
            .OfType<HfDestroyedSite>()
            .Where(attack => attack.Attacker == figure && attack.Site != null && !beastAttackEventIds.Contains(attack.Id))
            .Select(attack => (attack.Id, attack.Year, attack.Site!, (WorldEvent?)attack)));

        attacks.AddRange(figure.Events
            .OfType<HfSimpleBattleEvent>()
            .Where(attack => attack.HistoricalFigure1 == figure && attack.Site != null && IsAggressiveBattle(attack.SubType))
            .Select(attack => (attack.Id, attack.Year, attack.Site!, (WorldEvent?)attack)));

        return attacks
            .OrderBy(attack => attack.Year)
            .ThenBy(attack => attack.EventId)
            .Select(attack => new DragonSiteAttackDto(
                attack.EventId,
                attack.Year,
                attack.ReferenceEvent != null && GetPreviousSite(figure, attack.ReferenceEvent) is { } previousSite
                    ? ToDragonSiteDto(previousSite)
                    : null,
                ToDragonSiteDto(attack.Site)))
            .ToList();
    }

    // Include combat events where the figure is recorded as the initiating or winning aggressor.
    private static bool IsAggressiveBattle(HfSimpleBattleType battleType) =>
        battleType is HfSimpleBattleType.Attacked
            or HfSimpleBattleType.Ambushed
            or HfSimpleBattleType.Cornered
            or HfSimpleBattleType.Surprised
            or HfSimpleBattleType.Subdued
            or HfSimpleBattleType.Confronted
            or HfSimpleBattleType.Hf2LostAfterGivingWounds
            or HfSimpleBattleType.Hf2LostAfterMutualWounds
            or HfSimpleBattleType.Hf2LostAfterReceivingWounds;

    private static DragonRelatedSiteDto CreateDragonRelatedSiteDto(Site site) =>
        new(
            site.Id,
            site.Name,
            site.Type,
            ToDragonSiteDto(site),
            GetDragonSiteAttackers(site));

    private static List<DragonSiteAttackerDto> GetDragonSiteAttackers(Site site)
    {
        var attacks = new List<(HistoricalFigure Attacker, int EventId, int Year, WorldEvent? ReferenceEvent)>();

        // Site attacks use several event types; collect them together so dragon sites show ordinary HF attackers too.
        attacks.AddRange(site.Events
            .OfType<AttackedSite>()
            .Where(attack => attack.AttackerGeneral != null)
            .Select(attack => (attack.AttackerGeneral!, attack.Id, attack.Year, (WorldEvent?)attack)));

        attacks.AddRange(site.Events
            .OfType<HfAttackedSite>()
            .Where(attack => attack.Attacker != null)
            .Select(attack => (attack.Attacker!, attack.Id, attack.Year, (WorldEvent?)attack)));

        attacks.AddRange(site.Events
            .OfType<HfDestroyedSite>()
            .Where(attack => attack.Attacker != null)
            .Select(attack => (attack.Attacker!, attack.Id, attack.Year, (WorldEvent?)attack)));

        attacks.AddRange(site.Events
            .OfType<HfSimpleBattleEvent>()
            .Where(attack => attack.HistoricalFigure1 != null && IsAggressiveBattle(attack.SubType))
            .Select(attack => (attack.HistoricalFigure1!, attack.Id, attack.Year, (WorldEvent?)attack)));

        attacks.AddRange(site.BeastAttacks
            .Where(attack => attack.Beast != null)
            .Select(attack => (
                attack.Beast!,
                attack.Id,
                attack.StartYear,
                attack.AllEvents.OrderBy(worldEvent => worldEvent.Year).ThenBy(worldEvent => worldEvent.Id).FirstOrDefault())));

        return attacks
            .DistinctBy(attack => (attack.Attacker.Id, attack.EventId))
            .OrderBy(attack => attack.Year)
            .ThenBy(attack => attack.EventId)
            .Select(attack => CreateDragonSiteAttacker(attack.Attacker, attack.EventId, attack.Year, attack.ReferenceEvent))
            .ToList();
    }

    private static DragonSiteAttackerDto CreateDragonSiteAttacker(
        HistoricalFigure attacker,
        int eventId,
        int year,
        WorldEvent? referenceEvent)
    {
        var previousPosition = referenceEvent == null ? null : GetPreviousSite(attacker, referenceEvent);

        return new DragonSiteAttackerDto(
            attacker.Id,
            attacker.Name,
            eventId,
            year,
            previousPosition == null ? null : ToDragonSiteDto(previousPosition));
    }

    private static Site? GetPreviousSite(HistoricalFigure figure, WorldEvent referenceEvent)
    {
        // Use both state changes and travel records; some worlds record a visit without changing the figure's state.
        var locationEvents = figure.Events
            .OfType<ChangeHfState>()
            .Select(worldEvent => new
            {
                worldEvent.Year,
                worldEvent.Month,
                worldEvent.Day,
                worldEvent.Id,
                worldEvent.Site,
                worldEvent.Coordinates
            })
            .Concat(figure.Events
                .OfType<HfTravel>()
                .Select(worldEvent => new
                {
                    worldEvent.Year,
                    worldEvent.Month,
                    worldEvent.Day,
                    worldEvent.Id,
                    worldEvent.Site,
                    worldEvent.Coordinates
                }));

        // Compare by full event time, then map coordinate-only locations back to a known site.
        return locationEvents
            .Where(worldEvent => (worldEvent.Year, worldEvent.Month, worldEvent.Day, worldEvent.Id)
                .CompareTo((referenceEvent.Year, referenceEvent.Month, referenceEvent.Day, referenceEvent.Id)) < 0)
            .Select(worldEvent => new
            {
                worldEvent.Year,
                worldEvent.Month,
                worldEvent.Day,
                worldEvent.Id,
                Site = worldEvent.Site ?? (worldEvent.Coordinates == null
                    ? null
                    : figure.World?.Sites.FirstOrDefault(site => site.Coordinates.Contains(worldEvent.Coordinates)))
            })
            .Where(worldEvent => worldEvent.Site != null)
            .OrderByDescending(worldEvent => worldEvent.Year)
            .ThenByDescending(worldEvent => worldEvent.Month)
            .ThenByDescending(worldEvent => worldEvent.Day)
            .ThenByDescending(worldEvent => worldEvent.Id)
            .Select(worldEvent => worldEvent.Site)
            .FirstOrDefault();
    }

    private static DragonSiteDto ToDragonSiteDto(Site site) =>
        new(site.Id, site.Name, site.ToLink(), site.Coordinates);

    private static bool IsBefore(WorldEvent worldEvent, WorldEvent referenceEvent)
    {
        return (worldEvent.Year, worldEvent.Month, worldEvent.Day, worldEvent.Id)
            .CompareTo((referenceEvent.Year, referenceEvent.Month, referenceEvent.Day, referenceEvent.Id)) < 0;
    }

    private static double? GetDistanceToNearestVolcano(HistoricalFigure figure, List<Location> volcanoCoordinates)
    {
        var latestSettledEvent = figure.Events
            .OfType<ChangeHfState>()
            .Where(worldEvent => worldEvent.State == HfState.Settled && worldEvent.SubState is not (45 or 46 or 47))
            .OrderByDescending(worldEvent => worldEvent.Year)
            .ThenByDescending(worldEvent => worldEvent.Month)
            .ThenByDescending(worldEvent => worldEvent.Day)
            .ThenByDescending(worldEvent => worldEvent.Id)
            .FirstOrDefault();
        var dragonCoordinates = latestSettledEvent?.Site?.Coordinates;
        if (dragonCoordinates == null || dragonCoordinates.Count == 0)
        {
            dragonCoordinates = latestSettledEvent?.Coordinates is { } coordinates ? [coordinates] : [];
        }

        if (dragonCoordinates.Count == 0 || volcanoCoordinates.Count == 0)
        {
            return null;
        }

        return dragonCoordinates.Min(location => volcanoCoordinates.Min(volcano =>
        {
            var deltaX = (double)location.X - volcano.X;
            var deltaY = (double)location.Y - volcano.Y;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }));
    }
}

public class MountainPeakController(IWorldObjectRepository<MountainPeak> repository) : WorldObjectGenericController<MountainPeak>(repository)
{
}

public class StructureController(IWorldObjectRepository<Structure> repository) : WorldObjectGenericController<Structure>(repository)
{
}

public class ConstructionController(IWorldObjectRepository<WorldConstruction> repository) : WorldObjectGenericController<WorldConstruction>(repository)
{
}

public class EraController(IWorldObjectRepository<Era> repository) : WorldObjectGenericController<Era>(repository)
{
}

// Warfare

public class WarController(IWorldObjectRepository<War> repository) : WorldObjectGenericController<War>(repository)
{
}

public class BattleController(IWorldObjectRepository<Battle> repository) : WorldObjectGenericController<Battle>(repository)
{
}

public class DuelController(IWorldObjectRepository<Duel> repository) : WorldObjectGenericController<Duel>(repository)
{
}

public class RaidController(IWorldObjectRepository<Raid> repository) : WorldObjectGenericController<Raid>(repository)
{
}

public class SiteConqueredController(IWorldObjectRepository<SiteConquered> repository) : WorldObjectGenericController<SiteConquered>(repository)
{
}

// Politcal Conflicts 

public class InsurrectionController(IWorldObjectRepository<Insurrection> repository) : WorldObjectGenericController<Insurrection>(repository)
{
}

public class PersecutionController(IWorldObjectRepository<Persecution> repository) : WorldObjectGenericController<Persecution>(repository)
{
}

public class PurgeController(IWorldObjectRepository<Purge> repository) : WorldObjectGenericController<Purge>(repository)
{
}

public class CoupController(IWorldObjectRepository<EntityOverthrownCollection> repository) : WorldObjectGenericController<EntityOverthrownCollection>(repository)
{
}

public class BeastAttackController(IWorldObjectRepository<BeastAttack> repository) : WorldObjectGenericController<BeastAttack>(repository)
{
}

public class AbductionController(IWorldObjectRepository<Abduction> repository) : WorldObjectGenericController<Abduction>(repository)
{
}

public class TheftController(IWorldObjectRepository<Theft> repository) : WorldObjectGenericController<Theft>(repository)
{
}

// Rituals 

public class ProcessionController(IWorldObjectRepository<ProcessionCollection> repository) : WorldObjectGenericController<ProcessionCollection>(repository)
{
}

public class PerformanceController(IWorldObjectRepository<PerformanceCollection> repository) : WorldObjectGenericController<PerformanceCollection>(repository)
{
}

public class JourneyController(IWorldObjectRepository<Journey> repository) : WorldObjectGenericController<Journey>(repository)
{
}

public class CompetitionController(IWorldObjectRepository<CompetitionCollection> repository) : WorldObjectGenericController<CompetitionCollection>(repository)
{
}

public class CeremonyController(IWorldObjectRepository<CeremonyCollection> repository) : WorldObjectGenericController<CeremonyCollection>(repository)
{
}

public class OccasionController(IWorldObjectRepository<Occasion> repository) : WorldObjectGenericController<Occasion>(repository)
{
}
