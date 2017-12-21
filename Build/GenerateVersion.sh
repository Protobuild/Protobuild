#!/bin/bash

# Read the semantic version out of ModuleInfo.xml .... :O
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
rdom () { local IFS=\> ; read -d \< E C ;}
SEMANTIC_VERSION=$(while rdom; do
    if [[ $E = "SemanticVersion" ]]; then
        echo $C
        exit
    fi
done < $SCRIPT_DIR/Module.xml)

# Get build date
BUILD_DATE=`date -u '+%Y-%m-%d %H:%M:%S'`
BUILD_DATE="$BUILD_DATE UTC"

# Get current commit
BUILD_COMMIT=`git rev-parse HEAD`
if [ "$GIT_BRANCH" != "" ]; then
  BUILD_COMMIT="$BUILD_COMMIT; $GIT_BRANCH"
fi 

# Determine if there are pending changes (dirty working directory)
BUILD_IS_DIRTY="false"
if [ "$(git status --porcelain)" != "" ]; then
  BUILD_IS_DIRTY="true"
fi

cat >ProtobuildVersion.cs <<EOF
namespace Protobuild
{
    public static class ProtobuildVersion
    {
        public const string SemanticVersion = "$SEMANTIC_VERSION";

        public const string BuildDate = "$BUILD_DATE";

        public const string BuildCommit = "$BUILD_COMMIT";

        public const bool BuiltWithPendingChanges = $BUILD_IS_DIRTY;
    }
}
EOF