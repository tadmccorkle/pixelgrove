interface User {
  name: string;
  email: string;
}

interface HomePageProps {
  user: User;
  logout: () => void;
}

export function HomePage({ user, logout }: HomePageProps) {
  const lo = async () => {
    const response = await fetch("/auth/logout", {
      method: "POST",
    });
    if (response.ok) {
      logout();
    }
  };
  return (
    <div className="bg-slate-50 min-h-screen">
      <header className="bg-white shadow-sm">
        <nav className="container mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-4">
            <h1 className="text-xl font-bold text-slate-800">Pixel Grove</h1>
            <div className="flex items-center space-x-4">
              <span className="text-slate-600">Hello, {user.name}</span>
              <a
                onClick={lo}
                className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700"
              >
                Logout
              </a>
            </div>
          </div>
        </nav>
      </header>
      <main className="container mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <h2 className="text-2xl font-semibold text-slate-700 mb-6">
          Your Photo Gallery
        </h2>
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
          {/* Placeholder for photo grid */}
          <div className="bg-white rounded-lg shadow-md aspect-square flex items-center justify-center">
            <p className="text-slate-400">Your images will appear here</p>
          </div>
        </div>
      </main>
    </div>
  );
}
