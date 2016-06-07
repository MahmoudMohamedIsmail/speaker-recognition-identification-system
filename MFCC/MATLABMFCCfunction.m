function [MFCC] = MATLABMFCCfunction( signal, samplingRate)

%function [MFCC, MAG] = calculate_mfcc( fname);
%
% Calculates mel-frequency cepstral coefficients from a wave-file
% in a similar manner to HTK.

signal = signal(:);

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
% create frame blocking and windowing
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
frameLengthInSeconds = 0.02;
stepBetweenFramesInSeconds = 0.01;
windowSize = round( frameLengthInSeconds*samplingRate/2)*2; % 20 ms 
hammingWindow = hamming( windowSize);
windowStep = round(stepBetweenFramesInSeconds*samplingRate); % step 10 ms between frames

%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%build filterbank
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
numberOfFilterBanks = 26; % no. of filters in filterbank
numceps = 12; % no. of cepstral coefficients (number of Features)
L = 22; % liftering coefficient
numberOfFFTBins = 2048; % total length of fft
maximumFreqOnMelScale = 2595*log10( 1+(samplingRate/2)/700); % samplingRate/2 on mel scale
lifter = 1+L/2*sin( pi*(0:numceps).'/L); % lifter coefficients
frequenciesBins = (0:numberOfFFTBins/2)/(numberOfFFTBins)*samplingRate; % frequencies in half of fft
freqInMelScale = 2595*log10( 1 + frequenciesBins/700); % f on mel scale
for c=1:numberOfFilterBanks,
    % construct triangular filters on mel-scale for each channel
    % channel midfrequency is c*fs2mel/(numchannels+1)
    cdelta = 1*maximumFreqOnMelScale/(numberOfFilterBanks+1); % half of width of one filter channel
    cmid = c*maximumFreqOnMelScale/(numberOfFilterBanks+1);
    filterBank = 1 - abs(freqInMelScale-cmid)/cdelta;
    filterBank = max( filterBank, 0);
    filterBanks( c,:)= filterBank;    
end

frameStartIndex = 1;
frameind = 1;
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
% go through signal in frames and calculate MFCC's
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
while ( frameStartIndex + windowSize <= length( signal)),
    windowedFrame = signal( frameStartIndex+(0:windowSize-1)).*hammingWindow; % window
    fourierData = fft( windowedFrame, numberOfFFTBins);        % fft
    fourierData = fourierData(1:numberOfFFTBins/2+1);          % we need only half
    % calculate log magnitude for each channel
    for c = 1:numberOfFilterBanks,
        frameMagnitude( c) = 9 + log( sum( abs(fourierData).*filterBanks(c,:).')); % magic 9 from HTK       
    end
    MAG( :, frameind) = frameMagnitude(:); % store magnitudes for each frame
    cepstralCoeffs = dct( frameMagnitude); % take dct of magnitude vector
    c0 = sqrt(2)*cepstralCoeffs( 1); % scale and remember c0
    cepstralCoeffs = cepstralCoeffs(1:numceps+1).'.*lifter;     % lifter
    cepstralCoeffs(1) = []; % discard c0, keep only numceps
    MFCC( :, frameind) = [cepstralCoeffs; c0];  % shuffle into same order as HTK
        
    frameStartIndex = frameStartIndex + windowStep;
    frameind = frameind+1;
end



