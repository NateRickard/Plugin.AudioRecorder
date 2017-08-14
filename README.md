# Audio Recorder plugin for Xamarin and Windows ![NuGet](https://img.shields.io/nuget/v/Plugin.AudioRecorder.svg?label=NuGet)

Records audio on a device's microphone input.

# Setup

- Available on NuGet: https://www.nuget.org/packages/Plugin.AudioRecorder
- Install into your PCL project and any platform-specfiic libraries required for your app.

## Platform Support

|Platform|Supported|Version|
| ------------------- | :-----------: | :------------------: |
|Xamarin.iOS|Yes|iOS 7+|
|Xamarin.iOS Unified|Yes|iOS 7+|
|Xamarin.Android|Yes|API 16+|
|Windows Phone Silverlight|No||
|Windows Phone RT|No||
|Windows Store RT|No||
|Windows 10 UWP|Yes|10+|
|Xamarin.Mac|No||

**_Supports both classic Xamarin.iOS / Xamarin.Android and Xamarin.Forms_**

### Permissions

#### Android

```XML
<uses-permission android:name="android.permission.MODIFY_AUDIO_SETTINGS" />
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.RECORD_AUDIO" />
```

#### iOS

```XML
<key>NSMicrophoneUsageDescription</key>
<string>The [app name] wants to use your microphone to record audio.</string>
```

#### UWP

You must check the `Internet` and `Microphone` capabilities in your app's Package.appxmanifest file.


# Usage

In a controller/activity/page, initialize a new `AudioRecorderService` and listen for the `AudioInputReceived` event:

```C#
AudioRecorderService recorder = new AudioRecorderService ();recorder.AudioInputReceived += Recorder_AudioInputReceived;
```

## Recording

To begin recording, use the `StartRecording ()` and `StopRecording ()` methods as shown:

```C#
async void RecordButton_Click (object sender, EventArgs e){	await RecordAudio ();}async Task RecordAudio (){	try	{		if (!recorder.IsRecording)		{			await recorder.StartRecording ();		}		else		{			await recorder.StopRecording ();		}	}	catch (Exception ex)	{
	...	}}
```

In lieu of calling `StopRecording ()`, you can also make use of the `StopRecordingAfterTimeout` and/or `StopRecordingOnSilence` settings, which are explained below.

The `AudioInputReceived` is raised when recording is complete, and the full filepath of the recorded audio file is passed along:


```C#
private async void Recorder_AudioInputReceived(object sender, string audioFile){
...}
```

**NOTE:** This event is raised on a background thread to allow for further file processing as needed.  If the `audioFile` is null or empty, no audio was recorded.


## Using the Audio Data

There are multiple ways to use the recorded audio data:


### Accessing the Recorded File

The full path to the recorded audio file is contained in the `audioFile` parameter of the `AudioInputReceived` event handler, as shown above.  You can also retrieve the filename directly from the recorder object by calling `GetFilename ()`.

With this file path, you can use standard `FileStream` operations and/or a cross platform file system abstraction like [PCLStorage](https://github.com/dsplaisted/PCLStorage) to get a stream to the file data.

Complete samples showing this type of audio recording and use are available in the /Samples folder.


### Concurrent Streaming

It's also possible to get a stream to the recording audio data as it's being recorded, once `StartRecording ()` has been called.

To access this readonly stream of audio data, you may call the `GetAudioFileStream ()` method.  This is useful in the case you want to immediately begin streaming the audio data to a server or other consumer.  

An example of this type of concurrent writing and reading of the audio data is shown in the sample accompanying the [Xamarin.Cognitive.Speech](https://github.com/NateRickard/Xamarin.Cognitive.BingSpeech) library.


## Properties & Settings


- IsRecording

	```C#
	bool IsRecording
	```

	Returns a value indicating if the AudioRecorderService is currently recording audio.

- StopRecordingAfterTimeout / TotalAudioTimeout

	```C#
	bool StopRecordingAfterTimeout
	```
	
	Gets/sets a value indicating if the AudioRecorderService should stop recording after a certain amount of time.  Default is `true`.

	```C#
	TimeSpan TotalAudioTimeout
	```

	_If_ `StopRecordingAfterTimeout` is set to `true`, this `TimeSpan` indicates the total amount of time to record audio for before recording is stopped. Defaults to 30 seconds.

- StopRecordingOnSilence / AudioSilenceTimeout

	```C#
	bool StopRecordingOnSilence
	```
	
	Gets/sets a value indicating if the AudioRecorderService should stop recording after silence (low audio signal) is detected.  Default is `true`.
	
	```C#
	TimeSpan AudioSilenceTimeout
	```
	_If_ `StopRecordingOnSilence ` is set to `true`, this `TimeSpan` indicates the amount of 'silent' time is required before recording is stopped. Defaults to 2 seconds.

- SilenceThreshold

	```C#
	float SilenceThreshold
	```
	
	Gets/sets a value indicating the signal threshold that determines silence.  If the recorder is being over or under aggressive when detecting silence, you can alter this value to achieve different results.  Defaults to .2. Value should be between 0 and 1.


# Limitations

- Currently this is only recording in WAV audio format (due to original use case this was developed for).
- Signal detection (`StopRecordingOnSilence`) is not currently working well/at all on UWP.


# Contributing

Contributions are welcome.  Feel free to file issues and pull requests on the repo and they'll be reviewed as time permits.


# About

- Created by [Nate Rickard](https://github.com/naterickard)
- AudioRecorderService concept and some original code from [SmartCoffee](https://github.com/pierceboggan/smartcoffee) by [Pierce Boggan](https://github.com/pierceboggan)
- Audio stream and wave recorder classes adapted from [SimplyMobile](https://github.com/sami1971/SimplyMobile) by [Sami M. Kallio](https://github.com/sami1971)


## License

Licensed under the MIT License (MIT). See [LICENSE](LICENSE) for details.