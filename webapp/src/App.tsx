import { LandingPage } from "./LandingPage";
import { HomePage } from "./HomePage";
import { useAuth } from "./auth-context";

import "./index.css";

function ErrorBanner({ message }: { message: string }) {
  return (
    <div className="fixed top-0 left-0 right-0 bg-red-600 text-white px-4 py-2 text-center text-sm">
      {message}
    </div>
  );
}

export function App() {
  const { state: auth } = useAuth();

  const errorBanner = auth.error ? <ErrorBanner message={auth.error} /> : null;

  if (auth.user) {
    return (
      <>
        {errorBanner}
        <HomePage />
      </>
    );
  }

  if (auth.isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <p>Loading...</p>
      </div>
    );
  }

  return (
    <>
      {errorBanner}
      <LandingPage />
    </>
  );
}

export default App;
