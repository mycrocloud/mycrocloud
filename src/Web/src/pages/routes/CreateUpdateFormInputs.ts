import * as yup from "yup";
import type { ObjectSchema } from "yup";
import { methods } from "./constants";

export type RouteCreateUpdateInputs = {
  name: string;
  path: string;
  method: string;
  requireAuthorization: boolean;
  requestQuerySchema?: string | null;
  requestHeaderSchema?: string | null;
  requestBodySchema?: string | null;
  responseType: string;
  responseStatusCode?: number;
  responseHeaders?: HeaderInput[];
  responseBodyLanguage?: string;
  response?: string | null;
  functionHandlerDependencies?: (string | undefined)[];
  useDynamicResponse?: boolean;
  fileId?: number | null;
  enabled: boolean;
};

export interface HeaderInput {
  name: string;
  value: string;
}

export const routeCreateUpdateInputsSchema: ObjectSchema<RouteCreateUpdateInputs> =
  yup.object({
    name: yup.string().required("Name is required"),
    path: yup.string().required().matches(/^\//, "Path must start with /"),
    method: yup
      .string()
      .required()
      .oneOf(methods.map((m) => m.toUpperCase())),
    requireAuthorization: yup.boolean().required(),
    requestQuerySchema: yup.string().nullable(),
    requestHeaderSchema: yup.string().nullable(),
    requestBodySchema: yup.string().nullable(),
    responseType: yup.string().required(),
    responseStatusCode: yup.number(),
    responseHeaders: yup.array().of(
      yup.object({
        name: yup.string().required(),
        value: yup.string().required(),
      }),
    ),
    responseBodyLanguage: yup.string(),
    response: yup.string().nullable().when("responseType", {
      is: "Function",
      then: (schema) => schema.required("Function handler is required"),
    }),
    functionHandlerDependencies: yup.array().of(yup.string()),
    useDynamicResponse: yup.boolean(),
    fileId: yup.number().nullable(),
    enabled: yup.boolean().required(),
  });
