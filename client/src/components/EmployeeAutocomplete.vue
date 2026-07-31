<!--
  Employee dropdown with partial text input and a debounced AJAX search on the server.
  Supports a single selection (project manager) and a multiple one (project executors).
-->
<template>
  <div class="autocomplete" ref="root">
    <input
      v-model="query"
      type="text"
      :placeholder="placeholder"
      @focus="open = true"
      @input="onInput"
    />

    <div v-if="open && (results.length || loading)" class="dropdown">
      <div v-if="loading" class="dropdown-item muted">Поиск...</div>
      <div
        v-for="emp in results"
        :key="emp.id"
        class="dropdown-item"
        @click="select(emp)"
      >
        {{ emp.lastName }} {{ emp.firstName }} {{ emp.middleName }}
        <span class="muted"> — {{ emp.email }}</span>
      </div>
      <div v-if="!loading && results.length === 0" class="dropdown-item muted">
        Ничего не найдено
      </div>
    </div>

    <div v-if="multiple && selectedItems.length" class="chips">
      <span v-for="emp in selectedItems" :key="emp.id" class="chip">
        {{ emp.lastName }} {{ emp.firstName }}
        <button type="button" @click="remove(emp)">&times;</button>
      </span>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount, watch } from "vue";
import http from "../api/http";

const props = defineProps({
  modelValue: { type: [String, Array], default: null }, // an id string, or an array of ids
  multiple: { type: Boolean, default: false },
  placeholder: { type: String, default: "Начните вводить имя сотрудника..." },
});
const emit = defineEmits(["update:modelValue"]);

const query = ref("");
const results = ref([]);
const loading = ref(false);
const open = ref(false);
const root = ref(null);
const selectedItems = ref([]);
let debounceTimer = null;

async function search(term) {
  loading.value = true;
  try {
    const { data } = await http.get("/employees", {
      params: { search: term, take: 10 },
    });
    results.value = data;
  } finally {
    loading.value = false;
  }
}

function onInput() {
  open.value = true;
  clearTimeout(debounceTimer);
  debounceTimer = setTimeout(() => search(query.value), 300);
}

function select(emp) {
  if (props.multiple) {
    if (!selectedItems.value.find((e) => e.id === emp.id)) {
      selectedItems.value.push(emp);
      emit(
        "update:modelValue",
        selectedItems.value.map((e) => e.id),
      );
    }
    query.value = "";
  } else {
    query.value = `${emp.lastName} ${emp.firstName}`;
    emit("update:modelValue", emp.id);
  }
  open.value = false;
}

function remove(emp) {
  selectedItems.value = selectedItems.value.filter((e) => e.id !== emp.id);
  emit(
    "update:modelValue",
    selectedItems.value.map((e) => e.id),
  );
}

function onClickOutside(e) {
  if (root.value && !root.value.contains(e.target)) open.value = false;
}

onMounted(() => {
  document.addEventListener("click", onClickOutside);
  search("");
});
onBeforeUnmount(() => document.removeEventListener("click", onClickOutside));

watch(
  () => props.modelValue,
  async (val) => {
    if (
      props.multiple &&
      Array.isArray(val) &&
      val.length &&
      selectedItems.value.length === 0
    ) {
      const { data } = await http.get("/employees", { params: { take: 100 } });
      selectedItems.value = data.filter((e) => val.includes(e.id));
    }
  },
  { immediate: true },
);
</script>

<style scoped>
.autocomplete {
  position: relative;
}
.dropdown {
  position: absolute;
  z-index: 20;
  top: 100%;
  left: 0;
  right: 0;
  background: #fff;
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  margin-top: 4px;
  max-height: 220px;
  overflow-y: auto;
  box-shadow: 0 8px 20px rgba(0, 0, 0, 0.08);
}
.dropdown-item {
  padding: 8px 12px;
  font-size: 14px;
  cursor: pointer;
}
.dropdown-item:hover {
  background: #f0f4fb;
}
.muted {
  color: var(--color-muted);
  font-size: 12px;
}
.chips {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 10px;
}
.chip {
  background: #eaf0fb;
  color: var(--color-primary-dark);
  border-radius: 14px;
  padding: 4px 8px 4px 12px;
  font-size: 13px;
  display: inline-flex;
  align-items: center;
  gap: 6px;
}
.chip button {
  border: none;
  background: transparent;
  font-size: 14px;
  line-height: 1;
  color: var(--color-primary-dark);
}
</style>
