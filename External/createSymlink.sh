#!/bin/bash

cd `dirname $0`

FOLDERS_TO_LINK=(
    MixedRealityToolkit
    MixedRealityToolkit.SDK
    MixedRealityToolkit.Services
    MixedRealityToolkit.Providers
    MixedRealityToolkit.Examples
    MixedRealityToolkit.Extensions
    MixedRealityToolkit.Tools
)

for folder in "${FOLDERS_TO_LINK[@]}"
do
ln -s MixedRealityToolkit-Unity/Assets/$folder ../Assets/$folder
ln -s MixedRealityToolkit-Unity/Assets/$folder.meta ../Assets/$folder.meta
done