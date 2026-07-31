<!-- HTML5 file uploader with drag and drop (wizard step 5) -->
<template>
  <div
    class="dropzone"
    :class="{ dragging: isDragging }"
    @dragover.prevent="isDragging = true"
    @dragleave.prevent="isDragging = false"
    @drop.prevent="onDrop"
    @click="inputRef.click()"
  >
    <input ref="inputRef" type="file" multiple hidden @change="onSelect" />
    <p><strong>Перетащите файлы сюда</strong> или нажмите, чтобы выбрать</p>
    <p class="muted">PDF, Word, Excel, изображения, ZIP — до 25 МБ</p>
  </div>

  <ul v-if="files.length" class="file-list">
    <li v-for="(f, idx) in files" :key="idx">
      <span>{{ f.fileName || f.name }}</span>
      <span class="muted">{{ formatSize(f.sizeBytes ?? f.size) }}</span>
      <button type="button" class="btn btn-secondary btn-sm" @click="removeFile(idx)">Удалить</button>
    </li>
  </ul>
</template>

<script setup>
import { ref } from 'vue'

// files holds the documents already stored on the server; the parent component
// decides what to do with the File objects emitted by add-files (see StepDocuments.vue).
const props = defineProps({ files: { type: Array, default: () => [] } })
const emit = defineEmits(['add-files', 'remove-file'])

const isDragging = ref(false)
const inputRef = ref(null)

function onDrop(e) {
  isDragging.value = false
  emit('add-files', Array.from(e.dataTransfer.files))
}

function onSelect(e) {
  emit('add-files', Array.from(e.target.files))
  e.target.value = ''
}

function removeFile(idx) {
  emit('remove-file', idx)
}

function formatSize(bytes) {
  if (!bytes) return ''
  const kb = bytes / 1024
  return kb < 1024 ? `${kb.toFixed(0)} КБ` : `${(kb / 1024).toFixed(1)} МБ`
}
</script>

<style scoped>
.dropzone {
  border: 2px dashed var(--color-border);
  border-radius: var(--radius);
  padding: 32px;
  text-align: center;
  cursor: pointer;
  transition: border-color .15s, background .15s;
  color: var(--color-muted);
}
.dropzone.dragging { border-color: var(--color-primary); background: #eaf0fb; color: var(--color-primary-dark); }
.dropzone p { margin: 4px 0; }
.file-list { list-style: none; padding: 0; margin: 16px 0 0; display: flex; flex-direction: column; gap: 6px; }
.file-list li { display: flex; align-items: center; gap: 12px; font-size: 14px; padding: 6px 10px; background: #f7f9fc; border-radius: 6px; }
.file-list li span:first-child { flex: 1; }
.btn-sm { padding: 4px 10px; font-size: 12px; }
</style>
