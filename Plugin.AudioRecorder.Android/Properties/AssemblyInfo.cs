using System.Reflection;
using System.Runtime.CompilerServices;
using Android.App;

// Information about this assembly is defined by the following attributes. 
// Change them to the values specific to your project.

[assembly: AssemblyTitle ("Plugin.AudioRecorder.Android")]
[assembly: AssemblyDescription ("")]
[assembly: AssemblyConfiguration ("")]
[assembly: AssemblyCompany ("Nate Rickard")]
[assembly: AssemblyProduct ("")]
[assembly: AssemblyCopyright ("Copyright © 2017 Nate Rickard")]
[assembly: AssemblyTrademark ("")]
[assembly: AssemblyCulture ("")]

// The assembly version has the format "{Major}.{Minor}.{Build}.{Revision}".
// The form "{Major}.{Minor}.*" will automatically update the build and revision,
// and "{Major}.{Minor}.{Build}.*" will update just the revision.

[assembly: AssemblyVersion ("1.0.0.0")]

// The following attributes are used to specify the signing key for the assembly, 
// if desired. See the Mono documentation for more information about signing.

//[assembly: AssemblyDelaySign(false)]
//[assembly: AssemblyKeyFile("")]

[assembly: UsesPermission (Android.Manifest.Permission.WriteExternalStorage)]
[assembly: UsesPermission (Android.Manifest.Permission.ReadExternalStorage)]
[assembly: UsesPermission (Android.Manifest.Permission.RecordAudio)]
[assembly: UsesPermission (Android.Manifest.Permission.ModifyAudioSettings)]