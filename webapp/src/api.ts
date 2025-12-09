import type { User } from "./types/user";

export class ApiError extends Error {
  public override readonly name: string = "ApiError";
  public readonly code: number | undefined;

  constructor(message: string, code?: number) {
    super(message);
    this.code = code;

    Object.setPrototypeOf(this, ApiError.prototype);
  }
}

export type AuthProvider = "google";

function createCsrfTokenHeader() {
  const match = document.cookie.match(/XSRF-TOKEN=([^;]+)/);
  return { "X-XSRF-TOKEN": (match && match[1]) || "" };
}

export function authCreateLoginUrl(provider: AuthProvider): string {
  return `/auth/login?provider=${provider}`;
}

export async function logout(): Promise<void> {
  const response = await fetch("/auth/logout", {
    method: "POST",
    headers: createCsrfTokenHeader(),
  });

  if (!response.ok) {
    throw new ApiError(
      `${response.statusText}: Failed to log out.`,
      response.status,
    );
  }
}

export function isAuthError(error: unknown): boolean {
  return (
    error instanceof ApiError && (error.code === 401 || error.code === 403)
  );
}

export async function getAuthenticatedUser(): Promise<User> {
  let response;
  try {
    response = await fetch("/api/users/me", {
      headers: createCsrfTokenHeader(),
    });
  } catch (err) {
    const message = "Failed to authenticate current user.";
    throw new ApiError(
      err instanceof Error ? `${message} ${err.message}` : message,
    );
  }

  if (!response.ok) {
    throw new ApiError(
      `${response.statusText}: Failed to authenticate current user.`,
      response.status,
    );
  }

  return (await response.json()) as User;
}
