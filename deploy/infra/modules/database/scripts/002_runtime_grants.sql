

\set db_name mycrocloud

GRANT CONNECT ON DATABASE :db_name TO app_migrator;
GRANT CONNECT ON DATABASE :db_name TO api_runtime;
GRANT CONNECT ON DATABASE :db_name TO webapp_gateway;
GRANT CONNECT ON DATABASE :db_name TO grafana_reader;

-- =========================
-- SCHEMA
-- =========================

GRANT USAGE ON SCHEMA public TO app_migrator;
GRANT USAGE ON SCHEMA public TO api_runtime;
GRANT USAGE ON SCHEMA public TO webapp_gateway;
GRANT USAGE ON SCHEMA public TO grafana_reader;

-- =========================
-- MIGRATOR
-- =========================

GRANT CREATE ON SCHEMA public TO app_migrator;

GRANT ALL PRIVILEGES
ON ALL TABLES IN SCHEMA public
TO app_migrator;

GRANT ALL PRIVILEGES
ON ALL SEQUENCES IN SCHEMA public
TO app_migrator;

ALTER DEFAULT PRIVILEGES FOR ROLE app_migrator IN SCHEMA public
GRANT ALL ON TABLES TO app_migrator;

ALTER DEFAULT PRIVILEGES FOR ROLE app_migrator IN SCHEMA public
GRANT ALL ON SEQUENCES TO app_migrator;

-- =========================
-- API RUNTIME
-- =========================

GRANT SELECT, INSERT, UPDATE, DELETE
ON ALL TABLES IN SCHEMA public
TO api_runtime;

GRANT USAGE, SELECT
ON ALL SEQUENCES IN SCHEMA public
TO api_runtime;

ALTER DEFAULT PRIVILEGES FOR ROLE app_migrator IN SCHEMA public
GRANT SELECT, INSERT, UPDATE, DELETE
ON TABLES TO api_runtime;

ALTER DEFAULT PRIVILEGES FOR ROLE app_migrator IN SCHEMA public
GRANT USAGE, SELECT
ON SEQUENCES TO api_runtime;


-- read-only for all existing tables
GRANT SELECT
ON ALL TABLES IN SCHEMA public
TO webapp_gateway;

-- default: newly created tables are also read-only
ALTER DEFAULT PRIVILEGES FOR ROLE app_migrator IN SCHEMA public
GRANT SELECT
ON TABLES TO webapp_gateway;

-- write access only for AccessLogs table
GRANT INSERT
ON TABLE public."AccessLogs"
TO webapp_gateway;

-- if AccessLogs has identity/sequence:
GRANT USAGE, SELECT
ON ALL SEQUENCES IN SCHEMA public
TO webapp_gateway;

-- =========================
-- GRAFANA READER — read-only
-- =========================

GRANT SELECT
ON ALL TABLES IN SCHEMA public
TO grafana_reader;

ALTER DEFAULT PRIVILEGES FOR ROLE app_migrator IN SCHEMA public
GRANT SELECT
ON TABLES TO grafana_reader;
