<!-- Step 1: project name, start and end dates, priority. -->
<template>
  <div class="step">
    <h2>Основная информация</h2>
    <label>Название проекта</label>
    <input :value="modelValue.name" @input="update('name', $event.target.value)" placeholder="Например, Внедрение CRM" />

    <div class="row">
      <div>
        <label>Дата начала</label>
        <input type="date" :value="modelValue.startDate" @input="update('startDate', $event.target.value)" />
      </div>
      <div>
        <label>Дата окончания</label>
        <input type="date" :value="modelValue.endDate" @input="update('endDate', $event.target.value)" />
      </div>
    </div>

    <label>Приоритет (целое число, чем больше — тем важнее)</label>
    <input type="number" min="1" :value="modelValue.priority" @input="update('priority', Number($event.target.value))" style="max-width: 140px" />
  </div>
</template>

<script setup>
defineProps({ modelValue: { type: Object, required: true } })
const emit = defineEmits(['update:modelValue'])

// Every wizard step emits only the fields it owns; the parent merges the patch.
function update(key, value) {
  emit('update:modelValue', { [key]: value })
}
</script>

<style scoped>
.step { display: flex; flex-direction: column; gap: 10px; max-width: 480px; }
.row { display: flex; gap: 16px; }
.row > div { flex: 1; }
h2 { font-size: 16px; margin: 0 0 8px; }
</style>
