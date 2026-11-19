using LegendsViewer.Backend.Extensions;
using LegendsViewer.Backend.Legends.Enums;
using LegendsViewer.Backend.Legends.Extensions;
using LegendsViewer.Backend.Legends.Various;
using LegendsViewer.Backend.Legends.WorldObjects;
namespace LegendsViewer.Backend.Contracts;

/// <summary>Provides a dragon historical figure with its related sites and settled-in events.</summary>
public sealed record DragonHistoricalFigureDto(
    int Id,
    string Name,
    bool IsAlive,
    List<DragonRelatedSiteDto> RelatedSites,
    List<WorldEventDto> SettledInEvents);


/// <summary>Identifies a site related to a dragon historical figure.</summary>
public sealed record DragonRelatedSiteDto(int Id, string Name, string Type, Site site);