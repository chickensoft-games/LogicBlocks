# Tidy things up.

dotnet clean

# Remove all bin, obj, and nupkg folders
find . -name "bin" -type d | xargs rm -rf
find . -name "obj" -type d | xargs rm -rf
find . -name "nupkg" -type d | xargs rm -rf

dotnet nuget locals all --clear
dotnet restore --no-cache --force
dotnet build --no-incremental
