/**
 * This file is the entry point for the static React app, it sets up the root
 * element and renders the App component to the DOM.
 *
 * It is included in `src/static/index.html`.
 */

import { createRoot } from "react-dom/client";
import { App } from "./../App";

createRoot(document.getElementById("root")!).render(<App />);
