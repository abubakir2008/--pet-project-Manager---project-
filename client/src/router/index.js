import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const routes = [
  { path: '/login', name: 'login', component: () => import('../views/LoginView.vue'), meta: { public: true } },
  { path: '/', redirect: '/projects' },
  { path: '/projects', name: 'projects', component: () => import('../views/ProjectsListView.vue') },
  { path: '/projects/new', name: 'project-wizard-new', component: () => import('../views/ProjectWizardView.vue') },
  { path: '/projects/:id/edit', name: 'project-wizard-edit', component: () => import('../views/ProjectWizardView.vue'), props: true },
  { path: '/tasks', name: 'tasks', component: () => import('../views/TasksListView.vue') },
  {
    path: '/employees', name: 'employees', component: () => import('../views/EmployeesListView.vue'),
    meta: { roles: ['Director'] }
  }
]

const router = createRouter({ history: createWebHistory(), routes })

router.beforeEach((to) => {
  const auth = useAuthStore()

  if (!to.meta.public && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.meta.roles && !to.meta.roles.includes(auth.role)) {
    return { name: 'projects' }
  }

  if (to.name === 'login' && auth.isAuthenticated) {
    return { name: 'projects' }
  }

  return true
})

export default router
