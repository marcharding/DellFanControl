---------

**WARNING - WARNING - WARNING - WARNING**

**This app disables the internal thermal fan controls. Watch your temperatures so that you do not fry you machine. You can damage you machine with this!**

---------

# DellFanControl

[![Build status](https://ci.appveyor.com/api/projects/status/github/marcharding/DellFanControl?svg=true)](https://ci.appveyor.com/project/MarcHarding/dellfancontrol)

## Introduction

This C# app lets you control the fans on some dell laptops. The main purpose is to enable a much more silent laptop. E.g. the fans always spin when an USB-C dock is used etc.

**This app uses the driver https://github.com/424778940z/bzh-windrv-dell-smm-io, without this work the whole control would not be possible.**

## Shortcomings & Warning

For linux dell fan control is possible for quite some time via [i8kutils](https://github.com/vitorafsr/i8kutils).

For windows the situation is a bit trickier: the fan can only be controlled with a special kernel driver. Since Windows 10, version 1607 kernel drives must be signed or they will not load, at least when not without disabling the "Driver Signature Enforcement" via ´bcdedit -set TESTSIGNING ON`.

To overcome the signed driver limitation i used [WindowsD](https://github.com/katlogic/WindowsD), which may be reported as malware by antivirus software.

Furthermore driver only enables three fan speeds (off, 50%, 100%). See https://github.com/vitorafsr/i8kutils/issues/5 for more details.

To complete remove the service execute this in an elevated prompt
```
sc delete BZHDELLSMMIO
```

## Default configuration

In the Default configuration the fans kick in when the CPU reaches 55 °C. First the quieter GPU fan will kick in, when 60 °C are reached the CPU fan kicks in too.

```xml
<DellFanCtrl pollingInterval="1000" minCooldownTime="30">
  <!-- CPU -->
  <FanOne active="1">
    <TemperatureThresholdZero CPU="50" GPU="50"/>
    <TemperatureThresholdOne CPU="60" GPU="60"/>
    <TemperatureThresholdTwo CPU="70" GPU="70"/>
  </FanOne>
  <!-- GPU -->
  <FanTwo active="1">
    <TemperatureThresholdZero CPU="45" GPU="45"/>
    <TemperatureThresholdOne CPU="55" GPU="55"/>
    <TemperatureThresholdTwo CPU="65" GPU="65"/>
  </FanTwo>
</DellFanCtrl>
```

## Further Information / Links

https://github.com/openhardwaremonitor/openhardwaremonitor/issues/56

https://github.com/vitorafsr/i8kutils/issues/5

https://github.com/424778940z/bzh-windrv-dell-smm-io

https://github.com/424778940z/dell-fan-utility

https://github.com/katlogic/WindowsD

