import { jsPDF } from 'jspdf'
import type { PipelineResult } from '../types/pipeline'

export function downloadSoapPdf(result: PipelineResult, fileName = 'visit-note.pdf') {
  const { soap, coding, cleanedTranscript } = result
  const text = [
    soap.chiefComplaint ? soap.chiefComplaint : 'Visit note',
    '',
    'S (Subjective)',
    soap.subjective,
    '',
    'O (Objective)',
    soap.objective,
    '',
    'A (Assessment)',
    soap.assessment,
    '',
    'P (Plan)',
    soap.plan,
    '',
    '--- Conversation ---',
    cleanedTranscript,
    '',
    'ICD-10-CM',
    `Primary: ${coding.primaryIcd10}`,
    ...(coding.additionalNotes ? [coding.additionalNotes, ''] : []),
    'CPT / procedures',
    ...coding.lineItems.map(
      (l) =>
        `• ${l.cptCode}${l.quantity > 1 ? ` ×${l.quantity}` : ''} — ${l.icd10Codes.join(', ')} | ${l.reasoning}`
    ),
  ].join('\n')

  const doc = new jsPDF({ unit: 'pt', format: 'letter' })
  const margin = 48
  const lineHeight = 12
  const pageW = doc.internal.pageSize.getWidth()
  const pageH = doc.internal.pageSize.getHeight()
  const maxW = pageW - margin * 2
  const lines = doc.splitTextToSize(text, maxW)
  doc.setFontSize(10)
  let y = margin
  for (const line of lines) {
    if (y + lineHeight > pageH - margin) {
      doc.addPage()
      y = margin
    }
    doc.text(line, margin, y)
    y += lineHeight
  }
  doc.save(fileName)
}
