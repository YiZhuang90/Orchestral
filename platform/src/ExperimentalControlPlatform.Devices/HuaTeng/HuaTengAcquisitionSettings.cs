namespace ExperimentalControlPlatform.Devices.HuaTeng;

public sealed record HuaTengAcquisitionSettings(
    int CameraIndex,
    HuaTengPixelFormat PixelFormat,
    HuaTengTriggerMode TriggerMode,
    double? ExposureUs,
    double TargetFrameRate,
    HuaTengRoi? Roi);
