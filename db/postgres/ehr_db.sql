-- =============================================================================
-- EHR core schema (PostgreSQL) — **schema source** for the combined reset script
--
-- This file is the **DDL only** (no data, no full teardown). The script you usually
-- want to run is **ehr_db_reset_and_seed.sql** (drop public, recreate schema, seed).
--
-- After changing this file, regenerate the combined file:
--   python db/postgres/_build_reset_script.py
--
-- Database: ehr_db · identifiers snake_case
-- =============================================================================


CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- -----------------------------------------------------------------------------
-- updated_at touch (reusable)
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
CREATE TABLE IF NOT EXISTS public.tenants
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

CREATE UNIQUE INDEX IF NOT EXISTS uq_tenants_slug
  ON public.tenants (lower(slug));

CREATE INDEX IF NOT EXISTS idx_tenants_tenant
  ON public.tenants (tenant_id);

DROP TRIGGER IF EXISTS tr_tenants_updated_at ON public.tenants;
CREATE TRIGGER tr_tenants_updated_at
  BEFORE UPDATE ON public.tenants
  FOR EACH ROW EXECUTE PROCEDURE public.set_updated_at();

-- -----------------------------------------------------------------------------
-- facilities
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS public.facilities
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

CREATE INDEX IF NOT EXISTS idx_facilities_tenant
  ON public.facilities (tenant_id)
  WHERE is_active = true;

CREATE UNIQUE INDEX IF NOT EXISTS uq_facilities_tenant_name
  ON public.facilities (tenant_id, lower(name));

CREATE INDEX IF NOT EXISTS idx_facilities_tenant_ext
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
CREATE TABLE IF NOT EXISTS public.patients
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

CREATE INDEX IF NOT EXISTS idx_patients_tenant
  ON public.patients (tenant_id, is_active);

CREATE INDEX IF NOT EXISTS idx_patients_tenant_name
  ON public.patients (tenant_id, last_name, first_name);

CREATE INDEX IF NOT EXISTS idx_patients_facility
  ON public.patients (tenant_id, facility_id);

CREATE UNIQUE INDEX IF NOT EXISTS uq_patients_tenant_ext
  ON public.patients (tenant_id, external_patient_id)
  WHERE external_patient_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_patients_tenant_mrn
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
CREATE TABLE IF NOT EXISTS public.medical_codes
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

CREATE UNIQUE INDEX IF NOT EXISTS uq_medical_codes_tenant_type_code
  ON public.medical_codes (tenant_id, type, lower(btrim(code)));

CREATE INDEX IF NOT EXISTS idx_medical_codes_tenant_type
  ON public.medical_codes (tenant_id, type, is_active);

CREATE INDEX IF NOT EXISTS idx_medical_codes_code_search
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
CREATE TABLE IF NOT EXISTS public.encounters
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

CREATE INDEX IF NOT EXISTS idx_encounters_tenant
  ON public.encounters (tenant_id);

CREATE INDEX IF NOT EXISTS idx_encounters_tenant_patient_date
  ON public.encounters (tenant_id, patient_id, service_date DESC);

CREATE INDEX IF NOT EXISTS idx_encounters_tenant_status
  ON public.encounters (tenant_id, status);

CREATE INDEX IF NOT EXISTS idx_encounters_tenant_provider
  ON public.encounters (tenant_id, attending_provider_user_id, service_date DESC);

CREATE INDEX IF NOT EXISTS idx_encounters_facility
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
CREATE TABLE IF NOT EXISTS public.soap_notes
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

CREATE INDEX IF NOT EXISTS idx_soap_notes_tenant
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
CREATE TABLE IF NOT EXISTS public.encounter_transcripts
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

CREATE INDEX IF NOT EXISTS idx_encounter_transcripts_encounter
  ON public.encounter_transcripts (encounter_id, recorded_at DESC);

CREATE INDEX IF NOT EXISTS idx_encounter_transcripts_tenant_status
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
CREATE TABLE IF NOT EXISTS public.encounter_medical_codes
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

CREATE INDEX IF NOT EXISTS idx_encounter_medical_codes_encounter
  ON public.encounter_medical_codes (encounter_id, sort_order, role);

CREATE INDEX IF NOT EXISTS idx_encounter_medical_codes_tenant
  ON public.encounter_medical_codes (tenant_id, encounter_id);

CREATE INDEX IF NOT EXISTS idx_encounter_medical_codes_code
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
