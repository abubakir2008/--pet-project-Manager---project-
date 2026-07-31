<!-- Step 4: project executors (multiple selection, AJAX autocomplete). -->
<template>
  <div class="step">
    <h2>Исполнители проекта</h2>
    <p class="muted">Один сотрудник может работать одновременно на нескольких проектах.</p>
    <EmployeeAutocomplete
      :model-value="modelValue.employeeIds"
      :multiple="true"
      placeholder="Начните вводить имя сотрудника..."
      @update:model-value="(ids) => emit('update:modelValue', { employeeIds: ids })"
    />
  </div>
</template>

<script setup>
import EmployeeAutocomplete from '../EmployeeAutocomplete.vue'
defineProps({ modelValue: { type: Object, required: true } })
const emit = defineEmits(['update:modelValue'])
</script>

<style scoped>
.step { display: flex; flex-direction: column; gap: 10px; max-width: 480px; }
h2 { font-size: 16px; margin: 0 0 8px; }
</style>
