// See https://aka.ms/new-console-template for more information
using Communication.Sample.Host;

var sdrDeviceHost = new SDRDeviceHost();
await sdrDeviceHost.Start();