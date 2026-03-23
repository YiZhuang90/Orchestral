from __future__ import annotations

import ctypes
import time
from pathlib import Path


DLL_CANDIDATES = [
    Path(r"C:\Program Files\Pico Technology\SDK\lib\usbpt104.dll"),
    Path(r"C:\Program Files\Pico Technology\PicoLog\usbpt104.dll"),
]

CT_USB = 0x00000001
USBPT104_CHANNEL_4 = 4
USBPT104_PT100 = 1


def load_driver() -> ctypes.WinDLL:
    for candidate in DLL_CANDIDATES:
        if candidate.exists():
            return ctypes.WinDLL(str(candidate))
    raise FileNotFoundError(f"No usbpt104.dll found in: {DLL_CANDIDATES}")


def decode_buffer(raw: bytes) -> str:
    return raw.split(b"\x00", 1)[0].decode("ascii", errors="replace").strip()


def main() -> int:
    dll = load_driver()

    dll.UsbPt104Enumerate.argtypes = [
        ctypes.c_char_p,
        ctypes.POINTER(ctypes.c_uint32),
        ctypes.c_uint32,
    ]
    dll.UsbPt104Enumerate.restype = ctypes.c_int32

    dll.UsbPt104OpenUnit.argtypes = [
        ctypes.POINTER(ctypes.c_int16),
        ctypes.c_char_p,
    ]
    dll.UsbPt104OpenUnit.restype = ctypes.c_int32

    dll.UsbPt104SetMains.argtypes = [ctypes.c_int16, ctypes.c_uint16]
    dll.UsbPt104SetMains.restype = ctypes.c_int32

    dll.UsbPt104SetChannel.argtypes = [
        ctypes.c_int16,
        ctypes.c_int32,
        ctypes.c_int32,
        ctypes.c_int16,
    ]
    dll.UsbPt104SetChannel.restype = ctypes.c_int32

    dll.UsbPt104GetValue.argtypes = [
        ctypes.c_int16,
        ctypes.c_int32,
        ctypes.POINTER(ctypes.c_int32),
        ctypes.c_int16,
    ]
    dll.UsbPt104GetValue.restype = ctypes.c_int32

    dll.UsbPt104CloseUnit.argtypes = [ctypes.c_int16]
    dll.UsbPt104CloseUnit.restype = ctypes.c_int32

    details = ctypes.create_string_buffer(512)
    length = ctypes.c_uint32(len(details))
    status = dll.UsbPt104Enumerate(details, ctypes.byref(length), CT_USB)
    details_text = decode_buffer(details.raw)

    print(f"Enumerate status: {status}")
    print(f"Enumerate details: {details_text!r}")

    if status != 0 or not details_text:
        return 1

    first_entry = details_text.split(",")[0].strip()
    if ":" in first_entry:
        _, serial = first_entry.split(":", 1)
    else:
        serial = first_entry

    handle = ctypes.c_int16()
    status = dll.UsbPt104OpenUnit(ctypes.byref(handle), serial.encode("ascii"))
    print(f"Open status: {status}")
    print(f"Handle: {handle.value}")
    if status != 0:
        return 2

    try:
        status = dll.UsbPt104SetMains(handle.value, 0)
        print(f"Set mains status (50 Hz): {status}")
        if status != 0:
            return 3

        status = dll.UsbPt104SetChannel(handle.value, USBPT104_CHANNEL_4, USBPT104_PT100, 4)
        print(f"Set channel 4 PT100 4-wire status: {status}")
        if status != 0:
            return 4

        print("Waiting for a fresh measurement...")
        for attempt in range(1, 11):
            time.sleep(0.8)
            value = ctypes.c_int32()
            status = dll.UsbPt104GetValue(handle.value, USBPT104_CHANNEL_4, ctypes.byref(value), 1)
            print(f"Read attempt {attempt}: status={status}, raw={value.value}")
            if status == 0:
                temperature_c = value.value / 1000.0
                print(f"Channel 4 temperature: {temperature_c:.3f} C")
                return 0
        return 5
    finally:
        close_status = dll.UsbPt104CloseUnit(handle.value)
        print(f"Close status: {close_status}")


if __name__ == "__main__":
    raise SystemExit(main())
