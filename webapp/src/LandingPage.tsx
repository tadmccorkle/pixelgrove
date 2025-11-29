import logo from "./assets/logo.svg";

export function LandingPage() {
  return (
    <div className="flex flex-col items-center justify-center min-h-screen bg-slate-50">
      <div className="text-center p-4">
        <img
          src={logo}
          alt="Pixel Grove Logo"
          className="w-100 h-100 mx-auto mb-6"
        />
        <h1 className="text-4xl font-bold text-slate-800 mb-2">Pixel Grove</h1>
        <p className="text-lg text-slate-600 mb-8">
          Share and discover beautiful images.
        </p>
        <a
          href="/auth/login?provider=google"
          className="inline-flex items-center justify-center px-6 py-3 border border-transparent text-base font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 shadow-sm"
        >
          Login with Google
        </a>
      </div>
    </div>
  );
}
