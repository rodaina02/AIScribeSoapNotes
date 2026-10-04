export type PatientSessionOption = {
  patientId: string
  tenantId: string
  firstName: string
  lastName: string
  dateOfBirth: string
}

export function patientDisplayName(p: PatientSessionOption): string {
  return `${p.lastName}, ${p.firstName}`
}
