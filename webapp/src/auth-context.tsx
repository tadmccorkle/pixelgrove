import {
  createContext,
  use,
  useEffect,
  useReducer,
  type ActionDispatch,
} from "react";
import type { User } from "./types/user";
import * as Api from "./api";
import { isAuthError } from "./api";

type Action =
  | { type: "login_request" | "logout" | "unauthenticated" }
  | { type: "login_success"; payload: User }
  | { type: "login_failure" | "logout_failure"; error: string };

type State = { user: User | null; isLoading: boolean; error: string | null };
type Dispatch = ActionDispatch<[Action]>;

const AuthContext = createContext<
  { state: State; dispatch: Dispatch } | undefined
>(undefined);

function saveCurrentUser(user: User): void {
  window.sessionStorage.setItem("currentUser", JSON.stringify(user));
}

function loadCurrentUser(): User | null {
  const currentUser = window.sessionStorage.getItem("currentUser");
  try {
    return currentUser !== null ? JSON.parse(currentUser) : null;
  } catch {
    return null;
  }
}

function deleteCurrentUser(): void {
  window.sessionStorage.removeItem("currentUser");
}

function authReducer(state: State, action: Action): State {
  switch (action.type) {
    case "login_request":
      return { ...state, isLoading: true, error: null };
    case "login_success":
      return { user: action.payload, isLoading: false, error: null };
    case "login_failure":
      return { user: null, isLoading: false, error: action.error };
    case "unauthenticated":
    case "logout":
      return { user: null, isLoading: false, error: null };
    case "logout_failure":
      return { ...state, isLoading: false, error: action.error };
    default:
      throw new Error(`Unsupported auth action: ${JSON.stringify(action)}`);
  }
}

function AuthProvider({ children }: { children: React.ReactNode }) {
  const [state, dispatch] = useReducer(authReducer, {
    user: loadCurrentUser(),
    isLoading: true,
    error: null,
  });

  useEffect(() => {
    if (state.user) {
      saveCurrentUser(state.user);
    } else {
      deleteCurrentUser();
    }
  }, [state.user]);

  useEffect(() => {
    async function getUser() {
      dispatch({ type: "login_request" });
      try {
        const user = await Api.getAuthenticatedUser();
        dispatch({ type: "login_success", payload: user });
      } catch (err) {
        if (isAuthError(err)) {
          dispatch({ type: "unauthenticated" });
        } else {
          dispatch({
            type: "login_failure",
            error: (err as Api.ApiError).message,
          });
        }
      }
    }
    getUser();
  }, []);

  const value = { state, dispatch };
  return <AuthContext value={value}>{children}</AuthContext>;
}

async function authLogout(dispatch: Dispatch): Promise<void> {
  try {
    await Api.logout();
    dispatch({ type: "logout" });
  } catch (err) {
    dispatch({ type: "logout_failure", error: (err as Api.ApiError).message });
  }
}

function useAuth() {
  const context = use(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider.");
  }
  return context;
}

export { AuthProvider, authLogout, useAuth };
