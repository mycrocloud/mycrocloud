export interface IScheme {
  id: number;
  appId: number;
  type: string;
  name: string;
  description?: string;
  openIdConnectAuthority?: string;
  openIdConnectAudience?: string;
  enabled: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface CorsSettings {
  allowedHeaders?: string[];
  allowedMethods?: string[];
  allowedOrigins?: string[];
  exposeHeaders?: string[];
  maxAgeSeconds?: number;
}

export type CorsFormInputs = {
  allowedOrigins: string;
  allowedMethods: string;
  allowedHeaders: string;
  exposeHeaders: string;
  maxAgeSeconds: string;
};

export type SchemeFormInputs = {
  name: string;
  type: string;
  openIdConnectIssuer?: string;
  openIdConnectAudience?: string;
};

export type AuthOrderInputs = {
  schemes: IScheme[];
};

export interface IGitHubInstallation {
  installationId: number;
  accountLogin: string;
  accountType: string;
}

export interface GitHubRepo {
  id: number;
  name: string;
  fullName: string;
}

export interface IBuildConfig {
  branch: string;
  directory: string;
  installCommand: string;
  buildCommand: string;
  outDir: string;
  nodeVersion: string;
}

export const NODE_VERSIONS = ["20", "22", "24"] as const;
export type NodeVersion = (typeof NODE_VERSIONS)[number];

export type VariableTarget = "Runtime" | "Build" | "All";

export interface IEnvVariable {
  id: number;
  name: string;
  value: string;
  target: VariableTarget;
  isSecret: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface IEnvVariableCreate {
  name: string;
  value: string;
  target: VariableTarget;
  isSecret: boolean;
}

export const VARIABLE_TARGETS: { value: VariableTarget; label: string; description: string }[] = [
  { value: "Build", label: "Build", description: "Available during build only" },
  { value: "Runtime", label: "Runtime", description: "Available at runtime only" },
  { value: "All", label: "All", description: "Available both at build and runtime" },
];

export type RenameFormInput = { name: string };

// Routing Configuration Types
export type RouteMatchType = "prefix" | "exact" | "regex";
export type RouteTargetType = "api" | "spa" | "static" | "redirect";

export interface RouteMatch {
  type: RouteMatchType;
  path: string;
}

export interface RouteTarget {
  type: RouteTargetType;
  stripPrefix?: boolean;
  rewrite?: string | null;
  fallback?: string;
}

export interface Route {
  id?: string;
  priority?: number;
  match: RouteMatch;
  target: RouteTarget;
}

export interface RoutingConfig {
  schemaVersion: string;
  routes: Route[];
}

// Simple mode for UI (v1)
export type RoutingMode = "api" | "spa" | "hybrid";

export interface RoutingConfigFormInput {
  mode: RoutingMode;
  apiPrefix: string;
}
