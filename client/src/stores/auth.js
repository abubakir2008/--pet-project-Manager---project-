import { defineStore } from 'pinia'
import http from '../api/http'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('pm_token') || null,
    employeeId: localStorage.getItem('pm_employeeId') || null,
    fullName: localStorage.getItem('pm_fullName') || '',
    role: localStorage.getItem('pm_role') || ''
  }),
  getters: {
    isAuthenticated: (state) => !!state.token,
    isDirector: (state) => state.role === 'Director',
    isProjectManager: (state) => state.role === 'ProjectManager',
    isEmployee: (state) => state.role === 'Employee'
  },
  actions: {
    async login(email, password) {
      const { data } = await http.post('/account/login', { email, password })
      this.token = data.token
      this.employeeId = data.employeeId
      this.fullName = data.fullName
      this.role = data.role
      localStorage.setItem('pm_token', data.token)
      localStorage.setItem('pm_employeeId', data.employeeId)
      localStorage.setItem('pm_fullName', data.fullName)
      localStorage.setItem('pm_role', data.role)
    },
    logout() {
      this.token = null
      this.employeeId = null
      this.fullName = ''
      this.role = ''
      localStorage.removeItem('pm_token')
      localStorage.removeItem('pm_employeeId')
      localStorage.removeItem('pm_fullName')
      localStorage.removeItem('pm_role')
    }
  }
})
