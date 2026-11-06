export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5062'
let accessToken: string | null = null
export const setAccessToken = (token: string | null) => { accessToken = token }

export class ApiError extends Error {
  readonly status: number
  readonly code?: string
  constructor(message: string, status: number, code?: string) { super(message); this.status = status; this.code = code; this.name = 'ApiError' }
}

type ApiOptions = RequestInit & { anonymous?: boolean }
export async function api<T>(path: string, options: ApiOptions = {}): Promise<T> {
  const { anonymous, headers, ...init } = options
  let response: Response
  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      ...init,
      headers: {
        Accept: 'application/json',
        ...(init.body ? { 'Content-Type': 'application/json' } : {}),
        ...(!anonymous && accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
        ...headers,
      },
    })
  } catch (error) {
    if (error instanceof TypeError) throw new ApiError('No se pudo conectar con la API. Comprueba que el backend esté iniciado e inténtalo de nuevo.', 0, 'api_unreachable')
    throw error
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string; detail?: string; code?: string; errors?: Record<string, string[]> } | null
    if (response.status === 401 && !anonymous && accessToken)
      throw new ApiError('Tu sesión expiró o dejó de ser válida. Inicia sesión nuevamente y vuelve a intentar; si era un registro o cambio, no se guardó.', 401, 'session_expired')
    const validationMessage = Object.values(problem?.errors ?? {}).flat().filter(Boolean).join(' ')
    const message = problem?.detail?.trim() || problem?.title?.trim() || validationMessage || `Error HTTP ${response.status}`
    throw new ApiError(message, response.status, problem?.code)
  }
  if (response.status === 204) return undefined as T
  return await response.json() as T
}

export async function downloadCsv(path: string, filename: string) {
  let response: Response
  try {
    response = await fetch(`${apiBaseUrl}${path}`, { headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : {} })
  } catch (error) {
    if (error instanceof TypeError) throw new ApiError('No se pudo conectar con la API. Comprueba que el backend esté iniciado e inténtalo de nuevo.', 0, 'api_unreachable')
    throw error
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { title?: string } | null
    if (response.status === 401 && accessToken)
      throw new ApiError('Tu sesión expiró o dejó de ser válida. Inicia sesión nuevamente; la descarga no se realizó.', 401, 'session_expired')
    throw new ApiError(problem?.title?.trim() || `Error HTTP ${response.status}`, response.status)
  }
  const url = URL.createObjectURL(await response.blob())
  const anchor = document.createElement('a')
  anchor.href = url; anchor.download = filename; anchor.click(); URL.revokeObjectURL(url)
}

export type Page<T> = { items: T[]; totalCount: number; page: number; pageSize: number }
export type Customer = { id: string; customerCode: string; firstName: string; lastName: string; email: string; phone: string | null; documentType: string | null; documentNumber: string | null; isActive: boolean; createdAtUtc: string }
export type Operation = { id: string; operationCode: string; customerId: string; type: string; amount: number; currency: string; status: string; description: string | null; processingAttempts: number; providerCode: string | null; providerReference: string | null; failureCode: string | null; createdAtUtc: string; processedAtUtc: string | null }
export type Provider = { id: string; providerCode: string; name: string; type: string; isEnabled: boolean; isAvailable: boolean }
export const dateRange = () => {
  const now = new Date(), start = new Date(now)
  start.setDate(start.getDate() - 30)
  return { from: start.toISOString(), to: now.toISOString() }
}
export const money = (amount: number, currency: string) => new Intl.NumberFormat('es-PE', { style: 'currency', currency, maximumFractionDigits: 2 }).format(amount)
