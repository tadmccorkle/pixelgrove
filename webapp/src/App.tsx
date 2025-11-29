import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { APITester } from "./APITester";
import { useState, useEffect } from "react";
import { LandingPage } from "./LandingPage";
import { HomePage } from "./HomePage";
import "./index.css";

import logo from "./logo.svg";
import reactLogo from "./react.svg";

interface User {
  id: string;
  name: string;
  email: string;
}

function getCsrfToken() {
  const match = document.cookie.match(/XSRF-TOKEN=([^;]+)/);
  return match ? match[1] : null;
}

export function App() {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  const logout = () => {
    setUser(null);
  };

  useEffect(() => {
    const fetchUser = async () => {
      try {
        const response = await fetch("/api/users/me", {
          headers: {
            "X-XSRF-TOKEN": getCsrfToken() || "",
          },
        });
        if (response.ok) {
          const userData = await response.json();
          setUser(userData);
        } else if (response.status === 401) {
          setUser(null);
        }
      } catch (error) {
        console.error("Failed to fetch user:", error);
        setUser(null);
      } finally {
        setLoading(false);
      }
    };

    fetchUser();
  }, []);

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <p>Loading...</p>
      </div>
    );
  }

  return user ? <HomePage user={user} logout={logout} /> : <LandingPage />;

  // return (
  //   <div className="container mx-auto p-8 text-center relative z-10">
  //     <div className="flex justify-center items-center gap-8 mb-8">
  //       <img
  //         src={logo}
  //         alt="Bun Logo"
  //         className="h-36 p-6 transition-all duration-300 hover:drop-shadow-[0_0_2em_#646cffaa] scale-120"
  //       />
  //       <img
  //         src={reactLogo}
  //         alt="React Logo"
  //         className="h-36 p-6 transition-all duration-300 hover:drop-shadow-[0_0_2em_#61dafbaa] [animation:spin_20s_linear_infinite]"
  //       />
  //     </div>
  //     <Card>
  //       <CardHeader className="gap-4">
  //         <CardTitle className="text-3xl font-bold">Bun + React</CardTitle>
  //         <CardDescription>
  //           Edit{" "}
  //           <code className="rounded bg-muted px-[0.3rem] py-[0.2rem] font-mono">
  //             src/App.tsx
  //           </code>{" "}
  //           and save to test HMR
  //         </CardDescription>
  //       </CardHeader>
  //       <CardContent>
  //         <APITester />
  //       </CardContent>
  //     </Card>
  //   </div>
  // );
}

export default App;
