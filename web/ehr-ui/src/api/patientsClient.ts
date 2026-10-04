import type { PatientSessionOption } from '../types/patients'

function tryApiErrorMessage(body: string): string | null {
  const t = body.trim()
  if (!t.startsWith('{') || !t.endsWith('}')) {
    return null
  }
  try {
    const o = JSON.parse(t) as { error?: string; title?: string; detail?: string }
    if (typeof o.error === 'string' && o.error.length > 0) {
      return o.error
    }
    if (typeof o.title === 'string' && o.title.length > 0) {
      return o.title + (typeof o.detail === 'string' && o.detail ? ` — ${o.detail}` : '')
    }
  } catch {
    /* not JSON */
  }
  return null
}

function friendlyHttpError(res: Response, text: string): Error {
  const fromJson = tryApiErrorMessage(text)
  if (fromJson) {
    return new Error(fromJson)
  }
  if (res.status === 404) {
    return new Error('That patient could not be found. Choose another patient and try again.')
  }
  return new Error('Something went wrong. Please try again.')
}

export async function listPatientsForSession(): Promise<PatientSessionOption[]> {
  const res = await fetch('/api/patients')
  const text = await res.text()
  if (!res.ok) {
    throw friendlyHttpError(res, text)
  }
  return JSON.parse(text) as PatientSessionOption[]
}
