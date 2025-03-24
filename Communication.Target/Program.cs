// See https://aka.ms/new-console-template for more information

using Communication.Target;

var sdrDeviceHost = new SDRDeviceHost();
var sdrUdpDeviceHost = new SDRUDPDeviceHost();
Task.WaitAll([sdrDeviceHost.Start(),sdrUdpDeviceHost.Start()]);

