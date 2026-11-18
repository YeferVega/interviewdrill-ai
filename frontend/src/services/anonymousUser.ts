const STORAGE_KEY = 'interviewdrill:anonymousUserId'

function createUserId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID()
  }

  return `user-${Date.now()}-${Math.random().toString(16).slice(2)}`
}

export function getPersistentUserId(): string {
  try {
    const existing = localStorage.getItem(STORAGE_KEY)

    if (existing) {
      return existing
    }

    const created = createUserId()
    localStorage.setItem(STORAGE_KEY, created)
    return created
  } catch {
    return createUserId()
  }
}
