import { useAuth0 } from "@auth0/auth0-react";
import { useCallback } from "react";
import { NotFoundError} from "@/errors"
import { PaginatedResponse, PaginationParams } from "@/models/Pagination";

type RequestOptions = {
  headers?: Record<string, string>;
  body?: any;
  method?: string;
  //[key: string]: any;
};

const useApiClient = () => {
  const { getAccessTokenSilently, isAuthenticated } = useAuth0();

  const send = useCallback(
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

          if (response.status === 404) {
            throw new NotFoundError(message)
          } else {
            throw new Error(message);
          }
      }

      return data as T;
    },
    [getAccessTokenSilently, isAuthenticated]
  );

  const get = useCallback(
    async <T>(url: string): Promise<T> => {
      return send<T>(url, { method: "GET" });
    },
    [send]
  );

  const getPagination = useCallback(
    async <T>(url: string, params: PaginationParams): Promise<PaginatedResponse<T>> => {
      const query = new URLSearchParams({
        page: String(params.page),
        per_page: String(params.per_page),
      }).toString();
      if (url.includes("?")) {
        url += "&" + query;
      } else {
        url += "?" + query;
      }
      return send<PaginatedResponse<T>>(url, { method: "GET" });
    },
    [send]
  );

  const post = useCallback(
    async <T>(url: string, body?: any): Promise<T> => {
      return send<T>(url, { method: "POST", body });
    },
    [send]
  );

  const put = useCallback(
    async <T>(url: string, body?: any): Promise<T> => {
      return send<T>(url, { method: "PUT", body });
    },
    [send]
  );

  const del = useCallback(
    async <T>(url: string, body?: any): Promise<T> => {
      return send<T>(url, { method: "DELETE", body });
    },
    [send]
  );

  return { send, get, getPagination, post, put, del };
};

export default useApiClient;

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
