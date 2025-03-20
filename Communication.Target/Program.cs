// See https://aka.ms/new-console-template for more information
using Communication.Sample.Host;

Console.WriteLine("Hello, World!");
var sdrDeviceHost = new SDRDeviceHost();
await sdrDeviceHost.Start();