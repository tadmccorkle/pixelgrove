FROM oven/bun:1 AS base
WORKDIR /app

WORKDIR /src
COPY ["webapp/package.json", "webapp/bun.lock", "webapp/"]
WORKDIR ./webapp
RUN bun install
COPY webapp .
COPY . ..
CMD ["bun", "dev"]
