# MRTKExtensionForOculusQuest
This is a Mixed Reality Toolkit (MRTK) extension for Oculus Quest.

My fork will have some improvements over @tarukosu's original repo. I will list those out as they come.

## Demo Video
[![Demo video](https://i.imgur.com/wWzTaAw.png)](https://twitter.com/prvncher/status/1211768281536847872)

# Supported versions
- Unity 2018.4.x (Currently targetting 2018.4.14f1)
- Oculus Integration 12.0
- Mixed Reality Toolkit v2.2.0 (This fork is built targetting an MRTK fork branched off the latest development branch)

# Getting started with my fork
## 1. Clone this repo
Clone this repository, and then make sure to initialize submodules. (Command-line - (git submodule init - git submodule update)).
This will clone my MRTK fork as well, which I use for developing new features on that repo.

## 2. Run SymLink bat
Run bat External/createSymlink.bat by double clicking it.
This will link the MRTK folders cloned via the submodule into the project.

## 3. Import Oculus Integration
Download Oculus Integration 12.0 from Asset Store and import it.
- Alternatively just drag and drop the Oculus folder into Assets/

## 4. Project Configuration Window
MRTK has a Project Configuration modal window that pops up when you first open a project.
In this window, there is a checkbox for MSBuild, which will attempt to add MSBuild to your manifest.json that then adds various DLLs to your project via NuGET.
If like myself, your git folder is not in your drive root, you may run into [errors](https://github.com/microsoft/MixedRealityToolkit-Unity/issues/6972) as I have. For now, it seems that avoiding MSBuild does not raise any problems, but that may change in the future.


# Getting Started on new project (No release packages yet - this applies to @tarukosu's original repo)
## 1. Import Oculus Integration
Download Oculus Integration 12.0 from Asset Store and import it.

## 2. Import MRTK v2
Download and import MRTK v2 unitypackages.  
(https://github.com/microsoft/MixedRealityToolkit-Unity/releases)

## 3. Import MRTKExtensionForOculusQuest
Download and import the latest MRTKExtensionForOculusQuest unitypackage.  
(https://github.com/HoloLabInc/MRTKExtensionForOculusQuest/releases)

## Example scene
The example scenes are under `MixedRealityToolkit.ThirdParty/OculusQuestInput/Scenes`.

# Author
Eric Provencher [@prvncher](https://twitter.com/prvncher)

Modified from: 
Furuta, Yusuke ([@tarukosu](https://twitter.com/tarukosu))

# License
MIT
