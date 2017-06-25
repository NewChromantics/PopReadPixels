LOCAL_PATH := $(call my-dir)


# extra ../ as jni is always prepended
SRC := ../..


# gr: get this from env var
APP_MODULE := PopMovieTexture

# full speed arm instead of thumb
LOCAL_ARM_MODE  := arm

include cflags.mk


#--------------------------------------------------------
# Unity plugin
#--------------------------------------------------------
include $(CLEAR_VARS)

LOCAL_MODULE := PopReadPixels

LOCAL_C_INCLUDES += $(SOY_PATH)/src

LOCAL_STATIC_LIBRARIES :=
#LOCAL_STATIC_LIBRARIES += android-ndk-profiler

LOCAL_LDLIBS	+= -lGLESv3			# OpenGL ES 3.0
LOCAL_LDLIBS	+= -lEGL			# GL platform interface
LOCAL_LDLIBS  	+= -llog			# logging
LOCAL_LDLIBS  	+= -landroid		# native windows
LOCAL_LDLIBS	+= -lz				# For minizip
LOCAL_LDLIBS	+= -lOpenSLES		# audio

# project files
# todo: generate from input from xcode
LOCAL_SRC_FILES  := \
$(SRC)/Source/PopDebug.cpp \
$(SRC)/Source/PopUnity.cpp \
$(SRC)/Source/PopReadPixels.cpp \
$(SRC)/Source/TStringBuffer.cpp \


# soy lib files
LOCAL_SRC_FILES  += \
$(SOY_PATH)/src/SoyOpengl.cpp \
$(SOY_PATH)/src/SoyOpenglContext.cpp \
$(SOY_PATH)/src/SoyAssert.cpp \
$(SOY_PATH)/src/SoyTypes.cpp \
$(SOY_PATH)/src/SoyPixels.cpp \
$(SOY_PATH)/src/SoyPng.cpp \
$(SOY_PATH)/src/SoyDebug.cpp \
$(SOY_PATH)/src/SoyThread.cpp \
$(SOY_PATH)/src/SoyEvent.cpp \
$(SOY_PATH)/src/SoyString.cpp \
$(SOY_PATH)/src/memheap.cpp \
$(SOY_PATH)/src/SoyArray.cpp \
$(SOY_PATH)/src/SoyShader.cpp \
$(SOY_PATH)/src/SoyUnity.cpp \
$(SOY_PATH)/src/SoyBase64.cpp \
$(SOY_PATH)/src/SoyTime.cpp \
$(SOY_PATH)/src/SoyGraphics.cpp \
$(SOY_PATH)/src/SoyJava.cpp \
$(SOY_PATH)/src/SoyStream.cpp \




include $(BUILD_SHARED_LIBRARY)




#$(call import-module,android-ndk-profiler)
