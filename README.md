# ZModLauncher

![image](https://user-images.githubusercontent.com/98064221/210580022-cab6eeec-c17b-4334-868d-e70852062a8d.png)

# Introduction

Welcome to the official repository page for ZModLauncher! This mod launcher is designed with all games and mods in mind, providing various features and toolsets that can assist other mod developers in tailoring their mods to their intended audience in a very organized, streamlined, yet simplistic and efficient manner. For example, macro scripts can be used to implement game-specific functionality within the launcher directly, without being intrusive to the user experience and, therefore, the underlying program code. Mods can be configured to launch from their own executable, or to toggle on and off at will. A robust versioning system allows base mods to be updated by the inclusion of patch files. Mods perform automatic cleanup operations when deleted. A mod can even include a link to a web resource to tell a user more about it.

# Backend Developer Guide

ZModLauncher is essentially one source project, however multiple editions of the launcher intended for different purposes can be compiled and distributed with ease using a launcherconfig.json file. The content of the JSON file matches the following structure:

```json
{
	"DropboxRefreshToken": "",
	"DropboxClientId": "",
	"DropboxClientSecret": "",
	"PatreonClientId": "",
	"PatreonClientSecret": "",
	"PatreonCreatorUrl": "",
	"PatreonRedirectUri": "",
	"YouTubeResourceLink": "",
	"TwitterResourceLink": "",
	"RoadmapResourceLink": "https://trello.com/b/CUk6PqKu/zmodlauncher",
	"PrepareLauncherMessage": "PREPARING THE TEST MOD LAUNCHER",
	"RejectTierId": ""
}
```

The launcher utilizes Dropbox as a file hosting platform, although it is not conventional to serve as a proper content-delivery network. Games, mods, and other media and configuration files are uploaded to Dropbox following pre-defined folder and file structures, which the launcher can dynamically read and render in the UI. To specify a new launcher edition, you must create a developer app in Dropbox and then input the appropriate information into the aforementioned configuration file.

Patreon integration is also implemented directly into the launcher, which means you can create an app client for a desired launcher edition on the Patreon creator account of choice, and then input the necessary information into the configuration file, allowing users to subscribe to your campaign and then authorize themselves within the launcher to use it. The ```RejectTierId``` field in the configuration file can be set to prevent patrons of a certain tier from accessing the launcher.

External resource links for YouTube, Twitter, and even the official launcher roadmap, can be specified in the configuration file, which will then dynamically update in the Settings menu of the launcher:

![image](https://user-images.githubusercontent.com/98064221/210592098-6bb94185-30e1-459c-ab2a-61da7517cb30.png)

The ```PrepareLauncherMessage``` field in the configuration file changes the message that appears in the user interface when the launcher is preparing content:

![image](https://user-images.githubusercontent.com/98064221/210592475-0bfcc8c7-324a-4e37-a040-1ad49aabf5d4.png)

#

Every launcher app client registered through Dropbox follows a certain folder structure to retain consistency and maintainability across launcher editions. The root folder for every launcher app folder follows this naming scheme: ```[PublisherName]...Mods```. The root folder is required for the launcher to work properly.

The launcher uses two specific database files constructed in the JSON file format, similar to the internal launcher configuration file, that are placed in the launcher's root folder, respectively named ```games.json``` and ```mods.json```. The two databases can canonically be considered as one singular database, however they are split apart purposefully in order to establish a separation of concerns, to help isolate any user-made errors in one database from affecting the other.

The games database file matches the following JSON structure:

```json
{
    "GAME NAME": {
        "LocalPath": "Game",
        "ExecutablePath": "game.exe"
    }
}
```

Each game entry has a case-insensitive name key, which must match with the game's true name to allow the launcher to automatically determine the intended game's location if it is locally installed through Steam/Epic Games on the user's machine; otherwise, the game's install folder must be manually specified in the launcher. The name of the game's associated folder in Dropbox is also required to match the name key in the JSON.

It is important to note that the ```LocalPath``` property is inherently relative to the target game's base installation folder location. For example, if a game is installed through Steam and is located in ```C:\Program Files (x86)\Steam\steamapps\common\GAME NAME\``` and the base folder has an additional folder named ```Game``` inside of it where the main files are, the ```LocalPath``` should be set to ```Game```. The launcher will then read the full path as ```C:\Program Files (x86)\Steam\steamapps\common\GAME NAME\Game```, which will allow the launcher to detect the game's executable path, ```game.exe```, properly within the folder.

The mods database file matches the following JSON structure, where all properties are entirely optional:

```json
{
    "MOD NAME": {
        "ExecutablePath": "",
        "IsUsingSharedToggleMacro": true,
        "NativeToggleMacroPath": "",
        "ModInfoUri": ""
    }
}
```

Each mod entry has a case-insensitive name key, which must match with the name of the mod's associated folder in Dropbox. The ```ExecutablePath``` property allows you to specify the path to an executable located in the mod's extracted folder location, which then forces the launcher to recognize the mod as a launchable mod. Launchable mods are launched directly by clicking their associated mod card in the launcher:

![image](https://user-images.githubusercontent.com/98064221/210600528-084758ff-4145-4ef4-b16e-bdb97fde8be1.png)

The ```IsUsingSharedToggleMacro``` property defines whether the mod should execute a shared toggle macro script, which is placed in the mod's associated game folder in Dropbox, when the mod is being toggled on and off. This property is ignored if the mod is a launchable mod.

The ```NativeToggleMacroPath``` property defines the file path to a toggle macro script native to the mod's local install path to execute when the mod is toggled on and off. A native toggle macro can be executed alongside a shared toggle macro when the mod is toggled, providing additional flexibility and versatility.

The ```ModInfoUri``` property defines a web link to an external resource, such as a YouTube video or Patreon post, that will then appear as a clickable option in the options dropdown of a mod card in the launcher:

![image](https://user-images.githubusercontent.com/98064221/210601644-10a9b5ed-2582-4625-8bd5-c77f5303b415.png)

#

Every mod folder is placed in their associated game folder in the root folder of the launcher client in Dropbox. For a game to display an image for its item card in the launcher, simply place an image file (supported formats are JPG, JPEG, BMP, GIF, PNG, and WEBP) in its associated game folder. This holds true for mod folders as well. To display an image for a mod, simply place an image file in its respective mod folder.

# Integrity Checks

Sometimes, it is often the case that a given game's file/folder state needs to be adjusted to retain its integrity before beginning to install mods for that game. An integrity check is performed by a separate executable file, called an integrity checker, which is then placed in a game's folder in Dropbox. The filename of the integrity checker is required to contain "integritychecker" for it to be recognized by the launcher, otherwise it is recognized as a shared toggle macro.

When the game's item card is clicked in the launcher for the first time, the launcher will download the game's associated integrity checker to the game's installation folder and execute it to perform the necessary integrity check. Then, the launcher automatically deletes the integrity checker after it has been run, as it is no longer needed again unless the native.manifest file for the launcher is deleted, or if the value for the ```HasRunIntegrityCheck``` property for the associated game in the manifest file is set to false, in which case the integrity check will run once more when the item card is clicked.
