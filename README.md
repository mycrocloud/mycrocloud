# MycroCloud
**MycroCloud** provides cloud computing services to help you build and manage applications effortlessly.

## Available Services
### WebApp
The **WebApp** service enables you to create fully managed web applications with just a few clicks.

**Key Features**:
- Supports three types of responses:
    - *Static*: Returns content you specified with Handlebar supported for dynamic content.
    - *StaticFile*: Serves files from storage.
    - *Function*: Executes serverless functions.
- Offers various storage options for data persistence:
File, Object, Text, Variable.
- Includes request validation to ensure proper data handling.
- Provides request authentication and authorization using OpenID Connect or API keys.
- Allows viewing and exporting of logs for monitoring and debugging.
- Execute functions using a self-hosted or MycroCloud-hosted runner.
- Integrates with GitHub for automatic deployment from your repositories.
- Supports custom domains (coming soon).

### Database (coming soon)
The Database service will allow you to create, manage, and scale databases effortlessly.

### Form (coming soon)
The Form service will let you design forms and collect user data seamlessly.

<hr>
Feel free to expand on the upcoming services or add additional sections such as installation guides or FAQs!

### Architecture
![](/mycrocloud.drawio.png)

### Building Status
![api](https://github.com/mycrocloud/mycrocloud/actions/workflows/publish-api.yml/badge.svg)

![apigateway](https://github.com/mycrocloud/mycrocloud/actions/workflows/apigateway.yml/badge.svg)

![db-migrator](https://github.com/mycrocloud/mycrocloud/actions/workflows/db-migrator.yml/badge.svg)

![webapp](https://github.com/mycrocloud/mycrocloud/actions/workflows/webapp.yml/badge.svg)
