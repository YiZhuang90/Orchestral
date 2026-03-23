from __future__ import annotations

import argparse
import json
import os
import sys
import tempfile
import time
from ctypes import string_at
from pathlib import Path

from PIL import Image


def _load_mvsdk():
    repo_root = Path(__file__).resolve().parents[3]
    vendor_path = repo_root / "legacy" / "pipe-flow-reference" / "vendor"
    sys.path.insert(0, str(vendor_path))
    import mvsdk  # type: ignore

    return mvsdk


mvsdk = _load_mvsdk()


def _camera_info_to_dict(index: int, camera_info) -> dict:
    return {
        "index": index,
        "product_name": camera_info.GetProductName(),
        "friendly_name": camera_info.GetFriendlyName(),
        "port_type": camera_info.GetPortType(),
        "serial_number": camera_info.GetSn(),
        "sensor_type": camera_info.GetSensorType(),
        "instance": int(camera_info.uInstance),
    }


def _success(**payload):
    payload["ok"] = True
    print(json.dumps(payload, ensure_ascii=False), flush=True)


def _failure(summary: str, diagnostics: list[str] | None = None, **payload):
    payload["ok"] = False
    payload["summary"] = summary
    payload["diagnostics"] = diagnostics or []
    print(json.dumps(payload, ensure_ascii=False))
    raise SystemExit(1)


def _list_cameras() -> None:
    cameras = mvsdk.CameraEnumerateDevice()
    camera_payload = [_camera_info_to_dict(index, camera) for index, camera in enumerate(cameras)]
    _success(
        summary=f"Discovered {len(camera_payload)} HuaTeng camera(s).",
        cameras=camera_payload,
        diagnostics=[
            "SDK module imported through vendor mvsdk.py",
            f"CameraEnumerateDevice returned {len(camera_payload)} device(s)",
        ],
    )


def _resolve_output_path(output_path: str | None, serial_number: str, default_extension: str = ".png") -> str:
    if output_path:
        target = Path(output_path)
        target.parent.mkdir(parents=True, exist_ok=True)
        return str(target)

    safe_serial = serial_number or "camera"
    return str(Path(tempfile.gettempdir()) / f"huateng_{safe_serial}_latest{default_extension}")


def _pixel_format_to_sdk(pixel_format: str, mono_sensor: bool) -> tuple[int, str, int]:
    normalized = pixel_format.lower()
    if normalized == "auto":
        normalized = "mono8" if mono_sensor else "bgr8"

    if normalized == "mono8":
        return mvsdk.CAMERA_MEDIA_TYPE_MONO8, "Mono8", 1
    if normalized == "bgr8":
        return mvsdk.CAMERA_MEDIA_TYPE_BGR8, "Bgr8", 3

    raise ValueError(f"Unsupported pixel format: {pixel_format}")


def _trigger_mode_to_sdk(trigger_mode: str) -> tuple[int, str]:
    normalized = trigger_mode.lower()
    if normalized == "continuous":
        return 0, "Continuous"
    if normalized == "triggered":
        return 1, "Triggered"

    raise ValueError(f"Unsupported trigger mode: {trigger_mode}")


def _normalize_roi(roi_x: int | None, roi_y: int | None, roi_width: int | None, roi_height: int | None, max_width: int, max_height: int) -> tuple[int, int, int, int] | None:
    if roi_width is None or roi_height is None:
        return None

    x = max(0, int(roi_x or 0))
    y = max(0, int(roi_y or 0))
    width = max(2, int(roi_width))
    height = max(2, int(roi_height))

    # Most industrial camera ROI engines prefer even coordinates and sizes.
    x -= x % 2
    y -= y % 2
    width -= width % 2
    height -= height % 2

    width = min(width, max_width)
    height = min(height, max_height)
    x = min(x, max_width - width)
    y = min(y, max_height - height)
    x = max(0, x)
    y = max(0, y)

    return x, y, width, height


def _apply_camera_roi(camera_handle, capability, roi: tuple[int, int, int, int] | None, diagnostics: list[str]) -> tuple[int, int, int, int] | None:
    if roi is None:
        return None

    resolution = mvsdk.CameraGetImageResolution(camera_handle)
    resolution.iIndex = 0xFF
    x, y, width, height = roi
    diagnostics.append(f"CameraSetImageResolution -> ROI x={x}, y={y}, width={width}, height={height}")

    resolution.iHOffsetFOV = x
    resolution.iVOffsetFOV = y
    resolution.iWidthFOV = width
    resolution.iHeightFOV = height
    resolution.iWidth = width
    resolution.iHeight = height
    resolution.iWidthZoomHd = 0
    resolution.iHeightZoomHd = 0
    resolution.iWidthZoomSw = 0
    resolution.iHeightZoomSw = 0
    mvsdk.CameraSetImageResolution(camera_handle, resolution)
    return x, y, width, height


def _save_processed_frame(frame_buffer: int, width: int, height: int, channels: int, output_path: str) -> None:
    payload = string_at(frame_buffer, width * height * channels)
    if channels == 1:
        image = Image.frombytes("L", (width, height), payload)
    else:
        image = Image.frombuffer("RGB", (width, height), payload, "raw", "BGR", 0, 1)

    image = image.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
    image.save(output_path)


def _capture_processed_frame(camera_handle, frame_buffer: int, channels: int, output_path: str, timeout_ms: int = 2000, max_attempts: int = 5) -> tuple[object, list[str]]:
    frame_diagnostics: list[str] = []
    raw_buffer = None
    try:
        frame_head = None
        last_exc = None
        for attempt in range(1, max_attempts + 1):
            try:
                raw_buffer, frame_head = mvsdk.CameraGetImageBuffer(camera_handle, timeout_ms)
                frame_diagnostics.append(f"CameraGetImageBuffer succeeded on attempt {attempt}")
                break
            except Exception as exc:
                last_exc = exc
                frame_diagnostics.append(f"CameraGetImageBuffer timeout on attempt {attempt}: {exc}")
                if attempt == max_attempts:
                    raise

        if frame_head is None:
            raise last_exc if last_exc is not None else RuntimeError("CameraGetImageBuffer failed without frame data.")

        mvsdk.CameraImageProcess(camera_handle, raw_buffer, frame_buffer, frame_head)
        frame_diagnostics.append("CameraImageProcess succeeded")
        _save_processed_frame(frame_buffer, frame_head.iWidth, frame_head.iHeight, channels, output_path)
        frame_diagnostics.append(f"Saved PNG preview to {output_path}")
        return frame_head, frame_diagnostics
    finally:
        if raw_buffer is not None:
            mvsdk.CameraReleaseImageBuffer(camera_handle, raw_buffer)
            frame_diagnostics.append("CameraReleaseImageBuffer succeeded")


def _snapshot(index: int, exposure_us: float | None, trigger_mode: str, pixel_format: str, output_path: str | None, roi_x: int | None, roi_y: int | None, roi_width: int | None, roi_height: int | None) -> None:
    cameras = mvsdk.CameraEnumerateDevice()
    if not cameras:
        _failure("No HuaTeng cameras were discovered over USB.", ["CameraEnumerateDevice returned 0 devices"])

    if index < 0 or index >= len(cameras):
        _failure(
            f"Camera index {index} is out of range.",
            [f"Discovered {len(cameras)} camera(s)", f"Requested camera index {index}"],
        )

    camera_info = cameras[index]
    diagnostics: list[str] = [
        f"Selected camera index: {index}",
        f"Friendly name: {camera_info.GetFriendlyName()}",
        f"Port type: {camera_info.GetPortType()}",
    ]

    camera_handle = None
    raw_buffer = None
    frame_buffer = None

    try:
        camera_handle = mvsdk.CameraInit(camera_info)
        diagnostics.append("CameraInit succeeded")

        capability = mvsdk.CameraGetCapability(camera_handle)
        mono_sensor = capability.sIspCapacity.bMonoSensor != 0
        sdk_pixel_format, pixel_format_label, channels = _pixel_format_to_sdk(pixel_format, mono_sensor)
        sdk_trigger_mode, trigger_mode_label = _trigger_mode_to_sdk(trigger_mode)
        roi = _normalize_roi(
            roi_x,
            roi_y,
            roi_width,
            roi_height,
            capability.sResolutionRange.iWidthMax,
            capability.sResolutionRange.iHeightMax,
        )

        mvsdk.CameraSetIspOutFormat(camera_handle, sdk_pixel_format)
        diagnostics.append(f"CameraSetIspOutFormat -> {pixel_format_label}")

        mvsdk.CameraSetTriggerMode(camera_handle, sdk_trigger_mode)
        diagnostics.append(f"CameraSetTriggerMode -> {trigger_mode_label}")

        applied_roi = _apply_camera_roi(camera_handle, capability, roi, diagnostics)

        if exposure_us is not None:
            mvsdk.CameraSetExposureTime(camera_handle, exposure_us)
            diagnostics.append(f"CameraSetExposureTime -> {exposure_us:.0f} us")

        actual_exposure = mvsdk.CameraGetExposureTime(camera_handle)
        diagnostics.append(f"CameraGetExposureTime -> {actual_exposure:.0f} us")

        mvsdk.CameraPlay(camera_handle)
        diagnostics.append("CameraPlay succeeded")

        buffer_size = capability.sResolutionRange.iWidthMax * capability.sResolutionRange.iHeightMax * channels
        frame_buffer = mvsdk.CameraAlignMalloc(buffer_size, 16)
        diagnostics.append(f"Allocated frame buffer: {buffer_size} bytes")

        output = _resolve_output_path(output_path, camera_info.GetSn(), ".png")
        frame_head, frame_diagnostics = _capture_processed_frame(camera_handle, frame_buffer, channels, output)
        diagnostics.extend(frame_diagnostics)

        _success(
            summary=f"Captured one frame from {camera_info.GetFriendlyName()}.",
            camera=_camera_info_to_dict(index, camera_info),
            image_path=output,
            width=int(frame_head.iWidth),
            height=int(frame_head.iHeight),
            pixel_format=pixel_format_label,
            trigger_mode=trigger_mode_label,
            exposure_us=float(actual_exposure),
            roi=None if applied_roi is None else {
                "x": applied_roi[0],
                "y": applied_roi[1],
                "width": applied_roi[2],
                "height": applied_roi[3],
            },
            is_mono=mono_sensor,
            timestamp_0_1ms=int(frame_head.uiTimeStamp),
            captured_at_utc=time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
            diagnostics=diagnostics,
        )
    except Exception as exc:
        diagnostics.append(f"Capture failed: {exc}")
        _failure("HuaTeng camera snapshot failed.", diagnostics, exception=str(exc))
    finally:
        if raw_buffer is not None and camera_handle is not None:
            try:
                mvsdk.CameraReleaseImageBuffer(camera_handle, raw_buffer)
            except Exception:
                pass

        if frame_buffer is not None:
            try:
                mvsdk.CameraAlignFree(frame_buffer)
            except Exception:
                pass

        if camera_handle is not None:
            try:
                mvsdk.CameraUnInit(camera_handle)
            except Exception:
                pass


def _live(index: int, exposure_us: float | None, trigger_mode: str, pixel_format: str, output_path: str | None, fps: float, roi_x: int | None, roi_y: int | None, roi_width: int | None, roi_height: int | None) -> None:
    cameras = mvsdk.CameraEnumerateDevice()
    if not cameras:
        _failure("No HuaTeng cameras were discovered over USB.", ["CameraEnumerateDevice returned 0 devices"])

    if index < 0 or index >= len(cameras):
        _failure(
            f"Camera index {index} is out of range.",
            [f"Discovered {len(cameras)} camera(s)", f"Requested camera index {index}"],
        )

    camera_info = cameras[index]
    camera_handle = None
    frame_buffer = None

    try:
        camera_handle = mvsdk.CameraInit(camera_info)
        capability = mvsdk.CameraGetCapability(camera_handle)
        mono_sensor = capability.sIspCapacity.bMonoSensor != 0
        sdk_pixel_format, pixel_format_label, channels = _pixel_format_to_sdk(pixel_format, mono_sensor)
        sdk_trigger_mode, trigger_mode_label = _trigger_mode_to_sdk(trigger_mode)
        roi = _normalize_roi(
            roi_x,
            roi_y,
            roi_width,
            roi_height,
            capability.sResolutionRange.iWidthMax,
            capability.sResolutionRange.iHeightMax,
        )

        mvsdk.CameraSetIspOutFormat(camera_handle, sdk_pixel_format)
        mvsdk.CameraSetTriggerMode(camera_handle, sdk_trigger_mode)
        applied_roi = _apply_camera_roi(camera_handle, capability, roi, [])
        if exposure_us is not None:
            mvsdk.CameraSetExposureTime(camera_handle, exposure_us)

        actual_exposure = mvsdk.CameraGetExposureTime(camera_handle)
        mvsdk.CameraPlay(camera_handle)

        buffer_size = capability.sResolutionRange.iWidthMax * capability.sResolutionRange.iHeightMax * channels
        frame_buffer = mvsdk.CameraAlignMalloc(buffer_size, 16)
        output = _resolve_output_path(output_path, camera_info.GetSn(), ".bmp")
        frame_interval = 1.0 / fps if fps > 0 else 0

        while True:
            frame_started = time.perf_counter()
            frame_head, frame_diagnostics = _capture_processed_frame(camera_handle, frame_buffer, channels, output)
            _success(
                summary=f"Captured live frame from {camera_info.GetFriendlyName()}.",
                camera=_camera_info_to_dict(index, camera_info),
                image_path=output,
                width=int(frame_head.iWidth),
                height=int(frame_head.iHeight),
                pixel_format=pixel_format_label,
                trigger_mode=trigger_mode_label,
                exposure_us=float(actual_exposure),
                roi=None if applied_roi is None else {
                    "x": applied_roi[0],
                    "y": applied_roi[1],
                    "width": applied_roi[2],
                    "height": applied_roi[3],
                },
                is_mono=mono_sensor,
                timestamp_0_1ms=int(frame_head.uiTimeStamp),
                captured_at_utc=time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
                diagnostics=[
                    f"Selected camera index: {index}",
                    f"Friendly name: {camera_info.GetFriendlyName()}",
                    f"Port type: {camera_info.GetPortType()}",
                    "CameraInit succeeded",
                    f"CameraSetIspOutFormat -> {pixel_format_label}",
                    f"CameraSetTriggerMode -> {trigger_mode_label}",
                    *([f"CameraSetImageResolution -> ROI x={applied_roi[0]}, y={applied_roi[1]}, width={applied_roi[2]}, height={applied_roi[3]}"] if applied_roi is not None else []),
                    f"CameraGetExposureTime -> {actual_exposure:.0f} us",
                    "CameraPlay succeeded",
                    f"Allocated frame buffer: {buffer_size} bytes",
                    *frame_diagnostics,
                ],
            )

            elapsed = time.perf_counter() - frame_started
            remaining = frame_interval - elapsed
            if remaining > 0:
                time.sleep(remaining)
    except KeyboardInterrupt:
        return
    except Exception as exc:
        _failure("HuaTeng live stream failed.", [str(exc)], exception=str(exc))
    finally:
        if frame_buffer is not None:
            try:
                mvsdk.CameraAlignFree(frame_buffer)
            except Exception:
                pass

        if camera_handle is not None:
            try:
                mvsdk.CameraUnInit(camera_handle)
            except Exception:
                pass


def main() -> None:
    parser = argparse.ArgumentParser()
    subparsers = parser.add_subparsers(dest="command", required=True)

    subparsers.add_parser("list")

    snapshot = subparsers.add_parser("snapshot")
    snapshot.add_argument("--index", type=int, default=0)
    snapshot.add_argument("--exposure-us", type=float, default=None)
    snapshot.add_argument("--trigger-mode", default="continuous", choices=["continuous", "triggered"])
    snapshot.add_argument("--pixel-format", default="auto", choices=["auto", "mono8", "bgr8"])
    snapshot.add_argument("--output-path", default=None)
    snapshot.add_argument("--roi-x", type=int, default=None)
    snapshot.add_argument("--roi-y", type=int, default=None)
    snapshot.add_argument("--roi-width", type=int, default=None)
    snapshot.add_argument("--roi-height", type=int, default=None)

    live = subparsers.add_parser("live")
    live.add_argument("--index", type=int, default=0)
    live.add_argument("--exposure-us", type=float, default=None)
    live.add_argument("--trigger-mode", default="continuous", choices=["continuous", "triggered"])
    live.add_argument("--pixel-format", default="auto", choices=["auto", "mono8", "bgr8"])
    live.add_argument("--output-path", default=None)
    live.add_argument("--fps", type=float, default=3.0)
    live.add_argument("--roi-x", type=int, default=None)
    live.add_argument("--roi-y", type=int, default=None)
    live.add_argument("--roi-width", type=int, default=None)
    live.add_argument("--roi-height", type=int, default=None)

    args = parser.parse_args()

    if args.command == "list":
        _list_cameras()
        return

    if args.command == "snapshot":
        _snapshot(
            index=args.index,
            exposure_us=args.exposure_us,
            trigger_mode=args.trigger_mode,
            pixel_format=args.pixel_format,
            output_path=args.output_path,
            roi_x=args.roi_x,
            roi_y=args.roi_y,
            roi_width=args.roi_width,
            roi_height=args.roi_height,
        )
        return

    if args.command == "live":
        _live(
            index=args.index,
            exposure_us=args.exposure_us,
            trigger_mode=args.trigger_mode,
            pixel_format=args.pixel_format,
            output_path=args.output_path,
            fps=args.fps,
            roi_x=args.roi_x,
            roi_y=args.roi_y,
            roi_width=args.roi_width,
            roi_height=args.roi_height,
        )
        return

    _failure("Unknown command.")


if __name__ == "__main__":
    main()
