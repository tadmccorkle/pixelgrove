import { LandingPage } from "./LandingPage";
import { HomePage } from "./HomePage";
import { useAuth } from "./auth-context";
import { ErrorAlert } from "./components/ErrorAlert";

import "./index.css";

export function App() {
  const { state: auth } = useAuth();

  if (!auth.user && auth.isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <p>Loading...</p>
      </div>
    );
  }

  return (
    <>
      {auth.user ? <HomePage /> : <LandingPage />}
      <ErrorAlert message={auth.error} />
    </>
  );
}

export default App;
