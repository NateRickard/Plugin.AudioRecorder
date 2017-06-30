# Audio Recorder plugin for Xamarin and Windows ![NuGet](https://img.shields.io/nuget/v/Plugin.AudioRecorder.svg?label=NuGet)

Records audio on a device's microphone input.

# Setup

- Available on Nuet: https://www.nuget.org/packages/Plugin.AudioRecorder
- Install into your PCL project and any platform-specfiic libraries required for your app.

## Platform Support

|Platform|Supported|Version|
| ------------------- | :-----------: | :------------------: |
|Xamarin.iOS|Yes|iOS 7+|
|Xamarin.iOS Unified|Yes|iOS 7+|
|Xamarin.Android|Yes|API 10+|
|Windows Phone Silverlight|No||
|Windows Phone RT|No||
|Windows Store RT|No||
|Windows 10 UWP|Yes|10+|
|Xamarin.Mac|No||

**_Supports both classic Xamarin.iOS / Xamarin.Android and Xamarin.Forms_**

## Usage

In a controller/activity/page, initialize a new `AudioRecorderService` and listen for the `AudioInputReceived` event:

```C#
AudioRecorderService recorder = new AudioRecorderService ();recorder.AudioInputReceived += Recorder_AudioInputReceived;
```

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

Complete samples are available in the /Samples folder.


### Properties & Settings


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