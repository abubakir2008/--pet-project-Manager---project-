<template>
  <div>
    <div class="page-header">
      <h1>Проекты</h1>
      <router-link v-if="canCreate" class="btn btn-primary" to="/projects/new">+ Новый проект</router-link>
    </div>

    <div class="card filters">
      <div class="filter-field">
        <label>Поиск</label>
        <input v-model="filters.search" placeholder="Название, заказчик, исполнитель" @input="debouncedLoad" />
      </div>
      <div class="filter-field">
        <label>Дата начала с</label>
        <input v-model="filters.startDateFrom" type="date" @change="load" />
      </div>
      <div class="filter-field">
        <label>по</label>
        <input v-model="filters.startDateTo" type="date" @change="load" />
      </div>
      <div class="filter-field">
        <label>Приоритет с</label>
        <input v-model.number="filters.priorityFrom" type="number" min="1" @change="load" />
      </div>
      <div class="filter-field">
        <label>по</label>
        <input v-model.number="filters.priorityTo" type="number" min="1" @change="load" />
      </div>
      <button class="btn btn-secondary" @click="resetFilters">Сбросить</button>
    </div>

    <div class="card" style="margin-top: 16px; padding: 0;">
      <table>
        <thead>
          <tr>
            <th @click="sortBy('name')">Название <SortIcon field="name" :sort="sortState" /></th>
            <th>Заказчик</th>
            <th>Исполнитель</th>
            <th @click="sortBy('startDate')">Начало <SortIcon field="startDate" :sort="sortState" /></th>
            <th @click="sortBy('endDate')">Окончание <SortIcon field="endDate" :sort="sortState" /></th>
            <th @click="sortBy('priority')">Приоритет <SortIcon field="priority" :sort="sortState" /></th>
            <th>Руководитель</th>
            <th>Сотрудники</th>
            <th>Задачи</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in projects" :key="p.id">
            <td>{{ p.name }}</td>
            <td>{{ p.customerCompany }}</td>
            <td>{{ p.contractorCompany }}</td>
            <td>{{ formatDate(p.startDate) }}</td>
            <td>{{ formatDate(p.endDate) }}</td>
            <td>{{ p.priority }}</td>
            <td>{{ p.managerFullName }}</td>
            <td>{{ p.employeeCount }}</td>
            <td>{{ p.taskCount }}</td>
            <td>
              <router-link :to="`/projects/${p.id}/edit`">Открыть</router-link>
            </td>
          </tr>
          <tr v-if="!loading && projects.length === 0">
            <td colspan="10" class="muted" style="text-align:center; padding: 24px;">Проекты не найдены</td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="pagination">
      <button class="btn btn-secondary" :disabled="page <= 1" @click="page--; load()">Назад</button>
      <span>Страница {{ page }} из {{ totalPages }}</span>
      <button class="btn btn-secondary" :disabled="page >= totalPages" @click="page++; load()">Вперёд</button>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, h } from 'vue'
import http from '../api/http'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const canCreate = computed(() => auth.isDirector || auth.isProjectManager)

const projects = ref([])
const loading = ref(false)
const totalCount = ref(0)
const page = ref(1)
const pageSize = 20
const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize)))

const filters = reactive({
  search: '',
  startDateFrom: '',
  startDateTo: '',
  priorityFrom: null,
  priorityTo: null
})

const sortState = reactive({ field: 'startDate', desc: true })

function sortBy(field) {
  if (sortState.field === field) sortState.desc = !sortState.desc
  else { sortState.field = field; sortState.desc = false }
  load()
}

const SortIcon = {
  props: ['field', 'sort'],
  render() {
    if (this.sort.field !== this.field) return h('span')
    return h('span', this.sort.desc ? ' ▼' : ' ▲')
  }
}

let debounceTimer = null
function debouncedLoad() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(load, 350)
}

function resetFilters() {
  filters.search = ''
  filters.startDateFrom = ''
  filters.startDateTo = ''
  filters.priorityFrom = null
  filters.priorityTo = null
  page.value = 1
  load()
}

async function load() {
  loading.value = true
  try {
    const { data } = await http.get('/projects', {
      params: {
        search: filters.search || undefined,
        startDateFrom: filters.startDateFrom || undefined,
        startDateTo: filters.startDateTo || undefined,
        priorityFrom: filters.priorityFrom || undefined,
        priorityTo: filters.priorityTo || undefined,
        sortBy: sortState.field,
        desc: sortState.desc,
        page: page.value,
        pageSize
      }
    })
    projects.value = data.items
    totalCount.value = data.totalCount
  } finally {
    loading.value = false
  }
}

function formatDate(d) {
  return new Date(d).toLocaleDateString('ru-RU')
}

onMounted(load)
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.page-header h1 { margin: 0; font-size: 22px; }
.filters { display: flex; gap: 16px; align-items: flex-end; flex-wrap: wrap; }
.filter-field { display: flex; flex-direction: column; gap: 4px; font-size: 12px; color: var(--color-muted); min-width: 140px; }
.pagination { display: flex; gap: 12px; align-items: center; justify-content: center; margin-top: 16px; font-size: 14px; }
table td a { color: var(--color-primary); text-decoration: none; font-weight: 500; }
</style>
