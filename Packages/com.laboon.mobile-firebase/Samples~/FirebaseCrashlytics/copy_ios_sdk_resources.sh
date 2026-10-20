#!/bin/sh
set -eu

# SwiftPM builds resources beside UnityFramework, but does not embed them in it.
# These SDKs look up their bundles relative to their classes in UnityFramework.
destination="${TARGET_BUILD_DIR}/${UNLOCALIZED_RESOURCES_FOLDER_PATH}"
for required in Firebase_FirebaseCore.bundle Firebase_FirebaseCrashlytics.bundle; do
    source="${BUILT_PRODUCTS_DIR}/${required}"
    if [ ! -f "${source}/PrivacyInfo.xcprivacy" ]; then
        echo "error: Missing SwiftPM SDK resource ${required}/PrivacyInfo.xcprivacy" >&2
        exit 1
    fi
    /usr/bin/plutil -lint "${source}/PrivacyInfo.xcprivacy" >/dev/null
done

for source in "${BUILT_PRODUCTS_DIR}"/Firebase_*.bundle \
              "${BUILT_PRODUCTS_DIR}"/GoogleUtilities_*.bundle \
              "${BUILT_PRODUCTS_DIR}"/GoogleDataTransport_*.bundle \
              "${BUILT_PRODUCTS_DIR}"/Promises_*.bundle \
              "${BUILT_PRODUCTS_DIR}"/nanopb_*.bundle; do
    [ -d "$source" ] || continue
    /usr/bin/ditto "$source" "${destination}/$(basename "$source")"
done

for required in Firebase_FirebaseCore.bundle Firebase_FirebaseCrashlytics.bundle; do
    /usr/bin/plutil -lint "${destination}/${required}/PrivacyInfo.xcprivacy" >/dev/null
done
