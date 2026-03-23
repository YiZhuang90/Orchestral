using System.Collections.Generic;

namespace ExperimentalControlPlatform.Devices.HuaTeng;

public interface IHuaTengSdk
{
    IReadOnlyList<HuaTengCameraInfo> EnumerateDevices();

    IHuaTengCameraSession OpenCamera(HuaTengCameraInfo camera);

    string GetErrorString(int statusCode);
}
