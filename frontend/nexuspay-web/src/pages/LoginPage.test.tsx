import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from '../App'

describe('LoginPage', () => {
  beforeEach(() => {
    sessionStorage.clear()
    window.history.replaceState({}, '', '/login')
  })
  afterEach(() => vi.unstubAllGlobals())

  it('renders the local sign-in form', () => {
    render(<App />)
    expect(screen.getByRole('heading', { name: 'Inicia sesión' })).toBeInTheDocument()
    expect(screen.getByLabelText('Correo electrónico')).toBeInTheDocument()
    expect(screen.getByLabelText('Contraseña')).toBeInTheDocument()
  })

  it('validates a malformed email before calling the API', async () => {
    const fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)
    const user = userEvent.setup()
    render(<App />)
    await user.type(screen.getByLabelText('Correo electrónico'), 'no-es-correo')
    await user.type(screen.getByLabelText('Contraseña'), 'password')
    await user.click(screen.getByRole('button', { name: 'Entrar a NexusPay' }))
    expect(await screen.findByText('Ingresa un correo válido.')).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('shows a useful message when credentials are rejected', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(
      JSON.stringify({ title: 'Invalid email or password' }),
      { status: 401, headers: { 'Content-Type': 'application/problem+json' } },
    )))
    const user = userEvent.setup()
    render(<App />)
    await user.type(screen.getByLabelText('Correo electrónico'), 'admin@example.test')
    await user.type(screen.getByLabelText('Contraseña'), 'WrongPassword!1')
    await user.click(screen.getByRole('button', { name: 'Entrar a NexusPay' }))
    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Correo o contraseña incorrectos.'))
  })
})
