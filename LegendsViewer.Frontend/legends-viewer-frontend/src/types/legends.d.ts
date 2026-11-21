import { components } from '../generated/api-schema'; // Import from the OpenAPI schema

type ListItemDto = components['schemas']['ListItemDto'];

export type LegendLinkListData = {
    title: string,
    subtitle: string,
    icon: string,
    items: string[] | ListItemDto[]
}

export interface LoadItemsSortOption {
    key: string,
    order: "asc" | "desc"
}

export interface LoadItemsOptions {
    page: number;
    itemsPerPage: number;
    sortBy: LoadItemsSortOption[];
}

export interface LoadItemsOptionsWithSearch {
    page: number;
    itemsPerPage: number;
    sortBy: LoadItemsSortOption[];
    search: string;
}

export interface TableHeader {
    title: string;
    key: string;
    align?: 'start' | 'end' | 'center';
    sortable?: boolean;
}

export interface LeaderTimelineItemDto {
    id: number;
    name: string;
    link: string;
    positionTitle: string;
    caste: string;
    race: string;
    startYear?: number;
    endYear?: number;
    startYearDisplay: string;
    endYearDisplay: string;
    reignDuration: string;
    isAlive: boolean;
    birthYear: number;
    deathYear: number;
    deathCause: string;
    predecessorRelation: string;
}

export interface LeaderTimelineDto {
    positionId?: number;
    leaderType: string;
    leaders: LeaderTimelineItemDto[];
}

export interface LocationDto {
    x: number;
    y: number;
}

export interface BattleMapMarkerDto {
    id: number;
    name: string;
    link?: string;
    warId?: number;
    warName?: string;
    warLink?: string;
    attackerId?: number;
    attackerName?: string;
    attackerColor?: string;
    attackerLink?: string;
    defenderId?: number;
    defenderName?: string;
    defenderLink?: string;
    victorId?: number;
    victorName?: string;
    victorLink?: string;
    startYear?: number;
    endYear?: number;
    isActive: boolean;
    deathCount?: number;
    attackerDeathCount?: number;
    defenderDeathCount?: number;
    coordinates?: LocationDto;
}

export interface WarMapOverlayDto {
    id: number;
    name: string;
    link?: string;
    attackerId?: number;
    attackerName?: string;
    attackerColor?: string;
    attackerLink?: string;
    defenderId?: number;
    defenderName?: string;
    defenderLink?: string;
    startYear: number;
    endYear: number;
    isActive: boolean;
    deathCount?: number;
    attackerCoordinates?: LocationDto;
    defenderCoordinates?: LocationDto;
    battleIds?: number[];
}

export interface WarfareMapDto {
    wars: WarMapOverlayDto[];
    battles: BattleMapMarkerDto[];
}

export interface DragonHistoricalFigureDto{
    id: number;
    name: string;
    isAlive: boolean;
    relatedSites: DragonRelatedSiteDto[];
    settledSites: DragonRelatedSiteDto[];
    involvedSites: DragonRelatedSiteDto[];
    siteAttacks: DragonSiteAttackDto[];
    settledInEvents: WorldEventDto[];
}

export interface DragonRelatedSiteDto {
    id: number;
    name: string;
    type: string;
    site: DragonSiteDto;
    attackers: DragonSiteAttackerDto[];
}

export interface DragonSiteDto {
    id: number;
    name: string;
    link: string;
    coordinates: LocationDto[];
}

export interface DragonSiteAttackerDto {
    id: number;
    name: string;
    attackEventId: number;
    attackYear: number;
    previousPosition: DragonSiteDto | null;
}

export interface DragonSiteAttackDto {
    attackEventId: number;
    attackYear: number;
    previousPosition: DragonSiteDto | null;
    targetSite: DragonSiteDto;
}
