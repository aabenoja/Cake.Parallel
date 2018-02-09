using System.Reflection;
using System.Runtime.InteropServices;
using Cake.Core.Annotations;
using Cake.Parallel.Module;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Cake.Parallel")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Extend Health, Inc.")]
[assembly: AssemblyProduct("Cake.Parallel")]
[assembly: AssemblyCopyright("Copyright © Extend Health, Inc. 2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("f7b7373b-3c5a-4061-9bfd-8b82b16db5b8")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("0.20.0")]
[assembly: AssemblyInformationalVersion("0.20.0-rc")]
[assembly: AssemblyFileVersion("0.20.0")]

[assembly: CakeModule(typeof(ParallelCakeModule))]
