import { useEffect, useState, createContext, useContext, lazy, Suspense } from 'react'
import { BrowserRouter, Navigate, NavLink, Outlet, Route, Routes, useNavigate } from 'react-router'
import { LoginPage } from './pages/LoginPage'
import { api, ApiError, setAccessToken } from './services/api'

const DashboardPage = lazy(() => import('./pages/DashboardPage').then(module => ({ default: module.DashboardPage })))
const CustomersPage = lazy(() => import('./pages/CustomersPage').then(module => ({ default: module.CustomersPage })))
const OperationsPage = lazy(() => import('./pages/OperationsPage').then(module => ({ default: module.OperationsPage })))
const ProvidersPage = lazy(() => import('./pages/ProvidersPage').then(module => ({ default: module.ProvidersPage })))
const LogsPage = lazy(() => import('./pages/ManagementPages').then(module => ({ default: module.LogsPage })))
const ReportsPage = lazy(() => import('./pages/ManagementPages').then(module => ({ default: module.ReportsPage })))
const UsersPage = lazy(() => import('./pages/ManagementPages').then(module => ({ default: module.UsersPage })))

export type User = { id: string; email: string; roles: string[] }
type AuthState = { user: User | null; ready: boolean; login: (email: string, password: string) => Promise<void>; logout: () => void }
const AuthContext = createContext<AuthState | null>(null)
// oxlint-disable-next-line react/only-export-components -- shared auth hook is used by routed screens.
export const useAuth = () => {
  const value = useContext(AuthContext)
  if (!value) throw new Error('useAuth must be used inside AuthContext')
  return value
}

function AppRoutes() {
  const [token, setToken] = useState(() => sessionStorage.getItem('nexuspay-token'))
  const [user, setUser] = useState<User | null>(null)
  const [ready, setReady] = useState(() => !sessionStorage.getItem('nexuspay-token'))
  const navigate = useNavigate()

  useEffect(() => {
    setAccessToken(token)
    if (!token) return
    void api<User>('/api/auth/me').then(setUser).catch(() => {
      sessionStorage.removeItem('nexuspay-token'); setAccessToken(null); setToken(null); setUser(null)
    }).finally(() => setReady(true))
  }, [token])

  const auth: AuthState = {
    user, ready,
    async login(email, password) {
      const result = await api<{ accessToken: string; user: User }>('/api/auth/login', {
        method: 'POST', body: JSON.stringify({ email, password }), anonymous: true,
      })
      sessionStorage.setItem('nexuspay-token', result.accessToken)
      setAccessToken(result.accessToken); setToken(result.accessToken); setUser(result.user)
    },
    logout() {
      sessionStorage.removeItem('nexuspay-token'); setAccessToken(null); setToken(null); setUser(null); navigate('/login')
    },
  }

  return <AuthContext.Provider value={auth}>
    {!ready ? <div className="grid min-h-screen place-items-center text-sm text-slate-400">Cargando sesión…</div> :
      <Suspense fallback={<div className="grid min-h-[50vh] place-items-center text-sm text-slate-400">Cargando módulo…</div>}><Routes>
        <Route path="/login" element={user ? <Navigate to="/" replace /> : <LoginPage />} />
        <Route element={user ? <AppShell /> : <Navigate to="/login" replace />}>
          <Route index element={<DashboardPage />} />
          <Route path="customers" element={<CustomersPage />} />
          <Route path="operations" element={<OperationsPage />} />
          <Route path="providers" element={<ProvidersPage />} />
          <Route path="audit" element={<LogsPage />} />
          <Route path="reports" element={<ReportsPage />} />
          <Route path="users" element={<UsersPage />} />
        </Route>
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes></Suspense>}
  </AuthContext.Provider>
}

const navigation = [
  { to: '/', label: 'Resumen', icon: '◫', roles: ['Administrator', 'Operator', 'Auditor'] },
  { to: '/customers', label: 'Clientes', icon: '◉', roles: ['Administrator', 'Operator', 'Auditor'] },
  { to: '/operations', label: 'Operaciones', icon: '↗', roles: ['Administrator', 'Operator', 'Auditor'] },
  { to: '/providers', label: 'Proveedores', icon: '⌘', roles: ['Administrator', 'Operator', 'Auditor'] },
  { to: '/audit', label: 'Auditoría y logs', icon: '▤', roles: ['Administrator', 'Auditor'] },
  { to: '/reports', label: 'Reportes', icon: '▥', roles: ['Administrator', 'Auditor'] },
  { to: '/users', label: 'Usuarios', icon: '♙', roles: ['Administrator'] },
]

function AppShell() {
  const { user, logout } = useAuth()
  return <div className="min-h-screen bg-[#f4f7fb] text-slate-900">
    <aside className="fixed inset-y-0 left-0 z-20 hidden w-64 flex-col bg-[#101c32] text-white lg:flex">
      <div className="flex h-[76px] items-center gap-3 border-b border-white/10 px-6">
        <span className="grid size-10 place-items-center rounded-xl bg-cyan-300 text-lg font-black text-[#101c32]">N</span>
        <div><p className="font-bold tracking-wide">NexusPay</p><p className="text-[10px] uppercase tracking-[.18em] text-slate-400">Operations console</p></div>
      </div>
      <nav aria-label="Navegación principal" className="flex-1 space-y-1 px-3 py-6">
        {navigation.filter(item => item.roles.some(role => user?.roles.includes(role))).map(item =>
          <NavLink key={item.to} to={item.to} end={item.to === '/'} className={({ isActive }) =>
            `flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm transition ${isActive ? 'bg-cyan-300 font-semibold text-[#101c32]' : 'text-slate-300 hover:bg-white/10 hover:text-white'}`}>
            <span className="w-5 text-center text-base">{item.icon}</span>{item.label}
          </NavLink>)}
      </nav>
      <div className="border-t border-white/10 p-4">
        <p className="truncate text-sm font-medium">{user?.email}</p><p className="mt-1 text-xs text-slate-400">{user?.roles.join(' · ')}</p>
        <button onClick={logout} className="mt-4 w-full rounded-lg border border-white/15 px-3 py-2 text-left text-sm text-slate-300 hover:bg-white/10">Cerrar sesión</button>
      </div>
    </aside>
    <div className="lg:pl-64">
      <header className="sticky top-0 z-10 flex h-[76px] items-center justify-between border-b border-slate-200 bg-white/90 px-5 backdrop-blur sm:px-8">
        <div className="flex items-center gap-3 lg:hidden"><span className="grid size-9 place-items-center rounded-lg bg-[#101c32] font-bold text-cyan-300">N</span><b>NexusPay</b></div>
        <p className="hidden text-sm text-slate-500 lg:block">Panel de operaciones financieras simuladas</p>
        <div className="flex items-center gap-3"><span className="size-2 rounded-full bg-emerald-500"/><span className="text-xs font-medium text-slate-600">Entorno local</span><button onClick={logout} className="text-sm font-medium text-slate-600 lg:hidden">Salir</button></div>
      </header>
      <nav aria-label="Navegación móvil" className="flex gap-1 overflow-x-auto border-b border-slate-200 bg-white px-3 py-2 lg:hidden">
        {navigation.filter(item => item.roles.some(role => user?.roles.includes(role))).map(item => <NavLink key={item.to} to={item.to} end={item.to === '/'} className={({ isActive }) => `shrink-0 rounded-md px-3 py-2 text-xs ${isActive ? 'bg-cyan-50 font-semibold text-cyan-800' : 'text-slate-500'}`}>{item.label}</NavLink>)}
      </nav>
      <a href="#main-content" className="sr-only z-50 rounded bg-white p-3 text-slate-900 focus:not-sr-only focus:fixed focus:left-4 focus:top-4">Saltar al contenido</a>
      <main id="main-content" tabIndex={-1} className="mx-auto max-w-[1440px] p-5 outline-none focus-visible:ring-2 focus-visible:ring-cyan-700 sm:p-8"><Outlet /></main>
      <footer className="px-8 pb-6 text-center text-xs text-slate-400">NexusPay · Datos ficticios · No se procesa dinero real</footer>
    </div>
  </div>
}

export default function App() { return <BrowserRouter><AppRoutes /></BrowserRouter> }

export function ErrorNotice({ error }: { error: unknown }) {
  const message = error instanceof ApiError ? error.message : error instanceof Error ? error.message : 'Ocurrió un error inesperado.'
  return <div role="alert" aria-live="assertive" className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-800">{message}</div>
}
