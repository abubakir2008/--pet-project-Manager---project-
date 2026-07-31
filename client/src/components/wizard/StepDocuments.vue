<!-- Step 5: project documents, HTML5 file uploader with drag and drop. -->
<template>
  <div class="step">
    <h2>Документы проекта</h2>
    <p v-if="!projectId" class="muted">Сначала будет сохранён проект, затем можно будет прикрепить файлы.</p>

    <FileDropUploader :files="documents" @add-files="uploadFiles" @remove-file="removeFile" />

    <p v-if="uploading" class="muted">Загрузка файлов...</p>
    <p v-if="error" class="error">{{ error }}</p>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import http from '../../api/http'
import FileDropUploader from '../FileDropUploader.vue'

const props = defineProps({ projectId: { type: Number, default: null } })

const documents = ref([])
const uploading = ref(false)
const error = ref('')

async function loadDocuments() {
  if (!props.projectId) return
  const { data } = await http.get(`/projects/${props.projectId}`)
  documents.value = data.documents
}

async function uploadFiles(fileList) {
  if (!props.projectId) {
    error.value = 'Проект ещё не сохранён'
    return
  }
  error.value = ''
  uploading.value = true
  try {
    for (const file of fileList) {
      const formData = new FormData()
      formData.append('file', file)
      const { data } = await http.post(`/projects/${props.projectId}/documents`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      documents.value.push(data)
    }
  } catch (e) {
    error.value = e.response?.data?.message || 'Не удалось загрузить файл'
  } finally {
    uploading.value = false
  }
}

async function removeFile(idx) {
  const doc = documents.value[idx]
  if (!doc?.id) return
  await http.delete(`/projects/${props.projectId}/documents/${doc.id}`)
  documents.value.splice(idx, 1)
}

watch(() => props.projectId, loadDocuments, { immediate: true })
</script>

<style scoped>
.step { display: flex; flex-direction: column; gap: 10px; }
h2 { font-size: 16px; margin: 0 0 8px; }
</style>
