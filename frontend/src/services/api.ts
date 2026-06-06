import axios from 'axios'

let authToken: string | null = null

export function setAuthToken(token: string): void {
  authToken = token
}

export function clearAuthToken(): void {
  authToken = null
}

const api = axios.create({
  baseURL: 'https://localhost:5001/api/v1',
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  if (authToken) {
    config.headers.Authorization = `Bearer ${authToken}`
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      clearAuthToken()
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

export default api
