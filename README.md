# MapSnap

This small command line tool enables mappers from the OpenStreetMap community to create snapshots and timelapses of their work!

## Features
- **Snapshot**: Capture any region of the rendered OpenStreetMap slippy map, by entering the boundary coordinates and the zoom level you want to want.
- **Projects**: Store the area you want to capture in a project file, so you can capture a new snapshot with just one command! 
- **Filename customization**: Save your images with index numbering or with a date format.
- **Timelapses**: Create an animated gif from captured snapshots!

## Future features
- **Snap anywhere**: Run `snap` from anywhere and provide coordinates to create a single snapshot image. 
- **Output resizing**: Resize output images to a custom size, different from the standard tile size of 256 per tile.

# Documentation
See the [wiki](https://github.com/Creator13/mapsnap/wiki) for the full documentation of the tool.

# Installation
Download the latest binary zip from the releases pages. Extract it in a folder and run mapsnap.exe from the command line.

I'll be looking into creating an installer for mapsnap in the future!
### Add to Path (Windows)
You'll want to run mapsnap from any place on your computer. Follow [this guide](https://stackoverflow.com/questions/44272416/how-to-add-a-folder-to-path-environment-variable-in-windows-10-with-screensho) to add the folder with mapsnap.exe to your PATH in order to run `mapsnap` from a console window.

# Getting started
With mapsnap installed and added to the PATH, you should open a new terminal window. On Windows, you can use Windows Terminal, Command Line (cmd.exe), or PowerShell (ps.exe). On MacOS/Linux, open your system terminal.

## Create a project
To create a new project, run:
```sh
> mapsnap init myProject "52.3990,4.8591" "52.3393,4.9781" 15
```
This will create a new folder and project called myProject of Amsterdam at zoom level 15. For more info on the `init` command, including all the optional parameters, see this [wiki page](https://github.com/Creator13/mapsnap/wiki/init).

### **Where do I find my coordinates and zoom level?**
You can get your coordinates from any source, like Google Maps or openstreetmap.com. Navigate to your area of interest and choose two points as the outermost corners of your image.

**NOTE**: OpenStreetMap tiles come in sizes of 256x256. You can choose whether you want to capture an image at exactly the coordinates you specified by adding the `-P` option to the `init` command. If not specified, it will capture the entire tile in which your coordinates lie, resulting in an image that is slightly bigger than you might expect.
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

For more info on the `snap` command, see this [wiki page](https://github.com/Creator13/mapsnap/wiki/snap).

## Create a GIF
One of the most interesting features is the `gif` command that allows you to turn captured snapshots in an animated gif!

To start, capture at least 2 snapshots in a project. Then, run the gif command:

```sh
> mapsnap gif
```

That simple! A new gif will appear in the project folder.

# Contributing
If you want to contribute, please open an issue or a pull request!

# License
This project is licensed under the GNU GPLv3 license.

Map data & tiles are © OpenStreetMap contributors. Gifs and images created with mapsnap are CC BY-SA 2.0.

# See also
- https://www.openstreetmap.org
- OpenStreetMap tilemap wiki: https://wiki.openstreetmap.org/wiki/Slippy_map_tilename
