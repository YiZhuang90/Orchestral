from scipy import fftpack
import numpy as np
import matplotlib.pyplot as plt
from scipy.signal import argrelextrema
from itertools import chain

def has_numbers(inputString):
    return any(char.isdigit() for char in inputString)

def PuffSCEGene(PuffPositions): #detect the start, center and end of puffs
    FlatPuffPositions = list(chain.from_iterable(PuffPositions)) 
    PuffSCE = [FlatPuffPositions[0], (FlatPuffPositions[0]+FlatPuffPositions[-1])/2, FlatPuffPositions[-1]]
    
    return PuffSCE

def BoxSize(SigData, TimeData, Idxes):
    TimePoints = TimeData[Idxes]
    SigPoints = SigData[Idxes]
    Length = TimePoints[-1] - TimePoints[0]
    Height = SigPoints.max() - SigPoints.min()
    Area = Length * Height
    return Area

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

def ReadSig(FilePath, FileInfo, VisualInfo):
    s ,Freq = FileInfo
    StartIdx, EndIdx,  Sensors = VisualInfo
    
    PressureData = np.zeros((EndIdx - StartIdx, len(Sensors)))
    PressureRecord = np.genfromtxt(FilePath, delimiter = '\t', dtype = None)
    for i,Sensor in enumerate(Sensors):
        PressureData[:, i] = PressureRecord[StartIdx : EndIdx, i] - PressureRecord[StartIdx : EndIdx, i].mean()# Point_A is the point at 250d
    
    PuffCount = int((EndIdx - StartIdx) // (s * Freq))
#     print(PuffCount)
    maxs = np.zeros((PuffCount, len(Sensors)))
    for j, Sensor in enumerate(Sensors):
        for i in range(PuffCount):
            sig = PressureData[int(i * (s * Freq)) : int((i + 1) * (s * Freq)), j]
            maxs[i, j] = sig.max()
    coef = 1 / np.mean(maxs, axis = 0)
    return PressureData, coef

def RangeBasedMapping(Data, Threshold, ThresholdSTD):
    Sig = Data.copy()
    DataSTD = Data.std()
    Mean = Sig.mean()
    Max = Sig.max()
    STD = Sig.std()
    print(Mean, STD, Sig.size)
    iterate = 0
    SigN = Sig.size
    while STD > Threshold:
        iterate += 1
        # print(iterate)
        Sig = np.delete(Sig, np.argwhere(abs(Sig - Mean)>2*STD))
        Mean = Sig.mean()
        STD = Sig.std()
        
        if SigN - Sig.size < 2:
            # print(f'Iterates:{iterate} //Signal too wavy, consider another mapping method')
            break
        SigN = Sig.size
        print(Mean, STD, Sig.size)
        # print(Mean, STD)
        # ax[i].plot([0,SampleSigList[i][1].size],[Mean,Mean],label = f'I{iterate},M{Mean:.2f},S{STD:.2f},N{Sig.size}')
    # print(Mean, STD, Sig.size)
    if iterate <1 or DataSTD < ThresholdSTD:
        ZoomRatio = 1
    else:
        ZoomRatio = 20/(Max-Mean)
        
    # print(iterate, Mean, ZoomRatio)
    SigZero = Data - Mean
    
    SigPD = (Data - Mean) * ZoomRatio
    return SigPD, SigZero, Mean, STD

def NoiseBasedMapping(Data,Threshold):
    Sig = Data.copy()
    Mean = Sig.mean()
    STD = Sig.std()
    # print(Mean, STD, Sig.size)
    iterate = 0
    SigN = Sig.size
    while STD > Threshold:
        iterate += 1
        # print(iterate)
        Sig = np.delete(Sig, np.argwhere(abs(Sig - Mean)>2*STD))
        Mean = Sig.mean()
        STD = Sig.std()
        if SigN - Sig.size < 5:
            print(f'Iterates:{iterate} //Signal too wavy, consider another mapping method')
            break
        SigN = Sig.size
        # print(Mean, STD, Sig.size)
        # print(Mean, STD)
        # ax[i].plot([0,SampleSigList[i][1].size],[Mean,Mean],label = f'I{iterate},M{Mean:.2f},S{STD:.2f},N{Sig.size}')
    ZoomRatio = Threshold/STD
    SigPD = (Data - Mean) * ZoomRatio
    return SigPD, iterate

# make the background signal value to zero base on the laminar flow signal
def LamZero(Sig, LamPieceN, ThresholdSTD = 0.5, MethodIdx=0, LamPercentage = 0.3):
    if MethodIdx == 1:
        LamN = int(Sig.shape[0] * LamPercentage)

        LamSig = Sig[:LamN].copy()
        LamMean = np.mean(LamSig)

        LamSTD = np.std(LamSig)
        # print(LamN, LamMean, LamSTD)
        ZeroedSig = Sig - LamMean
    else:
        Data = Sig.copy()
        SigSplit = np.array_split(Sig,3)
        MeanMin = 100
        for idx,Split in enumerate(SigSplit):
            if Split.mean()<MeanMin:
                MeanMin = Split.mean()
                MinIdx = idx
        Sig = SigSplit[MinIdx]
        Mean = Sig.mean()
        Max = Sig.max()
        STD = Sig.std()
        print(Mean, STD, Sig.size)
        iterate = 0
        SigN = Sig.size
        # Threshold = 0.5
        while STD > ThresholdSTD:
            iterate += 1
            # print(iterate)
            Sig = np.delete(Sig, np.argwhere(abs(Sig - Mean)>(1+0.1*iterate)*STD))
            Mean = Sig.mean()
            STD = Sig.std()
            print(Mean, STD, Sig.size)
            if SigN - Sig.size < 2:
                # print(f'Iterates:{iterate} //Signal too wavy, consider another mapping method')
                break
            SigN = Sig.size
            # print(Mean, STD, Sig.size)
            
            # ax[i].plot([0,SampleSigList[i][1].size],[Mean,Mean],label = f'I{iterate},M{Mean:.2f},S{STD:.2f},N{Sig.size}')
        LamSTD = 20
        LamMean = Mean
        
        for Piece in np.array_split(Data,LamPieceN):
            if Piece.std()< LamSTD:
                LamSTD = Piece.std()
        # print(Mean, STD)
        ZeroedSig = Data - LamMean
    print(LamSTD, LamMean)
    return ZeroedSig, LamSTD, LamMean

def PuffSeeker(Sig, DAxis,Threshold, GapThreshold = 8, PeakThreshold = 5):

    PuffIdxes = np.where(Sig>Threshold)[0]
    EdgeIdxes = []
    if PuffIdxes.size:
        DisconIdx = np.where(np.diff(PuffIdxes) !=1)[0]
        PuffN = DisconIdx.size + 1
        EdgeIdxes.append(PuffIdxes[0])
        for i in range(PuffN-1):
            EdgeIdxes.append(PuffIdxes[DisconIdx[i]])
            EdgeIdxes.append(PuffIdxes[DisconIdx[i]+1])
        EdgeIdxes.append(PuffIdxes[-1])
        # np.concatenate(([0],DisconIdx,[PuffIdxes[-1]]))
    else:
        PuffN = 0
        
    # print(EdgeIdxes)
    # DAxisEdges = DAxis[EdgeIdxes]
    PeakEdges = []
    for i in range(PuffN):
        FrontEdge = EdgeIdxes[2 * i]
        EndEdge = EdgeIdxes[2 * i + 1]
        # print(FrontEdge,EndEdge)
        XDArray = np.linspace(DAxis[FrontEdge-1],DAxis[FrontEdge],1000)
        YArray = np.linspace(Sig[FrontEdge-1],Sig[FrontEdge],1000)
        FrontDenseIdx = np.where(abs(YArray-Threshold) == abs(YArray-Threshold).min())[0][0]
        FrontEdgeXD = XDArray[FrontDenseIdx]
        
        if EndEdge+1 < DAxis.size:
            XDArray = np.linspace(DAxis[EndEdge],DAxis[EndEdge+1],1000)
            YArray = np.linspace(Sig[EndEdge],Sig[EndEdge+1],1000)
            EndDenseIdx = np.where(abs(YArray-Threshold) == abs(YArray-Threshold).min())[0][0]
            EndEdgeXD = XDArray[EndDenseIdx]
            # PeakEdges.append([FrontEdgeXD, EndEdgeXD])
        else:
            # XDArray = np.linspace(DAxis[EndEdge],DAxis[-1],1000)
            # YArray = np.linspace(Sig[EndEdge],Sig[-1],1000)
            # EndDenseIdx = np.where(abs(YArray-Threshold) == abs(YArray-Threshold).min())[0][0]
            EndEdgeXD = DAxis[-1]
        PeakEdges.append([FrontEdgeXD, EndEdgeXD])
    # print(PeakEdges)
    
    #Generate infomation for each peak
    PeakFeatures = []
    # i = 0
    # while PuffN != 0:
    #     #Filter Peaks, accept peaks with size larger than the length threshold
    #     if DAxisEdges[i+1]-DAxisEdges[i] > PeakThreshold:
    #         PeakFeatures.append([DAxisEdges[i],(DAxisEdges[i]+DAxisEdges[i+1])/2,DAxisEdges[i+1],DAxisEdges[i+1]-DAxisEdges[i]]) #Start, center, end, length
    #     i += 2
    #     PuffN -= 1
    # PeakFeatures = np.array(PeakFeatures)
    # PuffFeatures = PeakFeatures
    
    i = 0
    while PuffN != 0:
        #Filter Peaks, accept peaks with size larger than the length threshold
        if PeakEdges[i][1]-PeakEdges[i][0] > PeakThreshold:
            PeakFeatures.append([PeakEdges[i][0],(PeakEdges[i][0]+PeakEdges[i][1])/2,PeakEdges[i][1],PeakEdges[i][1]-PeakEdges[i][0]]) #Start, center, end, length
        i += 1
        PuffN -= 1
    PeakFeatures = np.array(PeakFeatures)
    PuffFeatures = PeakFeatures
    
    #Filter Peaks, delete peaks with size smaller than the size threshold
    try:
        #Filter Peaks, merge peaks too close to each other into a single puff
        Gaps = PeakFeatures[1:,0] - PeakFeatures[:-1,2]
        GapN = len(Gaps)
        FilteredGaps = Gaps
        while GapN != 0:
            if Gaps[GapN - 1] < GapThreshold:
                NewPuff = np.array([PeakFeatures[GapN - 1,0],(PeakFeatures[GapN,2]+PeakFeatures[GapN - 1,0])/2,PeakFeatures[GapN,2],PeakFeatures[GapN,2]-PeakFeatures[GapN - 1,0]])
                PuffFeatures = np.delete(PeakFeatures, [GapN - 1, GapN], axis=0)
                PuffFeatures = np.insert(PuffFeatures, GapN - 1, NewPuff, axis=0)
                FilteredGaps = np.delete(Gaps, GapN - 1)
            GapN -=1
    except:
        Gaps = []
    

    return PuffFeatures


def PuffFinder(PressurePointIdx, PerturbationIdx, data, Thres, Sensors, Notification):
    PuffTimeNarrow, PuffSigNarrow, s = data
    [LimitsS, LimitsC], SplitThre = Thres # LimitsS: limits for square sensors, LimitsC limits for Circular sensors.
    lowers = argrelextrema(PuffSigNarrow, np.less)[0]
    uppers = argrelextrema(PuffSigNarrow, np.greater)[0]
    
    # Merge extrema indexes
    ExtremaIdx = np.zeros((lowers.size + uppers.size)).astype(int)
    if lowers[0] > uppers[0]:
        for j in range(min(uppers.size,lowers.size)):
            ExtremaIdx[j * 2] = uppers[j]
            ExtremaIdx[j * 2 + 1] = lowers[j]
        if lowers.size != uppers.size:
            ExtremaIdx[-1] = uppers[-1]
    else:
        for j in range(min(uppers.size,lowers.size)):
            ExtremaIdx[j * 2] = lowers[j]
            ExtremaIdx[j * 2 + 1] = uppers[j]
        if lowers.size != uppers.size:
            ExtremaIdx[-1] = lowers[-1]
    ExtremaIdx = np.hstack((0, ExtremaIdx, PuffSigNarrow.size-1))
    ExtremaValues = PuffSigNarrow[ExtremaIdx]
    ExtremaPositions = PuffTimeNarrow[ExtremaIdx]

    ## Looking for puffs, and classifing puffs
    PuffPositionSnEs = []
    IdxPositionSnEs = []
    PuffHeights = []
    ExtremaSnEs = [] # starting points and Ending points
    PuffTypeReco = []
    LegList = []
    PuffWidthIdxList = []
    
    AreaLimit = 0.00175 / 3 * 4 / s
    SlugWidthLimit = 0.05 # questionable value
    #For signal acquired by square sensors
    if 's' in Sensors[PressurePointIdx]:
        BackgroundValue = (PuffSigNarrow[0] + PuffSigNarrow[-1]) / 2
        Indicator = [False]
#         IndicatorBG = [True] #Indicator for create background threshold value
        for j, [ExtremaValue,ExtremaPosition]  in enumerate(zip(ExtremaValues,ExtremaPositions)):
#             print(Indicator)
            if Indicator == []:
                Indicator = [False]
                
            if Indicator[0]:
                del Indicator[0]
                continue
            else:
#                 print(f'{j} ', end='')
                if j > 0 and j < len(ExtremaValues) - 1:
                    
                    if j == 1:
                        LeftHeight = ExtremaValue - ExtremaValues[j-1]
                        j0 = j
                    else:
#                         print(f'j = {j}')
                        for l in range(j-1):
                            if ExtremaValues[j-l] - ExtremaValues[j-1-l] > LimitsS[0]:                            
                                break
#                         print(f'l = {l}')
                        if l > 0 and l < j -2:
                            LeftHeight = ExtremaValue - max(ExtremaValues[j-l:j])
                            j0 = np.where(ExtremaValues == max(ExtremaValues[j-l:j]))[0][0] + 1
                        else:
                            LeftHeight = ExtremaValue - ExtremaValues[j-1]
                            j0 = j
#                         print(j0)
#                     print(f'j = {j}, LeftHeight = {LeftHeight}')
                        
#                     AreaLimitSlug = 0.00057 * 4 / s

                    if LeftHeight < -1 * LimitsS[0] and j < len(ExtremaValues) - 2:

#                         #create background threshold value for furture use

#                         print(f'j = {j}')
                        IdxListSlug = [ExtremaIdx[j]]
    #                     AreaLimitSlug = 0.00057
                        SubExtremaValues = ExtremaValues[j+1:]
    #                     SubExtremaPositions = ExtremaPositions[j+1:]
#                         print(f'Length = {len(ExtremaValues)}')
                        for k0 in range(len(ExtremaValues) - 2 -j):
                            k0 += 1
                            IdxListSlug.append(ExtremaIdx[j+k0])
                            AreaLimitSlug = (1 + k0) * AreaLimit
                            AreaSlug = BoxSize(PuffSigNarrow, PuffTimeNarrow, IdxListSlug)
#                             print(f'k = {k}, AreaSlug = {AreaSlug:0.4F}, limit = {AreaLimitSlug:0.4F}, IdxListSlug = {IdxListSlug}')        
                            if AreaSlug < AreaLimitSlug:
#                                 print(f'k = {k}, Continue')
                                Indicator.append(True)
                                continue
                            else:
                                k0 -= 1
                                IdxListSlug = IdxListSlug[:-1]
                                if k0 % 2 == 1:
                                    del Indicator[-1]
                                    k0 -= 1
                                    IdxListSlug = IdxListSlug[:-1]
                                Indicator.append(False)
#                                 print(f'k0 ={k0}')
#                                 print(f'k = {k}, Break, Area = {AreaSlug:0.4f}, , IdxListSlug = {IdxListSlug}')
                                break

                        # Remove the last part if backgroud is taken into account
                        if k0 > 0:
                            IdxListSlug.reverse()
#                             print(IdxListSlug)
                            PeakValues = PuffSigNarrow[IdxListSlug]
#                             print(PeakValues)
                            x = 0
#                             if max(ExtremaValues[j:j+k0+1]) - min(ExtremaValues[j:j+k0+1]) < LimitsS[0]:
#                             print(f'j={j}',ExtremaValues[j + k0 + 1] - min(ExtremaValues[j:j+k0+1]), LimitsS[0])
                            if ExtremaValues[j + k0 + 1] - min(ExtremaValues[j:j+k0+1]) > LimitsS[0]:
                                for innerIdx in range(k0):
                                    x += 1
#                                     print(f'j = {j}, k0 = {k0}, x = {x}, innerIdx = {innerIdx}')
#                                     print(-1 * (PeakValues[innerIdx + 1] - PeakValues[innerIdx]), 0.5*LimitsS[0])
                                    if -1 * (PeakValues[innerIdx + 1] - PeakValues[innerIdx]) > 0.5*LimitsS[0]:
                                        k1 = k0 - x
#                                         print(k1)
                                        break
                                    else:
                                        k1 = k0
                            else:
                                k1 = k0
                        else:
                            k1 = k0

#                         print(f'k = {k}, x = {x}')
                        for m in range(len(ExtremaValues) - j - k1 - 1):
#                             print(m, ExtremaValues[j + k1 + m] - ExtremaValues[j + k1 + m + 1], LimitsS[0])
                            if ExtremaValues[j + k1 + m] - ExtremaValues[j + k1 + m + 1] > LimitsS[0]:                            
                                break
#                         print(f'l = {l}')
#                         if m > 0:
#                             LeftHeight = ExtremaValue - max(ExtremaValues[j-l:j])
#                         print(j, np.where(ExtremaValues == max(ExtremaValues[j:j+k1+m+1]))[0][0])
#                         if np.where(ExtremaValues == min(ExtremaValues[j:j+k1+m+1]))[0][0] == j:
#                         print(m, len(ExtremaValues) - j - k1 - 2)
                        if m < len(ExtremaValues) - j - k1 - 2:
                            if np.where(ExtremaValues == max(ExtremaValues[j:j+k1+m+1]))[0][0] == j:
                                k2 = 0
#                                 print(k2)
                            else:
    #                             k2 = np.where(ExtremaValues == min(ExtremaValues[j:j+k1+m+1]))[0][0] - j - 1
                                k2 = np.where(ExtremaValues == max(ExtremaValues[j:j+k1+m+1]))[0][0] - j - 1
#                         else:
# #                             LeftHeight = ExtremaValue - ExtremaValues[j-1]
#                         print(f'm = {m}, k2 = {k2})
                        else:
                            k2 = k1

                        ShapeWidth = ExtremaPositions[j+k2] - ExtremaPositions[j0]
                        RightHeight = min(ExtremaValues[j:j+k2+1]) - ExtremaValues[j+k2+1]
                        Legs = [LeftHeight, RightHeight]
                        AveHeight = (LeftHeight + RightHeight) / 2
                        PuffPositionSnE = [ExtremaPositions[j0], ExtremaPositions[j + k2]]
                        
#                         print(f'j = {j}, j0 = {j0},l = {l}, k0 = {k0}, k1 = {k1}, k2 ={k2}, m = {m}, LeftLeg = {LeftHeight:0.3f}, RightLeg = {RightHeight:0.3f}, AveLeg = {AveHeight:0.3f}')
#                         print(f'AveHeight = {AveHeight}, LeftHeight = {LeftHeight}, RightHeight = {RightHeight}, j = {j}, k ={k2}')

                        if AveHeight < -1 * LimitsS[1] and RightHeight < -1 * LimitsS[0]: # Recognised as a puff or slug
                            if Notification:
                                try:
                                    print(f'j = {j}, j0 = {j0},l = {l}, k0 = {k0}, k1 = {k1}, k2 ={k2}, m = {m}, Position = {(ExtremaPositions[j0] + ExtremaPositions[j + k2])/2:0.2f}, AveHeight = {AveHeight:0.2f}, LeftHeight = {LeftHeight:0.2f}, RightHeight = {RightHeight:0.2f}')
                                except:
                                    print(f'j = {j}, j0 = {j0}, k0 = {k0}, k1 = {k1}, k2 ={k2}, m = {m}, Position = {(ExtremaPositions[j0] + ExtremaPositions[j + k2])/2:0.2f},AveHeight = {AveHeight:0.2f}, LeftHeight = {LeftHeight:0.2f}, RightHeight = {RightHeight:0.2f}')
                               
    #                         print(f'Area = {AreaSlug:0.4f}')
                            del Indicator[0]
#                             print(Indicator)
#                             print(PuffPositionSnE)
                            indexPositionSnE = [ExtremaIdx[j0], ExtremaIdx[j+k2]]# The beginning point is added to the ExtremaValues
                            PuffWidthIdx = [ExtremaIdx[j0-1], ExtremaIdx[j+k2+1]]
        
#                             ExtremaSnEs.append([j0,j+k2]) # starting point and length
                            IdxPositionSnEs.append(indexPositionSnE)
                            PuffHeights.append(AveHeight)
    #                     PuffTypes.append(PuffType)
                            LegList.append(Legs)
                            PuffPositionSnEs.append(PuffPositionSnE)
                            PuffWidthIdxList.append(PuffWidthIdx)
                
#                             print(f'ShapeWidth = {ShapeWidth}')
                            # Differentiate the types of structures
                            if ShapeWidth < SlugWidthLimit:
                                PuffType = 'SinglePuff'
#                             elif k > 0 and ShapeWidth < SlugWidthLimit:
#                                 PuffType = 'SplitPuff'
                            else:
                                PuffType = 'Slug'
                            PuffTypeReco.append(PuffType)
                            
                        else:
                            Indicator = [False]
#         print(PuffPositionSnEs)
        PuffHeights = np.array(PuffHeights)
        
#         print(f'IdxPositionSnEs = {IdxPositionSnEs}, {type(IdxPositionSnEs)}')
#         print(f'PuffPositionSnEs = {PuffPositionSnEs}, {type(PuffPositionSnEs)}')
        
        if len(IdxPositionSnEs)>1:
        # remove background waves been detected as puffs
            IdxSnE = [0,-1] # Indexes of starting and ending point

            for i in range(2):
#                 print(f'i = {i}')
                Idx1 = IdxPositionSnEs[IdxSnE[i]][IdxSnE[i]]
                RelaHeight = abs(PuffSigNarrow[Idx1] - BackgroundValue) #relative height
                if Notification:
                    print(f'i = {i}, RelaHeight = {RelaHeight}, limit2 = {abs(LegList[IdxSnE[i]][IdxSnE[i]])}')
                if RelaHeight < LimitsS[0] and abs(LegList[IdxSnE[i]][IdxSnE[i]]) < LimitsS[1]: # select puffs to be detected
#                 if RelaHeight < LimitsS[0] or abs(LegList[IdxSnE[i]][IdxSnE[i]] - RelaHeight) < LimitsS[1]: # select puffs to be detected
#                     print(f'i = {i},Ext# = {ExtremaSnEs[IdxSnE[i]][IdxSnE[i]]}, nearby# = {ExtremaSnEs[IdxSnE[i]][IdxSnE[i]] + PosiNega[i]}, RelaHeight = {RelaHeight:0.4f}, value 2 = {abs(LegList[IdxSnE[i]][IdxSnE[i]] - RelaHeight):0.4f}')
                    
                    del PuffPositionSnEs[IdxSnE[i]]
                    del IdxPositionSnEs[IdxSnE[i]]
                    del PuffTypeReco[IdxSnE[i]]
                    del PuffWidthIdxList[IdxSnE[i]]
#                     PuffPositionSnEs = np.delete(PuffPositionSnEs, IdxSnE[i])
#                     IdxPositionSnEs = np.delete(IdxPositionSnEs, IdxSnE[i])
                    PuffHeights = np.delete(PuffHeights, IdxSnE[i])

        # remove small-scale undershots
        if len(IdxPositionSnEs)>1:
            
#             PuffValues = PuffSigNarrow[IdxPositions]
            for j in range(len(IdxPositionSnEs) - 1):
                          
#                 if PuffHeights.min() / PuffHeights.max() > SplitThre[0] and PuffHeights.max() > SplitThre[1]:
                if PuffHeights.min() / PuffHeights.max() > SplitThre[0]:
                    
                    del PuffPositionSnEs[int(np.where(PuffHeights == PuffHeights.max())[0])]
                    del IdxPositionSnEs[int(np.where(PuffHeights == PuffHeights.max())[0])]
                    del PuffTypeReco[int(np.where(PuffHeights == PuffHeights.max())[0])]
                    del PuffWidthIdxList[int(np.where(PuffHeights == PuffHeights.max())[0])]
                    #                     PuffPositionSnEs = np.delete(PuffPositionSnEs, )
#                     IdxPositionSnEs = np.delete(IdxPositionSnEs, np.where(PuffHeights == PuffHeights.max())[0])
                    PuffHeights = np.delete(PuffHeights, np.where(PuffHeights == PuffHeights.max())[0])

    
        if len(PuffPositionSnEs) == 0:
            PuffStatus = [PerturbationIdx, 'Empty']
            PuffWidthSnE = [[],[]]
            
        elif len(PuffPositionSnEs) >= 1:
            PuffStatus = [PerturbationIdx, 'NonEmpty']
            PuffWidthSnE =  [PuffWidthIdxList[0][0],PuffWidthIdxList[-1][1]]


    #For signal acquired by circular sensors
#     elif 'c' in Sensors[PressurePointIdx]:
#         for j, ExtremaValue in enumerate(ExtremaValues):
#             if ExtremaValue - (PuffSigNarrow[0] + PuffSigNarrow[-1]) / 2 > LimitsC[0]:

#                 if j == 0:
#                     if abs(ExtremaValues[j + 1] - ExtremaValue) > LimitsC[1]:
#                         indexPosition = ExtremaIdx[j] # The beginning point is added to the ExtremaValues
# #                         puffPosition = PuffTimeNarrow[indexPosition]
# #                         PuffPositions.append(puffPosition)
#                         IdxPositions.append(indexPosition)
#                 elif j == len(ExtremaValues) - 1:
#                     if abs(ExtremaValue - ExtremaValues[j - 1]) > LimitsC[1]:
#                         indexPosition = ExtremaIdx[j] # The beginning point is added to the ExtremaValues
# #                         puffPosition = PuffTimeNarrow[indexPosition]
# #                         PuffPositions.append(puffPosition)
#                         IdxPositions.append(indexPosition)
#                 elif (abs(ExtremaValue - ExtremaValues[j - 1]) + abs(ExtremaValues[j + 1] - ExtremaValue)) / 2 > LimitsC[1]:
#                     indexPosition = ExtremaIdx[j] # The beginning point is added to the ExtremaValues
# #                     puffPosition = PuffTimeNarrow[indexPosition]
# #                     PuffPositions.append(puffPosition)
#                     IdxPositions.append(indexPosition)
#                 PuffPositions = PuffTimeNarrow[IdxPositions]

# #         How to differentiate a single puff and a split?

#         if len(PuffPositions) == 1:
#             PuffStatus = [PerturbationIdx, 'Single puff']
#         elif len(PuffPositions) >= 2:
#             PuffStatus = [PerturbationIdx, 'Split puff']
#         elif len(PuffPositions) == 0:
#             PuffStatus = [PerturbationIdx, 'No puff']
            
    Positions = [PuffPositionSnEs, IdxPositionSnEs]
    BottomNTop = [lowers, uppers]
    

    return Positions, PuffWidthSnE, BottomNTop, PuffStatus, ExtremaValues, PuffTypeReco

# def PuffFinder(PressurePointIdx, PerturbationIdx, data, Thres, Sensors, Notification):
#     PuffTimeNarrow, PuffSigNarrow, s = data
#     [LimitsS, LimitsC], SplitThre = Thres # LimitsS: limits for square sensors, LimitsC limits for Circular sensors.
#     lowers = argrelextrema(PuffSigNarrow, np.less)[0]
#     uppers = argrelextrema(PuffSigNarrow, np.greater)[0]
    
#     # Merge extrema indexes
#     ExtremaIdx = np.zeros((lowers.size + uppers.size)).astype(int)
#     if lowers[0] > uppers[0]:
#         for j in range(min(uppers.size,lowers.size)):
#             ExtremaIdx[j * 2] = uppers[j]
#             ExtremaIdx[j * 2 + 1] = lowers[j]
#         if lowers.size != uppers.size:
#             ExtremaIdx[-1] = uppers[-1]
#     else:
#         for j in range(min(uppers.size,lowers.size)):
#             ExtremaIdx[j * 2] = lowers[j]
#             ExtremaIdx[j * 2 + 1] = uppers[j]
#         if lowers.size != uppers.size:
#             ExtremaIdx[-1] = lowers[-1]
#     ExtremaIdx = np.hstack((0, ExtremaIdx, PuffSigNarrow.size-1))
#     ExtremaValues = PuffSigNarrow[ExtremaIdx]
#     ExtremaPositions = PuffTimeNarrow[ExtremaIdx]

#     ## Looking for puffs, and classifing puffs
#     PuffPositionSnEs = []
#     IdxPositionSnEs = []
#     PuffHeights = []
#     ExtremaSnEs = [] # starting points and Ending points
#     PuffTypeReco = []
#     LegList = []
#     PuffWidthIdxList = []
    
#     AreaLimit = 0.00175 / 3 * 4 / s
#     SlugWidthLimit = 0.05 # questionable value
#     #For signal acquired by square sensors
#     if 's' in Sensors[PressurePointIdx]:
#         BackgroundValue = (PuffSigNarrow[0] + PuffSigNarrow[-1]) / 2
#         Indicator = [False]
# #         IndicatorBG = [True] #Indicator for create background threshold value
#         for j, [ExtremaValue,ExtremaPosition]  in enumerate(zip(ExtremaValues,ExtremaPositions)):
# #             print(Indicator)
#             if Indicator == []:
#                 Indicator = [False]
                
#             if Indicator[0]:
#                 del Indicator[0]
#                 continue
#             else:
# #                 print(f'{j} ', end='')
#                 if j > 0 and j < len(ExtremaValues) - 1:
#                     if j == 1:
#                         LeftHeight = ExtremaValue - ExtremaValues[j-1]

#                     else:
# #                         print(f'j = {j}')
#                         for l in range(j-1):
#                             if ExtremaValues[j-l] - ExtremaValues[j-1-l] > LimitsS[0]:                            
#                                 break
# #                         print(f'l = {l}')
#                         if l > 0 and l < j -2:
#                             LeftHeight = ExtremaValue - max(ExtremaValues[j-l:j])
#                             j0 = np.where(ExtremaValues == max(ExtremaValues[j-l:j]))[0][0] + 1
#                         else:
#                             LeftHeight = ExtremaValue - ExtremaValues[j-1]
#                             j0 = j
# #                     print(f'j = {j}, LeftHeight = {LeftHeight}')
                        
# #                     AreaLimitSlug = 0.00057 * 4 / s

#                     if LeftHeight < -1 * LimitsS[0] and j < len(ExtremaValues) - 2:

# #                         #create background threshold value for furture use

# #                         print(f'j = {j}')
#                         IdxListSlug = [ExtremaIdx[j]]
#     #                     AreaLimitSlug = 0.00057
#                         SubExtremaValues = ExtremaValues[j+1:]
#     #                     SubExtremaPositions = ExtremaPositions[j+1:]
# #                         print(f'Length = {len(ExtremaValues)}')
#                         for k0 in range(len(ExtremaValues) - 2 -j):
#                             k0 += 1
#                             IdxListSlug.append(ExtremaIdx[j+k0])
#                             AreaLimitSlug = (1 + k0) * AreaLimit
#                             AreaSlug = BoxSize(PuffSigNarrow, PuffTimeNarrow, IdxListSlug)
# #                             print(f'k = {k}, AreaSlug = {AreaSlug:0.4F}, limit = {AreaLimitSlug:0.4F}, IdxListSlug = {IdxListSlug}')        
#                             if AreaSlug < AreaLimitSlug:
# #                                 print(f'k = {k}, Continue')
#                                 Indicator.append(True)
#                                 continue
#                             else:
#                                 k0 -= 1
#                                 IdxListSlug = IdxListSlug[:-1]
#                                 if k0 % 2 == 1:
#                                     del Indicator[-1]
#                                     k0 -= 1
#                                     IdxListSlug = IdxListSlug[:-1]
#                                 Indicator.append(False)
# #                                 print(f'k0 ={k0}')
# #                                 print(f'k = {k}, Break, Area = {AreaSlug:0.4f}, , IdxListSlug = {IdxListSlug}')
#                                 break

#                         # Remove the last part if backgroud is taken into account
#                         if k0 > 0:
#                             IdxListSlug.reverse()
# #                             print(IdxListSlug)
#                             PeakValues = PuffSigNarrow[IdxListSlug]
#                             x = 0
#                             if ExtremaValues[j + k0 + 1] - min(ExtremaValues[j:j+k0+1]) < LimitsS[0]:
#                                 for innerIdx in range(k0):
#                                     x += 1
#     #                                 print(f'j = {j}, k = {k}, x = {x}, innerIdx = {innerIdx}')
#     #                                 print(IdxListSlug)
#                                     if -1 * (PeakValues[innerIdx + 1] - PeakValues[innerIdx]) > LimitsS[0]:
#                                         k1 = k0 - x
#                                         break
#                                     else:
#                                         k1 = k0
#                             else:
#                                 k1 = k0
#                         else:
#                             k1 = k0

# #                         print(f'k = {k}, x = {x}')
#                         for m in range(len(ExtremaValues) - j - k1 - 1):
#                             if ExtremaValues[j + k1 + m] - ExtremaValues[j + k1 + m + 1] > LimitsS[0]:                            
#                                 break
# #                         print(f'l = {l}')
# #                         if m > 0:
# #                             LeftHeight = ExtremaValue - max(ExtremaValues[j-l:j])
# #                         print(j, np.where(ExtremaValues == max(ExtremaValues[j:j+k1+m+1]))[0][0])
#                         if np.where(ExtremaValues == max(ExtremaValues[j:j+k1+m+1]))[0][0] == j:
#                             k2 = 0
#                         else:
#                             k2 = np.where(ExtremaValues == max(ExtremaValues[j:j+k1+m+1]))[0][0] - j - 1
# #                         else:
# # #                             LeftHeight = ExtremaValue - ExtremaValues[j-1]
# #                         print(f'm = {m}, k2 = {k2})

#                         ShapeWidth = ExtremaPositions[j+k2] - ExtremaPositions[j0]
#                         RightHeight = min(ExtremaValues[j:j+k2+1]) - ExtremaValues[j+k2+1]
#                         Legs = [LeftHeight, RightHeight]
#                         AveHeight = (LeftHeight + RightHeight) / 2
#                         PuffPositionSnE = [ExtremaPositions[j0], ExtremaPositions[j + k2]]
                        
# #                         print(f'j = {j}, k = {k}, LeftLeg = {LeftHeight:0.3f}, RightLeg = {RightHeight:0.3f}, AveLeg = {AveHeight:0.3f}')
#     #                     print(f'AveHeight = {AveHeight}, LeftHeight = {LeftHeight}, RightHeight = {RightHeight}')

#                         if AveHeight < -1 * LimitsS[1] and RightHeight < -1 * LimitsS[0]: # Recognised as a puff or slug
#                             if Notification:
#                                 print(f'j = {j}, j0 = {j0},l = {l}, k0 = {k0}, k1 = {k1}, k2 ={k2}, m = {m}, Position = {(ExtremaPositions[j0] + ExtremaPositions[j + k2])/2:0.2f}, AveHeight = {AveHeight:0.2f}, LeftHeight = {LeftHeight:0.2f}, RightHeight = {RightHeight:0.2f}')
#     #                         print(f'Area = {AreaSlug:0.4f}')
#                             del Indicator[0]
# #                             print(Indicator)
# #                             print(PuffPositionSnE)
#                             indexPositionSnE = [ExtremaIdx[j0], ExtremaIdx[j+k2]]# The beginning point is added to the ExtremaValues
#                             PuffWidthIdx = [ExtremaIdx[j0-1], ExtremaIdx[j+k2+1]]
        
# #                             ExtremaSnEs.append([j0,j+k2]) # starting point and length
#                             IdxPositionSnEs.append(indexPositionSnE)
#                             PuffHeights.append(AveHeight)
#     #                     PuffTypes.append(PuffType)
#                             LegList.append(Legs)
#                             PuffPositionSnEs.append(PuffPositionSnE)
#                             PuffWidthIdxList.append(PuffWidthIdx)
                
# #                             print(f'ShapeWidth = {ShapeWidth}')
#                             # Differentiate the types of structures
#                             if ShapeWidth < SlugWidthLimit:
#                                 PuffType = 'SinglePuff'
# #                             elif k > 0 and ShapeWidth < SlugWidthLimit:
# #                                 PuffType = 'SplitPuff'
#                             else:
#                                 PuffType = 'Slug'
#                             PuffTypeReco.append(PuffType)
                            
#                         else:
#                             Indicator = [False]
# #         print(PuffPositionSnEs)
#         PuffHeights = np.array(PuffHeights)
        
# #         print(f'IdxPositionSnEs = {IdxPositionSnEs}, {type(IdxPositionSnEs)}')
# #         print(f'PuffPositionSnEs = {PuffPositionSnEs}, {type(PuffPositionSnEs)}')
        
#         if len(IdxPositionSnEs)>1:
#         # remove background waves been detected as puffs
#             IdxSnE = [0,-1] # Indexes of starting and ending point

#             for i in range(2):
# #                 print(f'i = {i}')
#                 Idx1 = IdxPositionSnEs[IdxSnE[i]][IdxSnE[i]]
#                 RelaHeight = abs(PuffSigNarrow[Idx1] - BackgroundValue) #relative height
#                 if Notification:
#                     print(f'i = {i}, RelaHeight = {RelaHeight}, limit2 = {abs(LegList[IdxSnE[i]][IdxSnE[i]])}')
#                 if RelaHeight < LimitsS[0] and abs(LegList[IdxSnE[i]][IdxSnE[i]]) < LimitsS[1]: # select puffs to be detected
# #                 if RelaHeight < LimitsS[0] or abs(LegList[IdxSnE[i]][IdxSnE[i]] - RelaHeight) < LimitsS[1]: # select puffs to be detected
# #                     print(f'i = {i},Ext# = {ExtremaSnEs[IdxSnE[i]][IdxSnE[i]]}, nearby# = {ExtremaSnEs[IdxSnE[i]][IdxSnE[i]] + PosiNega[i]}, RelaHeight = {RelaHeight:0.4f}, value 2 = {abs(LegList[IdxSnE[i]][IdxSnE[i]] - RelaHeight):0.4f}')
                    
#                     del PuffPositionSnEs[IdxSnE[i]]
#                     del IdxPositionSnEs[IdxSnE[i]]
#                     del PuffTypeReco[IdxSnE[i]]
#                     del PuffWidthIdxList[IdxSnE[i]]
# #                     PuffPositionSnEs = np.delete(PuffPositionSnEs, IdxSnE[i])
# #                     IdxPositionSnEs = np.delete(IdxPositionSnEs, IdxSnE[i])
#                     PuffHeights = np.delete(PuffHeights, IdxSnE[i])

#         # remove small-scale undershots
#         if len(IdxPositionSnEs)>1:
            
# #             PuffValues = PuffSigNarrow[IdxPositions]
#             for j in range(len(IdxPositionSnEs) - 1):
                          
# #                 if PuffHeights.min() / PuffHeights.max() > SplitThre[0] and PuffHeights.max() > SplitThre[1]:
#                 if PuffHeights.min() / PuffHeights.max() > SplitThre[0]:
                    
#                     del PuffPositionSnEs[int(np.where(PuffHeights == PuffHeights.max())[0])]
#                     del IdxPositionSnEs[int(np.where(PuffHeights == PuffHeights.max())[0])]
#                     del PuffTypeReco[int(np.where(PuffHeights == PuffHeights.max())[0])]
#                     del PuffWidthIdxList[int(np.where(PuffHeights == PuffHeights.max())[0])]
#                     #                     PuffPositionSnEs = np.delete(PuffPositionSnEs, )
# #                     IdxPositionSnEs = np.delete(IdxPositionSnEs, np.where(PuffHeights == PuffHeights.max())[0])
#                     PuffHeights = np.delete(PuffHeights, np.where(PuffHeights == PuffHeights.max())[0])

    
#         if len(PuffPositionSnEs) == 0:
#             PuffStatus = [PerturbationIdx, 'Empty']

#         elif len(PuffPositionSnEs) >= 1:
#             PuffStatus = [PerturbationIdx, 'NonEmpty']
#             PuffWidthSnE =  [PuffWidthIdxList[0][0],PuffWidthIdxList[-1][1]]


#     #For signal acquired by circular sensors
# #     elif 'c' in Sensors[PressurePointIdx]:
# #         for j, ExtremaValue in enumerate(ExtremaValues):
# #             if ExtremaValue - (PuffSigNarrow[0] + PuffSigNarrow[-1]) / 2 > LimitsC[0]:

# #                 if j == 0:
# #                     if abs(ExtremaValues[j + 1] - ExtremaValue) > LimitsC[1]:
# #                         indexPosition = ExtremaIdx[j] # The beginning point is added to the ExtremaValues
# # #                         puffPosition = PuffTimeNarrow[indexPosition]
# # #                         PuffPositions.append(puffPosition)
# #                         IdxPositions.append(indexPosition)
# #                 elif j == len(ExtremaValues) - 1:
# #                     if abs(ExtremaValue - ExtremaValues[j - 1]) > LimitsC[1]:
# #                         indexPosition = ExtremaIdx[j] # The beginning point is added to the ExtremaValues
# # #                         puffPosition = PuffTimeNarrow[indexPosition]
# # #                         PuffPositions.append(puffPosition)
# #                         IdxPositions.append(indexPosition)
# #                 elif (abs(ExtremaValue - ExtremaValues[j - 1]) + abs(ExtremaValues[j + 1] - ExtremaValue)) / 2 > LimitsC[1]:
# #                     indexPosition = ExtremaIdx[j] # The beginning point is added to the ExtremaValues
# # #                     puffPosition = PuffTimeNarrow[indexPosition]
# # #                     PuffPositions.append(puffPosition)
# #                     IdxPositions.append(indexPosition)
# #                 PuffPositions = PuffTimeNarrow[IdxPositions]

# # #         How to differentiate a single puff and a split?

# #         if len(PuffPositions) == 1:
# #             PuffStatus = [PerturbationIdx, 'Single puff']
# #         elif len(PuffPositions) >= 2:
# #             PuffStatus = [PerturbationIdx, 'Split puff']
# #         elif len(PuffPositions) == 0:
# #             PuffStatus = [PerturbationIdx, 'No puff']
            
#     Positions = [PuffPositionSnEs, IdxPositionSnEs]
#     BottomNTop = [lowers, uppers]
    

#     return Positions, PuffWidthSnE, BottomNTop, PuffStatus, ExtremaValues, PuffTypeReco
# # , ExtremaIdx

def PuffShow(PressurePointIdx, data, PuffPositionSnEs, IdxPositionSnEs, BottomNTop, ax , Sensors, ylim = [-0.2, 0.2], boxsize = 0.02):
    PuffTimeNarrow, PuffSigNarrow, s = data
    BackgroundHight = (PuffSigNarrow[0] + PuffSigNarrow[-1])/2
    PuffSigNarrow = PuffSigNarrow - BackgroundHight
#     puff_positions, index_positions = positions
    lowers, uppers = BottomNTop
    
    
    title = f'Point #{PressurePointIdx+1}'
    
    ## Visualize puffs
    ax.plot(PuffTimeNarrow,PuffSigNarrow)
    ax.set_title(title)
    if 's' in Sensors[PressurePointIdx]:
        for k in range(len(PuffPositionSnEs)):
            IdxRange = np.arange(IdxPositionSnEs[k][0], IdxPositionSnEs[k][1]+1)
            SigCut = PuffSigNarrow[IdxRange]
            SigMean = SigCut.mean()
            BoxEdge = SigCut.min() - boxsize if SigMean < 0 else SigCut.max() + boxsize
            ax.plot([PuffPositionSnEs[k][0]-boxsize/2,PuffPositionSnEs[k][1]+boxsize/2,PuffPositionSnEs[k][1]+boxsize/2,PuffPositionSnEs[k][0]-boxsize/2,PuffPositionSnEs[k][0]-boxsize/2],
                     [0,0,BoxEdge,BoxEdge,0])
#     elif 'c' in Sensors[PressurePointIdx]:
#         for k in range(len(PuffPositions)):
#             ax.plot([PuffPositions[k]-boxsize/2,PuffPositions[k]+boxsize/2,PuffPositions[k]+boxsize/2,PuffPositions[k]-boxsize/2,PuffPositions[k]-boxsize/2],
#                      [0,0,PuffSigNarrow[IdxPositions[k]]+boxsize,PuffSigNarrow[IdxPositions[k]]+boxsize,0])
    ax.scatter(PuffTimeNarrow[lowers],PuffSigNarrow[lowers],color = 'hotpink')
    ax.scatter(PuffTimeNarrow[uppers],PuffSigNarrow[uppers],color = 'grey')
    ax.set_ylim([ylim[0], ylim[1]])