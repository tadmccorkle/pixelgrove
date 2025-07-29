import { build } from "bun";

const result = await build({
  entrypoints: ["./src/index.html"],
  outdir: "./dist",
  sourcemap: true,
  minify: true,
  define: {
    "process.env.NODE_ENV": '"production"',
  },
  env: "BUN_PUBLIC_*",
  plugins: [
    {
      name: "static-react-entry-script-plugin",
      setup({ onLoad }) {
        const rewriter = new HTMLRewriter().on("#pixelgrove-reactentryscript", {
          element(element) {
            element.setAttribute("src", "./static/frontend.tsx");
          },
        });

        onLoad({ filter: /index\.html$/ }, async (args) => {
          const html = await Bun.file(args.path).text();
          return {
            contents: rewriter.transform(html),
            loader: "html",
          };
        });
      },
    },
  ],
});

for (const message of result.logs) {
  console.warn(message);
}
