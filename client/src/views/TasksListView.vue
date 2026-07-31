<template>
  <div>
    <div class="page-header">
      <h1>Задачи</h1>
      <button v-if="canCreate" class="btn btn-primary" @click="openCreate">+ Новая задача</button>
    </div>

    <div class="card filters">
      <div class="filter-field">
        <label>Поиск</label>
        <input v-model="filters.search" placeholder="Название задачи" @input="debouncedLoad" />
      </div>
      <div class="filter-field">
        <label>Статус</label>
        <select v-model="filters.status" @change="load">
          <option :value="null">Все</option>
          <option :value="0">К выполнению</option>
          <option :value="1">В работе</option>
          <option :value="2">Готово</option>
        </select>
      </div>
    </div>

    <div class="card" style="margin-top: 16px; padding: 0;">
      <table>
        <thead>
          <tr>
            <th @click="sortBy('title')">Название</th>
            <th>Проект</th>
            <th>Автор</th>
            <th>Исполнитель</th>
            <th @click="sortBy('status')">Статус</th>
            <th @click="sortBy('priority')">Приоритет</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="t in tasks" :key="t.id">
            <td>{{ t.title }}</td>
            <td>{{ t.projectName }}</td>
            <td>{{ t.authorFullName }}</td>
            <td>{{ t.assigneeFullName || '—' }}</td>
            <td>
              <select :value="t.status" @change="changeStatus(t, $event.target.value)" :disabled="!canChangeStatus(t)">
                <option :value="0">К выполнению</option>
                <option :value="1">В работе</option>
                <option :value="2">Готово</option>
              </select>
            </td>
            <td>{{ t.priority }}</td>
            <td class="row-actions">
              <button v-if="canManage(t)" class="btn btn-secondary btn-sm" @click="openEdit(t)">Изменить</button>
              <button v-if="canManage(t)" class="btn btn-danger btn-sm" @click="remove(t)">Удалить</button>
            </td>
          </tr>
          <tr v-if="tasks.length === 0">
            <td colspan="7" class="muted" style="text-align:center; padding: 24px;">Задачи не найдены</td>
          </tr>
        </tbody>
      </table>
    </div>

    <p v-if="pageError" class="error">{{ pageError }}</p>

    <div v-if="showForm" class="modal-backdrop" @click.self="showForm = false">
      <form class="card modal" @submit.prevent="save">
        <h2>{{ editing ? 'Редактирование задачи' : 'Новая задача' }}</h2>

        <label>Название</label>
        <input v-model="form.title" required />

        <label v-if="!editing">Проект</label>
        <select v-if="!editing" v-model.number="form.projectId" required>
          <option v-for="p in myProjects" :key="p.id" :value="p.id">{{ p.name }}</option>
        </select>

        <label>Исполнитель</label>
        <EmployeeAutocomplete v-model="form.assigneeId" placeholder="Выберите исполнителя" />

        <label>Приоритет</label>
        <input v-model.number="form.priority" type="number" min="1" />

        <label>Комментарий</label>
        <textarea v-model="form.comment" rows="3"></textarea>

        <p v-if="formError" class="error">{{ formError }}</p>

        <div class="modal-actions">
          <button type="button" class="btn btn-secondary" @click="showForm = false">Отмена</button>
          <button type="submit" class="btn btn-primary">Сохранить</button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import http from '../api/http'
import { useAuthStore } from '../stores/auth'
import EmployeeAutocomplete from '../components/EmployeeAutocomplete.vue'

const auth = useAuthStore()
const canCreate = ref(auth.isDirector || auth.isProjectManager)

const tasks = ref([])
const myProjects = ref([])
const showForm = ref(false)
const editing = ref(null)

const formError = ref('')
const pageError = ref('')

const filters = reactive({ search: '', status: null })
const sortState = reactive({ field: 'priority', desc: true })
const form = reactive({ title: '', projectId: null, assigneeId: null, priority: 1, comment: '' })

function sortBy(field) {
  if (sortState.field === field) sortState.desc = !sortState.desc
  else { sortState.field = field; sortState.desc = false }
  load()
}

let debounceTimer = null
function debouncedLoad() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(load, 350)
}

async function load() {
  const { data } = await http.get('/tasks', {
    params: {
      search: filters.search || undefined,
      status: filters.status,
      sortBy: sortState.field,
      desc: sortState.desc,
      pageSize: 100
    }
  })
  tasks.value = data.items
}

async function loadMyProjects() {
  const { data } = await http.get('/projects', { params: { pageSize: 100 } })
  myProjects.value = data.items
}

function openCreate() {
  editing.value = null
  formError.value = ''
  Object.assign(form, { title: '', projectId: myProjects.value[0]?.id ?? null, assigneeId: null, priority: 1, comment: '' })
  showForm.value = true
}

function openEdit(t) {
  editing.value = t
  formError.value = ''
  Object.assign(form, { title: t.title, projectId: t.projectId, assigneeId: t.assigneeId, priority: t.priority, comment: t.comment })
  showForm.value = true
}

// The server rejects an assignee who does not work on the project, an unknown
// project and any operation the role is not allowed to perform, so every call
// reports the returned message instead of failing silently.
function describeError(e, fallback) {
  return e.response?.data?.message || e.response?.data?.detail || fallback
}

async function save() {
  formError.value = ''
  try {
    if (editing.value) {
      await http.put(`/tasks/${editing.value.id}`, {
        title: form.title, assigneeId: form.assigneeId, status: editing.value.status,
        comment: form.comment, priority: form.priority
      })
    } else {
      await http.post('/tasks', {
        title: form.title, projectId: form.projectId, assigneeId: form.assigneeId,
        comment: form.comment, priority: form.priority
      })
    }
    showForm.value = false
    await load()
  } catch (e) {
    formError.value = describeError(e, 'Не удалось сохранить задачу')
  }
}

async function remove(t) {
  if (!confirm(`Удалить задачу "${t.title}"?`)) return
  pageError.value = ''
  try {
    await http.delete(`/tasks/${t.id}`)
    await load()
  } catch (e) {
    pageError.value = describeError(e, 'Не удалось удалить задачу')
  }
}

async function changeStatus(t, statusValue) {
  pageError.value = ''
  try {
    await http.patch(`/tasks/${t.id}/status`, { status: Number(statusValue) })
  } catch (e) {
    pageError.value = describeError(e, 'Не удалось изменить статус задачи')
  }
  await load()
}

function canManage(t) {
  return auth.isDirector || (auth.isProjectManager)
}
function canChangeStatus(t) {
  return auth.isDirector || auth.isProjectManager || t.assigneeId === auth.employeeId
}

onMounted(async () => {
  await loadMyProjects()
  await load()
})
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.page-header h1 { margin: 0; font-size: 22px; }
.filters { display: flex; gap: 16px; }
.filter-field { display: flex; flex-direction: column; gap: 4px; font-size: 12px; color: var(--color-muted); min-width: 200px; }
.row-actions { display: flex; gap: 8px; }
.btn-sm { padding: 4px 10px; font-size: 12px; }
.modal-backdrop { position: fixed; inset: 0; background: rgba(0,0,0,.35); display: flex; align-items: center; justify-content: center; z-index: 50; }
.modal { width: 420px; display: flex; flex-direction: column; gap: 8px; max-height: 90vh; overflow-y: auto; }
.modal h2 { margin: 0 0 8px; font-size: 18px; }
.modal-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 12px; }
.error { color: var(--color-danger); font-size: 13px; }
</style>
