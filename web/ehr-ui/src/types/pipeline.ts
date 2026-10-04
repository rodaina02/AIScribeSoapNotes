export interface PipelineResult {
  encounterId: string
  soapNoteId: string
  cleanedTranscript: string
  soap: {
    subjective: string
    objective: string
    assessment: string
    plan: string
    chiefComplaint: string | null
  }
  coding: {
    primaryIcd10: string
    lineItems: Array<{
      cptCode: string
      icd10Codes: string[]
      quantity: number
      modifier1: string
      modifier2: string
      reasoning: string
    }>
    additionalNotes: string
  }
  medicalCodeLinksCreated: number
  medicalCodesCatalogEntriesCreated: number
}
