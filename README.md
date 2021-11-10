# MapSnap

This small command line tool enables mappers from the OpenStreetMap community to create snapshots and timelapses of their work!

## Features
- **Snapshot**: Capture any region of the rendered OpenStreetMap slippy map, by entering the boundary coordinates and the zoom level you want to want.
- **Projects**: Store the area you want to capture in a project file, so you can capture a new snapshot with just one command! 
- **Filename customization**: Save your images with index numbering or with a date format.

## Future features
- **Snap anywhere**: Run `snap` from anywhere and provide coordinates to create a single snapshot image. 
- **GIF timelapses**: Create timelapses from downloaded files in a project.
- **Output resizing**: Resize output images to a custom size, different from the standard tile size of 256 per tile.

# Installation
Download the latest binary zip from the releases pages. Extract it in a folder and run mapsnap.exe from the command line.

I'll be looking into creating an installer for mapsnap in the future!
### Add to Path
You'll want to run mapsnap from any place on your computer. Follow [this guide](https://stackoverflow.com/questions/44272416/how-to-add-a-folder-to-path-environment-variable-in-windows-10-with-screensho) to add the folder with mapsnap.exe to your PATH in order to run `mapsnap` from a console window.
### Linux/MacOS
Currently, there are no binaries available for Linux or MacOS. The application should be fully compatible with all .NET compatible systems and has no native dependencies. 

To use on Linux or MacOS, you'll need to clone this github repo and compile the source files using [.NET 6.0](https://dotnet.microsoft.com/download/dotnet) or later.

# Getting started
With mapsnap installed and added to the PATH, you'll want to open a new terminal window. On Windows, you can use Windows Terminal, Command Line (cmd.exe), or PowerShell (ps.exe). On MacOS/Linux, open your system terminal.
***
## Create a project
To create a new project, run:
```sh
> mapsnap init myProject "52.3990,4.8591" "52.3393,4.9781" 15
```
This will create a new folder and project called myProject of Amsterdam at zoom level 15. For more info on the `init` command, including all the optional parameters, see this [wiki page (WIP)](#).

### **Where do I find my coordinates and zoom level?**
You can get your coordinates from any source, like Google Maps or openstreetmap.com. Navigate to your area of interest and choose two points as the outermost corners of your image.

**NOTE**: OpenStreetMap tiles come in sizes of 256x256. Depending on your zoom level, the output image will likely be slightly larger than the area you chose, but its coordinates will always fall somewhere in the corner tiles.
#### **Coordinates from openstreetmap.com**
To get coordinates from the OpenStreetMap website, right click on the corner point and select "Show address." The coordinates of the point will appear in the search bar in the left sidebar and can be copied straight into MapSnap.

If your coordinates contain spaces, be sure to surround them with double quotes.

You can also copy the coordinates from the url. Mind that these are the coordinates of the center of the portion of the map you're viewing, so this method can be a little more tricky. Replace the slash between the coordinates with a comma, a space, or a semicolon.

#### **Zoom level**
The zoom level can be found in the url on openstreetmap.com. Navigate to a place and zoom to the level you want, then look at the url:
```
https://www.openstreetmap.org/#map=15/52.3728/4.8936
```
The part with `#map=15` indicates that the zoom level is `15` on this url.

***

## Snap an image
In your terminal, navigate to your project folder. Using your file explorer you can navigate to the folder, then shift+right click somewhere and select "Open in terminal/powershell". 

If you just created a new project called myProject, run:
```sh
> cd myProject
```

Once in your folder, simply run:
```sh
> mapsnap snap
```
After the program is done, your project folder will contain a new map snapshot!

For more info on the `snap` command, see this [wiki page (WIP)](#).

# Contributing
If you want to contribute, please open an issue or a pull request!

# License
This project is licensed under the GNU GPLv3 license.