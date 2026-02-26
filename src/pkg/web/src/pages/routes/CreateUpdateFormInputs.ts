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
  response: {
    type: string;
    staticResponse: {
      statusCode?: number;
      headers?: HeaderInput[];
      content?: string | null;
    } | null;
    functionResponse: {
      sourceCode?: string | null;
    } | null;
  };
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
    response: yup
      .object({
        type: yup.string().required().oneOf(["Static", "Function"]),
        staticResponse: yup
          .object({
            statusCode: yup.number().min(100).max(599),
            headers: yup.array().of(
              yup.object({
                name: yup.string().required(),
                value: yup.string().required(),
              }),
            ),
            content: yup.string().nullable(),
          })
          .nullable(),
        functionResponse: yup
          .object({
            sourceCode: yup.string().nullable(),
          })
          .nullable(),
      })
      .required()
      .test(
        "response-shape",
        "Invalid response configuration",
        (value, context) => {
          if (!value) return false;
          if (value.type === "Static") {
            if (value.functionResponse) {
              return context.createError({
                path: "response.functionResponse",
                message: "functionResponse is not allowed for Static response",
              });
            }
            return true;
          }
          if (value.type === "Function") {
            if (value.staticResponse) {
              return context.createError({
                path: "response.staticResponse",
                message: "staticResponse is not allowed for Function response",
              });
            }
            if (!value.functionResponse?.sourceCode?.trim()) {
              return context.createError({
                path: "response.functionResponse.sourceCode",
                message: "Function source code is required",
              });
            }
            return true;
          }
          return false;
        },
      ),
    enabled: yup.boolean().required(),
  });
