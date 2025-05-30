import { useAuth0 } from "@auth0/auth0-react";

type RequestOptions = {
  headers?: Record<string, string>;
  body?: any;
  method?: string;
  accessToken?: string;
  [key: string]: any;
};

export default async function request(
  url: string,
  options: RequestOptions = {},
) {
  const { getAccessTokenSilently } = useAuth0();

  const {
    headers,
    body,
    method = "GET",
    accessToken,
    ...restOfOptions
  } = options;

  const token = accessToken ?? (await getAccessTokenSilently());

  // There should never be a scenario where null is passed as the body,
  // but if ever there is, this logic should change.
  const jsonifiedBody = {
    body: body && typeof body !== "string" ? JSON.stringify(body) : body,
  };

  const fetchOptions = {
    method,
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
      ...headers,
    },
    //credentials: "same-origin" as RequestCredentials,
    ...jsonifiedBody,
    ...restOfOptions,
  };

  return fetch(url, fetchOptions);
}

export function ensureSuccess(response: Response, message?: string): Response {
  if (response.ok) {
    return response;
  } else {
    if (message) {
      throw new Error(message);
    } else {
      throw new Error(`Failed to fetch: ${response.statusText}`);
    }
  }
}
