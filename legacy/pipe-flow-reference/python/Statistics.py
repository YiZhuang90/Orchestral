import numpy as np
from scipy import fftpack

def CI_w(x, n): # This function computes confidence interal using The Wilson interval (Statistical Science 2001, Vol. 16, No. 2, 101–133)
    k = 2.576 # k value for confidence levels of 99%
    p = x / n
    q = 1 - p
    CIPlus = ( x + k**2 / 2 ) / ( n + k**2 ) + ( k * n**0.5 ) / ( n + k**2 ) * ( p * q + k**2 / (4*n))**0.5
    CIMinus = ( x + k**2 / 2 ) / ( n + k**2 ) - ( k * n**0.5 ) / ( n + k**2 ) * ( p * q + k**2 / (4*n))**0.5
    CIRange = [CIMinus, CIPlus]
    return CIRange

def FourierFilter(Sig, Freq, FreqLimit):
    TimeStep = 1 / Freq
    SigFFT = fftpack.fft(Sig)

    Amplitude = np.abs(SigFFT)
    Power = Amplitude ** 2
    Angle = np.angle(SigFFT)

    SampleFreq=fftpack.fftfreq(Sig.size, d = TimeStep)

    LongPassFreqFFT = SigFFT.copy()
    LongPassFreqFFT[np.abs(SampleFreq) > FreqLimit] = 0
    LongPassSig = fftpack.ifft(LongPassFreqFFT)

    ShortPassFreqFFT = SigFFT.copy()
    ShortPassFreqFFT[np.abs(SampleFreq) < FreqLimit] = 0
    ShortPassSig = fftpack.ifft(ShortPassFreqFFT)
    
    return ShortPassSig.real, LongPassSig.real