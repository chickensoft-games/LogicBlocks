#!/bin/bash

dotnet build

dotnet test \
  -p:CollectCoverage=true \
  -p:CoverletOutputFormat="opencover" \
  -p:CoverletOutput=./coverage/

reportgenerator \
  -reports:"./coverage/coverage.opencover.xml" \
  -targetdir:"./coverage/report" \
  -reporttypes:Html

reportgenerator \
  -reports:"./coverage/coverage.opencover.xml" \
  -targetdir:"./coverage/report" \
  -reporttypes:"Html;Badges"

# Copy badges into their own folder. The badges folder should be included in
# source control so that the README.md in the root can reference the badges.

mkdir -p ./badges
mv ./coverage/report/badge_branchcoverage.svg ./badges/branch_coverage.svg
mv ./coverage/report/badge_linecoverage.svg ./badges/line_coverage.svg

# Determine OS, open coverage accordingly.

case "$(uname -s)" in

   Darwin)
      echo 'Mac OS X'
      open coverage/report/index.htm
     ;;

   Linux)
      echo 'Linux'
      xdg-open coverage/report/index.htm
     ;;

   CYGWIN*|MINGW32*|MSYS*|MINGW*)
      echo 'MS Windows'
      start coverage/report/index.htm
     ;;

   *)
      echo 'Other OS'
      ;;
esac
