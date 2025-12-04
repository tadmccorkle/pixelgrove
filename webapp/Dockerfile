FROM oven/bun:1 AS base
WORKDIR /app

WORKDIR /src
COPY ["webapp/package.json", "webapp/bun.lock", "."]
RUN bun install
COPY webapp/ .
CMD ["bun", "dev"]
