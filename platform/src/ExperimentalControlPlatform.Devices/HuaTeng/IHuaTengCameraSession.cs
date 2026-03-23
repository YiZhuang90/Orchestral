namespace ExperimentalControlPlatform.Devices.HuaTeng;

public interface IHuaTengCameraSession : System.IDisposable
{
    HuaTengCameraInfo CameraInfo { get; }

    HuaTengCameraCapability Capability { get; }

    void SetOutputFormat(HuaTengPixelFormat pixelFormat);

    void SetTriggerMode(HuaTengTriggerMode triggerMode);

    void SetExposure(double? exposureUs);

    double GetExposure();

    HuaTengRoi? ApplyRoi(HuaTengRoi? roi);

    void Play();

    void SoftTrigger();

    HuaTengRawFrame GetFrame(int timeoutMs);
}
