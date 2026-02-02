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
  buildCommand: string;
  outDir: string;
}

export type RenameFormInput = { name: string };
