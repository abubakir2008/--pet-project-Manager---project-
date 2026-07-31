<template>
  <div>
    <div class="page-header">
      <h1>Сотрудники</h1>
      <button class="btn btn-primary" @click="openCreate">+ Новый сотрудник</button>
    </div>

    <div class="card filters">
      <input v-model="search" placeholder="Поиск по имени или email" style="max-width: 320px" @input="debouncedLoad" />
    </div>

    <div class="card" style="margin-top: 16px; padding: 0;">
      <table>
        <thead>
          <tr><th>ФИО</th><th>Email</th><th>Роль</th><th></th></tr>
        </thead>
        <tbody>
          <tr v-for="e in employees" :key="e.id">
            <td>{{ e.lastName }} {{ e.firstName }} {{ e.middleName }}</td>
            <td>{{ e.email }}</td>
            <td>{{ roleLabel(e.roles[0]) }}</td>
            <td class="row-actions">
              <button class="btn btn-secondary btn-sm" @click="openEdit(e)">Изменить</button>
              <button class="btn btn-danger btn-sm" @click="remove(e)">Удалить</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <p v-if="error && !showForm" class="error">{{ error }}</p>

    <div v-if="showForm" class="modal-backdrop" @click.self="showForm = false">
      <form class="card modal" @submit.prevent="save">
        <h2>{{ editing ? 'Редактирование сотрудника' : 'Новый сотрудник' }}</h2>
        <label>Фамилия</label>
        <input v-model="form.lastName" required />
        <label>Имя</label>
        <input v-model="form.firstName" required />
        <label>Отчество</label>
        <input v-model="form.middleName" />
        <label>Email</label>
        <input v-model="form.email" type="email" required />
        <template v-if="!editing">
          <label>Пароль</label>
          <input v-model="form.password" type="password" minlength="6" required />
        </template>
        <label>Роль</label>
        <select v-model="form.role">
          <option value="Director">Руководитель</option>
          <option value="ProjectManager">Менеджер проекта</option>
          <option value="Employee">Сотрудник</option>
        </select>

        <p v-if="error" class="error">{{ error }}</p>

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

const employees = ref([])
const search = ref('')
const showForm = ref(false)
const editing = ref(null)
const error = ref('')

const form = reactive({ firstName: '', lastName: '', middleName: '', email: '', password: '', role: 'Employee' })

async function load() {
  const { data } = await http.get('/employees', { params: { search: search.value || undefined, take: 100 } })
  employees.value = data
}

let debounceTimer = null
function debouncedLoad() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(load, 350)
}

function openCreate() {
  editing.value = null
  Object.assign(form, { firstName: '', lastName: '', middleName: '', email: '', password: '', role: 'Employee' })
  error.value = ''
  showForm.value = true
}

function openEdit(e) {
  editing.value = e
  Object.assign(form, { firstName: e.firstName, lastName: e.lastName, middleName: e.middleName, email: e.email, password: '', role: e.roles[0] || 'Employee' })
  error.value = ''
  showForm.value = true
}

async function save() {
  error.value = ''
  try {
    if (editing.value) {
      await http.put(`/employees/${editing.value.id}`, {
        firstName: form.firstName, lastName: form.lastName, middleName: form.middleName,
        email: form.email, role: form.role
      })
    } else {
      await http.post('/employees', { ...form })
    }
    showForm.value = false
    await load()
  } catch (e) {
    error.value = e.response?.data?.message || e.response?.data?.detail || 'Не удалось сохранить сотрудника'
  }
}

async function remove(e) {
  if (!confirm(`Удалить сотрудника ${e.lastName} ${e.firstName}?`)) return
  error.value = ''
  try {
    await http.delete(`/employees/${e.id}`)
    await load()
  } catch (err) {
    // The server refuses to delete an employee who still manages a project or owns tasks.
    error.value = err.response?.data?.message || err.response?.data?.detail || 'Не удалось удалить сотрудника'
  }
}

function roleLabel(role) {
  return { Director: 'Руководитель', ProjectManager: 'Менеджер проекта', Employee: 'Сотрудник' }[role] || role || '—'
}

onMounted(load)
</script>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
.page-header h1 { margin: 0; font-size: 22px; }
.filters { display: flex; gap: 16px; }
.row-actions { display: flex; gap: 8px; }
.btn-sm { padding: 4px 10px; font-size: 12px; }
.modal-backdrop { position: fixed; inset: 0; background: rgba(0,0,0,.35); display: flex; align-items: center; justify-content: center; z-index: 50; }
.modal { width: 380px; display: flex; flex-direction: column; gap: 8px; }
.modal h2 { margin: 0 0 8px; font-size: 18px; }
.modal-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 12px; }
.error { color: var(--color-danger); font-size: 13px; }
</style>
