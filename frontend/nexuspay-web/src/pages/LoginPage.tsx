import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useState } from 'react'
import { useAuth } from '../App'
import { ApiError } from '../services/api'
import { Button, Field, inputClass } from '../components/ui'

const schema = z.object({ email: z.email('Ingresa un correo válido.'), password: z.string().min(1, 'Ingresa tu contraseña.') })
type FormData = z.infer<typeof schema>

export function LoginPage() {
  const { login } = useAuth()
  const [error, setError] = useState('')
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormData>({ resolver: zodResolver(schema) })
  const submit = handleSubmit(async values => {
    setError('')
    try { await login(values.email, values.password) }
    catch (reason) { setError(reason instanceof ApiError && reason.status === 401 ? 'Correo o contraseña incorrectos.' : reason instanceof Error ? reason.message : 'No se pudo iniciar sesión.') }
  })
  return <main className="grid min-h-screen bg-[#101c32] lg:grid-cols-[1.1fr_.9fr]">
    <section className="relative hidden overflow-hidden p-12 text-white lg:flex lg:flex-col lg:justify-between xl:p-20">
      <div className="absolute -left-24 -top-24 size-96 rounded-full border border-cyan-300/10"/><div className="absolute -left-12 -top-12 size-72 rounded-full border border-cyan-300/10"/>
      <div className="relative flex items-center gap-3"><span className="grid size-11 place-items-center rounded-xl bg-cyan-300 text-xl font-black text-[#101c32]">N</span><div><b className="text-lg">NexusPay</b><p className="text-[10px] uppercase tracking-[.2em] text-slate-400">Financial operations</p></div></div>
      <div className="relative max-w-xl"><p className="mb-4 text-xs font-bold uppercase tracking-[.2em] text-cyan-300">Control financiero, sin fricción</p><h1 className="text-5xl font-semibold leading-tight">Todas tus operaciones, en un solo lugar.</h1><p className="mt-6 max-w-md leading-7 text-slate-300">Un centro de control para operaciones simuladas, clientes e integraciones de prueba.</p></div>
      <p className="relative text-xs text-slate-500">Entorno de demostración · No procesa dinero real</p>
    </section>
    <section className="flex items-center justify-center bg-white px-6 py-12">
      <form onSubmit={submit} noValidate className="w-full max-w-md">
        <div className="mb-10 lg:hidden"><span className="grid size-11 place-items-center rounded-xl bg-[#101c32] text-xl font-black text-cyan-300">N</span></div>
        <p className="text-xs font-bold uppercase tracking-[.16em] text-cyan-700">Bienvenido</p><h2 className="mt-2 text-3xl font-bold tracking-tight">Inicia sesión</h2><p className="mt-2 text-sm text-slate-500">Accede a tu consola NexusPay.</p>
        <div className="mt-8 space-y-5">
          <Field label="Correo electrónico" error={errors.email?.message}><input {...register('email')} autoComplete="username" className={inputClass} placeholder="nombre@empresa.com" type="email" /></Field>
          <Field label="Contraseña" error={errors.password?.message}><input {...register('password')} autoComplete="current-password" className={inputClass} placeholder="Tu contraseña" type="password" /></Field>
          {error && <p role="alert" className="rounded-lg bg-rose-50 px-3 py-2 text-sm text-rose-700">{error}</p>}
          <Button className="w-full" disabled={isSubmitting} type="submit">{isSubmitting ? 'Validando…' : 'Entrar a NexusPay'}</Button>
        </div>
        <p className="mt-8 text-center text-xs text-slate-400">Las credenciales iniciales se guardan en .NET User Secrets.</p>
      </form>
    </section>
  </main>
}
