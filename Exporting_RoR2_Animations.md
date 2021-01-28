# Shifty's RoR2 Animation Export Guide
Version 1.0


### Tools Used
[Asset Studio](https://github.com/Perfare/AssetStudio/releases)

# First Steps
Open Asset Studio.

Find the *Risk of Rain 2_Data* folder in the RoR2 main directory in your Steam library.

Select **Load file** in Asset Studio's File menu, and select the **resources.asset** file.

## Extracting the Animations
To get the animations for the character you want, follow the 3 step process as shown in the image.
1. Click on Assets Tab
2. Search the Data name of the character you wish to find (for example, Engineer is stored as **Engi**)
3. Click on Type to list Alphabetically by type name.  We are looking for all **Animations** and an **Animator** for our character.

![Finding the Data you want](Find_with_AssetStudio.png)

Now simply select all the AnimatorClips and the corresponding Animator file. It will be the one with the dataname prefixed with 'mdl'. In this example it's mdlEngi.

Then click on Export > Animator + selected AnimationClips. It will export as an FBX to the desired location.

Note: you may notice that there is more than 1 Animator. If you export and find the animations not working, try exporting with the other Animator.

![Selecting the data you want](Selecting_Anim_Data.png)

That's it!

