<template>
  <div class="login-page">
    <form class="card login-card" @submit.prevent="submit">
      <h1>Вход в систему</h1>
      <p class="hint">Тестовая учётная запись: director@sibers.local / Director123!</p>

      <label>Email</label>
      <input v-model="email" type="email" required autofocus />

      <label>Пароль</label>
      <input v-model="password" type="password" required />

      <p v-if="error" class="error">{{ error }}</p>

      <button class="btn btn-primary" type="submit" :disabled="loading">
        {{ loading ? 'Вход...' : 'Войти' }}
      </button>
    </form>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const email = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

async function submit() {
  error.value = ''
  loading.value = true
  try {
    await auth.login(email.value, password.value)
    router.push(route.query.redirect || '/projects')
  } catch (e) {
    error.value = e.response?.data?.message || 'Не удалось выполнить вход'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #1c2536, #2f5aa8);
}
.login-card { width: 360px; display: flex; flex-direction: column; gap: 10px; }
.login-card h1 { font-size: 20px; margin: 0 0 4px; }
.hint { font-size: 12px; color: var(--color-muted); margin: 0 0 8px; }
.error { color: var(--color-danger); font-size: 13px; }
</style>
