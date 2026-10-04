-- =============================================================================
-- EHR database: FULL RESET, schema, and dev seed (PostgreSQL)
-- Target database name MUST be: ehr_db
--
-- What it does:
--   1) Drops extension pgcrypto and schema public (all tables, functions, etc.)
--   2) Recreates public with default grants, reinstalls pgcrypto
--   3) Recreates the full EHR schema (same as legacy ehr_db.sql body)
--   4) Inserts dev sample tenant / facility / patients / encounters / notes
--
-- Re-run any time: everything in public is destroyed and rebuilt. Connect only to ehr_db.
-- Example:
--   psql -U postgres -c "CREATE DATABASE ehr_db;"  (once)
--   psql -U postgres -d ehr_db -f ehr_db_reset_and_seed.sql
-- =============================================================================

\set ON_ERROR_STOP on

DO $guard$
BEGIN
  IF current_database()::text <> 'ehr_db' THEN
    RAISE EXCEPTION
      'Refusing to run: must be connected to database ehr_db. Current: % (use: psql -U postgres -d ehr_db -f ...)',
      current_database()::text;
  END IF;
END;
$guard$;

-- TEARDOWN: all objects in public, then fresh schema + extension
DROP EXTENSION IF EXISTS pgcrypto CASCADE;
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO postgres;
GRANT ALL ON SCHEMA public TO public;
COMMENT ON SCHEMA public IS 'standard public schema';

CREATE EXTENSION pgcrypto;

-- -----------------------------------------------------------------------------
-- Schema (rebuilt; see legacy ehr_db.sql for same definition without teardown)
-- -----------------------------------------------------------------------------

CREATE OR REPLACE FUNCTION public.set_updated_at()
RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
  NEW.updated_at := now();
  RETURN NEW;
END;
$$;

-- -----------------------------------------------------------------------------
-- tenants: root partition; tenant_id must equal id for the tenant row
-- -----------------------------------------------------------------------------
CREATE TABLE public.tenants
(
  id            uuid        NOT NULL,
  tenant_id     uuid        NOT NULL,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now(),
  name          varchar(200) NOT NULL,
  slug          varchar(200) NOT NULL,
  npi           varchar(20),
  tax_id        varchar(20),
  phone         varchar(30),
  email         varchar(320),
  website       varchar(2000),
  address_line1 varchar(200),
  address_line2 varchar(200),
  city          varchar(100),
  state         varchar(2),
  zip_code      varchar(10),
  is_active     boolean     NOT NULL DEFAULT true,
  CONSTRAINT    pk_tenants PRIMARY KEY (id),
  CONSTRAINT    ck_tenants_tenant_matches CHECK (id = tenant_id)
);

CREATE UNIQUE INDEX uq_tenants_slug
  ON public.tenants (lower(slug));

CREATE INDEX idx_tenants_tenant
  ON public.tenants (tenant_id);

DROP TRIGGER IF EXISTS tr_tenants_updated_at ON public.tenants;
CREATE TRIGGER tr_tenants_updated_at
  BEFORE UPDATE ON public.tenants
  FOR EACH ROW EXECUTE PROCEDURE public.set_updated_at();

-- -----------------------------------------------------------------------------
-- facilities
-- -----------------------------------------------------------------------------
CREATE TABLE public.facilities
(
  id                   uuid         NOT NULL DEFAULT gen_random_uuid(),
  tenant_id            uuid         NOT NULL
    REFERENCES public.tenants (id) ON DELETE CASCADE,
  created_at           timestamptz  NOT NULL DEFAULT now(),
  updated_at           timestamptz  NOT NULL DEFAULT now(),
  name                 varchar(200) NOT NULL,
  address              text         NOT NULL DEFAULT '',
  city                 varchar(100) NOT NULL,
  state                varchar(2)  NOT NULL,
  zip_code             varchar(10)  NOT NULL,
  phone                varchar(30),
  external_facility_id varchar(100),
  is_active            boolean      NOT NULL DEFAULT true,
  CONSTRAINT pk_facilities PRIMARY KEY (id)
);

CREATE INDEX idx_facilities_tenant
  ON public.facilities (tenant_id)
  WHERE is_active = true;

CREATE UNIQUE INDEX uq_facilities_tenant_name
  ON public.facilities (tenant_id, lower(name));

CREATE INDEX idx_facilities_tenant_ext
  ON public.facilities (tenant_id, external_facility_id)
  WHERE external_facility_id IS NOT NULL;

DROP TRIGGER IF EXISTS tr_facilities_updated_at ON public.facilities;
CREATE TRIGGER tr_facilities_updated_at
  BEFORE UPDATE ON public.facilities
  FOR EACH ROW EXECUTE PROCEDURE public.set_updated_at();

-- -----------------------------------------------------------------------------
-- patients
--   gender: 0=Unknown, 1=Male, 2=Female, 3=Other
-- -----------------------------------------------------------------------------
CREATE TABLE public.patients
(
  id                uuid         NOT NULL DEFAULT gen_random_uuid(),
  tenant_id         uuid         NOT NULL
    REFERENCES public.tenants (id) ON DELETE CASCADE,
  created_at        timestamptz  NOT NULL DEFAULT now(),
  updated_at        timestamptz  NOT NULL DEFAULT now(),
  first_name        varchar(100) NOT NULL,
  last_name         varchar(100) NOT NULL,
  date_of_birth     date         NOT NULL,
  gender            smallint     NOT NULL DEFAULT 0
    CONSTRAINT ck_patients_gender CHECK (gender >= 0 AND gender <= 3),
  medical_record_number  varchar(50),
  external_patient_id    varchar(100),
  room              varchar(50),
  unit              varchar(50),
  bed               varchar(20),
  phone             varchar(30),
  email             varchar(320),
  address           text,
  city              varchar(100),
  state             varchar(2),
  zip_code          varchar(10),
  emergency_contact_name  varchar(200),
  emergency_contact_phone varchar(30),
  emergency_contact_relationship varchar(100),
  admission_date    date,
  discharge_date    date,
  is_active         boolean      NOT NULL DEFAULT true,
  facility_id       uuid         NOT NULL
    REFERENCES public.facilities (id) ON DELETE RESTRICT,
  CONSTRAINT pk_patients PRIMARY KEY (id)
);

CREATE INDEX idx_patients_tenant
  ON public.patients (tenant_id, is_active);

CREATE INDEX idx_patients_tenant_name
  ON public.patients (tenant_id, last_name, first_name);

CREATE INDEX idx_patients_facility
  ON public.patients (tenant_id, facility_id);

CREATE UNIQUE INDEX uq_patients_tenant_ext
  ON public.patients (tenant_id, external_patient_id)
  WHERE external_patient_id IS NOT NULL;

CREATE INDEX idx_patients_tenant_mrn
  ON public.patients (tenant_id, medical_record_number)
  WHERE medical_record_number IS NOT NULL;

-- patient.facility_id must reference a facility in the same tenant
CREATE OR REPLACE FUNCTION public.enforce_patient_facility_tenant()
RETURNS trigger
LANGUAGE plpgsql AS $$
DECLARE
  f_tenant uuid;
BEGIN
  SELECT tenant_id INTO f_tenant FROM public.facilities WHERE id = NEW.facility_id;
  IF f_tenant IS NULL THEN
    RAISE EXCEPTION 'patients: facility not found';
  END IF;
  IF NEW.tenant_id IS DISTINCT FROM f_tenant THEN
    RAISE EXCEPTION 'patients: tenant_id must match facility.tenant_id';
  END IF;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tr_patients_facility_tenant ON public.patients;
CREATE TRIGGER tr_patients_facility_tenant
  BEFORE INSERT OR UPDATE OF facility_id, tenant_id
  ON public.patients
  FOR EACH ROW EXECUTE PROCEDURE public.enforce_patient_facility_tenant();

DROP TRIGGER IF EXISTS tr_patients_updated_at ON public.patients;
CREATE TRIGGER tr_patients_updated_at
  BEFORE UPDATE ON public.patients
  FOR EACH ROW EXECUTE PROCEDURE public.set_updated_at();

-- -----------------------------------------------------------------------------
-- medical_codes
--   type: 1=Icd10Cm, 2=Cpt
-- -----------------------------------------------------------------------------
CREATE TABLE public.medical_codes
(
  id             uuid         NOT NULL DEFAULT gen_random_uuid(),
  tenant_id      uuid         NOT NULL
    REFERENCES public.tenants (id) ON DELETE CASCADE,
  created_at     timestamptz  NOT NULL DEFAULT now(),
  updated_at     timestamptz  NOT NULL DEFAULT now(),
  type           smallint     NOT NULL
    CONSTRAINT ck_medical_codes_type CHECK (type IN (1, 2)),
  code           varchar(32)  NOT NULL,
  description    text         NOT NULL,
  category       varchar(200),
  notes          text,
  is_active      boolean      NOT NULL DEFAULT true,
  default_fee    numeric(12,2),
  medicare_rate  numeric(12,2),
  work_rvu       numeric(8,2),
  CONSTRAINT     pk_medical_codes PRIMARY KEY (id)
);

CREATE UNIQUE INDEX uq_medical_codes_tenant_type_code
  ON public.medical_codes (tenant_id, type, lower(btrim(code)));

CREATE INDEX idx_medical_codes_tenant_type
  ON public.medical_codes (tenant_id, type, is_active);

CREATE INDEX idx_medical_codes_code_search
  ON public.medical_codes (tenant_id, type, code)
  WHERE is_active = true;

DROP TRIGGER IF EXISTS tr_medical_codes_updated_at ON public.medical_codes;
CREATE TRIGGER tr_medical_codes_updated_at
  BEFORE UPDATE ON public.medical_codes
  FOR EACH ROW EXECUTE PROCEDURE public.set_updated_at();

-- -----------------------------------------------------------------------------
-- encounters
--   status: 0..4 (EncounterStatus). type: 0..3 (EncounterType).
-- -----------------------------------------------------------------------------
CREATE TABLE public.encounters
(
  id                        uuid         NOT NULL DEFAULT gen_random_uuid(),
  tenant_id                 uuid         NOT NULL
    REFERENCES public.tenants (id) ON DELETE CASCADE,
  created_at                timestamptz  NOT NULL DEFAULT now(),
  updated_at                timestamptz  NOT NULL DEFAULT now(),
  patient_id                uuid         NOT NULL
    REFERENCES public.patients (id) ON DELETE RESTRICT,
  facility_id               uuid         NOT NULL
    REFERENCES public.facilities (id) ON DELETE RESTRICT,
  attending_provider_user_id uuid        NOT NULL,
  service_date              date         NOT NULL,
  scheduled_time            time(0),
  status                    smallint     NOT NULL DEFAULT 0
    CONSTRAINT ck_encounters_status CHECK (status >= 0 AND status <= 4),
  type                      smallint     NOT NULL DEFAULT 1
    CONSTRAINT ck_encounters_type CHECK (type >= 0 AND type <= 3),
  chief_complaint           text,
  visit_notes               text,
  check_in_at               timestamptz,
  check_out_at              timestamptz,
  completed_at              timestamptz,
  cancelled_at              timestamptz,
  cancellation_reason       text,
  blood_pressure_systolic   integer,
  blood_pressure_diastolic  integer,
  heart_rate                integer,
  respiratory_rate         integer,
  temperature_fahrenheit    numeric(5,1),
  oxygen_saturation_percent integer
    CONSTRAINT ck_encounters_spo2 CHECK (oxygen_saturation_percent IS NULL OR
      (oxygen_saturation_percent >= 0 AND oxygen_saturation_percent <= 100)),
  pain_level_0_to_10        integer
    CONSTRAINT ck_encounters_pain CHECK (pain_level_0_to_10 IS NULL OR
      (pain_level_0_to_10 >= 0 AND pain_level_0_to_10 <= 10)),
  weight_pounds             numeric(6,1),
  height_inches            numeric(5,1),
  bmi                      numeric(5,1),
  vitals_recorded_at        timestamptz,
  vitals_recorded_by_user_id uuid,
  CONSTRAINT pk_encounters PRIMARY KEY (id)
);

CREATE INDEX idx_encounters_tenant
  ON public.encounters (tenant_id);

CREATE INDEX idx_encounters_tenant_patient_date
  ON public.encounters (tenant_id, patient_id, service_date DESC);

CREATE INDEX idx_encounters_tenant_status
  ON public.encounters (tenant_id, status);

CREATE INDEX idx_encounters_tenant_provider
  ON public.encounters (tenant_id, attending_provider_user_id, service_date DESC);

CREATE INDEX idx_encounters_facility
  ON public.encounters (tenant_id, facility_id);

-- Row must be scoped to the same tenant as patient/facility (optional hard guard)
CREATE OR REPLACE FUNCTION public.enforce_encounter_tenant()
RETURNS trigger
LANGUAGE plpgsql AS $$
DECLARE
  p_tenant uuid;
  f_tenant uuid;
BEGIN
  SELECT tenant_id INTO p_tenant FROM public.patients WHERE id = NEW.patient_id;
  SELECT tenant_id INTO f_tenant FROM public.facilities WHERE id = NEW.facility_id;
  IF p_tenant IS NULL OR f_tenant IS NULL THEN
    RAISE EXCEPTION 'encounters: patient or facility not found';
  END IF;
  IF NEW.tenant_id IS DISTINCT FROM p_tenant OR NEW.tenant_id IS DISTINCT FROM f_tenant THEN
    RAISE EXCEPTION 'encounters: tenant_id must match patient and facility tenant_id';
  END IF;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tr_encounters_tenant_guard ON public.encounters;
CREATE TRIGGER tr_encounters_tenant_guard
  BEFORE INSERT OR UPDATE OF patient_id, facility_id, tenant_id
  ON public.encounters
  FOR EACH ROW EXECUTE PROCEDURE public.enforce_encounter_tenant();

DROP TRIGGER IF EXISTS tr_encounters_updated_at ON public.encounters;
CREATE TRIGGER tr_encounters_updated_at
  BEFORE UPDATE ON public.encounters
  FOR EACH ROW EXECUTE PROCEDURE public.set_updated_at();

-- -----------------------------------------------------------------------------
-- soap_notes (1:1 with encounters)
-- -----------------------------------------------------------------------------
CREATE TABLE public.soap_notes
(
  id                    uuid         NOT NULL DEFAULT gen_random_uuid(),
  tenant_id             uuid         NOT NULL
    REFERENCES public.tenants (id) ON DELETE CASCADE,
  created_at            timestamptz  NOT NULL DEFAULT now(),
  updated_at            timestamptz  NOT NULL DEFAULT now(),
  encounter_id          uuid         NOT NULL
    REFERENCES public.encounters (id) ON DELETE CASCADE,
  subjective            text         NOT NULL DEFAULT '',
  objective             text         NOT NULL DEFAULT '',
  assessment            text         NOT NULL DEFAULT '',
  plan                  text         NOT NULL DEFAULT '',
  chief_complaint_narrative text,
  is_signed             boolean      NOT NULL DEFAULT false,
  signed_at             timestamptz,
  signed_by_user_id     uuid,
  CONSTRAINT pk_soap_notes PRIMARY KEY (id),
  CONSTRAINT uq_soap_notes_one_per_encounter UNIQUE (encounter_id)
);

CREATE INDEX idx_soap_notes_tenant
  ON public.soap_notes (tenant_id, updated_at DESC);

-- SOAP row tenant must match encounter tenant
CREATE OR REPLACE FUNCTION public.enforce_soap_note_tenant()
RETURNS trigger
LANGUAGE plpgsql AS $$
DECLARE
  t uuid;
BEGIN
  SELECT tenant_id INTO t FROM public.encounters WHERE id = NEW.encounter_id;
  IF t IS NULL THEN
    RAISE EXCEPTION 'soap_notes: encounter not found';
  END IF;
  IF NEW.tenant_id IS DISTINCT FROM t THEN
    RAISE EXCEPTION 'soap_notes: tenant_id must match encounter.tenant_id';
  END IF;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tr_soap_notes_tenant_guard ON public.soap_notes;
CREATE TRIGGER tr_soap_notes_tenant_guard
  BEFORE INSERT OR UPDATE OF encounter_id, tenant_id
  ON public.soap_notes
  FOR EACH ROW EXECUTE PROCEDURE public.enforce_soap_note_tenant();

DROP TRIGGER IF EXISTS tr_soap_notes_updated_at ON public.soap_notes;
CREATE TRIGGER tr_soap_notes_updated_at
  BEFORE UPDATE ON public.soap_notes
  FOR EACH ROW EXECUTE PROCEDURE public.set_updated_at();

-- -----------------------------------------------------------------------------
-- encounter_transcripts
--   transcription_service: 0=Unknown, 1=Deepgram, 2=OpenAI Whisper, 3=Manual, 4=Azure, 5=Google
--   transcription_status: 0=Pending, 1=Processing, 2=Completed, 3=Failed
-- -----------------------------------------------------------------------------
CREATE TABLE public.encounter_transcripts
(
  id                     uuid         NOT NULL DEFAULT gen_random_uuid(),
  tenant_id              uuid         NOT NULL
    REFERENCES public.tenants (id) ON DELETE CASCADE,
  created_at             timestamptz  NOT NULL DEFAULT now(),
  updated_at             timestamptz  NOT NULL DEFAULT now(),
  encounter_id           uuid         NOT NULL
    REFERENCES public.encounters (id) ON DELETE CASCADE,
  audio_storage_key      text,
  raw_text               text,
  recorded_at            timestamptz  NOT NULL,
  duration_seconds       integer
    CONSTRAINT ck_transcripts_dur CHECK (duration_seconds IS NULL OR duration_seconds >= 0),
  file_size_bytes        bigint,
  word_count             integer,
  transcription_service  smallint     NOT NULL DEFAULT 0,
  average_confidence     double precision
    CONSTRAINT ck_transcripts_conf CHECK (average_confidence IS NULL OR
      (average_confidence >= 0.0 AND average_confidence <= 1.0)),
  transcription_status   smallint     NOT NULL DEFAULT 0
    CONSTRAINT ck_transcripts_tstat CHECK (transcription_status >= 0 AND transcription_status <= 3),
  soap_generated_from_this boolean   NOT NULL DEFAULT false,
  created_by_user_id     uuid,
  CONSTRAINT pk_encounter_transcripts PRIMARY KEY (id)
);

CREATE INDEX idx_encounter_transcripts_encounter
  ON public.encounter_transcripts (encounter_id, recorded_at DESC);

CREATE INDEX idx_encounter_transcripts_tenant_status
  ON public.encounter_transcripts (tenant_id, transcription_status, recorded_at DESC);

CREATE OR REPLACE FUNCTION public.enforce_transcript_tenant()
RETURNS trigger
LANGUAGE plpgsql AS $$
DECLARE
  t uuid;
BEGIN
  SELECT tenant_id INTO t FROM public.encounters WHERE id = NEW.encounter_id;
  IF t IS NULL THEN
    RAISE EXCEPTION 'encounter_transcripts: encounter not found';
  END IF;
  IF NEW.tenant_id IS DISTINCT FROM t THEN
    RAISE EXCEPTION 'encounter_transcripts: tenant_id must match encounter.tenant_id';
  END IF;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tr_encounter_transcripts_tenant_guard ON public.encounter_transcripts;
CREATE TRIGGER tr_encounter_transcripts_tenant_guard
  BEFORE INSERT OR UPDATE OF encounter_id, tenant_id
  ON public.encounter_transcripts
  FOR EACH ROW EXECUTE PROCEDURE public.enforce_transcript_tenant();

DROP TRIGGER IF EXISTS tr_encounter_transcripts_updated_at ON public.encounter_transcripts;
CREATE TRIGGER tr_encounter_transcripts_updated_at
  BEFORE UPDATE ON public.encounter_transcripts
  FOR EACH ROW EXECUTE PROCEDURE public.set_updated_at();

-- -----------------------------------------------------------------------------
-- encounter_medical_codes
--   role: 0..3 (EncounterMedicalCodeRole). source: 0=Manual, 1=Ai, 2=Imported
-- -----------------------------------------------------------------------------
CREATE TABLE public.encounter_medical_codes
(
  id                 uuid         NOT NULL DEFAULT gen_random_uuid(),
  tenant_id          uuid         NOT NULL
    REFERENCES public.tenants (id) ON DELETE CASCADE,
  created_at         timestamptz  NOT NULL DEFAULT now(),
  updated_at         timestamptz  NOT NULL DEFAULT now(),
  encounter_id       uuid         NOT NULL
    REFERENCES public.encounters (id) ON DELETE CASCADE,
  medical_code_id    uuid         NOT NULL
    REFERENCES public.medical_codes (id) ON DELETE RESTRICT,
  role               smallint     NOT NULL
    CONSTRAINT ck_enc_mc_role CHECK (role >= 0 AND role <= 3),
  source             smallint     NOT NULL DEFAULT 1
    CONSTRAINT ck_enc_mc_source CHECK (source >= 0 AND source <= 2),
  confidence         double precision
    CONSTRAINT ck_enc_mc_conf CHECK (confidence IS NULL OR
      (confidence >= 0.0 AND confidence <= 1.0)),
  rationale          text,
  sort_order         integer      NOT NULL DEFAULT 0,
  modifier1          varchar(2),
  modifier2          varchar(2),
  CONSTRAINT         pk_encounter_medical_codes PRIMARY KEY (id)
);

CREATE INDEX idx_encounter_medical_codes_encounter
  ON public.encounter_medical_codes (encounter_id, sort_order, role);

CREATE INDEX idx_encounter_medical_codes_tenant
  ON public.encounter_medical_codes (tenant_id, encounter_id);

CREATE INDEX idx_encounter_medical_codes_code
  ON public.encounter_medical_codes (medical_code_id);

-- Composite: medical_code belongs to same tenant
CREATE OR REPLACE FUNCTION public.enforce_encounter_medical_code_tenants()
RETURNS trigger
LANGUAGE plpgsql AS $$
DECLARE
  e_tenant uuid;
  c_tenant uuid;
BEGIN
  SELECT tenant_id INTO e_tenant FROM public.encounters WHERE id = NEW.encounter_id;
  SELECT tenant_id INTO c_tenant FROM public.medical_codes WHERE id = NEW.medical_code_id;
  IF e_tenant IS NULL OR c_tenant IS NULL THEN
    RAISE EXCEPTION 'encounter_medical_codes: encounter or medical_code not found';
  END IF;
  IF e_tenant IS DISTINCT FROM c_tenant OR NEW.tenant_id IS DISTINCT FROM e_tenant THEN
    RAISE EXCEPTION 'encounter_medical_codes: tenant_id must align for encounter, code, and row';
  END IF;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS tr_encounter_medical_codes_guard ON public.encounter_medical_codes;
CREATE TRIGGER tr_encounter_medical_codes_guard
  BEFORE INSERT OR UPDATE OF encounter_id, medical_code_id, tenant_id
  ON public.encounter_medical_codes
  FOR EACH ROW EXECUTE PROCEDURE public.enforce_encounter_medical_code_tenants();

DROP TRIGGER IF EXISTS tr_encounter_medical_codes_updated_at ON public.encounter_medical_codes;
CREATE TRIGGER tr_encounter_medical_codes_updated_at
  BEFORE UPDATE ON public.encounter_medical_codes
  FOR EACH ROW EXECUTE PROCEDURE public.set_updated_at();

-- Optional helpful comment
COMMENT ON TABLE public.tenants IS 'Tenant (organization) root; for each row id = tenant_id.';
COMMENT ON TABLE public.medical_codes IS 'type: 1=Icd10Cm, 2=Cpt (align with Ehr.Domain.Enums.MedicalCodeType).';
COMMENT ON TABLE public.encounters IS 'status: 0=Scheduled,1=InProgress,2=Completed,3=Cancelled,4=NoShow.';
COMMENT ON TABLE public.encounter_transcripts IS 'transcription_service: 0=Unknown,1=Deepgram,2=OpenAI Whisper,3=Manual,4=Azure,5=Google.';

-- =============================================================================
-- Dev seed (known GUIDs for API + UI)
--   X-Tenant-Id:  a0000000-0000-0000-0000-000000000001
--   Encounter ID: a0000000-0000-0000-0000-000000000004
-- =============================================================================

INSERT INTO public.tenants (id, tenant_id, name, slug, is_active) VALUES (
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000001',
  'Dev Practice',
  'dev-practice',
  true
);

INSERT INTO public.facilities (
  id, tenant_id, name, address, city, state, zip_code, is_active
) VALUES (
  'a0000000-0000-0000-0000-000000000002',
  'a0000000-0000-0000-0000-000000000001',
  'Main Campus',
  '100 Rehab Way',
  'Austin',
  'TX',
  '78701',
  true
);

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
);

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
);

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
);

INSERT INTO public.encounter_transcripts (
  id, tenant_id, encounter_id, raw_text, recorded_at, transcription_service, transcription_status, soap_generated_from_this, word_count
) VALUES
(
  'a0000000-0000-0000-0000-000000000204',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000004',
  E'Clinician: What brings you in today?\nPatient: My lower back has been stiff for about two weeks, especially in the morning.\nClinician: Any trouble with your legs, or bladder?\nPatient: No, just aching when I sit too long.',
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
  E'Clinician: How is the right knee since last time?\nPatient: Stairs are still hard. The cane helps outside. Tylenol takes the edge off.\nClinician: Any swelling or locking?\nPatient: No locking. A little puffiness after a long walk.',
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
  E'Patient: PT had me do deeper squats and my knee flared up.\nClinician: Catching or giving way?\nPatient: Neither. Ice after therapy helps by evening.',
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
  E'Clinician: How is the shoulder at night?\nPatient: Better than the first month. I still cannot reach a high shelf at work.\nClinician: Any wound drainage?\nPatient: No, the incisions look fine.',
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
  E'Patient: I get shimmering lights on the left, then a pounding headache and nausea.\nClinician: How long do they last?\nPatient: Six to eight hours. Twice this week.\nClinician: Any weakness or the worst headache of your life?\nPatient: No, same pattern as always.',
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
  E'Clinician: How are the feet and the sugars?\nPatient: Burning in the toes at night. Fasting numbers around one forty.\nClinician: Any sores?\nPatient: I check every day. Nothing open.',
  (CURRENT_DATE - 28)::timestamptz + time '13:20',
  3,
  2,
  true,
  38
);

\echo 'ehr_db_reset_and_seed.sql finished — database ehr_db is ready (schema + dev seed).'
