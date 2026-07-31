<!-- Step 3: project manager picked from all employees (AJAX autocomplete). -->
<template>
  <div class="step">
    <h2>Руководитель проекта</h2>
    <p class="muted">Начните вводить фамилию или email сотрудника, чтобы найти его на сервере.</p>
    <EmployeeAutocomplete
      :model-value="modelValue.managerId"
      placeholder="Выберите руководителя проекта"
      @update:model-value="(id) => emit('update:modelValue', { managerId: id })"
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
