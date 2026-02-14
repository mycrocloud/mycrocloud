CREATE ROLE migrator LOGIN PASSWORD 'xxx';
CREATE ROLE api LOGIN PASSWORD 'xxx';
CREATE ROLE webapp_gateway LOGIN PASSWORD 'xxx';
CREATE ROLE grafana LOGIN PASSWORD 'xxx';

GRANT CONNECT ON DATABASE mycrocloud TO migrator;
GRANT CONNECT ON DATABASE mycrocloud TO api;
GRANT CONNECT ON DATABASE mycrocloud TO webapp_gateway;
GRANT CONNECT ON DATABASE mycrocloud TO grafana;

GRANT USAGE ON SCHEMA public TO migrator;
GRANT CREATE ON SCHEMA public TO migrator;
