import { APITester } from "./APITester";
import { EventList } from "./EventList";
import "./index.css";
import { Login } from "./Login";

import logo from "./logo.svg";
import reactLogo from "./react.svg";

export function App() {
  return (
    <div className="app">
      <div className="logo-container">
        <img src={logo} alt="Bun Logo" className="logo bun-logo" />
        <img src={reactLogo} alt="React Logo" className="logo react-logo" />
      </div>

      <Login />
      <APITester />
      <EventList />
    </div>
  );
}

export default App;
