#!/usr/bin/env bash

set -euo pipefail

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

if [[ $# < 1 ]]
then
    # Perform restore and build, if no args are supplied.
    set -- '.';
fi

code "$@"


