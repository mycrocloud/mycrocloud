# MycroCloud

## Available Services
### WebApp
The **WebApp** service enables you to create fully managed web applications with just a few clicks.

**Key Features**:
- Supports three types of responses:
    - *Static*: Returns content you specified with Handlebar supported for dynamic content.
    - *StaticFile*: Serves uploaded files.
    - *Function*: Executes JavaScript code.
- Offers various storage options for data persistence:
File, Object, Text, Variable.
- Includes request validation to ensure proper data handling.
- Provides request authentication and authorization using OpenID Connect.
- Allows viewing and exporting of logs.
- Integrates with GitHub for automatic deployment from your repositories.

### Architecture
- CloudFlare for DNS
- Auth0 for identity
- PostgreSQL
- Nginx for proxying
- React
- ASP.NET Core
- Redis, RabbitMQ
- Seq, Fluentd, ElasticSearch for logging

### Building Status
![api](https://github.com/mycrocloud/mycrocloud/actions/workflows/api.yml/badge.svg)

![apigateway](https://github.com/mycrocloud/mycrocloud/actions/workflows/apigateway.yml/badge.svg)

![db-migrator](https://github.com/mycrocloud/mycrocloud/actions/workflows/db-migrator.yml/badge.svg)

![deployment-builder](https://github.com/mycrocloud/mycrocloud/actions/workflows/deployment-builder.yml/badge.svg)

![deployment-fluentd](https://github.com/mycrocloud/mycrocloud/actions/workflows/deployment-fluentd.yml/badge.svg)

![deployment-worker](https://github.com/mycrocloud/mycrocloud/actions/workflows/deployment-worker.yml/badge.svg)

![funtion-invoker](https://github.com/mycrocloud/mycrocloud/actions/workflows/funtion-invoker.yml/badge.svg)

![web-editor](https://github.com/mycrocloud/mycrocloud/actions/workflows/web-editor.yml/badge.svg)

![web](https://github.com/mycrocloud/mycrocloud/actions/workflows/web.yml/badge.svg)
