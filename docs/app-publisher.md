# App Publishing (User Flow)

This guide explains app publishing in plain language: what action triggers publishing, and what data is updated.

## What "published" means

When something is published, the platform refreshes runtime data so live requests use the latest app setup.

## Trigger-based flow

### 1. User publishes an API deployment

What happens:

- A versioned API snapshot is created.
- App runtime config is refreshed.
- Route list for that API version is refreshed.

Data updated:

- Redis key `app:{app-name}`
- Redis key `api_routes:{api-deployment-id}`
- Storage files:
  - `routes/{routeId}/meta.json`
  - `routes/{routeId}/content`
  - `routes.json`
  - `openapi.json`

### 2. User deploys SPA and build finishes successfully

What happens:

- Active SPA deployment pointer is updated.
- App runtime config is refreshed.

Data updated:

- Redis key `app:{app-name}`

### 3. User changes app-level settings

Examples:

- rename app
- change app state (active/disabled)
- update CORS
- update routing config

What happens:

- App runtime config is refreshed.
- If app name changed, old app-name cache is removed.

Data updated:

- Redis key `app:{app-name}`
- (rename only) old Redis key `app:{old-app-name}` is removed
- Redis key `api_routes:{api-deployment-id}` may also be refreshed if API deployment is active

### 4. User changes API routes

Examples:

- create/edit/delete route
- clone route
- duplicate/delete route folder

What happens:

- App runtime config is refreshed.
- Route list for active API deployment is refreshed.

Data updated:

- Redis key `app:{app-name}`
- Redis key `api_routes:{api-deployment-id}`

### 5. User changes variables

Examples:

- create/edit/delete variable

What happens:

- App runtime config is refreshed.

Data updated:

- Redis key `app:{app-name}`
- Redis key `api_routes:{api-deployment-id}` may also be refreshed if API deployment is active

### 6. User changes authentication schemes

Examples:

- create/edit/delete auth scheme
- reorder enabled schemes

What happens:

- Existing app runtime cache is invalidated.
- Fresh runtime data is republished on next publish-triggering action.

Data updated:

- Redis key `app:{app-name}` is removed first, then recreated later

## Data dictionary (non-technical)

- `app:{app-name}`: app-level runtime settings (state, active deployments, auth setup, variables, routing settings)
- `api_routes:{api-deployment-id}`: quick route list for an API deployment (method, path, response type)
- `route_meta:{deploymentId}:{routeId}`: detailed route metadata, loaded on demand and cached briefly

## Cache timing

- `app:{app-name}`: up to 30 days (or sooner if invalidated/republished)
- `api_routes:{api-deployment-id}`: up to 30 days (or sooner if republished)
- `route_meta:{deploymentId}:{routeId}`: about 1 hour
