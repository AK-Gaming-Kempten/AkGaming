#!/bin/sh
set -eu
node /app/initialize-storage.cjs
exec "$@"
