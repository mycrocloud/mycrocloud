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
![](/mycrocloud.drawio.png)

### Building Status
![api](https://github.com/mycrocloud/mycrocloud/actions/workflows/publish-api.yml/badge.svg)

![apigateway](https://github.com/mycrocloud/mycrocloud/actions/workflows/apigateway.yml/badge.svg)

![db-migrator](https://github.com/mycrocloud/mycrocloud/actions/workflows/db-migrator.yml/badge.svg)

![webapp](https://github.com/mycrocloud/mycrocloud/actions/workflows/webapp.yml/badge.svg)
