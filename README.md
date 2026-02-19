# MycroCloud

![api](https://github.com/mycrocloud/mycrocloud/actions/workflows/api.yml/badge.svg)
![gateway](https://github.com/mycrocloud/mycrocloud/actions/workflows/gateway.yml/badge.svg)
![web](https://github.com/mycrocloud/mycrocloud/actions/workflows/web.yml/badge.svg)

A cloud platform for building and deploying managed web applications.

## WebApp

Create fully managed web applications with just a few clicks.

### API

Build powerful APIs without managing infrastructure.

- **Response Types**: Return static content or execute JavaScript functions
- **Request Validation**: Define schemas to validate incoming requests automatically
- **Authentication**: Secure your endpoints with OpenID Connect

### SPA

Deploy your single-page applications with zero configuration.

- **GitHub Integration**: Connect your repository for automatic deployments
- **Instant Deploys**: Push to main and your changes go live automatically

### Fullstack App

Combine the power of API and SPA in a single application.

### Observability

Monitor and debug your applications with built-in logging.

- **Access Logs**: Track all incoming requests
- **Function Logs**: Monitor JavaScript function executions and errors
- **Export**: Download logs for external analysis

## App Specification Publishing

The API service publishes each app's runtime specification to Redis so the Gateway can resolve incoming requests.

### Redis keys

- `app:{slug}`: JSON `AppSpecification` (app state, active SPA/API deployment IDs, CORS, routing config, app settings, enabled auth schemes, runtime/all variables)
- `api_routes:{apiDeploymentId}`: JSON array of `ApiRouteSummary` (`id`, `method`, `path`, `responseType`) for the active API deployment
- `route_meta:{deploymentId}:{routeId}`: JSON `ApiRouteMetadata` (cached by Gateway on first read from deployment storage)

### Publish and invalidate triggers

`app:{slug}` and `api_routes:{apiDeploymentId}` are published by `AppSpecificationPublisher` when:

- API deployment is published
- SPA build completes and deployment is activated
- app is renamed (new slug), state is changed, CORS is updated, or routing config is updated
- API routes are created, updated, deleted, cloned, folder-duplicated, or folder-deleted
- variables are created, updated, or deleted
- legacy apps are bootstrapped into API deployments

`app:{slug}` is invalidated when:

- app is deleted
- app is renamed (old slug key removed)
- authentication scheme changes occur (create, update, delete, settings)

### TTL

- Publisher cache TTL for `app:{slug}` and pre-warmed `api_routes:{apiDeploymentId}`: 30 days
- Gateway in-memory cache for routes: 5 minutes
- Gateway Redis cache TTL for `route_meta:{deploymentId}:{routeId}`: 1 hour
