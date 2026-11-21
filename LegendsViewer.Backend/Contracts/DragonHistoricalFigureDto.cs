using LegendsViewer.Backend.Extensions;
using LegendsViewer.Backend.Legends.Enums;
using LegendsViewer.Backend.Legends.Extensions;
using LegendsViewer.Backend.Legends.Various;
using LegendsViewer.Backend.Legends.WorldObjects;
namespace LegendsViewer.Backend.Contracts;

/// <summary>Provides a dragon with related sites, their attackers, and settled-in events.</summary>
public sealed record DragonHistoricalFigureDto(
    int Id,
    string Name,
    string Gender,
    bool IsAlive,
    double? DistanceToNearestVolcano,
    List<DragonRelatedSiteDto> RelatedSites,
    List<DragonRelatedSiteDto> SettledSites,
    List<DragonRelatedSiteDto> InvolvedSites,
    List<DragonSiteAttackDto> SiteAttacks,
    List<WorldEventDto> SettledInEvents);


/// <summary>Identifies a site related to a dragon historical figure.</summary>
public sealed record DragonRelatedSiteDto(
    int Id,
    string Name,
    string Type,
    DragonSiteDto Site,
    List<DragonSiteAttackerDto> Attackers);

/// <summary>Provides a linkable site and its world-map coordinates.</summary>
public sealed record DragonSiteDto(int Id, string Name, string Link, List<Location> Coordinates);

/// <summary>Identifies an attacking general and their last known position before an attack.</summary>
public sealed record DragonSiteAttackerDto(
    int Id,
    string Name,
    int AttackEventId,
    int AttackYear,
    DragonSiteDto? PreviousPosition);

/// <summary>Describes a dragon attack between its previous and attacked sites.</summary>
public sealed record DragonSiteAttackDto(
    int AttackEventId,
    int AttackYear,
    DragonSiteDto? PreviousPosition,
    DragonSiteDto TargetSite);