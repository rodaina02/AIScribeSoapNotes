# One-off: builds ehr_db_reset_and_seed.sql (run from repo, not required at runtime)
import pathlib

root = pathlib.Path(__file__).resolve().parent
lines = (root / "ehr_db.sql").read_text(encoding="utf-8").splitlines()
# Skip ehr_db header + CREATE EXTENSION through line 14 (0-based: [14:] = from line 15)
body = "\n".join(lines[14:])
for a, b in [
    ("CREATE TABLE IF NOT EXISTS", "CREATE TABLE"),
    ("CREATE UNIQUE INDEX IF NOT EXISTS", "CREATE UNIQUE INDEX"),
    ("CREATE INDEX IF NOT EXISTS", "CREATE INDEX"),
]:
    body = body.replace(a, b)

head = r"""-- =============================================================================
-- EHR database: FULL RESET, schema, and dev seed (PostgreSQL)
-- Target database name MUST be: ehr_db
--
-- What it does:
--   1) Drops extension pgcrypto and schema public (all tables, functions, etc.)
--   2) Recreates public with default grants, reinstalls pgcrypto
--   3) Recreates the full EHR schema (same as legacy ehr_db.sql body)
--   4) Inserts dev sample tenant / facility / patient / encounter
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

"""

seed = r"""
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
  id, tenant_id, first_name, last_name, date_of_birth, gender, facility_id, is_active
) VALUES (
  'a0000000-0000-0000-0000-000000000003',
  'a0000000-0000-0000-0000-000000000001',
  'Demo',
  'Patient',
  '1980-05-15',
  1,
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
  status,
  type,
  chief_complaint
) VALUES (
  'a0000000-0000-0000-0000-000000000004',
  'a0000000-0000-0000-0000-000000000001',
  'a0000000-0000-0000-0000-000000000003',
  'a0000000-0000-0000-0000-000000000002',
  'a0000000-0000-0000-0000-0000000000ff',
  CURRENT_DATE,
  1,
  0,
  'Lower back pain and stiffness for two weeks.'
);

\echo 'ehr_db_reset_and_seed.sql finished — database ehr_db is ready (schema + dev seed).'
"""

out = head + body.strip() + "\n" + seed
(root / "ehr_db_reset_and_seed.sql").write_text(out, encoding="utf-8", newline="\n")
print("Wrote ehr_db_reset_and_seed.sql")
