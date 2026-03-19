import numpy as np

# density coefficients
dC = [-3.983035, 301.797, 522528.90, 69.34881, 999.97495]
# water dynamic viscosity coefficients
vC = [-3.7188, 578.919, -137.546]

def PipeFlowComputer_Re(D, T, Re, leng, Roug = 0): # Re number based

    dens = (1-(T+dC[0])**2*(T+dC[1])/(T+dC[3])/dC[2])*dC[4] # Density
    dynVis = np.exp(vC[0]+vC[1]/(vC[2]+T+273.15))/1000 # Dynamic viscosity
    kinVis = dynVis/dens # Kinematic viscosity

    bV = Re*kinVis/D # Bulk velocity
    fR = np.pi*D**2/4*bV*1000*60 # flow rate(l/min)

    lFF = 64/Re # Laminar friction factor
    tFF = 1/((-1.8*np.log10((Roug/D/3.71)**1.11+6.9/Re))**2) # Turbulent friction factor

    lPD = 0.5*dens*bV**2*leng/D*lFF# Laminar pressure drop(Pa)
    tPD = 0.5*dens*bV**2*leng/D*tFF# Turbulent pressure drop(Pa)

    uB_uTau = np.sqrt(8/tFF)
    ReT = Re/uB_uTau/2 # Re_tau
    fV = bV/uB_uTau*1000 # Friction velocity (mm/s)
    vL = kinVis/fV*1000000 # Viscous length (mm)
    vT = vL/fV*1000 # Viscous time (ms)
    details = [fR, lPD, tPD, ReT, fV, vL, vT]
    
    return bV, details

def PipeFlowComputer_FR(D, T, fR, leng, Roug = 0): # Flowrate number based
    dens = (1-(T+dC[0])**2*(T+dC[1])/(T+dC[3])/dC[2])*dC[4] # Density
    dynVis = np.exp(vC[0]+vC[1]/(vC[2]+T+273.15))/1000 # Dynamic viscosity
    kinVis = dynVis/dens # Kinematic viscosity

    bV = fR/60/1000/np.pi/D**2*4 # Bulk velocity
    Re = bV*D/kinVis
    # fR = np.pi*D**2/4*bV*1000*60 # flow rate(l/min)
    
    lPD = None
    tPD = None
    ReT = None
    fV = None
    vL = None
    vT = None
    
    if Re!= 0:
        lFF = 64/Re # Laminar friction factor
        tFF = 1/((-1.8*np.log10((Roug/D/3.71)**1.11+6.9/Re))**2) # Turbulent friction factor
    
        lPD = 0.5*dens*bV**2*leng/D*lFF# Laminar pressure drop(Pa)
        tPD = 0.5*dens*bV**2*leng/D*tFF# Turbulent pressure drop(Pa)

    # diaphragms = np.array([[14,16,18,20,22,24,26,28,30,32,34],[0.22, 0.35, 0.55, 0.86, 1.40 ,2.2, 3.5, 5.5, 8.6, 14.0, 22.0]])

    # for i in range(diaphragms.shape[1]):
    #     if lPD < diaphragms[1,i]*1000:
    #         lDia = diaphragms[0,i]
    #         break

    # for i in range(diaphragms.shape[1]):    
    #     if tPD < diaphragms[1,i]*1000:
    #         tDia = diaphragms[0,i]
    #         break

        uB_uTau = np.sqrt(8/tFF)
        ReT = Re/uB_uTau/2 # Re_tau
        fV = bV/uB_uTau*1000 # Friction velocity (mm/s)
        vL = kinVis/fV*1000000 # Viscous length (mm)
        vT = vL/fV*1000 # Viscous time (ms)+
    details = [lPD, tPD, ReT, fV, vL, vT]
    
    return bV, Re, details

    
def PipeFlowComputer_Piston(PiD, PiV, PiN, D, T, leng, Roug = 0): # PiD = Piston diameter, PiV = Piston velosity, PiN = Piston number
    dens = (1-(T+dC[0])**2*(T+dC[1])/(T+dC[3])/dC[2])*dC[4] # Density
    dynVis = np.exp(vC[0]+vC[1]/(vC[2]+T+273.15))/1000 # Dynamic viscosity
    kinVis = dynVis/dens # Kinematic viscosity

    bV = PiV*PiD**2*PiN/D**2 # Bulk velocity
    Re = bV*D/kinVis
    # fR = np.pi*D**2/4*bV*1000*60 # flow rate(l/min)

    lFF = 64/Re # Laminar friction factor
    tFF = 1/((-1.8*np.log10((Roug/D/3.71)**1.11+6.9/Re))**2) # Turbulent friction factor

    lPD = 0.5*dens*bV**2*leng/D*lFF# Laminar pressure drop(Pa)
    tPD = 0.5*dens*bV**2*leng/D*tFF# Turbulent pressure drop(Pa)

    diaphragms = np.array([[14,16,18,20,22,24,26,28,30,32,34],[0.22, 0.35, 0.55, 0.86, 1.40 ,2.2, 3.5, 5.5, 8.6, 14.0, 22.0]])

    for i in range(diaphragms.shape[1]):
        if lPD < diaphragms[1,i]*1000:
            lDia = diaphragms[0,i]
            break

    for i in range(diaphragms.shape[1]):    
        if tPD < diaphragms[1,i]*1000:
            tDia = diaphragms[0,i]
            break

    uB_uTau = np.sqrt(8/tFF)
    ReT = Re/uB_uTau/2 # Re_tau
    fV = bV/uB_uTau*1000 # Friction velocity (mm/s)
    vL = kinVis/fV*1000000 # Viscous length (mm)
    vT = vL/fV*1000 # Viscous time (ms)+
    details = [lPD, tPD, ReT, fV, vL, vT]
    
    return bV, Re, details
