#!/bin/sh

# require param
ACTION="$1"

DEFAULT_ACTION="release"

if [ "$ACTION" == "" ]; then
	echo "Defaulting build ACTION to $DEFAULT_ACTION"
	ACTION=$DEFAULT_ACTION
#	echo "Android/build.sh: No action specified"
#	exit 1;
fi


if [ -z "$ANDROID_API" ]; then
	ANDROID_API="23"
fi

MAXCONCURRENTBUILDS=16

echo "Android targets..."
android list targets

echo "Update android project"
android update project -t android-$ANDROID_API -p . -s

# set android NDK dir
if [ -z "$ANDROID_NDK" ]; then
	echo "ANDROID_NDK env var not set"
	exit 1
fi


#We never pass NDK_DEBUG=1 to vrlib as this generates a duplicate gdbserver
#instead the app using vrlib can set it 
if [ $ACTION == "release" ]; then
	echo "Android/build.sh: $ACTION..."
	$ANDROID_NDK/ndk-build -j$MAXCONCURRENTBUILDS NDK_DEBUG=0
	RESULT=$?

	if [[ $RESULT -ne 0 ]]; then
		exit $RESULT
	fi

	echo "Asset path"
	echo

	SRC_PATH="libs/armeabi-v7a/libPopReadPixels.so"
	DEST_PATH="$UNITY_ASSET_PLUGIN_PATH/Android"
	echo "Copying $SRC_PATH to $DEST_PATH"

	mkdir -p $DEST_PATH && cp $SRC_PATH $DEST_PATH

	RESULT=$?
	if [[ $RESULT -ne 0 ]]; then
		exit $RESULT
	fi

	exit 0
fi

if [ $ACTION == "clean" ]; then
	echo "Android/build.sh: Cleaning..."
	$ANDROID_NDK/ndk-build clean NDK_DEBUG=0
	#ant clean
	exit $?
fi


# havent exit'd, don't know this command
echo "Android/build.sh: Unknown command $ACTION"
exit 1
