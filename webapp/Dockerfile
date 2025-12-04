FROM oven/bun:1 AS base
WORKDIR /app

COPY ["webapp/package.json", "webapp/bun.lock", "."]
RUN bun install
COPY webapp/ .
CMD ["bun", "dev"]
