import { serve } from "bun";
import index from "./index.html";

const isDev = process.env.NODE_ENV !== "production";

const routes = {
  "/*": index,
  ...(isDev
    ? (() => {
        let authenticated = false;
        return {
          "/auth/login": {
            async GET() {
              authenticated = true;
              return Response.redirect("/");
            },
          },
          "/auth/logout": {
            async POST() {
              authenticated = false;
              return Response.redirect("/");
            },
          },
          "/api/users/me": {
            async GET() {
              return authenticated
                ? Response.json({
                    id: "0",
                    name: "Test User",
                    email: "user@test.com",
                  })
                : new Response("Unauthorized", { status: 401 });
            },
          },
        };
      })()
    : ({} as any)),
};

const server = serve({
  port: process.env.PIXELGROVE_WEBAPP_DEV_PORT || 3001,
  routes,
  development: isDev && {
    // Enable browser hot reloading in development
    hmr: true,

    // Echo console logs from the browser to the server
    console: true,
  },
});

console.log(`🚀 ${isDev ? "Dev server" : "Server"} running at ${server.url}`);
