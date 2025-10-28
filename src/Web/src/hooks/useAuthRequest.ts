import { useAuth0 } from "@auth0/auth0-react";
import { useCallback } from "react";

type RequestOptions = {
  headers?: Record<string, string>;
  body?: any;
  method?: string;
  //[key: string]: any;
};

const useAuthRequest = () => {
  const { getAccessTokenSilently, isAuthenticated } = useAuth0();

  const authRequest = useCallback(
    async <T>(url: string, options: RequestOptions = {}): Promise<T> => {
      if (!isAuthenticated) {
        throw new Error("User is not authenticated");
      }

      const token = await getAccessTokenSilently();

      const headers: Record<string, string> = {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
        ...options.headers,
      };

      const response = await fetch(url, {
        ...options,
        headers,
        body:
          options.body && typeof options.body !== "string"
            ? JSON.stringify(options.body)
            : options.body,
      });

      const contentType = response.headers.get("content-type");
      const data = contentType?.includes("application/json")
        ? await response.json().catch(() => ({}))
        : await response.text();

      if (!response.ok) {
        const message =
          (data as any)?.message || `Request failed with ${response.status}`;
        throw new Error(message);
      }

      return data as T;
    },
    [getAccessTokenSilently, isAuthenticated]
  );

  const get = useCallback(
    async <T>(url: string): Promise<T> => {
      return authRequest<T>(url, { method: "GET" });
    },
    [authRequest]
  );

  const post = useCallback(
    async <T>(url: string, body?: any): Promise<T> => {
      return authRequest<T>(url, { method: "POST", body });
    },
    [authRequest]
  );

  return { authRequest, get, post };
};

export default useAuthRequest;

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
