Explanations
====================================
- `/PopReadPixels.Windows/` contains a visual studio (community 2015) solution which [with soylib] will build a 32 or 64 bit DLL
- `/PopReadPixels.Android/` contains the minimum makefiles and a build script (for OSX) to build and android .so (shared object library). Currently configured for ARM (armeabi-v7a), but this could be easily changed to build X86 and whatever else NDK supplies compilers for.
- `/PopReadPixels.xcodeproj/` contains an XCode project to build OSX and IOS libraries. A .a staticcly linked library on ios (IOS doesnt allow dynamic linking) and a .bundle for OSX (which essentially contains a dylib/shared library binary). It also can build android like a normal target/scheme.
- `SoyLib` (under the `Source/` directory) is @soylentgraham 's open source C++14 library which does a lot, but most importantly in this case handles a lot of unity quirks and implements opengl, directx, metal, etc handling for textures (as well as other things) 
- `/Source/` is all the c++ code for the libraries.
- `/Unity/PopReadPixels/` contains a demo project as well as the c# PopReadPixels interface, and .meta files for all the platform library files which tell unity which platforms each file applies to. (contained in the PopReadPixels subdirectory, so only assets under here need to be exported to a package)
- `PopUnityCommon` inside the Unity/Demo project is an open source collection of c# scripts to help unity. Used only for the demo 

Temporary fixes
====================================
- In `SoyLib/SoyDebug.h` comment out `#define SOYDEBUG_ENABLE`. This disables all platform logging. There were some issues using my internal debug logging with the debug output in c#. This may not actually be neccessary, but during development, this was done.

Building Platform Libraries
====================================

All
------------------------------------
- Checkout this repository and initialise `soylib` submodule

Windows
------------------------------------
- Open the `/PopReadPixels.Windows/PopReadPixels.Windows.sln` solution.
- Ensure PopReadPixels is the *startup project* (visual studio can sometimes set this wrong)
- Select the correct platform (x64 usually, but you may want a 32bit/win32 build)
- Build solution
- A successfull build will copy the x64/win32 DLL to the `Unity/PopReadPixels/Assets/PopReadPixels/` appropriate directory
- If unity is open and has used the DLL in the editor, the DLL will be locked and visual studio should report an error. Quitting/restarting unity should fix this. If the DLL hasn't been used (ie, not pressed play) the DLL should be okay to be overriden.
- Sometimes the meta settings (shown in the inspector when the DLL is selected) for the `.DLL` may be lost by unity (this happens often on upgrading unity versions as 5.3 -> 5.6 the meta data/yaml setup changed and this information was often lost)

OSX
------------------------------------
- Download & install the 10.10 OSX SDK into xcode https://github.com/phracker/MacOSX-SDKs
 - Note: this may no longer be neccessary. Plugins for unity 5.3, 5.4 or so required 10.10 and not 10.11 (most modern at the time) but this may not be true any more.
- Open the `/PopReadPixels.xcodeproj` project with xcode
- Change the scheme to `PopReadPixels_OSX` and build
- Like windows, a successfull build will copy a `PopReadPixels_OSX.bundle` into the appropriate directory (via `Build Phases`)
- Unlike windows, if the Library is in use in the editor, NO ERROR will be thrown, but unity will continue using the old library. Re-opening the project will fix this.
- Like windows, check the meta settings for the bundle in the inspector if you get `DLLNotFoundException` errors.

IOS
-------------------------------------
- Open the `/PopReadPixels.xcodeproj` project with xcode
- Change the scheme to `PopReadPixels_IOS` and build
- A successfull build will copy a `PopReadPixels_IOS.a` into the appropriate directory (via `Build Phases`)
- Check meta for the `.a` static library file to make sure it's setup for IOS.

Android
-------------------------------------
- Ensure the android NDK is installed. 
 - I personally use [homebrew](brew.sh) and install android with the command `brew install android-sdk` and `brew install android-ndk`
- Open the `/PopReadPixels.xcodeproj` project with xcode
- Change the scheme to `PopReadPixels_Android` and build
- A successfull build will copy a `PopReadPixels_Android.so` into the appropriate directory (via the build script `build.sh`)
- Check meta for the `.so` static library file to make sure it's setup for Android.
