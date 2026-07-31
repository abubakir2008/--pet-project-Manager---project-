<template>
  <div v-if="auth.isAuthenticated" class="layout">
    <aside class="sidebar">
      <div class="brand">Управление проектами</div>
      <nav>
        <router-link to="/projects">Проекты</router-link>
        <router-link to="/tasks">Задачи</router-link>
        <router-link v-if="auth.isDirector" to="/employees">Сотрудники</router-link>
      </nav>
      <div class="user-box">
        <div class="user-name">{{ auth.fullName }}</div>
        <div class="user-role">{{ roleLabel }}</div>
        <button class="btn btn-secondary" @click="logout">Выйти</button>
      </div>
    </aside>
    <main class="content">
      <router-view />
    </main>
  </div>
  <router-view v-else />
</template>

<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'

const auth = useAuthStore()
const router = useRouter()

const roleLabel = computed(() => ({
  Director: 'Руководитель',
  ProjectManager: 'Менеджер проекта',
  Employee: 'Сотрудник'
}[auth.role] || auth.role))

function logout() {
  auth.logout()
  router.push('/login')
}
</script>

<style>
.layout { display: flex; min-height: 100vh; }
.sidebar {
  width: 240px;
  background: #1c2536;
  color: #fff;
  display: flex;
  flex-direction: column;
  padding: 20px 16px;
}
.brand { font-weight: 700; font-size: 16px; margin-bottom: 24px; }
.sidebar nav { display: flex; flex-direction: column; gap: 4px; flex: 1; }
.sidebar nav a {
  color: #cbd3e1;
  text-decoration: none;
  padding: 10px 12px;
  border-radius: 6px;
  font-size: 14px;
}
.sidebar nav a.router-link-active { background: #2f5aa8; color: #fff; }
.sidebar nav a:hover { background: #2a3651; }
.user-box { border-top: 1px solid #33405c; padding-top: 12px; }
.user-name { font-size: 14px; font-weight: 600; }
.user-role { font-size: 12px; color: #9aa6bd; margin-bottom: 10px; }
.content { flex: 1; padding: 28px 32px; max-width: 1200px; }
</style>
