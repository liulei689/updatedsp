# RobotArmHelix
3D Simulation, forward and inverse kinematics of a robotic arm in C# using WPF and helix-toolkit

![alt text](https://raw.githubusercontent.com/Gabryxx7/RobotArmHelix/master/Images/robotArmHelix.png)

This is a simple project developed in C# by using [helix-tookit](https://github.com/helix-toolkit/helix-toolkit). In order to compile this project the DLLs of Helix need to be compiled by downloading the source code from the helix repository and compiling them.

The aim of the project was to test forward and inverse kinematics which has been achieved thanks to helix and wpf. The Inverse kinematics has been developed on the base of http://www.alanzucconi.com/2017/04/10/robotic-arms/, it is in fact based on gradient descent.

The 3D models belong to the ABB's IRB 4600 Robotic Arm and can be found here: http://new.abb.com/products/robotics/industrial-robots/irb-4600/irb-4600-cad. Finally, the meshes have been converted from STEP to STL by using FreeCad and simplified for faster processing and better performances with MeshLab using the Qaudric Edge Collapse Decimation semplification filter giving 10.000 faces as input.

Based on the [RAV2](https://github.com/karthikram827/RAV2) project of karthikram827

## Use As Child Window

After converting the project to a class library, you can reference the built DLL from another WPF application and show the window directly:

- Add a project or assembly reference to `RobotArmHelix`.
- Create and show the window from your host app:

```csharp
using RobotArmHelix;

// Option 1: default models path (relative to host's working directory)
var win = new MainWindow();
win.Owner = Application.Current.MainWindow; // optional
win.Show();

// Option 2: provide explicit models folder
var modelsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "3D_Models");
var winWithPath = new MainWindow(modelsPath);
winWithPath.Show();
```

Notes:
- The library no longer defines `App.xaml`; the host application must provide the `Application` entry point.
- Ensure the `3D_Models` folder is deployed next to your host executable, or pass an absolute path via the constructor.
