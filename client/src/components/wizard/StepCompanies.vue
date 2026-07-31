<!-- Step 2: customer company and contractor company. -->
<template>
  <div class="step">
    <h2>Компании</h2>
    <label>Компания-заказчик</label>
    <input :value="modelValue.customerCompany" @input="update('customerCompany', $event.target.value)" placeholder="ООО «Заказчик»" />

    <label>Компания-исполнитель</label>
    <input :value="modelValue.contractorCompany" @input="update('contractorCompany', $event.target.value)" placeholder="Sibers" />
  </div>
</template>

<script setup>
const props = defineProps({ modelValue: { type: Object, required: true } })
const emit = defineEmits(['update:modelValue'])
function update(key, value) {
  emit('update:modelValue', { [key]: value })
}
</script>

<style scoped>
.step { display: flex; flex-direction: column; gap: 10px; max-width: 480px; }
h2 { font-size: 16px; margin: 0 0 8px; }
</style>
