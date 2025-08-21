#!/usr/bin/env sh
# Usage: ./rm-wap-for-store.sh x.y.z.w
[ "$#" -eq 1 ] || { echo "Usage: $0 x.y.z.w" >&2; exit 2; }
v="$1"
rm -rf -- "WapForStore_${v}_Test"
rm -f  -- "WapForStore_${v}_x86_x64_arm64_bundle.msixupload"
