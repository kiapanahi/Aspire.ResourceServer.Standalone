#! /bin/zsh

# Assign the solution file to a variable
SOLUTION_FILE="Aspire.ResourceService.Standalone.sln"

# Check if the file exists
if [[ ! -f "$SOLUTION_FILE" ]]; then
  echo "Error: Solution file '$SOLUTION_FILE' does not exist."
  exit 1
fi

# Open Rider with the provided solution file
open -na /Applications/Rider.app "$SOLUTION_FILE"
