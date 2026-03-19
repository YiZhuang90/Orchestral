import cv2 # type: ignore
import numpy as np # type: ignore
import mvsdk # type: ignore
# import ipywidgets as widgets # type: ignore
# from IPython.display import display, clear_output # type: ignore
import win_precise_time as wpt # type: ignore
import matplotlib.pyplot as plt # type: ignore
from matplotlib.backends.backend_agg import FigureCanvasAgg as FigureCanvas # type: ignore
import plotly.graph_objs as go # type: ignore
from plotly.subplots import make_subplots # type: ignore
from bokeh.plotting import figure, show, output_notebook #  type: ignore
from bokeh.models import ColumnDataSource, Div, Legend # type: ignore
from bokeh.io import push_notebook, curdoc # type: ignore
from bokeh.layouts import row, column # type: ignore
import pandas as pd # type: ignore
import ctypes
from picosdk.usbPT104 import usbPt104 as pt104 # type: ignore
from picosdk.functions import assert_pico_ok # type: ignore
import serial.tools.list_ports # type: ignore
import serial # type: ignore
# sys.path.append('../../Functions')
from pufffuncs import * # load DIY functions
from PipeFlowComputer import PipeFlowComputer_FR, PipeFlowComputer_Re  # load pipe flow computer functions
from scipy.signal import butter, lfilter, argrelextrema # type: ignore
from Statistics import CI_w
import warnings
# from datetime import datetime
import re


# Statistics
# total_puff_tracked = 0
# total_decay = 0
# decay_rate = 0
# total_split = 0
# split_rate = 0



def send_global_parameters(switches_wrap, camera_setting_wrap, display_setting_wrap, detection_setting_wrap, temperature_setting_wrap, data_path, experiment_setting_wrap):
    global exit_flag, toy_thread_switch, puff_tracking_switch, puff_detection_switch, data_compute_switch, generate_laminar_field_switch, read_flowrate_switch, arduino_switch, temperature_switch, camera_switch, display_switch
    global exp_in_millisecond_array, exp_time_array, soft_trigger, expected_camera_freq, base_frame_number, wait_frame_number, gain_value_array, ROIs
    global display_freq, plot_window_size_in_time
    global data_frequency, moving_average_filter_size, recompute_background_n, recompute_laminar_threshold_ref, recompute_laminar_threshold_step_ref, time_shift, actuator_delay, trigger_interval, detection_window_ratio, threshold, detection_window_size_frame_number, gap_size_threshold_in_d
    global channel_idxes, temperature_shifts, filtered
    global raw_data_path
    global experiment_idx, RR, distance_in_d, pipe_d, Re_target, Re_frequency, concentration, temperature, exp_mode, log_data, bokeh_display, max_frame, flow_speed_init, distance, delay_final, delay_in_seconds, delay_list

    exit_flag, toy_thread_switch, puff_tracking_switch, puff_detection_switch, data_compute_switch, generate_laminar_field_switch, read_flowrate_switch, arduino_switch, temperature_switch, camera_switch, display_switch = switches_wrap
    exp_in_millisecond_array, exp_time_array, soft_trigger, expected_camera_freq, base_frame_number, wait_frame_number, gain_value_array, ROIs = camera_setting_wrap
    display_freq, plot_window_size_in_time = display_setting_wrap
    data_frequency, moving_average_filter_size, recompute_background_n, recompute_laminar_threshold_ref, recompute_laminar_threshold_step_ref, time_shift, actuator_delay, trigger_interval, detection_window_ratio, threshold, detection_window_size_frame_number, gap_size_threshold_in_d = detection_setting_wrap
    channel_idxes, temperature_shifts, filtered = temperature_setting_wrap
    experiment_idx, RR, distance_in_d, pipe_d, Re_target, Re_frequency, concentration, temperature, exp_mode, log_data, bokeh_display, max_time, flow_speed_init, distance, delay_final, delay_in_seconds, delay_list = experiment_setting_wrap
    max_frame = max_time * expected_camera_freq
    raw_data_path = data_path

def send_exit_flag(flag):
    global exit_flag
    exit_flag = flag

def butter_lowpass(cutoff, fs, order=5):
    nyq = 0.5 * fs  # Nyquist Frequency
    normal_cutoff = cutoff / nyq
    b, a = butter(order, normal_cutoff, btype='low', analog=False)
    return b, a

def butter_lowpass_filter(data, cutoff, fs, order=5):
    b, a = butter_lowpass(cutoff, fs, order=order)
    y = lfilter(b, a, data)
    return y

def m_shape_check(sig):
    global data_frequency, threshold
    cutoff = 5  # Desired cutoff frequency in Hz
    # fs = 100  # Sampling rate in Hz
    order = 1  # Filter order
    filtered_sig = butter_lowpass_filter(sig, cutoff, data_frequency, order)
    filtered_sig_above = np.delete(filtered_sig, np.where(filtered_sig < threshold))
    maxima_indices = argrelextrema(filtered_sig_above, np.greater)[0]
    maxima_values = filtered_sig_above[maxima_indices]
    
    maxima_diff = abs(maxima_values[-1] - maxima_values[0])
    
    if maxima_values.shape[0] == 2 and maxima_diff <= threshold *2 / 3:
        #this following content of checking the gap deepth is added at 4/25/2024, it returns True as long as there are two peaks and the difference is not too big before this modification.
        # start from the 7th data point for verification of the periodic experiment.
        maxima_mean = (maxima_values[-1] + maxima_values[0])/2
        minima_indices = argrelextrema(filtered_sig_above, np.less)[0]
        minima_value = filtered_sig_above[minima_indices]
        if maxima_mean - minima_value > 0.4:
            return True
        else:
            return False
    else:
        return False

def moving_average_filter(data, window_size = 5):
    try:
        data = np.array(data)
    except:
        pass
    if len(data) < window_size:
        return np.mean(data)
    else:
        return np.mean(data[-window_size:])

def initialize_plot(image_resolution, dpi = 100):
    # Desired image resolution (in pixels)
    desired_width_px = image_resolution.iWidthFOV
    desired_height_px = image_resolution.iHeightFOV

    # Calculate figure size in inches
    fig_width = desired_width_px / dpi
    fig_height = desired_height_px / dpi
    # Prepare the figure and the plot
    fig, ax = plt.subplots(figsize=(fig_width, fig_height), dpi=dpi)
    
    ax.set_xlim(0, 5)
    # ax.set_ylim(-1, 1)
    # ax.plot([0,1000000],[threshold,threshold], 'g--')
    ax.axhline(y=threshold, color='g', linestyle='--')
    line1, = ax.plot([], [], 'r-', label='Point 1(250D)')
    line2, = ax.plot([], [], 'b-', label='Point 2(1000D)')
    ax.legend(loc='upper left', fontsize = 7)
    ax.set_xlabel('Time (s)')
    return fig, ax, line1, line2

def grab_an_image(hCamera, pFrameBuffer):
    pRawData, FrameHead = mvsdk.CameraGetImageBuffer(hCamera, 200)

    # pFrameBuffer, FrameHead = mvsdk.CameraGetImageBuffer(hCamera, 2000)
    mvsdk.CameraImageProcess(hCamera, pRawData, pFrameBuffer, FrameHead)
    mvsdk.CameraReleaseImageBuffer(hCamera, pRawData)
    

    # windows下取到的图像数据是上下颠倒的，以BMP格式存放。转换成opencv则需要上下翻转成正的
    # linux下直接输出正的，不需要上下翻转
    # if platform.system() == "Windows":
    mvsdk.CameraFlipFrameBuffer(pFrameBuffer, FrameHead, 1)

        # 此时图片已经存储在pFrameBuffer中，对于彩色相机pFrameBuffer=RGB数据，黑白相机pFrameBuffer=8位灰度数据
    # 把pFrameBuffer转换成opencv的图像格式以进行后续算法处理
    frame_data = (mvsdk.c_ubyte * FrameHead.uBytes).from_address(pFrameBuffer)
    frame = np.frombuffer(frame_data, dtype=np.uint8)
    # frame = frame.reshape((FrameHead.iHeight, FrameHead.iWidth, 1 if FrameHead.uiMediaType == mvsdk.CAMERA_MEDIA_TYPE_MONO8 else 3) )
    frame = frame.reshape((FrameHead.iHeight, FrameHead.iWidth) )
    
    return frame, FrameHead

def shut_down_camera(hCamera, pFrameBuffer):
    # 关闭相机
    mvsdk.CameraUnInit(hCamera)
    # 释放帧缓存
    mvsdk.CameraAlignFree(pFrameBuffer)
    
def initialize_all_cameras():
    #Enumerate camera(s)
    DevList = mvsdk.CameraEnumerateDevice()
    # print(DevList)
    # p1 acSn = b'043151223050', p2 acSn = b'043091920168'
    if DevList[0].acSn == b'043091920168':
        DevList.reverse()

    # print(DevList)
    nDev = len(DevList)
    
    if nDev < 1:
        print("No camera was found!")
        return
    
    camera_handles = []
    
     # initialize all cameras
    for i, Dev_info in enumerate(DevList):
        hCamera = 0
        try:
            hCamera = mvsdk.CameraInit(Dev_info, -1, -1)
            camera_handles.append(hCamera)
            print(f'Camera#{i} initialized')
        except mvsdk.CameraException as e:
            print("CameraInit Failed({}): {}".format(e.error_code, e.message) )
            # 关闭相机
            mvsdk.CameraUnInit(hCamera)
            return

    return camera_handles

def setup_camera(hCamera, camera_parameters, camera_idx):
    exp_time_array, soft_trigger, gain_value_array = camera_parameters
    exp_time = exp_time_array[camera_idx]
    gain_value = gain_value_array[camera_idx]
        # 获取相机特性描述
    cap = mvsdk.CameraGetCapability(hCamera)

    mvsdk.CameraSetAnalogGainX(hCamera, gain_value)
    # print(mvsdk.CameraGetAnalogGainX(hCamera),'CameraGetAnalogGainX')
    # print(mvsdk.CameraGetAnalogGain(hCamera),'CameraGetAnalogGain')
    # 判断是黑白相机还是彩色相机
    # monoCamera = (cap.sIspCapacity.bMonoSensor != 0)
    # print('Mono' if monoCamera else 'RGB')

    # 黑白相机让ISP直接输出MONO数据，而不是扩展成R=G=B的24位灰度
    # if monoCamera:
    #     mvsdk.CameraSetIspOutFormat(hCamera, mvsdk.CAMERA_MEDIA_TYPE_MONO8)
    # else:
    #     # mvsdk.CameraSetIspOutFormat(hCamera, mvsdk.CAMERA_MEDIA_TYPE_BGR8)
    mvsdk.CameraSetIspOutFormat(hCamera, mvsdk.CAMERA_MEDIA_TYPE_MONO8)

    # 相机模式切换成软触发采集
    if soft_trigger:
        mvsdk.CameraSetTriggerMode(hCamera, 1)
        mvsdk.CameraSetTriggerCount(hCamera, 1)
    else:
        mvsdk.CameraSetTriggerMode(hCamera, 0)

    # Get an instance of the resolution setting structure  
    image_resolution = mvsdk.tSdkImageResolution()
    image_resolution = mvsdk.CameraGetImageResolution(hCamera)
    # CameraCustomizeResolution
    # print(image_resolution.iWidthFOV, image_resolution.iHeightFOV)
    
    # 手动曝光
    mvsdk.CameraSetAeState(hCamera, 0)
    mvsdk.CameraSetExposureTime(hCamera, exp_time)
    
    mvsdk.CameraSetFrameSpeed(hCamera, 0)

    # 让SDK内部取图线程开始工作
    mvsdk.CameraPlay(hCamera)

    # 计算RGB buffer所需的大小，这里直接按照相机的最大分辨率来分配
    FrameBufferSize = image_resolution.iHeightFOV* image_resolution.iWidthFOV * 1

    # 分配RGB buffer，用来存放ISP输出的图像
    # 备注：从相机传输到PC端的是RAW数据，在PC端通过软件ISP转为RGB数据（如果是黑白相机就不需要转换格式，但是ISP还有其它处理，所以也需要分配这个buffqer）
    pFrameBuffer = mvsdk.CameraAlignMalloc(FrameBufferSize, 16)

    return pFrameBuffer, image_resolution

def all_elements_same(lst):
    return all(x == lst[0] for x in lst)

# Assume fig and ax are defined outside this function and reused
def plot_to_image_optimized(fig):
    """Converts a given figure to an RGB image (as a numpy array) more efficiently."""
    # ax.clear()  # Clear previous drawings
    # Your plotting code here (e.g., ax.plot(...))
    
    # Draw the canvas and extract the image from it
    fig.canvas.draw()
    image = np.frombuffer(fig.canvas.tostring_rgb(), dtype='uint8')
    image = image.reshape(fig.canvas.get_width_height()[::-1] + (3,))

    return cv2.cvtColor(image, cv2.COLOR_RGB2BGR)  # Return RGB image directly if BGR is not strictly necessary

def data_display_thread(image_resolution, data_queues, ROIs):
    global exit_flag, image_yx, total_puff_tracked, total_decay, total_split, decay_rate, split_rate, Re, temp_list, plot_window_size_in_time, exp_mode, display_freq
    
    camera_number = len(data_queues)
    temp_init = None


    # Create a named window
    cv2.namedWindow('Real-time Plot', cv2.WINDOW_NORMAL)

    # # Attempt to set window to topmost (note: might not work on all OSs)
    try:
        cv2.setWindowProperty('Real-time Plot', cv2.WND_PROP_TOPMOST, 1)
    except:
        pass
    
    fig, ax, line1, line2 = initialize_plot(image_resolution)
    display_frame = np.ones(( image_resolution.iHeightFOV * 3 + 4, image_resolution.iWidthFOV, 3), dtype=np.uint8) * 255
    error_idx = 0

    while not exit_flag:
        all_queue_not_empty = 1
        all_queue_are_empty = 1
        for data_queue in data_queues:
            all_queue_are_empty = all_queue_are_empty * data_queue.empty()
            all_queue_not_empty = all_queue_not_empty * (1-data_queue.empty())
            # print(data_queue.empty())
            
        error_idx += 1

        frame_list = []
        display_time_list = []
        display_signal_list = []
        sync_time_stamp_list = []
        real_time_data_frequency_list = []
        camera_frequency_list = []
        
        if not all_queue_are_empty:
            # Get an image from the input queue
            for camera_idx, data_queue in enumerate(data_queues):
                frame, display_time, display_signal, real_time_data_frequency, sync_time_stamp, camera_frequency = data_queue.get()
                frame_list.append(frame)
                display_time_list.append(np.array(display_time))
                display_signal_list.append(np.array(display_signal))
                sync_time_stamp_list.append(sync_time_stamp)
                real_time_data_frequency_list.append(real_time_data_frequency)
                camera_frequency_list.append(camera_frequency)

            
            try:
                if temp_init == None:
                    temp_init = temp_list[-1][2]
                    print(f'Initial temperature = {temp_init:.3f}')
                temp_start = temp_list[-1][0]
                temp_end = temp_list[-1][1]
                temp = temp_list[-1][2]
            except:
                temp_start = 0
                temp_end = 0
                temp = 0

            line1.set_data(display_time_list[0], display_signal_list[0])
            line2.set_data(display_time_list[1], display_signal_list[1])            
            ax.relim()
            ax.autoscale_view()
            max_value = max([max(display_signal_list[0]), max(display_signal_list[1]), threshold])
            min_value = min([min(display_signal_list[0]), min(display_signal_list[1]), threshold])
            figure_height = max_value - min_value
            live_aspect = plot_window_size_in_time / figure_height * 0.2
            ax.set_aspect(live_aspect)

                        
            if display_time[-1] > plot_window_size_in_time:
                ax.set_xlim(display_time[-1]-plot_window_size_in_time, display_time[-1])

            sig_plot_bgr = plot_to_image_optimized(fig)
            sig_plot_bgr = sig_plot_bgr[40:,:,:]
            
            display_frame[image_resolution.iHeightFOV*2 + 4 : image_resolution.iHeightFOV*2 + 4 + sig_plot_bgr.shape[0], : sig_plot_bgr.shape[1],:] = sig_plot_bgr
            
            display_frame[:image_resolution.iHeightFOV,:,:] = cv2.cvtColor(frame_list[0], cv2.COLOR_RGB2BGR)
            display_frame[image_resolution.iHeightFOV + 2 : image_resolution.iHeightFOV * 2+ 2,:,:] = cv2.cvtColor(frame_list[1], cv2.COLOR_RGB2BGR)
            banner_height = 35


            
            # cv2.putText(display_frame, f"Trigger_index: {trigger_index}", (10, 50), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (255, 255, 255), 2)                    
            
            cv2.putText(display_frame, f"P1 data: {real_time_data_frequency_list[0]:.2f}Hz", (330, 15), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 1)
            cv2.putText(display_frame, f"P1 cam: {camera_frequency_list[0]:.2f}Hz", (330, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 1)
            cv2.putText(display_frame, f"P2 data: {real_time_data_frequency_list[1]:.2f}Hz", (330, 190), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 1)
            cv2.putText(display_frame, f"P2 cam: {camera_frequency_list[1]:.2f}Hz", (330, 205), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 1)
            # temperature
            # cv2.putText(display_frame, f"T_start: {temperature_start:.3f}", (10, 15), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 1)
            # cv2.putText(display_frame, f"T_end: {temperature_end:.3f}", (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 1)

            if exp_mode:
                cv2.putText(display_frame, f"Data acquiring", (5, 475), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 0, 200), 1)
                cv2.rectangle(display_frame, (0,display_frame.shape[0]-banner_height), (display_frame.shape[1],display_frame.shape[0]), (0, 0, 200), -1)
            else:
                cv2.putText(display_frame, f"Test & adjustment", (5, 475), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (152, 106, 142), 1)
                cv2.rectangle(display_frame, (0,display_frame.shape[0]-banner_height), (display_frame.shape[1],display_frame.shape[0]), (190, 132, 178), -1)
            cv2.putText(display_frame, f"T_mean: {(temp_start+temp_end)/2:.3f}", (10, 15), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 1)
            cv2.putText(display_frame, f"deltaT: {temp_start-temp_end:.3f}", (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 1)
            
            # print(temp, temp_init)
            try:
                T_shift = temp - temp_init
            except:
                T_shift = 0
            if abs(T_shift) > 0.1:
                cv2.putText(display_frame, f"T_shift: {T_shift:.3f}", (10, 45), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 0, 255), 1)
            else:
                cv2.putText(display_frame, f"T_shift: {T_shift:.3f}", (10, 45), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 1)
            
            
            try:
                cv2.putText(display_frame, f"Re{Re:.1f}| N = {total_puff_tracked}| DN = {total_decay} ({decay_rate*100:.1f}%)| SN = {total_split} ({split_rate*100:.1f}%)", (5, display_frame.shape[0] - 15), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255, 255, 255), 1)
            except:
                pass
            for i, ROI in enumerate(ROIs):
                top_left = (ROI[2], ROI[0] + (image_resolution.iHeightFOV+2)*i  )  # (x, y) coordinates
                bottom_right = (ROI[2] + ROI[3], ROI[0] + ROI[1] + (image_resolution.iHeightFOV+2)*i)  # (x, y) coordinates
            
                # Define the color and thickness
                color = (255, 255, 255)  # Blue in BGR format
                thickness = 1  # Pixel
                cv2.rectangle(display_frame, top_left, bottom_right, color, thickness)
                cv2.putText(display_frame, f"ROI P{i+1}", (ROI[2]+5, ROI[0] + (image_resolution.iHeightFOV+2)*i + 15), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 1)
            # cv2.putText(display_frame, f"Current type: {puff_type}", (10, 75), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (255, 255, 255), 2)
            cv2.imshow("Real-time Plot", display_frame)
            # print(f'Time = {(t1 - t0)*1000:.2f}s')
            if cv2.waitKey(1) & 0xFF == ord('q'):
                exit_flag = True
                break
        else:
            wpt.sleep(1/display_freq)
    
    cv2.destroyAllWindows()
    # exit_flag = True

def find_puff_in_real_time(signal, start_idx, time_array, threshold):
    puff_list = []
    puff_type = None
    error = None
    # Identify points above the threshold
    above_threshold = signal > threshold
    time_threshold = 5 * pipe_d / flow_speed_init
    
    
    # Find the start and end indices of the peaks
    edges = np.diff(above_threshold.astype(int))
    start_indices = np.where(edges == 1)[0] + 1  # +1 to correct the index after diff
    end_indices = np.where(edges == -1)[0]

    if len(start_indices) == len(end_indices):
        puff_n = len(start_indices)
    
        if puff_n == 0:
            puff_type = 'Empty'
        else:
            for i in range(puff_n):
                if time_array[end_indices[puff_n-1-i]] - time_array[start_indices[puff_n-1-i]] < time_threshold:
                    # print(puff_n, puff_n-1-i)
                    start_indices = np.delete(start_indices, puff_n-1-i)
                    end_indices = np.delete(end_indices, puff_n-1-i)
                
            puff_n = len(start_indices)
            
            if puff_n == 0:
                puff_type = 'Empty'
            elif puff_n == 1:
                puff_type = 'SinglePuff'
            else:
                puff_type = 'SplitPuff'
    
        for i in range(puff_n):
            puff_list.append([start_indices[i] + start_idx, end_indices[i] + start_idx])
    elif len(end_indices) > len(start_indices):
        error = 'down'
        # print(f'Down edge in the range ({time_array[0]:.1f} to {time_array[-1]:.1f})', end=' ')
    else:
        error = 'up'
        # print(f'Up edge in the range ({time_array[0]:.1f} to {time_array[-1]:.1f})', end=' ')

    return puff_list, puff_type, error

def laminar_flow_compute(laminar_flow_frame_queue, laminar_flow_result_queue):
    global exit_flag
    
    while not exit_flag:
        if not laminar_flow_frame_queue.empty():
            # print('P1 laminar computing')
            base_frame_buffer = laminar_flow_frame_queue.get()
            # print(base_frame_buffer.shape)
            base_frame_buffer_mean = np.float32(np.mean(base_frame_buffer,axis = 0))
            normalization_buffer = base_frame_buffer - base_frame_buffer_mean
            std_normalization_buffer = np.std(normalization_buffer, axis=(1,2))
            laminar_flow_result_queue.put([base_frame_buffer_mean, std_normalization_buffer])
            # print('P1 laminar result sent')
        else:
            pass
        wpt.sleep(1/display_freq)
    


def find_big_gaps(list, gap_size_threshold):
    diff_arr = np.array(list[1:]) - np.array(list[:-1])
    gap_size = (np.array(list[1:]) - np.array(list[:-1])).max()
    max_index = np.where(diff_arr == diff_arr.max())[0][0]
    
    if gap_size < gap_size_threshold:
        return False, max_index
    else:
        return True, max_index
        
def if_gap_important(sig, time_array, max_index, threshold):
    global pipe_d, flow_speed_init
    criteria1 = True if max([sig[max_index],sig[max_index+1]]) > threshold/2 else False
    criteria2 = True if abs(sig[max_index] - sig[max_index+1]) < 3 * threshold else False
    criteria3 = True if min([sig[max_index],sig[max_index+1]]) < 4 * threshold else False
    criteria4 = True if time_array[max_index+1] - time_array[max_index] > 15 * pipe_d / flow_speed_init else False

    if criteria1 and criteria2 and criteria3 or criteria1 and criteria4:
        return True
    else:
        return False


def initialize_pico_channel_multisensor(channel_idxes):
    # Create chandle and status ready for use
    status = {}
    chandle = ctypes.c_int16()
    
    # Open the device
    status["openUnit"] = pt104.UsbPt104OpenUnit(ctypes.byref(chandle),0)
    assert_pico_ok(status["openUnit"])
    
    # Set mains noise filtering
    sixty_hertz = 0 #50 Hz
    status["setMains"] = pt104.UsbPt104SetMains(chandle, sixty_hertz)
    assert_pico_ok(status["setMains"])
    
    # Setup channel 1
    # channel_idx = 1
    datatype = pt104.PT104_DATA_TYPE["USBPT104_PT100"] #pt100
    noOfWires = 4 #wires
    channels = []
    for channel_idx in channel_idxes:
        channel = pt104.PT104_CHANNELS[f"USBPT104_CHANNEL_{channel_idx}"] 
        channels.append(channel)
        status["setChannel1"] = pt104.UsbPt104SetChannel(chandle, channel, datatype, noOfWires)
        assert_pico_ok(status["setChannel1"])
        print(f'PT-104 Channel #{channel_idx} initialized')

    return chandle, channels, status

def get_current_temp(chandle, channel, temperature_offset):
    global filtered
    measurement = ctypes.c_int32()
    pt104.UsbPt104GetValue(chandle, channel, ctypes.byref(measurement), filtered)
    current_temp = measurement.value/1000 + temperature_offset
    return current_temp
    
def acquire_temperature_multisensor(chandle, channels, status, temperature_shifts):
    global exit_flag, temperature_start, temperature_end, temp_list, Re_frequency
    #collect data
    print("Temperature testing starts")
    
    # temp_list = [[0, 0, 0, 0]]
    temp_list = []
    interval = 1/Re_frequency
    
    #pause
    initial_time = wpt.time()
    wpt.sleep(2)
    i=0
    while not exit_flag:
    
        start = wpt.time()        
        
        current_temp_list = []
        for channel, temperature_shift in zip(channels, temperature_shifts):
            current_temp = get_current_temp(chandle, channel, temperature_shift)
            current_temp_list.append(current_temp)
        current_temp_list.append(sum(current_temp_list)/len(current_temp_list))
        current_temp_list.append(current_temp_list[0] - current_temp_list[1])
        current_temp_list.append(wpt.time() - initial_time)
        temperature_start = current_temp_list[0]
        temperature_end = current_temp_list[1]
        
        if i != 0:
            temp_list.append(current_temp_list)
        # _, Re, _ = PipeFlowComputer_FR(pipe_d, current_temp, flowrate, leng = 1)
        # Re_record.append(Re)
        i+=1
        end = wpt.time()  
        sleep_time = interval - (end-start)
        # print(sleep_time)
        wpt.sleep(sleep_time)       

def terminate_pico_logger(chandle, status):
    status["closeUnit"] = pt104.UsbPt104CloseUnit(chandle)
    assert_pico_ok(status["closeUnit"])
    # print('PT-104 box closed')

def find_arduino_com_port():
    ports = serial.tools.list_ports.comports()
    for port in ports:
        if "Arduino" in port.description or "USB Serial Device" in port.description:
            return port.device
    return None

def initialize_arduino():
    arduino_port = find_arduino_com_port()
    # Setup serial connection to Arduino
    ser = serial.Serial(f'{arduino_port}', 9600)
    print('Arduino for flowrate initialized')
    wpt.sleep(5) # Wait for the connection to establish
    return ser

def initialize_actuator_set_interval(ser, trigger_delay):
    # wpt.sleep(actuator_delay)
    actuator_switch = 1
    command = f"{actuator_switch},{trigger_delay}\n"
    ser.write(command.encode())

def stop_actuator(ser, trigger_interval = 1):
    actuator_switch = 0
    command = f"{actuator_switch},{trigger_interval}\n"
    ser.write(command.encode())

def read_pulse_count(ser):
    while True:
        if ser.in_waiting:
            number_str = ser.readline().decode('utf-8').strip()
            # print(number_str)
            # Split the string at the comma
            numbers = number_str.split(',')
            # Convert the list of strings to a list of integers
            [time_stamp, pulse_count] = [int(number) for number in numbers]
            time_stamp = time_stamp / 1000
            break
    return time_stamp, pulse_count
    
def acquire_flowrate_control_center_version(ser):
    global exit_flag, temperature, temperature_start, temperature_end, Re,Re_record, pipe_status_logger, volume_flowrate_record_ori, average_count, actuator_delay, exp_index
    
    start_time, _ = read_pulse_count(ser)

    previous_time = start_time
    
    volume_flowrate_record_ori=[]
    Re_record = []
    pipe_status_logger = []
    average_count = 100 if exp_mode else 50

    while not exit_flag:
        current_time, pulse_count = read_pulse_count(ser)
        
        volume_flowrate = pulse_count/80*60/1000/(current_time - previous_time) #l/min
        volume_flowrate_record_ori.append(volume_flowrate)
        volume_flowrate_filtered = sum(volume_flowrate_record_ori[-1*average_count:]) / len(volume_flowrate_record_ori[-1*average_count:])
        
        try:
            current_temp = (temperature_start + temperature_end)/2
        except:
            current_temp = temperature
        bulk_velocity, Re, _ = PipeFlowComputer_FR(pipe_d, current_temp, volume_flowrate_filtered, leng = 1)
        # print(Re, volume_flowrate_filtered, current_time - start_time)
        Re_record.append([Re, volume_flowrate_filtered, current_time - start_time, volume_flowrate])
        pipe_status_logger.append([current_time - start_time, Re, current_temp, volume_flowrate_filtered, bulk_velocity])
        previous_time = current_time
    Re_record_array = np.array(Re_record)
    np.savetxt(f"#{exp_index}_Re_record.cvs", Re_record_array, delimiter = ",")
    
def acquire_flowrate(ser, trigger_interval):
    global exit_flag, temperature, temperature_start, temperature_end, Re,Re_record, flowrate_time_stamps, flowrate_records, data_log_at_end, flowrate_live_record, average_count, actuator_delay
    
    initial_time = wpt.time()
    # wpt.sleep(2) # Wait for the connection to establish
    flowrate_live_record=[]
    Re_record = []
    
    average_interval = 100 if exp_mode else 50
    average_count = int(average_interval//trigger_interval)
    
    i=0
    while not exit_flag:
        if ser.in_waiting:
            
            pulseCount = ser.readline().decode('utf-8').strip()
            # Assuming each pulse represents a fixed volume, e.g., 1 liter
            if i <= actuator_delay + 4:
                flowRate = int(pulseCount)/80*60/1000
            else:
                flowRate = int(pulseCount)/80*60/1000/trigger_interval   # Pulses per second = liters per second

            if i == 0 or abs(flowRate - sum(flowrate_live_record)/len(flowrate_live_record)) < 0.2 * sum(flowrate_live_record)/len(flowrate_live_record):
                flowrate_live_record.append(flowRate)
                try:
                    plot_flowrate_list = flowrate_live_record[-average_count:]
                except:
                    plot_flowrate_list = flowrate_live_record
                plot_flowrate = sum(plot_flowrate_list)/len(plot_flowrate_list)
                # print(f"Flow rate: {plot_flowrate:.5f} L/min, N = {len(plot_flowrate_list)}")
                time_stamp = wpt.time() - initial_time
                # flowrate_time_stamps.append()
                # flowrate_records.append(plot_flowrate)
                try:
                    current_temp = (temperature_start + temperature_end)/2
                except:
                    current_temp = temperature
                _, Re, _ = PipeFlowComputer_FR(pipe_d, current_temp, plot_flowrate, leng = 1)
                if i != 0:
                    Re_record.append([Re, plot_flowrate, time_stamp])
                i += 1
            else:
                i += 1


    
def process_numeric_in_pandas(x, A, B):
    if isinstance(x, (int, float)):  # Check if x is numeric
        return (x + A) * B
    else:
        return x  # Return unchanged if not numeric
        



def data_log_at_end(raw_data_path):
    global log_data, trigger_interval, Re_target, exp_in_millisecond_array, concentration, gain_value_array, RR, distance, delay_in_seconds, base_frame_number, exit_flag, puff_log, average_count, delay_final, compute_time_list_p1, std_sig_record_p1, compute_time_list_p2, std_sig_record_p2, temp_list, Re_record, std_sig_record_ori_p1, std_sig_record_ori_p2,  total_puff_tracked, total_decay, total_split, experiment_idx
    global imaging_time_stampes_p1, imaging_time_stampes_p2

    plotly_x = 'Position' if exp_mode else 'Time'
    if not exp_mode:
        experiment_idx = f'test{experiment_idx}'

    cutoff_index = int((base_frame_number + 10))

    if total_puff_tracked < average_count + 10:
        average_count = -10
    temp_array = np.array(temp_list)
    Re_record = np.array(Re_record)

    temp_mean = temp_array[average_count+10:,2].mean()
    Re_mean = Re_record[average_count+10:,0].mean()
    flow_speed_mean, _ = PipeFlowComputer_Re(pipe_d, temp_mean, Re_mean, 1)

    delay_final = distance / flow_speed_mean
    delay_diff = delay_final - delay_in_seconds

    puff_count = puff_log.shape[0]
    # Create a figure with make_subplots for more customization
    fig = make_subplots(specs=[[{"secondary_y": True}]])
    if plotly_x == 'Position':
        # Add the first signal to the plot
        fig.add_trace(
            go.Scatter(x=np.array(compute_time_list_p1[cutoff_index:])*flow_speed_mean/pipe_d, y=np.array(std_sig_record_p1[cutoff_index:]), name="P1 STD", line=dict(color='red')),
            secondary_y=False,
        )

        # Add the second signal to the plot
        fig.add_trace(
            go.Scatter(x=(np.array(compute_time_list_p2[cutoff_index:])-delay_diff)*flow_speed_mean/pipe_d, y=np.array(std_sig_record_p2[cutoff_index:]), name="P2 STD", line=dict(color='blue')),
            secondary_y=False,
        )

        fig.add_trace(
            go.Scatter(x=[(np.array(compute_time_list_p1[cutoff_index:])*flow_speed_mean/pipe_d)[0],
                          (np.array(compute_time_list_p1[cutoff_index:])*flow_speed_mean/pipe_d)[-1]], 
                       y=[threshold, threshold], name=f"Threshold = {threshold}", line=dict(color='green',dash='dash')),
            secondary_y=False,
        )

        fig.update_layout(
            template='simple_white',
            title_text=f'X vs STD[#{experiment_idx}_RR{RR}_Re{Re_mean:.1f}_{puff_count}puffs]',
            xaxis_title="Position(d)",
            # yaxis_title="Signal 1 Value",
            xaxis=dict(
                rangeselector=dict(

                ),
                rangeslider=dict(
                    visible=True
                ),
                type="linear"
            ),

            # Set the size of the figure here
            width=1100,  # Width of the figure in pixels
            height=500,  # Height of the figure in pixels
        )
    elif plotly_x == 'Time':
        fig.add_trace(
            go.Scatter(x=np.array(compute_time_list_p1[cutoff_index:]), y=np.array(std_sig_record_p1[cutoff_index:]), name="P1 STD", line=dict(color='red')),
            secondary_y=False,
        )

        # Add the second signal to the plot
        fig.add_trace(
            go.Scatter(x=(np.array(compute_time_list_p2[cutoff_index:])-delay_diff), y=np.array(std_sig_record_p2[cutoff_index:]), name="P2 STD", line=dict(color='blue')),
            secondary_y=False,
        )

        fig.add_trace(
            go.Scatter(x=[np.array(compute_time_list_p1[cutoff_index:])[0],
                          np.array(compute_time_list_p1[cutoff_index:])[-1]], 
                       y=[threshold, threshold], name=f"Threshold = {threshold}", line=dict(color='green',dash='dash')),
            secondary_y=False,
        )

        fig.update_layout(
            template='simple_white',
            title_text=f'Time vs STD [#{experiment_idx}_RR{RR}_Re{Re_mean:.1f}_{puff_count}puffs]',
            xaxis_title="Time(s)",
            # yaxis_title="Signal 1 Value",
            xaxis=dict(
                rangeselector=dict(

                ),
                rangeslider=dict(
                    visible=True
                ),
                type="linear"
            ),
            # Set the size of the figure here
            width=1100,  # Width of the figure in pixels
            height=500,  # Height of the figure in pixels
        )

    # Show the figure
    fig.show()
    fig.write_html(f"{raw_data_path}/#{experiment_idx}_RR{RR}_Re{Re_mean:.1f}_{exp_in_millisecond_array}ms_conc{concentration}_gain{gain_value_array}_{puff_count}puffs.html")

    print(f'{experiment_idx}\t{Re_mean}\t{Re_target}\t1\t{RR}\t{pipe_d:.5f}\t{temp_mean}\t{flow_speed_mean}\t0\t1\t{trigger_interval}\t{total_puff_tracked}\t{total_decay}\t{total_split}')
    print(f'Temperature mean = {temp_array[:,2].mean():.3f}')
    print(f'Temperature max shift = {temp_array[:,2].max() - temp_array[:,2].min():.3f}')
    print(f'Re mean = {Re_record[:,0].mean():.2f}')
    print(f'Re max shift = {Re_record[:,0].max() - Re_record[:,0].min():.2f}')
    print(total_puff_tracked, total_decay, total_split)

    fig,ax = plt.subplots(3,1,figsize = (6,5))
    ax[0].plot(temp_array[average_count+10:,-1], temp_array[average_count+10:,2])
    ax[0].set_xlabel('Time(s)')
    ax[0].set_ylabel('Temperature')
    ax[0].axhline(y = temp_mean, color='red', linestyle='--')

    ax[1].plot(Re_record[average_count+10:,-1], Re_record[average_count+10:,1])
    ax[1].set_xlabel('Time(s)')
    ax[1].set_ylabel('Flowrate(l/min)')
    # ax[1].axhline(y = Re_mean, color='red', linestyle='--')

    ax[2].plot(Re_record[average_count+10:,-1], Re_record[average_count+10:,0])
    ax[2].set_xlabel('Time(s)')
    ax[2].set_ylabel('Re')
    ax[2].axhline(y = Re_mean, color='red', linestyle='--')

    plt.tight_layout()
    plt.show()

    data_p1 = np.vstack((np.array(compute_time_list_p1[cutoff_index:]), np.array(std_sig_record_p1[cutoff_index:]), np.array(std_sig_record_ori_p1[cutoff_index:])))
    data_p1 = data_p1.T

    data_p2 = np.vstack((np.array(compute_time_list_p2[cutoff_index:])-delay_diff, np.array(std_sig_record_p2[cutoff_index:]), np.array(std_sig_record_ori_p2[cutoff_index:])))
    data_p2 = data_p2.T

    A = 0
    if plotly_x == 'Position':
        B = flow_speed_mean/pipe_d
    else:
        B = 1
    puff_log.Point1H = puff_log.Point1H.apply(process_numeric_in_pandas, args=(A, B))
    puff_log.Point1T = puff_log.Point1T.apply(process_numeric_in_pandas, args=(A, B))
    puff_log.Point1C = puff_log.Point1C.apply(process_numeric_in_pandas, args=(A, B))
    puff_log.Point1PuffWidth = puff_log.Point1PuffWidth.apply(process_numeric_in_pandas, args=(A, B))

    A = - delay_final
    if plotly_x == 'Position':
        B = flow_speed_mean/pipe_d
    else:
        B = 1
    puff_log.Point2H = puff_log.Point2H.apply(process_numeric_in_pandas, args=(A, B))
    puff_log.Point2T = puff_log.Point2T.apply(process_numeric_in_pandas, args=(A, B))
    puff_log.Point2C = puff_log.Point2C.apply(process_numeric_in_pandas, args=(A, B))
    puff_log.Point2PuffWidth = puff_log.Point2PuffWidth.apply(process_numeric_in_pandas, args=(0, B))

    if log_data:
        np.savetxt(f'{raw_data_path}/#{experiment_idx}_RR{RR}_Re{Re_mean:.1f}_{exp_in_millisecond_array}ms_conc{concentration}_gain{gain_value_array}_Temp_record.txt', temp_array, delimiter=',')
        np.savetxt(f'{raw_data_path}/#{experiment_idx}_RR{RR}_Re{Re_mean:.1f}_{exp_in_millisecond_array}ms_conc{concentration}_gain{gain_value_array}_Re_record.txt', Re_record, delimiter=',')
        np.savetxt(f'{raw_data_path}/#{experiment_idx}_RR{RR}_Re{Re_mean:.1f}_{exp_in_millisecond_array}ms_conc{concentration}_gain{gain_value_array}_data_p1.txt', data_p1, delimiter=',')
        np.savetxt(f'{raw_data_path}/#{experiment_idx}_RR{RR}_Re{Re_mean:.1f}_{exp_in_millisecond_array}ms_conc{concentration}_gain{gain_value_array}_data_p2.txt', data_p2, delimiter=',')
        puff_log.to_csv(f'{raw_data_path}/#{experiment_idx}_RR{RR}_Re{Re_mean:.1f}_{puff_count}puffs.csv', index=False)
        print('Files saved.')

    return imaging_time_stampes_p1, imaging_time_stampes_p2

def data_display_bokeh(data_queues, exit_event):
    global exit_flag, image_yx, total_puff_tracked, total_decay, total_split, decay_rate, split_rate, Re, temp_list, exp_mode

    output_notebook()
    temp_init = None
    # print(data_queues[0],data_queues[1])
    # Initial data
    t_p1 = np.linspace(0, plot_window_size_in_time, plot_window_size_in_time * data_frequency)
    sig_p1 = np.zeros(plot_window_size_in_time * data_frequency)
    t_p2 = np.linspace(0, plot_window_size_in_time, plot_window_size_in_time * data_frequency)
    sig_p2 = np.zeros(plot_window_size_in_time * data_frequency)
    threshold_data = np.full_like(t_p1, threshold)

    # Create a ColumnDataSource with additional data
    source = ColumnDataSource(data={'t_p1': t_p1, 'sig_p1': sig_p1, 't_p2': t_p2, 'sig_p2': sig_p2, "threshold_data": threshold_data})

    # Create the figure without the toolbar
    figure_title = "Data acquiring (recording)" if exp_mode else "Test mode (no data recording)"
    title_color = 'red' if exp_mode else 'green'
    
    p = figure(title = figure_title, height=300, width=900, toolbar_location=None, x_range=(0, plot_window_size_in_time), x_axis_label='Time (s)', y_axis_label='Signal Amplitude')
    p.title.text_font_size = '16pt'
    p.title.text_color = title_color

    # Add lines to the plot
    P1_line = p.line('t_p1', 'sig_p1', source=source, line_width=2, color="Magenta", legend_label="P1")
    P2_line = p.line('t_p2', 'sig_p2', source=source, line_width=2, color="Blue", legend_label="P2")
    threshold_line = p.line('t_p2', 'threshold_data', source=source, line_width=2, color="LimeGreen", legend_label="Threshold")

    # Hide the original automatically generated legend
    p.legend.visible = False

    # Move the legend outside the plot area
    new_legend = Legend(items=[
        ("P1", [P1_line]),
        ("P2", [P2_line]),
        ("Threshold", [threshold_line])
    ], location="center", orientation = 'horizontal')
    p.add_layout(new_legend, 'below')

    # Create Divs for current time and elapsed time with inline CSS for styling and outlining
    cam_div = Div(text="""
        <div style='border: 3px solid gray; font-size: 14pt; color: blue; padding: 20px; background: white;'>
            Cameras<br>
            Cam1:<br>
            Data1:<br>
            Cam2:<br>
            Data2:
        </div>
    """, width=220, height=330)

    temp_div = Div(text="""
        <div style='border: 3px solid gray; font-size: 14pt; color: blue; padding: 20px; background: white;'>
            Temperatures<br>
            Current Time:
        </div>
    """, width=200, height=330)

    Re_and_stat_div = Div(text="""
        <div style='border: 3px solid orange; font-size: 14pt; color: green; padding: 20px; background: white;'>
            Re = <br>
            Total puff number = <br>
            Decay puff number = Decay rate = <br>
            Split puff number = Split rate =
        </div>
    """, width=400, height=330)

    cam_div.text = """
        <div style='font-size: 14pt; color: gray; padding: 20px;'>
            <p style='font-size: 16pt; color: DarkGreen; font-weight: bold;'>Cameras<br></p>
            Cam P1:  0 Hz <br>
            Data P1:  0 Hz <br>
            Cam P2:  0 Hz <br>
            Data P2:  0 Hz
        </div>
    """

    temp_div.text = """
        <div style='font-size: 14pt; color: gray; padding: 20px;'>
            <p style='font-size: 16pt; color: DarkGreen; font-weight: bold;'>Temperatures<br></p>
                T_mean:      0<br>
                &Delta;T:      0<br>
                T_shift:      0
        </div>
    """

    #<div style='border: 3px solid gray; padding: 20px;'>
    Re_and_stat_div.text = f"""
        <div style='padding: 20px;'>
            <p style='font-size: 16pt; color: DarkGreen; font-weight: bold;'>Statistics</p>
            <p style='font-size: 14pt; color: gray; font-weight: bold;'>Re = {Re:.2f}</p>
            <p style='font-size: 14pt; color: gray; font-weight: bold;'>Total puff number = 0</p>
            <p style='font-size: 14pt; color: DarkTurquoise;'>
                Decay puff number = 0<br>
                Decay rate = <span style='font-weight: bold;'>0%</span> (0%~0%)
            </p>
            <p style='font-size: 14pt; color: Crimson;'>
                Split puff number = 0<br>
                Split rate = <span style='font-weight: bold;'>0%</span> (0%~0%)
            </p>
        </div>
    """

    # # Layout the plot above and time information Divs side by side below
    layout = column(p, row(cam_div, temp_div, Re_and_stat_div))

    # Display the layout
    handle = show(layout, notebook_handle=True)

    # while not exit_flag:
    try:
        while not exit_event.is_set() and not exit_flag:

            all_queue_not_empty = 1
            all_queue_are_empty = 1
            for data_queue in data_queues:
                all_queue_are_empty = all_queue_are_empty * data_queue.empty()
                all_queue_not_empty = all_queue_not_empty * (1-data_queue.empty())
                # print(data_queue.empty())

            frame_list = []
            display_time_list = []
            display_signal_list = []
            sync_time_stamp_list = []
            real_time_data_frequency_list = []
            camera_frequency_list = []

            if not all_queue_are_empty:
                t1 = wpt.time()
                TN = total_puff_tracked
                DN = total_decay
                SN = total_split

                try:
                    DR = DN/TN*100
                    SR = SN/TN*100
                    DR_CI = CI_w(DN,TN)
                    SR_CI = CI_w(SN,TN)

                except:
                    DR = 0
                    SR = 0
                    DR_CI = [0,0]
                    SR_CI = [0,0]

                # Get an display information from the input queue
                for data_queue in data_queues:
                    _, display_time, display_signal, real_time_data_frequency, sync_time_stamp, camera_frequency = data_queue.get()
                    # frame_list.append(frame)
                    # print(frame.shape)
                    display_time_list.append(np.array(display_time))
                    display_signal_list.append(np.array(display_signal))
                    # print(len(display_signal))
                    sync_time_stamp_list.append(sync_time_stamp)
                    real_time_data_frequency_list.append(real_time_data_frequency)
                    camera_frequency_list.append(camera_frequency)

                if len(temp_list) != 0:
                    temp_start = temp_list[-1][0]
                    temp_end = temp_list[-1][1]
                    temp = temp_list[-1][2]
                    if temp_init == None:
                        temp_init = temp_list[-1][2]
                        print(f'Initial temperature = {temp_init:.3f}')
                    T_shift = temp - temp_init

                else:
                    temp_start = 0
                    temp_end = 0
                    T_shift = 0

                # Update the x-axis range to match the new x values
                p.x_range.start = display_time_list[0][0]
                p.x_range.end = display_time_list[0][-1] if display_time_list[0][-1] > plot_window_size_in_time else plot_window_size_in_time
                # print(p.x_range.start, p.x_range.end)

                # Update ColumnDataSource
                threshold_data = np.full_like(np.array(display_time_list[0]), threshold)

                if len(display_time_list[0]) == len(display_time_list[1]):
                    source.data = {'t_p1': np.array(display_time_list[0]), 'sig_p1': np.array(display_signal_list[0]), 
                                't_p2': np.array(display_time_list[1]), 'sig_p2': np.array(display_signal_list[1]), 
                                "threshold_data": threshold_data}
                else:
                    print("P1 P2 signal length different")
                    pass

                cam_div.text = f"""
                    <div style='font-size: 14pt; color: gray; padding: 20px; background: white;'>
                        <p style='font-size: 16pt; color: DarkGreen; font-weight: bold;'>Cameras<br></p>
                        Cam P1:  {camera_frequency_list[0]:.2f}Hz <br>
                        Data P1:  {real_time_data_frequency_list[0]:.2f}Hz <br>
                        Cam P2:  {camera_frequency_list[1]:.2f}Hz <br>
                        Data P2:  {real_time_data_frequency_list[1]:.2f}Hz
                    </div>
                """

                temp_div.text = f"""
                    <div style='font-size: 14pt; color: gray; padding: 20px; background: white;'>
                        <p style='font-size: 16pt; color: DarkGreen; font-weight: bold;'>Temperatures<br></p>
                            T_mean:      {(temp_start+temp_end)/2:.3f}<br>
                            &Delta;T:      {temp_start-temp_end:.3f}<br>
                            T_shift:      {T_shift:.3f}
                    </div>
                """

                #<div style='border: 3px solid gray; padding: 20px;'>
                Re_and_stat_div.text = f"""
                    <div style='padding: 20px; background: white;'>

                        <p style='font-size: 16pt; color: DarkGreen; font-weight: bold;'>Statistics</p>
                        <p style='font-size: 14pt; color: gray; font-weight: bold;'>Re = {Re:.2f}</p>
                        <p style='font-size: 14pt; color: gray; font-weight: bold;'>Total puff number = {TN}</p>
                        <p style='font-size: 14pt; color: DarkTurquoise;'>
                            Decay puff number = {DN:.0f}<br>
                            Decay rate = <span style='font-weight: bold;'>{DR:.1f}%</span> ({DR_CI[0]*100:.1f}%~{DR_CI[1]*100:.1f}%)
                        </p>
                        <p style='font-size: 14pt; color: Crimson;'>
                            Split puff number = {SN:.0f}<br>
                            Split rate = <span style='font-weight: bold;'>{SR:.1f}%</span> ({SR_CI[0]*100:.1f}%~{SR_CI[1]*100:.1f}%)
                        </p>
                    </div>
                """

                push_notebook(handle=handle)
                t2 = wpt.time()

                sleep_time = 1 / display_freq - (t2 - t1)
                sleep_time = sleep_time if sleep_time > 0 else 0
                wpt.sleep(sleep_time)
            else:
                # print("empty queues")
                wpt.sleep(1 / display_freq)
    except Exception as e:
        print(f"Exception occurred: {e}")
    finally:
        print("stop display")
    # else:
    #     print("stop display")
        

def point_type_check(v_previous, threshold, signal_value):
    # print(v_previous, signal_value)
    if v_previous == None:
        point_type = None

    else:
        v_current = signal_value
        if v_previous < threshold and v_current > threshold:
            point_type = 'start point'
        elif v_previous > threshold and v_current < threshold:
            point_type = 'end point'
        else:
            point_type = None

    v_previous = signal_value

    return v_previous, point_type

size_limit_1 = 45
size_limit_2 = 35
def puff_tracker(result_queue_p1, result_queue_p2):
    global exit_flag, total_puff_tracked, total_decay, total_split, decay_rate, split_rate, puff_log, compute_time_list_p1, compute_time_list_p2, gap_size_threshold_in_d

    #Initialize the puff tracker
    total_puff_tracked = 0
    total_decay = 0
    total_split = 0
    # Puff records
    puff_log = ['Re','RadiusRatio','PuffIndex','Point1','Point1PuffNum','Point1H','Point1T','Point1C','Point2','Point2PuffNum','Point2H','Point2T','Point2C','BugShow','Point1PuffWidth','Point2PuffWidth']
    puff_log = pd.DataFrame(columns=puff_log)

    print('Puff tracking start')

    #Run the while loop to
    while not exit_flag:
        #Wait for data to entering the queues.
        wpt.sleep(trigger_interval)

        #When both data points have got data in its result queue.
        if not result_queue_p1.empty() and not result_queue_p2.empty():
            #get data out from the queues for detection points
            puff_n_p1, puff_type_p1, puff_info_p1, detection_time_array_p1, detection_sig_array_p1, error_p1  = result_queue_p1.get()
            puff_n_p2, puff_type_p2, puff_info_p2, detection_time_array_p2, detection_sig_array_p2, error_p2 = result_queue_p2.get()
            #print(f'P1 time range ({detection_time_array_p1[0]:.1f},{detection_time_array_p1[-1]:.1f}), P2 time range ({detection_time_array_p2[0]:.1f},{detection_time_array_p2[-1]:.1f})')

            #check if any big gaps been detected
            gap_size_threshold_in_seconds = gap_size_threshold_in_d * pipe_d / flow_speed_init
            big_gaps_in_p1, max_index_p1 = find_big_gaps(detection_time_array_p1, gap_size_threshold_in_seconds)
            big_gaps_in_p2, max_index_p2 = find_big_gaps(detection_time_array_p2, gap_size_threshold_in_seconds)

            #Compute if the big gap(s) are important
            if big_gaps_in_p1 or big_gaps_in_p2:
                max_gap_important_p1 = if_gap_important(detection_sig_array_p1, detection_time_array_p1, max_index_p1, threshold/3)
                max_gap_important_p2 = if_gap_important(detection_sig_array_p2, detection_time_array_p2, max_index_p2, threshold/3)

            # Jump to the next iteration if any type of error is found in the puff detection.
            if error_p1 != None or error_p2 != None:
                # print(f'Edge error found in P1 time range ({detection_time_array_p1[0]:.1f},{detection_time_array_p1[-1]:.1f}) or P2 time range ({detection_time_array_p2[0]:.1f},{detection_time_array_p2[-1]:.1f})')
                pass
            # Jump to the next iteration if the structure at P1 is not a single puff.
            elif puff_n_p1 != 1:
                # print(f'P1 in time range ({detection_time_array_p1[0]:.1f},{detection_time_array_p1[-1]:.1f}) has no single puff')
                pass

            # Jump to the next iteration if a important gap is found
            elif big_gaps_in_p1 and max_gap_important_p1 or big_gaps_in_p2 and max_gap_important_p2:
                print(f'Important data gap found in P1 time range ({detection_time_array_p1[0]:.1f},{detection_time_array_p1[-1]:.1f}) or P2 time range ({detection_time_array_p2[0]:.1f},{detection_time_array_p2[-1]:.1f})')
                pass

            #If none of the above is there, record the details of this puff
            else:
                #Generate the details of the puff at P1
                head_position_p1 = compute_time_list_p1[puff_info_p1[0][0]]
                tail_position_p1 = compute_time_list_p1[puff_info_p1[-1][1]]
                center_position_p1 = (head_position_p1 + tail_position_p1) / 2
                puff_width_p1 = (tail_position_p1 - head_position_p1)

                # for the case where the turbulence structure at P1 is larger than size_limit_1*D, recognize it as a split, and jump to the next iteration.
                if puff_width_p1 > size_limit_1 / flow_speed_init * pipe_d or puff_width_p1 > size_limit_2 / flow_speed_init * pipe_d and m_shape_check(detection_sig_array_p1):
                    puff_type_p1 = 'SplitPuff'
                    pass

                else:
                    #If no puff is detected at P2, give "Nan" to the details.
                    if puff_n_p2 == 0:
                        head_position_p2 = 'Nan'
                        tail_position_p2 = 'Nan'
                        center_position_p2 = 'Nan'
                        puff_width_p2 = 'Nan'

                    #Else generate details of the puff at P2
                    else:
                        head_position_p2 = compute_time_list_p2[puff_info_p2[0][0]] + delay_in_seconds
                        tail_position_p2 = compute_time_list_p2[puff_info_p2[-1][1]] + delay_in_seconds
                        center_position_p2 = (head_position_p2 + tail_position_p2) / 2
                        puff_width_p2 = (tail_position_p2 - head_position_p2)

                        if puff_n_p2 == 1:
                            #Change the puff type at P2 to "split" if a single structure is bigger than size_limit_1*D
                            if puff_width_p2 > size_limit_1 / flow_speed_init * pipe_d or puff_width_p2 > size_limit_2 / flow_speed_init * pipe_d and m_shape_check(detection_sig_array_p2):
                                puff_type_p2 = 'SplitPuff'
                                puff_n_p2 = 2

                        #Change the puff type at P2 to "single" if a dual structure is smaller than 27D
                        elif puff_n_p2 == 2 and puff_width_p2 < 27 / flow_speed_init * pipe_d:
                            puff_type_p2 = 'SinglePuff'
                            puff_n_p2 = 1

                    #Record details into a pandas data frame
                    total_puff_tracked += 1

                    puff_logger = {'Re':Re,'RadiusRatio':RR,'PuffIndex':total_puff_tracked,'Point1':puff_type_p1,'Point1PuffNum':puff_n_p1,
                                'Point1H':head_position_p1,'Point1T':tail_position_p1,'Point1C':center_position_p1,'Point2':puff_type_p2,
                                'Point2PuffNum':puff_n_p2,'Point2H':head_position_p2,'Point2T':tail_position_p2,'Point2C':center_position_p2,
                                'BugShow':'Nan','Point1PuffWidth':puff_width_p1,'Point2PuffWidth':puff_width_p2}
                    puff_logger = pd.DataFrame([puff_logger])
                    # Suppress specific FutureWarning from pandas
                    with warnings.catch_warnings():
                        warnings.simplefilter("ignore", category=FutureWarning)
                        puff_log = pd.concat([puff_log, puff_logger], ignore_index=True)

                    if puff_n_p2 == 0:
                        total_decay += 1
                    elif puff_n_p2 >= 2:
                        total_split += 1

                    decay_rate = total_decay / total_puff_tracked
                    split_rate = total_split / total_puff_tracked

def minus_mean_laminar(frame, mean_laminar):
    if isinstance(mean_laminar, np.ndarray):
        frame = frame - mean_laminar + np.full_like(frame, 128)

    return frame

def recompute_laminar_mean_std(std_normalization_base, std_normalization_buffer, base_frame_buffer_mean, base_frame_buffer_mean_previous, recompute_laminar_escape_idx, recompute_laminar_threshold, recompute_laminar_threshold_step, recompute_laminar_threshold_ref):
    # for the first computation(when value of std_normalization_base is none), take the result directly
    if std_normalization_base == None:
        std_normalization_base = np.mean(std_normalization_buffer)
    # for the following cases, compare the difference of the new mean flow field with the old one, only take the new one whtn the difference is not too obvious.
    else:
        std_normalization_base_previous = std_normalization_base
        std_normalization_base = np.mean(std_normalization_buffer)
        std_normalization_base_diff = abs(std_normalization_base - std_normalization_base_previous)
        if std_normalization_base_diff > recompute_laminar_threshold:
            base_frame_buffer_mean = base_frame_buffer_mean_previous
            std_normalization_base = std_normalization_base_previous
            recompute_laminar_escape_idx += 1
            if recompute_laminar_escape_idx > 2:
                recompute_laminar_threshold += recompute_laminar_threshold_step
        else:
            recompute_laminar_escape_idx = 0
            recompute_laminar_threshold = recompute_laminar_threshold_ref

    return std_normalization_base, base_frame_buffer_mean, recompute_laminar_escape_idx, recompute_laminar_threshold

# Main funtion for P1 of the life-time change caused by small curvature experiment
def data_acquiring_thread_p1(hCamera, pFrameBuffer, image_resolution, data_queue, communication_queue, result_queue_p1, ROI, laminar_flow_frame_queue_p1, laminar_flow_result_queue_p1):
    global max_frame, recompute_laminar_threshold_ref, recompute_laminar_threshold_step_ref, exit_flag, compute_time_list_p1, std_sig_record_p1, std_sig_record_ori_p1, laminar_compute_flag_p1, display_switch
    global generate_laminar_field_switch, detection_window_size_frame_number, data_compute_switch, moving_average_filter_size, puff_detection_switch, time_shift, recompute_background_n, wait_frame_number
    global imaging_time_stampes_p1
    print(f'Point#1 starts working.')

    idx = 0 # index for looping
    indicator = 0 # indicator is the index for recomputation of laminar background
    compute_idx = 0 #index for data points computation
    display_idx = 0 #index for display
    center_idx = None #index for a section of signal for puff detection
    puff_type = None #single or split
    sync_time_stamp = None #
    v1 = None #v_previous for the point type detection
    std_normalization_std = None #Ref std value for signal normalization
    std_normalization_base = None #Mean std value of laminar flow for signal normalization
    base_frame_buffer_mean_previous = None
    recompute_laminar_threshold = recompute_laminar_threshold_ref #the purpose of this threshold is to rule out problematic newly computed mean laminar flow 
    recompute_laminar_threshold_step = recompute_laminar_threshold_step_ref # with longer waiting time, a bigger difference between the old and new mean laminar flow is acceptable
    recompute_laminar_escape_idx = 0
    real_time_data_frequency = 0 #Data frequency in real-time
    camera_frequency = 0 #Camera frequency in real-time
    compute_time_list_p1 = []
    std_sig_record_p1 = []
    imaging_time_stampes_p1 = []
    std_sig_record_ori_p1 = []

    base_frame_buffer = np.zeros((base_frame_number, ROI[1], ROI[3])) #buffer array for the laminar flow
    base_frame_buffer_mean = None  #mean laminar flow

    #start the data acquiring process
    while idx < max_frame and not exit_flag:

        # grab an image and get its time stamp
        frame, FrameHead = grab_an_image(hCamera, pFrameBuffer)
        frame_stamp_time = FrameHead.uiTimeStamp/10000
        imaging_time_stampes_p1.append(frame_stamp_time)
        #crop to the needed ROI
        frame_compute = np.float32(frame[ROI[0]:ROI[0]+ROI[1],ROI[2]:ROI[2]+ROI[3]])

        #At the beginning, record the starting time as a reference
        if idx == 0:
            record_start_time = frame_stamp_time
            last_frame_time = frame_stamp_time
            sync_time_stamp = wpt.time()

        if generate_laminar_field_switch:
            #record frames for the computation of mean laminar flow
            if indicator >= 0 and indicator < base_frame_number:
                base_frame_buffer[indicator-1,:,:] = frame_compute
                frame_compute = minus_mean_laminar(frame_compute, base_frame_buffer_mean)

            #At the end of recording,  frames for the computation of mean laminar flow
            elif indicator == base_frame_number:
                frame_compute = minus_mean_laminar(frame_compute, base_frame_buffer_mean)
                # for the first time, directly send the recorded frames for computing
                if idx == base_frame_number:
                    laminar_flow_frame_queue_p1.put(base_frame_buffer)
                    laminar_compute_flag_p1 = True

                # for the following times, send the recorded frames for computing after making a copy of the current mean laminar flow
                else:
                    base_frame_buffer_mean_previous = base_frame_buffer_mean
                    laminar_flow_frame_queue_p1.put(base_frame_buffer)
                    laminar_compute_flag_p1 = True

            # after the recording, prepare the image for further computing or check if the mean laminar flow is ready
            else:
                frame_compute = minus_mean_laminar(frame_compute, base_frame_buffer_mean)

                # take out the result of mean laminar flow, and do the flowlloing computation to make sure if the new one should be acceptted or not.
                if not laminar_flow_result_queue_p1.empty():
                    base_frame_buffer_mean , std_normalization_buffer = laminar_flow_result_queue_p1.get()
                    #recompute the mean std for the laminar flow
                    std_normalization_base, base_frame_buffer_mean, recompute_laminar_escape_idx, recompute_laminar_threshold = recompute_laminar_mean_std(std_normalization_base, std_normalization_buffer, base_frame_buffer_mean, base_frame_buffer_mean_previous, recompute_laminar_escape_idx, recompute_laminar_threshold, recompute_laminar_threshold_step, recompute_laminar_threshold_ref)

                    if std_normalization_std == None:
                        std_normalization_std = 8.235833333 * 1.693 * 0.8453181 * 0.56542 * 1.438 * 1.070125 * 1.0684375 * 1.185909091 * 1.16037037 * 0.922592593

        current_stamp_time = frame_stamp_time - record_start_time
        last_frame_time = current_stamp_time

        #when the puff dection is on and a turbulent section is detected initially, send this section of signal for further detection
        if center_idx != None and compute_idx == center_idx + detection_window_size_frame_number//2:
            detection_sig_array = np.array(std_sig_record_p1[start_idx:])
            detection_time_array = np.array(compute_time_list_p1[start_idx:])

            puff_info, puff_type, error = find_puff_in_real_time(detection_sig_array, start_idx, detection_time_array, threshold)
            if error != None:
                pass
            puff_n = len(puff_info)
            result_queue_p1.put([puff_n, puff_type, puff_info, detection_time_array, detection_sig_array, error])
            center_idx = None

        # When it is time to get a new data point(based on the data frequency), compute this data point
        compute_active_time_diff = current_stamp_time - compute_idx / data_frequency
        if data_compute_switch and compute_active_time_diff > 0:

            compute_time_list_p1.append(current_stamp_time)
            #compute the std value of the current moment
            try:
                std_value = (np.std(frame_compute)-std_normalization_base)/std_normalization_std
            except:
                std_value = np.std(frame_compute)

            # apply a moving average filter in real-time
            std_sig_record_p1.append(std_value)
            std_sig_record_ori_p1.append(std_value)
            std_sig_record_p1[-1] = moving_average_filter(std_sig_record_p1[-10:], moving_average_filter_size)
            filtered_std = std_sig_record_p1[-1]

            # Check if the current point is the starting or ending point of a section of turbulence
            v1, point_type  = point_type_check(v1, threshold, filtered_std)

            '''
            When the current section of signal is not in detection and a new turbulent section appear, 
            set the current compute_idx as the center as a new section of signal to be tested,
            meantime, send the start moment for this section to the other testing points.
            '''
            if puff_detection_switch and center_idx == None and point_type == 'start point':
                center_idx = compute_idx
                start_idx = center_idx - detection_window_size_frame_number//2
                start_time = compute_time_list_p1[start_idx]
                communication_queue.put(start_time+time_shift)

            '''
            When the waiting time for a new laminar section is finished, and an end point of turbulence is met, 
            set the indicator value to a negtive value, so that there is a high possibility that the new section of laminar flow is truly laminar.
            '''
            if indicator > recompute_background_n and center_idx != None and point_type == 'end point':
                indicator = - wait_frame_number

            compute_idx += 1

            # When it is time to update the display(based on the display frequency), do the computation accordingly.
            display_active_time_diff = current_stamp_time - display_idx / display_freq
            if display_switch and display_active_time_diff > 0:

                # Compute data frequency as well as camera frequency
                try:
                    real_time_data_frequency = compute_idx / current_stamp_time
                    camera_frequency = idx / current_stamp_time
                except:
                    continue

                display_idx += 1

                # prepare signal and send all necessary infomation to the data_queue for later display
                display_time_list = compute_time_list_p1[-1 * (plot_window_size_in_time + 2) * data_frequency:]
                display_signal_list = std_sig_record_p1[-1 * (plot_window_size_in_time + 2) * data_frequency:]
                data_queue.put([frame, display_time_list, display_signal_list, real_time_data_frequency, sync_time_stamp, camera_frequency])

        idx += 1
        indicator += 1

    # When the max frame number is excessed, stop all the threads by rising the exit_flag
    exit_flag = True
    # compute_time_stamp = np.array(compute_time_list_p1[1:])
    # compute_time_array = np.array(compute_time_list_p1)
    # compute_time_array = compute_time_array[1:] - compute_time_array[:-1]

# Main funtion for P2 of the life-time change caused by small curvature experiment
def data_acquiring_thread_p2(hCamera, pFrameBuffer, image_resolution, data_queue, communication_queue, result_queue_p2, result_queue_p1, ROI, laminar_flow_frame_queue_p2, laminar_flow_result_queue_p2):
    global exit_flag, compute_time_list_p2, std_sig_record_p2, std_sig_record_ori_p2, laminar_compute_flag_p2
    global imaging_time_stampes_p2
    print(f'Point#2 starts working.')

    idx = 0 # index for looping
    indicator = 0 # indicator is the index for recomputation of laminar background
    compute_idx = 0 #index for data points computation
    display_idx = 0 #index for display
    center_idx = None #index for a section of signal for puff detection
    puff_type = None #single or split
    sync_time_stamp = None #
    start_time = None
    v1 = None #v_previous for the point type detection
    std_normalization_std = None #Ref std value for signal normalization
    std_normalization_base = None #Mean std value of laminar flow for signal normalization
    base_frame_buffer_mean_previous = None
    recompute_laminar_threshold = recompute_laminar_threshold_ref #the purpose of this threshold is to rule out problematic newly computed mean laminar flow 
    recompute_laminar_threshold_step = recompute_laminar_threshold_step_ref # with longer waiting time, a bigger difference between the old and new mean laminar flow is acceptable
    recompute_laminar_escape_idx = 0
    real_time_data_frequency = 0 #Data frequency in real-time
    camera_frequency = 0 #Camera frequency in real-time
    imaging_time_stampes_p2 = []
    compute_time_list_p2 = []
    std_sig_record_ori_p2 = []
    std_sig_record_p2 = []

    base_frame_buffer = np.zeros((base_frame_number, ROI[1], ROI[3])) #buffer array for the laminar flow
    base_frame_buffer_mean = None  #mean laminar flow

    while idx < max_frame and not exit_flag:

        frame, FrameHead = grab_an_image(hCamera, pFrameBuffer)
        frame_stamp_time = FrameHead.uiTimeStamp/10000
        imaging_time_stampes_p2.append(frame_stamp_time)
        frame_compute = np.float32(frame[ROI[0]:ROI[0]+ROI[1],ROI[2]:ROI[2]+ROI[3]])

        if idx == 0:
            record_start_time = frame_stamp_time
            last_frame_time = frame_stamp_time
            sync_time_stamp = wpt.time()

        if generate_laminar_field_switch:
            if indicator >= 0 and indicator < base_frame_number:
                base_frame_buffer[indicator-1,:,:] = frame_compute
                frame_compute = minus_mean_laminar(frame_compute, base_frame_buffer_mean)

            elif indicator == base_frame_number:
                frame_compute = minus_mean_laminar(frame_compute, base_frame_buffer_mean)

                if idx == base_frame_number:
                    laminar_flow_frame_queue_p2.put(base_frame_buffer)
                    laminar_compute_flag_p2 = True
                else:
                    base_frame_buffer_mean_previous = base_frame_buffer_mean
                    laminar_flow_frame_queue_p2.put(base_frame_buffer)
                    laminar_compute_flag_p2 = True

            else:
                frame_compute = minus_mean_laminar(frame_compute, base_frame_buffer_mean)

                if not laminar_flow_result_queue_p2.empty():
                    base_frame_buffer_mean , std_normalization_buffer = laminar_flow_result_queue_p2.get()
                    std_normalization_base, base_frame_buffer_mean, recompute_laminar_escape_idx, recompute_laminar_threshold = recompute_laminar_mean_std(std_normalization_base, std_normalization_buffer, base_frame_buffer_mean, base_frame_buffer_mean_previous, recompute_laminar_escape_idx, recompute_laminar_threshold, recompute_laminar_threshold_step, recompute_laminar_threshold_ref)

                    if std_normalization_std == None:
                        std_normalization_std = 9.642916667 * 1.096705263 * 1.1040708 * 0.673125 * 0.860666667 * 1.4784375 * 1.343125 * 0.964090909 * 0.773333333 * 1.322592593

        current_stamp_time = frame_stamp_time - record_start_time
        last_frame_time = current_stamp_time

        if center_idx != None and compute_idx == center_idx + detection_window_size_frame_number//2:
            detection_sig_array = np.array(std_sig_record_p2[start_idx:])
            detection_time_array = np.array(compute_time_list_p2[start_idx:])

            # print(f'p2 t-range = {compute_time_list_p2[start_idx]:.2f}~{compute_time_list_p2[-1]:.2f}')

            puff_info, puff_type, error = find_puff_in_real_time(detection_sig_array, start_idx, detection_time_array, threshold)
            if error != None:
                pass
            puff_n = len(puff_info)
            result_queue_p2.put([puff_n, puff_type, puff_info, detection_time_array, detection_sig_array, error])

            center_idx = None

            # print(center_idx)


        compute_active_time_diff = current_stamp_time - compute_idx / data_frequency
        # print(f"compute_active_time_diff = {compute_active_time_diff}")
        if data_compute_switch and compute_active_time_diff > 0:

            compute_time_list_p2.append(current_stamp_time)
            try:
                std_value = (np.std(frame_compute)-std_normalization_base)/std_normalization_std
            except:
                std_value = np.std(frame_compute)

            std_sig_record_p2.append(std_value)
            std_sig_record_ori_p2.append(std_value)
            std_sig_record_p2[-1] = moving_average_filter(std_sig_record_p2[-10:], moving_average_filter_size)
            filtered_std = std_sig_record_p2[-1]

            # Check if the current point is the starting or ending point of a section of turbulence
            v1, point_type  = point_type_check(v1, threshold, filtered_std)

            if start_time == None and not communication_queue.empty():
                start_time = communication_queue.get()

            if center_idx == None and start_time != None and current_stamp_time > start_time:
                start_idx = int(start_time * data_frequency)
                center_idx = start_idx + detection_window_size_frame_number//2
                start_time = None

            if point_type == 'end point' and center_idx != None and indicator > recompute_background_n:
                indicator = - wait_frame_number

            compute_idx += 1
            display_active_time_diff = current_stamp_time - display_idx / display_freq
            # print(f'display_active_time_diff = {display_active_time_diff}')
            if display_switch and display_active_time_diff > 0:

                try:
                    real_time_data_frequency = compute_idx / current_stamp_time 
                    camera_frequency = idx / current_stamp_time
                except:
                    continue

                display_idx += 1

                display_time_list = compute_time_list_p2[-1 * (plot_window_size_in_time + 2) * data_frequency:]
                display_signal_list = std_sig_record_p2[-1 * (plot_window_size_in_time + 2) * data_frequency:]

                data_queue.put([frame, display_time_list, display_signal_list, real_time_data_frequency, sync_time_stamp,camera_frequency])

        idx += 1
        indicator += 1

    # compute_time_stamp = np.array(compute_time_list_p2[1:])
    # compute_time_array = np.array(compute_time_list_p2)

def check_data_gap(imaging_time_stampes_p1, imaging_time_stampes_p2,title_text, y_range = 1):
    imaging_time_stampes_array_display_p2 = np.array(imaging_time_stampes_p2)
    imaging_time_stampes_diff_display_p2 = imaging_time_stampes_array_display_p2[1:] - imaging_time_stampes_array_display_p2[:-1]
    imaging_time_stampes_array_display_p1 = np.array(imaging_time_stampes_p1)
    imaging_time_stampes_diff_display_p1 = imaging_time_stampes_array_display_p1[1:] - imaging_time_stampes_array_display_p1[:-1]
    
    # Create traces for each time series
    trace1 = go.Scatter(x=imaging_time_stampes_array_display_p2[1:]-imaging_time_stampes_array_display_p2[0]+delay_final, y=imaging_time_stampes_diff_display_p2, mode='lines', name='P2 time diff')
    # trace2 = go.Scatter(x=imaging_time_stampes_array_display_freq1_10k[1:]-imaging_time_stampes_array_display_freq1_10k[0], y=imaging_time_stampes_diff_display_freq1_10k, mode='lines', name='display freq 1 10k')
    trace3 = go.Scatter(x=imaging_time_stampes_array_display_p1[1:]-imaging_time_stampes_array_display_p1[0], y=imaging_time_stampes_diff_display_p1, mode='lines', name='P1 time diff')
    
    # Create the figure and add traces
    fig = go.Figure()
    fig.add_trace(trace1)
    # fig.add_trace(trace2)
    fig.add_trace(trace3)
    
    # Set plot layout
    fig.update_layout(
                      title=title_text,
                      xaxis_title='Time',
                      yaxis_title='Time difference between frames',
                      legend_title='Series',
                      xaxis=dict(
                          rangeslider=dict(
                              visible=True
                              ),
                      ))
    
    # Update y-axis range
    if y_range != 1:
        fig.update_yaxes(range=[0, y_range])  # Replace min_y_value and max_y_value with your desired values
    
    # Show the plot
    fig.show()
    

def toy_task_p1():
    global imaging_time_stampes_p1, toy_start_time
    toy_start_time = wpt.time()
    print("toy_task_p1 start")
    
    while not exit_flag:
        frame_stamp_time = wpt.time() - toy_start_time
        imaging_time_stampes_p1.append(frame_stamp_time)
        wpt.sleep(0.003)
    print("toy_task_p1 end")   
    
def toy_task_p2():
    global imaging_time_stampes_p2, toy_start_time
    print("toy_task_p2 start")
    
    while not exit_flag:
        frame_stamp_time = wpt.time() - toy_start_time
        imaging_time_stampes_p2.append(frame_stamp_time)
        wpt.sleep(0.003)
    print("toy_task_p2 end")

# def Looping_verification(hCamera, pFrameBuffer, )

def Looping_detection(hCamera, pFrameBuffer, looping_delay, control_center_ser, ROI, data_queue, laminar_flow_frame_queue_detection, laminar_flow_result_queue_detection):
    global exit_flag, compute_time_list_detection, std_sig_record_detection, std_sig_record_ori_detection, laminar_compute_flag_detection
    global puff_logger, data_frequency, constant_trigger_frequency, trigger_mode
    print(f'Puff detection starts working.')

    #initializing variables
    idx = 0 # index for looping
    indicator = 0 # indicator is the index for recomputation of laminar background
    compute_idx = 0 #index for data points computation
    display_idx = 0 #index for display
    
    sync_time_stamp = None #
    start_time = None
    v1 = None #v_previous for the point type detection
    std_normalization_std = None #Ref std value for signal normalization
    std_normalization_base = None #Mean std value of laminar flow for signal normalization
    base_frame_buffer_mean_previous = None
    recompute_laminar_threshold = recompute_laminar_threshold_ref #the purpose of this threshold is to rule out problematic newly computed mean laminar flow 
    recompute_laminar_threshold_step = recompute_laminar_threshold_step_ref # with longer waiting time, a bigger difference between the old and new mean laminar flow is acceptable
    recompute_laminar_escape_idx = 0
    real_time_data_frequency = 0 #Data frequency in real-time
    camera_frequency = 0 #Camera frequency in real-time
    
    compute_time_list_detection = []
    std_sig_record_ori_detection = []
    std_sig_record_detection = []
    trigger_time_list = []
    trigger_time_waiting_list = []
    puff_logger = []
    trigger_N = 0
    constant_trigger_idx = 0

    base_frame_buffer = np.zeros((base_frame_number, ROI[1], ROI[3])) #buffer array for the laminar flow
    base_frame_buffer_mean = None  #mean laminar flow

    #start acquiring images from camera
    while idx < max_frame and not exit_flag:

        #grab an image and get the metadata
        frame, FrameHead = grab_an_image(hCamera, pFrameBuffer)
        frame_stamp_time = FrameHead.uiTimeStamp/10000

        #get the cropped image for later
        frame_compute = np.float32(frame[ROI[0]:ROI[0]+ROI[1],ROI[2]:ROI[2]+ROI[3]])

        # set the start time for looping
        if idx == 0:
            record_start_time = frame_stamp_time + looping_delay
            # sync_time_stamp = wpt.time()

        #generate the base mean laminar flow 
        if generate_laminar_field_switch:
            #in the 0~base frame number range of indicator, collect all the frames for later
            if indicator >= 0 and indicator < base_frame_number:
                base_frame_buffer[indicator-1,:,:] = frame_compute
                #when there is a valid mean laminar flow, do the minus computation to the cropped image
                frame_compute = minus_mean_laminar(frame_compute, base_frame_buffer_mean)

            #when the indicator equals to the base_frame_number, send the collected frames for the computation of the mean laminar flow
            elif indicator == base_frame_number:

                #when there is a valid mean laminar flow, do the minus computation to the cropped image
                frame_compute = minus_mean_laminar(frame_compute, base_frame_buffer_mean)

                #for the first computation of the mean laminar flow, send directly
                if idx == base_frame_number:
                    laminar_flow_frame_queue_detection.put(base_frame_buffer)
                    laminar_compute_flag_detection = True
                
                #for the later computations of the mean laminar flow, record the current one before send frames.
                else:
                    base_frame_buffer_mean_previous = base_frame_buffer_mean
                    laminar_flow_frame_queue_detection.put(base_frame_buffer)
                    laminar_compute_flag_detection = True

            #for later iterations, get the processed image and check if the mean laminar flow is ready to be used.
            else:
                frame_compute = minus_mean_laminar(frame_compute, base_frame_buffer_mean)

                if not laminar_flow_result_queue_detection.empty():
                    base_frame_buffer_mean , std_normalization_buffer = laminar_flow_result_queue_detection.get()
                    std_normalization_base, base_frame_buffer_mean, recompute_laminar_escape_idx, recompute_laminar_threshold = recompute_laminar_mean_std(std_normalization_base, std_normalization_buffer, base_frame_buffer_mean, base_frame_buffer_mean_previous, recompute_laminar_escape_idx, recompute_laminar_threshold, recompute_laminar_threshold_step, recompute_laminar_threshold_ref)

                    if std_normalization_std == None:
                        std_normalization_std = 9.642916667 * 1.096705263 * 1.1040708 * 0.673125 * 0.860666667 * 1.4784375 * 1.343125 * 0.964090909 * 0.773333333

        #get the time of the current frame
        current_stamp_time = frame_stamp_time - record_start_time
        
        if trigger_mode == 'constant':
            trigger_time_diff = current_stamp_time - constant_trigger_idx / constant_trigger_frequency
            if trigger_time_diff > 0:
                activate_puff_actuator(control_center_ser)
                constant_trigger_idx += 1

        #check if here is an element in the waiting list of triggers, and trigger a puff if it is time.
        elif trigger_N != 0 and current_stamp_time > trigger_time_waiting_list[0]:
            activate_puff_actuator(control_center_ser)
            trigger_N -= 1
            trigger_time_waiting_list.remove(trigger_time_waiting_list[0])

        
        #check if it is time to generate a new data point, if yes, get a new data point.
        compute_active_time_diff = current_stamp_time - compute_idx / data_frequency
        if data_compute_switch and compute_active_time_diff > 0:
            
            compute_idx += 1
            #record time for computing
            compute_time_list_detection.append(current_stamp_time)

            #compute (normalized) std value of the processed image
            try:
                std_value = (np.std(frame_compute)-std_normalization_base)/std_normalization_std
            except:
                std_value = np.std(frame_compute)

            #record std value, and the filtered std value
            std_sig_record_detection.append(std_value)
            std_sig_record_ori_detection.append(std_value)
            std_sig_record_detection[-1] = moving_average_filter(std_sig_record_detection[-10:], moving_average_filter_size)
            filtered_std = std_sig_record_detection[-1]

            # Check if the current point is the starting or ending point of a section of turbulence
            v1, point_type  = point_type_check(v1, threshold, filtered_std)

            # Record the index of starting point type
            if point_type == 'start point':
                start_idx = compute_idx

            # Once reaches an end point, send the section of signal for detection of puff and further transfer into triggering timing.
            elif point_type == 'end point':
                detection_sig_array = np.array(std_sig_record_detection[start_idx : compute_idx])
                detection_time_array = np.array(compute_time_list_detection[start_idx : compute_idx])

                trigger_times = trigger_converter(detection_sig_array, detection_time_array)
                trigger_N += len(trigger_times)

                for trigger_time in trigger_times:
                    trigger_time_list.append(trigger_time)
                    trigger_time_waiting_list.append(trigger_time)
            
            #check if it is time for display, if yes, prepare and send data for display
            display_active_time_diff = current_stamp_time - display_idx / display_freq

            if display_switch and display_active_time_diff > 0:

                try:
                    real_time_data_frequency = compute_idx / current_stamp_time 
                    camera_frequency = idx / current_stamp_time
                except:
                    continue

                display_idx += 1

                display_time_list = compute_time_list_detection[-1 * (plot_window_size_in_time + 2) * data_frequency:]
                display_signal_list = std_sig_record_detection[-1 * (plot_window_size_in_time + 2) * data_frequency:]

                data_queue.put([frame, display_time_list, display_signal_list, real_time_data_frequency, sync_time_stamp,camera_frequency])

        idx += 1
        indicator += 1

def looping_display_exp():
    pass




def m_shape_check_with_location_output(sig):
    global data_frequency, threshold
    cutoff = 5  # Desired cutoff frequency in Hz
    # fs = 100  # Sampling rate in Hz
    order = 1  # Filter order
    filtered_sig = butter_lowpass_filter(sig, cutoff, data_frequency, order)
    filtered_sig_above = np.delete(filtered_sig, np.where(filtered_sig < threshold))
    maxima_indices = argrelextrema(filtered_sig_above, np.greater)[0]
    maxima_values = filtered_sig_above[maxima_indices]
    
    maxima_diff = abs(maxima_values[-1] - maxima_values[0])
    
    if maxima_values.shape[0] == 2 and maxima_diff <= threshold *2 / 3:
        #this following content of checking the gap deepth is added at 4/25/2024, it returns True as long as there are two peaks and the difference is not too big before this modification.
        # start from the 7th data point for verification of the periodic experiment.
        maxima_mean = (maxima_values[-1] + maxima_values[0])/2
        minima_indices = argrelextrema(filtered_sig_above, np.less)[0]
        minima_value = filtered_sig_above[minima_indices]
        if maxima_mean - minima_value > 0.4:
            return True, maxima_indices
        else:
            max_indice = maxima_indices[-1] if maxima_values[-1] - maxima_values[0] > 0 else maxima_indices[0]
            return False, max_indice
    else:
        return False, maxima_indices
    


def trigger_converter(detection_sig_array, detection_time_array):
    # Check if the structure is smaller than size_limit_0*D, if yes, do not recognize as a puff.
    # Check if the turbulence structure at is larger than size_limit_1*D, if yes, recognize it as a split. Then check if it is M shape, if yes, get the peak location moment for later.
    # Check if the turbulence structure at is smaller than size_limit_1*D but larger than size_limit_2*D. If yes, check if it is of M shape. If yes, recognize as a split and get the peak location moments for later.

    global pipe_status_logger, pipe_d, puff_size_limit_list, puff_logger, trigger_delay

    #unpack current status information of the system
    _, _, _, _, bulk_velocity = pipe_status_logger[-1]
    
    structure_duration = detection_time_array[-1] - detection_time_array[0]
    structure_duration_normalized = structure_duration / (pipe_d / bulk_velocity)

    if structure_duration_normalized < puff_size_limit_list[0]:
        return None
    
    elif structure_duration_normalized > puff_size_limit_list[1]:
        is_m_shape, trigger_idxes = m_shape_check_with_location_output(detection_sig_array)

        if is_m_shape:
            puff_times = detection_time_array[trigger_idxes]
            puff_type = 'm_shape' 
            puff_logger.append([puff_type, structure_duration_normalized, puff_times])
            
            
        else:
            sig_length = len(detection_time_array)
            puff_times = [detection_time_array[ int( 0.4 * sig_length)], detection_time_array[ int( 0.9 * sig_length)]]
            puff_type = 'big_non_m'
            puff_logger.append([puff_type, structure_duration_normalized, puff_times])

        return [puff_times[0] + trigger_delay, puff_times[-1] + trigger_delay]

    elif structure_duration_normalized > puff_size_limit_list[2]:
        is_m_shape, trigger_idxes = m_shape_check_with_location_output(detection_sig_array)

        if is_m_shape:
            puff_times = detection_time_array[trigger_idxes]
            puff_type = 'm_shape' 
            puff_logger.append([puff_type, structure_duration_normalized, puff_times])

            return [puff_times[0] + trigger_delay, puff_times[-1] + trigger_delay]
        else:
            puff_times = detection_time_array[trigger_idxes]
            puff_type = 'single' 
            puff_logger.append([puff_type, structure_duration_normalized, puff_times])

            return puff_times + trigger_delay
    else:
        is_m_shape, trigger_idxes = m_shape_check_with_location_output(detection_sig_array)
        puff_times = detection_time_array[trigger_idxes]
        maxima_values = detection_sig_array[trigger_idxes]
        puff_type = 'single' 

        if is_m_shape:
            trigger_idx = trigger_idxes[-1] if maxima_values[-1] - maxima_values[0] > 0 else trigger_idxes[0]
            puff_times = detection_time_array[trigger_idx]
        
        puff_logger.append([puff_type, structure_duration_normalized, puff_times])

        return puff_times + trigger_delay

def Re_control_center(Kp, iteration_time, ser, lower_step_limit):
    global exit_flag, Re_record, step_number, mass_flowrate_list_filtered, temperature_start, temperature_end, average_count, end_height_control_switch, exp_index, set_temp
    
    Re_control_test_list = []
    stepper_reso= -0.045
    height_Re_ratio = -0.66
    wpt.sleep(20)
    print(f'WT{iteration_time},Kp{Kp},ReAVe{average_count}')
    print("Time\tRe\t\tRe_error\tFlowrate\tTemperature\tdeltaT\tSet_temperature\tStep_number")
    start_time = wpt.time()

    while not exit_flag:
        current_Re = Re_record[-1][0]
        flowrate = Re_record[-1][1]
        temperature = (temperature_start + temperature_end)/2
        deltaT = temperature_start - temperature_end
        # mass_flowrate = mass_flowrate_list_filtered[-1][1]*60/1000
        # volume_flowrate_scale = mass_flowrate/water_density(temperature)*1000
        # _, Re_scale, _ = PipeFlowComputer_FR(pipe_d, temperature, volume_flowrate_scale, 1)
        # current_Re = Re_scale
        # Control error
        error = desired_Re - current_Re
        
        if abs(error) > 15:
            Kp_dynamic = Kp * abs(error) / 15
            if Kp_dynamic > 0.5:
                Kp_dynamic = 0.5
        else:
            Kp_dynamic = Kp
        
        current_time = wpt.time() - start_time
        # Proportional control
        if end_height_control_switch:
            step_number = int(Kp * error / height_Re_ratio / stepper_reso)
        else:
            step_number = 0

        if abs(step_number) > 4000:
            step_number = 0
            print("Possible error in the system")
        elif abs(step_number) < lower_step_limit:
            step_number = 0
        # print(f"step_number = {step_number}")
        print(f"{current_time:.0f}\t{current_Re:.2f}\t\t{error:.2f}\t\t{flowrate:.4f}\t\t{temperature:.2f}\t\t{deltaT:.3f}\t{set_temp:.2f}\t\t{step_number}")
        Re_control_test_list.append([current_time, current_Re, error, flowrate, temperature, deltaT, set_temp, step_number])

        # Adjust actuator position
        # change_exit_high(ser) # control center solution, needs serial port of control center as input
        change_end_height(ser) # Separate motor control solution

        # Wait before the next control cycle
        wpt.sleep(iteration_time)  # Adjust sleep time as needed for your system's response time
    Re_control_test_array = np.array(Re_control_test_list)
    np.savetxt(f"#{exp_index}_WT{iteration_time},Kp{Kp},ReAVe{average_count}.csv", Re_control_test_array, delimiter = ',')
        
def initialize_control_center(port_info):
    ser = serial.Serial(port_info, 9600, timeout=2)
    wpt.sleep(2)  # Wait for the connection to establish

    print("Control center initialized.")
    return ser

def initialize_end_height_control(port_info):
    ser = serial.Serial(port_info, 9600, timeout=2)
    wpt.sleep(2)  # Wait for the connection to establish

    print("End height control initialized.")
    return ser

def change_end_height(ser):
    global step_number

    command = f"{step_number}"
    ser.write(command.encode())  # Send command
    # set number of steps into 0 to make sure that
    # one given motion wouldn't be run two times
    step_number = 0


def release_end_height_control(ser):

    turn_off_laser(ser)
    ser.close()
    print("End height control released.")

def release_control_center(ser):

    turn_off_laser(ser)
    ser.close()
    print("Control center released.")


def send_control_command(ser):
    """
    Send command to Arduino to set the state of output pin and relay pin.
    puff_state: bool (True for HIGH, False for LOW)
    laser_state: bool (True for HIGH, False for LOW)
    step_number: number of steps for the stepper motor to move
    """
    global puff_state, laser_state, step_number
    # Prepare the command string based on the desired state of the pins，
    command = f"{int(puff_state)},{int(laser_state)},{step_number}\n"
    ser.write(command.encode())  # Send command
    # set number of steps into 0 to make sure that
    # one given motion wouldn't be run two times
    step_number = 0
    # print(f"Sent: {command.strip()}")  # Print what was sent for verification


def turn_on_laser(ser):
    global laser_state
    laser_state = 1
    send_control_command(ser)
    # print("Laser ON")


def turn_off_laser(ser):
    global laser_state
    laser_state = 0
    send_control_command(ser)
    # print("Laser OFF")


def activate_puff_actuator(ser):
    global puff_state
    puff_state ^= 1
    send_control_command(ser)
    # print("Puff actuator moved")


def change_exit_high(ser):
    send_control_command(ser)


def send_global_variables_Re_control(variable_warp):
    global actuator_delay, filtered, pipe_d, exit_flag, exp_mode, Re_frequency, puff_state, laser_state, step_number, desired_Re, trigger_interval, end_height_control_switch, exp_index

    actuator_delay, filtered, pipe_d, exit_flag, exp_mode, Re_frequency, puff_state, laser_state, step_number, desired_Re, trigger_interval, end_height_control_switch, exp_index = variable_warp


def format_cmd(cmd):
    return cmd.encode('ascii')

def initialize_kern_scale(kern_port, baud, time_out = 10000):
    ser = serial.Serial(
        port= kern_port,
        baudrate=  baud,
        parity=serial.PARITY_NONE,
        stopbits=serial.STOPBITS_ONE,
        bytesize=serial.EIGHTBITS,
        timeout= time_out
    )

    command = format_cmd('SI')
    ser.write(command)
    print("Kern scale initialized")
    
    return ser

def release_kern(ser):
    ser.close()
    print("Kern scale stopped")


def read_weight_kern(ser):
    # the default time to read a value from kern scale is 1s.
    weight = ''
    skip_list = ['',' ', '\r', 'k', 'g']
    while True:
        line=ser.read()
        if line.decode('ascii') == '\n':
            break
        elif line.decode('ascii') not in skip_list:
            weight += line.decode('ascii')
    weight = float(weight)
    return weight


def extract_number(text):
    # Use regular expression to find numbers in the string
    match = re.search(r'\d+\.\d+', text)
    if match:
        return float(match.group(0))  # Convert the matched string to a float
    else:
        return None  # No number found
        

def read_weight_inline_kern(ser):
    # the default time to read a value from kern scale is 1s.
    while True:
        if ser.in_waiting:
            line=ser.readline().decode('ascii')
            weight = extract_number(line)
            # print(weight, wpt.time()-start_time, idx)
            break
        else:
            wpt.sleep(0.001)
    return weight


def read_mass_flowrate(ser, average_number = 80):
    global weight_list, mass_flowrate_list_ori, mass_flowrate_list_filtered, exit_flag
    
    weight_list = []
    mass_flowrate_list_ori = []
    mass_flowrate_list_filtered = []

    weight_previous = read_weight_inline_kern(ser)-0.6004
    
    t_previous  = wpt.time()
    weight_list.append([0, weight_previous])
    t_start  = wpt.time()
    idx = 0
    while not exit_flag:

        weight = read_weight_inline_kern(ser)-0.6004
        t_current = wpt.time()
        weight_list.append([t_current-t_previous, weight])
        if idx > 1:
            mass_flowrate = (weight - weight_previous)/(t_current-t_previous)
            mass_flowrate_list_ori.append(mass_flowrate)
            mass_flowrate_filtered = sum(mass_flowrate_list_ori[-1*average_number:])/len(mass_flowrate_list_ori[-1*average_number:]) *1000
            mass_flowrate_list_filtered.append([t_current- t_start, mass_flowrate_filtered])

            # print(f"T = {(t_current- t_start):.2f} s")
            # print(f"{mass_flowrate_filtered*60:.2f}g/min")
            # print(f"Average over {len(mass_flowrate_list_ori[-1*average_number:])} points")
        weight_previous = weight
        t_previous = t_current
        idx += 1


def check_bath_set_temp(ser):
    ser.write(b"S\r")
    response = ser.readline().decode().strip()  # decode to convert bytes to str
    set_temperature = float(response[5:10])
    return set_temperature


def temperature_control_center(ser, iteration_time):
    global exit_flag, temperature_start, temperature_end, set_temp

    set_temp = check_bath_set_temp(ser)
    print(set_temp)
    wpt.sleep(20)

    while not exit_flag:
        # set_temp = check_bath_set_temp(ser)
        delta_t = temperature_start - temperature_end
        if abs(delta_t) > 0.01:
            if delta_t > 0:
                set_temp = set_temp - 0.03
            else:
                set_temp = set_temp + 0.03
            change_bath_temp(ser, set_temp)

            wpt.sleep(iteration_time*5)
        else:
            wpt.sleep(iteration_time)


def change_bath_temp(ser, set_temp):
    command = f"W S0 {set_temp:.2f}\r".encode('utf-8')
    ser.write(command)
    response = ser.readline().decode().strip()  # decode to convert bytes to str
    if response != '$':
        print("Can't sent curent temperature to the heat bath.")


def initialize_bath(bath_port):
    ser = serial.Serial(
                        bath_port,  #COM4
						baudrate = 4800,
						bytesize=8,
						#parity="NONE",
                        #stopbits=1,
						timeout = 2)

    print("Bath communication initialized.")
    wpt.sleep(2)
    return ser


def release_bath(ser):
    ser.close()
    print('Bath communication released.')