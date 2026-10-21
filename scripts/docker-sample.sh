#!/bin/sh
set -eu

# The proxy creates its CA on first startup; depends_on alone does not wait for it.
ca_dir=${FASTGITHUB_CA_DIR:-/tmp/cacert}
ca_destination=${FASTGITHUB_CA_DESTINATION:-/usr/local/share/ca-certificates/fastgithub.crt}
attempt=0
while :; do
    if [ -s "$ca_dir/fastgithub.crt" ]; then
        ca_source="$ca_dir/fastgithub.crt"
        break
    fi
    if [ -s "$ca_dir/fastgithub.cer" ]; then
        ca_source="$ca_dir/fastgithub.cer"
        break
    fi
    attempt=$((attempt + 1))
    if [ "$attempt" -ge 60 ]; then
        echo "FastGithub CA certificate was not created in $ca_dir within 60 seconds" >&2
        exit 1
    fi
    sleep 1
done

cp "$ca_source" "$ca_destination"
update-ca-certificates

if [ -d kubernetes/.git ]; then
    echo "Reusing the existing kubernetes checkout"
elif [ -e kubernetes ]; then
    echo "kubernetes exists but is not a Git checkout; move it aside before retrying" >&2
    exit 1
else
    # Only publish a complete checkout, so an interrupted clone can be retried.
    clone_dir=$(mktemp -d .fastgithub-clone.XXXXXX)
    trap 'rm -rf "$clone_dir"' EXIT
    trap 'exit 1' HUP INT TERM
    git clone https://github.com/kubernetes/kubernetes.git "$clone_dir/kubernetes"
    mv "$clone_dir/kubernetes" kubernetes
fi
