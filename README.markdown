Dropbox for Screenpresso
===========================================================================================

Dropbox for Screenpresso is a script which copies the public link of the selected image on the history window. It only accepts one.

For the first run, it prompts for the Dropbox UID and the public folder path. Once entered, it verifies if it's valid or not. 
If it passes the verification, then creates an .xml with the data entered, so the next time it will just copy the public link to the clipboard.

Next versions will check if it's available every link copied. Of course, this can be avoided with external resources (DLL's) but I don't want to add dependencies... for now.


Installation
-------------------------------------------------------------------------------------------

Just drop this script on `C:\Users\<USERNAME>\AppData\LearnPulse\Screenpresso\Scripts`, restart the program and done.


First use
-------------------------------------------------------------------------------------------

The first time you execute it, it will ask your Dropbox UID and also the public folder path. If you have no idea what's a Dropbox UID and where to get it,
you can follow [this short tutorial](http://maharbaz.tumblr.com/post/3260527136/how-do-i-obtain-my-dropbox-uid "Algo if you want to follow me...")