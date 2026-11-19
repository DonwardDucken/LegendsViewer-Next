<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { API_BASE_URL } from '../apiClient';

interface DragonRelatedSite {
    id: number;
    name: string;
    type: string;
}

interface DragonEvent {
    id: number;
    date: string;
    html: string;
}

interface Dragon {
    id: number;
    name: string;
    gender: string;
    isAlive: boolean;
    distanceToNearestVolcano: number | null;
    relatedSites: DragonRelatedSite[];
    settledInEvents: DragonEvent[];
}

const dragons = ref<Dragon[]>([]);
const isLoading = ref(false);
const errorMessage = ref('');
const search = ref('');
const aliveFilter = ref('all');

const statusOptions = [
    { title: 'All', value: 'all' },
    { title: 'Alive', value: 'alive' },
    { title: 'Not alive', value: 'dead' },
];

const headers = [
    { title: 'Name', key: 'name', align: 'start' as const },
    { title: 'Distance to nearest volcano', key: 'distanceToNearestVolcano', align: 'end' as const },
    { title: 'Status', key: 'isAlive', align: 'start' as const },
    { title: 'Related sites and other dragons', key: 'relatedSites', align: 'start' as const },
    { title: 'Settled in events', key: 'settledInEvents', align: 'start' as const },
];

function genderIcon(gender: string): string {
    if (gender === 'female') {
        return 'mdi-gender-female';
    }
    if (gender === 'male') {
        return 'mdi-gender-male';
    }
    return 'mdi-help-circle-outline';
}

function formatDistance(distance: number | null): string {
    return distance == null ? 'Unknown' : `${distance.toFixed(1)} tiles`;
}

const visibleDragons = computed(() => {
    const query = search.value.trim().toLocaleLowerCase();
    return dragons.value.filter((dragon) => {
        const matchesStatus = aliveFilter.value === 'all'
            || (aliveFilter.value === 'alive' && dragon.isAlive)
            || (aliveFilter.value === 'dead' && !dragon.isAlive);
        const matchesSearch = !query
            || dragon.name.toLocaleLowerCase().includes(query)
            || dragon.relatedSites.some((site) => site.name.toLocaleLowerCase().includes(query));
        return matchesStatus && matchesSearch;
    });
});

function otherDragonsAtSite(dragon: Dragon, siteId: number): Dragon[] {
    return visibleDragons.value.filter((candidate) =>
        candidate.id !== dragon.id && candidate.relatedSites.some((site) => site.id === siteId));
}

async function loadDragons() {
    isLoading.value = true;
    errorMessage.value = '';
    try {
        const response = await fetch(new URL('api/HistoricalFigure/dragons', API_BASE_URL));
        if (!response.ok) {
            throw new Error(`Request failed (${response.status})`);
        }
        dragons.value = await response.json() as Dragon[];
    } catch (error) {
        errorMessage.value = error instanceof Error ? error.message : 'Could not load dragons.';
    } finally {
        isLoading.value = false;
    }
}

onMounted(loadDragons);
</script>

<template>
    <v-container fluid>
        <v-row>
            <v-col cols="12">
                <v-card variant="text">
                    <v-row align="center" no-gutters>
                        <v-col cols="auto" class="large-icon">
                            <v-icon icon="mdi-dragon"></v-icon>
                        </v-col>
                        <v-col>
                            <v-card-title>Dragons</v-card-title>
                            <v-card-subtitle class="multiline-subtitle">
                                Historical figures of the dragon race
                            </v-card-subtitle>
                        </v-col>
                    </v-row>
                </v-card>
            </v-col>
        </v-row>

        <v-row>
            <v-col cols="12">
                <v-card title="Overview" subtitle="Browse dragons, their related sites and settlement events" variant="text">
                    <template #prepend>
                        <v-icon class="mr-2" icon="mdi-card-search-outline" size="32px"></v-icon>
                    </template>
                    <template #text>
                        <v-row align="center">
                            <v-col cols="12" md="7">
                                <v-text-field v-model="search" label="Search dragons or sites"
                                    prepend-inner-icon="mdi-magnify" variant="outlined" hide-details single-line />
                            </v-col>
                            <v-col cols="12" md="3">
                                <v-select v-model="aliveFilter" :items="statusOptions" label="Status"
                                    variant="outlined" hide-details />
                            </v-col>
                            <v-col cols="12" md="2" class="text-md-end">
                                <v-chip color="cyan" label>
                                    <v-icon icon="mdi-dragon" start />
                                    {{ visibleDragons.length }} / {{ dragons.length }}
                                </v-chip>
                            </v-col>
                        </v-row>
                    </template>

                    <v-alert v-if="errorMessage" class="mx-4 mb-4" type="error" variant="tonal">
                        {{ errorMessage }}
                    </v-alert>

                    <v-card-text>
                        <v-data-table :headers="headers" :items="visibleDragons" :loading="isLoading"
                            item-value="id" no-data-text="No dragons found">
                            <template #item.name="{ item }">
                                <v-icon :icon="genderIcon(item.gender)" :title="item.gender || 'Unknown gender'"
                                    size="small" class="mr-1" />
                                <router-link :to="`/hf/${item.id}`" class="dragon-link">
                                    {{ item.name }}
                                </router-link>
                            </template>
                            <template #item.distanceToNearestVolcano="{ value }">
                                {{ formatDistance(value) }}
                            </template>
                            <template #item.isAlive="{ value }">
                                <v-chip size="small" :color="value ? 'green' : 'grey'" variant="tonal">
                                    {{ value ? 'Alive' : 'Not alive' }}
                                </v-chip>
                            </template>
                            <template #item.relatedSites="{ item }">
                                <div v-if="item.relatedSites.length" class="site-list">
                                    <div v-for="site in item.relatedSites" :key="site.id" class="site-entry">
                                        <router-link :to="`/site/${site.id}`" class="dragon-link">
                                            {{ site.name }}
                                        </router-link>
                                        <span class="site-type">{{ site.type }}</span>
                                        <span v-if="otherDragonsAtSite(item, site.id).length" class="co-located">
                                            Also related:
                                            <router-link v-for="other in otherDragonsAtSite(item, site.id)"
                                                :key="other.id" :to="`/hf/${other.id}`" class="dragon-link">
                                                {{ other.name }}
                                            </router-link>
                                        </span>
                                    </div>
                                </div>
                                <span v-else class="muted">No related sites</span>
                            </template>
                            <template #item.settledInEvents="{ item }">
                                <div v-if="item.settledInEvents.length">
                                    <div v-for="event in item.settledInEvents" :key="event.id" class="event-entry"
                                        v-html="event.html"></div>
                                </div>
                                <span v-else class="muted">None</span>
                            </template>
                        </v-data-table>
                    </v-card-text>
                </v-card>
            </v-col>
        </v-row>
    </v-container>
</template>

<style scoped>
.multiline-subtitle {
    white-space: normal;
}

.dragon-link {
    color: rgb(var(--v-theme-primary));
    text-decoration: none;
}

.dragon-link:hover {
    text-decoration: underline;
}

.site-list {
    display: grid;
    gap: 6px;
}

.site-entry {
    display: flex;
    flex-wrap: wrap;
    align-items: baseline;
    gap: 4px 8px;
}

.site-type,
.muted {
    color: rgba(var(--v-theme-on-surface), 0.62);
}

.site-type {
    font-size: 0.85em;
}

.co-located {
    flex-basis: 100%;
    font-size: 0.9em;
}

.co-located .dragon-link + .dragon-link::before {
    content: ', ';
}

.event-entry {
    margin-top: 6px;
}
</style>