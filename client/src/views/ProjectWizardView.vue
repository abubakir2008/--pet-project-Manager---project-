<template>
  <div class="wizard-page">
    <h1>{{ isEditing ? 'Редактирование проекта' : 'Новый проект' }}</h1>

    <ol class="steps">
      <li v-for="(label, idx) in stepLabels" :key="idx" :class="{ active: step === idx, done: step > idx }">
        <span class="step-num">{{ idx + 1 }}</span>{{ label }}
      </li>
    </ol>

    <div class="card step-body">
      <!--
        form is a reactive() object, so instead of v-model (which would try to
        reassign the constant) every step emits a partial patch that is merged in.
      -->
      <StepBasicInfo v-if="step === 0" :model-value="form" @update:model-value="patchForm" />
      <StepCompanies v-else-if="step === 1" :model-value="form" @update:model-value="patchForm" />
      <StepManager v-else-if="step === 2" :model-value="form" @update:model-value="patchForm" />
      <StepExecutors v-else-if="step === 3" :model-value="form" @update:model-value="patchForm" />
      <StepDocuments v-else-if="step === 4" :project-id="projectId" />

      <p v-if="error" class="error">{{ error }}</p>

      <div class="step-actions">
        <button class="btn btn-secondary" :disabled="step === 0" @click="step--">Назад</button>
        <button v-if="step < 4" class="btn btn-primary" @click="next">Далее</button>
        <button v-else class="btn btn-primary" @click="finish">Завершить</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import http from '../api/http'
import StepBasicInfo from '../components/wizard/StepBasicInfo.vue'
import StepCompanies from '../components/wizard/StepCompanies.vue'
import StepManager from '../components/wizard/StepManager.vue'
import StepExecutors from '../components/wizard/StepExecutors.vue'
import StepDocuments from '../components/wizard/StepDocuments.vue'

const props = defineProps({ id: { type: String, default: null } })
const router = useRouter()

const stepLabels = ['Проект', 'Компании', 'Руководитель', 'Исполнители', 'Документы']
const step = ref(0)
const error = ref('')
const projectId = ref(props.id ? Number(props.id) : null)
const isEditing = computed(() => !!projectId.value)

const form = reactive({
  name: '',
  startDate: '',
  endDate: '',
  priority: 1,
  customerCompany: '',
  contractorCompany: '',
  managerId: null,
  employeeIds: []
})

function patchForm(partial) {
  Object.assign(form, partial)
}

function validateStep() {
  error.value = ''
  if (step.value === 0) {
    if (!form.name || !form.startDate || !form.endDate) { error.value = 'Заполните название и даты проекта'; return false }
    if (new Date(form.endDate) < new Date(form.startDate)) { error.value = 'Дата окончания раньше даты начала'; return false }
    if (!form.priority || form.priority < 1) { error.value = 'Приоритет должен быть целым числом больше нуля'; return false }
  }
  // Both company names are required by the server, so they are checked before leaving step 2.
  if (step.value === 1 && (!form.customerCompany || !form.contractorCompany)) {
    error.value = 'Укажите компанию-заказчика и компанию-исполнителя'
    return false
  }
  if (step.value === 2 && !form.managerId) { error.value = 'Выберите руководителя проекта'; return false }
  return true
}

async function next() {
  if (!validateStep()) return

  // Documents are attached to a project id, so the project has to exist on the
  // server before the last step is opened.
  if (step.value === 3) {
    await persistProject()
    if (error.value) return
  }
  step.value++
}

async function persistProject() {
  const payload = {
    name: form.name,
    startDate: form.startDate,
    endDate: form.endDate,
    priority: form.priority,
    customerCompany: form.customerCompany,
    contractorCompany: form.contractorCompany,
    managerId: form.managerId,
    employeeIds: form.employeeIds
  }
  try {
    if (isEditing.value) {
      await http.put(`/projects/${projectId.value}`, payload)
      await http.put(`/projects/${projectId.value}/employees`, { employeeIds: form.employeeIds })
    } else {
      const { data } = await http.post('/projects', payload)
      projectId.value = data.id
    }
  } catch (e) {
    error.value = e.response?.data?.message || 'Не удалось сохранить проект'
  }
}

function finish() {
  router.push('/projects')
}

onMounted(async () => {
  if (isEditing.value) {
    const { data } = await http.get(`/projects/${projectId.value}`)
    Object.assign(form, {
      name: data.name,
      startDate: data.startDate.substring(0, 10),
      endDate: data.endDate.substring(0, 10),
      priority: data.priority,
      customerCompany: data.customerCompany,
      contractorCompany: data.contractorCompany,
      managerId: data.managerId,
      employeeIds: data.employees.map((e) => e.id)
    })
  }
})
</script>

<style scoped>
.wizard-page h1 { font-size: 22px; margin-bottom: 20px; }
.steps { list-style: none; display: flex; gap: 8px; padding: 0; margin: 0 0 20px; }
.steps li { flex: 1; text-align: center; font-size: 12px; color: var(--color-muted); padding-bottom: 8px; border-bottom: 3px solid var(--color-border); display: flex; flex-direction: column; align-items: center; gap: 6px; }
.steps li.active { color: var(--color-primary-dark); border-color: var(--color-primary); font-weight: 600; }
.steps li.done { color: var(--color-success); border-color: var(--color-success); }
.step-num { width: 22px; height: 22px; border-radius: 50%; background: #eef1f6; display: flex; align-items: center; justify-content: center; font-size: 12px; }
.steps li.active .step-num { background: var(--color-primary); color: #fff; }
.steps li.done .step-num { background: var(--color-success); color: #fff; }
.step-body { min-height: 320px; display: flex; flex-direction: column; }
.step-actions { display: flex; justify-content: space-between; margin-top: auto; padding-top: 20px; }
.error { color: var(--color-danger); font-size: 13px; }
</style>
