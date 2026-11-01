#!/bin/sh
# Builds the native library for the current OS (64-bit). Run from anywhere.
set -e
cd "$(dirname "$0")"
mkdir -p out
case "$(uname -s)" in
  Darwin*)               gcc -shared -fPIC -O2 -o out/libgymnative.dylib gym_native.c ;;
  MINGW*|MSYS*|CYGWIN*)  gcc -shared -O2 -o out/gymnative.dll gym_native.c ;;
  *)                     gcc -shared -fPIC -O2 -o out/libgymnative.so gym_native.c ;;
esac
echo "Built native library into native/out"
