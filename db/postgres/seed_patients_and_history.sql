-- =============================================================================
-- Additive seed: extra patients + visit history (SOAP + transcripts)
-- Safe to re-run: inserts use fixed UUIDs and ON CONFLICT DO NOTHING.
-- Requires the base tenant/facility from ehr_db_reset_and_seed.sql.
--
--   psql -U postgres -d ehr_db -f db/postgres/seed_patients_and_history.sql
-- =============================================================================

\set ON_ERROR_STOP on

INSERT INTO public.patients (
  id, tenant_id, first_name, last_name, date_of_birth, gender, medical_record_number, facility_id, is_active
) VALUES
(
  'a0000000-0000-0000-0000-000000000003',
  'a0000000-0000-0000-0000-000000000001',
  'Demo',
  'Patient',
  '1980-05-15',
  1,
  'MRN-1003',
  'a0000000-0000-0000-0000-000000000002',
  true
),
(
  'a0000000-0000-0000-0000-000000000013',
  'a0000000-0000-0000-0000-000000000001',
  'Elena',
  'Vasquez',
  '1972-03-08',
  2,
  'MRN-1013',
  'a0000000-0000-0000-0000-000000000002',
  true
),
(
  'a0000000-0000-0000-0000-000000000023',
  'a0000000-0000-0000-0000-000000000001',
  'James',
  'Okonkwo',
  '1965-11-22',
  1,
  'MRN-1023',
  'a0000000-0000-0000-0000-000000000002',
  true
),
(
  'a0000000-0000-0000-0000-000000000033',
  'a0000000-0000-0000-0000-000000000001',
  'Priya',
  'Shah',
  '1991-07-19',
  2,
  'MRN-1033',
  'a0000000-0000-0000-0000-000000000002',
  true
),
(
  'a0000000-0000-0000-0000-000000000043',
  'a0000000-0000-0000-0000-000000000001',
  'Robert',
  'Chen',
  '1958-01-30',
  1,
  'MRN-1043',
  'a0000000-0000-0000-0000-000000000002',
  true
)
ON CONFLICT (id) DO NOTHING;

INSERT INTO public.encounters (
  id,
  tenant_id,
  patient_id,
  facility_id,
  attending_provider_user_id,
  service_date,
  scheduled_time,
  status,
  type,
  chief_complaint
) VALUES
(
  'a0000000-0000-0000-0000-000000000004',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000003',
  'a0000000-0000-0000-0000-000000000002',
  'a0000000-0000-0000-0000-0000000000ff',
  CURRENT_DATE - 14,
  '09:15:00',
  2,
  0,
  'Lower back pain and stiffness'
),
(
  'a0000000-0000-0000-0000-000000000014',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000013',
  'a0000000-0000-0000-0000-000000000002',
  'a0000000-0000-0000-0000-0000000000ff',
  CURRENT_DATE - 40,
  '10:00:00',
  2,
  1,
  'Right knee osteoarthritis follow-up'
),
(
  'a0000000-0000-0000-0000-000000000015',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000013',
  'a0000000-0000-0000-0000-000000000002',
  'a0000000-0000-0000-0000-0000000000ff',
  CURRENT_DATE - 7,
  '14:30:00',
  2,
  1,
  'Knee pain after physical therapy'
),
(
  'a0000000-0000-0000-0000-000000000024',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000023',
  'a0000000-0000-0000-0000-000000000002',
  'a0000000-0000-0000-0000-0000000000ff',
  CURRENT_DATE - 21,
  '11:00:00',
  2,
  0,
  'Shoulder stiffness after rotator cuff repair'
),
(
  'a0000000-0000-0000-0000-000000000034',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000033',
  'a0000000-0000-0000-0000-000000000002',
  'a0000000-0000-0000-0000-0000000000ff',
  CURRENT_DATE - 3,
  '08:45:00',
  2,
  0,
  'Migraine with visual aura'
),
(
  'a0000000-0000-0000-0000-000000000044',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000043',
  'a0000000-0000-0000-0000-000000000002',
  'a0000000-0000-0000-0000-0000000000ff',
  CURRENT_DATE - 28,
  '13:15:00',
  2,
  1,
  'Type 2 diabetes and neuropathy check-in'
)
ON CONFLICT (id) DO NOTHING;

INSERT INTO public.soap_notes (
  id, tenant_id, encounter_id, subjective, objective, assessment, plan, chief_complaint_narrative, is_signed
) VALUES
(
  'a0000000-0000-0000-0000-000000000104',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000004',
  'Patient reports aching lumbar pain for two weeks, worse with sitting and morning stiffness about 20 minutes. Denies bowel or bladder changes.',
  'Ambulated independently. Lumbar ROM reduced in flexion. Straight-leg raise negative. No focal neurologic deficit.',
  'Mechanical low back pain without red-flag features.',
  'Home exercise program, heat as needed, follow up in two weeks if not improving. Return sooner for weakness or incontinence.',
  'Lower back pain and stiffness',
  true
),
(
  'a0000000-0000-0000-0000-000000000114',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000014',
  'Reports aching in the right knee, worse on stairs. Using a cane outdoors. Sleeping better with current acetaminophen schedule.',
  'Antalgic gait. Crepitus with flexion. No effusion. Neurovascularly intact.',
  'Symptomatic right knee osteoarthritis.',
  'Continue PT, acetaminophen as directed, discuss injection at next visit if function plateaus.',
  'Right knee osteoarthritis follow-up',
  true
),
(
  'a0000000-0000-0000-0000-000000000115',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000015',
  'Patient notes increased right knee soreness after advancing squat program in PT. Ice helps. No locking or giving way.',
  'Mild medial joint-line tenderness. Full extension. McMurray negative. No effusion.',
  'Activity-related flare of knee osteoarthritis; no mechanical symptoms.',
  'Temporarily reduce squat depth, ice after sessions, resume prior PT load in one week.',
  'Knee pain after physical therapy',
  true
),
(
  'a0000000-0000-0000-0000-000000000124',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000024',
  'Six weeks after rotator cuff repair. Night pain improved. Difficulty reaching overhead at work.',
  'Well-healed portal sites. Forward flexion 120 degrees actively. Strength 4/5 in abduction. Distal neurovascular exam intact.',
  'Expected postoperative stiffness after rotator cuff repair.',
  'Continue protocol with PT, sling wean per surgeon plan, follow up in four weeks.',
  'Shoulder stiffness after rotator cuff repair',
  true
),
(
  'a0000000-0000-0000-0000-000000000134',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000034',
  'Recurrent migraine with shimmering lights in the left visual field, then throbbing left-sided headache and nausea. Lasts 6–8 hours. Two episodes this week.',
  'Alert, comfortable between attacks. Cranial nerves intact. No papilledema. BP 118/74.',
  'Migraine with typical visual aura. No concerning neurologic findings today.',
  'Start migraine abortive as discussed, hydration, sleep routine, keep a headache diary. Urgent return for thunderclap or focal weakness.',
  'Migraine with visual aura',
  true
),
(
  'a0000000-0000-0000-0000-000000000144',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000044',
  'Tingling in both feet, worse at night. Home sugars in the 140s fasting. Taking metformin as prescribed. No chest pain.',
  'Monofilament reduced at first toes. Pulses present. No ulcers. BMI 31.2.',
  'Type 2 diabetes with peripheral neuropathy, currently without foot wounds.',
  'Continue metformin, daily foot checks, discuss neuropathic pain options, labs before next visit.',
  'Type 2 diabetes and neuropathy check-in',
  true
)
ON CONFLICT (encounter_id) DO NOTHING;

INSERT INTO public.encounter_transcripts (
  id, tenant_id, encounter_id, raw_text, recorded_at, transcription_service, transcription_status, soap_generated_from_this, word_count
) VALUES
(
  'a0000000-0000-0000-0000-000000000204',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000004',
  'Clinician: What brings you in today?
Patient: My lower back has been stiff for about two weeks, especially in the morning.
Clinician: Any trouble with your legs, or bladder?
Patient: No, just aching when I sit too long.',
  (CURRENT_DATE - 14)::timestamptz + time '09:20',
  3,
  2,
  true,
  52
),
(
  'a0000000-0000-0000-0000-000000000214',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000014',
  'Clinician: How is the right knee since last time?
Patient: Stairs are still hard. The cane helps outside. Tylenol takes the edge off.
Clinician: Any swelling or locking?
Patient: No locking. A little puffiness after a long walk.',
  (CURRENT_DATE - 40)::timestamptz + time '10:05',
  3,
  2,
  true,
  48
),
(
  'a0000000-0000-0000-0000-000000000215',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000015',
  'Patient: PT had me do deeper squats and my knee flared up.
Clinician: Catching or giving way?
Patient: Neither. Ice after therapy helps by evening.',
  (CURRENT_DATE - 7)::timestamptz + time '14:35',
  3,
  2,
  true,
  32
),
(
  'a0000000-0000-0000-0000-000000000224',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000024',
  'Clinician: How is the shoulder at night?
Patient: Better than the first month. I still cannot reach a high shelf at work.
Clinician: Any wound drainage?
Patient: No, the incisions look fine.',
  (CURRENT_DATE - 21)::timestamptz + time '11:08',
  3,
  2,
  true,
  40
),
(
  'a0000000-0000-0000-0000-000000000234',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000034',
  'Patient: I get shimmering lights on the left, then a pounding headache and nausea.
Clinician: How long do they last?
Patient: Six to eight hours. Twice this week.
Clinician: Any weakness or the worst headache of your life?
Patient: No, same pattern as always.',
  (CURRENT_DATE - 3)::timestamptz + time '08:50',
  3,
  2,
  true,
  54
),
(
  'a0000000-0000-0000-0000-000000000244',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000044',
  'Clinician: How are the feet and the sugars?
Patient: Burning in the toes at night. Fasting numbers around one forty.
Clinician: Any sores?
Patient: I check every day. Nothing open.',
  (CURRENT_DATE - 28)::timestamptz + time '13:20',
  3,
  2,
  true,
  38
)
ON CONFLICT (id) DO NOTHING;
