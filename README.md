# Close the Loop

A small 3D time travel puzzle game made in Unity. The theme of the jam was "Power has a price".

Play it on itch.io: https://merijnsnelders.itch.io/closetheloop

## Status

Finished. I made this alone in one weekend for school, in the style of a game jam.

## How it works

Press F and a version of you from the future spawns, and you control him. He steps on a pressure plate, which opens a door. Then you go back, and your recorded movement replays what you did before. He opens the door for you, and you walk through.

After 30 seconds you are warped back to the past. You then have to repeat the actions of your first character, the one you called from the future. If you don't, you create a paradox, because walking through the door has become the recorded movement.

## Technologies

- Unity
- C#

## Features

- A controllable version of yourself from the future, called with Q
- Recorded player movement that plays back as a copy
- Pressure plates and doors
- A 30 second time loop with a paradox if you break it
- Test levels
- A character model

## Project structure

The Unity project is in the `CloseTheLoop/` folder.

## How to run it

The easiest way is to play it on [itch.io](https://merijnsnelders.itch.io/closetheloop).

To open the project yourself:

1. Clone the repository: `git clone https://github.com/MerijnSnelders/CloseTheLoop.git`
2. Open the `CloseTheLoop/` folder with Unity Hub. Unity Hub will show which editor version the project needs.
3. Open the scene and press Play.

I did not record which Unity version I used. The exact version is stored in `ProjectSettings/ProjectVersion.txt`.

## Process

I came up with the idea and built the core loop first. After that I added test levels, and then the idea of getting called back a second time. Last I added a character model. That was the whole weekend.

## What I learned

- Scoping a project to a weekend
- Recording player movement and playing it back
- Working with 3D in Unity

## How it could improve

- More levels
- Proper polish
- Better movement
- A game menu
