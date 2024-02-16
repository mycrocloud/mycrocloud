﻿CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Apps" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "UserId" text,
    "Name" text,
    "Description" text,
    "Status" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "CorsSettings" jsonb,
    "Settings" jsonb,
    CONSTRAINT "PK_Apps" PRIMARY KEY ("Id")
);

CREATE TABLE "AuthenticationSchemes" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "AppId" integer NOT NULL,
    "Type" integer NOT NULL,
    "Name" text,
    "Description" text,
    "OpenIdConnectAuthority" text,
    "OpenIdConnectAudience" text,
    "Enabled" boolean NOT NULL,
    "Order" integer,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_AuthenticationSchemes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AuthenticationSchemes_Apps_AppId" FOREIGN KEY ("AppId") REFERENCES "Apps" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Routes" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "AppId" integer NOT NULL,
    "Name" text,
    "Method" text,
    "Path" text,
    "Description" text,
    "ResponseType" text,
    "ResponseStatusCode" integer,
    "ResponseBody" text,
    "ResponseBodyLanguage" text,
    "FunctionHandler" text,
    "FunctionHandlerDependencies" text[],
    "RequireAuthorization" boolean NOT NULL,
    "Status" integer NOT NULL,
    "UseDynamicResponse" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    "ResponseHeaders" jsonb,
    CONSTRAINT "PK_Routes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Routes_Apps_AppId" FOREIGN KEY ("AppId") REFERENCES "Apps" ("Id") ON DELETE CASCADE
);

CREATE TABLE "TextStorages" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "AppId" integer NOT NULL,
    "Name" text,
    "Description" text,
    "Content" text,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_TextStorages" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_TextStorages_Apps_AppId" FOREIGN KEY ("AppId") REFERENCES "Apps" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Variables" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "AppId" integer NOT NULL,
    "Name" text,
    "StringValue" text,
    "ValueType" integer NOT NULL,
    "IsSecret" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Variables" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Variables_Apps_AppId" FOREIGN KEY ("AppId") REFERENCES "Apps" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Logs" (
    "Id" uuid NOT NULL,
    "AppId" integer NOT NULL,
    "RouteId" integer,
    "Method" text,
    "Path" text,
    "StatusCode" integer NOT NULL,
    "AdditionalLogMessage" text,
    "FunctionExecutionDuration" interval,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Logs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Logs_Apps_AppId" FOREIGN KEY ("AppId") REFERENCES "Apps" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Logs_Routes_RouteId" FOREIGN KEY ("RouteId") REFERENCES "Routes" ("Id")
);

CREATE TABLE "RouteValidations" (
    "Id" integer GENERATED BY DEFAULT AS IDENTITY,
    "RouteId" integer NOT NULL,
    "Source" text,
    "Name" text,
    "Rules" text,
    "Expressions" text[],
    CONSTRAINT "PK_RouteValidations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RouteValidations_Routes_RouteId" FOREIGN KEY ("RouteId") REFERENCES "Routes" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AuthenticationSchemes_AppId" ON "AuthenticationSchemes" ("AppId");

CREATE INDEX "IX_Logs_AppId" ON "Logs" ("AppId");

CREATE INDEX "IX_Logs_RouteId" ON "Logs" ("RouteId");

CREATE INDEX "IX_Routes_AppId" ON "Routes" ("AppId");

CREATE INDEX "IX_RouteValidations_RouteId" ON "RouteValidations" ("RouteId");

CREATE INDEX "IX_TextStorages_AppId" ON "TextStorages" ("AppId");

CREATE INDEX "IX_Variables_AppId" ON "Variables" ("AppId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240215091047_Init', '8.0.1');

COMMIT;

