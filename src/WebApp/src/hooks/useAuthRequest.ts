import { useAuth0 } from "@auth0/auth0-react";
import { useCallback } from "react";

type RequestOptions = {
  headers?: Record<string, string>;
  //body?: any;
  //method?: string;
  //accessToken?: string;
  //[key: string]: any;
};

const useAuthRequest = () => {
  const { getAccessTokenSilently, isAuthenticated } = useAuth0();

  const authRequest = useCallback(
    async (url: string, options: RequestOptions = {}) => {
      if (!isAuthenticated) {
        throw new Error("User is not authenticated");
      }

      const token = await getAccessTokenSilently();

      const headers = {
        ...options.headers,
        Authorization: `Bearer ${token}`,
        "Content-Type": "application/json",
      };

      const response = await fetch(url, {
        ...options,
        headers,
      });

      if (!response.ok) {
        const error = await response.json().catch(() => ({}));
        throw new Error(error.message || "Request failed");
      }

      return response.json();
    },
    [getAccessTokenSilently, isAuthenticated],
  );

  return authRequest;
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
